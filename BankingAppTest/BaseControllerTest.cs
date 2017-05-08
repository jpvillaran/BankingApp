using BankingApp.Enums;
using BankingApp.Infrastructure;
using BankingApp.Model;
using BankingAppTest.Internals;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace BankingAppTest
{
    public abstract class BaseControllerTest
    {
        protected Mock<UserManager<BankingIdentityUser>> FakeUserManager(
            Action<Mock<UserManager<BankingIdentityUser>>> setupUserManager) 
        {
            var userManager = new Mock<UserManager<BankingIdentityUser>>(new object[] {
                new Mock<IUserStore<BankingIdentityUser>>().Object,
                new Mock<IOptions<IdentityOptions>>().Object,
                new Mock<IPasswordHasher<BankingIdentityUser>>().Object,
                new IUserValidator<BankingIdentityUser>[0],
                new IPasswordValidator<BankingIdentityUser>[0],
                new Mock<ILookupNormalizer>().Object,
                new Mock<IdentityErrorDescriber>().Object,
                new Mock<IServiceProvider>().Object,
                new Mock<ILogger<UserManager<BankingIdentityUser>>>().Object
            });

            setupUserManager(userManager);
            return userManager;
        }

        protected Mock<SignInManager<BankingIdentityUser>> FakeSignInManager(
            UserManager<BankingIdentityUser> userManager,
            Action<Mock<SignInManager<BankingIdentityUser>>> setupSignInManager)
        {
            var signInManager = new Mock<SignInManager<BankingIdentityUser>>(new object[] {
                userManager,
                new Mock<IHttpContextAccessor>().Object,
                new Mock<IUserClaimsPrincipalFactory<BankingIdentityUser>>().Object,
                new Mock<IOptions<IdentityOptions>>().Object,
                new Mock<ILogger<SignInManager<BankingIdentityUser>>>().Object
            });

            setupSignInManager(signInManager);
            return signInManager;
        }

        protected Mock<RoleManager<BankingIdentityRole>> FakeRoleManager(
            Action<Mock<RoleManager<BankingIdentityRole>>> setupRoleManager)
        {
            var roleManager = new Mock<RoleManager<BankingIdentityRole>>(new object[] {
                new Mock<IRoleStore<BankingIdentityRole>>().Object,
                new IRoleValidator<BankingIdentityRole>[0],
                new Mock<ILookupNormalizer>().Object,
                new Mock<IdentityErrorDescriber>().Object,
                new Mock<ILogger<RoleManager<BankingIdentityRole>>>().Object,
                new Mock<IHttpContextAccessor>().Object
            });
            setupRoleManager(roleManager);
            return roleManager;
        }

        protected Mock<DbSet<TEntity>> FakeDbSet<TEntity>(List<TEntity> mockData) 
            where TEntity : class
        {
            var data = mockData.AsQueryable();
            var mockDbSet = new Mock<DbSet<TEntity>>();
            mockDbSet.As<IAsyncEnumerable<TEntity>>()
                .Setup(m => m.GetEnumerator())
                .Returns(new TestAsyncEnumerator<TEntity>(data.GetEnumerator()));
            mockDbSet.As<IQueryable<TEntity>>()
                .Setup(m => m.Provider)
                .Returns(new TestAsyncQueryProvider<TEntity>(data.Provider));
            mockDbSet.As<IQueryable<TEntity>>()
                .Setup(m => m.Expression)
                .Returns(data.Expression);
            mockDbSet.As<IQueryable<TEntity>>()
                .Setup(m => m.ElementType)
                .Returns(data.ElementType);
            mockDbSet.As<IQueryable<TEntity>>()
                .Setup(m => m.GetEnumerator())
                .Returns(() => data.GetEnumerator());

            return mockDbSet;
        }

        protected UserAccount CreateUserAccount(string accountNumber, List<TransactionInfo> transactions)
        {
            var id = Guid.NewGuid();
            transactions.ForEach(t =>
            {
                t.UserAccountId = id;
            });
            return new UserAccount
            {
                Id = id,
                AccountNumber = accountNumber,
                Transactions = transactions
            };
        }

        protected TransactionInfo CreateTransaction(double amount, TransactionType type)
        {
            return new TransactionInfo
            {
                Id = Guid.NewGuid(),
                Amount = amount,
                TransactionType = type,
                TransactionDate = DateTime.Now
            };
        }

    }
}
