using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace JCAppStore_Parser.Utils
{
    public class Lexem
    {
        private string searchKey;
        public string SearchKey { get => searchKey; set { if (locked) return; searchKey = value; } }
        
        private bool locked;

        public bool Found { get; set; }

        public Lexem(string SearchKey, bool locked = true)
        {
            this.SearchKey = SearchKey;
            this.locked = locked;
        }

        public override int GetHashCode()
        {
            return SearchKey.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is Lexem l)
            {
                return SearchKey.Equals(l.SearchKey);
            }
            return false;
        }
    }

    public class Lexers : HashSet<Lexem>
    {
        private Lexem searchObject = new Lexem("", false);
        private string filename;

        public Lexers(string filename)
        {
            this.filename = filename;
            ForeachLineWithLexem((category, line) =>
            {
                var items = line.Split(' ');

                for (int i = 0; i < items.Length; i++)
                {
                    var curr = items[i].Trim();
                    if (curr.Length < 3) continue;
                    Add(new Lexem(curr));
                }
            });
        }

        public new Lexem Add(Lexem l)
        {
            if (TryGetValue(l, out Lexem value)) return value;
            base.Add(l);
            return l;
        }

        public bool Contains(string value)
        {
            searchObject.SearchKey = value;
            return Contains(searchObject);
        }

        public bool TryGetValue(string searchKey, out Lexem value)
        {
            searchObject.SearchKey = searchKey;
            return TryGetValue(searchObject, out value);
        }

        public void ToDependencyFile(string filename)
        {
            if (File.Exists(filename)) File.Delete(filename);
           
            using (var writer = new StreamWriter(File.Create(filename)))
            {
                string ctg = null;
                ForeachLineWithLexem((category, line) =>
                {
                    bool contains = true;
                    var items = line.Split(' ');
                    for (int i = 0; i < items.Length; i++)
                    {
                        var curr = items[i].Trim();
                        if (curr.Length < 3) continue;
                        contains = contains && TryGetValue(curr, out Lexem found) && found.Found;
                    }

                    if (contains) {
                        if (ctg != category)
                        {
                            writer.WriteLine();
                            writer.WriteLine(category);
                            ctg = category;
                        }
                        writer.WriteLine(line);
                    }
                });
            }
        }

        public void ResetTokens()
        {
            this.ToList().ForEach(l => l.Found = false);
        }

        private void ForeachLineWithLexem(Action<string, string> worker)
        {
            using (StreamReader reader = File.OpenText(filename))
            {
                string category = null;
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.Length < 1)
                    {
                        category = null;
                        continue;
                    }

                    if (category == null) category = line;
                    else
                    {
                        worker(category, line);
                    }
                }
            }
        }
    }
}
