using Liberis.OrchestrationAdapter.Application.Services;
using Liberis.OrchestrationAdapter.Core.Interfaces;
using Liberis.OrchestrationAdapter.Core.Options;
using Gelf.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Scrutor;
using System;
using System.Reflection;
using MassTransit;
using RabbitMQ.Client;
using Liberis.OrchestrationHub.Messages.V1;
using Liberis.OrchestrationAdapter.Application.Consumers;
using Liberis.OrchestrationAdapter.Messages.V1;
using Liberis.OrchestrationAdapter.Core.Models;

namespace Liberis.OrchestrationAdapter.Application
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddJsonFile("secrets/appsettings.secrets.json", optional: false, reloadOnChange: true);

            if (env.IsDevelopment())
            {
                builder.AddUserSecrets(Assembly.Load(new AssemblyName(env.ApplicationName)));
            }

            builder.AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Logging:
            void ConfigureGelf(ILoggingBuilder loggingBuilder) =>
                loggingBuilder.AddGelf(options =>
                {
                    var gelfOptions = new GelfOptions();
                    Configuration.GetSection(GelfOptions.Options).Bind(gelfOptions);

                    var assembly = Assembly.GetEntryAssembly();

                    options.Host = gelfOptions.Host;
                    options.Port = gelfOptions.Port;
                    options.Protocol = gelfOptions.Protocol;
                    options.LogSource = gelfOptions.Source ?? assembly?.GetName().Name;
                    options.AdditionalFields["machine_name"] = Environment.MachineName;
                    options.AdditionalFields["app_version"] = assembly
                        ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                        .InformationalVersion;
                });

            services.AddLogging(ConfigureGelf);

            // Options:
            services.Configure<MessageBrokerOptions>(Configuration.GetSection(MessageBrokerOptions.Options));

            //Services:
            services.Scan(scan => scan
                .FromCallingAssembly()
                .FromAssemblies(
                    typeof(Startup).Assembly,
                    typeof(MessageBrokerOptions).Assembly
                )
                .AddClasses(c => c.InNamespaceOf<ExampleService>())
                .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            );

            services.AddScoped<IAdapterService<ExampleHubRequest, ExampleAdapterResponse>, ExampleService>();

            services.AddControllers();
            services.AddHttpClient();
            services.AddHealthChecks();
            
            ConfigureBroker(services);
        }

        private void ConfigureBroker(IServiceCollection services)
        {
            // each adapter should only have 1 consumer, be able to process one type of request (consumer payload), 
            // and produce responses always in the same format
            Type adapterConsumerType = typeof(ExampleRequestConsumer);
            // ---------------------------------------------------------------------------------------------------
            
            var messageBrokerOptions = new MessageBrokerOptions();
            Configuration.GetSection(MessageBrokerOptions.Options).Bind(messageBrokerOptions);

            services.AddMassTransit(transitCfg =>
            {
                transitCfg.AddConsumer(adapterConsumerType);
                transitCfg.UsingRabbitMq((context, rabbitMqCfg) =>
                {
                    rabbitMqCfg.Host(messageBrokerOptions.Host, h =>
                    {
                        h.Username(messageBrokerOptions.Username);
                        h.Password(messageBrokerOptions.Password);
                    });

                    var queueName = typeof(Program).Assembly.GetName().Name;
                    if (queueName.EndsWith(".Application"))
                    {
                        queueName = queueName.Remove(queueName.Length - 12);
                    }
                    
                    rabbitMqCfg.ReceiveEndpoint(queueName, ep =>
                    {
                        ep.ConfigureConsumeTopology = false;
                        ep.Bind<HubRequest<ExampleHubRequest>>(c =>
                        {
                            c.ExchangeType = ExchangeType.Topic;
                            c.RoutingKey = messageBrokerOptions.RoutingKey;
                        });

                        ep.ConfigureConsumer(context, adapterConsumerType);
                    });

                    rabbitMqCfg.Publish<AdapterResponse<ExampleAdapterResponse>>(x => x.ExchangeType = ExchangeType.Fanout);
                });
            });

            services.AddMassTransitHostedService();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks(Configuration["HealthChecks:Liveness"], new HealthCheckOptions
                {
                    Predicate = _ => false,
                    ResponseWriter = HealthCheckFormatter.LivenessResponseAsync
                });

                endpoints.MapHealthChecks(Configuration["HealthChecks:Readiness"], new HealthCheckOptions
                {
                    ResponseWriter = HealthCheckFormatter.ReadinessResponseAsync
                });
            });

        }
    }
}
