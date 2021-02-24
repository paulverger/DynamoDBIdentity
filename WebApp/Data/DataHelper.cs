using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using WebApp.Models;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2.DocumentModel;

namespace WebApp.Data
{
    public class DataHelper
    {
        private IAmazonDynamoDB _client;

        private const int UserIdIncrement = 5;
        private const int RoleIdIncrement = 1;

        public DataHelper(IAmazonDynamoDB client)
        {
            _client = client;
        }

        public async Task<int> GetNewUserIdAsync()
        {
            var tableName = "MaxId";
            var userIdDict = new Dictionary<string, AttributeValue>();

            userIdDict.Add("IdType", new AttributeValue("UserId"));
            var result = await _client.GetItemAsync(tableName, userIdDict);

            if (result.Item.Count == 0)
            {
                return 0;
            }

            List<MaxId> ids = new List<MaxId>();
            var doc = Document.FromAttributeMap(result.Item);
            DynamoDBContext context = new DynamoDBContext(_client);
            var typedDoc = context.FromDocument<MaxId>(doc);
            ids.Add(typedDoc);

            var maxUserIdEntry = ids.FirstOrDefault<MaxId>();
            int maxId = maxUserIdEntry.Id;
            int newMaxId = maxId + UserIdIncrement;

            MaxId updatedMaxIdItem = new MaxId();
            updatedMaxIdItem.IdType = "UserId";
            updatedMaxIdItem.Id = newMaxId;
            var userDoc = context.ToDocument<MaxId>(updatedMaxIdItem);
            Table table = Table.LoadTable(_client, "MaxId");
            await table.PutItemAsync(userDoc);

            return newMaxId;

            //List<ApplicationUser> retrievedUsers = await ReadUserTable();

            //int maxId = 0;
            //foreach (ApplicationUser user in retrievedUsers)
            //{
            //    if (user.Id > maxId)
            //    {
            //        maxId = user.Id;
            //    }
            //}

            // return maxId;
        }

        public async Task<int> GetNewRoleIdAsync()
        {
            var tableName = "MaxId";
            var roleIdDict = new Dictionary<string, AttributeValue>();

            roleIdDict.Add("IdType", new AttributeValue("RoleIdId"));
            var result = await _client.GetItemAsync(tableName, roleIdDict);

            if (result.Item.Count == 0)
            {
                return 0;
            }

            List<MaxId> ids = new List<MaxId>();
            var doc = Document.FromAttributeMap(result.Item);
            DynamoDBContext context = new DynamoDBContext(_client);
            var typedDoc = context.FromDocument<MaxId>(doc);
            ids.Add(typedDoc);

            var maxRoleIdEntry = ids.FirstOrDefault<MaxId>();
            int maxId = maxRoleIdEntry.Id;
            int newMaxId = maxId + RoleIdIncrement;

            MaxId updatedMaxIdItem = new MaxId();
            updatedMaxIdItem.IdType = "RoleId";
            updatedMaxIdItem.Id = newMaxId;
            var userDoc = context.ToDocument<MaxId>(updatedMaxIdItem);
            Table table = Table.LoadTable(_client, "MaxId");
            await table.PutItemAsync(userDoc);

            return newMaxId;

         }

            public async Task<int> GetMaxRoleIdAsync()
             {
            List<ApplicationRole> retrievedRoles = await ReadRoleTable();

            int maxId = 0;
            foreach (ApplicationRole role in retrievedRoles)
            {
                if (role.RoleId > maxId)
                {
                    maxId = role.RoleId;
                }
            }

            return maxId;
        }

        public async Task<UserToRoles> GetUserToRolesByKeyAsync(string normalizedUserName, int roleId)
        {
            AttributeValue hashKey = new AttributeValue { S = "DAENERYS.TARGARYEN@DRAGON.COM" };

            Condition condition = new Condition
            {
                ComparisonOperator = "EQ",
                AttributeValueList = new List<AttributeValue>
                {
                    new AttributeValue { N = roleId.ToString() }
                }
            };

            Dictionary<string, Condition> keyConditions = new Dictionary<string, Condition>
            {
                {
                    "NormalizedUserName",
                    new Condition
                    {
                        ComparisonOperator = "EQ",
                        AttributeValueList = new List<AttributeValue> { hashKey }
                    }
                },
                    {
                        "RoleId",
                        condition
                    }
            };

            List<UserToRoles> userRoles = new List<UserToRoles>();
            Dictionary<string, AttributeValue> startKey = null;
            do
            {
                QueryRequest request = new QueryRequest
                {
                    TableName = "UserToRoles",
                    ExclusiveStartKey = startKey,
                    KeyConditions = keyConditions
                };

                var result = await _client.QueryAsync(request);

                List<Dictionary<string, AttributeValue>> items = result.Items;
                foreach (Dictionary<string, AttributeValue> item in items)
                {
                    int roleIdHolder = 0;
                    foreach (var keyValuePair in item)
                    {
                        if (keyValuePair.Key == "RoleId")
                        {
                            roleIdHolder = Convert.ToInt32(keyValuePair.Value.N);
                        }
                    }
                    UserToRoles userRole = new UserToRoles();
                    userRole.NormalizedUserName = item["NormalizedUserName"].S;
                    userRole.RoleId = roleIdHolder;
                    userRoles.Add(userRole);
                }

                startKey = result.LastEvaluatedKey;
            } while (startKey != null && startKey.Count > 0);

            return userRoles[0];
        }

            //public async Task<Dictionary<int, string>> GetRoleDictionary()
            //{
            //    List<ApplicationRole> roles = await ReadRoleTable();

            //    Dictionary<int, string> roleDict = new Dictionary<int, string>();
            //    foreach (ApplicationRole appRole in roles)
            //    {
            //        roleDict.Add(appRole.RoleId, appRole.NormalizedRoleName);
            //    }

            //    return roleDict;
            //}

            public async Task<Dictionary<string, int>> GetRoleToRoleIdDictionary()
        {
            List<ApplicationRole> roles = await ReadRoleTable();

            Dictionary<string, int> roleToRoleIdDict = new Dictionary<string, int>();
            foreach (ApplicationRole appRole in roles)
            {
                roleToRoleIdDict.Add(appRole.NormalizedRoleName, appRole.RoleId);
            }

            return roleToRoleIdDict;
        }

   //     public async Task<Dictionary<string, ApplicationUser>> GetUserDictionary()
   //     {
   //         Dictionary<string, ApplicationUser> userDict = new Dictionary<string, ApplicationUser>();
   //         List<ApplicationUser> users = await ReadUserTable();
   //         foreach(ApplicationUser appuser in users)
			//{
   //             userDict.Add(appuser.NormalizedUserName, appuser);
			//}

   //         return userDict;
   //     }


        //public async Task<ApplicationRole> FindRoleByNameAsync(string normalizedRoleName, System.Threading.CancellationToken cancellationToken)
        //{
        //    cancellationToken.ThrowIfCancellationRequested();

        //    var tableName = "ApplicationRole";
        //    var dict = new Dictionary<string, AttributeValue>();
        //    dict.Add("NormalizedName", new AttributeValue(normalizedRoleName));
        //    var result = await _client.GetItemAsync(tableName, dict);

        //    if (result.Item.Count == 0)
        //    {
        //        return null;
        //    }

        //    List<ApplicationRole> retrievedRoles = new List<ApplicationRole>();

        //    var doc = Document.FromAttributeMap(result.Item);
        //    DynamoDBContext context = new DynamoDBContext(_client);
        //    var typedDoc = context.FromDocument<ApplicationRole>(doc);
        //    retrievedRoles.Add(typedDoc);

        //    return retrievedRoles.FirstOrDefault<ApplicationRole>();

        //    //using (var connection = new SqlConnection(_connectionString))
        //    //{
        //    //    await connection.OpenAsync(cancellationToken);
        //    //    return await connection.QuerySingleOrDefaultAsync<ApplicationRole>($@"SELECT * FROM [ApplicationRole]
        //    //        WHERE [NormalizedName] = @{nameof(normalizedRoleName)}", new { normalizedRoleName });
        //    //}
        //}

   //     public async Task<List<ApplicationUser>> ReadUserTable()
   //     {
			//var tableName = "ApplicationUser";
			//var dict = new Dictionary<string, AttributeValue>();
			//var result = await _client.GetItemAsync(tableName, dict);

			//List<ApplicationUser> users = new List<ApplicationUser>();

			//var doc = Document.FromAttributeMap(result.Item);
			//DynamoDBContext context = new DynamoDBContext(_client);
			//var typedDoc = context.FromDocument<ApplicationUser>(doc);
			//users.Add(typedDoc);

			//return users;
   //     }
        public async Task<List<ApplicationRole>> ReadRoleTable()
        {
            var tableName = "ApplicationRole";
            var dict = new Dictionary<string, AttributeValue>();
            var result = await _client.GetItemAsync(tableName, dict);

            List<ApplicationRole> roles = new List<ApplicationRole>();

            var doc = Document.FromAttributeMap(result.Item);
            DynamoDBContext context = new DynamoDBContext(_client);
            var typedDoc = context.FromDocument<ApplicationRole>(doc);
            roles.Add(typedDoc);

            return roles;
        }
    }

}
