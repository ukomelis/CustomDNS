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
            var message = new DnsQuery(url).ToDnsRequestByteArray();
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

class DnsQuery
{
    private readonly string domain;

    public DnsQuery(string domain)
    {
        this.domain = domain;
    }

    public byte[] ToDnsRequestByteArray()
    {
        // Start with a list to hold all the bytes
        List<byte> queryBytes =
        [
            // Add a transaction ID
            0xAB,
            0xCD,
            // Add flags: standard query, recursion desired
            0x01,
            0x00,
            // Add question count: 1
            0x00,
            0x01,
            // Add answer count: 0
            0x00,
            0x00,
            // Add authority count: 0
            0x00,
            0x00,
            // Add additional count: 0
            0x00,
            0x00,
        ];

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

        // Add type: A
        queryBytes.Add(0x00);
        queryBytes.Add(0x01);

        // Add class: IN
        queryBytes.Add(0x00);
        queryBytes.Add(0x01);

        return queryBytes.ToArray();
    }
}