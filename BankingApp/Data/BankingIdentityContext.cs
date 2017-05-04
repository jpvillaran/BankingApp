using BankingApp.Infrastructure;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BankingApp.Data
{
    public class BankingIdentityContext : IdentityDbContext<BankingIdentityUser, BankingIdentityRole, string>
    {
        public BankingIdentityContext(DbContextOptions<BankingIdentityContext> options)
        : base(options)
        {
            //nothing here
        }

    }
}
