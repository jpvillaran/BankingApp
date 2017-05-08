using BankingApp.Data;
using BankingApp.Infrastructure;
using BankingApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BankingApp.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly IBankingContext _context;
        private readonly UserManager<BankingIdentityUser> _userManager;

        public ReportController(IBankingContext context, UserManager<BankingIdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> TransactionList()
        {
            BankingIdentityUser user = _userManager.GetUserAsync(HttpContext.User).Result;

            ViewData["FullName"] = user.FullName;
            var accountAsync = await _context.UserAccounts
                                    .Include(u => u.Transactions)
                                    .Where(u => u.AccountNumber == User.Identity.Name)
                                    .ToListAsync();

            var account = accountAsync.FirstOrDefault();
            ViewData["UserAccount"] = account;

            return View(account.Transactions.OrderByDescending(t => t.TransactionDate));
        }
    }
}
