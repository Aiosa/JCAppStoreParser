using System;
using System.Collections.Generic;

namespace JCAppStore_Parser
{
    /// <summary>
    /// Useful methods in cmd editors.
    /// </summary>
    public static class EditorTools
    {
        public static void PrintHeader(string name)
        {
            Console.Clear();
            Console.WriteLine("===================================");
            Console.WriteLine(name);
            Console.WriteLine("===================================");
        }

        public static bool AskIfSure(string msg)
        {
            Console.Write($"{msg} (y/n): ");
            if (Console.ReadLine().Trim().Equals("y"))
            {
                return true;
            }
            return false;
        }

        public static bool? EditBool(string key, string value)
        {
            PrintCurrentValue(key, value);
            Console.Write(" yes/no: ");
            var result = Console.ReadLine().Trim().ToLower();
            if (result.Equals("yes")) return true;
            if (result.Equals("no")) return false;
            return null;
        }

        public static string EditString(string key, string value, string message)
        {
            Console.WriteLine(message);
            return EditString(key, value);
        }

        public static string EditString(string key, string value)
        {
            PrintCurrentValue(key, value);
            var result = Console.ReadLine();
            if (result.Length == 0) return null;
            return result;
        }

        public static void PrintOptions(IEnumerable<object> from)
        {
            var i = 1;
            foreach (var value in from)
            {
                Console.WriteLine($"  {i++}.\t {value.ToString()}");
            }
        }

        public static void PrintCurrentValue(string key, string value)
        {
            Console.WriteLine($"{key}: {value}");
            Console.Write("New: ");
        }
    }
}
