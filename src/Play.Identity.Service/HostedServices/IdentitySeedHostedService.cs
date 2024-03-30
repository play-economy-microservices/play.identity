using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Play.Identity.Service.Entities;
using Play.Identity.Service.Settings;

namespace Play.Identity.Service.HostedServices;

/// <summary>
/// This class will server for running background tasks regarding for IdentityService.
/// </summary>
public class IdentitySeedHostedService : IHostedService
{
    /// <summary>
    /// Used to retrieve an instance of IServiceProvider to get register dependencies.
    /// </summary>
    private readonly IServiceScopeFactory serviceScopeFactory;

    /// <summary>
    /// Settimgs used for any admin roles.
    /// </summary>
    private readonly IdentitySettings settings;

    public IdentitySeedHostedService(IServiceScopeFactory serviceScopeFactory, IOptions<IdentitySettings> identityOptions)
    {
        this.serviceScopeFactory = serviceScopeFactory;
        settings = identityOptions.Value;
    }

    // This will create a newly service scope factory to be able to get access to IServiceProvider where we can get registered
    // dependencies, These dependencies are necessary to make async creation of roles. We will also add any admins with a role of admin.
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();

        // Get the instnace of the RoleManager that was registed in statup.
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Do some background processing with the RoleManager dependency
        await CreateRoleIfNotExistAsync(Roles.Admin, roleManager);
        await CreateRoleIfNotExistAsync(Roles.Player, roleManager);

        var adminUser = await userManager.FindByEmailAsync(settings.AdminUserEmail);

        if (adminUser is null)
        {
            adminUser = new ApplicationUser()
            {
                UserName = settings.AdminUserEmail,
                Email = settings.AdminUserEmail
            };

            await userManager.CreateAsync(adminUser, settings.AdminUserPassword);
            await userManager.AddToRoleAsync(adminUser, Roles.Admin);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task CreateRoleIfNotExistAsync(string role, RoleManager<ApplicationRole> roleManager)
    {
        var roleExists = await roleManager.RoleExistsAsync(role);

        if (!roleExists)
        {
            await roleManager.CreateAsync(new ApplicationRole() { Name = role });
        }
    }
}
