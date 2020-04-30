using System.Collections.Generic;
using System.IO;
using System.IO.Compression;


namespace JCAppStore_Parser
{
    /// <summary>
    /// Parsing a javacard binary (.cap file).
    /// 
    /// Stripped verions of what below implementation does, reads a java package and gets applet definitions
    /// -> our software needs to verify that applet JSON info file defines existing applet instance names = the number of instances must be the same.
    /// 
    /// source (template) implementation:
    /// https://github.com/martinpaljak/capfile/tree/7b93239f574270d0d556c420de5b003fa8d78cf8
    /// file CAPFile.java
    /// </summary>
    public static class CAPFileParser
    {
        public static List<AID> Parse(string file)
        {
            var result = new List<AID>();
            using (ZipArchive archive = ZipFile.OpenRead(file))
            {
                var data = Read(archive);
                var applet = getAppletComponent();
                if (applet != null)
                {
                    int offset = 4;
                    for (int j = 0; j < (applet[3] & 0xFF); j++)
                    {
                        var len = applet[offset++];
                        var appaid = new AID(applet, offset, len);
                        if (!result.Contains(appaid)) result.Add(appaid);
                        offset += len + 2;
                    }
                }

                byte[] getAppletComponent()
                {
                    foreach(var tuple in data)
                    {
                        if (tuple.Key.EndsWith("Applet.cap"))
                        {
                            return tuple.Value;
                        }
                    }
                    return null;
                }
            }
            return result;
        }

        private static Dictionary<string, byte[]> Read(ZipArchive archive)
        {
            var result = new Dictionary<string, byte[]>();
            foreach (var entry in archive.Entries)
            {
                using (Stream input = entry.Open())
                {
                    result.Add(entry.Name, ToByteArray(input));
                }
            }

            return result;

            byte[] ToByteArray(Stream stream)
            {
                using(MemoryStream memory = new MemoryStream())
                {
                    stream.CopyTo(memory);
                    return memory.ToArray();
                }
            }
        }
    }
}
