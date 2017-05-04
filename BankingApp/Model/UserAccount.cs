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
    /// Contains details about a user's account, including the create date and the last access date.
    /// </summary>
    public class UserAccount
    {
        /// <summary>
        /// Unique identifier for the User's account.  For DB use.
        /// </summary>
        [Key]
        public Guid Id { get; set; }


        /// <summary>
        /// The user's account number.  This value should be unique for the whole accounts table.
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string AccountNumber { get; set; }

        /// <summary>
        /// The date when the account was created.
        /// </summary>
        public DateTime CreateDate { get; set; }

        /// <summary>
        /// The currently computed balance of the user.
        /// </summary>
        [ConcurrencyCheck]
        [NotMapped]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}")]
        public double CurrentBalance {
            get
            {
                return
                    Transactions.Where(e => e.TransactionType == TransactionType.Credit).Select(e => e.Amount).Sum() +
                    Transactions.Where(e => e.TransactionType == TransactionType.Receive).Select(e => e.Amount).Sum() -
                    Transactions.Where(e => e.TransactionType == TransactionType.Debit).Select(e => e.Amount).Sum() -
                    Transactions.Where(e => e.TransactionType == TransactionType.Transfer).Select(e => e.Amount).Sum();
            }
        }

        /// <summary>
        /// Shows a list of transactions performed by the user.
        /// </summary>
        public List<TransactionInfo> Transactions { get; set; }
    }
}
