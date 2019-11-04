using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OneTimeSecret.Web.Controllers;

namespace OneTimeSecret.Web.Services.HealthChecks
{
    public class VersionHealthCheck : IHealthCheck
    {
        private static readonly string VersionNumber = FileVersionInfo.GetVersionInfo(typeof(SecretController).Assembly.Location).ProductVersion;

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var result = HealthCheckResult.Healthy(data: new ReadOnlyDictionary<string, object>(new Dictionary<string, object>
            {
                { "version", VersionNumber },
            }));

            return Task.FromResult(result);
        }
    }
}
