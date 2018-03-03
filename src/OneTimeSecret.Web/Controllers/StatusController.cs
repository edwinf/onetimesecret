using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OneTimeSecret.Web.Models.Status;
using OneTimeSecret.Web.Utiltiies;
using StackExchange.Redis;

namespace OneTimeSecret.Web.Controllers
{
    [Route("status")]
    public class StatusController : Controller
    {
        private const string UP = "UP";
        private const string DOWN = "DOWN";

        private static Lazy<string> versionNumber = new Lazy<string>(() =>
        {
            return FileVersionInfo.GetVersionInfo(typeof(StatusController).Assembly.Location).ProductVersion;
        });

        private readonly IDatabase redis;

        public StatusController(IDatabase redis)
        {
            this.redis = redis;
        }

        [Route("")]
        public IActionResult Status()
        {
            var version = this.GetVersion();
             var cacheStatus = this.SafelyGetStatus("Redis", () => this.GetCacheStatus());
      
            var items = new List<StatusItem> { version, cacheStatus };

            if (items.Any(s => s.Status == DOWN))
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, items);
            }
            else
            {
                return this.Ok(items);
            }
        }

        private StatusItem GetVersion()
        {
            return new StatusItem
            {
                Name = "Version",
                Message = versionNumber.Value,
                Status = UP
            };
        }

        private void GetCacheStatus()
        {
            this.redis.StringGet("NonExistentKeyToTestConnectivity");
        }

        private StatusItem SafelyGetStatus(string name, Action getStatusFunc, TimeSpan? timeout = null)
        {
            var response = new StatusItem { Name = name, Status = UP };
            var sw = new Stopwatch();
            sw.Start();
            try
            {
                getStatusFunc();
            }
            catch (Exception ex)
            {
               // this.logger.Error(ex, "Exception getting status for {name}", name);
                //NewRelic.Api.Agent.NewRelic.NoticeError(e);
                response = new StatusItem
                {
                    Name = name,
                    Status = DOWN,
                    Message = this.SafeExceptionRendering(ex)
                };
            }

            sw.Stop();
            response.ResponseTime = sw.ElapsedMilliseconds.ToString();

            return response;
        }

        private async Task<StatusItem> SafelyGetStatusAsync(string name, Func<Task> getStatusFunc, TimeSpan? timeout = null)
        {
            var response = new StatusItem { Name = name, Status = UP };
            var sw = new Stopwatch();
            sw.Start();
            try
            {
                await getStatusFunc();
            }
            catch (Exception ex)
            {
                response = new StatusItem
                {
                    Name = name,
                    Status = DOWN,
                    Message = this.SafeExceptionRendering(ex)
                };
            }

            sw.Stop();
            response.ResponseTime = sw.ElapsedMilliseconds.ToString();

            return response;
        }


        private string SafeExceptionRendering(Exception ex)
        {
            if (this.HttpContext.Connection.RemoteIpAddress.IsInternal())
            {
                return ex.ToString();
            }
            else
            {
                return "";
            }
        }
    }
}
