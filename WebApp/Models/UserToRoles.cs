using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.DataModel;

namespace WebApp.Models
{
	[DynamoDBTable("UserToRoles")]
	public class UserToRoles
	{
		[DynamoDBRangeKey]
		[DynamoDBProperty("RoleId")]
		public int RoleId { get; set; }
		[DynamoDBHashKey]
		[DynamoDBProperty("NormalizedUserName")]
		public string NormalizedUserName { get; set; }
	}
}
