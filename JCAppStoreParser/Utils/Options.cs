namespace JCAppStore_Parser.Utils
{
    public abstract class Options
    {
        protected Options() { }

        public enum Values
        {
            KEY_EDITOR,
            KEY_EDITOR_FILEARG,
            GNUPG
        }

        //Editor to be used
        string Editor { get; }
        //Argument for editor to specify the file to edit
        string EditorFileArg { get; }

        /// <summary>
        /// Get value from options
        /// </summary>
        /// <param name="key">value key</param>
        public abstract string Get(Values key);

        /// <summary>
        /// Get bool from options. Throws exception if the key does not conform to boolaen.
        /// </summary>
        /// <param name="key">bool key</param>
        /// <returns>false if options[key] = false</returns>
        public abstract bool GetBool(Values key);

        /// <summary>
        /// Set kvalue
        /// </summary>
        /// <param name="key">key of the value to modify</param>
        /// <param name="value">value to set</param>
        public abstract void Set(Values key, string value);

        /// <summary>
        /// Set bool. Throws exception if the key does not conform to boolaen.
        /// </summary>
        /// <param name="key">key of the value to modify</param>
        public abstract void SetBool(Values key, bool value);

        /// <summary>
        /// Save the options into a file.
        /// </summary>
        public abstract void Save();
    }
}
