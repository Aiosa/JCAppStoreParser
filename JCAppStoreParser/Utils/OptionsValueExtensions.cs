namespace JCAppStore_Parser.Utils
{
    public static class OptionsValueExtensions
    {
        /// <summary>
        /// Enum extensions
        /// </summary>
        public static string Key(this Options.Values value)
        {
            switch (value)
            {
                case Options.Values.KEY_EDITOR:
                    return "editor";
                case Options.Values.KEY_EDITOR_FILEARG:
                    return "editor_file_arg";
                case Options.Values.GNUPG:
                    return "gnupg_executable";

            }
            return null;
        }

        public static bool IsBool(this Options.Values value)
        {
            //switch (value)
            //{
            //    define all fields that conforms to bool in this way:
            //    case Options.Values.BOOL1:
            //    case Options.Values.BOOL2:
            //        return true;
            //}
            return false;
        }
    }
}
