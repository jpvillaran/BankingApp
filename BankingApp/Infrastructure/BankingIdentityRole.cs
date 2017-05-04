using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BankingApp.Infrastructure
{
    public class BankingIdentityRole : IdentityRole
    {
        public string Description { get; set; }
    }
}
