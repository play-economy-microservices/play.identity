using System;
using System.Collections.Generic;
using AspNetCore.Identity.MongoDbCore.Models;
using MongoDbGenericRepository.Attributes;

namespace Play.Identity.Service.Entities
{
    [CollectionName("Users")]
    public class ApplicationUser : MongoIdentityUser<Guid>
    {
        /// <summary>
        /// Amount of Gil of the user.
        /// </summary>
        public decimal Gil { get; set; }
        
        /// <summary>
        /// Represents unique identifiers for the messages being consumed
        /// </summary>
        public HashSet<Guid> MessageIds { get; set; } = new();
    }
}