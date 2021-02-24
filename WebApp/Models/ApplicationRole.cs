using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.DataModel;


namespace WebApp.Models
{
    [DynamoDBTable("AppplicationRole")]
    public class ApplicationRole
    {
        [DynamoDBHashKey]
        [DynamoDBProperty("RoleId")]
        public int RoleId { get; set; }

        [DynamoDBProperty("RoleName")]
        public string RoleName { get; set; }

        [DynamoDBProperty("NormalizedRoleName")]
        public string NormalizedRoleName { get; set; }
    }
}
