using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.DataModel;

namespace WebApp.Models
{
    [DynamoDBTable("ApplicationUser")]
    public class ApplicationUser
    {
        //[DynamoDBProperty("Id")]
        //public int Id { get; set; }
        [DynamoDBProperty("UserName")]
        public string UserName { get; set; }

        [DynamoDBHashKey]
       [DynamoDBProperty("NormalizedUserName")]
        public string NormalizedUserName { get; set; }
        [DynamoDBProperty("Email")]
        public string Email { get; set; }

        [DynamoDBProperty("NormalizedEmail")]
        public string NormalizedEmail { get; set; }

        [DynamoDBProperty("EmailConfirmed")]
        public bool EmailConfirmed { get; set; }

        [DynamoDBProperty("PasswordHash")]
        public string PasswordHash { get; set; }

        [DynamoDBProperty("PhoneNumber")]
        public string PhoneNumber { get; set; }

        [DynamoDBProperty("PhoneNumberConfirmed")]
        public bool PhoneNumberConfirmed { get; set; }

        [DynamoDBProperty("TwoFactorEnabled")]
        public bool TwoFactorEnabled { get; set; }
        
    }
}
