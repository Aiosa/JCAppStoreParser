using System.Linq;
using System.Text;
using System.Diagnostics;

namespace JCAppStore_Parser.Utils
{
    /// <summary>
    /// Run a Windows CMD command
    /// </summary>
    public class Cmd
    {
        public static Process Run(params string[] args)
        {
            return Run(args.Aggregate(new StringBuilder(),
                (builder, chunk) => builder.Append(chunk).Append(' ')).ToString());
        }

        public static Process RunAndWait(params string[] args)
        {
            var p = Run(args);
            p.WaitForExit();
            return p;
        }

        private static Process Run(string cmdline)
        {
            var p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.Arguments = $"/c {cmdline}";
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            return p;
        }
    }
}
