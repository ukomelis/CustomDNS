using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Program
{
    static int Main(string[] args)
    {
        // Create a new command line application
        var rootCommand = new RootCommand
        {
            new Option<int>(
                "--port",
                getDefaultValue: () => 11000,
                description: "The port to listen on.")
        };

        rootCommand.Handler = CommandHandler.Create<int>(RunServer);

        // Parse the incoming args and invoke the handler
        return rootCommand.InvokeAsync(args).Result;
    }

    static void RunServer(int port)
    {
        // Set up a UDP client
        var udpClient = new UdpClient(port); // Listen on specified port
        Console.WriteLine($"Listening on port {port}...");
        try
        {
            while (true)
            {
                // Listen for incoming data
                var remoteEP = new IPEndPoint(IPAddress.Any, 0);
                var data = udpClient.Receive(ref remoteEP);

                // Print the raw received data
                Console.WriteLine($"Received data: {BitConverter.ToString(data)}");

                // Parse the DNS query and display it
                var message = ParseDnsQuery(data);
                Console.WriteLine($"Received: {message}");
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"SocketException: {ex}");
        }
        finally
        {
            udpClient.Close();
        }
    }

    public static string ParseDnsQuery(byte[] bytes)
    {
        var index = 0;

        // Read the transaction ID
        var transactionId = BitConverter.ToString(bytes, index, 2);
        index += 2;

        // Read the flags
        var flags = BitConverter.ToString(bytes, index, 2);
        index += 2;

        // Read the question count
        var questionCount = BitConverter.ToString(bytes, index, 2);
        index += 2;

        // Skip the answer, authority, and additional counts
        index += 6;

        // Read the domain name
        var domain = new StringBuilder();
        while (bytes[index] != 0)
        {
            // Read the length of the next part
            int length = bytes[index++];

            // Read the part itself
            domain.Append(Encoding.ASCII.GetString(bytes, index, length));

            // Move to the next part
            index += length;

            // Add a dot if there's more to come
            if (bytes[index] != 0)
            {
                domain.Append('.');
            }
        }

        // Skip the zero byte at the end of the domain name
        index++;

        // Read the type
        string type = BitConverter.ToString(bytes, index, 2);
        index += 2;

        // Read the class
        var class_ = BitConverter.ToString(bytes, index, 2);

        return $"Transaction ID: {transactionId}, Flags: {flags}, Question Count: {questionCount}, Domain: {domain}, Type: {type}, Class: {class_}";
    }
}