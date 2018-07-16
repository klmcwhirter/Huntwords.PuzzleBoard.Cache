#pragma warning disable CS1572, CS1573, CS1591
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Huntwords.Common.Repositories;
using Huntwords.Common.Services;
using Huntwords.Common.Utils;
using Huntwords.PuzzleBoard.Cache.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Huntwords.PuzzleBoard.Cache
{
    public class Startup
    {
        ILogger Logger { get; }

        public Startup(IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory.CreateLogger<Startup>();

            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }
        public IContainer Container { get; private set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            // Adds services required for using options.
            services.AddOptions();

            // Register the IConfiguration instance which the Options classes bind against.
            services.Configure<LimitedConcurrencyLevelTaskSchedulerOptions>(options => Configuration.GetSection("Tasks").Bind(options));
            services.Configure<PuzzleBoardGeneratorOptions>(options => Configuration.GetSection("Board").Bind(options));

            services.AddMvc();

            Container = AddToAutofac(services);

            // Start worker threads filling the cache
            Task.Factory.StartNew(() =>
            {
                var first = true;
                do
                {
                    var state = first ? "Starting" : "Retrying";
                    if (!first)
                    {
                        Thread.Sleep(5000); // wait 5 secs before retrying
                    }

                    try
                    {
                        Logger.LogInformation($"{state} filler task...");
                        var svc = Container.Resolve<PuzzleBoardCacheManager>();
                        Logger.LogInformation($"svc={svc}");
                        svc?.Initialize(false);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, $"Error starting filler task");
                    }
                    finally
                    {
                        Logger.LogInformation("Started filler task.");
                    }
                    first = false;
                } while (true);
            }, TaskCreationOptions.LongRunning);

            var rc = new AutofacServiceProvider(Container);
            return rc;
        }

        private IContainer AddToAutofac(IServiceCollection services)
        {
            var builder = new ContainerBuilder();

            builder.RegisterCommonRedis(Configuration);
            builder.RegisterCommonRepositories();
            builder.RegisterCommonServices();

            builder.RegisterType<LimitedConcurrencyLevelTaskScheduler>().As<TaskScheduler>().SingleInstance();

            builder.RegisterType<PuzzleBoardCache>().AsSelf();
            builder.RegisterType<PuzzleBoardCacheManager>().AsSelf();

            builder.RegisterType<PuzzleBoardGenerator>().As<IGenerator<Huntwords.Common.Models.PuzzleBoard>>();

            // Word Generators
            builder.RegisterType<PuzzleWordGenerator>().Keyed<IGenerator<string>>(WordGeneratorsNamesProvider.Cached);
            builder.RegisterType<RandomWordGenerator>().Keyed<IGenerator<string>>(WordGeneratorsNamesProvider.Random);
            builder.RegisterType<WordWordGenerator>().Keyed<IGenerator<string>>(WordGeneratorsNamesProvider.Word);

            builder.Populate(services);
            var rc = builder.Build();
            return rc;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }
    }
}
