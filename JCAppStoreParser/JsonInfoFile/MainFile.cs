using System.Collections.Generic;
using System.Text;

namespace JCAppStore_Parser.JsonInfoFile
{
    public class MainFile : List<Category>, IContentPrintable
    {
        public string FileName;
        public static MainFile FromJson(string file)
        {
            var result = new JcappstoreParser(file).Deserialize();
            result.FileName = file;
            return result;
        }

        public void ToJson(string file)
        {
            new JcappstoreParser(file).Serialize(this);
        }

        public void ToJson()
        {
            new JcappstoreParser(FileName).Serialize(this);
        }

        public string GetContents()
        {
            var builder = new StringBuilder();
            var i = 1;
            foreach (var c in this)
            {
                builder.Append(i++).Append(". ").Append(c).Append("\r\n");
            }
            return builder.ToString();
        }

        public string GetValues()
        {
            return $"{ToString()} with {Count} categories.";
        }

        public override string ToString()
        {
            return $"File {FileName}";
        }
    }
}
