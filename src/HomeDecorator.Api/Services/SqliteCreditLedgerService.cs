using HomeDecorator.Core.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HomeDecorator.Api.Services
{
    /// <summary>
    /// Implementation of ICreditLedgerService using SQLite
    /// </summary>
    public class SqliteCreditLedgerService : ICreditLedgerService
    {
        private readonly string _connectionString;

        public SqliteCreditLedgerService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                "Data Source=homedecorator.db";

            // Ensure the database and table exist
            EnsureDatabaseCreated();
        }

        private void EnsureDatabaseCreated()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
            CREATE TABLE IF NOT EXISTS credit_transactions (
                id TEXT PRIMARY KEY,
                user_id TEXT NOT NULL,
                amount INTEGER NOT NULL,
                type TEXT NOT NULL,
                description TEXT,
                reference_id TEXT,
                timestamp TEXT NOT NULL
            );
            
            CREATE INDEX IF NOT EXISTS ix_credit_transactions_user_id 
            ON credit_transactions(user_id);
        ";
            command.ExecuteNonQuery();

            // Add initial credits for the test user if they don't exist already
            var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = @"
            SELECT COUNT(*) FROM credit_transactions WHERE user_id = 'test-user';
        ";
            var count = Convert.ToInt32(checkCommand.ExecuteScalar());

            if (count == 0)
            {
                // Add 100 initial credits for testing
                var insertCommand = connection.CreateCommand();
                insertCommand.CommandText = @"
                INSERT INTO credit_transactions (id, user_id, amount, type, description, timestamp)
                VALUES ($id, $userId, $amount, $type, $description, $timestamp)
            ";
                insertCommand.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
                insertCommand.Parameters.AddWithValue("$userId", "test-user");
                insertCommand.Parameters.AddWithValue("$amount", 100); // 100 credits
                insertCommand.Parameters.AddWithValue("$type", "initial");
                insertCommand.Parameters.AddWithValue("$description", "Initial test credits");
                insertCommand.Parameters.AddWithValue("$timestamp", DateTime.UtcNow.ToString("o"));

                insertCommand.ExecuteNonQuery();

                Console.WriteLine("Added 100 initial credits for test-user");
            }
        }

        public async Task<int> GetBalanceAsync(string userId)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COALESCE(SUM(amount), 0) 
                FROM credit_transactions 
                WHERE user_id = $userId
            ";
            command.Parameters.AddWithValue("$userId", userId);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        public async Task<CreditTransaction> AddCreditsAsync(
            string userId,
            int amount,
            string type,
            string description,
            string? referenceId = null)
        {
            // Ensure amount is positive
            if (amount <= 0)
            {
                throw new ArgumentException("Amount must be positive", nameof(amount));
            }

            var transaction = new CreditTransaction
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Amount = amount, // Positive for additions
                Type = type,
                Description = description,
                ReferenceId = referenceId,
                Timestamp = DateTime.UtcNow
            };

            await SaveTransactionAsync(transaction);
            return transaction;
        }
        public async Task<CreditTransaction> DeductCreditsAsync(
            string userId,
            int amount,
            string type,
            string description,
            string? referenceId = null)
        {
            // Ensure amount is positive
            int positiveAmount = Math.Abs(amount);

            // Check if user has enough credits
            int balance = await GetBalanceAsync(userId);
            if (balance < positiveAmount)
            {
                throw new InvalidOperationException($"Insufficient credits. Current balance: {balance}, Required: {positiveAmount}");
            }

            var transaction = new CreditTransaction
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Amount = -positiveAmount, // Negative for deductions
                Type = type,
                Description = description,
                ReferenceId = referenceId,
                Timestamp = DateTime.UtcNow
            };

            await SaveTransactionAsync(transaction);
            return transaction;
        }

        public async Task<List<CreditTransaction>> GetTransactionHistoryAsync(string userId, int count = 20)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, user_id, amount, type, description, reference_id, timestamp
                FROM credit_transactions
                WHERE user_id = $userId
                ORDER BY timestamp DESC
                LIMIT $count
            ";
            command.Parameters.AddWithValue("$userId", userId);
            command.Parameters.AddWithValue("$count", count);

            var transactions = new List<CreditTransaction>();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                transactions.Add(new CreditTransaction
                {
                    Id = reader.GetString(0),
                    UserId = reader.GetString(1),
                    Amount = reader.GetInt32(2),
                    Type = reader.GetString(3),
                    Description = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    ReferenceId = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Timestamp = DateTime.Parse(reader.GetString(6))
                });
            }

            return transactions;
        }

        private async Task SaveTransactionAsync(CreditTransaction transaction)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO credit_transactions (id, user_id, amount, type, description, reference_id, timestamp)
                VALUES ($id, $userId, $amount, $type, $description, $referenceId, $timestamp)
            ";
            command.Parameters.AddWithValue("$id", transaction.Id);
            command.Parameters.AddWithValue("$userId", transaction.UserId);
            command.Parameters.AddWithValue("$amount", transaction.Amount);
            command.Parameters.AddWithValue("$type", transaction.Type); command.Parameters.AddWithValue("$description", transaction.Description == null ? (object)DBNull.Value : transaction.Description);
            command.Parameters.AddWithValue("$referenceId", transaction.ReferenceId == null ? (object)DBNull.Value : transaction.ReferenceId);
            command.Parameters.AddWithValue("$timestamp", transaction.Timestamp.ToString("o"));

            await command.ExecuteNonQueryAsync();
        }
    }
}
