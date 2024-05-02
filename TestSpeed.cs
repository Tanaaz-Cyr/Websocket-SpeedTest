using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Starting speed test...");
        Console.WriteLine("How many messages would you like to send?");
        var numMessages = Convert.ToInt32(Console.ReadLine());

        Console.WriteLine("Please enter the server name or IP address: ");
        var serverName = Console.ReadLine();

        Console.WriteLine("Please enter the port number: ");
        var port = Convert.ToInt32(Console.ReadLine());

        Console.WriteLine("Enter the message you would like to send: ");
        var messageToSend = Console.ReadLine();


        var client = new ClientWebSocket();
        var uri = new Uri("ws://"+serverName+":"+port.ToString()+"/");

        Console.WriteLine("Connecting to server...");
        Task.Run(() => client.ConnectAsync(uri, CancellationToken.None)).GetAwaiter().GetResult();

        var message = Encoding.UTF8.GetBytes(messageToSend);
        var buffer = new ArraySegment<byte>(message);

        Console.WriteLine("Sending messages...");
        var startTime = DateTime.Now;

        try
        {
            for (int i = 0; i < numMessages; i++)
            {
                Task.Run(() => client.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None)).GetAwaiter().GetResult();
                Console.WriteLine($"Sent message {i + 1}");

                // Start listening for messages
                string response = null;
                while (response == null)
                {
                    response = Task.Run(() => ListenForMessages(client)).GetAwaiter().GetResult();
                }
                Console.WriteLine($"Received message: {response}");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: " + e.Message);
        }

        Console.WriteLine("All messages sent and responses received.");

        var endTime = DateTime.Now;
        var totalTime = endTime - startTime;
        Console.WriteLine($"Total time: {totalTime.TotalSeconds} seconds");
        Console.WriteLine($"Total Message per second: {numMessages / totalTime.TotalSeconds}");

        // Close the connection after all messages are sent and responses are received
        if (client.State == WebSocketState.Open)
        {
            Task.Run(() => client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None)).GetAwaiter().GetResult();
        }
    }

    private static string ListenForMessages(ClientWebSocket client)
    {
        var receiveBuffer = new ArraySegment<byte>(new byte[8192]);

        if (client.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result;
            try
            {
                result = Task.Run(() => client.ReceiveAsync(receiveBuffer, CancellationToken.None)).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
                return null;
            }

            if (result.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }
            else
            {
                var message = Encoding.UTF8.GetString(receiveBuffer.Array, 0, result.Count);
                return message;
            }
        }

        return null;
    }
}