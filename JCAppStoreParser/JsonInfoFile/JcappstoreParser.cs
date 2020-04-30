using System;
using System.Linq;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JCAppStore_Parser.JsonInfoFile
{
    public class JcappstoreParser
    {
        //CONSTANTS THAT CONFORM TO JSON NAMES
        public const string TAG_TYPE = "type";
        public const string TAG_NAME = "name";
        public const string TAG_TITLE = "title";
        public const string TAG_ICON = "icon";
        public const string TAG_LATEST = "latest";
        public const string TAG_VERSION = "versions";
        public const string TAG_BUILD = "builds";
        public const string TAG_AUTHOR = "author";
        public const string TAG_DESC = "description";
        public const string TAG_URL = "url";
        public const string TAG_USAGE = "usage";
        public const string TAG_KEYS = "keys";
        public const string TAG_DEFAULT_SELECTED = "default_selected";
        public const string TAG_PGP_IDENTIFIER = "pgp";
        public const string TAG_PGP_SIGNER = "signed_by";
        public const string TAG_APPLET_INSTANCE_NAMES = "applet_instance_names";

        private string filename;

        public JcappstoreParser(string fromFile)
        {
            filename = fromFile;
        }

        public MainFile Deserialize()
        {
            var result = new MainFile();
            Category current = null;
            var idx = 0;

            using (StreamReader reader = File.OpenText(filename))
            {
                var array = (JArray)JToken.ReadFrom(new JsonTextReader(reader));
                array.ToList().ForEach(a => Parse((JObject)a));
            }

            void Parse(JObject obj)
            {
                if (((string)obj[TAG_TYPE]).Equals(Category.Type))
                {
                    current = ParseCategory(obj);
                } 
                else
                {
                    if (current == null) throw new Exception("Invalid JSON store info file: missing category as the first item. Applet can't be outside a category.");
                    ParseItem(obj, current);
                }
                idx++;
            }

            Category ParseCategory(JObject category)
            {
                var temp = Category.FromJsonObject(category, idx);
                result.Add(temp);
                return temp;
            }

            void ParseItem(JObject item, Category of)
            {
                of.Add(StoreItem.FromJsonObject(item, idx));
            }

            return result;
        }

        public void Serialize(MainFile file)
        {
            var root = new JArray();
            foreach (var category in file)
            {
                root.Add(category.ToJsonObject());
                foreach (var item in category)
                {
                    root.Add(item.ToJsonObject());
                }
            }
            using (StreamWriter output = File.CreateText(filename))
            using (JsonTextWriter writer = new JsonTextWriter(output))
            {
                root.WriteTo(writer);
            }
        }
    }
}
