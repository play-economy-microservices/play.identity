using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;

namespace Play.Identity.Service.HealthChecks;

/// <summary>
/// This is a custom health check for our Mongo Db. Attempt to retrieve something from the db, if
/// it's able to retrieve then it is consider healthy.
/// </summary>
public class MongoDbHealthCheck : IHealthCheck
{
    private readonly MongoClient client;

    public MongoDbHealthCheck(MongoClient client)
    {
        this.client = client;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            await client.ListDatabaseNamesAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception e)
        {
            return HealthCheckResult.Unhealthy();
        }
    }
}