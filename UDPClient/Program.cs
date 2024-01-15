using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        // Check if at least the URL argument is provided
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: program <url> [<port>]");
            return;
        }

        // Parse the URL from the command line arguments
        var url = args[0];

        // Parse the port from the command line arguments, or use the default port
        var port = (args.Length > 1) ? int.Parse(args[1]) : 11000;

        // Set up a UDP client
        var udpClient = new UdpClient();

        try
        {
            // Construct a DNS query for the specified URL
            var message = new DnsQuery(url).ToByteArray();
            udpClient.Send(message, message.Length, "localhost", port);

            Console.WriteLine($"Sent DNS query for {url}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex}");
        }
        finally
        {
            // Always close the UDP client when done
            udpClient.Close();
        }
    }
}

public class DnsQuery
{
    private readonly string domain;

    public DnsQuery(string domain)
    {
        this.domain = domain;
    }

    public byte[] ToByteArray()
    {
        // Start with a list to hold all the bytes
        List<byte> queryBytes = new List<byte>();

        // Add a transaction ID
        queryBytes.Add(0xAB);
        queryBytes.Add(0xCD);

        // Split the domain into parts
        var parts = domain.Split('.');

        // Add each part of the domain to the query
        foreach (var part in parts)
        {
            var partBytes = Encoding.ASCII.GetBytes(part);

            // Add the length of the part
            queryBytes.Add((byte)partBytes.Length);

            // Add the part itself
            queryBytes.AddRange(partBytes);
        }

        // Add a zero byte to end the domain name
        queryBytes.Add(0);

        return queryBytes.ToArray();
    }
}