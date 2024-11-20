using System;
using System.Security.Cryptography.X509Certificates;
using MassTransit;
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
using Play.Common.Logging;
using Play.Common.MassTransit;
using Play.Common.Settings;
using Play.Identity.Service.Entities;
using Play.Identity.Service.Exceptions;
using Play.Identity.Service.HostedServices;
using Play.Identity.Service.Settings;

namespace Play.Identity.Service
{
    public class Startup
    {
        private const string AllowedOriginSetting = "AllowedOrigin";
        
        private readonly IHostEnvironment environment;

        public Startup(IConfiguration configuration, IHostEnvironment environment)
        {
            Configuration = configuration;
            this.environment = environment;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Manual Configurations - Keep original types when inserting MongoDB Documents
            BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));
            
            var serviceSettings = Configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();
            var mongoDbSettings = Configuration.GetSection(nameof(MongoDbSettings)).Get<MongoDbSettings>();

            // Default Configurations for Identity and Mongo DB
            services.Configure<IdentitySettings>(Configuration.GetSection(nameof(IdentitySettings)))
                .AddDefaultIdentity<ApplicationUser>()
                .AddRoles<ApplicationRole>()
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

            AddIdentityServer(services);

            services.AddLocalApiAuthentication(); // Add this to secure Api with built-in Policy

            services.AddControllers();
            services.AddHostedService<IdentitySeedHostedService>(); // Register HostedServices here
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Play.Identity.Service", Version = "v1" });
            });

            // Health Checks
            services.AddHealthChecks()
                    .AddMongoDb(); // Health Check for Mongo Db

            // Seq logging
            services.AddSeqLogging(Configuration);

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
            app.UseForwardedHeaders();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Play.Identity.Service v1"));

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
                var identitySettings = Configuration.GetSection(nameof(IdentitySettings))
                                                    .Get<IdentitySettings>();
                context.Request.PathBase = new PathString(identitySettings.PathBase);
                return next();
            });

            app.UseStaticFiles();

            app.UseRouting();

            app.UseIdentityServer(); // Add this after UseRouting()
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
                endpoints.MapPlayEconomyHealthChecks(); // Map endpoints for Healthchecks
            });
        }

        private void AddIdentityServer(IServiceCollection services)
        {
            var identityServerSettings = Configuration.GetSection(nameof(IdentityServerSettings))
                                                      .Get<IdentityServerSettings>();

            var builder = services.AddIdentityServer(options =>
            {
                options.Events.RaiseSuccessEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseErrorEvents = true;
            })
            .AddAspNetIdentity<ApplicationUser>()
            .AddInMemoryApiScopes(identityServerSettings.ApiScopes)
            .AddInMemoryApiResources(identityServerSettings.ApiResources)
            .AddInMemoryClients(identityServerSettings.Clients)
            .AddInMemoryIdentityResources(identityServerSettings.IdentityResources);

            if (environment.IsDevelopment())
            {
                builder.AddDeveloperSigningCredential();
            }
            else
            {
                var identitySettings = Configuration.GetSection(nameof(IdentitySettings))
                                                          .Get<IdentitySettings>();
                var cert = X509Certificate2.CreateFromPemFile(
                    identitySettings.CertificateCerFilePath,
                    identitySettings.CertificateKeyFilePath
                );

                builder.AddSigningCredential(cert);
            }
        }
    }
}
