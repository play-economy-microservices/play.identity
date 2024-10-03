using System;
using Azure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Play.Identity.Service;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, configurationBuilder) =>
            {
                // Only use Key vault in production and use default
                // Azure credentials available for the environment to authenticate
                if (context.HostingEnvironment.IsProduction())
                {
                    configurationBuilder.AddAzureKeyVault(
                        new Uri("https://playeconomykeyvault.vault.azure.net/"),
                        new DefaultAzureCredential());
                }
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}
