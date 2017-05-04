using BankingApp.Models;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace BankingApp.Model
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new BankingContext(serviceProvider.GetRequiredService<DbContextOptions<BankingContext>>()))
            {
                if (context.UserAccounts.Any())
                {
                    return;
                }

                // nothing to seed right now
            }
        }
    }
}
