using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HomeDecorator.Core.Services;

namespace HomeDecorator.MauiApp.Services
{
    /// <summary>
    /// Mock implementation of ICreditLedgerService for development
    /// </summary>
    public class MockCreditLedgerService : ICreditLedgerService
    {
        private readonly Dictionary<string, List<CreditTransaction>> _userTransactions = new();
        private readonly Dictionary<string, int> _balances = new();

        // Default balance for new users
        private const int DefaultStartingBalance = 15;

        public Task<int> GetBalanceAsync(string userId)
        {
            if (!_balances.ContainsKey(userId))
            {
                _balances[userId] = DefaultStartingBalance;

                // Add initial credit as first transaction
                if (!_userTransactions.ContainsKey(userId))
                {
                    _userTransactions[userId] = new List<CreditTransaction>();
                }

                _userTransactions[userId].Add(new CreditTransaction
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    Amount = DefaultStartingBalance,
                    Type = "Welcome",
                    Description = "Welcome bonus credits",
                    Timestamp = DateTime.UtcNow.AddDays(-30)
                });
            }

            return Task.FromResult(_balances[userId]);
        }
        public Task<CreditTransaction> AddCreditsAsync(
            string userId,
            int amount,
            string type,
            string description,
            string? referenceId = null)
        {
            // Create transaction
            var transaction = new CreditTransaction
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Amount = amount,
                Type = type,
                Description = description,
                ReferenceId = referenceId,
                Timestamp = DateTime.UtcNow
            };

            // Initialize collections if needed
            if (!_userTransactions.ContainsKey(userId))
            {
                _userTransactions[userId] = new List<CreditTransaction>();
            }

            if (!_balances.ContainsKey(userId))
            {
                _balances[userId] = 0;
            }

            // Add transaction and update balance
            _userTransactions[userId].Add(transaction);
            _balances[userId] += amount;

            return Task.FromResult(transaction);
        }
        public Task<CreditTransaction> DeductCreditsAsync(
            string userId,
            int amount,
            string type,
            string description,
            string? referenceId = null)
        {
            // Ensure positive amount for deduction logic
            int positiveAmount = Math.Abs(amount);

            // Ensure user exists
            if (!_balances.ContainsKey(userId))
            {
                _balances[userId] = DefaultStartingBalance;
            }

            // Check if user has enough credits
            if (_balances[userId] < positiveAmount)
            {
                throw new InvalidOperationException("Insufficient credits");
            }

            // Create transaction (amount as negative for deductions)
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

            // Initialize transactions if needed
            if (!_userTransactions.ContainsKey(userId))
            {
                _userTransactions[userId] = new List<CreditTransaction>();
            }

            // Add transaction and update balance
            _userTransactions[userId].Add(transaction);
            _balances[userId] -= positiveAmount;

            return Task.FromResult(transaction);
        }

        public Task<List<CreditTransaction>> GetTransactionHistoryAsync(string userId, int count = 20)
        {
            if (!_userTransactions.ContainsKey(userId))
            {
                return Task.FromResult(new List<CreditTransaction>());
            }

            // Return the most recent transactions first
            var transactions = _userTransactions[userId]
                .OrderByDescending(t => t.Timestamp)
                .Take(count)
                .ToList();

            return Task.FromResult(transactions);
        }
    }
}
