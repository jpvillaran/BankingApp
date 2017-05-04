using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BankingApp.ViewModel
{
    public class TransferViewModel
    {
        /// <summary>
        /// The user's account number.  This value should be unique for the whole accounts table.
        /// </summary>
        [Display(Name = "Destination Account Number")]
        [MaxLength(20)]
        [Required]
        public string AccountNumber { get; set; }

        /// <summary>
        /// The account's withdrawal amount.
        /// </summary>
        [Display(Name = "Transfer amount")]
        [Required]
        [Range(0.01, Double.MaxValue, ErrorMessage = "Transfer amount must be greater than zero.")]
        public double Amount { get; set; }
    }
}
