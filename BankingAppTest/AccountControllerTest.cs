using BankingApp.Controllers;
using BankingApp.Data;
using BankingApp.Enums;
using BankingApp.Infrastructure;
using BankingApp.Model;
using BankingApp.ViewModel;
using BankingAppTest.Internals;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    public class AccountControllerTest : BaseControllerTest
    {
        public Tuple<AccountController, 
            Mock<IBankingContext>, 
            Mock<UserManager<BankingIdentityUser>>,
            Mock<SignInManager<BankingIdentityUser>>,
            Mock<RoleManager<BankingIdentityRole>>> CreateAccountController(AccountTestOptions options)
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
                um.Setup(u => u.CreateAsync(It.IsAny<BankingIdentityUser>(), It.IsAny<string>()))
                    .Returns(
                        options.IsCreateAccountSuccessful 
                            ? Task.FromResult(IdentityResult.Success)
                            : Task.FromResult(IdentityResult.Failed(new IdentityError[] {
                                new IdentityError {
                                    Code = "test",
                                    Description = "test"
                                }
                            })));
                um.Setup(u => u.AddToRoleAsync(It.IsAny<BankingIdentityUser>(), It.IsAny<string>()))
                    .Returns(Task.FromResult(IdentityResult.Success));
            });
            var loginManager = FakeSignInManager(userManager.Object, sim =>
            {
                sim.Setup(s => s.PasswordSignInAsync(options.Username, options.Password, It.IsAny<bool>(), It.IsAny<bool>()))
                    .Returns(Task.FromResult<Microsoft.AspNetCore.Identity.SignInResult>(
                        options.IsSignInSuccessful
                            ? Microsoft.AspNetCore.Identity.SignInResult.Success 
                            : Microsoft.AspNetCore.Identity.SignInResult.Failed));
            });
            var roleManager = FakeRoleManager(rm =>
            {
                rm.Setup(r => r.RoleExistsAsync("NormalUser"))
                    .Returns(Task.FromResult(options.IsRoleExisting));
                rm.Setup(r => r.CreateAsync(It.IsAny<BankingIdentityRole>()))
                    .Returns(Task.FromResult(
                            options.IsRoleCreationSuccessful
                                ? IdentityResult.Success
                                : IdentityResult.Failed(new IdentityError[] {
                                new IdentityError {
                                    Code = "test",
                                    Description = "test"
                                }
                            })));
            });

            var controller = new AccountController(mockContext.Object, userManager.Object, loginManager.Object, roleManager.Object);
            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = claimsPrincipal }
            };

            if (!options.IsModelStateValid)
            {
                controller.ModelState.AddModelError("error", "error");
            }

            return new Tuple<AccountController,
                Mock<IBankingContext>,
                Mock<UserManager<BankingIdentityUser>>,
                Mock<SignInManager<BankingIdentityUser>>,
                Mock<RoleManager<BankingIdentityRole>>>(controller, mockContext, userManager, loginManager, roleManager);
        }


        [Fact(DisplayName = "Login should redirect to Home if authenticated")]
        public void Login_Should_RedirectToHomeIfAuthenticated()
        {
            // arrange
            var userName = "123";
            var password = "password";
            var fullName = "fullName";
            var tuple = CreateAccountController(new AccountTestOptions
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

            // act
            var actionResult = controller.Login() as RedirectToActionResult;

            // assert
            Assert.Equal(actionResult.ActionName, "Index");
            Assert.Equal(actionResult.ControllerName, "Home");
        }

        [Fact(DisplayName = "Login should redirect to the Login page if not yet authenticated")]
        public void Login_Should_RedirectToLoginPageIfNotYetAuthenticated()
        {
            // arrange
            var userName = "123";
            var password = "password";
            var fullName = "fullName";
            var tuple = CreateAccountController(new AccountTestOptions
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
                IsAuthenticated = false
            });

            var controller = tuple.Item1;

            // act
            var actionResult = controller.Login() as ViewResult;

            // assert
            Assert.NotNull(actionResult);
            Assert.Null(actionResult.Model);
        }

        [Fact(DisplayName = "Login should redirect to the Login page the login entries are invalid")]
        public void Login_Should_RedirectToLoginPageIfLoginEntriesAreInvalid()
        {
            // arrange
            var userName = "123";
            var password = "password";
            var fullName = "fullName";
            var tuple = CreateAccountController(new AccountTestOptions
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
                IsAuthenticated = false,
                IsModelStateValid = false
            });

            var controller = tuple.Item1;

            // act
            var vm = new LoginViewModel
            {
                AccountNumber = userName,
                Password = password
            };

            var actionResult = controller.Login(vm) as ViewResult;
            var model = actionResult.Model as LoginViewModel;

            // assert
            Assert.NotNull(actionResult);
            Assert.NotNull(model);
            Assert.IsType<LoginViewModel>(model);
        }

        [Fact(DisplayName = "Login should redirect to Home upon a successful sign-in")]
        public void Login_Should_RedirectToHomeUponASuccessfulSignIn()
        {
            // arrange
            var userName = "123";
            var password = "password";
            var fullName = "fullName";
            var tuple = CreateAccountController(new AccountTestOptions
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
                IsAuthenticated = false,
                IsModelStateValid = true,
                IsSignInSuccessful = true
            });

            var controller = tuple.Item1;
            var loginManager = tuple.Item4;

            var loginViewModel = new LoginViewModel
            {
                AccountNumber = userName,
                Password = password
            };

            // act
            var actionResult = controller.Login(loginViewModel) as RedirectToActionResult;

            // assert
            Assert.Equal(actionResult.ActionName, "Index");
            Assert.Equal(actionResult.ControllerName, "Home");
            loginManager.Verify(c => c.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<String>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact(DisplayName = "Login should redirect to Login page upon an unsuccessful sign-in")]
        public void Login_Should_RedirectToLoginUponAnUnsuccessfulSignIn()
        {
            // arrange
            var userName = "123";
            var password = "password";
            var fullName = "fullName";
            var tuple = CreateAccountController(new AccountTestOptions
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
                IsAuthenticated = false,
                IsModelStateValid = true,
                IsSignInSuccessful = false
            });

            var controller = tuple.Item1;
            var loginManager = tuple.Item4;

            var loginViewModel = new LoginViewModel
            {
                AccountNumber = userName,
                Password = password
            };

            // act
            var actionResult = controller.Login(loginViewModel) as ViewResult;
            var model = actionResult.Model as LoginViewModel;

            // assert
            Assert.NotNull(actionResult);
            Assert.NotNull(model);
            Assert.Equal(model.AccountNumber, userName);
            Assert.Equal(model.Password, password);
            loginManager.Verify(c => c.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<String>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact(DisplayName = "Register should redirect to Home if the user is already authenticated")]
        public void Register_Should_RedirectToHomeIfAlreadyAuthenticated()
        {
            // arrange
            var userName = "123";
            var password = "password";
            var fullName = "fullName";
            var tuple = CreateAccountController(new AccountTestOptions
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
                IsAuthenticated = true
            });

            var controller = tuple.Item1;

            // act
            var actionResult = controller.Register() as RedirectToActionResult;

            // assert
            Assert.Equal(actionResult.ActionName, "Index");
            Assert.Equal(actionResult.ControllerName, "Home");
        }

        [Fact(DisplayName = "Register should Fail if the model details are invalid")]
        public void Register_Should_FailIfModelDetailsAreInvalid()
        {
            // arrange
            var userName = "123";
            var password = "password";
            var fullName = "fullName";
            var tuple = CreateAccountController(new AccountTestOptions
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
                IsAuthenticated = false,
                IsModelStateValid = false
            });

            var controller = tuple.Item1;

            var vm = new RegisterAccountViewModel
            {
                AccountNumber = userName,
                Password = password,
                AccountName = fullName,
                InitialBalance = 1
            };

            // act
            var actionResultTask = controller.Register(vm);
            actionResultTask.Wait();
            var actionResult = actionResultTask.Result as ViewResult;
            var model = actionResult.Model as RegisterAccountViewModel;

            // assert
            Assert.NotNull(actionResult);
            Assert.NotNull(model);
            Assert.IsType<RegisterAccountViewModel>(model);
            Assert.Equal(model.AccountNumber, userName);
            Assert.Equal(model.Password, password);
            Assert.Equal(model.AccountName, fullName);
            Assert.Equal(model.InitialBalance, 1);
        }

        [Fact(DisplayName = "Register should Fail if the account already exists")]
        public void Register_Should_FailIfAccountAlreadyExists()
        {
            // arrange
            var userName = "123";
            var password = "password";
            var fullName = "fullName";
            var tuple = CreateAccountController(new AccountTestOptions
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
                IsAuthenticated = false,
                IsModelStateValid = true
            });

            var controller = tuple.Item1;
            var mockContext = tuple.Item2;

            var vm = new RegisterAccountViewModel
            {
                AccountNumber = userName,
                Password = password,
                AccountName = fullName,
                InitialBalance = 1
            };

            // act
            var actionResultTask = controller.Register(vm);
            actionResultTask.Wait();
            var actionResult = actionResultTask.Result as ViewResult;
            var model = actionResult.Model as RegisterAccountViewModel;

            // assert
            Assert.NotNull(actionResult);
            Assert.NotNull(model);
            Assert.IsType<RegisterAccountViewModel>(model);
            Assert.Equal(actionResult.ViewData.ModelState.Count, 1);
            Assert.NotNull(actionResult.ViewData.ModelState["Error"]);
            Assert.Equal(actionResult.ViewData.ModelState["Error"].Errors.First().ErrorMessage, "The account number specified already exists.");
            Assert.Equal(model.AccountNumber, userName);
            Assert.Equal(model.Password, password);
            Assert.Equal(model.AccountName, fullName);
            Assert.Equal(model.InitialBalance, 1);
            mockContext.Verify(c => c.UserAccounts, Times.Once);
        }

        [Fact(DisplayName = "Register should Fail if the user manager account creation fails")]
        public void Register_Should_FailIfUserManagerAccountCreationFails()
        {
            // arrange
            var userName = "123";
            var password = "password";
            var fullName = "fullName";
            var tuple = CreateAccountController(new AccountTestOptions
            {
                Username = userName,
                Password = password,
                FullName = fullName,
                userAccounts = new List<UserAccount>(),
                IsCreateAccountSuccessful = false,
                IsAuthenticated = false,
                IsModelStateValid = true
            });

            var controller = tuple.Item1;
            var mockContext = tuple.Item2;
            var userManager = tuple.Item3;

            var vm = new RegisterAccountViewModel
            {
                AccountNumber = userName,
                Password = password,
                AccountName = fullName,
                InitialBalance = 1
            };

            // act
            var actionResultTask = controller.Register(vm);
            actionResultTask.Wait();
            var actionResult = actionResultTask.Result as ViewResult;
            var model = actionResult.Model as RegisterAccountViewModel;

            // assert
            Assert.NotNull(actionResult);
            Assert.NotNull(model);
            Assert.IsType<RegisterAccountViewModel>(model);
            Assert.Equal(actionResult.ViewData.ModelState.Count, 1);
            Assert.NotNull(actionResult.ViewData.ModelState["Error"]);
            Assert.Contains("Unable to create the user.", actionResult.ViewData.ModelState["Error"].Errors.First().ErrorMessage);
            Assert.Equal(model.AccountNumber, userName);
            Assert.Equal(model.Password, password);
            Assert.Equal(model.AccountName, fullName);
            Assert.Equal(model.InitialBalance, 1);
            mockContext.Verify(c => c.UserAccounts, Times.Once);
            userManager.Verify(c => c.CreateAsync(It.IsAny<BankingIdentityUser>(), It.IsAny<string>()), Times.Once);
        }

        [Fact(DisplayName = "Register should Fail if the role manager role creation fails")]
        public void Register_Should_FailIfRoleManagerRoleCreationFails()
        {
            // arrange
            var userName = "123";
            var password = "password";
            var fullName = "fullName";
            var tuple = CreateAccountController(new AccountTestOptions
            {
                Username = userName,
                Password = password,
                FullName = fullName,
                userAccounts = new List<UserAccount>(),
                IsCreateAccountSuccessful = true,
                IsAuthenticated = false,
                IsModelStateValid = true,
                IsRoleExisting = false,
                IsRoleCreationSuccessful = false
            });

            var controller = tuple.Item1;
            var mockContext = tuple.Item2;
            var userManager = tuple.Item3;
            var roleManager = tuple.Item5;

            var vm = new RegisterAccountViewModel
            {
                AccountNumber = userName,
                Password = password,
                AccountName = fullName,
                InitialBalance = 1
            };

            // act
            var actionResultTask = controller.Register(vm);
            actionResultTask.Wait();
            var actionResult = actionResultTask.Result as ViewResult;
            var model = actionResult.Model as RegisterAccountViewModel;

            // assert
            Assert.NotNull(actionResult);
            Assert.NotNull(model);
            Assert.IsType<RegisterAccountViewModel>(model);
            Assert.Equal(actionResult.ViewData.ModelState.Count, 1);
            Assert.NotNull(actionResult.ViewData.ModelState[""]);
            Assert.Contains("Error while creating role!", actionResult.ViewData.ModelState[""].Errors.First().ErrorMessage);
            Assert.Equal(model.AccountNumber, userName);
            Assert.Equal(model.Password, password);
            Assert.Equal(model.AccountName, fullName);
            Assert.Equal(model.InitialBalance, 1);
            mockContext.Verify(c => c.UserAccounts, Times.Once);
            userManager.Verify(c => c.CreateAsync(It.IsAny<BankingIdentityUser>(), It.IsAny<string>()), Times.Once);
            roleManager.Verify(c => c.RoleExistsAsync(It.IsAny<string>()), Times.Once);
            roleManager.Verify(c => c.CreateAsync(It.IsAny<BankingIdentityRole>()), Times.Once);
        }

        [Fact(DisplayName = "Register should Succeed if all details are correct, even with an existing role")]
        public void Register_Should_SucceedIfAllDetailsAreCorrect_ExistingRole()
        {
            // arrange
            var userName = "123";
            var password = "password";
            var fullName = "fullName";
            var tuple = CreateAccountController(new AccountTestOptions
            {
                Username = userName,
                Password = password,
                FullName = fullName,
                userAccounts = new List<UserAccount>(),
                IsCreateAccountSuccessful = true,
                IsAuthenticated = false,
                IsModelStateValid = true,
                IsRoleExisting = true,
                IsRoleCreationSuccessful = false
            });

            var controller = tuple.Item1;
            var mockContext = tuple.Item2;
            var userManager = tuple.Item3;
            var roleManager = tuple.Item5;

            var vm = new RegisterAccountViewModel
            {
                AccountNumber = userName,
                Password = password,
                AccountName = fullName,
                InitialBalance = 1
            };

            // act
            var actionResultTask = controller.Register(vm);
            actionResultTask.Wait();
            var actionResult = actionResultTask.Result as RedirectToActionResult;

            // assert
            Assert.NotNull(actionResult);
            Assert.Equal(actionResult.ActionName, "Login");
            Assert.Equal(actionResult.ControllerName, "Account");
            mockContext.Verify(c => c.UserAccounts, Times.Once);
            userManager.Verify(c => c.CreateAsync(It.IsAny<BankingIdentityUser>(), It.IsAny<string>()), Times.Once);
            roleManager.Verify(c => c.RoleExistsAsync(It.IsAny<string>()), Times.Once);
            roleManager.Verify(c => c.CreateAsync(It.IsAny<BankingIdentityRole>()), Times.Never);
            userManager.Verify(c => c.AddToRoleAsync(It.IsAny<BankingIdentityUser>(), It.IsAny<string>()), Times.Once);
            mockContext.Verify(c => c.Add<UserAccount>(It.IsAny<UserAccount>()), Times.Once);
            mockContext.Verify(c => c.Add<TransactionInfo>(It.IsAny<TransactionInfo>()), Times.Once);
            mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact(DisplayName = "Register should Succeed if all details are correct, even with an non-existing role")]
        public void Register_Should_SucceedIfAllDetailsAreCorrect_NonExistingRole()
        {
            // arrange
            var userName = "123";
            var password = "password";
            var fullName = "fullName";
            var tuple = CreateAccountController(new AccountTestOptions
            {
                Username = userName,
                Password = password,
                FullName = fullName,
                userAccounts = new List<UserAccount>(),
                IsCreateAccountSuccessful = true,
                IsAuthenticated = false,
                IsModelStateValid = true,
                IsRoleExisting = false,
                IsRoleCreationSuccessful = true
            });

            var controller = tuple.Item1;
            var mockContext = tuple.Item2;
            var userManager = tuple.Item3;
            var roleManager = tuple.Item5;

            var vm = new RegisterAccountViewModel
            {
                AccountNumber = userName,
                Password = password,
                AccountName = fullName,
                InitialBalance = 1
            };

            // act
            var actionResultTask = controller.Register(vm);
            actionResultTask.Wait();
            var actionResult = actionResultTask.Result as RedirectToActionResult;

            // assert
            Assert.NotNull(actionResult);
            Assert.Equal(actionResult.ActionName, "Login");
            Assert.Equal(actionResult.ControllerName, "Account");
            mockContext.Verify(c => c.UserAccounts, Times.Once);
            userManager.Verify(c => c.CreateAsync(It.IsAny<BankingIdentityUser>(), It.IsAny<string>()), Times.Once);
            roleManager.Verify(c => c.RoleExistsAsync(It.IsAny<string>()), Times.Once);
            roleManager.Verify(c => c.CreateAsync(It.IsAny<BankingIdentityRole>()), Times.Once);
            userManager.Verify(c => c.AddToRoleAsync(It.IsAny<BankingIdentityUser>(), It.IsAny<string>()), Times.Once);
            mockContext.Verify(c => c.Add<UserAccount>(It.IsAny<UserAccount>()), Times.Once);
            mockContext.Verify(c => c.Add<TransactionInfo>(It.IsAny<TransactionInfo>()), Times.Once);
            mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact(DisplayName = "LogOff should Succeed")]
        public void LogOff_Should_Succeed()
        {
            // arrange
            var userName = "123";
            var password = "password";
            var fullName = "fullName";
            var tuple = CreateAccountController(new AccountTestOptions
            {
                Username = userName,
                Password = password,
                FullName = fullName,
                userAccounts = new List<UserAccount>()
            });

            var controller = tuple.Item1;
            var loginManager = tuple.Item4;

            var vm = new RegisterAccountViewModel
            {
                AccountNumber = userName,
                Password = password,
                AccountName = fullName,
                InitialBalance = 1
            };

            // act
            var actionResult = controller.LogOff() as RedirectToActionResult;

            // assert
            Assert.NotNull(actionResult);
            Assert.Equal(actionResult.ActionName, "Login");
            Assert.Equal(actionResult.ControllerName, "Account");
            loginManager.Verify(c => c.SignOutAsync(), Times.Once);
        }

    }


    public class AccountTestOptions
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public bool IsAuthenticated { get; set; }
        public bool IsSignInSuccessful { get; set; }
        public bool IsModelStateValid { get; set; }
        public List<UserAccount> userAccounts { get; set; }
        public bool IsCreateAccountSuccessful { get; set; }
        public bool IsRoleExisting { get; set; }
        public bool IsRoleCreationSuccessful { get; set; }
    }
}
