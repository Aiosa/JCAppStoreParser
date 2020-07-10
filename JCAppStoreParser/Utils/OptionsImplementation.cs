using System;
using System.Collections.Generic;
using System.IO;

using IniParser;
using IniParser.Model;

namespace JCAppStore_Parser.Utils
{
    public class OptionsImplementation : Options
    {
        private const string _optsFile = "JCParser.options";
        private const string _optsSection = "JCParser";
        private Dictionary<int, string> _opts = new Dictionary<int, string>();

        public OptionsImplementation()
        {
            Load();
        }

        public string Editor { get; private set; } = "";

        public string EditorFileArg { get; private set; } = "";

        public override string Get(Values key)
        {
            if (_opts.TryGetValue((int)key, out string value))
            {
                return value;
            }
            return null;
        }

        public override bool GetBool(Values key)
        {
            if (!key.IsBool()) throw new Exception($"Invalid option key {key}: not a boolean.");
            if (_opts.TryGetValue((int)key, out string value))
            {
                return value.Trim().ToLower().Equals("true");
            }
            return false;
        }

        public override void Set(Values key, string value)
        {
            if (_opts.ContainsKey((int)key))
            {
                _opts[(int)key] = value;
                return;
            }
            _opts.Add((int)key, value);
        }

        public override void SetBool(Values key, bool value)
        {
            if (!key.IsBool()) throw new Exception($"Invalid option key {key}: not a boolean.");
            Set(key, value ? "true" : "false");
        }

        private void Load()
        {
            if (!File.Exists(_optsFile))
            {
                File.Create(_optsFile);
            }

            var parser = new FileIniDataParser();
            var data = parser.ReadFile(_optsFile);

            if (data[_optsSection] == null) data.Sections.AddSection(_optsSection);
            var section = data[_optsSection];
            foreach (var key in Enum.GetValues(typeof(Values)))
            {
                string value = section[((Values)key).Key()];
                if (ValidString(value)) _opts.Add((int)key, value);
            }
            FillDefaults();

            bool ValidString(string input) => input != null && input.Length > 0;
        }

        public override void Save()
        {
            var data = new IniData();
            data.Sections.AddSection(_optsSection);
            var section = data[_optsSection];
            foreach (var dataset in _opts)
            {
                section.AddKey(((Values)dataset.Key).Key(), dataset.Value);
            }
            new FileIniDataParser().WriteFile(_optsFile, data);
        }

        private void FillDefaults()
        {
            AddNotOverwrite(Values.KEY_EDITOR, "notepad");
            AddNotOverwrite(Values.KEY_EDITOR_FILEARG, "");
            AddNotOverwrite(Values.GNUPG, "gpg");
            AddNotOverwrite(Values.KEY_LEXEM_FILE, "lexems.txt");
            AddNotOverwrite(Values.KEY_LAST_DEPENDENCY_SRC_DIR, null);

            void AddNotOverwrite(Values key, string value)
            {
                if (!_opts.ContainsKey((int)key))
                {
                    _opts.Add((int)key, value);
                }
            }
        }
    }
}
