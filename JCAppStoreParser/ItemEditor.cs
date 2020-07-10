using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using JCAppStore_Parser.JsonInfoFile;
using System.Threading;
using JCAppStore_Parser.Utils;

namespace JCAppStore_Parser
{
    /// <summary>
    /// Editor for an item : StoreItem
    /// </summary>
    public class ItemEditor
    {
        private StoreItem _item;
        private Category _parentCategory;
        private string _root;
        private StoreItem _newItem;
        private MainFile _source;
        private Lexers _lexers;
        
        private int _numOfFields;
        private volatile bool _dirty = false;
        private bool _isAdded = false;

        private List<AID> _aids;

        //synchronization and threading issues
        private bool _anyLockHeld { get =>  Monitor.IsEntered(_buildsLock) 
                || Monitor.IsEntered(_urlsLock)
                || Monitor.IsEntered(_appletNamesLock)
                || Monitor.IsEntered(_descriptionLock)
                || Monitor.IsEntered(_usageLock);
        }

        private readonly object _buildsLock = new object();
        private readonly object _urlsLock = new object();
        private readonly object _appletNamesLock = new object();
        private readonly object _descriptionLock = new object();
        private readonly object _usageLock = new object();

        //define editable fields
        public enum Fields
        {
            NAME, TITLE, APPLET_NAMES, ICON, VERSIONS_N_BUILDS, AUTHOR, DESCRIPTION, URLS, USAGE,
            KEYS, DEFAULT_SELECTED, PGP, SIGNED_BY
        }

        public ItemEditor(string rootDir, MainFile source, Category category, StoreItem item)
        {
            _item = item;
            _parentCategory = category;
            _root = rootDir;
            _newItem = _item.CopyUpdateable();
            _source = source;
            _numOfFields = Enum.GetValues(typeof(Fields)).Length;
        }

        public ItemEditor(string rootDir, MainFile source, Category category) : this(rootDir, source, category, StoreItem.Empty(category))
        {
            _dirty = true;
            _isAdded = true;
            _parentCategory.Add(_item);
        }

        /// <summary>
        /// Run interactive editor.
        /// </summary>
        public void Run()
        {
            EditorTools.PrintHeader($"Edit item: {_item}");
            PrintCommands();
            Console.Write($"{_item.Name} $ ");
            while (ParseCommand(Console.ReadLine()))
            {
                Console.Write($"{_item.Name} $ ");
            }
            Console.Clear();
        }

        /// <summary>
        /// Run chain editor: automatically asks for all the fields (adding a new item).
        /// </summary>
        public void Chain()
        {
            foreach (var field in Enum.GetValues(typeof(Fields)))
            {
                switch (field)
                {   //ignore these
                    case Fields.APPLET_NAMES:
                    case Fields.DEFAULT_SELECTED:
                    case Fields.PGP:
                    case Fields.SIGNED_BY:
                        continue;
                }
                EditField((Fields)field);
            }
            Console.Clear();
            ParseTextCommand("save");
            Console.WriteLine("..Entering interactive mode...");
            Console.WriteLine();
            Run();
        }

        private void PrintCommands()
        {

            var i = 1;
            foreach (var value in Enum.GetValues(typeof(Fields)))
            {
                Console.WriteLine($"  {i++}.\t {value}");
            }
            Console.WriteLine($"  help\t Show help menu.");
            Console.WriteLine($"  show\t Show original values and changes.");
            Console.WriteLine($"  save\t Save changes and validate (lighweight).");
            Console.WriteLine($"  gen\t Generate dependencies from SOURCE.");
            Console.WriteLine($"  exit\t Exit without saving.");
        }

        private bool ParseCommand(string cmd)
        {
            if (int.TryParse(cmd, out int value))
            {
                if (value < 1 || value > _numOfFields) Console.WriteLine("Invalid field.");
                _dirty = EditField((Fields)(value - 1));
                EditorTools.PrintHeader($"Edit item: {_item}");
                PrintCommands();
            }
            else
            {
                return ParseTextCommand(cmd);
            }
            return true;
        }

        private bool ParseTextCommand(string cmd)
        {
            switch (cmd.Trim().ToLower())
            {
                case "help":
                    PrintCommands();
                    return true;
                case "show":
                    Console.Clear();
                    Console.WriteLine("===================================");
                    Console.WriteLine($"ORIGINAL: {_item.Name}");
                    Console.WriteLine(_item.GetValues());
                    Console.WriteLine("===================================");
                    Console.WriteLine($"CHANGES: {_newItem.Name}");
                    Console.WriteLine(_newItem.GetValuesNotNull());
                    return true;
                case "save":
                    if (!checkExternalEditors()) return true;

                    Console.Clear();
                    _item.Update(_newItem);
                    var validation = _item.Validate();
                    _dirty = false;
                    if (validation != null && validation.Length > 0)
                    {
                        Console.WriteLine(validation);
                        Console.WriteLine("Changes has been saved. Consider fixing issues described above.");
                        return true; //do not close if invalid fields found.
                    }
                    return false;
                case "exit":
                    if (!checkExternalEditors()) return true;

                    if (_dirty)
                    {
                        //user agrees -> false as not to continue
                        if (EditorTools.AskIfSure("You've made some changes that are going to be lost. Exit?"))
                        {
                            if (_isAdded)
                            {
                                _parentCategory.Remove(_item);
                            }
                            return false;
                        }
                        else return true;
                    }
                    Console.Clear();
                    return false;
                case "gen":
                    if (_lexers == null)
                    {
                        var lexers = OptionsFactory.GetOptions().Get(Options.Values.KEY_LEXEM_FILE);
                        if (!File.Exists(lexers))
                        {
                            Console.WriteLine($"File does not exist: {Directory.GetCurrentDirectory()}\\{lexers}. " +
                                $"Fix the problem by creating one or fill in correct file path in app config file (relative) to the working directory.");
                            return true;
                        }
                        _lexers = new Lexers(lexers);
                    }
                    Console.WriteLine("Select a latest version this dependency list is legit for.");
                    var list = _item.Versions.ToList();
                    EditorTools.PrintOptions(list);
                    Console.Write("Select the version by number: ");
                    if (int.TryParse(Console.ReadLine(), out int value))
                    {
                        value--;
                        if (value > -1 && value < list.Count)
                        {
                            if(!DependenciesGenerator.CreateFile(list[value], _lexers,
                                Path.Combine(Path.Combine(_root, "JCApplets"), _item.Name))) {
                                Console.WriteLine("Could not open selected directory.");
                            }
                            return true;
                        }
                    }
                    Console.WriteLine("Invalid option.");
                    return true;
                default:
                    Console.WriteLine("Unknown command.");
                    return true;
            }

            bool checkExternalEditors()
            {
                if (_anyLockHeld)
                {
                    Console.WriteLine("Some fields are being edited in an external text editor. Close all to proceed.");
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Edit any field defined in StoreItem.Values
        /// </summary>
        /// <param name="field">field to edit</param>
        /// <returns></returns>
        private bool EditField(Fields field)
        {
            Console.Clear();
            switch (field)
            {
                case Fields.NAME:
                    {
                        return ParseName();
                    }
                case Fields.TITLE:
                    {
                        var result = EditorTools.EditString(Fields.TITLE.ToString(), 
                            _newItem.Title == null ? _item.Title : _newItem.Title);
                        if (result != null) _newItem.Title = result;
                        return result != null;
                    }
                case Fields.APPLET_NAMES:
                    return tryExecute(_appletNamesLock, Fields.APPLET_NAMES, EditAppletInstanceNamesAsynchronous);
                case Fields.ICON:
                    {
                        var result = EditImage(Fields.ICON.ToString(),
                            _newItem.Icon == null ? _item.Icon : _newItem.Icon);
                        _newItem.Icon = result;
                        if (_newItem == null) _newItem.Icon = "";
                        return result != null;
                    }
                case Fields.AUTHOR:
                    {
                        var result = EditorTools.EditString(Fields.AUTHOR.ToString(),
                            _newItem.Author == null ? _item.Author : _newItem.Author);
                        if (result != null) _newItem.Author = result;
                        return result != null;
                    }
                case Fields.KEYS:
                    {
                        bool? result = EditorTools.EditBool(Fields.KEYS.ToString(), _newItem.Keys.ToString());
                        if (result.HasValue) _newItem.Keys = result.Value;
                        return result.HasValue;
                    }
                case Fields.VERSIONS_N_BUILDS:
                    return tryExecute(_buildsLock, Fields.VERSIONS_N_BUILDS, EditVersionsAndBuildsAsynchronous);
                case Fields.URLS:
                    return tryExecute(_urlsLock, Fields.URLS, EditUrlsAsynchronous);
                case Fields.DEFAULT_SELECTED:
                    return EditDefaultSelected();
                case Fields.DESCRIPTION:
                    return tryExecute(_descriptionLock, Fields.DESCRIPTION, EditDescriptionAsynchronous);
                case Fields.USAGE:
                    return tryExecute(_usageLock, Fields.USAGE, EditUsageAsynchronous);
                case Fields.PGP:
                    {
                        var result = EditorTools.EditString(Fields.PGP.ToString(),
                            _newItem.Pgp == null || _newItem.Pgp.IsEmpty() ? _item.Pgp : _newItem.Pgp);
                        if (result != null) _newItem.Pgp = result;
                        return result != null;
                    }
                case Fields.SIGNED_BY:
                    {
                        var result = EditorTools.EditString(Fields.SIGNED_BY.ToString(),
                             _newItem.SignedBy == null || _newItem.SignedBy.IsEmpty() ? _item.SignedBy : _newItem.SignedBy);
                        if (result != null) _newItem.SignedBy = result;
                        return result != null;
                    }
            }
            return false;

            bool tryExecute(object lockObject, Fields lockField, Func<bool> executor)
            {
                if (Monitor.IsEntered(lockObject))
                {
                    Console.WriteLine($"Unable to edit {lockField} it is being edited now.");
                    return false;
                }

                ThreadPool.QueueUserWorkItem(_ => {
                    if (Monitor.TryEnter(lockObject))
                    {
                        try
                        {
                            if (executor())
                            {
                                _dirty = true;
                            }
                        }
                        finally
                        {
                            Monitor.Exit(lockObject);
                        }
                    }
                });
                return false;
            }
        }

        private bool ParseName()
        {
            var result = EditorTools.EditString(Fields.NAME.ToString(), _newItem.Name == null ? _item.Name : _newItem.Name,
                "The applet name serves as identifier. The assumed form is 'AppletDeveloperAppletName' without spaces.");
            if (result != null)
            {
                if (_parentCategory.Contains(new StoreItem { Name = result }))
                {
                    Console.WriteLine($"Invalid applet name: '{result}'. The name must be unique.");
                    return false;
                }
                if (!Directory.Exists($@"{_root}\JCApplets\{_newItem.Name}"))
                {
                    if (EditorTools.AskIfSure($"Applet name should reflect a folder binaries are in. Foler JCApplets/{_newItem.Name} does not exist. Really change?"))
                    {
                        _newItem.Name = result;
                        Directory.CreateDirectory($@"{_root}\JCApplets\{_newItem.Name}");
                        return true;
                    }
                    return false;
                }
                _newItem.Name = result;
            }
            return result != null;
        }

        private bool EditDefaultSelected()
        {
            if (_aids == null) _aids = ReadAppletAIDs();
            Console.Clear();
            if (_aids.Count > 1)
            {
                Console.WriteLine(" Error: binaries not found. Add .cap files to specify this field.");
                return false;
            }
            Console.WriteLine("Sellect applet to mark as default selected. Empty to leave unchanged.");
            EditorTools.PrintOptions(_aids);
            var line = Console.ReadLine().Trim();
            if (line.Length > 1) return false;
            if (int.TryParse(line, out int value))
            {
                if (value > 0 && value <= _aids.Count)
                {
                    _newItem.DefatulSelected = _aids[value--].ToString();
                    return true;
                }
            }
            Console.WriteLine("Invalid input. Nothing has changed.");
            return false;
        }

        private bool EditAppletInstanceNamesAsynchronous()
        {
            if (_aids == null)
            {
                _aids = ReadAppletAIDs();
                if (_aids == null) return false;
            }
            var file = Path.GetTempFileName();
            if (file == null)
            {
                Console.WriteLine("Unable to launch the editor.");
                return false;
            }
            using (var writer = new StreamWriter(file))
            {
                writer.WriteLine("Format: AppletName[, AID 10-32 characters long]. Specify names only, or ALL AIDs as well."
                    + (_aids.Count < 1 ? "Note: applet instances not loaded, format not checked." : ""));
                var data = _newItem.AppletNames == null ? _item.AppletNames : _newItem.AppletNames;
                if (data == null || data.Count < 1) //undefined
                {
                    foreach (var applet in _aids)
                    {
                        writer.WriteLine(applet.ToString());
                    }
                }
                else if (data[0].Equals("0x")) //[0x, applet, AID, applet, AID...]
                {
                    for (int i = 0; i < Math.Max(_aids.Count, data.Count / 2); i++)
                    {
                        if (2 * i + 2 < data.Count)
                        {
                            writer.Write($"{data[2 * i + 1]}, {data[2 * i + 2]}");
                            if (i >= _aids.Count) Console.WriteLine($" (DELETE_ME: Original AID's missing: Data missing or applet should have only {_aids.Count} instances.)");
                            else Console.WriteLine($" (DELETE_ME: original value: {_aids[i].ToString()})");
                        }
                        else
                        {
                            writer.Write(" FILL NAME , OPTIONALLY AID");
                            Console.WriteLine($" (DELETE_ME: original value: {_aids[i].ToString()})");
                        }
                    }
                }
                else //[applet, applet...]
                {
                    for (int i = 1; i < Math.Max(_aids.Count, data.Count); i++)
                    {
                        if (i < data.Count)
                        {
                            writer.Write(data[i]);
                            if (i >= _aids.Count) Console.WriteLine($" (DELETE_ME: Original AID's missing: Data missing or applet should have only {_aids.Count} instances.)");
                            else Console.WriteLine($" (DELETE_ME: original value: {_aids[i].ToString()})");
                        }
                        else
                        {
                            writer.Write(" FILL IN NAME , OPTIONALLY AID");
                            Console.WriteLine($" (DELETE_ME: original value: {_aids[i].ToString()})");
                        }
                    }
                }
            }

            new ExternalEditor().Edit(file);

            using (var reader = new StreamReader(file))
            {
                string line;
                var counter = 0;
                var applets = new List<string>();

                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.Length > 0)
                    {
                        if (ParseLine(line)) counter++;
                    }
                }
                _newItem.AppletNames = applets;

                bool ParseLine(string input)
                {
                    var data = input.Split(',').Select(x => x.Trim()).ToArray();
                    if (data.Length > 2)
                    {
                        if (counter > 0) Console.WriteLine($"Skipping: {input}, invalid format.");
                        return false;
                    }

                    if (counter < 1 && data.Length == 2) applets.Add("0x");
                    applets.Add(data[0]);
                    if (data.Length == 2) applets.Add(data[1]);
                    return true;
                }
            }
            File.Delete(file);
            return true;
        }

        private bool EditUrlsAsynchronous()
        {
            string file = Path.GetTempFileName();
            if (file == null)
            {
                Console.WriteLine("Unable to launch the editor.");
                return false;
            }
            var data = _newItem.Urls == null ? _item.Urls : _newItem.Urls;
            using (var writer = new StreamWriter(file))
            {
                if (_item.Urls != null) //enforce to create the file anyway..
                {
                    foreach (var url in _item.Urls)
                    {
                        writer.WriteLine($"{url.Key}; {url.Value}");
                    }
                } else
                {
                    writer.WriteLine("DELETE_ME: Include urls to official repository and useful tutorials. Format: 'link name;URL'. Each on a new line.");
                }
            }

            new ExternalEditor().Edit(file);

            using (var reader = new StreamReader(file))
            {
                string line = null;
                var counter = 0;
                var urls = new Dictionary<string, string>();

                while ((line = reader.ReadLine()) != null)
                {
                    counter++;
                    line = line.Trim();
                    if (line.Length > 0)
                    {
                        string[] url = line.Split(';').Select(x => x.Trim()).ToArray();
                        if (url.Length != 2) Console.WriteLine($"Invalid url syntax: line {counter} must have only one ';' smybol. Skipping...");
                        else if (urls.ContainsKey(url[0])) Console.WriteLine($"Invalid url syntax: line {counter} url name defined twice. Skipping...");
                        else urls.Add(url[0], url[1]);
                    }
                }
                _newItem.Urls = urls;
            }
            File.Delete(file);
            return true;
        }

        private bool EditVersionsAndBuildsAsynchronous()
        {
            var file = Path.GetTempFileName();
            if (file == null)
            {
                Console.WriteLine("Unable to launch the editor.");
                return false;
            }
            var data = _newItem.Builds == null ? _item.Builds : _newItem.Builds;
            using (var writer = new StreamWriter(file))
            {
                if (_item.Versions == null) _item.Versions = new SortedSet<string>();
                if (data == null) data = new Dictionary<string, SortedSet<string>>();

                writer.WriteLine("------ DELETE THIS SECTION ------");
                writer.WriteLine("Each version should have at least one SDK. Valid SDKs: " + FieldUtils.GetValues(StoreItem.SDK_VERSIONS));
                writer.WriteLine("Example: first line version, second line build list.");
                writer.WriteLine("1.0");
                writer.WriteLine("2.2.2, 2.1.2, 3.0.5");
                writer.WriteLine("------ DELETE THIS SECTION ------");

                foreach (var version in data.Keys)
                {
                    writer.WriteLine(version);
                    if (data.TryGetValue(version, out SortedSet<string> sdks))
                    {
                        writer.WriteLine(FieldUtils.GetAllValues(sdks, $"{version}: This version is missing data. Specify SDKs or delete this version."));
                    }
                    else
                    {
                        writer.WriteLine($"{version}: This version is missing data. Specify SDKs on a line below or delete this version.");
                        writer.WriteLine();
                    }
                }
            }

            new ExternalEditor().Edit(file);

            using (var reader = new StreamReader(file))
            {
                string line = null;
                var counter = 0;
                var versions = new SortedSet<string>();
                var builds = new Dictionary<string, SortedSet<string>>();
                SortedSet<string> sdks = null;

                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.Length > 0)
                    {
                        if (counter % 2 == 0)
                        {
                            if (versions.Contains(line))
                            {
                                Console.WriteLine($"Skipping: {line}, already present.");
                                sdks = builds[line];
                            }
                            else
                            {
                                sdks = new SortedSet<string>();
                                builds.Add(line, sdks);
                            }
                        }
                        else
                        {
                            if (sdks == null) continue;
                            var sdkValues = line.Split(',');
                            foreach (var value in sdkValues)
                            {
                                var res = value.Trim();
                                if (res.Length > 0 && !sdks.Contains(res)) sdks.Add(res);
                            }
                        }
                        counter++;
                    }
                }
                _newItem.Builds = builds;
            }
            File.Delete(file);
            return true;
        }

        private string EditImage(string key, string value)
        {
            var images = Directory.GetFiles($@"{_root}\Resources", "*", SearchOption.TopDirectoryOnly);
            EditorTools.PrintCurrentValue(key, value);
            Console.WriteLine("Choose an existing image or type a new name. The image existence can be verified later (e.g. you don't have to add the image immediatelly. Enter to leave without change.");
            if (images.Length == 0) Console.WriteLine("  [no images found]");
            EditorTools.PrintOptions(images);

            do
            {
                Console.WriteLine("Select number, Enter to skip or enter new image name (relative to the Resources/ folder):");
                var data = Console.ReadLine();
                if (data.Length == 0)
                {
                    return null;
                }
                if (int.TryParse(data, out int parsed))
                {
                    if (parsed > 0 && parsed <= images.Length)
                    {
                        return images[parsed];
                    }
                    Console.Write("Invalid. ");
                }
                else
                {
                    return data;
                }
            } while (true);
        }

        private bool EditDescriptionAsynchronous()
        {
            var text = _newItem.Description == null ? _item.Description : _newItem.Description;
            if (text == null || text.IsEmpty()) text = "<p>Write an applet description here. Use HTML. Describe the features.</p>";
            return EditTextHtmlAsynchronous(text, result => { _newItem.Description = result; });
        }

        private bool EditUsageAsynchronous()
        {
            var text = _newItem.Usage == null ? _item.Usage : _newItem.Usage;
            if (text == null || text.IsEmpty()) text = "<p>Write an applet use guide. Use HTML. Describe how to install and use the applet.</p>";
            return EditTextHtmlAsynchronous(text, result => { _newItem.Usage = result; });
        }

        private bool EditTextHtmlAsynchronous(string input, Action<string> output)
        {
            var file = Path.GetTempFileName();
            if (file == null)
            {
                Console.WriteLine("Unable to launch the editor.");
                return false;
            }
            file = $"{file}.html";
            using (var writer = new StreamWriter(file))
            {
                writer.Write(input);
            }

            new ExternalEditor().Edit(file);

            using (var reader = new StreamReader(file))
            {
                var builder = new StringBuilder();
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    builder.Append(line);
                }
                output(builder.ToString());
            }
            File.Delete(file);
            return true;
        }

        public List<AID> ReadAppletAIDs()
        {
            return ReadAppletAIDs($@"{_root}\JCApplets\{_item.Name}");
        }

        public static List<AID> ReadAppletAIDs(string dir)
        {
            if (!Directory.Exists(dir)) return new List<AID>();
            var files = Directory.GetFiles(dir, "*.cap", SearchOption.TopDirectoryOnly);
            if (!Directory.Exists(dir) || files.Length < 1)
            {
                Console.WriteLine("This applet binaries are unavailable. Do you still wish to edit applet names? (y to confirm)");
                if (!Console.ReadLine().Trim().Equals("y"))
                {
                    return null;
                }
                return new List<AID>();
            }
            else
            {
                return CAPFileParser.Parse(files[files.Length - 1]);
            }
        }
    }
}
