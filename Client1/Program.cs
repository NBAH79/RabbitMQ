// See https://aka.ms/new-console-template for more information
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;


Console.WriteLine("Client1 press [Enter] to start");
Console.ReadLine();
var factory = new ConnectionFactory { HostName = "localhost", UserName = "client1", Password = "111122223333" };

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

    channel.BasicPublish(exchange: string.Empty,
                         routingKey: "hello",
                         basicProperties: null,
                         body: body);
    Console.WriteLine($" [x] Sent {message}");

    Console.WriteLine(" Press [enter] to exit.");
    Console.ReadLine();
}

/// RPC

{
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

    Console.WriteLine(" Press [enter] to exit.");
    Console.ReadLine();

    // Assumes only valid positive integer input.
    // Don't expect this one to work for big numbers, and it's probably the slowest recursive implementation possible.
    static int Fib(int n)
    {
        if (n is 0 or 1)
        {
            return n;
        }

        return Fib(n - 1) + Fib(n - 2);
    }
}