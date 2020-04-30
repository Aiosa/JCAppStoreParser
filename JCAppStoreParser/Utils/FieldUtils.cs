using System.Collections.Generic;
using System.Text;

namespace JCAppStore_Parser
{
    public static class FieldUtils
    {
        public static string GetValues<T>(IEnumerator<T> of, string emptyMessage = null, int? max = null)
        {
            if (of == null || !of.MoveNext()) return emptyMessage;
            StringBuilder builder = new StringBuilder($"[{of.Current.ToString()}");
 
            while (of.MoveNext() && (!max.HasValue || max > 0))
            {
                builder.Append(", ").Append(of.Current.ToString());
                if (max.HasValue) max--;
            }
            if (of.MoveNext()) builder.Append(", ...");
            return builder.Append(']').ToString();
        }

        public static string GetValues<T>(ICollection<T> of, string emptyMessage = null, int? max = null)
        {
            if (of == null || of.Count == 0) return emptyMessage;
            StringBuilder builder = new StringBuilder($"[");
            int num = 0;
            string prefix = "";
            foreach(var value in of)
            {
                if (max.HasValue)
                {
                    if (num >= max) break;
                    num++;
                }
                builder.Append(prefix).Append(value);
                prefix = ", ";
            }
            if (max.HasValue && num < of.Count) builder.Append(", ...");
            return builder.Append(']').ToString();
        }

        public static string GetAllValues<T>(IEnumerator<T> of, string emptyMessage = null)
        {
            if (of == null || !of.MoveNext()) return emptyMessage;
            StringBuilder builder = new StringBuilder($"{of.Current.ToString()}");
            while (of.MoveNext())
            {
                builder.Append(", ").Append(of.Current.ToString());
            }
            return builder.ToString();
        }

        public static string GetAllValues<T>(ICollection<T> of, string emptyMessage = null)
        {
            if (of == null || of.Count == 0) return emptyMessage;
            StringBuilder builder = new StringBuilder();
            string prefix = "";
            foreach (var value in of)
            {
                builder.Append(prefix).Append(value);
                prefix = ", ";
            }
            return builder.ToString();
        }
    }
}
