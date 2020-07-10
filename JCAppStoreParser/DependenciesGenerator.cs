using JCAppStore_Parser.JsonInfoFile;
using JCAppStore_Parser.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JCAppStore_Parser
{
    public class DependenciesGenerator
    {
        public static bool CreateFile(string upToVersion, Lexers lexers, string itemLocation)
        {
            var latestDir = OptionsFactory.GetOptions().Get(Options.Values.KEY_LAST_DEPENDENCY_SRC_DIR);
            var dir = FileBrowser.Open(latestDir == null ? Directory.GetCurrentDirectory() : latestDir);
            if (dir == null) return false;
            OptionsFactory.GetOptions().Set(Options.Values.KEY_LAST_DEPENDENCY_SRC_DIR, dir);
            Console.WriteLine("Walking the directory tree...");
            WalkDirectoryTree(Directory.CreateDirectory(dir), file =>
            {
                var contents = File.ReadAllText(file.FullName);
                MatchCollection matches = Regex.Matches(contents, "([a-z0-9_-][a-z0-9_-]*)", RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    if (lexers.TryGetValue(match.Value, out Lexem token))
                    {
                        token.Found = true;
                    }
                }
            });
            var output = Path.Combine(itemLocation, $"lexems_{upToVersion}.txt");
            lexers.ToDependencyFile(output);
            Console.WriteLine($"Inspection done. Dependencies written in {output}.");
            lexers.ResetTokens();
            return true;
        }

        /// <summary>
        /// FROM https://docs.microsoft.com/cs-cz/dotnet/csharp/programming-guide/file-system/how-to-iterate-through-a-directory-tree
        /// </summary>
        private static void WalkDirectoryTree(DirectoryInfo root, Action<FileInfo> worker)
        {
            FileInfo[] files = null;
            try
            {
                files = root.GetFiles("*.java");
            }
            catch (UnauthorizedAccessException e)
            {
                throw new Exception($"Cannot access given folder: {root}. Check the access rights.");
            }
            catch (DirectoryNotFoundException e)
            {
                Console.Error.WriteLine(e.Message);
            }

            if (files != null)
            {
                foreach (FileInfo fi in files)
                {
                    worker(fi);
                    Console.WriteLine($"  {fi.FullName}");
                }
                foreach (DirectoryInfo dirInfo in root.GetDirectories())
                {
                    WalkDirectoryTree(dirInfo, worker);
                }
            }
        }
    }
}

