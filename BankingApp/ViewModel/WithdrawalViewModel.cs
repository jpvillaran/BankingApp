using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BankingApp.ViewModel
{
    public class WithdrawalViewModel
    {
        /// <summary>
        /// The account's withdrawal amount.
        /// </summary>
        [Display(Name = "Withdrawal amount")]
        [Required]
        [Range(0.01, Double.MaxValue, ErrorMessage = "Withdrawal amount must be greater than zero.")]
        public double Amount { get; set; }
    }
}
