using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BankingApp.ViewModel
{
    public class DepositViewModel
    {
        /// <summary>
        /// The account's deposit amount.
        /// </summary>
        [Display(Name = "Deposit amount")]
        [Required]
        [Range(0.01, Double.MaxValue, ErrorMessage = "Deposit amount must be greater than zero.")]
        public double Amount { get; set; }
    }
}
