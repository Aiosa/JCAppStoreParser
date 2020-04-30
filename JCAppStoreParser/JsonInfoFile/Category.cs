using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace JCAppStore_Parser.JsonInfoFile
{
    public class Category : List<StoreItem>, IContentPrintable, IComparable<Category>
    {
        public const string Type = "category";
        private int _index;

        public string Title { get; set; }

        public static Category Empty(MainFile of)
        {
            var lastCategory = of[of.Count - 1];

            return new Category
            {
                Title = $"NewCategory{of.Count + 1}",
                _index = lastCategory._index + 1
            };
        }

        public static Category FromJsonObject(JObject o, int at)
        {
            return new Category
            {
                Title = (string)o[JcappstoreParser.TAG_TITLE],
                _index = at
            };
        }

        public JObject ToJsonObject()
        {
            var o = new JObject();
            o[JcappstoreParser.TAG_TYPE] = "category";
            o[JcappstoreParser.TAG_TITLE] = Title;
            return o;
        }

        public int CompareTo(Category other)
        {
            if (Title == null) return other.Title.CompareTo(Title);
            return Title.CompareTo(other.Title);
        }

        public override bool Equals(object y)
        {
            if (y == null || !(y is Category))
            {
                return false;
            }
            return Title.Equals(((Category)y).Title);
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

        public override int GetHashCode()
        {
            return Title.GetHashCode();
        }

        public string GetValues()
        {
            return ToString();
        }

        public override string ToString()
        {
            return $"{Title} category with {Count} items.";
        }
    }
}
