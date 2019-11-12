using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace OneTimeSecret.Web.Services.HealthChecks
{
    public class RedisHealthCheck : IHealthCheck
    {
        private readonly IDatabase redisDatabase;

        public RedisHealthCheck(IDatabase redisDatabase)
        {
            this.redisDatabase = redisDatabase;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            string randomKey = Guid.NewGuid().ToString();
            this.redisDatabase.StringSet(randomKey, "test", TimeSpan.FromMinutes(1));
            this.redisDatabase.StringGet(randomKey);
            return Task.FromResult(HealthCheckResult.Healthy());
        }
    }
}
