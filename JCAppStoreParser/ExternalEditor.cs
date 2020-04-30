using JCAppStore_Parser.Utils;

namespace JCAppStore_Parser
{
    /// <summary>
    /// Calls external GUI editor
    /// </summary>
    public class ExternalEditor
    {
        public void Edit(string filename)
        {
            var opts = OptionsFactory.GetOptions();
            Cmd.RunAndWait($"{opts.Get(Options.Values.KEY_EDITOR)} {opts.Get(Options.Values.KEY_EDITOR_FILEARG)} {filename}");
        }
    }
}
