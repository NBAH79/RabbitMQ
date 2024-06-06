// See https://aka.ms/new-console-template for more information
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using RabbitMQ.Stream.Client;
using RabbitMQ.Stream.Client.Reliable;


await SuperStreamConsumer.Start("rabbit_stream").ConfigureAwait(false);

public static class SuperStreamConsumer
{
    public static async Task Start(string consumerName)
    {
        var config = new StreamSystemConfig()
        {
            UserName = "client1",
            Password = "111122223333",
            VirtualHost = "/",
        };
        var system = await StreamSystem.Create(config).ConfigureAwait(false);

        Console.WriteLine("Super Stream Consumer connected to RabbitMQ. ConsumerName {0}", consumerName);
        // tag::consumer-simple[]
        var consumer = await Consumer.Create(new ConsumerConfig(system, consumerName)
        {
            
            IsSuperStream = true, // Mandatory for enabling the super stream // <1>
                                  // this is mandatory for super stream single active consumer
                                  // must have the same ReferenceName for all the consumers
            Reference = "MyApp",
            OffsetSpec = new OffsetTypeFirst(),
            MessageHandler = async (stream, consumerSource, context, message) => // <2>
            {
                Console.WriteLine("Consumer Name {ConsumerName} " +
                                          "-Received message id: {PropertiesMessageId} body: {S}, Stream {Stream}, Offset {Offset}",
                    consumerName, message.Properties.MessageId, Encoding.UTF8.GetString(message.Data.Contents),
                    stream, context.Offset);
                //end::consumer-simple[]
                // tag::sac-manual-offset-tracking[]
                await consumerSource.StoreOffset(context.Offset).ConfigureAwait(false); // <1>
                await Task.CompletedTask.ConfigureAwait(false);
            },
            IsSingleActiveConsumer = true, // mandatory for enabling the Single Active Consumer // <2>
            ConsumerUpdateListener = async (reference, stream, isActive) => // <3>
            {
                Console.WriteLine($"******************************************************");
                Console.WriteLine("reference {Reference} stream {Stream} is active: {IsActive}", reference,
                    stream, isActive);

                ulong offset = 0;
                try
                {
                    offset = await system.QueryOffset(reference, stream).ConfigureAwait(false);
                }
                catch (OffsetNotFoundException e)
                {
                    Console.WriteLine("OffsetNotFoundException {Message}, will use OffsetTypeNext", e.Message);
                    return new OffsetTypeNext();
                }

                if (isActive)
                {
                    Console.WriteLine("Restart Offset {Offset}", offset);
                }

                Console.WriteLine($"******************************************************");
                await Task.CompletedTask.ConfigureAwait(false);
                return new OffsetTypeOffset(offset + 1); // <4>
            },
            //end::sac-manual-offset-tracking[]
        }).ConfigureAwait(false);
        // 
    }
}

