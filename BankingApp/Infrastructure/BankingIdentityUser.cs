using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BankingApp.Infrastructure
{
    public class BankingIdentityUser : IdentityUser
    {
        public string FullName { get; set; }
    }
}
