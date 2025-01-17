﻿using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using NodaTime;
using OneTimeSecret.Web.Models.Config;
using OneTimeSecret.Web.Services;
using OneTimeSecret.Web.Services.HealthChecks;
using StackExchange.Redis;

namespace OneTimeSecret.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            NetworkingConfig networkingConfig = this.Configuration.GetSection("Networking").Get<NetworkingConfig>();

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardLimit = networkingConfig.UpstreamProxyHops;

                if (networkingConfig.KnownProxyServers?.Count > 0)
                {
                    foreach (string ip in networkingConfig.KnownProxyServers)
                    {
                        options.KnownProxies.Add(IPAddress.Parse(ip));
                    }
                }
            });

            string redisConString = this.Configuration.GetValue<string>("RedisConnectionString");
            AesConfig aesSettings = this.Configuration.GetSection("AesSettings").Get<AesConfig>();

            services.AddSingleton<IConnectionMultiplexer>(_ =>
               ConnectionMultiplexer.Connect(redisConString));

            services.AddTransient<IDatabase>(sp => sp
                .GetRequiredService<IConnectionMultiplexer>()
                .GetDatabase(0));
            services.AddTransient<IClock>(s => SystemClock.Instance);
            services.AddTransient<ICryptoService, CryptoService>();

            services.AddTransient<IAesEncryptionService>(s => new AesEncryptionService(aesSettings.MasterKey, aesSettings.Version));

            services
                .AddMvc()
                .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.IgnoreNullValues = true;
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

                // TODO: figure out NodaTime in 3.0
            });

            services
                .AddHealthChecks()
                .AddCheck<VersionHealthCheck>("version_health_check")
                .AddCheck<RedisHealthCheck>("redis_health_check");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/error/500");
                app.UseStatusCodePagesWithReExecute("/error/{0}");
            }

            app.UseStaticFiles();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health", new HealthCheckOptions()
                {
                    ResponseWriter = HealthCheckResponseWriter.WriteResponse,
                    ResultStatusCodes =
                    {
                        [HealthStatus.Healthy] = StatusCodes.Status200OK,
                        [HealthStatus.Degraded] = StatusCodes.Status200OK,
                        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
                    },
                });
            });
        }
    }
}
