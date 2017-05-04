using BankingApp.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace BankingApp.Model
{
    /// <summary>
    /// Contains details about the transactions that occurred in the system.  Each
    /// transaction requires the user account, as well as the amount and the transaction type.
    /// </summary>
    public class TransactionInfo
    {
        /// <summary>
        /// Unique identifier for the transaction performed by the user.  For DB use.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// The type of transaction performed by the user.  This could be any of the following: Debit, Credit, Transfer, Receive.
        /// </summary>
        public TransactionType TransactionType { get; set; }

        /// <summary>
        /// The value that was used for the transaction, be it a Deposit, Withdrawal, Transfer, or Receiving of an amount.
        /// </summary>
        [DisplayFormat(DataFormatString = "{0:#,##0.00}")]
        public Double Amount { get; set; }

        /// <summary>
        /// The date that the transaction was performed.
        /// </summary>
        public DateTime TransactionDate { get; set; }

        /// <summary>
        /// The Reference Id for the User Account performing the transaction.
        /// </summary>
        public Guid UserAccountId { get; set; }

        /// <summary>
        /// Relationship between the user account and this transaction record.
        /// </summary>
        [ForeignKey("UserAccountId")]
        public UserAccount UserAccount { get; set; }
    }
}
