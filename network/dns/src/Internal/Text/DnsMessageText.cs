using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace dns;

internal static class DnsMessageText
{
    internal static DnsTextResult FormatMessage(
        DnsMessage message,
        DnsTextOptions options)
    {
        if (message == null || options == null || message.Header == null ||
            message.Questions == null || message.Answers == null ||
            message.Authorities == null || message.Additionals == null)
        {
            return DnsTextOperations.Failure(
                DnsDiagnosticCode.ValueOutOfRange,
                "message",
                "A complete DNS message and text options are required.",
                0);
        }

        if (options.IndentSize < 0 || options.MaximumLineLength <= 0)
        {
            return DnsTextOperations.Failure(
                DnsDiagnosticCode.ValueOutOfRange,
                "options",
                "Text indentation and line-length limits must be positive.",
                0);
        }

        StringBuilder builder = new StringBuilder();
        if (options.IncludeHeader)
        {
            AppendBlock(builder, FormatHeader(message));
        }

        List<DnsResourceRecord> optRecords = FindOptRecords(
            message.Additionals);
        int optIndex = 0;
        while (optIndex < optRecords.Count)
        {
            DnsTextResult opt = FormatEdnsRecord(
                optRecords[optIndex], options);
            if (!opt.Success) return opt;
            AppendBlock(builder, opt.Text);
            optIndex = optIndex + 1;
        }

        DnsTextResult questions = FormatQuestionSection(
            message.Questions, options);
        if (!questions.Success) return questions;
        AppendBlock(builder, questions.Text);
        DnsTextResult answers = FormatRecordSection(
            "ANSWER", message.Answers, false, options);
        if (!answers.Success) return answers;
        AppendBlock(builder, answers.Text);
        DnsTextResult authorities = FormatRecordSection(
            "AUTHORITY", message.Authorities, false, options);
        if (!authorities.Success) return authorities;
        AppendBlock(builder, authorities.Text);
        DnsTextResult additionals = FormatRecordSection(
            "ADDITIONAL", message.Additionals, true, options);
        if (!additionals.Success) return additionals;
        AppendBlock(builder, additionals.Text);

        DnsTextResult result = DnsTextOperations.SuccessText(
            builder.ToString());
        result.Message = message;
        return result;
    }

    static string FormatHeader(DnsMessage message)
    {
        DnsMessageHeader header = message.Header;
        int opcode = DnsHeaderCodec.ResolveCode(
            header.OpcodeValue, (int)header.Opcode);
        int responseCode = ReadEffectiveResponseCode(message);
        string opcodeText = DnsMnemonicText.FormatNamedValue(
            opcode, ((DnsOpcode)opcode).ToString(), "OPCODE");
        string statusText = DnsMnemonicText.FormatNamedValue(
            responseCode,
            ((DnsResponseCode)responseCode).ToString(),
            "RCODE");
        int flagValue = header.FlagsValue;
        if (flagValue == 0) flagValue = (int)header.Flags;
        string first = ";; ->>HEADER<<- opcode: " + opcodeText +
            ", status: " + statusText +
            ", id: " + header.Identifier.ToString(
                CultureInfo.InvariantCulture);
        string second = ";; flags: " + FormatHeaderFlagTokens(flagValue) +
            "; QUERY: " + message.Questions.Count.ToString(
                CultureInfo.InvariantCulture) +
            ", ANSWER: " + message.Answers.Count.ToString(
                CultureInfo.InvariantCulture) +
            ", AUTHORITY: " + message.Authorities.Count.ToString(
                CultureInfo.InvariantCulture) +
            ", ADDITIONAL: " + message.Additionals.Count.ToString(
                CultureInfo.InvariantCulture);
        return first + "\n" + second;
    }

    static DnsTextResult FormatQuestionSection(
        List<DnsQuestion> questions,
        DnsTextOptions options)
    {
        List<string> lines = new List<string>();
        int index = 0;
        while (index < questions.Count)
        {
            DnsTextResult formatted = DnsQuestionText.FormatQuestion(
                questions[index], options);
            if (!formatted.Success) return formatted;
            lines.Add(";" + formatted.Text);
            index = index + 1;
        }

        if (options.Canonical) SortLines(lines);
        return BuildSection("QUESTION", lines, options);
    }

    static DnsTextResult FormatRecordSection(
        string name,
        List<DnsResourceRecord> records,
        bool omitOpt,
        DnsTextOptions options)
    {
        List<string> lines = new List<string>();
        int index = 0;
        while (index < records.Count)
        {
            DnsResourceRecord record = records[index];
            int typeValue = DnsHeaderCodec.ResolveCode(
                record.RecordTypeValue, (int)record.RecordType);
            if (!omitOpt || typeValue != 41)
            {
                DnsTextResult formatted =
                    DnsResourceRecordText.FormatResourceRecord(
                        record, options);
                if (!formatted.Success) return formatted;
                lines.Add(formatted.Text);
            }

            index = index + 1;
        }

        if (options.Canonical) SortLines(lines);
        return BuildSection(name, lines, options);
    }

    static DnsTextResult BuildSection(
        string name,
        List<string> lines,
        DnsTextOptions options)
    {
        if (!options.IncludeSectionNames && lines.Count == 0)
        {
            return DnsTextOperations.SuccessText(string.Empty);
        }

        StringBuilder builder = new StringBuilder();
        if (options.IncludeSectionNames)
        {
            builder.Append(";; ");
            builder.Append(name);
            builder.Append(" SECTION:");
        }

        int index = 0;
        while (index < lines.Count)
        {
            if (builder.Length > 0) builder.Append('\n');
            builder.Append(lines[index]);
            index = index + 1;
        }

        return DnsTextOperations.SuccessText(builder.ToString());
    }

    static DnsTextResult FormatEdnsRecord(
        DnsResourceRecord record,
        DnsTextOptions options)
    {
        DnsEdnsData? edns = null;
        if (record.Data != null) edns = record.Data.Edns;
        int udpPayloadSize = DnsHeaderCodec.ResolveCode(
            record.ClassValue, (int)record.Class);
        int version = (int)((record.TimeToLive >> 16) & 255);
        int extendedResponseCode = (int)(record.TimeToLive >> 24);
        int flags = (int)(record.TimeToLive & 65535);
        if (edns != null)
        {
            udpPayloadSize = edns.UdpPayloadSize;
            version = edns.Version;
            extendedResponseCode = edns.ExtendedResponseCode;
            flags = EdnsRecordDataCodec.ReadEffectiveFlags(edns);
        }

        StringBuilder builder = new StringBuilder();
        if (options.IncludeSectionNames)
        {
            builder.Append(";; OPT PSEUDOSECTION:\n");
        }

        builder.Append("; EDNS: version: ");
        builder.Append(version.ToString(CultureInfo.InvariantCulture));
        builder.Append(", flags: ");
        builder.Append(FormatEdnsFlagTokens(flags));
        builder.Append("; udp: ");
        builder.Append(udpPayloadSize.ToString(CultureInfo.InvariantCulture));
        if (extendedResponseCode != 0)
        {
            builder.Append("; ext-rcode: ");
            builder.Append(extendedResponseCode.ToString(
                CultureInfo.InvariantCulture));
        }

        if (edns != null)
        {
            List<string> optionLines = new List<string>();
            int index = 0;
            while (index < edns.Options.Count)
            {
                optionLines.Add(FormatEdnsOption(
                    edns.Options[index], options));
                index = index + 1;
            }

            if (options.Canonical) SortLines(optionLines);
            index = 0;
            while (index < optionLines.Count)
            {
                builder.Append('\n');
                builder.Append(optionLines[index]);
                index = index + 1;
            }
        }
        else if (record.RawData.Length > 0)
        {
            builder.Append("\n; OPT-RDATA: ");
            builder.Append(DnsTextEncoding.FormatHex(record.RawData));
        }

        return DnsTextOperations.SuccessText(builder.ToString());
    }

    static string FormatEdnsOption(
        DnsEdnsOption option,
        DnsTextOptions options)
    {
        int code = DnsHeaderCodec.ResolveCode(
            option.CodeValue, (int)option.Code);
        string name = DnsMnemonicText.FormatNamedValue(
            code, ((DnsEdnsOptionCode)code).ToString(), "OPTION");
        string value = FormatEdnsOptionValue(option, code, options);
        if (value.Length == 0) return "; " + name;
        return "; " + name + ": " + value;
    }

    static string FormatEdnsOptionValue(
        DnsEdnsOption option,
        int code,
        DnsTextOptions options)
    {
        if (option.UseRawData)
            return DnsTextEncoding.FormatHex(option.RawData);
        switch (code)
        {
            case 2:
                if (option.HasUpdateLease)
                {
                    string lease = DnsTextEncoding.FormatUnsigned(
                        option.UpdateLeaseSeconds) + " seconds";
                    if (option.HasUpdateKeyLease)
                    {
                        lease = lease + "; key " +
                            DnsTextEncoding.FormatUnsigned(
                                option.UpdateKeyLeaseSeconds) + " seconds";
                    }

                    return lease;
                }
                break;
            case 3:
                return DnsTextEncoding.FormatHex(option.NameServerIdentifier);
            case 5:
                return FormatIntegerList(option.DnssecAlgorithmValues, "ALGORITHM");
            case 6:
                return FormatIntegerList(option.DigestAlgorithmValues, "DIGEST");
            case 7:
                return FormatIntegerList(option.Nsec3HashAlgorithmValues, "HASH");
            case 8:
                if (option.ClientSubnet != null)
                    return FormatClientSubnet(option.ClientSubnet);
                break;
            case 9:
                if (option.HasExpire)
                    return DnsTextEncoding.FormatUnsigned(
                        option.ExpireSeconds) + " seconds";
                break;
            case 10:
                if (option.Cookie != null)
                    return DnsTextEncoding.FormatHex(option.Cookie.ClientCookie) +
                        DnsTextEncoding.FormatHex(option.Cookie.ServerCookie);
                break;
            case 11:
                if (option.Keepalive != null && option.Keepalive.HasTimeout)
                    return DnsTextEncoding.FormatSigned(
                        option.Keepalive.TimeoutUnits * 100) + " ms";
                return "request";
            case 12:
                return option.Padding.Length.ToString(
                    CultureInfo.InvariantCulture) + " octets";
            case 13:
                if (option.ChainName != null)
                    return ReadFormattedName(option.ChainName, options);
                break;
            case 14:
                return FormatIntegerList(option.KeyTagValues, "KEYTAG");
            case 15:
                if (option.ExtendedError != null)
                    return FormatExtendedError(option.ExtendedError);
                break;
            case 18:
                if (option.ReportChannel != null)
                    return ReadFormattedName(option.ReportChannel, options);
                break;
            case 19:
                if (option.ZoneVersion != null)
                    return FormatZoneVersion(option.ZoneVersion);
                break;
        }

        return DnsTextEncoding.FormatHex(option.RawData);
    }

    static string FormatHeaderFlagTokens(int value)
    {
        StringBuilder builder = new StringBuilder();
        AppendFlagToken(builder, value, 32768, "qr");
        AppendFlagToken(builder, value, 1024, "aa");
        AppendFlagToken(builder, value, 512, "tc");
        AppendFlagToken(builder, value, 256, "rd");
        AppendFlagToken(builder, value, 128, "ra");
        AppendFlagToken(builder, value, 64, "z");
        AppendFlagToken(builder, value, 32, "ad");
        AppendFlagToken(builder, value, 16, "cd");
        return builder.ToString();
    }

    static string FormatEdnsFlagTokens(int value)
    {
        StringBuilder builder = new StringBuilder();
        AppendFlagToken(builder, value, 32768, "do");
        AppendFlagToken(builder, value, 16384, "co");
        AppendFlagToken(builder, value, 8192, "de");
        int unknown = value & ~57344 & 65535;
        if (unknown != 0)
        {
            if (builder.Length > 0) builder.Append(' ');
            builder.Append("0x");
            builder.Append(unknown.ToString("X4", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    static void AppendFlagToken(
        StringBuilder builder,
        int value,
        int mask,
        string text)
    {
        if ((value & mask) == 0) return;
        if (builder.Length > 0) builder.Append(' ');
        builder.Append(text);
    }

    static int ReadEffectiveResponseCode(DnsMessage message)
    {
        int baseCode = DnsHeaderCodec.ResolveCode(
            message.Header.ResponseCodeValue,
            (int)message.Header.ResponseCode);
        int extended = 0;
        int index = 0;
        while (index < message.Additionals.Count)
        {
            DnsResourceRecord record = message.Additionals[index];
            int type = DnsHeaderCodec.ResolveCode(
                record.RecordTypeValue, (int)record.RecordType);
            if (type == 41)
            {
                extended = (int)(record.TimeToLive >> 24);
                if (record.Data != null && record.Data.Edns != null)
                    extended = record.Data.Edns.ExtendedResponseCode;
                break;
            }

            index = index + 1;
        }

        return (extended << 4) | (baseCode & 15);
    }

    static List<DnsResourceRecord> FindOptRecords(
        List<DnsResourceRecord> records)
    {
        List<DnsResourceRecord> result = new List<DnsResourceRecord>();
        int index = 0;
        while (index < records.Count)
        {
            DnsResourceRecord record = records[index];
            int type = DnsHeaderCodec.ResolveCode(
                record.RecordTypeValue, (int)record.RecordType);
            if (type == 41) result.Add(record);
            index = index + 1;
        }

        return result;
    }

    static string FormatIntegerList(List<int> values, string prefix)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < values.Count)
        {
            if (index > 0) builder.Append(' ');
            builder.Append(prefix);
            builder.Append(values[index].ToString(CultureInfo.InvariantCulture));
            index = index + 1;
        }

        return builder.ToString();
    }

    static string FormatClientSubnet(DnsEdnsClientSubnet subnet)
    {
        int length = 4;
        if (subnet.AddressFamily == 2) length = 16;
        string address = DnsTextEncoding.FormatHex(subnet.Address);
        if ((subnet.AddressFamily == 1 || subnet.AddressFamily == 2) &&
            subnet.Address.Length <= length)
        {
            byte[] padded = new byte[length];
            subnet.Address.CopyTo(padded, 0);
            address = DnsTextEncoding.FormatAddress(padded);
        }

        return subnet.AddressFamily.ToString(CultureInfo.InvariantCulture) +
            " " + address + "/" +
            subnet.SourcePrefixLength.ToString(CultureInfo.InvariantCulture) +
            " scope/" +
            subnet.ScopePrefixLength.ToString(CultureInfo.InvariantCulture);
    }

    static string FormatExtendedError(DnsEdnsExtendedError error)
    {
        int code = DnsHeaderCodec.ResolveCode(
            error.CodeValue, (int)error.Code);
        string text = DnsMnemonicText.FormatNamedValue(
            code, ((DnsExtendedErrorCode)code).ToString(), "EDE");
        if (error.ExtraText != null && error.ExtraText.Length > 0)
            text = text + ": " + error.ExtraText;
        return text;
    }

    static string FormatZoneVersion(DnsEdnsZoneVersion version)
    {
        if (!version.HasVersion) return "request";
        return "labels=" + version.LabelCount.ToString(
            CultureInfo.InvariantCulture) +
            " type=" + version.Type.ToString(CultureInfo.InvariantCulture) +
            " version=" + DnsTextEncoding.FormatHex(version.Version);
    }

    static string ReadFormattedName(
        DnsName name,
        DnsTextOptions options)
    {
        DnsTextResult result = DnsPresentationNameText.FormatName(name, options);
        if (!result.Success) return "<invalid-name>";
        return result.Text;
    }

    static void SortLines(List<string> lines)
    {
        int index = 1;
        while (index < lines.Count)
        {
            string selected = lines[index];
            int position = index;
            while (position > 0 && string.CompareOrdinal(
                lines[position - 1], selected) > 0)
            {
                lines[position] = lines[position - 1];
                position = position - 1;
            }

            lines[position] = selected;
            index = index + 1;
        }
    }

    static void AppendBlock(StringBuilder builder, string block)
    {
        if (block.Length == 0) return;
        if (builder.Length > 0) builder.Append("\n\n");
        builder.Append(block);
    }
}
