namespace JCAppStore_Parser.Utils
{
    class OptionsFactory
    {
        private static Options _options;

        public static Options GetOptions()
        {
            if (_options == null) _options = new OptionsImplementation();
            return _options;
        }
    }
}
