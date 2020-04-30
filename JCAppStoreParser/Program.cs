using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JCAppStore_Parser.JsonInfoFile;
using JCAppStore_Parser.Utils;

namespace JCAppStore_Parser
{
    /// <summary>
    /// Program parsing input commands and processing them.
    /// </summary>
    public static class Program
    {
        private delegate int Executor(Command command, List<Command> aux = null);

        private static Dictionary<Command, Executor> _commands;
        private static Dictionary<Command, Func<string, string>> _auxiliary;

        //--edit --file ..\..\..\..\..\..\JCAppStore\store\info_en.json
        public static int Main(string[] args)
        {
            _auxiliary = new Dictionary<Command, Func<string, string>>()
            {
                { new Command("--file", "Specifies a JSON file to work with in other commands.", "JSON store file"), CheckIsJson},
                { new Command("-f", "Specifies a JSON file to work with in other commands.", "JSON store file"), CheckIsJson},
                { new Command("--meta", "Specifies the translation file to parse.", "Text file for translation."), x => null },
                { new Command("-m", "Specifies the translation file to parse.", "Text file for translation."),  x => null },
                { new Command("--directory", "Specifies root directory of hte JCAppStoreContent.", "Existing root directory."),
                    x => FileEditor.CheckFileHierarchy(x) ? null : "Invalid directory. does crucial folders exist (JCApplets/Resources folders)?"},
                { new Command("-d", "Specifies root directory of hte JCAppStoreContent.", "Existing root directory."), 
                    x => FileEditor.CheckFileHierarchy(x) ? null : "Invalid directory. does crucial folders exist (JCApplets/Resources folders)?"},
            };

            _commands = new Dictionary<Command, Executor>()
            {
                { new Command("--help", "Print help.\t"), Help },
                { new Command("-h", "Print help.\t"), Help },
                { new Command("--edit", "Run interactive editor to edit given JSON store file. Requires --file/-f.\t"), Edit},
                { new Command("-e", "Run interactive editor to edit given JSON store file. Requires --file/-f.\t"), Edit},
                { new Command("--to-meta", "Generates an easily-readable file for translation. " +
                      "Requires --file/-f (input) and --meta/-m (output, stdout if not specified)."), GenerateToTranslationDocument},
                { new Command("-t", "Generates an easily-readable file for translation. " +
                      "Requires --file/-f (input) and --meta/-m (output, stdout if not specified)."), GenerateToTranslationDocument},
                { new Command("--to-file", "Generates new json file based on meta translation provided. " +
                      "Requires --file/-f (original file) and --meta/-m (translated file, stdout if not specified).", 
                      "language the info file is translated to."), GenerateFromTranslationDocument},
                { new Command("-g", "Generates new json file based on meta translation provided. " +
                      "Requires --file/-f (original file) and --meta/-m  (translated file, stdout if not specified).", 
                      "language the info file is translated to."), GenerateFromTranslationDocument},
                { new Command("--validate", "Performs exhausting validation against store folder. Requires --file/-f."), Validate },
                { new Command("-v", "Performs exhausting validation against store folder. Requires --directory/-d."), Validate },
                { new Command("--gen-sign", "Generates missing signatures. Requires --directory/-d.", 
                      "PGP key ID, the key must be in keyring"), GenerateSignatures },
                { new Command("-s", "Generates missing signatures. Requires --directory/-d.", 
                      "PGP key ID, the key must be in keyring"), GenerateSignatures },
                { new Command("--re-sign", "Regenerate all signatures. Requires --directory/-d.", 
                      "PGP key ID, the key must be in keyring"), ReGenerateSiagnatures },
                { new Command("-r", "Regenerate all signatures. Requires --directory/-d.", 
                      "PGP key ID, the key must be in keyring"), ReGenerateSiagnatures},
            };

            if (args.Length < 1) return Help(null);

            int res;
            if ((res = ParseArgs(args, out Command command, out Executor action, out List<Command> userArgs)) == 0) {
                Options options = OptionsFactory.GetOptions();

                try
                {
                    action(command, userArgs);
                    return 0;
                }
                catch (Exception e)
                {
                    options.Save();
                    Console.WriteLine("The application exited with an error: " + e.StackTrace);
                    return 1;
                }
            }

            Console.WriteLine("Invalid command.");
            Help(null);
            return 1;
        }

        private static int ParseArgs(string[] args, 
            out Command userCmd, out Executor action, out List<Command> UserArgs)
        {
            userCmd = null;
            action = null;
            UserArgs = new List<Command>();

            for (int i = 0; i < args.Length; i++)
            {
                Command temp = Command.FromString(args[i]);
                if (temp == null)
                {
                    Console.WriteLine($"Invalid Command '{args[i]}'. Maybe predecessor was taken as an argument?");
                    return Help(null);
                }
                if (_commands.TryGetValue(temp, out Executor cmdAction))
                {
                    //kinda sad that dict has no direct way to access the key
                    temp = _commands.Keys.FirstOrDefault(x => x.Equals(temp));
                    if (userCmd != null)
                    {
                        Console.WriteLine("The main argument can be specified one only.");
                        return Help(null);
                    }
                    var error = ParseArg(temp);
                    if (error != null)
                    {
                        Console.WriteLine(error);
                        return 1;
                    }
                    userCmd = temp;
                    action = cmdAction;
                }
                else if (_auxiliary.TryGetValue(temp, out Func<string, string> validator))
                {
                    temp = _auxiliary.Keys.FirstOrDefault(x => x.Equals(temp));
                    var error = ParseArg(temp, validator);
                    if (error != null)
                    {
                        Console.WriteLine(error);
                        return 1;
                    }
                    UserArgs.Add(temp);
                }
                else
                {
                    return Help(null);
                }

                string ParseArg(Command cmd, Func<string, string> validator = null)
                {
                    if (cmd.Arg == null) return null;
                    i++;
                    if (i == args.Length)
                    {
                        return $"Option {cmd.Name} requires an argument: {cmd.Arg} ";
                    }
                    if (validator != null)
                    {
                        string error = validator(args[i]);
                        if (error != null)
                        {
                            return error;
                        }
                    }
                    cmd.ArgValue = args[i];
                    if (cmd.ArgValue.StartsWith("-")) Console.WriteLine($"Warning: {cmd.ArgValue} evaluated as argument.");
                    return null;
                }
            }
            return 0;
        }

        private static string CheckIsJson(string file)
        {
            if (!file.EndsWith(".json")) return "Invalid file format.";
            return null;
        }

        private static int GenerateToTranslationDocument(Command cmd, List<Command> aux = null)
        {
            GetTranlationFiles(out string file, out string outfile, aux);
            if (file == null || !File.Exists(file))
            {
                Console.WriteLine("Missing a source file (--file), or the specified file does not exist.");
                return 1;
            }
            try
            {
                MainFile parsed = MainFile.FromJson(file);
                if (outfile == null)
                {
                    JCAppstoreTranslationParser.ParseFile(parsed, x => Console.WriteLine(x));
                }
                else
                { 
                    outfile = outfile.EndsWith(".txt") ? outfile : $"{outfile}.txt";
                    using (var writer = new StreamWriter(outfile))
                    {
                        JCAppstoreTranslationParser.ParseFile(parsed, x => writer.WriteLine(x));
                    }
                    Console.WriteLine($"File {outfile} succesfully generated.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unable to parse the file: are you sure a correct file(s) is(are) provided?\r\n{e}");
                return 2;
            }
            return 0;
        }

        private static int GenerateFromTranslationDocument(Command cmd, List<Command> aux = null)
        {
            GetTranlationFiles(out string original, out string metafile, aux);
            if (original == null || !File.Exists(original))
            {
                Console.WriteLine("Missing a source file (--file), or the specified file does not exist.");
                return 1;
            }
            if (cmd.ArgValue == null || cmd.ArgValue.Length < 1)
            {
                Console.WriteLine("Command --to-file expects a language parameter.");
                return 1;
            }
            try
            {
                MainFile parsed = MainFile.FromJson(original);
                if (metafile == null)
                {
                    parsed = JCAppstoreTranslationParser.ParseFile(parsed, () => Console.ReadLine().Trim());
                }
                else
                {
                    metafile = metafile.EndsWith(".txt") ? metafile : $"{metafile}.txt";
                    if (!File.Exists(metafile))
                    {
                        Console.WriteLine($"Invalid metafile: {metafile} does not exist.");
                        return 2;
                    }
                    using (var reader = new StreamReader(metafile))
                    {
                        parsed = JCAppstoreTranslationParser.ParseFile(parsed, () => reader.ReadLine());
                    }
                }
                if (parsed == null) return 1;
                var outputName = $@"{Path.GetFullPath(Path.GetDirectoryName(original))}\info_{cmd.ArgValue}.json";
                parsed.ToJson(outputName);
                Console.WriteLine($"Metadata successfully parsed. Created {outputName}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unable to parse the file: are you sure a correct file(s) is(are) provided?\r\n{e}");
                return 2;
            }
            return 0;    
        }

        private static void GetTranlationFiles(out string file, out string meta, List<Command> aux)
        {
            file = null;
            meta = null;
            if (aux == null) return;
            foreach (var arg in aux)
            {
                if (arg.Name.Equals("-m") || arg.Name.Equals("--meta"))
                {
                    meta = arg.ArgValue;
                }
                else if (arg.Name.Equals("-f") || arg.Name.Equals("--file"))
                {
                    file = arg.ArgValue;
                }
            }
        }

        private static int Edit(Command cmd, List<Command> aux = null)
        {
            if (aux == null) return 0;
            foreach (var arg in aux)
            {
                if (arg.Name.Equals("-f") || arg.Name.Equals("--file"))
                {
                    if (!File.Exists(arg.ArgValue))
                    {
                        Console.WriteLine($"The provided file {arg.ArgValue} does not exist. Is the path correct?");
                        return 2;
                    }

                    FileEditor editor;
                    try
                    {
                        editor = new FileEditor(arg.ArgValue);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Failed to parse given info file: " + e.Message);
                        return 3;
                    }
                    
                    try
                    {
                        editor.Run();
                    } 
                    catch (Exception e)
                    {
                        Console.WriteLine("An exception encountered when editing. " + e.StackTrace);
                        Console.WriteLine($"Creating an auto save file {arg.ArgValue}.crashsave.json ...");
                        editor.File.ToJson($"{arg.ArgValue}.crashsave.json");
                        return 4;
                    }
                    return 0;
                }
            }
            Console.WriteLine("Missing compulsory --file auxiliary command");
            return 1;
        }

        private static int Validate(Command cmd, List<Command> aux = null)
        {
            if (aux == null) return 0;
            foreach (var arg in aux)
            {
                if (arg.Name.Equals("-f") || arg.Name.Equals("--file"))
                {
                    new FileEditor(arg.ArgValue).RunCommand("check");
                    return 0;
                }
            }
            Console.WriteLine("Invalid action: missing --file.");
            return 2;
        }

        private static int CheckSignature(Command cmd, List<Command> aux, out string rootDir, out string keyId)
        {
            rootDir = null;
            keyId = null;

            if (aux == null)
            {
                Console.WriteLine("Invalid command use: expected --directory parameter.");
                return 1;
            }

            foreach (var arg in aux)
            {
                if (arg.Name.Equals("-d") || arg.Name.Equals("--directory"))
                {
                    rootDir = arg.ArgValue;
                    keyId = cmd.ArgValue;
                    if (cmd.ArgValue == null || cmd.ArgValue.IsEmpty())
                    {
                        Console.WriteLine("Invalid argument for signing command: expected key id to be used for signatures.");
                        return 2;
                    }

                    var gpg = OptionsFactory.GetOptions().Get(Options.Values.GNUPG);
                    Console.WriteLine("Veryfying GPG and keys...");
                    if (Cmd.RunAndWait(gpg, "--version").ExitCode != 0)
                    {
                        Console.WriteLine($"Failed to run GnuPG: '{gpg}': is this valid command? " +
                            "Setup the executable command for GPG in options file.");
                        return 4;
                    }
                    if (!Cmd.RunAndWait(gpg, "--list-secret-keys").StandardOutput.ReadToEnd().Contains(keyId))
                    {
                        Console.WriteLine($"Failed to identify key ID '{keyId}': " +
                            "is this GPG key private part present in your keyring?");
                        return 5;
                    }
                    return 0;
                }
            }
            Console.WriteLine("Invalid command use: expected --directory parameter.");
            return 1;
        }

        private static void ForeachApplet(string rootdir, Action<string> worker)
        {
            foreach (var appletDir in Directory.GetDirectories($@"{rootdir}\JCApplets\", "*", SearchOption.TopDirectoryOnly))
            {
                foreach (var applet in Directory.GetFiles(appletDir, "*.cap", SearchOption.TopDirectoryOnly))
                {
                    worker(Path.GetFullPath(applet));
                }
            }
        }


        private static int GenerateSignatures(Command cmd, List<Command> aux = null)
        {
            var returnCode = CheckSignature(cmd, aux, out string rootDir, out string keyId);
            if (returnCode != 0) return returnCode;
            var gpg = OptionsFactory.GetOptions().Get(Options.Values.GNUPG);
            Console.WriteLine("Generating signatures...");
            var counter = 0;
            ForeachApplet(rootDir, appletFile =>
            {
                var signature = $"{appletFile}.sig";
                if (!File.Exists(signature))
                {
                    var process = Cmd.RunAndWait(gpg, "--default-key", $"\"{keyId}\"", "--output", $"\"{signature}\"",
                        "--detach-sig", $"\"{appletFile}\"");
                    if (process.ExitCode != 0)
                    {
                        Console.WriteLine(process.StandardError.ReadToEnd());
                    }
                    else counter++;
                }
            });
            Console.WriteLine($"Generated {counter} signatures.");
            return 0;
        }

        private static int ReGenerateSiagnatures(Command cmd, List<Command> aux = null)
        {
            var returnCode = CheckSignature(cmd, aux, out string rootDir, out string keyId);
            if (returnCode != 0) return returnCode;
            var gpg = OptionsFactory.GetOptions().Get(Options.Values.GNUPG);
            Console.WriteLine("Regenerating signatures...");
            var counter = 0;
            ForeachApplet(rootDir, appletFile =>
            {
                var signature = $"{appletFile}.sig";
                if (File.Exists(signature))
                {
                    File.Delete(signature);
                }
                var process = Cmd.RunAndWait(gpg, "--default-key", $"\"{keyId}\"", "--output", $"\"{signature}\"", 
                    "--detach-sig", $"\"{appletFile}\"");
                if (process.ExitCode != 0)
                {
                    Console.WriteLine(process.StandardError.ReadToEnd());
                }
                else counter++;
            });
            Console.WriteLine($"Generated {counter} signatures.");
            return 0;
        }

        private static int Help(Command _, List<Command> aux = null)
        {
            Console.WriteLine("Specify a command to run. Multiple main commands result in error.");
            Console.WriteLine();
            PrintDict(_commands);
            Console.WriteLine();
            Console.WriteLine();

            Console.WriteLine("Auxiliary commands that can be required by a command.");
            PrintDict(_auxiliary);
            return 1;

            void PrintDict<V>(Dictionary<Command,V> dict)
            {
                string kept = null;
                var i = 0;
                foreach (var command in dict)
                {
                    if (i % 2 == 0)
                    {
                        kept = command.Key.Name;
                    } 
                    else
                    {
                        Console.Write($"     {command.Key.Name,-5}");
                        Console.WriteLine(kept);
                        Console.WriteLine(command.Key.GetDescription());
                    }
                    i++;
                }
            }
        }
    }
}
