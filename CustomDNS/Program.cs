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
            // Always close the UDP client when done
            udpClient.Close();
        }
    }

    public static string ParseDnsQuery(byte[] bytes)
    {
        // Skip the transaction ID
        int index = 2;

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

        return domain.ToString();
    }
}