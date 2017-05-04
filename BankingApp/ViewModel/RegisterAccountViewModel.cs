using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BankingApp.ViewModel
{
    public class RegisterAccountViewModel
    {
        /// <summary>
        /// The user's account number.  This value should be unique for the whole accounts table.
        /// </summary>
        [Display(Name = "Account Number")]
        [MaxLength(20)]
        [Required]
        public string AccountNumber { get; set; }

        /// <summary>
        /// The name of the user who's account is registered.
        /// </summary>
        [Display(Name = "Account Name")]
        [MaxLength(200)]
        [Required]
        public string AccountName { get; set; }

        /// <summary>
        /// A secret word or phrase that must be used to gain admission to the said account.
        /// This value is the encrypted version of the password.
        /// </summary>
        [Display(Name = "Password")]
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        /// <summary>
        /// The account's initial balance.
        /// </summary>
        [Display(Name = "Initial Balance")]
        [Required]
        [Range(0.01, Double.MaxValue, ErrorMessage = "Initial balance must be greater than zero.")]
        public double InitialBalance { get; set; }

    }
}
