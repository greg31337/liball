using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace jmap;

internal interface IDnsQueryClient
{
    Task<byte[]> QuerySrvAsync(string queryName, int timeoutMilliseconds, CancellationToken cancellationToken);
}

internal interface IDnsPacketExchange
{
    byte[] ExchangeUdp(byte[] request, IPEndPoint server, int timeoutMilliseconds, CancellationToken cancellationToken);

    byte[] ExchangeTcp(byte[] request, IPEndPoint server, int timeoutMilliseconds, CancellationToken cancellationToken);
}

internal class DnsQueryClient : IDnsQueryClient
{
    internal const int DnsPort = 53;
    internal IDnsPacketExchange Exchange;
    internal IPAddress? DnsServerOverride;

    internal DnsQueryClient()
    {
        Exchange = new SocketDnsPacketExchange();
    }

    public Task<byte[]> QuerySrvAsync(string queryName, int timeoutMilliseconds, CancellationToken cancellationToken)
    {
        return Task.Run(
            () => QuerySrv(queryName, timeoutMilliseconds, cancellationToken),
            cancellationToken);
    }

    internal byte[] QuerySrv(string queryName, int timeoutMilliseconds, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        int transactionId = RandomNumberGenerator.GetInt32(65536);
        byte[] request = BuildQuery(queryName, transactionId);
        IPAddress serverAddress = DnsServerOverride == null ? FindDnsServer() : DnsServerOverride;
        IPEndPoint server = new IPEndPoint(serverAddress, DnsPort);
        Stopwatch elapsed = Stopwatch.StartNew();
        byte[] response = Exchange.ExchangeUdp(request, server, timeoutMilliseconds, cancellationToken);
        ValidateResponseIdentity(response, transactionId);
        if (IsTruncated(response))
        {
            int remaining = timeoutMilliseconds - (int)elapsed.ElapsedMilliseconds;
            if (remaining <= 0)
            {
                throw TransportFailure("The DNS SRV query timed out before its TCP retry.", queryName, null);
            }

            response = Exchange.ExchangeTcp(request, server, remaining, cancellationToken);
            ValidateResponseIdentity(response, transactionId);
            if (IsTruncated(response))
            {
                throw ProtocolFailure("The DNS SRV response remained truncated after its TCP retry.");
            }
        }

        return response;
    }

    internal byte[] BuildQuery(string queryName, int transactionId)
    {
        if (transactionId < 0 || transactionId > 65535)
        {
            throw new ArgumentOutOfRangeException("transactionId");
        }

        using MemoryStream stream = new MemoryStream();
        WriteUnsignedShort(stream, transactionId);
        WriteUnsignedShort(stream, 256);
        WriteUnsignedShort(stream, 1);
        WriteUnsignedShort(stream, 0);
        WriteUnsignedShort(stream, 0);
        WriteUnsignedShort(stream, 0);
        string[] labels = queryName.Split('.');
        foreach (string label in labels)
        {
            byte[] encoded = Encoding.ASCII.GetBytes(label);
            if (encoded.Length == 0 || encoded.Length > 63)
            {
                throw ProtocolFailure("The DNS query contains an invalid label.");
            }

            stream.WriteByte((byte)encoded.Length);
            stream.Write(encoded, 0, encoded.Length);
        }

        stream.WriteByte(0);
        WriteUnsignedShort(stream, DnsSrvParser.SrvRecordType);
        WriteUnsignedShort(stream, DnsSrvParser.InternetClass);
        if (stream.Length > 255 + DnsSrvParser.HeaderLength + 4)
        {
            throw ProtocolFailure("The DNS query name exceeds 255 octets.");
        }

        return stream.ToArray();
    }

    internal void ValidateResponseIdentity(byte[]? response, int transactionId)
    {
        if (response == null || response.Length < DnsSrvParser.HeaderLength)
        {
            throw ProtocolFailure("The DNS response is shorter than its header.");
        }

        int responseId = (response[0] << 8) | response[1];
        if (responseId != transactionId)
        {
            throw ProtocolFailure("The DNS response transaction identifier does not match the query.");
        }
    }

    internal bool IsTruncated(byte[] response)
    {
        return (response[2] & 2) != 0;
    }

    internal IPAddress FindDnsServer()
    {
        foreach (NetworkInterface network in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (network.OperationalStatus == OperationalStatus.Up)
            {
                IPInterfaceProperties properties = network.GetIPProperties();
                foreach (IPAddress address in properties.DnsAddresses)
                {
                    if (address.AddressFamily == AddressFamily.InterNetwork || address.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        return address;
                    }
                }
            }
        }

        throw TransportFailure("No system DNS server is configured for JMAP discovery.", null, null);
    }

    internal void WriteUnsignedShort(Stream stream, int value)
    {
        stream.WriteByte((byte)((value >> 8) & 255));
        stream.WriteByte((byte)(value & 255));
    }

    internal JmapProtocolException ProtocolFailure(string message)
    {
        return new JmapProtocolException(message, "dns", default(System.Text.Json.JsonElement));
    }

    internal JmapTransportException TransportFailure(string message, string? endpoint, System.Exception? innerException)
    {
        return new JmapTransportException(message, endpoint, 0, false, true, null, innerException);
    }
}

internal class SocketDnsPacketExchange : IDnsPacketExchange
{
    internal const int MaximumDnsMessageSize = 65535;
    internal const int PollSliceMilliseconds = 50;

    public byte[] ExchangeUdp(
        byte[] request,
        IPEndPoint server,
        int timeoutMilliseconds,
        CancellationToken cancellationToken)
    {
        using Socket socket = new Socket(server.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        socket.Connect(server);
        socket.Send(request);
        byte[] buffer = new byte[MaximumDnsMessageSize];
        int received = ReceiveAvailable(socket, buffer, timeoutMilliseconds, cancellationToken);
        byte[] response = new byte[received];
        Buffer.BlockCopy(buffer, 0, response, 0, received);
        return response;
    }

    public byte[] ExchangeTcp(
        byte[] request,
        IPEndPoint server,
        int timeoutMilliseconds,
        CancellationToken cancellationToken)
    {
        Stopwatch elapsed = Stopwatch.StartNew();
        using Socket socket = new Socket(server.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        Task connection = socket.ConnectAsync(server);
        if (!connection.Wait(timeoutMilliseconds, cancellationToken))
        {
            throw TransportFailure("The DNS TCP connection timed out.", server.ToString(), null);
        }

        connection.GetAwaiter().GetResult();
        byte[] lengthPrefix = new byte[2];
        lengthPrefix[0] = (byte)((request.Length >> 8) & 255);
        lengthPrefix[1] = (byte)(request.Length & 255);
        SendAll(socket, lengthPrefix, Remaining(timeoutMilliseconds, elapsed), cancellationToken);
        SendAll(socket, request, Remaining(timeoutMilliseconds, elapsed), cancellationToken);
        byte[] responseLength = new byte[2];
        ReceiveAll(socket, responseLength, Remaining(timeoutMilliseconds, elapsed), cancellationToken);
        int length = (responseLength[0] << 8) | responseLength[1];
        if (length < DnsSrvParser.HeaderLength)
        {
            throw ProtocolFailure("The length-prefixed DNS TCP response is too short.");
        }

        byte[] response = new byte[length];
        ReceiveAll(socket, response, Remaining(timeoutMilliseconds, elapsed), cancellationToken);
        return response;
    }

    internal int ReceiveAvailable(
        Socket socket,
        byte[] buffer,
        int timeoutMilliseconds,
        CancellationToken cancellationToken)
    {
        Stopwatch elapsed = Stopwatch.StartNew();
        while (elapsed.ElapsedMilliseconds < timeoutMilliseconds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int remaining = timeoutMilliseconds - (int)elapsed.ElapsedMilliseconds;
            int wait = remaining < PollSliceMilliseconds ? remaining : PollSliceMilliseconds;
            if (socket.Poll(wait * 1000, SelectMode.SelectRead))
            {
                return socket.Receive(buffer);
            }
        }

        throw TransportFailure("The DNS UDP query timed out.", socket.RemoteEndPoint == null ? null : socket.RemoteEndPoint.ToString(), null);
    }

    internal void SendAll(Socket socket, byte[] data, int timeoutMilliseconds, CancellationToken cancellationToken)
    {
        Stopwatch elapsed = Stopwatch.StartNew();
        int offset = 0;
        while (offset < data.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int remaining = timeoutMilliseconds - (int)elapsed.ElapsedMilliseconds;
            int wait = remaining < PollSliceMilliseconds ? remaining : PollSliceMilliseconds;
            if (remaining <= 0)
            {
                throw TransportFailure("The DNS TCP send timed out.", socket.RemoteEndPoint == null ? null : socket.RemoteEndPoint.ToString(), null);
            }

            if (socket.Poll(wait * 1000, SelectMode.SelectWrite))
            {
                int sent = socket.Send(data, offset, data.Length - offset, SocketFlags.None);
                if (sent == 0)
                {
                    throw TransportFailure("The DNS TCP connection closed while sending a query.", socket.RemoteEndPoint == null ? null : socket.RemoteEndPoint.ToString(), null);
                }

                offset = offset + sent;
            }
        }
    }

    internal void ReceiveAll(Socket socket, byte[] data, int timeoutMilliseconds, CancellationToken cancellationToken)
    {
        Stopwatch elapsed = Stopwatch.StartNew();
        int offset = 0;
        while (offset < data.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int remaining = timeoutMilliseconds - (int)elapsed.ElapsedMilliseconds;
            int wait = remaining < PollSliceMilliseconds ? remaining : PollSliceMilliseconds;
            if (remaining <= 0)
            {
                throw TransportFailure("The DNS TCP receive timed out.", socket.RemoteEndPoint == null ? null : socket.RemoteEndPoint.ToString(), null);
            }

            if (socket.Poll(wait * 1000, SelectMode.SelectRead))
            {
                int received = socket.Receive(data, offset, data.Length - offset, SocketFlags.None);
                if (received == 0)
                {
                    throw TransportFailure("The DNS TCP connection closed before the response was complete.", socket.RemoteEndPoint == null ? null : socket.RemoteEndPoint.ToString(), null);
                }

                offset = offset + received;
            }
        }
    }

    internal int Remaining(int timeoutMilliseconds, Stopwatch elapsed)
    {
        int remaining = timeoutMilliseconds - (int)elapsed.ElapsedMilliseconds;
        if (remaining <= 0)
        {
            throw TransportFailure("The DNS TCP query timed out.", null, null);
        }

        return remaining;
    }

    internal JmapProtocolException ProtocolFailure(string message)
    {
        return new JmapProtocolException(message, "dns", default(System.Text.Json.JsonElement));
    }

    internal JmapTransportException TransportFailure(string message, string? endpoint, System.Exception? innerException)
    {
        return new JmapTransportException(message, endpoint, 0, false, true, null, innerException);
    }
}
