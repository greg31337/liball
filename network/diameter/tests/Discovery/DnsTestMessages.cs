using System.Buffers.Binary;
using System.Net;
using System.Text;

namespace diameter.Tests;

internal static class DnsTestMessages
{
    internal static byte[] BuildResponse(
        byte[] query,
        int flags,
        params byte[][] records)
    {
        List<byte> response = new List<byte>();
        response.Add(query[0]);
        response.Add(query[1]);
        response.Add((byte)(flags >> 8));
        response.Add((byte)flags);
        response.Add(0);
        response.Add(1);
        response.Add((byte)(records.Length >> 8));
        response.Add((byte)records.Length);
        response.Add(0);
        response.Add(0);
        response.Add(0);
        response.Add(0);
        response.AddRange(query.AsSpan(12).ToArray());
        foreach (byte[] record in records)
        {
            response.AddRange(record);
        }

        return response.ToArray();
    }

    internal static byte[] BuildRecord(int recordType, uint timeToLive, byte[] data)
    {
        List<byte> record = new List<byte>();
        record.Add(192);
        record.Add(12);
        AddUInt16(record, recordType);
        AddUInt16(record, 1);
        AddUInt32(record, timeToLive);
        AddUInt16(record, data.Length);
        record.AddRange(data);
        return record.ToArray();
    }

    internal static byte[] BuildNaptrData(
        int order,
        int preference,
        string flags,
        string service,
        string expression,
        string replacement)
    {
        List<byte> data = new List<byte>();
        AddUInt16(data, order);
        AddUInt16(data, preference);
        AddCharacterString(data, flags);
        AddCharacterString(data, service);
        AddCharacterString(data, expression);
        data.AddRange(EncodeName(replacement));
        return data.ToArray();
    }

    internal static byte[] BuildSrvData(
        int priority,
        int weight,
        int port,
        string target)
    {
        List<byte> data = new List<byte>();
        AddUInt16(data, priority);
        AddUInt16(data, weight);
        AddUInt16(data, port);
        data.AddRange(EncodeName(target));
        return data.ToArray();
    }

    internal static byte[] EncodeName(string name)
    {
        List<byte> result = new List<byte>();
        foreach (string label in name.TrimEnd('.').Split('.'))
        {
            byte[] bytes = Encoding.ASCII.GetBytes(label);
            result.Add((byte)bytes.Length);
            result.AddRange(bytes);
        }

        result.Add(0);
        return result.ToArray();
    }

    internal static byte[] BuildRecordData(int recordType)
    {
        if (recordType == DnsMessageCodec.NaptrRecordType)
        {
            return BuildNaptrData(10, 20, "S", "AAA+D2T", "", "peer.example");
        }

        if (recordType == DnsMessageCodec.SrvRecordType)
        {
            return BuildSrvData(1, 10, DiameterProtocolConstants.Port, "peer.example");
        }

        if (recordType == DnsMessageCodec.Ipv4RecordType)
        {
            return new byte[] { 192, 0, 2, 10 };
        }

        return IPAddress.Parse("2001:db8::10").GetAddressBytes();
    }

    internal static int ReadQueryType(byte[] query)
    {
        return BinaryPrimitives.ReadUInt16BigEndian(query.AsSpan(query.Length - 4, 2));
    }

    internal static void AddUInt16(List<byte> destination, int value)
    {
        destination.Add((byte)(value >> 8));
        destination.Add((byte)value);
    }

    internal static void AddUInt32(List<byte> destination, uint value)
    {
        destination.Add((byte)(value >> 24));
        destination.Add((byte)(value >> 16));
        destination.Add((byte)(value >> 8));
        destination.Add((byte)value);
    }

    internal static void AddCharacterString(List<byte> destination, string value)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(value);
        destination.Add((byte)bytes.Length);
        destination.AddRange(bytes);
    }
}
