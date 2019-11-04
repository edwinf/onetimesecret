using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OneTimeSecret.Web.Utiltiies;

namespace OneTimeSecret.Web.Services.HealthChecks
{
    public static class HealthCheckResponseWriter
    {
        // https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-3.0#customize-output
        public static Task WriteResponse(HttpContext httpContext, HealthReport result)
        {
            httpContext.Response.ContentType = "application/json";

            var json = new JObject(
                new JProperty("status", result.Status.ToString()),
                new JProperty("results", new JObject(result.Entries.Select(pair =>
                    new JProperty(pair.Key, new JObject(
                        new JProperty("status", pair.Value.Status.ToString()),
                        new JProperty("description", pair.Value.Description),
                        new JProperty("responseTime", pair.Value.Duration.TotalMilliseconds),
                        new JProperty("exception", FilterExceptionIfPublicCall(httpContext, pair)),
                        new JProperty("data", new JObject(pair.Value.Data.Select(
                            p => new JProperty(p.Key, p.Value))))))))));
            return httpContext.Response.WriteAsync(
                json.ToString(Formatting.Indented));
        }

        private static object FilterExceptionIfPublicCall(HttpContext httpContext, KeyValuePair<string, HealthReportEntry> pair)
        {
            if (httpContext.Connection.RemoteIpAddress.IsInternal())
            {
                return JsonConvert.SerializeObject(pair.Value.Exception);
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
