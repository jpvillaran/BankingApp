using BankingApp.Models;
using System;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using BankingApp.Infrastructure;
using Moq;
using BankingApp.Controllers;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using BankingApp.Model;
using System.Collections.Generic;
using System.Linq;
using BankingApp.Data;
using BankingAppTest.Internals;
using BankingApp.Enums;

namespace BankingAppTest
{
    public class HomeControllerTest : BaseControllerTest
    {
        [Fact(DisplayName ="Index should return a ViewResult with a UserAccount Model")]
        public void Index_Should_ReturnAValidViewResult()
        {
            // arrange
            var userName = "123";
            var fullName = "fullName";
            var tuple = CreateHomeController(userName, fullName,
                new List<UserAccount>
                {
                    CreateUserAccount(userName, new List<TransactionInfo> {
                        CreateTransaction(100, TransactionType.Credit)
                    })
            });

            var controller = tuple.Item1;
            var mockContext = tuple.Item2;

            // act
            var actionResultTask = controller.Index();
            actionResultTask.Wait();
            var result = actionResultTask.Result as ViewResult;

            // assert
            Assert.NotNull(result);
            Assert.NotNull(result.Model);
            Assert.IsType(typeof(UserAccount), result.Model);
            Assert.Equal((result.Model as UserAccount).AccountNumber, userName);
            Assert.Equal(result.ViewData["FullName"], fullName);
            mockContext.Verify(c => c.UserAccounts, Times.Once);

        }

        [Fact(DisplayName = "Index should return a ViewResult with an Empty Model")]
        public void Index_Should_ReturnAViewResultWithANullModel()
        {
            // arrange
            var userName = "123";
            var fullName = "fullName";
            var tuple = CreateHomeController(userName, fullName,
                new List<UserAccount>
                {
                    CreateUserAccount("111", new List<TransactionInfo> {
                        CreateTransaction(100, TransactionType.Credit)
                    })
                });

            var controller = tuple.Item1;
            var mockContext = tuple.Item2;

            // act
            var actionResultTask = controller.Index();
            actionResultTask.Wait();
            var result = actionResultTask.Result as ViewResult;

            // assert
            Assert.NotNull(result);
            Assert.Null(result.Model);
            Assert.Equal(result.ViewData["FullName"], fullName);
            mockContext.Verify(c => c.UserAccounts, Times.Once);
        }

        private Tuple<HomeController, Mock<IBankingContext>> CreateHomeController(string userName, string fullName, List<UserAccount> userAccounts)
        {
            var user = new BankingIdentityUser
            {
                UserName = userName,
                FullName = fullName
            };

            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                 new Claim(ClaimTypes.Name, userName)
            }));

            var mockContext = new Mock<IBankingContext>();
            mockContext.Setup(m => m.UserAccounts).Returns(FakeDbSet<UserAccount>(userAccounts).Object);

            var controller = new HomeController(mockContext.Object, 
                FakeUserManager(um =>
                {
                    um.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                        .Returns(Task.FromResult(user));
                }).Object);
            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = claimsPrincipal }
            }; ;

            return new Tuple<HomeController, Mock<IBankingContext>>(controller, mockContext);
        }
    }
}
