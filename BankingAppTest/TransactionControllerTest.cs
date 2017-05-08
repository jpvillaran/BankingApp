using BankingApp.Controllers;
using BankingApp.Data;
using BankingApp.Enums;
using BankingApp.Infrastructure;
using BankingApp.Model;
using BankingApp.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BankingAppTest
{
    public class TransactionControllerTest : BaseControllerTest
    {
        public Tuple<TransactionController,
            Mock<IBankingContext>,
            Mock<UserManager<BankingIdentityUser>>> CreateTransactionController(TransactionTestOptions options)
        {
            var user = new BankingIdentityUser
            {
                UserName = options.Username,
                FullName = options.FullName
            };

            var identity = new Mock<ClaimsIdentity>();
            identity.Setup(i => i.Name).Returns(options.Username);
            identity.Setup(i => i.IsAuthenticated).Returns(options.IsAuthenticated);

            var claimsPrincipal = new ClaimsPrincipal(identity.Object);

            var mockContext = new Mock<IBankingContext>();
            var mockUserAccount = FakeDbSet<UserAccount>(options.userAccounts);
            mockContext.Setup(m => m.UserAccounts).Returns(mockUserAccount.Object);
            var userManager = FakeUserManager(um =>
            {
                um.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .Returns(Task.FromResult(user));
            });

            var controller = new TransactionController(mockContext.Object, userManager.Object);
            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = claimsPrincipal }
            };

            if (!options.IsModelStateValid)
            {
                controller.ModelState.AddModelError("error", "error");
            }

            return new Tuple<TransactionController,
                Mock<IBankingContext>,
                Mock<UserManager<BankingIdentityUser>>>(controller, mockContext, userManager);
        }

        [Fact(DisplayName = "Deposit should return a view result with the user account")]
        public void Deposit_Should_ReturnAViewResultWithTheUserAccount()
        {
            // arrange
            var userName = "123";
            var password = "password";
            var fullName = "fullName";
            var tuple = CreateTransactionController(new TransactionTestOptions
            {
                Username = userName,
                Password = password,
                FullName = fullName,
                userAccounts = new List<UserAccount>
                {
                    CreateUserAccount(userName, new List<TransactionInfo> {
                        CreateTransaction(100, TransactionType.Credit)
                    })
                },
                IsAuthenticated = true,
                IsModelStateValid = true
            });

            var controller = tuple.Item1;
            var mockContext = tuple.Item2;
            var userManager = tuple.Item3;

            // act
            var actionResultTask = controller.Deposit();
            actionResultTask.Wait();
            var actionResult = actionResultTask.Result as ViewResult;
            var account = actionResult.ViewData["UserAccount"];

            // assert
            Assert.NotNull(actionResult.ViewData["UserAccount"]);
            Assert.IsType<UserAccount>(actionResult.ViewData["UserAccount"]);

            userManager.Verify(c => c.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
            mockContext.Verify(c => c.UserAccounts, Times.Once);
        }

        [Fact(DisplayName = "Deposit should fail if the details are invalid")]
        public void Deposit_Should_FailIfTheDetailsAreInvalid()
        {
            // arrange
            var userName = "123";
            var password = "password";
            var fullName = "fullName";
            var tuple = CreateTransactionController(new TransactionTestOptions
            {
                Username = userName,
                Password = password,
                FullName = fullName,
                userAccounts = new List<UserAccount>
                {
                    CreateUserAccount(userName, new List<TransactionInfo> {
                        CreateTransaction(100, TransactionType.Credit)
                    })
                },
                IsAuthenticated = true,
                IsModelStateValid = false
            });

            var controller = tuple.Item1;
            var mockContext = tuple.Item2;
            var userManager = tuple.Item3;

            var deposit = new DepositViewModel
            {
                Amount = -1
            };

            // act
            var actionResultTask = controller.Deposit(deposit);
            actionResultTask.Wait();
            var actionResult = actionResultTask.Result as ViewResult;
            var model = actionResult.Model;

            // assert
            Assert.NotNull(model);
            Assert.IsType<DepositViewModel>(model);
            Assert.Contains("error", actionResult.ViewData.ModelState["Error"].Errors.First().ErrorMessage);
            userManager.Verify(c => c.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
            mockContext.Verify(c => c.Add(It.IsAny<TransactionInfo>()), Times.Never);
            mockContext.Verify(c => c.UserAccounts, Times.Once);
        }

        [Fact(DisplayName = "Deposit should Succeed if all details are correct")]
        public void Deposit_Should_SucceedIfAllDetailsAreCorrect()
        {
            // arrange
            var userName = "123";
            var password = "password";
            var fullName = "fullName";
            var tuple = CreateTransactionController(new TransactionTestOptions
            {
                Username = userName,
                Password = password,
                FullName = fullName,
                userAccounts = new List<UserAccount>
                {
                    CreateUserAccount(userName, new List<TransactionInfo> {
                        CreateTransaction(100, TransactionType.Credit)
                    })
                },
                IsAuthenticated = true,
                IsModelStateValid = true
            });

            var controller = tuple.Item1;
            var mockContext = tuple.Item2;
            var userManager = tuple.Item3;

            var deposit = new DepositViewModel
            {
                Amount = 100
            };

            // act
            var actionResultTask = controller.Deposit(deposit);
            actionResultTask.Wait();
            var actionResult = actionResultTask.Result as RedirectToActionResult;

            // assert
            Assert.Equal(actionResult.ActionName, "Index");
            Assert.Equal(actionResult.ControllerName, "Home");
            userManager.Verify(c => c.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
            mockContext.Verify(c => c.Add(It.Is<TransactionInfo>(t => t.TransactionType == TransactionType.Credit && t.Amount == 100)), Times.Once);
            mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            mockContext.Verify(c => c.UserAccounts, Times.Once);
        }

        [Fact(DisplayName = "Withdraw should return a view result with the user account")]
        public void Withdraw_Should_ReturnAViewResultWithTheUserAccount()
        {
            // arrange
            var userName = "123";
            var password = "password";
            var fullName = "fullName";
            var tuple = CreateTransactionController(new TransactionTestOptions
            {
                Username = userName,
                Password = password,
                FullName = fullName,
                userAccounts = new List<UserAccount>
                    {
                        CreateUserAccount(userName, new List<TransactionInfo> {
                            CreateTransaction(100, TransactionType.Credit)
                        })
                    },
                IsAuthenticated = true,
                IsModelStateValid = true
            });

            var controller = tuple.Item1;
            var mockContext = tuple.Item2;
            var userManager = tuple.Item3;

            // act
            var actionResultTask = controller.Withdraw();
            actionResultTask.Wait();
            var actionResult = actionResultTask.Result as ViewResult;
            var account = actionResult.ViewData["UserAccount"];

            // assert
            Assert.NotNull(actionResult.ViewData["UserAccount"]);
            Assert.IsType<UserAccount>(actionResult.ViewData["UserAccount"]);

            userManager.Verify(c => c.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
            mockContext.Verify(c => c.UserAccounts, Times.Once);
        }

        [Fact(DisplayName = "Withdraw should fail if the details are invalid")]
        public void Withdraw_Should_FailIfTheDetailsAreInvalid()
        {
            // arrange
            var userName = "123";
            var password = "password";
            var fullName = "fullName";
            var tuple = CreateTransactionController(new TransactionTestOptions
            {
                Username = userName,
                Password = password,
                FullName = fullName,
                userAccounts = new List<UserAccount>
                {
                    CreateUserAccount(userName, new List<TransactionInfo> {
                        CreateTransaction(100, TransactionType.Credit)
                    })
                },
                IsAuthenticated = true,
                IsModelStateValid = false
            });

            var controller = tuple.Item1;
            var mockContext = tuple.Item2;
            var userManager = tuple.Item3;

            var withdrawal = new WithdrawalViewModel
            {
                Amount = -1
            };

            // act
            var actionResultTask = controller.Withdraw(withdrawal);
            actionResultTask.Wait();
            var actionResult = actionResultTask.Result as ViewResult;
            var model = actionResult.Model;

            // assert
            Assert.NotNull(model);
            Assert.IsType<WithdrawalViewModel>(model);
            Assert.Contains("error", actionResult.ViewData.ModelState["Error"].Errors.First().ErrorMessage);
            userManager.Verify(c => c.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
            mockContext.Verify(c => c.Add(It.IsAny<TransactionInfo>()), Times.Never);
            mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            mockContext.Verify(c => c.UserAccounts, Times.Once);
        }

        [Fact(DisplayName = "Withdraw should fail if the withdrawal amount is greater than the current balance")]
        public void Withdraw_Should_FailIfTheWithdrawalAmountIsGreaterThanTheCurrentBalance()
        {
            // arrange
            var userName = "123";
            var password = "password";
            var fullName = "fullName";
            var tuple = CreateTransactionController(new TransactionTestOptions
            {
                Username = userName,
                Password = password,
                FullName = fullName,
                userAccounts = new List<UserAccount>
                {
                    CreateUserAccount(userName, new List<TransactionInfo> {
                        CreateTransaction(100, TransactionType.Credit)
                    })
                },
                IsAuthenticated = true,
                IsModelStateValid = true
            });

            var controller = tuple.Item1;
            var mockContext = tuple.Item2;
            var userManager = tuple.Item3;

            var withdrawal = new WithdrawalViewModel
            {
                Amount = 200
            };

            // act
            var actionResultTask = controller.Withdraw(withdrawal);
            actionResultTask.Wait();
            var actionResult = actionResultTask.Result as ViewResult;
            var model = actionResult.Model;

            // assert
            Assert.NotNull(model);
            Assert.IsType<WithdrawalViewModel>(model);
            Assert.Contains("The account balance is lesser than the withdrawal amount.", actionResult.ViewData.ModelState["Error"].Errors.First().ErrorMessage);
            userManager.Verify(c => c.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Exactly(2));
            mockContext.Verify(c => c.Add(It.IsAny<TransactionInfo>()), Times.Never);
            mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            mockContext.Verify(c => c.UserAccounts, Times.Exactly(2));
        }

        [Fact(DisplayName = "Withdraw should succeed if all the details are correct")]
        public void Withdraw_Should_SucceedIfAllTheDetailsAreCorrect()
        {
            // arrange
            var userName = "123";
            var password = "password";
            var fullName = "fullName";
            var tuple = CreateTransactionController(new TransactionTestOptions
            {
                Username = userName,
                Password = password,
                FullName = fullName,
                userAccounts = new List<UserAccount>
                {
                    CreateUserAccount(userName, new List<TransactionInfo> {
                        CreateTransaction(100, TransactionType.Credit)
                    })
                },
                IsAuthenticated = true,
                IsModelStateValid = true
            });

            var controller = tuple.Item1;
            var mockContext = tuple.Item2;
            var userManager = tuple.Item3;

            var withdrawal = new WithdrawalViewModel
            {
                Amount = 50
            };

            // act
            var actionResultTask = controller.Withdraw(withdrawal);
            actionResultTask.Wait();
            var actionResult = actionResultTask.Result as RedirectToActionResult;

            // assert
            Assert.Equal(actionResult.ActionName, "Index");
            Assert.Equal(actionResult.ControllerName, "Home");
            userManager.Verify(c => c.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Exactly(2));
            mockContext.Verify(c => c.Add(It.Is<TransactionInfo>(t => t.TransactionType == TransactionType.Debit && t.Amount == 50)), Times.Once);
            mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            mockContext.Verify(c => c.UserAccounts, Times.Exactly(2));
        }

        [Fact(DisplayName = "Transfer should return a view result with the user account")]
        public void Transfer_Should_ReturnAViewResultWithTheUserAccount()
        {
            // arrange
            var userName = "123";
            var password = "password";
            var fullName = "fullName";
            var tuple = CreateTransactionController(new TransactionTestOptions
            {
                Username = userName,
                Password = password,
                FullName = fullName,
                userAccounts = new List<UserAccount>
                    {
                        CreateUserAccount(userName, new List<TransactionInfo> {
                            CreateTransaction(100, TransactionType.Credit)
                        })
                    },
                IsAuthenticated = true,
                IsModelStateValid = true
            });

            var controller = tuple.Item1;
            var mockContext = tuple.Item2;
            var userManager = tuple.Item3;

            // act
            var actionResultTask = controller.Transfer();
            actionResultTask.Wait();
            var actionResult = actionResultTask.Result as ViewResult;
            var account = actionResult.ViewData["UserAccount"];

            // assert
            Assert.NotNull(actionResult.ViewData["UserAccount"]);
            Assert.IsType<UserAccount>(actionResult.ViewData["UserAccount"]);

            userManager.Verify(c => c.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
            mockContext.Verify(c => c.UserAccounts, Times.Once);

        }

        [Fact(DisplayName = "Transfer should fail if the details are invalid")]
        public void Transfer_Should_FailIfTheDetailsAreInvalid()
        {
            // arrange
            var userName = "123";
            var password = "password";
            var fullName = "fullName";
            var transferUserName = "456";

            var tuple = CreateTransactionController(new TransactionTestOptions
            {
                Username = userName,
                Password = password,
                FullName = fullName,
                userAccounts = new List<UserAccount>
                {
                    CreateUserAccount(userName, new List<TransactionInfo> {
                        CreateTransaction(100, TransactionType.Credit)
                    }),
                    CreateUserAccount(transferUserName, new List<TransactionInfo> {
                        CreateTransaction(100, TransactionType.Credit)
                    })
                },
                IsAuthenticated = true,
                IsModelStateValid = false
            });

            var controller = tuple.Item1;
            var mockContext = tuple.Item2;
            var userManager = tuple.Item3;

            var transfer = new TransferViewModel
            {
                AccountNumber = transferUserName,
                Amount = 50
            };

            // act
            var actionResultTask = controller.Transfer(transfer);
            actionResultTask.Wait();
            var actionResult = actionResultTask.Result as ViewResult;
            var model = actionResult.Model;

            // assert
            Assert.NotNull(model);
            Assert.IsType<TransferViewModel>(model);
            Assert.Contains("error", actionResult.ViewData.ModelState["Error"].Errors.First().ErrorMessage);
            userManager.Verify(c => c.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Never);
            mockContext.Verify(c => c.UserAccounts, Times.Never);
            mockContext.Verify(c => c.Add(It.IsAny<TransactionInfo>()), Times.Never);
            mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact(DisplayName = "Transfer should fail if the withdrawal amount is greater than the current balance")]
        public void Transfer_Should_FailIfTheTransferAmountIsGreaterThanCurrentBalance()
        {
            // arrange
            var userName = "123";
            var password = "password";
            var fullName = "fullName";
            var transferUserName = "456";

            var tuple = CreateTransactionController(new TransactionTestOptions
            {
                Username = userName,
                Password = password,
                FullName = fullName,
                userAccounts = new List<UserAccount>
                {
                    CreateUserAccount(userName, new List<TransactionInfo> {
                        CreateTransaction(100, TransactionType.Credit)
                    }),
                    CreateUserAccount(transferUserName, new List<TransactionInfo> {
                        CreateTransaction(100, TransactionType.Credit)
                    })
                },
                IsAuthenticated = true,
                IsModelStateValid = true
            });

            var controller = tuple.Item1;
            var mockContext = tuple.Item2;
            var userManager = tuple.Item3;

            var transfer = new TransferViewModel
            {
                AccountNumber = transferUserName,
                Amount = 200
            };

            // act
            var actionResultTask = controller.Transfer(transfer);
            actionResultTask.Wait();
            var actionResult = actionResultTask.Result as ViewResult;
            var model = actionResult.Model;

            // assert
            Assert.NotNull(model);
            Assert.IsType<TransferViewModel>(model);
            Assert.Contains("The account balance is lesser than the transfer amount.", actionResult.ViewData.ModelState["Error"].Errors.First().ErrorMessage);
            userManager.Verify(c => c.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
            mockContext.Verify(c => c.UserAccounts, Times.Once);
            mockContext.Verify(c => c.Add(It.IsAny<TransactionInfo>()), Times.Never);
            mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact(DisplayName = "Transfer should fail if the transferee does not exist")]
        public void Transfer_Should_FailIfTheTransfereeDoesNotExist()
        {
            // arrange
            var userName = "123";
            var password = "password";
            var fullName = "fullName";
            var transferUserName = "456";

            var tuple = CreateTransactionController(new TransactionTestOptions
            {
                Username = userName,
                Password = password,
                FullName = fullName,
                userAccounts = new List<UserAccount>
                {
                    CreateUserAccount(userName, new List<TransactionInfo> {
                        CreateTransaction(100, TransactionType.Credit)
                    })
                },
                IsAuthenticated = true,
                IsModelStateValid = true
            });

            var controller = tuple.Item1;
            var mockContext = tuple.Item2;
            var userManager = tuple.Item3;

            var transfer = new TransferViewModel
            {
                AccountNumber = transferUserName,
                Amount = 50
            };

            // act
            var actionResultTask = controller.Transfer(transfer);
            actionResultTask.Wait();
            var actionResult = actionResultTask.Result as ViewResult;
            var model = actionResult.Model;

            // assert
            Assert.NotNull(model);
            Assert.IsType<TransferViewModel>(model);
            Assert.Contains(String.Format("The account number {0} does not exist.", transferUserName), actionResult.ViewData.ModelState["Error"].Errors.First().ErrorMessage);
            userManager.Verify(c => c.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
            mockContext.Verify(c => c.UserAccounts, Times.Exactly(2));
            mockContext.Verify(c => c.Add(It.IsAny<TransactionInfo>()), Times.Never);
            mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact(DisplayName = "Transfer should fail if the transferee is the same as the current account")]
        public void Transfer_Should_FailIfTheTransfereeIsSameAsCurrentAccount()
        {
            // arrange
            var userName = "123";
            var password = "password";
            var fullName = "fullName";
            var transferUserName = "123";

            var tuple = CreateTransactionController(new TransactionTestOptions
            {
                Username = userName,
                Password = password,
                FullName = fullName,
                userAccounts = new List<UserAccount>
                {
                    CreateUserAccount(userName, new List<TransactionInfo> {
                        CreateTransaction(100, TransactionType.Credit)
                    })
                },
                IsAuthenticated = true,
                IsModelStateValid = true
            });

            var controller = tuple.Item1;
            var mockContext = tuple.Item2;
            var userManager = tuple.Item3;

            var transfer = new TransferViewModel
            {
                AccountNumber = transferUserName,
                Amount = 50
            };

            // act
            var actionResultTask = controller.Transfer(transfer);
            actionResultTask.Wait();
            var actionResult = actionResultTask.Result as ViewResult;
            var model = actionResult.Model;

            // assert
            Assert.NotNull(model);
            Assert.IsType<TransferViewModel>(model);
            Assert.Contains("Unable to transfer to own account.", actionResult.ViewData.ModelState["Error"].Errors.First().ErrorMessage);
            userManager.Verify(c => c.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
            mockContext.Verify(c => c.UserAccounts, Times.Once);
            mockContext.Verify(c => c.Add(It.IsAny<TransactionInfo>()), Times.Never);
            mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }


        [Fact(DisplayName = "Transfer should succeed if all details are correct")]
        public void Transfer_Should_SucceedIfAllDetailsAreCorrect()
        {
            // arrange
            var userName = "123";
            var password = "password";
            var fullName = "fullName";
            var transferUserName = "456";

            var tuple = CreateTransactionController(new TransactionTestOptions
            {
                Username = userName,
                Password = password,
                FullName = fullName,
                userAccounts = new List<UserAccount>
                {
                    CreateUserAccount(userName, new List<TransactionInfo> {
                        CreateTransaction(100, TransactionType.Credit)
                    }),
                    CreateUserAccount(transferUserName, new List<TransactionInfo> {
                        CreateTransaction(100, TransactionType.Credit)
                    })
                },
                IsAuthenticated = true,
                IsModelStateValid = true
            });

            var controller = tuple.Item1;
            var mockContext = tuple.Item2;
            var userManager = tuple.Item3;

            var transfer = new TransferViewModel
            {
                AccountNumber = transferUserName,
                Amount = 50
            };

            // act
            var actionResultTask = controller.Transfer(transfer);
            actionResultTask.Wait();
            var actionResult = actionResultTask.Result as RedirectToActionResult;

            // assert
            Assert.Equal(actionResult.ActionName, "Index");
            Assert.Equal(actionResult.ControllerName, "Home");
            userManager.Verify(c => c.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
            mockContext.Verify(c => c.UserAccounts, Times.Exactly(2));
            mockContext.Verify(c => c.Add(It.IsAny<TransactionInfo>()), Times.Exactly(2));
            mockContext.Verify(c => c.Add(It.Is<TransactionInfo>(t => t.TransactionType == TransactionType.Transfer && t.Amount == 50)), Times.Once);
            mockContext.Verify(c => c.Add(It.Is<TransactionInfo>(t => t.TransactionType == TransactionType.Receive && t.Amount == 50)), Times.Once);
            mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    public class TransactionTestOptions
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public bool IsAuthenticated { get; set; }
        public bool IsModelStateValid { get; set; }
        public List<UserAccount> userAccounts { get; set; }
    }

}
