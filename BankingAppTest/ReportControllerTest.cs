using BankingApp.Controllers;
using BankingApp.Data;
using BankingApp.Enums;
using BankingApp.Infrastructure;
using BankingApp.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace BankingAppTest
{
    public class ReportControllerTest : BaseControllerTest
    {
        public Tuple<ReportController,
            Mock<IBankingContext>,
            Mock<UserManager<BankingIdentityUser>>> CreateReportController(ReportTestOptions options)
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

            var controller = new ReportController(mockContext.Object, userManager.Object);
            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = claimsPrincipal }
            };

            if (!options.IsModelStateValid)
            {
                controller.ModelState.AddModelError("error", "error");
            }

            return new Tuple<ReportController,
                Mock<IBankingContext>,
                Mock<UserManager<BankingIdentityUser>>>(controller, mockContext, userManager);
        }

        [Fact(DisplayName = "TransactionList should return the list of transactions")]
        public void TransactionList_Should_ReturnTheListOfTransactions()
        {
            // arrange
            var userName = "123";
            var password = "password";
            var fullName = "fullName";

            var tuple = CreateReportController(new ReportTestOptions
            {
                Username = userName,
                Password = password,
                FullName = fullName,
                userAccounts = new List<UserAccount>
                {
                    CreateUserAccount(userName, new List<TransactionInfo> {
                        CreateTransaction(100, TransactionType.Credit),
                        CreateTransaction(100, TransactionType.Debit),
                        CreateTransaction(100, TransactionType.Receive),
                        CreateTransaction(100, TransactionType.Transfer)
                    })
                },
                IsAuthenticated = true,
                IsModelStateValid = true
            });

            var controller = tuple.Item1;
            var mockContext = tuple.Item2;
            var userManager = tuple.Item3;

            // act
            var actionResultTask = controller.TransactionList();
            actionResultTask.Wait();
            var actionResult = actionResultTask.Result as ViewResult;
            var model = actionResult.Model as IOrderedEnumerable<TransactionInfo>;

            // assert
            Assert.Equal(actionResult.ViewData["FullName"], fullName);
            Assert.IsType<UserAccount>(actionResult.ViewData["UserAccount"]);
            Assert.Equal(model.ToList().Count, 4);

            userManager.Verify(c => c.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
            mockContext.Verify(c => c.UserAccounts, Times.Once);
        }

    }

    public class ReportTestOptions
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public bool IsAuthenticated { get; set; }
        public bool IsModelStateValid { get; set; }
        public List<UserAccount> userAccounts { get; set; }
    }

}
