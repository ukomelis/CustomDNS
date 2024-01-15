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
        //Console.WriteLine(Dns.GetHostEntry("www.google.com").AddressList[0]);

        // Set up a UDP client for listening
        var listenClient = new UdpClient(port); // Listen on specified port
        Console.WriteLine($"Listening on port {port}...");

        try
        {
            while (true)
            {
                // Listen for incoming data
                var remoteEP = new IPEndPoint(IPAddress.Any, 0);
                var data = listenClient.Receive(ref remoteEP);

                Console.WriteLine($"Received DNS query from {remoteEP}:");
                Console.WriteLine(ParseDnsQuery(data));

                // Set up a UDP client for forwarding
                using var forwardClient = new UdpClient();

                // Forward the DNS query to Google's public DNS server
                var googleDnsEp = new IPEndPoint(IPAddress.Parse("8.8.8.8"), 53);
                forwardClient.Send(data, data.Length, googleDnsEp);

                // Receive the DNS response from Google's public DNS server
                var googleDnsResponse = forwardClient.Receive(ref googleDnsEp);
                Console.WriteLine($"Received DNS response from google {googleDnsEp}:");
                Console.WriteLine(ParseDnsQuery(googleDnsResponse));

                var serverIp = ParseIpFromDnsResponse(googleDnsResponse);
                Console.WriteLine($"Server IP: {serverIp}");

                // Send the DNS response back to the original client
                forwardClient.Send(googleDnsResponse, googleDnsResponse.Length, remoteEP);
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"SocketException: {ex}");
        }
        finally
        {
            listenClient.Close();
        }
    }

    public static string ParseIpFromDnsResponse(byte[] bytes)
    {
        var index = 12; // Skip the header

        // Skip the question section
        while (bytes[index] != 0)
            index++;
        index += 5; // Skip the type and class

        // Skip until we find an answer (indicated by 0xC0)
        while (bytes[index] != 0xC0)
            index++;
        index += 2; // Skip the pointer

        // Skip the type, class, and TTL
        index += 10;

        // The next four bytes are the IP address
        var ip = new byte[4];
        Array.Copy(bytes, index, ip, 0, 4);

        return new IPAddress(ip).ToString();
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