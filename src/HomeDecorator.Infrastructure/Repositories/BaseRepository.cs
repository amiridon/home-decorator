using HomeDecorator.Core.Services;
using Microsoft.Data.Sqlite;
using System.Data;

namespace HomeDecorator.Infrastructure.Repositories;

/// <summary>
/// Base repository class that provides common database functionality
/// </summary>
public class BaseRepository
{
    private readonly string _connectionString;

    public BaseRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// Creates a new SQLite connection
    /// </summary>
    /// <returns>An open SQLite connection</returns>
    protected IDbConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    /// <summary>
    /// Ensures the database schema is created
    /// </summary>
    public void EnsureDatabaseCreated()
    {
        using var connection = CreateConnection();
        using var command = connection.CreateCommand();
        
        // Create Users table
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id TEXT PRIMARY KEY,
                Email TEXT NOT NULL UNIQUE,
                DisplayName TEXT,
                StripeCustomerId TEXT,
                Credits INTEGER DEFAULT 0,
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
            );";
        command.ExecuteNonQuery();
        
        // Create PaymentTransactions table
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS PaymentTransactions (
                Id TEXT PRIMARY KEY,
                UserId TEXT NOT NULL,
                StripePaymentIntentId TEXT,
                AmountCents INTEGER,
                Currency TEXT,
                CreditsPurchased INTEGER,
                Type TEXT CHECK(Type IN ('OneTime','Subscription','Refund')),
                Status TEXT CHECK(Status IN ('Pending','Succeeded','Failed','Refunded')),
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY(UserId) REFERENCES Users(Id)
            );";
        command.ExecuteNonQuery();
        
        // Create Subscriptions table
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Subscriptions (
                Id TEXT PRIMARY KEY,
                UserId TEXT NOT NULL,
                StripeSubscriptionId TEXT,
                PlanName TEXT,
                Status TEXT CHECK(Status IN ('Active','PastDue','Canceled','Trialing')),
                CurrentPeriodEnd DATETIME,
                FOREIGN KEY(UserId) REFERENCES Users(Id)
            );";
        command.ExecuteNonQuery();
        
        // Create ImageRequests table
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
                FOREIGN KEY(UserId) REFERENCES Users(Id)
            );";
        command.ExecuteNonQuery();
        
        // Create Products table
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Products (
                Id TEXT PRIMARY KEY,
                ExternalId TEXT NOT NULL,
                Name TEXT,
                ThumbnailUrl TEXT,
                DetailUrl TEXT,
                Price DECIMAL(10,2),
                Currency TEXT,
                Vendor TEXT
            );";
        command.ExecuteNonQuery();
        
        // Create ImageRequestProducts table
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS ImageRequestProducts (
                ImageRequestId TEXT,
                ProductId TEXT,
                Score REAL,
                PRIMARY KEY (ImageRequestId, ProductId),
                FOREIGN KEY(ImageRequestId) REFERENCES ImageRequests(Id),
                FOREIGN KEY(ProductId) REFERENCES Products(Id)
            );";
        command.ExecuteNonQuery();
        
        // Create suggested indexes
        command.CommandText = "CREATE INDEX IF NOT EXISTS idx_user_credits ON Users(Id,Credits);";
        command.ExecuteNonQuery();
        
        command.CommandText = "CREATE INDEX IF NOT EXISTS idx_img_status ON ImageRequests(Status);";
        command.ExecuteNonQuery();
    }
}
