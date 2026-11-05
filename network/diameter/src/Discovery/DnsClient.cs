using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace diameter;

internal class DnsClient
{
    internal const int MaximumResponseLength = 65535;

    internal IPAddress ServerAddress;
    internal int ServerPort;

    internal DnsClient(IPAddress serverAddress, int serverPort)
    {
        ServerAddress = serverAddress;
        ServerPort = serverPort;
    }

    internal DiameterDnsQueryResult ResolveDnsQuery(
        string name,
        int recordType,
        int timeoutMilliseconds)
    {
        if (timeoutMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException("timeoutMilliseconds");
        }

        int identifier = Random.Shared.Next(0, 65536);
        byte[] query = DnsMessageCodec.EncodeDnsQuery(name, recordType, identifier);
        long deadline = Environment.TickCount64 + timeoutMilliseconds;
        byte[] udpResponse = ExchangeUdpDnsQuery(query, identifier, deadline);
        DnsResponseHeader udpHeader = DnsMessageCodec.ReadDnsResponseHeader(
            udpResponse,
            identifier,
            name,
            recordType);
        byte[] response = udpResponse;
        bool usedTcpFallback = (udpHeader.Flags & 512) != 0;
        if (usedTcpFallback)
        {
            response = ExchangeTcpDnsQuery(query, deadline);
        }

        DiameterDnsQueryResult result = DnsMessageCodec.DecodeDnsResponse(
            response,
            identifier,
            name,
            recordType,
            DateTimeOffset.UtcNow);
        result.UsedTcpFallback = usedTcpFallback;
        return result;
    }

    internal byte[] ExchangeUdpDnsQuery(
        byte[] query,
        int identifier,
        long deadline)
    {
        IPEndPoint endpoint = new IPEndPoint(ServerAddress, ServerPort);
        using (Socket socket = new Socket(
            ServerAddress.AddressFamily,
            SocketType.Dgram,
            ProtocolType.Udp))
        {
            socket.Connect(endpoint);
            int sent = socket.Send(query);
            if (sent != query.Length)
            {
                throw new DiameterPeerException("The complete DNS UDP query was not sent.");
            }

            byte[] receiveBuffer = new byte[MaximumResponseLength];
            while (true)
            {
                Task<int> receiveOperation = socket.ReceiveAsync(
                    new ArraySegment<byte>(receiveBuffer),
                    SocketFlags.None);
                WaitForDnsOperation(
                    receiveOperation,
                    deadline,
                    socket,
                    "The DNS UDP response timed out or failed.");
                int received = receiveOperation.Result;
                if (received < 2)
                {
                    throw new DiameterCodecException("The DNS UDP response is truncated.");
                }

                int responseIdentifier =
                    (receiveBuffer[0] << 8) | receiveBuffer[1];
                if (responseIdentifier == identifier)
                {
                    byte[] response = new byte[received];
                    Buffer.BlockCopy(receiveBuffer, 0, response, 0, received);
                    return response;
                }
            }
        }
    }

    internal byte[] ExchangeTcpDnsQuery(byte[] query, long deadline)
    {
        IPEndPoint endpoint = new IPEndPoint(ServerAddress, ServerPort);
        using (Socket socket = new Socket(
            ServerAddress.AddressFamily,
            SocketType.Stream,
            ProtocolType.Tcp))
        {
            socket.NoDelay = true;
            Task connectOperation = socket.ConnectAsync(endpoint);
            WaitForDnsOperation(
                connectOperation,
                deadline,
                socket,
                "The DNS TCP connection timed out or failed.");

            byte[] frame = new byte[query.Length + 2];
            DnsMessageCodec.WriteDnsUInt16(frame, 0, query.Length);
            Buffer.BlockCopy(query, 0, frame, 2, query.Length);
            SendDnsBytes(socket, frame, deadline);

            byte[] lengthBytes = new byte[2];
            ReceiveDnsBytes(socket, lengthBytes, deadline);
            int responseLength =
                (lengthBytes[0] << 8) | lengthBytes[1];
            if (responseLength < DnsMessageCodec.HeaderLength ||
                responseLength > MaximumResponseLength)
            {
                throw new DiameterCodecException("The DNS TCP response length is invalid.");
            }

            byte[] response = new byte[responseLength];
            ReceiveDnsBytes(socket, response, deadline);
            return response;
        }
    }

    internal void SendDnsBytes(Socket socket, byte[] source, long deadline)
    {
        int offset = 0;
        while (offset < source.Length)
        {
            Task<int> sendOperation = socket.SendAsync(
                new ArraySegment<byte>(source, offset, source.Length - offset),
                SocketFlags.None);
            WaitForDnsOperation(
                sendOperation,
                deadline,
                socket,
                "The DNS TCP send timed out or failed.");
            int sent = sendOperation.Result;
            if (sent <= 0)
            {
                throw new DiameterPeerException("The DNS TCP connection closed while sending.");
            }

            offset = offset + sent;
        }
    }

    internal void ReceiveDnsBytes(Socket socket, byte[] destination, long deadline)
    {
        int offset = 0;
        while (offset < destination.Length)
        {
            Task<int> receiveOperation = socket.ReceiveAsync(
                new ArraySegment<byte>(
                    destination,
                    offset,
                    destination.Length - offset),
                SocketFlags.None);
            WaitForDnsOperation(
                receiveOperation,
                deadline,
                socket,
                "The DNS TCP receive timed out or failed.");
            int received = receiveOperation.Result;
            if (received <= 0)
            {
                throw new DiameterPeerException("The DNS TCP response ended prematurely.");
            }

            offset = offset + received;
        }
    }

    internal void WaitForDnsOperation(
        Task operation,
        long deadline,
        Socket socket,
        string failureMessage)
    {
        int remaining = ReadDnsRemainingMilliseconds(deadline);
        bool completed = remaining > 0 &&
            SpinWait.SpinUntil(() => operation.IsCompleted, remaining);
        if (!completed || !operation.IsCompletedSuccessfully)
        {
            socket.Close();
            throw new DiameterPeerException(failureMessage);
        }
    }

    internal int ReadDnsRemainingMilliseconds(long deadline)
    {
        long remaining = deadline - Environment.TickCount64;
        if (remaining <= 0)
        {
            return 0;
        }

        if (remaining > int.MaxValue)
        {
            return int.MaxValue;
        }

        return (int)remaining;
    }
}
