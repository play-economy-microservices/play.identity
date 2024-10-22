using System;
using System.IO;
using System.Reflection;
using GreenPipes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Play.Common.HealthChecks;
using Play.Common.MassTransit;
using Play.Common.Settings;
using Play.Identity.Service.Consumers;
using Play.Identity.Service.Entities;
using Play.Identity.Service.HostedServices;
using Play.Identity.Service.Settings;
using MongoDbSettings = Play.Common.Settings.MongoDbSettings;


namespace Play.Identity.Service;

public class Startup
{
    private const string AllowedOriginSetting = "AllowedOrigin";

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        // Manual Configurations - Keep original types when inserting MongoDB Docouments
        BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));

        // Bindings
        var serviceSettings = Configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();
        var mongoDbSettings = Configuration.GetSection(nameof(MongoDbSettings)).Get<MongoDbSettings>();
        var identityServiceSettings = Configuration.GetSection(nameof(IdentityServerSettings)).Get<IdentityServerSettings>();

        // Default Configurations for Identity Mongo DB
        // Note: You're able to retrieve IdentitySettings from the runtime.
        services
            .Configure<IdentitySettings>(Configuration.GetSection(nameof(IdentitySettings))) // for IOptions<IdentitySettings>
            .AddDefaultIdentity<ApplicationUser>()
            .AddRoles<ApplicationRole>() // registers managers dependencies
            .AddMongoDbStores<ApplicationUser, ApplicationRole, Guid>
            (
                mongoDbSettings.ConnectionString,
                serviceSettings.ServiceName
            );

        // Register Rabbit MQ to consume messages
        services.AddMassTransitWithMessageBroker(Configuration, retryConfigurator =>
        {
            retryConfigurator.Interval(3, TimeSpan.FromSeconds(5));
            retryConfigurator.Ignore(typeof(UnknownUserException));
            retryConfigurator.Ignore(typeof(InsufficientFundsException));
        });

        // Default Configurations for Identity Service
        services
            .AddIdentityServer(options =>
            {
                // For logging purposes
                options.Events.RaiseSuccessEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseErrorEvents = true;
                // explicitly define the location where IdentityServer can store and retrieve cryptographic keys used for token protection
                options.KeyManagement.KeyPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            })
            .AddAspNetIdentity<ApplicationUser>()
            .AddInMemoryApiScopes(identityServiceSettings.ApiScopes)
            .AddInMemoryApiResources(identityServiceSettings.ApiResources)
            .AddInMemoryClients(identityServiceSettings.Clients)
            .AddInMemoryIdentityResources(identityServiceSettings.identityResources);

        // Add this line to secure Api with built in Policy
        services.AddLocalApiAuthentication();

        services.AddControllers();

        // Register HostedServices here
        services.AddHostedService<IdentitySeedHostedService>();

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Play.Identity.Service", Version = "v1" });
        });

        // Health Checks
        services
            .AddHealthChecks()
            .AddMongoDB(); // Health Check for Mongo Db
        
        // Configure headers to access gateway
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseForwardedHeaders(); // for ForwardedHeadersOptions
        
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Play.Identity.Service v1"));

            // Register CORS for the frontend.
            app.UseCors(builder =>
            {
                builder.WithOrigins(Configuration[AllowedOriginSetting])
                .AllowAnyHeader()
                .AllowAnyMethod();
            });
        }

        app.UseHttpsRedirection();

        // Configure middleware for service Path Base use by the Emissary Ingress Gateway
        app.Use((context, next) =>
        {
            var identitySettings = Configuration.GetSection(nameof(IdentitySettings)).Get<IdentitySettings>();

            context.Request.PathBase = new PathString(identitySettings.PathBase);
            return next();
        });
        
        app.UseStaticFiles();

        app.UseRouting();

        // Add this after UseRouting()
        app.UseIdentityServer();

        app.UseAuthorization();

        app.UseCookiePolicy(new CookiePolicyOptions
        {
            MinimumSameSitePolicy = SameSiteMode.Lax
        });

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapRazorPages();
            
            // Map endpoints for Healthchecks
            endpoints.MapPlayEconomyHealthChecks();
        });
    }
}
