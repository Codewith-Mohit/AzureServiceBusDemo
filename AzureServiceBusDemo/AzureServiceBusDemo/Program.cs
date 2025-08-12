using Azure.Messaging.ServiceBus;
using System;
using System.Text;
using System.Threading.Tasks;

class Program
{
    // The connection string is a key to your Service Bus namespace
    private const string connectionString = "AZURE_SERVICEBUS_CONNECTION_STRING";

    // The name of the queue you created
    private const string queueName = "msg-queue";

    static async Task Main(string[] args)
    {
        // The ServiceBusClient will be responsible for creating senders and receivers
        await using var client = new ServiceBusClient(connectionString);

        // The ServiceBusSender is used to publish messages to a queue
        ServiceBusSender sender = client.CreateSender(queueName);



        string messageBody = "Hello, Azure Service Bus!";

        try
        {
            // Create a ServiceBusMessage object with the message body
            var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(messageBody));

            for (int i = 0; i < 50000; i++)
            {
                // Add a custom application property to the message
                message.ApplicationProperties["MessageNumber"] = i;
                message.ApplicationProperties["Sender"] = "MsgSender";
                await sender.SendMessageAsync(message);
                Console.WriteLine($"Sent message: {i + ":" +messageBody}");

           /* Task.Delay(10).Wait(); */// Simulate some delay between messages
            }

            // Send the message to the queue
            
            Console.WriteLine($"Sent message: {messageBody}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An exception occurred: {ex.Message}");
        }
        finally
        {
            // Clean up the sender
            await sender.DisposeAsync();
        }

        Console.WriteLine("Press any key to exit.");
        Console.ReadKey();
    }
}