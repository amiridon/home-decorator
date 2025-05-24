using HomeDecorator.Core.Models;
using HomeDecorator.Core.Services; // Add interface namespace
using Microsoft.Data.Sqlite;

namespace HomeDecorator.Api.Services;

/// <summary>
/// Service for writing log entries to SQLite database.
/// </summary>
public class SqliteLogService : ILogService
{
    private readonly string _connectionString;

    public SqliteLogService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=homedecorator.db";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS LogEntries (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                RequestId TEXT NOT NULL,
                Timestamp DATETIME NOT NULL,
                Level TEXT NOT NULL,
                Message TEXT NOT NULL
            );";
        command.ExecuteNonQuery();
    }

    public void Log(string requestId, string level, string message)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO LogEntries (RequestId, Timestamp, Level, Message)
            VALUES (@RequestId, @Timestamp, @Level, @Message);";
        command.Parameters.AddWithValue("@RequestId", requestId);
        command.Parameters.AddWithValue("@Timestamp", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("@Level", level);
        command.Parameters.AddWithValue("@Message", message);
        command.ExecuteNonQuery();
    }

    public List<LogEntry> GetLogs(string requestId)
    {
        var logs = new List<LogEntry>();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, RequestId, Timestamp, Level, Message
            FROM LogEntries
            WHERE RequestId = @RequestId
            ORDER BY Timestamp ASC;";
        command.Parameters.AddWithValue("@RequestId", requestId);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            logs.Add(new LogEntry
            {
                Id = reader.GetInt64(0),
                RequestId = reader.GetString(1),
                Timestamp = DateTime.Parse(reader.GetString(2)),
                Level = reader.GetString(3),
                Message = reader.GetString(4)
            });
        }
        return logs;
    }
}
