using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BankingApp.Model;

namespace BankingApp.Models
{
    public class BankingContext : DbContext
    {
        public BankingContext (DbContextOptions<BankingContext> options)
            : base(options)
        {
        }

        public DbSet<UserAccount> UserAccounts { get; set; }
        public DbSet<TransactionInfo> Transactions { get; set; }
    }
}
