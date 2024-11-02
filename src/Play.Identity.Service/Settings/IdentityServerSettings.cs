using System;
using System.Collections.Generic;
using Duende.IdentityServer.Models;

namespace Play.Identity.Service.Settings
{
    /// <summary>
    /// Specify neccessary IdentityServer settings in this class. Ensure that these settings
    /// are configure in appsettings.Development.json or appsettings.json
    /// </summary>
    public class IdentityServerSettings
    {
        /// <summary>
        /// Collection of ApiScopes that will be used for Identity Server.
        /// </summary>
        public IReadOnlyCollection<ApiScope> ApiScopes { get; init; }
        
        /// <summary>
        /// Coolection of ApiResources
        /// </summary>
        public IReadOnlyCollection<ApiResource> ApiResources { get; init; }
        
        /// <summary>
        /// Collection of clients.
        /// </summary>
        public IReadOnlyCollection<Client> Clients { get; init; }
        
        /// <summary>
        /// Specify other types of scopes for Identity Server.
        /// </summary>
        public IReadOnlyCollection<IdentityResource> IdentityResources =>
            new IdentityResource[]
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResource("roles", new[]{"role"})
            };
    }
}