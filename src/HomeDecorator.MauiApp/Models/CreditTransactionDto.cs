using System;

namespace HomeDecorator.MauiApp.Models
{    /// <summary>
     /// DTO to represent a credit transaction
     /// </summary>
    public class CreditTransactionDto
    {
        /// <summary>
        /// Unique identifier for the transaction
        /// </summary>
        public required string Id { get; set; }

        /// <summary>
        /// User ID associated with this transaction
        /// </summary>
        public required string UserId { get; set; }

        /// <summary>
        /// Amount of credits (positive for additions, negative for deductions)
        /// </summary>
        public int Amount { get; set; }

        /// <summary>
        /// Type of transaction (Purchase, Usage, Refund, etc.)
        /// </summary>
        public required string Type { get; set; }

        /// <summary>
        /// Description of the transaction
        /// </summary>
        public required string Description { get; set; }

        /// <summary>
        /// References to external systems (e.g., Stripe payment ID)
        /// </summary>
        public string? ReferenceId { get; set; }

        /// <summary>
        /// When the transaction occurred
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
