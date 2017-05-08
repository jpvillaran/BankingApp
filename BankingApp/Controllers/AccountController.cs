using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BankingApp.Model;
using BankingApp.Models;
using BankingApp.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using BankingApp.Infrastructure;
using BankingApp.Data;

namespace BankingApp.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly IBankingContext _context;
        private readonly UserManager<BankingIdentityUser> _userManager;
        private readonly SignInManager<BankingIdentityUser> _loginManager;
        private readonly RoleManager<BankingIdentityRole> _roleManager;


        public AccountController(IBankingContext context, 
            UserManager<BankingIdentityUser> userManager,
            SignInManager<BankingIdentityUser> loginManager,
            RoleManager<BankingIdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _loginManager = loginManager;
            _roleManager = roleManager;
        }

        // GET: Accounts/Login
        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public IActionResult Login([Bind("AccountNumber,Password")] LoginViewModel login)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            if (ModelState.IsValid)
            {
                var result = _loginManager.PasswordSignInAsync(login.AccountNumber, login.Password, login.RememberMe, false).Result;

                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError("", "Invalid login!");
            }

            return View(login);
        }

        // GET: Accounts/Register
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Register([Bind("AccountNumber,AccountName,Password,InitialBalance")] RegisterAccountViewModel userAccount)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            if (ModelState.IsValid)
            {
                var userAccounts = await _context.UserAccounts.Where(u => u.AccountNumber == userAccount.AccountNumber).ToListAsync();

                if (userAccounts.Count > 0)
                {
                    ModelState.AddModelError("Error", "The account number specified already exists.");
                }
                else
                {
                    BankingIdentityUser user = new BankingIdentityUser
                    {
                        UserName = userAccount.AccountNumber,
                        Email = "account_" + userAccount.AccountNumber + "@banking.io",
                        FullName = userAccount.AccountName
                    };

                    IdentityResult result = _userManager.CreateAsync(user, userAccount.Password).Result;

                    if (result.Succeeded)
                    {
                        if (!_roleManager.RoleExistsAsync("NormalUser").Result)
                        {
                            BankingIdentityRole role = new BankingIdentityRole();
                            role.Name = "NormalUser";
                            role.Description = "Perform normal operations.";
                            IdentityResult roleResult = _roleManager.CreateAsync(role).Result;

                            if (!roleResult.Succeeded)
                            {
                                ModelState.AddModelError("", "Error while creating role!");
                                return View(userAccount);
                            }
                        }

                        _userManager.AddToRoleAsync(user,"NormalUser").Wait();

                        var id = Guid.NewGuid();
                        var registeredAccount = new UserAccount
                        {
                            Id = id,
                            AccountNumber = userAccount.AccountNumber,
                            CreateDate = DateTime.Now
                        };

                        _context.Add(registeredAccount);
                        _context.Add(new TransactionInfo
                        {
                            Id = Guid.NewGuid(),
                            TransactionType = Enums.TransactionType.Credit,
                            TransactionDate = DateTime.Now,
                            UserAccountId = id,
                            Amount = userAccount.InitialBalance
                        });
                        await _context.SaveChangesAsync();

                        return RedirectToAction("Login", "Account");
                    }
                    else
                    {
                        ModelState.AddModelError("Error", "Unable to create the user. " + result.Errors.FirstOrDefault().Description);
                    }
                }
            }
            return View(userAccount);
        }

        public IActionResult LogOff()
        {
            _loginManager.SignOutAsync().Wait();
            return RedirectToAction("Login", "Account");
        }

    }
}
