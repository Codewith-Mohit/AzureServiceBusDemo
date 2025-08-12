using Azure.Messaging.ServiceBus;
using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;


class Program
{


    // The connection string and queue name are the same as the sender
    private const string connectionString = "AZURE_SERVICEBUS_CONNECTION_STRING";
    private const string queueName = "msg-queue";

    static async Task Main(string[] args)
    {
        await using var client = new ServiceBusClient(connectionString);

        // The ServiceBusProcessor is a convenient way to receive and process messages
        // from a queue in an event-driven manner.
        ServiceBusProcessor processor = client.CreateProcessor(queueName, new ServiceBusProcessorOptions());

        // Define the handlers for received messages and for errors
        processor.ProcessMessageAsync += MessageHandler;
        processor.ProcessErrorAsync += ErrorHandler;

        // Start processing messages
        await processor.StartProcessingAsync();

        Console.WriteLine("Waiting for messages...");
        Console.WriteLine("Press any key to stop the processor and exit.");
        Console.ReadKey();

        // Stop processing messages
        await processor.StopProcessingAsync();
    }

    static async Task MessageHandler(ProcessMessageEventArgs args)
    {
        string body = args.Message.Body.ToString();
        string mNumber = args.Message.ApplicationProperties.TryGetValue("MessageNumber", out var value) ? value.ToString() : "Unknown";
        Console.WriteLine($"Received: {mNumber}");

        // "Complete" the message to remove it from the queue.
        // If this is not called, the message will be "abandoned" and reappear after a timeout.
        await args.CompleteMessageAsync(args.Message);
    }

    static Task ErrorHandler(ProcessErrorEventArgs args)
    {
        Console.WriteLine($"An error occurred: {args.Exception.Message}");
        return Task.CompletedTask;
    }
}