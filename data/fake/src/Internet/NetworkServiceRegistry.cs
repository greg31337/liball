using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace fake;

internal class NetworkServiceRegistry
{
    internal string SourceVersion = string.Empty;
    NetworkServiceRecord[] Values;
    static readonly string[] TransportValues = new string[] { "dccp", "sctp", "tcp", "udp" };

    internal NetworkServiceRegistry(
        string sourceVersion,
        NetworkServiceRecord[] values)
    {
        Guard.NotNullOrWhiteSpace(sourceVersion, "sourceVersion");
        Guard.NotNull(values, "values");
        if (values.Length == 0)
        {
            throw FakeException.Create("Bundled IANA service metadata contains no assigned single-port records.");
        }
        SourceVersion = sourceVersion;
        Values = new NetworkServiceRecord[values.Length];
        Array.Copy(values, Values, values.Length);
    }

    internal int Port(FakeRandom random, PortKind kind)
    {
        switch (kind)
        {
            case PortKind.System:
                return random.IntFromMinimumAndMaximum(0, 1024);
            case PortKind.Registered:
                return random.IntFromMinimumAndMaximum(1024, 49152);
            case PortKind.Dynamic:
                return random.IntFromMinimumAndMaximum(49152, 65536);
            default:
                throw new ArgumentOutOfRangeException(
                    "kind",
                    kind,
                    "kind is not a supported port-number range.");
        }
    }

    internal NetworkServiceRecord Choose(
        FakeRandom random,
        string? transport,
        int maximumOutputSize)
    {
        return Copy(ChooseEntry(random, transport), maximumOutputSize);
    }

    internal string ChooseService(
        FakeRandom random,
        string? transport,
        int maximumOutputSize)
    {
        string result = ChooseEntry(random, transport).Service;
        Guard.OutputSize(result.Length, maximumOutputSize, "service");
        return result;
    }

    NetworkServiceRecord ChooseEntry(FakeRandom random, string? transport)
    {
        string? normalized = NormalizeTransport(transport);
        int count = 0;
        int index = 0;
        while (index < Values.Length)
        {
            if (normalized == null || Values[index].Transport == normalized)
            {
                count = count + 1;
            }
            index = index + 1;
        }
        if (count == 0)
        {
            throw new ArgumentException(
                $"transport '{transport}' has no assigned single-port service records.",
                "transport");
        }
        int selected = random.IntFromMaximumExclusive(count);
        index = 0;
        while (index < Values.Length)
        {
            if (normalized == null || Values[index].Transport == normalized)
            {
                if (selected == 0)
                {
                    return Values[index];
                }
                selected = selected - 1;
            }
            index = index + 1;
        }
        throw FakeException.Create("An assigned IANA service record could not be selected.");
    }

    internal NetworkServiceRecord Find(
        string service,
        string transport,
        int maximumOutputSize)
    {
        Guard.NotNullOrWhiteSpace(service, "service");
        Guard.NotNullOrWhiteSpace(transport, "transport");
        string normalizedService = service.ToLowerInvariant();
        string? normalizedTransport = NormalizeTransport(transport);
        int index = 0;
        while (index < Values.Length)
        {
            NetworkServiceRecord value = Values[index];
            if (value.Service == normalizedService &&
                value.Transport == normalizedTransport)
            {
                return Copy(value, maximumOutputSize);
            }
            index = index + 1;
        }
        throw new ArgumentException(
            $"service '{service}' is not assigned for transport '{transport}' in the bundled IANA snapshot.",
            "service");
    }

    internal string Protocol(FakeRandom random, int maximumOutputSize)
    {
        string result = TransportValues[random.IntFromMaximumExclusive(TransportValues.Length)];
        Guard.OutputSize(result.Length, maximumOutputSize, "protocol");
        return result;
    }

    internal string[] Protocols(int maximumCount, int maximumOutputSize)
    {
        Guard.Count(TransportValues.Length, maximumCount, "protocols");
        int total = 0;
        string[] result = new string[TransportValues.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = TransportValues[index];
            total = total + result[index].Length;
            index = index + 1;
        }
        Guard.OutputSize(total, maximumOutputSize, "protocols");
        return result;
    }

    static NetworkServiceRecord Copy(
        NetworkServiceRecord value,
        int maximumOutputSize)
    {
        Guard.OutputSize(value.Service.Length, maximumOutputSize, "service");
        Guard.OutputSize(value.Transport.Length, maximumOutputSize, "service");
        Guard.OutputSize(value.RegistryVersion.Length, maximumOutputSize, "service");
        NetworkServiceRecord result = new NetworkServiceRecord();
        result.Service = value.Service;
        result.Port = value.Port;
        result.Transport = value.Transport;
        result.PortKind = value.PortKind;
        result.RegistryVersion = value.RegistryVersion;
        return result;
    }

    static string? NormalizeTransport(string? transport)
    {
        if (transport == null)
        {
            return null;
        }
        Guard.NotNullOrWhiteSpace(transport, "transport");
        string normalized = transport.ToLowerInvariant();
        int index = 0;
        while (index < TransportValues.Length)
        {
            if (TransportValues[index] == normalized)
            {
                return normalized;
            }
            index = index + 1;
        }
        throw new ArgumentException(
            $"transport '{transport}' is not supported; use dccp, sctp, tcp, or udp.",
            "transport");
    }
}

internal static class NetworkServiceRegistryStore
{
    static readonly NetworkServiceRegistry?[] Value = new NetworkServiceRegistry?[1];
    static readonly object Sync = new object();

    internal static NetworkServiceRegistry Current()
    {
        lock (Sync)
        {
            NetworkServiceRegistry? current = Value[0];
            if (current == null)
            {
                current = Load();
                Value[0] = current;
            }
            return current;
        }
    }

    static NetworkServiceRegistry Load()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        Stream? stream = assembly.GetManifestResourceStream(
            "fake.Data.standards.ports.json");
        if (stream == null)
        {
            throw FakeException.Create("Embedded IANA service metadata resource is missing.");
        }
        using (stream)
        {
            using (JsonDocument document = JsonDocument.Parse(stream))
            {
                JsonElement root = document.RootElement;
                string sourceVersion = JsonText.Required(
                    root.GetProperty("source").GetProperty("version"),
                    "$.source.version");
                List<NetworkServiceRecord> values = new List<NetworkServiceRecord>();
                foreach (JsonElement item in root.GetProperty("records").EnumerateArray())
                {
                    string state = JsonText.Required(item.GetProperty("state"), "$.records[].state");
                    string service = JsonText.Required(item.GetProperty("service"), "$.records[].service");
                    string portText = JsonText.Required(item.GetProperty("port"), "$.records[].port");
                    string transport = JsonText.Required(item.GetProperty("transport"), "$.records[].transport");
                    int port = ParsePort(portText);
                    if (state == "assigned" &&
                        service.Length != 0 &&
                        port >= 0 &&
                        AllowedTransport(transport))
                    {
                        NetworkServiceRecord value = new NetworkServiceRecord();
                        value.Service = service.ToLowerInvariant();
                        value.Port = port;
                        value.Transport = transport;
                        value.PortKind = Kind(port);
                        value.RegistryVersion = sourceVersion;
                        values.Add(value);
                    }
                }
                return new NetworkServiceRegistry(
                    sourceVersion,
                    values.ToArray());
            }
        }
    }

    static bool AllowedTransport(string transport)
    {
        return transport == "dccp" ||
            transport == "sctp" ||
            transport == "tcp" ||
            transport == "udp";
    }

    static int ParsePort(string value)
    {
        if (value.Length == 0 || value.Length > 5)
        {
            return -1;
        }
        int result = 0;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < '0' || character > '9')
            {
                return -1;
            }
            result = result * 10 + character - '0';
            index = index + 1;
        }
        if (result > 65535)
        {
            return -1;
        }
        return result;
    }

    static PortKind Kind(int port)
    {
        if (port <= 1023)
        {
            return PortKind.System;
        }
        if (port <= 49151)
        {
            return PortKind.Registered;
        }
        return PortKind.Dynamic;
    }
}
