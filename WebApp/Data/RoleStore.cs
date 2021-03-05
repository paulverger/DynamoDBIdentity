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

namespace WebApp.Data
{
    public class RoleStore : IRoleStore<ApplicationRole>
    {
        private readonly IAmazonDynamoDB _client;

        private readonly DataHelper _helper;

        public RoleStore(IAmazonDynamoDB dynamoDBClient)
        {
            _client = dynamoDBClient;
            _helper = new DataHelper(_client);
        }

        public async Task<IdentityResult> CreateAsync(ApplicationRole role, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int maxRoleId = await _helper.GetNewRoleIdAsync();
            DynamoDBContext context = new DynamoDBContext(_client);
            var roleDoc = context.ToDocument<ApplicationRole>(role);
            Table table = Table.LoadTable(_client, "ApplicationRole");
            await table.PutItemAsync(roleDoc);

            return IdentityResult.Success;
        }

        public async Task<IdentityResult> UpdateAsync(ApplicationRole role, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ApplicationRole updatedRole = await FindByNameAsync(role.NormalizedRoleName, cancellationToken);
            if (updatedRole == null)
            {
                return IdentityResult.Failed();
            }

            updatedRole.RoleId = role.RoleId;
            updatedRole.RoleName = role.RoleName;
            updatedRole.NormalizedRoleName = role.NormalizedRoleName;

            DynamoDBContext context = new DynamoDBContext(_client);
            var roleDoc = context.ToDocument<ApplicationRole>(updatedRole);
            Table table = Table.LoadTable(_client, "ApplicationRole");
            await table.PutItemAsync(roleDoc);

            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(ApplicationRole role, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tableName = "ApplicationRole";
            Dictionary<string, AttributeValue> deleteDict = new Dictionary<string, AttributeValue>();
            deleteDict.Add("NormalizedName", new AttributeValue(role.NormalizedRoleName.ToLower()));
            await _client.DeleteItemAsync(tableName, deleteDict, cancellationToken);

            return IdentityResult.Success;
        }

        public Task<string> GetRoleIdAsync(ApplicationRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult(role.RoleId.ToString());
        }

        public Task<string> GetRoleNameAsync(ApplicationRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult(role.RoleName);
        }

        public Task SetRoleNameAsync(ApplicationRole role, string roleName, CancellationToken cancellationToken)
        {
            role.RoleName = roleName;
            return Task.FromResult(0);
        }

        public Task<string> GetNormalizedRoleNameAsync(ApplicationRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult(role.NormalizedRoleName);
        }

        public Task SetNormalizedRoleNameAsync(ApplicationRole role, string normalizedName, CancellationToken cancellationToken)
        {
            role.NormalizedRoleName = normalizedName;
            return Task.FromResult(0);
        }

        public async Task<ApplicationRole> FindByIdAsync(int roleId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ApplicationRole role = await _helper.GetApplicationRoleItemByKeyAsync(roleId);

            return role;
        }

        public async Task<ApplicationRole> FindByIdAsync(string normalizedRoleName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ApplicationRole role = await FindByNameAsync(normalizedRoleName, cancellationToken);

            return role;
        }

        public async Task<ApplicationRole> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ApplicationRole role = await _helper.GetApplicationRoleItemByNonKeyAsync("NormalizedRoleName", normalizedRoleName);

            return role;
        }

        public void Dispose()
        {
            // Nothing to dispose.
        }
    }
}
