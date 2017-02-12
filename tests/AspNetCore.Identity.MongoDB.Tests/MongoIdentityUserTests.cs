﻿using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AspNetCore.Identity.MongoDB.Models;
using AspNetCore.Identity.MongoDB.Tests.Common;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace AspNetCore.Identity.MongoDB.Tests
{
    public class MongoIdentityUserTests
    {
        [Fact]
        public async Task MongoIdentityUser_CanBeSavedAndRetrieved_WhenItBecomesTheSubclass()
        {
            var username = TestUtils.RandomString(10);
            var countryName = TestUtils.RandomString(10);
            var loginProvider = TestUtils.RandomString(5);
            var providerKey = TestUtils.RandomString(5);
            var displayName = TestUtils.RandomString(5);
            var myCustomThing = TestUtils.RandomString(10);
            var user = new MyIdentityUser(username) { MyCustomThing = myCustomThing };
            user.AddClaim(new Claim(ClaimTypes.Country, countryName));
            user.AddLogin(new MongoUserLogin(new UserLoginInfo(loginProvider, providerKey, displayName)));

            using (var dbProvider = MongoDbServerTestUtils.CreateDatabase())
            {
                var store = new MongoUserStore<MyIdentityUser>(dbProvider.Database);

                // ACT, ASSERT
                var result = await store.CreateAsync(user, CancellationToken.None);
                Assert.True(result.Succeeded);

                // ACT, ASSERT
                var retrievedUser = await store.FindByIdAsync(user.Id, CancellationToken.None);
                Assert.NotNull(retrievedUser);
                Assert.Equal(username, retrievedUser.UserName);
                Assert.Equal(myCustomThing, retrievedUser.MyCustomThing);

                var countryClaim = retrievedUser.Claims.FirstOrDefault(x => x.ClaimType == ClaimTypes.Country);
                Assert.NotNull(countryClaim);
                Assert.Equal(countryName, countryClaim.ClaimValue);

                var retrievedLoginProvider = retrievedUser.Logins.FirstOrDefault(x => x.LoginProvider == loginProvider);
                Assert.NotNull(retrievedLoginProvider);
                Assert.Equal(providerKey, retrievedLoginProvider.ProviderKey);
                Assert.Equal(displayName, retrievedLoginProvider.ProviderDisplayName);
            }
        }

        [Fact]
        public async Task MongoIdentityUser_ShouldSaveAndRetrieveTheFutureOccuranceCorrectly()
        {
            var lockoutEndDate = new DateTime(2017, 2, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(8996910);
            var user = new MongoIdentityUser(TestUtils.RandomString(10));
            user.LockUntil(lockoutEndDate);

            using (var dbProvider = MongoDbServerTestUtils.CreateDatabase())
            {
                var store = new MongoUserStore<MongoIdentityUser>(dbProvider.Database);

                // ACT
                var result = await store.CreateAsync(user, CancellationToken.None);

                // ASSERT
                Assert.True(result.Succeeded);
                var collection = dbProvider.Database.GetCollection<MongoIdentityUser>(Constants.DefaultCollectionName);
                var retrievedUser = await collection.FindByIdAsync(user.Id);
                Assert.Equal(user.LockoutEndDate, retrievedUser.LockoutEndDate);
            }
        }

        public class MyIdentityUser : MongoIdentityUser
        {
            public MyIdentityUser(string userName) : base(userName)
            {
            }

            public string MyCustomThing { get; set; }
        }
    }
}