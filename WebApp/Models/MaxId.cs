using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2.DocumentModel;

namespace WebApp.Models
{
	[DynamoDBTable("MaxId")]
	public class MaxId
	{
		[DynamoDBHashKey]
		[DynamoDBProperty("IdType")]
		public string IdType { get; set; }

		[DynamoDBProperty("Id")]
		public int Id { get; set; }
	}
}
