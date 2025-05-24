using HomeDecorator.Core.Models;
using System.Collections.Generic;

namespace HomeDecorator.Core.Services
{
    /// <summary>
    /// Service for writing and retrieving log entries.
    /// </summary>
    public interface ILogService
    {
        void Log(string requestId, string level, string message);
        List<LogEntry> GetLogs(string requestId);
    }
}
