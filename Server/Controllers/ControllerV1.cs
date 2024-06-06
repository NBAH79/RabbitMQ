using Asp.Versioning;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading.Channels;
using System.Text;
using Microsoft.Extensions.Logging;
using RabbitMQ.Stream.Client;
using RabbitMQ.Stream.Client.AMQP;
using RabbitMQ.Stream.Client.Reliable;
using System.Net;
using System.Diagnostics;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Server.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class ControllerV1 : ControllerBase
    {
        // GET: api/<ValuesController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        /// <summary>
        /// GET api/{id}
        /// 1 - Service1
        /// 2 - ServiceRPC
        /// 3 - ServiceStream
        /// </summary>
        [HttpGet("{id}")]
        public string Get(int id)
        {
            switch (id)
            {
                case 0: return TryService1();
                case 1: return TryServicsRPC();
                case 2:
                    {
                        var t = Task.Run(TryServiceStream);
                        return t.Result;
                    }
            }
            return "value";
        }

        // POST api/<ValuesController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<ValuesController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<ValuesController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }

        private static string TryService1()
        {
            var factory = new ConnectionFactory { HostName = "rabbitmq", UserName = "client1", Password = "111122223333" };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
            {
                channel.QueueDeclare(queue: "hello",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var message = "Hello World!";
                var body = Encoding.UTF8.GetBytes(message);
                Stopwatch stopwatch = Stopwatch.StartNew();
                for (int i = 0; i < 1000; i++) {
                channel.BasicPublish(exchange: string.Empty,
                                     routingKey: "hello",
                                     basicProperties: null,
                                     body: body);
                }
                stopwatch.Stop();
                Console.WriteLine($" [x] Sent {message} in {stopwatch.ElapsedMilliseconds} ms");
            }
            return "done 0";
        }
        private static string TryServicsRPC()
        {
            var factory = new ConnectionFactory { HostName = "rabbitmq", UserName = "client1", Password = "111122223333" };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
            channel.QueueDeclare(queue: "rpc_queue",
                         durable: false,
                         exclusive: false,
                         autoDelete: false,
                         arguments: null);
            channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);


            var consumer = new EventingBasicConsumer(channel);
            channel.BasicConsume(queue: "rpc_queue",
                                 autoAck: false,
                                 consumer: consumer);
            Console.WriteLine(" [x] Awaiting RPC requests");

            consumer.Received += (model, ea) =>
            {
                string response = string.Empty;

                var body = ea.Body.ToArray();
                var props = ea.BasicProperties;
                var replyProps = channel.CreateBasicProperties();
                replyProps.CorrelationId = props.CorrelationId;

                try
                {
                    var message = Encoding.UTF8.GetString(body);
                    int n = int.Parse(message);
                    Console.WriteLine($" [.] Fib({message})");
                    response = Fib(n).ToString();
                }
                catch (Exception e)
                {
                    Console.WriteLine($" [.] {e.Message}");
                    response = string.Empty;
                }
                finally
                {
                    var responseBytes = Encoding.UTF8.GetBytes(response);
                    channel.BasicPublish(exchange: string.Empty,
                                         routingKey: props.ReplyTo,
                                         basicProperties: replyProps,
                                         body: responseBytes);
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
            };
            return "done 1";
        }

        static int Fib(int n)
        {
            if (n is 0 or 1)
            {
                return n;
            }

            return Fib(n - 1) + Fib(n - 2);
        }

        private static async Task<string> TryServiceStream()
        {

            await SuperStreamProducer.Start().ConfigureAwait(false);
            return "done 2";

        }
        private static class SuperStreamProducer
        {
            public static async Task Start()
            {
                var config = new StreamSystemConfig()
                {
                    UserName = "client1",
                    Password = "111122223333",
                    VirtualHost = "/",
                };
                var system = await StreamSystem.Create(config).ConfigureAwait(false);

                // tag::super-stream-creation[]
                await system.CreateSuperStream(new PartitionsSuperStreamSpec("stream_name", 3)).ConfigureAwait(false);
                // end::super-stream-creation[]
                // We define a Producer with the SuperStream name (that is the Exchange name)
                // tag::super-stream-producer[]
                var producer = await Producer.Create(
                    new ProducerConfig(system,
                            // Costants.StreamName is the Exchange name
                            // invoices
                            "stream_name") // <1>
                    {
                        SuperStreamConfig = new SuperStreamConfig() // <2>
                        {
                            // The super stream is enable and we define the routing hashing algorithm
                            Routing = msg => msg.Properties.MessageId.ToString() // <3>
                        }
                    }).ConfigureAwait(false);
                const int NumberOfMessages = 1_000_000;
                for (var i = 0; i < NumberOfMessages; i++)
                {
                    var message = new Message(Encoding.Default.GetBytes($"my_invoice_number{i}")) // <4>
                    {
                        Properties = new Properties() { MessageId = $"id_{i}" }
                    };
                    await producer.Send(message).ConfigureAwait(false);
                    // end::super-stream-producer[]
                    Console.WriteLine("Sent {I} message to {StreamName}, id: {ID}", $"my_invoice_number{i}",
                        "stream_name", $"id_{i}");
                    Thread.Sleep(TimeSpan.FromMilliseconds(1000));
                }
            }
        }

    }


}
