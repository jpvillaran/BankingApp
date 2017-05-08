using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BankingApp.Models;
using Microsoft.AspNetCore.Identity;
using BankingApp.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using BankingApp.ViewModel;
using Microsoft.EntityFrameworkCore;
using BankingApp.Model;
using BankingApp.Data;

namespace BankingApp.Controllers
{
    [Authorize]
    public class TransactionController : Controller
    {
        private readonly IBankingContext _context;
        private readonly UserManager<BankingIdentityUser> _userManager;

        public TransactionController(IBankingContext context, UserManager<BankingIdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Deposit()
        {
            ViewData["UserAccount"] = await this.GetAccount();
            return View();
        }

        public async Task<IActionResult> Withdraw()
        {
            ViewData["UserAccount"] = await this.GetAccount();
            return View();
        }

        public async Task<IActionResult> Transfer()
        {
            ViewData["UserAccount"] = await this.GetAccount();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deposit([Bind("Amount")] DepositViewModel deposit)
        {
            if (ModelState.IsValid)
            {
                var account = await this.GetAccount();
                _context.Add(new TransactionInfo
                {
                    Id = Guid.NewGuid(),
                    TransactionType = Enums.TransactionType.Credit,
                    Amount = deposit.Amount,
                    TransactionDate = DateTime.Now,
                    UserAccountId = account.Id
                });

                var result = await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Home");
            }
            ViewData["UserAccount"] = await this.GetAccount();
            return View(deposit);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw([Bind("Amount")] WithdrawalViewModel withdrawal)
        {
            if (ModelState.IsValid)
            {
                var account = await this.GetAccount();
                if (withdrawal.Amount > account.CurrentBalance)
                {
                    ModelState.AddModelError("Error", "The account balance is lesser than the withdrawal amount.");
                }
                else
                {
                    _context.Add(new TransactionInfo
                    {
                        Id = Guid.NewGuid(),
                        TransactionType = Enums.TransactionType.Debit,
                        Amount = withdrawal.Amount,
                        TransactionDate = DateTime.Now,
                        UserAccountId = account.Id
                    });

                    var result = await _context.SaveChangesAsync();
                    ViewData["UserAccount"] = await this.GetAccount();
                    return RedirectToAction("Index", "Home");
                }
            }
            ViewData["UserAccount"] = await this.GetAccount();
            return View(withdrawal);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Transfer([Bind("AccountNumber", "Amount")] TransferViewModel transfer)
        {
            if (ModelState.IsValid)
            {
                var account = await this.GetAccount();
                if (transfer.AccountNumber == account.AccountNumber)
                {
                    ModelState.AddModelError("Error", "Unable to transfer to own account.");
                }
                else if (transfer.Amount > account.CurrentBalance)
                {
                    ModelState.AddModelError("Error", "The account balance is lesser than the transfer amount.");
                }
                else
                {
                    var destinationAccount = await GetAccount(transfer.AccountNumber);

                    if (destinationAccount == null)
                    {
                        ModelState.AddModelError("Error", string.Format("The account number {0} does not exist.", transfer.AccountNumber));
                    }
                    else
                    {
                        _context.Add(new TransactionInfo
                        {
                            Id = Guid.NewGuid(),
                            TransactionType = Enums.TransactionType.Transfer,
                            Amount = transfer.Amount,
                            TransactionDate = DateTime.Now,
                            UserAccountId = account.Id
                        });
                        _context.Add(new TransactionInfo
                        {
                            Id = Guid.NewGuid(),
                            TransactionType = Enums.TransactionType.Receive,
                            Amount = transfer.Amount,
                            TransactionDate = DateTime.Now,
                            UserAccountId = destinationAccount.Id
                        });
                        var result = await _context.SaveChangesAsync();
                        return RedirectToAction("Index", "Home");
                    }
                }
                ViewData["UserAccount"] = account;
            }
            return View(transfer);
        }

        private async Task<UserAccount> GetAccount()
        {
            BankingIdentityUser user = _userManager.GetUserAsync(HttpContext.User).Result;
            ViewData["FullName"] = user.FullName;

            return await GetAccount(User.Identity.Name);
        }

        private async Task<UserAccount> GetAccount(string accountNumber)
        {
            var accounts = await _context.UserAccounts
                                    .Include(u => u.Transactions)
                                    .Where(u => u.AccountNumber == accountNumber)
                                    .ToListAsync();

            return accounts.FirstOrDefault();
        }
    }
}