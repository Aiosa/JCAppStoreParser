using System.Collections.Generic;

using Newtonsoft.Json.Linq;

namespace JCAppStore_Parser.JsonInfoFile
{
    public class StoreItemDictionaryConverter<V> 
    {
        public delegate V ToValue(JToken from);

        public static Dictionary<string, V> FromJsonObject(JObject o, string key, ToValue converter)
        {
            return FromJsonObject(o[key], converter);
        }

        public static Dictionary<string, V> FromJsonObject(JToken token, ToValue converter)
        {
            var result = new Dictionary<string, V>();
            foreach (var item in (JObject)token)
            {
                result.Add(item.Key, converter(item.Value));
            }
            return result;
        }
    }
}
