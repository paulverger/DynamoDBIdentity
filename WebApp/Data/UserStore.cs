using System;
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
        private readonly string _connectionString;
        private IAmazonDynamoDB _client;

        public UserStore(IAmazonDynamoDB dynamoDBClient)
        {
            //_connectionString = configuration.GetConnectionString("DefaultConnection");
            _client = dynamoDBClient;
        }

        public async Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            //Dictionary<string, AttributeValue> applicationUserAttributes =
            //    new Dictionary<string, AttributeValue>();

            DynamoDBContext context = new DynamoDBContext(_client);
            var userDoc = context.ToDocument<ApplicationUser>(user);
            Table table = Table.LoadTable(_client, "ApplicationUser");
            await table.PutItemAsync(userDoc);

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

            throw new NotImplementedException();
            //var tableName = "ApplicationUser";
            //var userIdDict = new Dictionary<string, AttributeValue>();
            
            //userIdDict.Add("Id", new AttributeValue(userId));
            //var result = await _client.GetItemAsync(tableName, userIdDict);

            //if (result.Item.Count == 0)
            //{
            //    return null;
            //}

            //List<ApplicationUser> retrievedUsers = new List<ApplicationUser>();
            //var doc = Document.FromAttributeMap(result.Item);
            //DynamoDBContext context = new DynamoDBContext(_client);
            //var typedDoc = context.FromDocument<ApplicationUser>(doc);
            //retrievedUsers.Add(typedDoc);

            //return retrievedUsers.FirstOrDefault<ApplicationUser>();

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

            var tableName = "ApplicationUser";
            var dict = new Dictionary<string, AttributeValue>();
            dict.Add("NormalizedUserName", new AttributeValue(normalizedUserName.ToLower()));
            var result = await _client.GetItemAsync(tableName, dict );

            if (result.Item.Count == 0)
            {
                return null;
            }

            List<ApplicationUser> retrievedUsers = new List<ApplicationUser>();

            var doc = Document.FromAttributeMap(result.Item);
            DynamoDBContext context = new DynamoDBContext(_client);
            var typedDoc = context.FromDocument<ApplicationUser>(doc);
            retrievedUsers.Add(typedDoc);

            return retrievedUsers.FirstOrDefault<ApplicationUser>();

            //         Dictionary<string, Condition> conditions = new Dictionary<string, Condition>();
            //         Condition condition = new Condition();
            //         condition.ComparisonOperator = ComparisonOperator.EQ;
            //         condition.AttributeValueList.Add(new AttributeValue { S = normalizedUserName.ToLower() });
            //         conditions["NormalizedUserName"] = condition;

            //         ScanRequest request = new ScanRequest
            //         {
            //             TableName = "ApplicationUser",
            //             ScanFilter = conditions
            //         };


            //         ScanResponse result = await _client.ScanAsync(request);


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

            var tableName = "ApplicationUser";
            var dict = new Dictionary<string, AttributeValue>();
            dict.Add("NormalizedEmail", new AttributeValue(normalizedEmail.ToUpper()));
            var result = await _client.GetItemAsync(tableName, dict);

            if (result.Item.Count == 0)
            {
                return null;
            }

            List<ApplicationUser> retrievedUsers = new List<ApplicationUser>();

            var doc = Document.FromAttributeMap(result.Item);
            DynamoDBContext context = new DynamoDBContext(_client);
            var typedDoc = context.FromDocument<ApplicationUser>(doc);
            retrievedUsers.Add(typedDoc);

            return retrievedUsers.FirstOrDefault<ApplicationUser>();


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

        public async Task AddToRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync(cancellationToken);

                var normalizedRole = roleName.ToUpper();
                var roles = new StringBuilder();
                roles.Append(user.Roles.Replace("," + normalizedRole, "") + "," + normalizedRole);
                user.Roles = roles.ToString();

                DynamoDBContext context = new DynamoDBContext(_client);
                var userDoc = context.ToDocument<ApplicationUser>(user);
                Table table = Table.LoadTable(_client, "ApplicationUser");
                await table.PutItemAsync(userDoc);

                //var normalizedName = roleName.ToUpper();
                //var roleId = await connection.ExecuteScalarAsync<int?>($"SELECT [Id] FROM [ApplicationRole] WHERE [NormalizedName] = @{nameof(normalizedName)}", new { normalizedName });
                //if (!roleId.HasValue)
                //    roleId = await connection.ExecuteAsync($"INSERT INTO [ApplicationRole]([Name], [NormalizedName]) VALUES(@{nameof(roleName)}, @{nameof(normalizedName)})",
                //        new { roleName, normalizedName });

                //await connection.ExecuteAsync($"IF NOT EXISTS(SELECT 1 FROM [ApplicationUserRole] WHERE [UserId] = @userId AND [RoleId] = @{nameof(roleId)}) " +
                //    $"INSERT INTO [ApplicationUserRole]([UserId], [RoleId]) VALUES(@userId, @{nameof(roleId)})",
                //    new { userId = user.Id, roleId });
            }
        }

        public async Task RemoveFromRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var normalizedRole = roleName.ToUpper();
            var roles = new StringBuilder();
            roles.Append(user.Roles.Replace("," + normalizedRole, ""));
            user.Roles = roles.ToString();

            DynamoDBContext context = new DynamoDBContext(_client);
            var userDoc = context.ToDocument<ApplicationUser>(user);
            Table table = Table.LoadTable(_client, "ApplicationUser");
            await table.PutItemAsync(userDoc);

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
            //ApplicationUser appUser = await FindByNameAsync(normalizedUserName, cancellationToken);

			using (var connection = new SqlConnection(_connectionString))
			{
				await connection.OpenAsync(cancellationToken);
				var queryResults = await connection.QueryAsync<string>("SELECT r.[Name] FROM [ApplicationRole] r INNER JOIN [ApplicationUserRole] ur ON ur.[RoleId] = r.Id " +
					"WHERE ur.UserId = @userId", new { userId = user.Id });

				return queryResults.ToList();
            }
        }

        public async Task<bool> IsInRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string normalizedUserName = user.NormalizedUserName.ToUpper();
            string normalizedRole = roleName.ToUpper();

            ApplicationUser appuser = await FindByNameAsync(normalizedUserName, cancellationToken);
            return appuser.Roles.Contains(normalizedRole);

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

			//string normalizedRole = roleName.ToUpper();
			//var tableName = "ApplicationUser";
			//var dict = new Dictionary<string, AttributeValue>();
			//dict.Add("Roles", new AttributeValue(normalizedUserName.ToLower()));
			//var result = await _client.GetItemAsync(tableName, dict);

			//if (result.Item.Count == 0)
			//{
			//    return null;
			//}

			//List<ApplicationUser> retrievedUsers = new List<ApplicationUser>();

			//var doc = Document.FromAttributeMap(result.Item);
			//DynamoDBContext context = new DynamoDBContext(_client);
			//var typedDoc = context.FromDocument<ApplicationUser>(doc);
			//retrievedUsers.Add(typedDoc);
			using (var connection = new SqlConnection(_connectionString))
			{
				var queryResults = await connection.QueryAsync<ApplicationUser>("SELECT u.* FROM [ApplicationUser] u " +
					"INNER JOIN [ApplicationUserRole] ur ON ur.[UserId] = u.[Id] INNER JOIN [ApplicationRole] r ON r.[Id] = ur.[RoleId] WHERE r.[NormalizedName] = @normalizedName",
					new { normalizedName = roleName.ToUpper() });

				return queryResults.ToList();
			}
		}

        public void Dispose()
        {
            // Nothing to dispose.
        }
    }
}
