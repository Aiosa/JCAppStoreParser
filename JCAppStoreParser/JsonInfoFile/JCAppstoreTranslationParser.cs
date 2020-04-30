using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JCAppStore_Parser.JsonInfoFile
{
    class JCAppstoreTranslationParser
    {
        /// <summary>
        /// Generate text file for translation
        /// </summary>
        /// <param name="source">parset json file to translate</param>
        /// <param name="output">callback, receives generated file line by line</param>
        public static void ParseFile(MainFile source, Action<string> output)
        {
            foreach (var category in source)
            {
                output(AddTemplate("Title for category", category.Title));
                foreach (var item in category)
                {
                    output(AddTemplate("Title", item.Title, true));
                    if (item.AppletNames != null && item.AppletNames.Count > 0)
                    {
                        var hasAID = item.AppletNames[0].Equals("0x");
                        output(AddTemplate("Applets",
                            item.AppletNames.Skip(hasAID ? 1 : 0).Aggregate(new StringBuilder(), (builder, s) => builder.Append(s).Append("\r\n")).ToString(),
                            true));
                    }
                    output(AddTemplate("Url addresses (keep the ';' sign)",
                        item.Urls.Aggregate(new StringBuilder(), (builder, s) => builder.Append(s.Key).Append(" ; ").Append(s.Value).Append("\r\n")).ToString(),
                        true));
                    output(AddTemplate("Description", item.Description));
                    output(AddTemplate("Usage", item.Usage));
                }
            }
        }

        private static string AddTemplate(string fieldName, string value, bool copy = false) 
            => $">>>>> {fieldName}\r\n{value}\r\n{AddTranslation(fieldName, value, copy)}";
        private static string AddTranslation(string fieldName, string value, bool copy) 
            => $"<<<<< {fieldName} (translation)\r\n{(copy ? value : "TODO")}";

        /// <summary>
        /// Generate JSON from translation file.
        /// </summary>
        /// <param name="source">source, the original file being translated. Translation is written into it 
        /// (the original language is lost, don't save it to the same file the object was parsed from). </param>
        /// <param name="input">input data, calling it will return a line from translation.</param>
        /// <returns></returns>
        public static MainFile ParseFile(MainFile source, Func<string> input)
        {
            var firstLine = GetNonEmptyLine(input);
            if (!firstLine.StartsWith("<<<<< Title for category"))
            {
                Console.WriteLine($"Failed to parse the translation: expected '<<<<< Title for category' at the file beginning, found '{firstLine}'.");
                return null;
            }
            foreach (var category in source)
            {
                var builder = new StringBuilder();
                if (!AddItemField(input, "<<<<< Title", title => builder.Append(" ").Append(title), line => builder.Length < 150 && line != null,
                        line => $"Error: Title should not exceed 150 characters. Current line: '{line}'.")) return null;
                category.Title = builder.ToString().Trim();

                foreach (var item in category)
                {
                    builder.Clear();
                    var hasApplets = item.AppletNames != null && item.AppletNames.Count > 0;
                    if (!AddItemField(input, hasApplets ? "<<<<< Applets" : "<<<<< Url addresses",
                        title => builder.Append(" ").Append(title), line => builder.Length < 150 && line != null,
                        line => $"Error: Title should not exceed 150 characters. Current line: '{line}'.")) return null;
                    item.Title = builder.ToString().Trim();

                    if (hasApplets)
                    {
                        var hasAID = item.AppletNames[0].Equals("0x");
                        var applets = new List<string>();
                        if (hasAID) applets.Add("0x");
                        if (!AddItemField(input, "<<<<< Url addresses", data => applets.Add(data), line => applets.Count < 20 && line != null,
                        line => $"Error: Header mismatch: applets for {item.Title} parsing stucked.  Current line: '{line}'.")) return null;
                        if (applets.Count != item.AppletNames.Count)
                        {
                            Console.WriteLine($"Applet {item.Title} has { (hasAID ? item.AppletNames.Count / 2 : item.AppletNames.Count) } " +
                                $"applets defined, the translation has { (hasAID ? applets.Count / 2 : applets.Count) } applet names.");
                            return null;
                        }
                        item.AppletNames = applets;
                    }

                    var urls = new Dictionary<string, string>();
                    if (!AddItemField(input, "<<<<< Description", urlLine =>
                    {
                        var urlData = urlLine.Split(';').Select(x => x.Trim()).ToArray();
                        if (urlData.Length != 2) Console.WriteLine($"Invalid url: expected [name ; value], got '{urlLine}'. Skipping...");
                        else if (urls.ContainsKey(urlData[0])) Console.WriteLine($"Invalid url name: contains twice {urlData[0]}. Skipping...");
                        else urls.Add(urlData[0], urlData[1]);
                    }, line => urls.Count < 30 && line != null,
                    line => $"Error: Header mismatch: url fields for {item.Title} parsing stucked. Current line: '{line}'.")) return null;
                    item.Urls = urls;

                    builder.Clear();
                    if (!AddItemField(input, "<<<<< Usage", desc => builder.Append(" ").Append(desc), line => line != null,
                        line => "Error: Description stuck. Found end of file when parsing, expected Usage.")) return null;
                    item.Description = builder.ToString().Trim();

                    builder.Clear();
                    AddItemField(input, "<<<<< Title", desc => builder.Append(" ").Append(desc), line => line != null, line => null);
                    item.Usage = builder.ToString().Trim();
                }
            }
            return source;
        }

        private static bool CheckPrefix(string expectedPrefix, string data)
        {
            if (data == null) return false;
            return data.StartsWith(expectedPrefix);
        }

        private static bool AddItemField(Func<string> input, string expectedNextPrefix, Action<string> assigner, 
            Func<string, bool> invariant, Func<string, string> errorMessage)
        {
            var line = GetNonEmptyLine(input);
            while (!CheckPrefix(expectedNextPrefix, line))
            {
                if (!invariant(line))
                {
                    Console.WriteLine(errorMessage(line));
                    return false;
                }
                assigner(line);
                line = GetNonEmptyLine(input);
            }
            return true;
        }

        private static string GetNonEmptyLine(Func<string> input)
        {
            string data;
            while ((data = input()) != null)
            {
                if (data.Length > 0)
                {
                    if (data.StartsWith(">>>>>"))
                    {
                        do
                        {
                            data = input();
                            if (data != null) data.Trim();
                        } while (data != null && !data.StartsWith("<<<<<"));
                    }
                    return data;
                }
            }
            return null;
        }
    }
}
