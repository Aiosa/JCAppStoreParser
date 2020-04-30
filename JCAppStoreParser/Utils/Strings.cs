
namespace JCAppStore_Parser.JsonInfoFile
{
    public static class Strings
    {
        public static bool IsEmpty(this string value)
        {
            return value.Length < 1;
        }

        public static string Cut(this string value, int length)
        {
            if (value.Length < length) return value;
            return $"{value.Substring(0, length)}...";
        }
    }
}
