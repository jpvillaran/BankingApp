using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BankingApp.Models;
using Microsoft.EntityFrameworkCore;
using BankingApp.Infrastructure;
using Microsoft.AspNetCore.Identity;
using BankingApp.Data;

namespace BankingApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly IBankingContext _context;
        private readonly UserManager<BankingIdentityUser> _userManager;

        public HomeController(IBankingContext context, UserManager<BankingIdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            BankingIdentityUser user = _userManager.GetUserAsync(HttpContext.User).Result;

            ViewData["FullName"] = user.FullName;
            var account = await _context.UserAccounts
                                    .Include(u => u.Transactions)
                                    .Where(u => u.AccountNumber == User.Identity.Name)
                                    .ToListAsync();

            return View(account.FirstOrDefault());
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
