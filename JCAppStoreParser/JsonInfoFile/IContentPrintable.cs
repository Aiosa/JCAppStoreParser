
namespace JCAppStore_Parser.JsonInfoFile
{
    public interface IContentPrintable
    {
        /// <summary>
        /// Returns description on inner fields / items. These should be in numbered order, starting from 1.
        /// </summary>
        string GetContents();

        /// <summary>
        /// Returns field names along with values.
        /// </summary>
        string GetValues();
    }
}
