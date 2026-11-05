using System;
using System.Collections.Generic;
using System.Globalization;

namespace diameter;

internal static class SnaptrMatcher
{
    internal const int NoResolution = 0;
    internal const int AddressResolution = 1;
    internal const int ServiceResolution = 2;

    internal static List<DiameterNaptrRecord> SelectCompatibleNaptrRecords(
        List<DiameterNaptrRecord> records,
        DiameterDiscoveryRequest request)
    {
        List<DiameterNaptrRecord> result = new List<DiameterNaptrRecord>();
        bool extendedMode = HasExtendedDiameterService(records);
        string expectedService = CreateDiameterApplicationService(request.Application);
        string expectedProtocol = CreateDiameterProtocolTag(request);
        int index = 0;
        while (index < records.Count)
        {
            DiameterNaptrRecord record = records[index];
            if (record != null && DoesNaptrRecordMatch(
                record,
                expectedService,
                expectedProtocol,
                extendedMode))
            {
                InsertNaptrRecord(result, record);
            }

            index = index + 1;
        }

        return result;
    }

    internal static bool HasExtendedDiameterService(List<DiameterNaptrRecord> records)
    {
        int index = 0;
        while (index < records.Count)
        {
            DiameterNaptrRecord record = records[index];
            if (record != null)
            {
                string service = ReadApplicationService(record.Service);
                if (IsValidExtendedDiameterService(service))
                {
                    return true;
                }
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool DoesNaptrRecordMatch(
        DiameterNaptrRecord record,
        string expectedService,
        string expectedProtocol,
        bool extendedMode)
    {
        if (record.Order < 0 || record.Order > 65535 ||
            record.Preference < 0 || record.Preference > 65535 ||
            !string.IsNullOrEmpty(record.RegularExpression) ||
            string.IsNullOrWhiteSpace(record.Replacement) ||
            ReadNaptrResolution(record.Flags) == NoResolution ||
            string.IsNullOrEmpty(expectedProtocol))
        {
            return false;
        }

        string applicationService = ReadApplicationService(record.Service);
        if (extendedMode && !string.Equals(
            applicationService,
            expectedService,
            StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!extendedMode && !string.Equals(
            applicationService,
            "aaa",
            StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return ContainsApplicationProtocol(record.Service, expectedProtocol);
    }

    internal static string CreateDiameterApplicationService(
        DiameterApplicationId application)
    {
        if (application == null)
        {
            throw new ArgumentException("A Diameter application is required.");
        }

        return "aaa+ap" + application.Id.ToString(CultureInfo.InvariantCulture);
    }

    internal static string CreateDiameterProtocolTag(DiameterDiscoveryRequest request)
    {
        if (request.Transport == DiameterTransportKind.Tcp)
        {
            if (request.Security == DiameterSecurityMode.Tls)
            {
                return "diameter.tls.tcp";
            }

            if (request.Security == DiameterSecurityMode.None ||
                request.Security == DiameterSecurityMode.Ipsec)
            {
                return "diameter.tcp";
            }
        }

        if (request.Transport == DiameterTransportKind.Sctp)
        {
            if (request.Security == DiameterSecurityMode.Dtls)
            {
                return "diameter.dtls.sctp";
            }

            if (request.Security == DiameterSecurityMode.None ||
                request.Security == DiameterSecurityMode.Ipsec)
            {
                return "diameter.sctp";
            }
        }

        return string.Empty;
    }

    internal static string CreateDiameterSrvFallbackName(
        DiameterDiscoveryRequest request)
    {
        string realm = DnsMessageCodec.NormalizeDnsQueryName(request.Realm.Value);
        if (request.Transport == DiameterTransportKind.Tcp)
        {
            if (request.Security == DiameterSecurityMode.Tls)
            {
                return "_diameters._tcp." + realm;
            }

            return "_diameter._tcp." + realm;
        }

        if (request.Transport == DiameterTransportKind.Sctp)
        {
            if (request.Security == DiameterSecurityMode.Dtls)
            {
                return "_diameters._sctp." + realm;
            }

            return "_diameter._sctp." + realm;
        }

        return string.Empty;
    }

    internal static int ReadNaptrResolution(string flags)
    {
        if (string.Equals(flags, "A", StringComparison.OrdinalIgnoreCase))
        {
            return AddressResolution;
        }

        if (string.Equals(flags, "S", StringComparison.OrdinalIgnoreCase))
        {
            return ServiceResolution;
        }

        return NoResolution;
    }

    internal static string ReadApplicationService(string service)
    {
        if (string.IsNullOrEmpty(service))
        {
            return string.Empty;
        }

        int delimiter = service.IndexOf(':');
        if (delimiter < 0)
        {
            return service;
        }

        return service.Substring(0, delimiter);
    }

    internal static bool ContainsApplicationProtocol(
        string service,
        string expectedProtocol)
    {
        string[] fields = service.Split(':');
        int index = 1;
        while (index < fields.Length)
        {
            if (string.Equals(
                fields[index],
                expectedProtocol,
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool IsValidExtendedDiameterService(string service)
    {
        const string prefix = "aaa+ap";
        if (!service.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        int digitCount = service.Length - prefix.Length;
        if (digitCount <= 0 || digitCount > 10 ||
            (digitCount > 1 && service[prefix.Length] == '0'))
        {
            return false;
        }

        ulong value = 0;
        int index = prefix.Length;
        while (index < service.Length)
        {
            char digit = service[index];
            if (digit < '0' || digit > '9')
            {
                return false;
            }

            value = (value * 10UL) + (uint)(digit - '0');
            if (value > uint.MaxValue)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static void InsertNaptrRecord(
        List<DiameterNaptrRecord> records,
        DiameterNaptrRecord record)
    {
        int index = 0;
        while (index < records.Count &&
            (records[index].Order < record.Order ||
            (records[index].Order == record.Order &&
            records[index].Preference <= record.Preference)))
        {
            index = index + 1;
        }

        records.Insert(index, record);
    }
}
