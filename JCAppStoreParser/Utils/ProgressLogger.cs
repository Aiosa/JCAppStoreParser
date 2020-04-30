using System;

namespace JCAppStore_Parser
{
    class ProgressLogger : ILogger
    {
        private const int LENGTH = 60;
        public float Progress { get => (float)_progress / _max; }

        private int _progress;
        private int _max;
        public ProgressLogger(int max)
        {
            _progress = 0;
            _max = max;
        }

        public void Log(string message)
        {
            Console.CursorLeft = 0;
            Console.Write(new string(' ', Console.WindowWidth));
            Console.CursorTop--;
            Console.CursorLeft = 0;
            Console.WriteLine(message);
            WriteProgress();
        }

        private void WriteProgress()
        {
            int completed = (int)(LENGTH * Progress);
            Console.Write($"[{new string('=', completed)}{new string(' ', LENGTH - completed)}] {(int)(Progress * 100)}%");
        }

        public void Step()
        {
            _progress++;
        }

        public void Dispose()
        {
            //forces to print out 100% message bar.
            Log("Done.");
            Console.WriteLine();
        }
    }
}
