namespace sip.Tests;

class SipHarnessDnsScript : IDisposable
{
    System.Net.Sockets.UdpClient server;
    CancellationTokenSource cancellation = new CancellationTokenSource();
    Task worker;
    Dictionary<string, System.Net.IPAddress> addresses =
        new Dictionary<string, System.Net.IPAddress>(
            StringComparer.OrdinalIgnoreCase);
    internal int ResponseCount;

    internal SipHarnessDnsScript()
    {
        server = new System.Net.Sockets.UdpClient(
            new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 0));
        worker = Task.Run(Serve);
    }

    internal void AddAddress(string name, string address)
    {
        addresses[name.TrimEnd('.')] = System.Net.IPAddress.Parse(address);
    }

    internal SipDnsOptions CreateOptions()
    {
        SipEndpointAddress nameServer = new SipEndpointAddress();
        nameServer.Host = "127.0.0.1";
        nameServer.Port = ((System.Net.IPEndPoint)
            server.Client.LocalEndPoint!).Port;
        nameServer.Transport = SipTransportKind.Udp;
        SipDnsOptions result = new SipDnsOptions();
        result.NameServers = new SipEndpointAddress[] { nameServer };
        result.MaxAttempts = 1;
        result.TimeoutMilliseconds = 1000;
        result.MaxRecordCount = 16;
        return result;
    }

    public void Dispose()
    {
        cancellation.Cancel();
        server.Dispose();
        try
        {
            worker.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        cancellation.Dispose();
    }

    async Task Serve()
    {
        while (!cancellation.IsCancellationRequested)
        {
            System.Net.Sockets.UdpReceiveResult query;
            try
            {
                query = await server.ReceiveAsync(cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            byte[] response = CreateResponse(query.Buffer);
            await server.SendAsync(response, query.RemoteEndPoint,
                cancellation.Token);
            ResponseCount++;
        }
    }

    byte[] CreateResponse(byte[] query)
    {
        string name = ReadQuestionName(query);
        int type = ReadQuestionType(query);
        System.Net.IPAddress? address = addresses.GetValueOrDefault(name);
        byte[] addressBytes = address == null
            ? Array.Empty<byte>() : address.GetAddressBytes();
        bool matched = type == 1 && addressBytes.Length == 4
            || type == 28 && addressBytes.Length == 16;
        List<byte> response = new List<byte>();
        response.Add(query[0]);
        response.Add(query[1]);
        AddUInt16(response, 0x8180);
        AddUInt16(response, 1);
        AddUInt16(response, matched ? 1 : 0);
        AddUInt16(response, 0);
        AddUInt16(response, 0);
        response.AddRange(query.Skip(12));
        if (matched)
        {
            response.Add(0xc0);
            response.Add(0x0c);
            AddUInt16(response, type);
            AddUInt16(response, 1);
            AddUInt32(response, 60);
            AddUInt16(response, addressBytes.Length);
            response.AddRange(addressBytes);
        }
        return response.ToArray();
    }

    static string ReadQuestionName(byte[] query)
    {
        List<string> labels = new List<string>();
        int offset = 12;
        while (offset < query.Length && query[offset] != 0)
        {
            int length = query[offset];
            offset++;
            labels.Add(System.Text.Encoding.ASCII.GetString(
                query, offset, length));
            offset += length;
        }
        return string.Join(".", labels);
    }

    static int ReadQuestionType(byte[] query)
    {
        int offset = 12;
        while (offset < query.Length && query[offset] != 0)
        {
            offset += query[offset] + 1;
        }
        offset++;
        return query[offset] << 8 | query[offset + 1];
    }

    static void AddUInt16(List<byte> bytes, int value)
    {
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)value);
    }

    static void AddUInt32(List<byte> bytes, uint value)
    {
        bytes.Add((byte)(value >> 24));
        bytes.Add((byte)(value >> 16));
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)value);
    }
}
