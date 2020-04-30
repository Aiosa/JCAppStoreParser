using System;

using JCAppStore_Parser.JsonInfoFile;

namespace JCAppStore_Parser
{
    /// <summary>
    /// Editing category (not much of a work, it has only name -- for now).
    /// </summary>
    public class CategoryEditor
    {
        private Category _category;
        private MainFile _source;

        public CategoryEditor(MainFile source, Category category)
        {
            _category = category;
            _source = source;
        }

        public CategoryEditor(MainFile source) : this(source, Category.Empty(source))
        {
            source.Add(_category);
        }

        /// <summary>
        /// Run interactive editor.
        /// </summary>
        public void Run()
        {
            EditorTools.PrintHeader($"Edit category: {_category}");
            PrintCommands();
            while (ParseCommand(Console.ReadLine()))
            {
            }
            Console.Clear();
        }

        /// <summary>
        /// Run chain editor: automatically asks for all the fields (adding a new item).
        /// </summary>
        public void Chain()
        {
            ParseCommand("edit");
            Console.Clear();
            Console.WriteLine("Entering interactive mode...");
            Console.WriteLine();
            Run();
        }

        private void PrintCommands()
        {
            Console.WriteLine($"  edit\t Edit title.");
            Console.WriteLine($"  exit\t Exit.");
        }

        private bool ParseCommand(string cmd)
        {
            switch (cmd.Trim().ToLower())
            {
                case "edit":
                    var result = EditorTools.EditString("Title:", _category.Title);
                    if (result != null && !_source.Contains(new Category { Title = result })) _category.Title = result;
                    else Console.WriteLine("Invalid name: null or already exists. Category name must be unique.");
                    Console.Clear();
                    PrintCommands();
                    return result != null;
                case "exit":
                    Console.Clear();
                    return false;
                default:
                    Console.WriteLine("Unknown command.");
                    return true;
            }

        }
    }
}
