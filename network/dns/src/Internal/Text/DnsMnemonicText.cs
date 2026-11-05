using System;
using System.Globalization;

namespace dns;

internal static class DnsMnemonicText
{
    internal static DnsTextNumberResult ParseClass(
        string text,
        bool useMnemonic)
    {
        string normalized = text.ToUpperInvariant();
        if (useMnemonic)
        {
            switch (normalized)
            {
                case "IN": return CodeSuccess(1);
                case "CH": return CodeSuccess(3);
                case "CHAOS": return CodeSuccess(3);
                case "HS": return CodeSuccess(4);
                case "HESIOD": return CodeSuccess(4);
                case "NONE": return CodeSuccess(254);
                case "ANY": return CodeSuccess(255);
            }
        }

        if (normalized.StartsWith("CLASS", StringComparison.Ordinal) &&
            normalized.Length > 5)
        {
            return DnsTextNumber.ParseDecimal(
                normalized.Substring(5), 65535, "class");
        }

        return DnsTextNumber.Failure(
            "class", "The DNS class mnemonic is not recognized.", 0);
    }

    internal static DnsTextNumberResult ParseRecordType(
        string text,
        bool useMnemonic)
    {
        string normalized = text.ToUpperInvariant();
        if (normalized.StartsWith("TYPE", StringComparison.Ordinal) &&
            normalized.Length > 4)
        {
            return DnsTextNumber.ParseDecimal(
                normalized.Substring(4), 65535, "recordType");
        }

        if (useMnemonic)
        {
            int value = ReadRecordTypeMnemonic(normalized);
            if (value >= 0)
            {
                return CodeSuccess(value);
            }
        }

        return DnsTextNumber.Failure(
            "recordType", "The DNS record-type mnemonic is not recognized.", 0);
    }

    internal static string FormatClass(int value, bool useMnemonic)
    {
        if (useMnemonic)
        {
            switch (value)
            {
                case 1: return "IN";
                case 3: return "CH";
                case 4: return "HS";
                case 254: return "NONE";
                case 255: return "ANY";
            }
        }

        return "CLASS" + value.ToString(CultureInfo.InvariantCulture);
    }

    internal static string FormatRecordType(int value, bool useMnemonic)
    {
        if (useMnemonic &&
            RecordDataModelCatalog.ReadRecordDataModel(value) !=
                RecordDataModel.Unassigned)
        {
            if (value == 23)
            {
                return "NSAP-PTR";
            }

            if (value == 261)
            {
                return "RESINFO";
            }

            return ((DnsRecordType)value).ToString().ToUpperInvariant();
        }

        return "TYPE" + value.ToString(CultureInfo.InvariantCulture);
    }

    internal static string FormatNamedValue(
        int value,
        string enumText,
        string numericPrefix)
    {
        if (enumText.Length > 0 &&
            enumText[0] != '-' &&
            (enumText[0] < '0' || enumText[0] > '9'))
        {
            return enumText.ToUpperInvariant().Replace(", ", " ");
        }

        return numericPrefix + value.ToString(CultureInfo.InvariantCulture);
    }

    static DnsTextNumberResult CodeSuccess(int value)
    {
        DnsTextNumberResult result = new DnsTextNumberResult();
        result.Success = true;
        result.Value = value;
        return result;
    }

    static int ReadRecordTypeMnemonic(string text)
    {
        switch (text)
        {
            case "RESERVED": return 0;
            case "A": return 1;
            case "NS": return 2;
            case "MD": return 3;
            case "MF": return 4;
            case "CNAME": return 5;
            case "SOA": return 6;
            case "MB": return 7;
            case "MG": return 8;
            case "MR": return 9;
            case "NULL": return 10;
            case "WKS": return 11;
            case "PTR": return 12;
            case "HINFO": return 13;
            case "MINFO": return 14;
            case "MX": return 15;
            case "TXT": return 16;
            case "RP": return 17;
            case "AFSDB": return 18;
            case "X25": return 19;
            case "ISDN": return 20;
            case "RT": return 21;
            case "NSAP": return 22;
            case "NSAP-PTR": return 23;
            case "SIG": return 24;
            case "KEY": return 25;
            case "PX": return 26;
            case "GPOS": return 27;
            case "AAAA": return 28;
            case "LOC": return 29;
            case "NXT": return 30;
            case "EID": return 31;
            case "NIMLOC": return 32;
            case "SRV": return 33;
            case "ATMA": return 34;
            case "NAPTR": return 35;
            case "KX": return 36;
            case "CERT": return 37;
            case "A6": return 38;
            case "DNAME": return 39;
            case "SINK": return 40;
            case "OPT": return 41;
            case "APL": return 42;
            case "DS": return 43;
            case "SSHFP": return 44;
            case "IPSECKEY": return 45;
            case "RRSIG": return 46;
            case "NSEC": return 47;
            case "DNSKEY": return 48;
            case "DHCID": return 49;
            case "NSEC3": return 50;
            case "NSEC3PARAM": return 51;
            case "TLSA": return 52;
            case "SMIMEA": return 53;
            case "HIP": return 55;
            case "NINFO": return 56;
            case "RKEY": return 57;
            case "TALINK": return 58;
            case "CDS": return 59;
            case "CDNSKEY": return 60;
            case "OPENPGPKEY": return 61;
            case "CSYNC": return 62;
            case "ZONEMD": return 63;
            case "SVCB": return 64;
            case "HTTPS": return 65;
            case "DSYNC": return 66;
            case "HHIT": return 67;
            case "BRID": return 68;
            case "UNECE": return 69;
            case "ISO": return 70;
            case "SPF": return 99;
            case "UINFO": return 100;
            case "UID": return 101;
            case "GID": return 102;
            case "UNSPEC": return 103;
            case "NID": return 104;
            case "L32": return 105;
            case "L64": return 106;
            case "LP": return 107;
            case "EUI48": return 108;
            case "EUI64": return 109;
            case "NXNAME": return 128;
            case "TKEY": return 249;
            case "TSIG": return 250;
            case "IXFR": return 251;
            case "AXFR": return 252;
            case "MAILB": return 253;
            case "MAILA": return 254;
            case "ANY": return 255;
            case "URI": return 256;
            case "CAA": return 257;
            case "AVC": return 258;
            case "DOA": return 259;
            case "AMTRELAY": return 260;
            case "RESINFO": return 261;
            case "WALLET": return 262;
            case "CLA": return 263;
            case "IPN": return 264;
            case "TA": return 32768;
            case "DLV": return 32769;
            default: return -1;
        }
    }
}
