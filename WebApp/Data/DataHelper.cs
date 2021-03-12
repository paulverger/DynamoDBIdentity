using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApp.Models;

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
            var tableName = "ApplicationUser";

            List<string> attributes = new List<string>();
            attributes.Add("Id");

            var result = await _client.ScanAsync(tableName, attributes);

            List<Dictionary<string, AttributeValue>> items = result.Items;
            int maxId = 0;
            foreach (Dictionary<string, AttributeValue> item in items)
            {
                int idVal = Convert.ToInt32(item["Id"].N);
                if (idVal > maxId)
                {
                    maxId = idVal;
                }
            }

            return maxId + UserIdIncrement;

        }

        public async Task<int> GetNewRoleIdAsync()
        {
            var tableName = "ApplicationRole";
            List<string> attributes = new List<string>();
            attributes.Add("RoleId");

            var result = await _client.ScanAsync(tableName, attributes);


            List<Dictionary<string, AttributeValue>> items = result.Items;
            int maxRoleId = 0;
            foreach (Dictionary<string, AttributeValue> item in items)
            {
                int roleIdVal = Convert.ToInt32(item["RoleId"].N);
                if (roleIdVal > maxRoleId)
                {
                    maxRoleId = roleIdVal;
                }
            }

            return maxRoleId + RoleIdIncrement;

        }

        public async Task<bool> UserHasRoleAsync(string normalizedUserName, int roleId)
        {
            AttributeValue hashKey = new AttributeValue { S = normalizedUserName };

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

            return userRoles.Count > 0;
        }

        public async Task<List<UserToRoles>> GetUserToRolesItemListByNonKeyAsync(string propertyName, Object value)
        {
            var tableName = "UserToRoles";
            Dictionary<string, Condition> dict = new Dictionary<string, Condition>();
            AttributeValue attributeValue = null;
            if (value.GetType() == typeof(string))
            {
                attributeValue = new AttributeValue { S = value.ToString() };
            }
            if (value.GetType() == typeof(int))
            {
                attributeValue = new AttributeValue { N = value.ToString() };
            }

            Condition condition = new Condition
            {
                ComparisonOperator = "EQ",
                AttributeValueList = new List<AttributeValue> { attributeValue }
            };
            dict.Add(propertyName, condition);
            var result = await _client.ScanAsync(tableName, dict);

            List<UserToRoles> userRoles = new List<UserToRoles>();
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
                UserToRoles userRole = new UserToRoles()
                {
                    NormalizedUserName = item["NormalizedUserName"].S,
                    RoleId = roleIdHolder
                };
                userRoles.Add(userRole);
            }

            return userRoles;
        }


        public  async Task<ApplicationUser> GetApplicationUserItemByKeyAsync(string normalizedUserName)
        {
            AttributeValue hashKey = new AttributeValue { S = normalizedUserName };

            Dictionary<string, Condition> keyConditions = new Dictionary<string, Condition>
            {
                {
                    "NormalizedUserName",
                    new Condition
                    {
                        ComparisonOperator = "EQ",
                        AttributeValueList = new List<AttributeValue> { hashKey }
                    }
                }
            };

            List<ApplicationUser> users = new List<ApplicationUser>();

            Dictionary<string, AttributeValue> startKey = null;

            do
            {
                QueryRequest request = new QueryRequest
                {
                    TableName = "ApplicationUser",
                    ExclusiveStartKey = startKey,
                    KeyConditions = keyConditions
                };

                // Issue request
                var result = await _client.QueryAsync(request);

                List<Dictionary<string, AttributeValue>> items = result.Items;
                foreach (Dictionary<string, AttributeValue> item in items)
                {
                    int idHolder = 0;
                    bool emailConfirmedHolder = false;
                    bool phoneNumberConfirmedHolder = false;
                    bool twoFactorEnabledHolder = false;
                    foreach (var keyValuePair in item)
                    {
                        if (keyValuePair.Key == "EmailConfirmed")
                        {
                            emailConfirmedHolder = Convert.ToBoolean(keyValuePair.Value.B);
                        }
                        if (keyValuePair.Key == "Id")
                        {
                            idHolder = Convert.ToInt32(keyValuePair.Value.N);
                        }
                        if (keyValuePair.Key == "PhoneNumberConfirmed")
                        {
                            phoneNumberConfirmedHolder = Convert.ToBoolean(keyValuePair.Value.B);
                        }
                        if (keyValuePair.Key == "TwoFactorEnabled")
                        {
                            twoFactorEnabledHolder = Convert.ToBoolean(keyValuePair.Value.B);
                        }
                    }
                    ApplicationUser user = new ApplicationUser();

                    user.NormalizedUserName = item["NormalizedUserName"].S;
                    user.Email = item["Email"].S;
                    user.EmailConfirmed = emailConfirmedHolder;
                    user.Id = idHolder;
                    user.NormalizedEmail = item["NormalizedEmail"].S;
                    user.PasswordHash = item["PasswordHash"].S;
                    user.PhoneNumberConfirmed = phoneNumberConfirmedHolder;
                    user.TwoFactorEnabled = twoFactorEnabledHolder;
                    user.UserName = item["UserName"].S;
                    if(item.ContainsKey("PhoneNumber"))
					{
                        user.PhoneNumber = item["PhoneNumber"].S;
                    }
                    users.Add(user);
                }
                startKey = result.LastEvaluatedKey;
            } while (startKey != null && startKey.Count > 0);

            if(users.Count > 0)
			{
                return users[0];
			}
            else
			{
                return null;
			}
        }

        public async Task<ApplicationUser> GetApplicationUserItemByNonKeyAsync(string propertyName, object value)
        {
            var tableName = "ApplicationUser";
            AttributeValue attributeValue = null;
            if (value.GetType() == typeof(string))
            {
                attributeValue = new AttributeValue { S = value.ToString() };
            }
            if (value.GetType() == typeof(int))
            {
                attributeValue = new AttributeValue { N = value.ToString() };
            }

            Dictionary<string, Condition> conditionDict = new Dictionary<string, Condition>();
            conditionDict.Add(propertyName, new Condition
            {
                ComparisonOperator = "EQ",
                AttributeValueList = new List<AttributeValue> { attributeValue }
            });

            var result = await _client.ScanAsync(tableName, conditionDict);

            List<ApplicationUser> users = new List<ApplicationUser>();

            List<Dictionary<string, AttributeValue>> items = result.Items;
            foreach (Dictionary<string, AttributeValue> item in items)
            {
                int idHolder = 0;
                bool emailConfirmedHolder = false;
                bool phoneNumberConfirmedHolder = false;
                bool twoFactorEnabledHolder = false;
                foreach (var keyValuePair in item)
                {
                    if (keyValuePair.Key == "EmailConfirmed")
                    {
                        emailConfirmedHolder = Convert.ToBoolean(keyValuePair.Value.B);
                    }
                    if (keyValuePair.Key == "Id")
                    {
                        idHolder = Convert.ToInt32(keyValuePair.Value.N);
                    }
                    if (keyValuePair.Key == "PhoneNumberConfirmed")
                    {
                        phoneNumberConfirmedHolder = Convert.ToBoolean(keyValuePair.Value.B);
                    }
                    if (keyValuePair.Key == "TwoFactorEnabled")
                    {
                        twoFactorEnabledHolder = Convert.ToBoolean(keyValuePair.Value.B);
                    }
                }
                ApplicationUser user = new ApplicationUser();

                user.NormalizedUserName = item["NormalizedUserName"].S;
                user.Email = item["Email"].S;
                user.EmailConfirmed = emailConfirmedHolder;
                user.Id = idHolder;
                user.NormalizedEmail = item["NormalizedEmail"].S;
                user.PasswordHash = item["PasswordHash"].S;
                user.PhoneNumberConfirmed = phoneNumberConfirmedHolder;
                user.TwoFactorEnabled = twoFactorEnabledHolder;
                user.UserName = item["UserName"].S;
                user.PhoneNumber = " ";
                try
                {
                    user.PhoneNumber = item["PhoneNumber"].S;
                }
                catch
                {
                }
                users.Add(user);

            }
            if(users.Count > 0)
			{
                return users[0];
            }
            else
			{
                return null;
			}
        }

        public async Task<ApplicationRole> GetApplicationRoleItemByKeyAsync(int roleId)
        {
            var tableName = "ApplicationRole";
            AttributeValue attributeValue = new AttributeValue { N = roleId.ToString() };

            Dictionary<string, Condition> conditionDict = new Dictionary<string, Condition>();
            conditionDict.Add("RoleId", new Condition
            {
                ComparisonOperator = "EQ",
                AttributeValueList = new List<AttributeValue> { attributeValue }
            });

            List<ApplicationRole> roles = new List<ApplicationRole>();
            Dictionary<string, AttributeValue> startKey = null;

            do
            {
                QueryRequest request = new QueryRequest
                {
                    TableName = tableName,
                    ExclusiveStartKey = startKey,
                    KeyConditions = conditionDict
                };

                var result = await _client.QueryAsync(request);

                List<Dictionary<string, AttributeValue>> items = result.Items;
                foreach (Dictionary<string, AttributeValue> item in items)
                {
                    int roleIdHolder = 0; ;
                    foreach (var keyValuePair in item)
                    {
                        if (keyValuePair.Key == "RoleId")
                        {
                            roleIdHolder = Convert.ToInt32(keyValuePair.Value.N);
                        }
                    }
                    ApplicationRole role = new ApplicationRole();
                    role.RoleId = roleIdHolder;
                    role.RoleName = item["RoleName"].S;
                    role.NormalizedRoleName = item["NormalizedRoleName"].S;
                    roles.Add(role);
                }

                startKey = result.LastEvaluatedKey;
            } while (startKey != null && startKey.Count > 0);

            if(roles.Count > 0)
			{
                return roles[0];
            }
            else
			{
                return null;
			}
        }

        public async Task<ApplicationRole> GetApplicationRoleItemByNonKeyAsync(string propertyName, object value)
        {
            var tableName = "ApplicationRole";
            AttributeValue attributeValue = null;
            if (value.GetType() == typeof(string))
            {
                attributeValue = new AttributeValue { S = value.ToString() };
            }
            if (value.GetType() == typeof(int))
            {
                attributeValue = new AttributeValue { N = value.ToString() };
            }

            Dictionary<string, Condition> conditionDict = new Dictionary<string, Condition>();
            conditionDict.Add(propertyName, new Condition
            {
                ComparisonOperator = "EQ",
                AttributeValueList = new List<AttributeValue> { attributeValue }
            });

            List<ApplicationRole> roles = new List<ApplicationRole>();

            var result = await _client.ScanAsync(tableName, conditionDict);

            List<Dictionary<string, AttributeValue>> items = result.Items;
            foreach (Dictionary<string, AttributeValue> item in items)
            {
                int roleIdHolder = 0; ;
                foreach (var keyValuePair in item)
                {
                    if (keyValuePair.Key == "RoleId")
                    {
                        roleIdHolder = Convert.ToInt32(keyValuePair.Value.N);
                    }
                }
                ApplicationRole role = new ApplicationRole();
                role.RoleId = roleIdHolder;
                role.RoleName = item["RoleName"].S;
                role.NormalizedRoleName = item["NormalizedRoleName"].S;
                roles.Add(role);
            }

            return roles[0];

        }

    }

}
