﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using WebApp.Models;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Runtime.Internal.Transform;
using System.Text;

namespace WebApp.Data
{
    [DynamoDBTable("ApplicationUser")]
    public class UserStore : IUserStore<ApplicationUser>, IUserEmailStore<ApplicationUser>, IUserPhoneNumberStore<ApplicationUser>,
        IUserTwoFactorStore<ApplicationUser>, IUserPasswordStore<ApplicationUser>, IUserRoleStore<ApplicationUser>
    {
        // private readonly string _connectionString;
        private IAmazonDynamoDB _client;
        private RoleStore _roleStore;
        private DataHelper _helper;

        public UserStore(IAmazonDynamoDB dynamoDBClient)
        {
            //_connectionString = configuration.GetConnectionString("DefaultConnection");
            _client = dynamoDBClient;
            _roleStore = new RoleStore(_client);
            _helper = new DataHelper(_client);
        }

        public async Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            user.Id = await _helper.GetNewUserIdAsync();
            DynamoDBContext context = new DynamoDBContext(_client);
            var userDoc = context.ToDocument<ApplicationUser>(user);
            Table table = Table.LoadTable(_client, "ApplicationUser");
            await table.PutItemAsync(userDoc);

            ApplicationRole appRole = await (_roleStore.FindByIdAsync(1, cancellationToken));
            await AddToRoleAsync(user, appRole.NormalizedRoleName, cancellationToken);

            //var applicationUserRole = new UserToRoles();  this block commented out 2-19-2021
            //applicationUserRole.NormalizedUserName = user.NormalizedUserName;
            //applicationUserRole.RoleId = _dummyPrefix + 1.ToString();
            //var applicationUserRoleDoc = context.ToDocument<UserToRoles>(applicationUserRole);
            //Table table2 = Table.LoadTable(_client, "ApplicationUserRole");
            //await table2.PutItemAsync(applicationUserRoleDoc);

            //DSimilar to this code where you converted Att value dict to a document and then use that document to create a C# object, you should be able to do the reverse
            //user  -->   Document   --> Dict of string, attrbute value

            //var doc = Document.FromAttributeMap(result.Item);
            //DynamoDBContext context = new DynamoDBContext(_client);
            //var typedDoc = context.FromDocument<ApplicationUser>(doc);

            //await _client.PutItemAsync("ApplicationUser",applicationUserAttributes);

            //using (var connection = new SqlConnection(_connectionString))
            //{
            //    await connection.OpenAsync(cancellationToken);
            //    user.Id = await connection.QuerySingleAsync<int>($@"INSERT INTO [ApplicationUser] ([UserName], [NormalizedUserName], [Email],
            //        [NormalizedEmail], [EmailConfirmed], [PasswordHash], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled])
            //        VALUES (@{nameof(ApplicationUser.UserName)}, @{nameof(ApplicationUser.NormalizedUserName)}, @{nameof(ApplicationUser.Email)},
            //        @{nameof(ApplicationUser.NormalizedEmail)}, @{nameof(ApplicationUser.EmailConfirmed)}, @{nameof(ApplicationUser.PasswordHash)},
            //        @{nameof(ApplicationUser.PhoneNumber)}, @{nameof(ApplicationUser.PhoneNumberConfirmed)}, @{nameof(ApplicationUser.TwoFactorEnabled)});
            //        SELECT CAST(SCOPE_IDENTITY() as int)", user);
            //}

            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tableName = "ApplicationUser";
            Dictionary<string, AttributeValue> deleteDict = new Dictionary<string, AttributeValue>();
            deleteDict.Add("NormalizedUserName", new AttributeValue(user.NormalizedUserName.ToLower()));
            await _client.DeleteItemAsync(tableName, deleteDict, cancellationToken);

            //using (var connection = new SqlConnection(_connectionString))
            //{
            //    await connection.OpenAsync(cancellationToken);
            //    await connection.ExecuteAsync($"DELETE FROM [ApplicationUser] WHERE [Id] = @{nameof(ApplicationUser.Id)}", user);
            //}

            return IdentityResult.Success;
        }

        public async Task<ApplicationUser> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ApplicationUser user = await _helper.GetApplicationUserItemByNonKeyAsync("Id", userId);

            return user;

			//using (var connection = new SqlConnection(_connectionString))
			//{
			//    await connection.OpenAsync(cancellationToken);
			//    return await connection.QuerySingleOrDefaultAsync<ApplicationUser>($@"SELECT * FROM [ApplicationUser]
			//        WHERE [Id] = @{nameof(userId)}", new { userId });
			//}
		}

        public async Task<ApplicationUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ApplicationUser user = await _helper.GetApplicationUserItemByKeyAsync(normalizedUserName);

            return user;
		}

        public Task<string> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.NormalizedUserName);
        }

        public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Id.ToString());
        }

        public Task<string> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.UserName);
        }

        public Task SetNormalizedUserNameAsync(ApplicationUser user, string normalizedName, CancellationToken cancellationToken)
        {
            user.NormalizedUserName = normalizedName;
            return Task.FromResult(0);
        }

        public Task SetUserNameAsync(ApplicationUser user, string userName, CancellationToken cancellationToken)
        {
            user.UserName = userName;
            return Task.FromResult(0);
        }

        public async Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ApplicationUser updatedUser = await FindByNameAsync(user.NormalizedUserName, cancellationToken);    //await FindByIdAsync(user.Id.ToString(), cancellationToken);
            if(updatedUser == null)
			{
                return IdentityResult.Failed();
			}

            updatedUser.Id = user.Id;
            updatedUser.UserName = user.UserName;
            updatedUser.NormalizedUserName = user.NormalizedUserName;
            updatedUser.Email = user.Email;
            updatedUser.NormalizedEmail = user.NormalizedEmail;
            updatedUser.EmailConfirmed = user.EmailConfirmed;
            updatedUser.PasswordHash = user.PasswordHash;
            updatedUser.PhoneNumber  = user.PhoneNumber;
            updatedUser.PhoneNumberConfirmed = user.PhoneNumberConfirmed;
            updatedUser.TwoFactorEnabled = user.TwoFactorEnabled;

            DynamoDBContext context = new DynamoDBContext(_client);
            var userDoc = context.ToDocument<ApplicationUser>(updatedUser);
            Table table = Table.LoadTable(_client, "ApplicationUser");
            await table.PutItemAsync(userDoc);

            //using (var connection = new SqlConnection(_connectionString))
            //{
            //    await connection.OpenAsync(cancellationToken);

            //    await connection.ExecuteAsync($@"UPDATE [ApplicationUser] SET
            //        [UserName] = @{nameof(ApplicationUser.UserName)},
            //        [NormalizedUserName] = @{nameof(ApplicationUser.NormalizedUserName)},
            //        [Email] = @{nameof(ApplicationUser.Email)},
            //        [NormalizedEmail] = @{nameof(ApplicationUser.NormalizedEmail)},
            //        [EmailConfirmed] = @{nameof(ApplicationUser.EmailConfirmed)},
            //        [PasswordHash] = @{nameof(ApplicationUser.PasswordHash)},
            //        [PhoneNumber] = @{nameof(ApplicationUser.PhoneNumber)},
            //        [PhoneNumberConfirmed] = @{nameof(ApplicationUser.PhoneNumberConfirmed)},
            //        [TwoFactorEnabled] = @{nameof(ApplicationUser.TwoFactorEnabled)}
            //        WHERE [Id] = @{nameof(ApplicationUser.Id)}", user);
            //}

            return IdentityResult.Success;
        }

        public Task SetEmailAsync(ApplicationUser user, string email, CancellationToken cancellationToken)
        {
            user.Email = email;
            return Task.FromResult(0);
        }

        public Task<string> GetEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Email);
        }

        public Task<bool> GetEmailConfirmedAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.EmailConfirmed);
        }

        public Task SetEmailConfirmedAsync(ApplicationUser user, bool confirmed, CancellationToken cancellationToken)
        {
            user.EmailConfirmed = confirmed;
            return Task.FromResult(0);
        }

        public async Task<ApplicationUser> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ApplicationUser user = await _helper.GetApplicationUserItemByNonKeyAsync("NormalizedEmail", normalizedEmail);

            return user;

            //using (var connection = new SqlConnection(_connectionString))
            //{
            //    await connection.OpenAsync(cancellationToken);
            //    return await connection.QuerySingleOrDefaultAsync<ApplicationUser>($@"SELECT * FROM [ApplicationUser]
            //        WHERE [NormalizedEmail] = @{nameof(normalizedEmail)}", new { normalizedEmail });
            //}
        }

        public Task<string> GetNormalizedEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.NormalizedEmail);
        }

        public Task SetNormalizedEmailAsync(ApplicationUser user, string normalizedEmail, CancellationToken cancellationToken)
        {
            user.NormalizedEmail = normalizedEmail;
            return Task.FromResult(0);
        }

        public Task SetPhoneNumberAsync(ApplicationUser user, string phoneNumber, CancellationToken cancellationToken)
        {
            user.PhoneNumber = phoneNumber;
            return Task.FromResult(0);
        }

        public Task<string> GetPhoneNumberAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.PhoneNumber);
        }

        public Task<bool> GetPhoneNumberConfirmedAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.PhoneNumberConfirmed);
        }

        public Task SetPhoneNumberConfirmedAsync(ApplicationUser user, bool confirmed, CancellationToken cancellationToken)
        {
            user.PhoneNumberConfirmed = confirmed;
            return Task.FromResult(0);
        }

        public Task SetTwoFactorEnabledAsync(ApplicationUser user, bool enabled, CancellationToken cancellationToken)
        {
            user.TwoFactorEnabled = enabled;
            return Task.FromResult(0);
        }

        public Task<bool> GetTwoFactorEnabledAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.TwoFactorEnabled);
        }

        public Task SetPasswordHashAsync(ApplicationUser user, string passwordHash, CancellationToken cancellationToken)
        {
            user.PasswordHash = passwordHash;
            return Task.FromResult(0);
        }

        public Task<string> GetPasswordHashAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.PasswordHash);
        }

        public Task<bool> HasPasswordAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.PasswordHash != null);
        }

        //public async Task<IdentityResult> AddToRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
        public async Task AddToRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var normalizedUserName = user.NormalizedUserName.ToUpper();
            var normalizedRole = roleName.ToUpper();
            //ApplicationRole role = await _roleStore.FindByNameAsync(normalizedRole, cancellationToken);
            bool alreadyHasRole = await IsInRoleAsync(user, normalizedRole, cancellationToken);
            if(!alreadyHasRole)
			{
                ApplicationRole role = await _roleStore.FindByNameAsync(normalizedRole, cancellationToken);
                int roleId = role.RoleId;
                DynamoDBContext context = new DynamoDBContext(_client);
                UserToRoles userRole = new UserToRoles();
                userRole.RoleId = role.RoleId;
                userRole.NormalizedUserName = normalizedUserName;
                var userDoc = context.ToDocument<UserToRoles>(userRole);
                Table table = Table.LoadTable(_client, "UserToRoles");
                await table.PutItemAsync(userDoc);
            }

            //return IdentityResult.Success;

            //using (var connection = new SqlConnection(_connectionString))
            //{
                //var normalizedName = roleName.ToUpper();
                //var roleId = await connection.ExecuteScalarAsync<int?>($"SELECT [Id] FROM [ApplicationRole] WHERE [NormalizedName] = @{nameof(normalizedName)}", new { normalizedName });
                //if (!roleId.HasValue)
                //    roleId = await connection.ExecuteAsync($"INSERT INTO [ApplicationRole]([Name], [NormalizedName]) VALUES(@{nameof(roleName)}, @{nameof(normalizedName)})",
                //        new { roleName, normalizedName });

                //await connection.ExecuteAsync($"IF NOT EXISTS(SELECT 1 FROM [ApplicationUserRole] WHERE [UserId] = @userId AND [RoleId] = @{nameof(roleId)}) " +
                //    $"INSERT INTO [ApplicationUserRole]([UserId], [RoleId]) VALUES(@userId, @{nameof(roleId)})",
                //    new { userId = user.Id, roleId });
                //}
            
        }

        public async Task RemoveFromRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var normalizedRole = roleName.ToUpper();
            ApplicationRole role = await _roleStore.FindByNameAsync(normalizedRole, cancellationToken);
            int roleId = role.RoleId;
            var tableName = "UserToRoles";
            Dictionary<string, AttributeValue> deleteDict = new Dictionary<string, AttributeValue>();
            deleteDict.Add("NormalizedUserName", new AttributeValue(user.NormalizedUserName));
            deleteDict.Add("RoleId", new AttributeValue(roleId.ToString()));
            await _client.DeleteItemAsync(tableName, deleteDict, cancellationToken);

            //using (var connection = new SqlConnection(_connectionString))
            //{
            //    await connection.OpenAsync(cancellationToken);
            //    var roleId = await connection.ExecuteScalarAsync<int?>("SELECT [Id] FROM [ApplicationRole] WHERE [NormalizedName] = @normalizedName", new { normalizedName = roleName.ToUpper() });
            //    if (!roleId.HasValue)
            //        await connection.ExecuteAsync($"DELETE FROM [ApplicationUserRole] WHERE [UserId] = @userId AND [RoleId] = @{nameof(roleId)}", new { userId = user.Id, roleId });
            //}
        }

        public async Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string normalizedUserName = user.NormalizedUserName.ToUpper();

            List<UserToRoles> userRoles = await _helper.GetUserToRolesItemListByNonKeyAsync("NormalizedUserName", normalizedUserName);
            List<int> roleIds = new List<int>();
            foreach(UserToRoles userRole in userRoles)
			{
                roleIds.Add(userRole.RoleId);
			}
            List<string> roleNames = new List<string>();
            foreach(int roleId in roleIds)
			{
                ApplicationRole role = await _helper.GetApplicationRoleItemByKeyAsync(roleId);
                roleNames.Add(role.NormalizedRoleName);
			}

            return roleNames;

			//using (var connection = new SqlConnection(_connectionString))
			//{
			//	await connection.OpenAsync(cancellationToken);
			//	var queryResults = await connection.QueryAsync<string>("SELECT r.[Name] FROM [ApplicationRole] r INNER JOIN [ApplicationUserRole] ur ON ur.[RoleId] = r.Id " +
			//		"WHERE ur.UserId = @userId", new { userId = user.Id });

			//	return queryResults.ToList();
   //         }
        }

        public async Task<bool> IsInRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string normalizedUserName = user.NormalizedUserName.ToUpper();
            string normalizedRole = roleName.ToUpper();

            ApplicationRole role = await _roleStore.FindByNameAsync(normalizedRole, cancellationToken);
            int roleId = role.RoleId;
            bool userHasRole = await _helper.UserHasRoleAsync(normalizedUserName, roleId);
            return userHasRole;


            //using (var connection = new SqlConnection(_connectionString))
            //{
            //    var roleId = await connection.ExecuteScalarAsync<int?>("SELECT [Id] FROM [ApplicationRole] WHERE [NormalizedName] = @normalizedName", new { normalizedName = roleName.ToUpper() });
            //    if (roleId == default(int)) return false;
            //    var matchingRoles = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM [ApplicationUserRole] WHERE [UserId] = @userId AND [RoleId] = @{nameof(roleId)}",
            //        new { userId = user.Id, roleId });

            //    return matchingRoles > 0;
            //}
        }

        public async Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

			string normalizedRole = roleName.ToUpper();
            ApplicationRole role = await _roleStore.FindByNameAsync(normalizedRole, cancellationToken);
            int roleId = role.RoleId;
            List<UserToRoles> userRoles = await _helper.GetUserToRolesItemListByNonKeyAsync("RoleId", roleId);
            List<ApplicationUser> users = new List<ApplicationUser>();
            foreach(UserToRoles userRole in userRoles)
			{
                string userName = userRole.NormalizedUserName;
                ApplicationUser user = await _helper.GetApplicationUserItemByKeyAsync(userName);
                users.Add(user);
			}

            return users;

			//using (var connection = new SqlConnection(_connectionString))
			//{
			//	var queryResults = await connection.QueryAsync<ApplicationUser>("SELECT u.* FROM [ApplicationUser] u " +
			//		"INNER JOIN [ApplicationUserRole] ur ON ur.[UserId] = u.[Id] INNER JOIN [ApplicationRole] r ON r.[Id] = ur.[RoleId] WHERE r.[NormalizedName] = @normalizedName",
			//		new { normalizedName = roleName.ToUpper() });

			//	return queryResults.ToList();
			//}
		}


        public void Dispose()
        {
            // Nothing to dispose.
        }
    }
}
