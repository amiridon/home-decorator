using HomeDecorator.Core.Models;
using HomeDecorator.Core.Services;
using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace HomeDecorator.Api.Services;

/// <summary>
/// SQLite implementation of IImageRequestRepository
/// </summary>
public class SqliteImageRequestRepository : IImageRequestRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteImageRequestRepository> _logger;

    public SqliteImageRequestRepository(IConfiguration configuration, ILogger<SqliteImageRequestRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                           "Data Source=homedecorator.db";
        _logger = logger;

        InitializeDatabase();
    }
    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // First ensure Users table exists
        var usersCommand = connection.CreateCommand();
        usersCommand.CommandText = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id TEXT PRIMARY KEY,
                Name TEXT,
                Email TEXT,
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
            );";
        usersCommand.ExecuteNonQuery();

        // Ensure test user exists
        var testUserCommand = connection.CreateCommand();
        testUserCommand.CommandText = @"
            INSERT OR IGNORE INTO Users (Id, Name, Email)
            VALUES ('test-user', 'Test User', 'test@example.com');";
        testUserCommand.ExecuteNonQuery();

        // Ensure ImageRequests table exists
        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS ImageRequests (
                Id TEXT PRIMARY KEY,
                UserId TEXT NOT NULL,
                OriginalImageUrl TEXT NOT NULL,
                Prompt TEXT NOT NULL,
                Status TEXT CHECK(Status IN ('Pending','Processing','Completed','Failed')),
                GeneratedImageUrl TEXT,
                CreditsCharged INTEGER DEFAULT 0,
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                CompletedAt DATETIME,
                ErrorMessage TEXT,
                FOREIGN KEY(UserId) REFERENCES Users(Id)
            );";
        command.ExecuteNonQuery();

        _logger.LogInformation("Database initialized with Users and ImageRequests tables");
    }

    public async Task<ImageRequest> CreateAsync(ImageRequest request)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO ImageRequests (Id, UserId, OriginalImageUrl, Prompt, Status, CreditsCharged, CreatedAt)
            VALUES (@Id, @UserId, @OriginalImageUrl, @Prompt, @Status, @CreditsCharged, @CreatedAt)";

        command.Parameters.AddWithValue("@Id", request.Id);
        command.Parameters.AddWithValue("@UserId", request.UserId);
        command.Parameters.AddWithValue("@OriginalImageUrl", request.OriginalImageUrl);
        command.Parameters.AddWithValue("@Prompt", request.Prompt);
        command.Parameters.AddWithValue("@Status", request.Status);
        command.Parameters.AddWithValue("@CreditsCharged", request.CreditsCharged);
        command.Parameters.AddWithValue("@CreatedAt", request.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));

        await command.ExecuteNonQueryAsync();

        _logger.LogInformation("Created image request: {RequestId}", request.Id);
        return request;
    }

    public async Task<ImageRequest?> GetByIdAsync(string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, UserId, OriginalImageUrl, Prompt, Status, GeneratedImageUrl, 
                   CreditsCharged, CreatedAt, CompletedAt, ErrorMessage
            FROM ImageRequests 
            WHERE Id = @Id";

        command.Parameters.AddWithValue("@Id", id);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapFromReader(reader);
        }

        return null;
    }

    public async Task<ImageRequest> UpdateAsync(ImageRequest request)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE ImageRequests 
            SET Status = @Status, 
                GeneratedImageUrl = @GeneratedImageUrl,
                CompletedAt = @CompletedAt,
                ErrorMessage = @ErrorMessage
            WHERE Id = @Id";

        command.Parameters.AddWithValue("@Id", request.Id);
        command.Parameters.AddWithValue("@Status", request.Status);
        command.Parameters.AddWithValue("@GeneratedImageUrl", request.GeneratedImageUrl ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CompletedAt", request.CompletedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ErrorMessage", request.ErrorMessage ?? (object)DBNull.Value);

        await command.ExecuteNonQueryAsync();

        _logger.LogInformation("Updated image request: {RequestId}", request.Id);
        return request;
    }

    public async Task<List<ImageRequest>> GetByUserIdAsync(string userId, int limit = 10)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, UserId, OriginalImageUrl, Prompt, Status, GeneratedImageUrl, 
                   CreditsCharged, CreatedAt, CompletedAt, ErrorMessage
            FROM ImageRequests 
            WHERE UserId = @UserId 
            ORDER BY CreatedAt DESC 
            LIMIT @Limit";

        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@Limit", limit);

        var requests = new List<ImageRequest>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            requests.Add(MapFromReader(reader));
        }

        return requests;
    }

    public async Task<List<ImageRequest>> GetRecentAsync(int limit = 50)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, UserId, OriginalImageUrl, Prompt, Status, GeneratedImageUrl, 
                   CreditsCharged, CreatedAt, CompletedAt, ErrorMessage
            FROM ImageRequests 
            ORDER BY CreatedAt DESC 
            LIMIT @Limit";

        command.Parameters.AddWithValue("@Limit", limit);

        var requests = new List<ImageRequest>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            requests.Add(MapFromReader(reader));
        }

        return requests;
    }
    private static ImageRequest MapFromReader(SqliteDataReader reader)
    {
        return new ImageRequest
        {
            Id = reader.GetString(reader.GetOrdinal("Id")),
            UserId = reader.GetString(reader.GetOrdinal("UserId")),
            OriginalImageUrl = reader.GetString(reader.GetOrdinal("OriginalImageUrl")),
            Prompt = reader.GetString(reader.GetOrdinal("Prompt")),
            Status = reader.GetString(reader.GetOrdinal("Status")),
            GeneratedImageUrl = reader.IsDBNull(reader.GetOrdinal("GeneratedImageUrl")) ? null : reader.GetString(reader.GetOrdinal("GeneratedImageUrl")),
            CreditsCharged = reader.GetInt32(reader.GetOrdinal("CreditsCharged")),
            CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedAt"))),
            CompletedAt = reader.IsDBNull(reader.GetOrdinal("CompletedAt")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("CompletedAt"))),
            ErrorMessage = reader.IsDBNull(reader.GetOrdinal("ErrorMessage")) ? null : reader.GetString(reader.GetOrdinal("ErrorMessage"))
        };
    }
}
