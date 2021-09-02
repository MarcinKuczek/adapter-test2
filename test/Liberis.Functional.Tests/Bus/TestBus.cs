using Liberis.OrchestrationHub.Messages.V1;
using Liberis.OrchestrationHub.Messages.V1.Advert;
using Liberis.Testing;
using MassTransit;
using MassTransit.RabbitMqTransport;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Liberis.OrchestrationAdapter.Functional.Tests.Bus
{
    public sealed class TestBus : IDisposable
    {
        private BusHandle _busHandle;
        private IBusControl _busControl;

        private readonly List<Tuple<Type, Action<object>, ConsumerConfiguration>> _consumerConfigurations = new List<Tuple<Type, Action<object>, ConsumerConfiguration>>();

        public static async Task<TestBus> StartNewAsync()
        {
            var bus = new TestBus();
            await bus.StartAsync();
            return bus;
        }

        public async Task StartAsync()
        {
            _busControl = MassTransit.Bus.Factory.CreateUsingRabbitMq(configurator =>
            {
                configurator.Host("amqp://localhost:5672", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });

                configurator.ReceiveEndpoint("Liberis.OrchestrationAdapter.Functional.Tests-" + Guid.NewGuid(), endpointConfigurator =>
                {
                    RegisterConsumers(endpointConfigurator);
                    endpointConfigurator.AutoDelete = true;
                });

                RegisterPublishers(configurator);

                configurator.AutoDelete = true;
            });

            _busHandle = await _busControl.StartAsync();
            await _busHandle.Ready;
        }

        private void RegisterConsumers(IRabbitMqReceiveEndpointConfigurator endpointConfigurator)
        {
            foreach (var consumerConfiguration in _consumerConfigurations)
            {
                var messageType = consumerConfiguration.Item1;
                var action = consumerConfiguration.Item2;
                var customizedConfiguration = consumerConfiguration.Item3;
                var consumerType = typeof(IConsumer<>).MakeGenericType(messageType);

                endpointConfigurator.ConfigureConsumeTopology = false;

                endpointConfigurator.Bind(customizedConfiguration.ExchangeName, c =>
                {
                    c.ExchangeType = customizedConfiguration.ExchangeType;
                    c.RoutingKey = customizedConfiguration.RoutingKey;
                });
                endpointConfigurator.Consumer(consumerType, type => new ActionConsumer(action));
            }
        }

        private void RegisterPublishers(IRabbitMqBusFactoryConfigurator configurator)
        {
            // Add publisher configurations manually
            configurator.Publish<HubRequest<GetAdvertRequest>>(x => x.ExchangeType = ExchangeType.Topic);
        }

        public async Task Publish<T>(T message, string routingKey) where T : class => await _busControl.Publish(message, x => x.SetRoutingKey(routingKey)).ConfigureAwait(false);

        public void AddCustomConsumer<T>(Action<T> action, ConsumerConfiguration consumerConfiguration)
        {
            var kvp = new Tuple<Type, Action<object>, ConsumerConfiguration>(typeof(T), x => action((T)x), consumerConfiguration);
            _consumerConfigurations.Add(kvp);
        }

        public TimedPromise<T> Expect<T>(TimeSpan wait, ConsumerConfiguration consumerConfiguration)
        {
            var timeoutTask = new TimedPromise<T>(wait);
            _consumerConfigurations.Add(new Tuple<Type, Action<object>, ConsumerConfiguration>(typeof(T), x => timeoutTask.Resolve((T)x), consumerConfiguration));

            return timeoutTask;
        }

        public TimedPromise<T> Expect<T>(TimeSpan wait)
        {
            var consumerConfiguration = new ConsumerConfiguration();

            return Expect<T>(wait, consumerConfiguration);
        }

        public void Dispose() => _busHandle.StopAsync().Wait();

        private class ActionConsumer : IConsumer<object>
        {
            private readonly Action<object> _action;

            public ActionConsumer(Action<object> action)
            {
                _action = action;
            }

            public Task Consume(ConsumeContext<object> context)
            {
                _action(context.Message);

                return Task.FromResult(0);
            }
        }
    }
}
