using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting ;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Serialization.JsonNet;
using OneTimeSecret.Web.Models.Config;
using OneTimeSecret.Web.Services;
using StackExchange.Redis;

namespace OneTimeSecret.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var networkingConfig = this.Configuration.GetSection("Networking").Get<NetworkingConfig>();

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardLimit = networkingConfig.UpstreamProxyHops;

                if (networkingConfig.KnownProxyServers?.Count > 0)
                {
                    foreach (var ip in networkingConfig.KnownProxyServers)
                    {
                        options.KnownProxies.Add(IPAddress.Parse(ip));
                    }
                }
            });

            var redisConString = this.Configuration.GetValue<string>("RedisConnectionString");
            var aesSettings = this.Configuration.GetSection("AesSettings").Get<AesConfig>();


            services.AddSingleton<IConnectionMultiplexer>(_ =>
               ConnectionMultiplexer.Connect(redisConString));

            services.AddTransient<IDatabase>(sp => sp
                .GetRequiredService<IConnectionMultiplexer>()
                .GetDatabase(0));
            services.AddTransient<IClock>(s => SystemClock.Instance);
            services.AddTransient<ICryptoService, CryptoService>();

            var key = Encoding.UTF8.GetBytes(aesSettings.MasterKey);

            services.AddTransient<IAesEncryptionService>(s => new AesEncryptionService(key, aesSettings.Version));
            services
                .AddMvc()
                .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.IgnoreNullValues = true;
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                //TODO: figure out NodaTime in 3.0
            }); ;
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

            app.UseMvc();
        }
    }
}
