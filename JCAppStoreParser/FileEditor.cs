using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using JCAppStore_Parser.JsonInfoFile;

namespace JCAppStore_Parser
{
    /// <summary>
    /// Editor used to edit a JSON file for store.
    /// </summary>
    public class FileEditor
    {
        private string _root;
        public string Root { get => _root; }
        public MainFile File { get => _file; }


        private readonly MainFile _file;
        private readonly Dictionary<Command, Action> _commands;

        private bool _running = true;
        private int _depth = 0;
        private bool _dirty = false;

        
        private Category _curentCategory;
        private StoreItem _currentItem;
        
        public FileEditor(string fileName)
        {
    
            _file = MainFile.FromJson(fileName);
            _commands = new Dictionary<Command, Action>()
            {
                {new Command("save", "Save all changes done to the JSON file. This will OVERWRITE the original file edited."), Save },
                {new Command("exit", "Exit the editor."), Exit },
                //todo save
                {new Command("ls", "List current tree (categories or items or item contents)."), List },
                {new Command("cd", "Move deeper in the item tree."), ChangeDirectory },
                {new Command("help", "Print this help."), Help },
                {new Command("edit", "Edits current node."), Edit },
                {new Command("delete", "Removes current node."), Delete },
                {new Command("add", "Adds new child node at current level."), Add },
                {new Command("check", "Perform exhaustive verification against node and all its children."), RunVerification },

            };
        }

        /// <summary>
        /// Run editor.
        /// </summary>
        public void Run()
        {
            if (!CheckFileHierarchyInfoFile(_file.FileName, out _root))
            {
                Console.WriteLine("Closing editor...");
                return;
            }

            while (_running)
            {
                Console.Write($"{(_curentCategory == null ? "" : $"/{_curentCategory.Title}")}{(_currentItem == null ? "" : $"/{_currentItem.Title}")}$ ");
                if (_commands.TryGetValue(new Command(Console.ReadLine().Trim(), ""), out Action action))
                {
                    action();
                } 
                else
                {
                    Help();
                }
            } 
        }

        /// <summary>
        /// Run one command in editor and close.
        /// </summary>
        /// <param name="cmd">Command to run. See constructor for valid commands.</param>
        public void RunCommand(string cmd)
        {
            if (!CheckFileHierarchyInfoFile(_file.FileName, out _root))
            {
                Console.WriteLine("Closing editor...");
                return;
            }
            if (_commands.TryGetValue(new Command(cmd, ""), out Action action))
            {
                action();
            }
            else
            {
                Help();
            }
        }

        private void RunVerification()
        {
            var oldCategory = _curentCategory;
            var oldItem = _currentItem;
            var oldDepth = _depth;
            Console.Clear();

            RunDeepVerification(_depth);

            _curentCategory = oldCategory;
            _currentItem = oldItem;
            _depth = oldDepth;
        }

        private void RunDeepVerification(int depth, ILogger logger = null)
        {
            switch (depth)
            {
                case 0:
                    CheckMainFile();
                    break;
                case 1:
                    CheckCategory();
                    break;
                case 2:
                    CheckItem();
                    break;
                default:
                    throw new Exception("Invalid node for verification.");
            }

            void CheckMainFile()
            {
                Perform(_file.Aggregate(0, (x, y) => x + y.Count), () =>
                {
                    foreach (var category in _file)
                    {
                        logger.Log("%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%");
                        logger.Log("%%%%%%%%%%%%% CATEGORY SEPARETOR %%%%%%%%%%%%%%");
                        logger.Log("%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%");

                        _depth = 1;
                        _curentCategory = category;
                        RunDeepVerification(1, logger);
                    }
                });
            }

            void CheckCategory()
            {
                Perform(_curentCategory.Count, () =>
                {
                    logger.Log(_curentCategory.ToString());
                    foreach (var item in _curentCategory)
                    {
                        _depth = 2;
                        _currentItem = item;
                        RunDeepVerification(2, logger);
                    }
                });
            }

            void CheckItem()
            {
                Perform(1, () =>
                {
                    logger.Log(_currentItem.Title);
                    string directory = $@"{_root}\JCApplets\{_currentItem.Name}";
                    _currentItem.ValidateExhaustive(_root, directory, ItemEditor.ReadAppletAIDs(directory), logger.Log);
                    logger.Step();
                });               
            }

            void Perform(int loggerMaxValue, Action action)
            {
                if (logger == null)
                {
                    using (logger = new ProgressLogger(loggerMaxValue))
                    {
                        action();
                    }
                }
                else action();
            }
        }

        private void Save()
        {
            if (!_dirty)
            {
                Console.WriteLine("Nothing to save.");
                return;
            }
            _file.ToJson();
            _dirty = false;
            Console.WriteLine("Saved.");
        }

        private void Help() => _commands.All(x => { Console.WriteLine(x.Key); return true; });

        private void Exit()
        {
            if (_dirty)
            {
                //user agrees -> false as not to continue
                _running = !EditorTools.AskIfSure("You've made some changes that are going to be lost. Exit?");
            } 
            else
            {
                _running = false;
            }
           
        }

        private void ChangeDirectory()
        {
            List();
            Console.Write("Enter node number: ");
            var num = Console.ReadLine();
            if (!int.TryParse(num, out int idx))
            {
                Console.Write("Invalid node.");
                return;
            }

            if (idx <= 0)
            {
                DecreaseDepth(idx);
                return;
            }
            IncreaseDepth(--idx);
        }

        private void DecreaseDepth(int selectedNode)
        {
            if (selectedNode == 0 && _depth > 0)
            {
                switch (_depth)
                {
                    case 1:
                        _curentCategory = null;
                        break;
                    case 2:
                        _currentItem = null;
                        break;
                }
                _depth--;
            }
        }

        private void IncreaseDepth(int selectedNode)
        {
            switch (_depth)
            {
                case 0:
                    if (selectedNode > _file.Count)
                    {
                        Console.WriteLine("Invalid category name.");
                    }
                    else
                    {
                        _curentCategory = _file[selectedNode];
                        _depth++;
                    }
                    break;
                case 1:
                    if (selectedNode > _curentCategory.Count)
                    {
                        Console.WriteLine("Invalid file name.");
                    }
                    else
                    {
                        _currentItem = _curentCategory[selectedNode];
                        _depth++;
                    }
                    break;
                case 2:
                    Console.WriteLine(_currentItem);
                    break;
            }
        }

        private void List()
        {
            Console.WriteLine("0. ../  go up.");
            switch (_depth)
            {
                case 0:
                    Console.WriteLine(_file.GetContents());
                    break;
                case 1:
                    Console.WriteLine(_curentCategory.GetContents());
                    break;
                case 2:
                    Console.WriteLine(_currentItem);
                    break;

            }
        }

        private void Edit()
        {
            switch (_depth)
            {
                case 0:
                    Console.WriteLine("Root File is uneditable.");
                    break;
                case 1:
                    new CategoryEditor(_file, _curentCategory).Run();
                    break;
                case 2:
                    new ItemEditor(_root, _file, _curentCategory, _currentItem).Run();
                    break;
                default:
                    Console.WriteLine("Invalid node for this action.");
                    return;
            }
            _dirty = true;
        }

        private void Add()
        {
            switch (_depth)
            {
                case 0:
                    new CategoryEditor(_file).Chain();
                    break;
                case 1:
                    new ItemEditor(_root, _file, _curentCategory).Chain();
                    break;
                default:
                    Console.WriteLine("Invalid node for this action.");
                    return;
            }
            _dirty = true;
        }

        private void Delete()
        {
            string node;
            Action action;
            switch (_depth)
            {
                case 1:
                    node = _curentCategory.Title;
                    //todo did not work..?
                    action = () => { _file.Remove(_curentCategory); _curentCategory = null; _depth--; };
                    break;
                case 2:
                    node = _currentItem.Title;
                    action = () => { _curentCategory.Remove(_currentItem); _currentItem = null; _depth--; };
                    break;
                default:
                    Console.WriteLine("Invalid node for this action.");
                    return;
            }
            if (EditorTools.AskIfSure($"Are you sure you want to discard {node} node (and ALL its subnodes?")) {
                action();
                Console.WriteLine("Removed.");
            }
            _dirty = true;
        }

        public static bool CheckFileHierarchyInfoFile(string filename, out string rootDir)
        {
            rootDir = Path.GetFullPath(Path.GetDirectoryName(filename));
            return CheckFileHierarchy(rootDir);
        }

        public static bool CheckFileHierarchy(string rootDir)
        {
            Console.WriteLine($"Performing check on the root directory: {rootDir}");
            if (!Directory.Exists($@"{rootDir}\Resources"))
            {
                Console.WriteLine("The info_[lang].json file must be in JCAppStore/ format directory: missing Resources/ folder");
                return false;
            }
            if (!Directory.Exists($@"{rootDir}\JCApplets"))
            {
                Console.WriteLine("The info_[lang].json file must be in JCAppStore/ format directory: missing JCApplets/ folder");
                return false;
            }
            else
            {
                Console.WriteLine("JCApplets found.");
                Console.WriteLine("Applets: ");
                foreach (var dir in Directory.GetDirectories($@"{rootDir}\JCApplets"))
                {
                    Console.WriteLine($"  {dir}/");
                }
            }
            return true;
        }
    }
}
