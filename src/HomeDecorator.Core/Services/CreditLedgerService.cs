using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HomeDecorator.Core.Services
{
    /// <summary>
    /// Represents a credit transaction in the ledger
    /// </summary>
    public class CreditTransaction
    {
        /// <summary>
        /// Unique identifier for the transaction
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// User ID associated with this transaction
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Amount of credits (positive for additions, negative for deductions)
        /// </summary>
        public int Amount { get; set; }

        /// <summary>
        /// Type of transaction (Purchase, Usage, Refund, etc.)
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Description of the transaction
        /// </summary>
        public string Description { get; set; }        /// <summary>
                                                       /// References to external systems (e.g., Stripe payment ID)
                                                       /// </summary>
        public string? ReferenceId { get; set; }

        /// <summary>
        /// When the transaction occurred
        /// </summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Service for managing the credit ledger
    /// </summary>
    public interface ICreditLedgerService
    {
        /// <summary>
        /// Get the current credit balance for a user
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>Number of available credits</returns>
        Task<int> GetBalanceAsync(string userId);        /// <summary>
                                                         /// Add credits to a user's account
                                                         /// </summary>
                                                         /// <param name="userId">The user ID</param>
                                                         /// <param name="amount">Amount of credits to add</param>
                                                         /// <param name="type">Transaction type</param>
                                                         /// <param name="description">Transaction description</param>
                                                         /// <param name="referenceId">External reference ID (e.g., Stripe payment ID)</param>
                                                         /// <returns>The created transaction</returns>
        Task<CreditTransaction> AddCreditsAsync(
            string userId,
            int amount,
            string type,
            string description,
            string? referenceId = null);        /// <summary>
                                                /// Deduct credits from a user's account
                                                /// </summary>
                                                /// <param name="userId">The user ID</param>
                                                /// <param name="amount">Amount of credits to deduct</param>
                                                /// <param name="type">Transaction type</param>
                                                /// <param name="description">Transaction description</param>
                                                /// <param name="referenceId">External reference ID</param>
                                                /// <returns>The created transaction</returns>
        Task<CreditTransaction> DeductCreditsAsync(
            string userId,
            int amount,
            string type,
            string description,
            string? referenceId = null);

        /// <summary>
        /// Get transaction history for a user
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="count">Maximum number of transactions to return</param>
        /// <returns>List of transactions</returns>
        Task<List<CreditTransaction>> GetTransactionHistoryAsync(string userId, int count = 20);
    }
}
