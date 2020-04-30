using System;

namespace JCAppStore_Parser
{
    public interface ILogger : IDisposable
    {
        /// <summary>
        /// Loggs message
        /// </summary>
        /// <param name="message">message to log</param>
        void Log(string message);

        /// <summary>
        /// Increases the progress by one unit
        /// </summary>
        void Step();
    }
}
