using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BankingApp.Model;
using BankingApp.Data;

namespace BankingApp.Models
{
    public class BankingContext : DbContext, IBankingContext
    {
        public BankingContext (DbContextOptions<BankingContext> options)
            : base(options)
        {
        }

        public DbSet<UserAccount> UserAccounts { get; set; }
        public DbSet<TransactionInfo> Transactions { get; set; }
    }
}
