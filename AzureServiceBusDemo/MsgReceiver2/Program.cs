using Azure.Messaging.ServiceBus;
using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

class Program
{
    static async Task Main(string[] args)
    {
        // ================================================================
        // 1. CONFIGURATION SETUP (TOP-LEVEL STATEMENTS)
        // ================================================================

        // Get the current environment. We use "Development" for local testing 
        // and "Production" as a fallback if the environment variable is not set.
        // You can set this variable in Visual Studio's debug settings.
        string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        // Create a configuration builder.
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            // Add the environment-specific file. "optional: true" means it won't crash if it doesn't exist.
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            // Also add environment variables for production overrides.
            .AddEnvironmentVariables();

        IConfiguration configuration = builder.Build();

        // Get the connection string and queue name from the configuration.
        string connectionString = configuration.GetSection("AzureServiceBus:ConnectionString").Value;
        const string queueName = "msg-queue";

        Console.WriteLine($"Environment: {environment}");
        Console.WriteLine($"Connection String from config: {connectionString}\n");

        // A basic check to ensure a connection string is present.
        if (string.IsNullOrWhiteSpace(connectionString) || connectionString.Contains("Placeholder"))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: Connection string is not configured. Please update appsettings.Development.json.");
            Console.ResetColor();
            return;
        }

        // The ServiceBusClient will be responsible for creating senders and receivers
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