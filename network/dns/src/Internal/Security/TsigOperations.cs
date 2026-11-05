using System.Collections.Generic;
using System.Security.Cryptography;

namespace dns;

internal class DnsTsigAlgorithmInfo
{
    internal int Kind;
    internal int OutputLength;
    internal int DefaultMacLength;
    internal int MinimumMacLength;
}

internal static class TsigOperations
{
    internal static DnsTsigResult Sign(
        DnsMessage message,
        DnsTsigKey key,
        byte[] priorMac,
        DnsClockValue now)
    {
        return SignCore(
            message,
            key,
            priorMac,
            now,
            false,
            new List<byte[]>());
    }

    internal static DnsTsigResult SignChained(
        DnsMessage message,
        DnsTsigKey key,
        byte[] priorMac,
        List<byte[]> precedingMessages,
        DnsClockValue now)
    {
        return SignCore(
            message,
            key,
            priorMac,
            now,
            true,
            precedingMessages);
    }

    static DnsTsigResult SignCore(
        DnsMessage message,
        DnsTsigKey key,
        byte[] priorMac,
        DnsClockValue now,
        bool timersOnly,
        List<byte[]> precedingMessages)
    {
        DnsTsigResult result = CreateResult(key, priorMac);
        if (message == null || message.Header == null || key == null ||
            priorMac == null || now == null || precedingMessages == null)
        {
            return Fail(
                result,
                DnsDiagnosticCode.ValueOutOfRange,
                "tsig",
                "A message, key, prior MAC, and clock value are required.");
        }

        DnsTsigAlgorithmInfo? algorithm = ValidateKey(result, key, now);
        if (algorithm == null)
        {
            result.Success = false;
            return result;
        }

        if (priorMac.Length > 65535)
        {
            return Fail(
                result,
                DnsDiagnosticCode.InvalidLength,
                "priorMac",
                "A prior TSIG MAC cannot exceed 65535 octets.");
        }

        TsigPlacementResult placement =
            TransactionRecordDataCodec.InspectTsigPlacement(message);
        if (!placement.Success)
        {
            CopyDiagnostics(result, placement.Diagnostics);
            return result;
        }

        if (placement.HasTsig)
        {
            return Fail(
                result,
                DnsDiagnosticCode.InvalidTsigPlacement,
                "tsig.placement",
                "A message being signed must not already contain a TSIG record.");
        }

        ulong timeSigned = ReadUnixTime(result, now);
        if (result.Diagnostics.Count > 0)
        {
            return result;
        }

        DnsEncodeResult messageBytes = EncodeUnsignedMessage(
            message,
            (int)message.Header.Identifier);
        if (!messageBytes.Success)
        {
            CopyDiagnostics(result, messageBytes.Diagnostics);
            return result;
        }

        int macLength = ReadSigningMacLength(result, key, algorithm);
        if (macLength == 0)
        {
            return result;
        }

        DnsTsigData tsig = new DnsTsigData();
        tsig.AlgorithmName = RecordSetOperations.CopyName(
            key.AlgorithmName,
            true);
        tsig.TimeSigned = timeSigned;
        tsig.Fudge = key.Fudge;
        tsig.OriginalIdentifier = message.Header.Identifier;
        tsig.Error = DnsTsigError.NoError;
        tsig.ErrorValue = 0;
        DnsEncodeResult macInput;
        if (timersOnly)
        {
            macInput = BuildTimerMacInput(
                messageBytes.Bytes,
                tsig,
                priorMac,
                precedingMessages);
        }
        else
        {
            macInput = BuildMacInput(
                messageBytes.Bytes,
                key.Name,
                tsig,
                priorMac);
        }
        if (!macInput.Success)
        {
            CopyDiagnostics(result, macInput.Diagnostics);
            return result;
        }

        byte[] fullMac = ComputeMac(
            algorithm,
            key.Secret,
            macInput.Bytes);
        tsig.Mac = CopyPrefix(fullMac, macLength);
        DnsMessage signed = MessageOperations.CloneMessage(message);
        DnsResourceRecord record = CreateTsigRecord(key.Name, tsig);
        signed.Additionals.Add(record);
        signed.Header.AdditionalCount = signed.Additionals.Count;
        DnsEncodeResult signedBytes = DnsMessageEncoder.EncodeMessage(
            signed,
            new DnsEncodeOptions());
        if (!signedBytes.Success)
        {
            CopyDiagnostics(result, signedBytes.Diagnostics);
            return result;
        }

        result.Success = true;
        result.Message = signed;
        result.Tsig = tsig;
        result.KeyName = RecordSetOperations.CopyName(key.Name, true);
        result.Mac = RecordSetOperations.CopyBytes(tsig.Mac);
        result.HasTimeSigned = true;
        result.TimeSigned = timeSigned;
        return result;
    }

    internal static DnsTsigResult Verify(
        DnsMessage message,
        DnsTsigKey key,
        byte[] priorMac,
        DnsClockValue now)
    {
        return VerifyCore(
            message,
            key,
            priorMac,
            now,
            false,
            new List<byte[]>());
    }

    internal static DnsTsigResult VerifyChained(
        DnsMessage message,
        DnsTsigKey key,
        byte[] priorMac,
        List<byte[]> precedingMessages,
        DnsClockValue now)
    {
        return VerifyCore(
            message,
            key,
            priorMac,
            now,
            true,
            precedingMessages);
    }

    static DnsTsigResult VerifyCore(
        DnsMessage message,
        DnsTsigKey key,
        byte[] priorMac,
        DnsClockValue now,
        bool timersOnly,
        List<byte[]> precedingMessages)
    {
        DnsTsigResult result = CreateResult(key, priorMac);
        if (message == null || message.Header == null || key == null ||
            priorMac == null || now == null || precedingMessages == null)
        {
            return Fail(
                result,
                DnsDiagnosticCode.ValueOutOfRange,
                "tsig",
                "A message, key, prior MAC, and clock value are required.");
        }

        if (priorMac.Length > 65535)
        {
            return Fail(
                result,
                DnsDiagnosticCode.InvalidLength,
                "priorMac",
                "A prior TSIG MAC cannot exceed 65535 octets.");
        }

        TsigPlacementResult placement =
            TransactionRecordDataCodec.InspectTsigPlacement(message);
        if (!placement.Success)
        {
            CopyDiagnostics(result, placement.Diagnostics);
            return result;
        }

        if (!placement.HasTsig || placement.Record == null)
        {
            return Fail(
                result,
                DnsDiagnosticCode.InvalidTsigPlacement,
                "tsig.placement",
                "The authenticated message must contain one final TSIG record.");
        }

        DnsResourceRecord record = placement.Record;
        if (record.Data == null || record.Data.Tsig == null ||
            record.UseRawData)
        {
            return Fail(
                result,
                DnsDiagnosticCode.InvalidRecordData,
                "tsig",
                "The final TSIG record must contain typed TSIG data.");
        }

        DnsTsigData tsig = record.Data.Tsig;
        PopulatePeerResult(result, message, record, tsig);
        DnsTsigAlgorithmInfo? algorithm = ValidateKey(result, key, now);
        if (algorithm == null)
        {
            return result;
        }

        int recordClass = RecordSetOperations.EffectiveValue(
            record.ClassValue,
            (int)record.Class);
        if (!RecordSetOperations.NamesEqual(record.Name, key.Name) ||
            !RecordSetOperations.NamesEqual(
                tsig.AlgorithmName,
                key.AlgorithmName) ||
            recordClass != 255 || record.TimeToLive != 0)
        {
            return CompleteError(
                result,
                DnsTsigError.BadKey,
                DnsDiagnosticCode.ResponseMismatch,
                "tsig.key",
                "The TSIG owner, algorithm, class, or TTL does not match the key.");
        }

        int errorValue = RecordSetOperations.EffectiveValue(
            tsig.ErrorValue,
            (int)tsig.Error);
        if (errorValue < 0 || errorValue > 65535 ||
            tsig.TimeSigned > 281474976710655 ||
            tsig.Fudge < 0 || tsig.Fudge > 65535 ||
            tsig.OriginalIdentifier < 0 ||
            tsig.OriginalIdentifier > 65535 ||
            tsig.Mac == null || tsig.OtherData == null)
        {
            return Fail(
                result,
                DnsDiagnosticCode.InvalidRecordData,
                "tsig",
                "The TSIG fields must fit their protocol widths.");
        }

        bool response = MessageIsResponse(message);
        if ((!response && errorValue != 0) ||
            (response && errorValue == (int)DnsTsigError.BadTime &&
             tsig.OtherData.Length != 6) ||
            (response && errorValue != (int)DnsTsigError.BadTime &&
             tsig.OtherData.Length != 0))
        {
            return Fail(
                result,
                DnsDiagnosticCode.InvalidRecordData,
                "tsig.error",
                "The TSIG error and other-data fields are invalid for the message direction.");
        }

        if (tsig.Mac.Length == 0 &&
            (errorValue == (int)DnsTsigError.BadKey ||
             errorValue == (int)DnsTsigError.BadSignature))
        {
            result.Success = true;
            result.Error = (DnsTsigError)errorValue;
            result.ErrorValue = errorValue;
            AddDiagnostic(
                result,
                DnsDiagnosticCode.ResponseMismatch,
                "tsig.error",
                "The peer returned an unsigned TSIG key or signature error.");
            return result;
        }

        int minimumMacLength = ReadVerificationMinimum(
            result,
            key,
            algorithm);
        if (minimumMacLength == 0)
        {
            return result;
        }

        if (tsig.Mac.Length > algorithm.OutputLength ||
            tsig.Mac.Length < algorithm.MinimumMacLength ||
            tsig.Mac.Length < minimumMacLength ||
            (algorithm.DefaultMacLength != algorithm.OutputLength &&
             tsig.Mac.Length != algorithm.DefaultMacLength))
        {
            return CompleteError(
                result,
                DnsTsigError.BadTruncation,
                DnsDiagnosticCode.InvalidLength,
                "tsig.mac",
                "The TSIG MAC length violates the algorithm or local truncation policy.");
        }

        DnsEncodeResult messageBytes = EncodeMessageWithoutTsig(
            message,
            tsig.OriginalIdentifier);
        if (!messageBytes.Success)
        {
            CopyDiagnostics(result, messageBytes.Diagnostics);
            return result;
        }

        DnsEncodeResult macInput;
        if (timersOnly)
        {
            macInput = BuildTimerMacInput(
                messageBytes.Bytes,
                tsig,
                priorMac,
                precedingMessages);
        }
        else
        {
            macInput = BuildMacInput(
                messageBytes.Bytes,
                record.Name,
                tsig,
                priorMac);
        }
        if (!macInput.Success)
        {
            CopyDiagnostics(result, macInput.Diagnostics);
            return result;
        }

        byte[] fullMac = ComputeMac(
            algorithm,
            key.Secret,
            macInput.Bytes);
        byte[] expectedMac = CopyPrefix(fullMac, tsig.Mac.Length);
        if (!CryptographicOperations.FixedTimeEquals(
                expectedMac,
                tsig.Mac))
        {
            return CompleteError(
                result,
                DnsTsigError.BadSignature,
                DnsDiagnosticCode.ResponseMismatch,
                "tsig.mac",
                "The TSIG message authentication code did not verify.");
        }

        result.Authenticated = true;
        ulong currentTime = ReadUnixTime(result, now);
        if (result.Diagnostics.Count > 0)
        {
            result.Authenticated = false;
            return result;
        }

        if (!TimeIsAccepted(currentTime, tsig.TimeSigned, tsig.Fudge) ||
            tsig.Fudge > key.Fudge)
        {
            return CompleteError(
                result,
                DnsTsigError.BadTime,
                DnsDiagnosticCode.ResponseMismatch,
                "tsig.timeSigned",
                "The TSIG signing time is outside the permitted time window.");
        }

        result.Success = true;
        result.Error = (DnsTsigError)errorValue;
        result.ErrorValue = errorValue;
        ReadServerTime(result, tsig, errorValue);
        return result;
    }

    static DnsTsigAlgorithmInfo? ValidateKey(
        DnsTsigResult result,
        DnsTsigKey key,
        DnsClockValue now)
    {
        if (key.Name == null || key.AlgorithmName == null ||
            key.Secret == null)
        {
            Fail(
                result,
                DnsDiagnosticCode.InvalidRecordData,
                "key",
                "A TSIG key requires names and shared-secret bytes.");
            return null;
        }

        RawDnsNameResult keyName = RawDnsName.ValidateName(key.Name);
        RawDnsNameResult algorithmName = RawDnsName.ValidateName(
            key.AlgorithmName);
        if (!keyName.Success || keyName.Name == null ||
            !keyName.Name.IsAbsolute || !algorithmName.Success ||
            algorithmName.Name == null || !algorithmName.Name.IsAbsolute)
        {
            Fail(
                result,
                DnsDiagnosticCode.InvalidRecordData,
                "key.name",
                "TSIG key and algorithm names must be valid absolute DNS names.");
            return null;
        }

        DnsTsigAlgorithmInfo? algorithm = ReadAlgorithm(key.AlgorithmName);
        if (algorithm == null)
        {
            CompleteError(
                result,
                DnsTsigError.BadKey,
                DnsDiagnosticCode.UnsupportedAlgorithm,
                "key.algorithmName",
                "The TSIG HMAC algorithm is not supported.");
            return null;
        }

        if (!key.Enabled || key.Fudge < 0 || key.Fudge > 65535 ||
            key.ValidFrom > 281474976710655 ||
            key.ValidUntil > 281474976710655 ||
            (key.HasValidFrom && key.HasValidUntil &&
             key.ValidFrom > key.ValidUntil))
        {
            CompleteError(
                result,
                DnsTsigError.BadKey,
                DnsDiagnosticCode.InvalidRecordData,
                "key",
                "The TSIG key is disabled or has invalid policy values.");
            return null;
        }

        ulong currentTime = ReadUnixTime(result, now);
        if (result.Diagnostics.Count > 0)
        {
            return null;
        }

        if ((key.HasValidFrom && currentTime < key.ValidFrom) ||
            (key.HasValidUntil && currentTime > key.ValidUntil))
        {
            CompleteError(
                result,
                DnsTsigError.BadKey,
                DnsDiagnosticCode.ResponseMismatch,
                "key.validity",
                "The TSIG key is outside its configured validity interval.");
            return null;
        }

        return algorithm;
    }

    static DnsTsigAlgorithmInfo? ReadAlgorithm(DnsName name)
    {
        if (NameIs(name, "hmac-sha1"))
        {
            return CreateAlgorithm(1, 20, 20, 10);
        }

        if (NameIs(name, "hmac-sha256"))
        {
            return CreateAlgorithm(2, 32, 32, 16);
        }

        if (NameIs(name, "hmac-sha256-128"))
        {
            return CreateAlgorithm(2, 32, 16, 16);
        }

        if (NameIs(name, "hmac-sha384"))
        {
            return CreateAlgorithm(3, 48, 48, 24);
        }

        if (NameIs(name, "hmac-sha384-192"))
        {
            return CreateAlgorithm(3, 48, 24, 24);
        }

        if (NameIs(name, "hmac-sha512"))
        {
            return CreateAlgorithm(4, 64, 64, 32);
        }

        if (NameIs(name, "hmac-sha512-256"))
        {
            return CreateAlgorithm(4, 64, 32, 32);
        }

        return null;
    }

    static DnsTsigAlgorithmInfo CreateAlgorithm(
        int kind,
        int outputLength,
        int defaultMacLength,
        int minimumMacLength)
    {
        DnsTsigAlgorithmInfo result = new DnsTsigAlgorithmInfo();
        result.Kind = kind;
        result.OutputLength = outputLength;
        result.DefaultMacLength = defaultMacLength;
        result.MinimumMacLength = minimumMacLength;
        return result;
    }

    static int ReadSigningMacLength(
        DnsTsigResult result,
        DnsTsigKey key,
        DnsTsigAlgorithmInfo algorithm)
    {
        int length = key.MacLength;
        if (length == 0)
        {
            return algorithm.DefaultMacLength;
        }

        if (algorithm.DefaultMacLength != algorithm.OutputLength)
        {
            if (length == algorithm.DefaultMacLength)
            {
                return length;
            }

            Fail(
                result,
                DnsDiagnosticCode.InvalidLength,
                "key.macLength",
                "A registered truncated TSIG algorithm requires its defined MAC length.");
            return 0;
        }

        if (length < algorithm.MinimumMacLength ||
            length > algorithm.OutputLength)
        {
            Fail(
                result,
                DnsDiagnosticCode.InvalidLength,
                "key.macLength",
                "The requested TSIG MAC length violates the algorithm truncation bounds.");
            return 0;
        }

        return length;
    }

    static int ReadVerificationMinimum(
        DnsTsigResult result,
        DnsTsigKey key,
        DnsTsigAlgorithmInfo algorithm)
    {
        if (algorithm.DefaultMacLength != algorithm.OutputLength)
        {
            if (key.MacLength == 0 ||
                key.MacLength == algorithm.DefaultMacLength)
            {
                return algorithm.DefaultMacLength;
            }

            Fail(
                result,
                DnsDiagnosticCode.InvalidLength,
                "key.macLength",
                "A registered truncated TSIG algorithm requires its defined MAC length.");
            return 0;
        }

        if (key.MacLength == 0)
        {
            return algorithm.MinimumMacLength;
        }

        if (key.MacLength < algorithm.MinimumMacLength ||
            key.MacLength > algorithm.OutputLength)
        {
            Fail(
                result,
                DnsDiagnosticCode.InvalidLength,
                "key.macLength",
                "The TSIG verification MAC policy is outside the algorithm bounds.");
            return 0;
        }

        return key.MacLength;
    }

    static bool NameIs(DnsName name, string labelText)
    {
        if (name == null || !name.IsAbsolute ||
            name.Labels == null || name.Labels.Count != 1)
        {
            return false;
        }

        byte[] label = name.Labels[0];
        if (label == null || label.Length != labelText.Length)
        {
            return false;
        }

        int index = 0;
        while (index < label.Length)
        {
            byte value = label[index];
            if (value >= 65 && value <= 90)
            {
                value = (byte)(value + 32);
            }

            if (value != (byte)labelText[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static DnsEncodeResult EncodeUnsignedMessage(
        DnsMessage message,
        int originalIdentifier)
    {
        DnsMessage unsigned = MessageOperations.CloneMessage(message);
        unsigned.Header.Identifier = (ushort)originalIdentifier;
        return DnsMessageEncoder.EncodeMessage(
            unsigned,
            new DnsEncodeOptions());
    }

    static DnsEncodeResult EncodeMessageWithoutTsig(
        DnsMessage message,
        int originalIdentifier)
    {
        DnsMessage unsigned = MessageOperations.CloneMessage(message);
        unsigned.Additionals.RemoveAt(unsigned.Additionals.Count - 1);
        unsigned.Header.AdditionalCount = unsigned.Additionals.Count;
        return EncodeUnsignedMessage(unsigned, originalIdentifier);
    }

    static DnsEncodeResult BuildMacInput(
        byte[] messageBytes,
        DnsName keyName,
        DnsTsigData tsig,
        byte[] priorMac)
    {
        DnsEncodeResult keyBytes = CanonicalDnsData.EncodeCanonicalName(
            keyName);
        if (!keyBytes.Success)
        {
            return keyBytes;
        }

        DnsEncodeResult algorithmBytes = CanonicalDnsData.EncodeCanonicalName(
            tsig.AlgorithmName);
        if (!algorithmBytes.Success)
        {
            return algorithmBytes;
        }

        int errorValue = RecordSetOperations.EffectiveValue(
            tsig.ErrorValue,
            (int)tsig.Error);
        if (messageBytes == null || priorMac == null ||
            tsig.OtherData == null || priorMac.Length > 65535 ||
            tsig.OtherData.Length > 65535 ||
            errorValue < 0 || errorValue > 65535)
        {
            return EncodeFailure(
                DnsDiagnosticCode.InvalidRecordData,
                "tsig.variables",
                "The TSIG MAC input fields must fit their protocol widths.");
        }

        long totalLength = messageBytes.Length + keyBytes.Bytes.Length +
            algorithmBytes.Bytes.Length + tsig.OtherData.Length + 18;
        if (priorMac.Length > 0)
        {
            totalLength = totalLength + priorMac.Length + 2;
        }

        if (totalLength > int.MaxValue)
        {
            return EncodeFailure(
                DnsDiagnosticCode.ResourceLimitExceeded,
                "tsig.variables",
                "The TSIG MAC input exceeds the supported length.");
        }

        byte[] result = new byte[(int)totalLength];
        int offset = 0;
        if (priorMac.Length > 0)
        {
            WriteUnsigned16(result, offset, priorMac.Length);
            offset = offset + 2;
            offset = CopyBytes(priorMac, result, offset);
        }

        offset = CopyBytes(messageBytes, result, offset);
        offset = CopyBytes(keyBytes.Bytes, result, offset);
        WriteUnsigned16(result, offset, 255);
        offset = offset + 2;
        WriteUnsigned32(result, offset, 0);
        offset = offset + 4;
        offset = CopyBytes(algorithmBytes.Bytes, result, offset);
        WriteUnsigned48(result, offset, tsig.TimeSigned);
        offset = offset + 6;
        WriteUnsigned16(result, offset, tsig.Fudge);
        offset = offset + 2;
        WriteUnsigned16(result, offset, errorValue);
        offset = offset + 2;
        WriteUnsigned16(result, offset, tsig.OtherData.Length);
        offset = offset + 2;
        CopyBytes(tsig.OtherData, result, offset);
        DnsEncodeResult encoded = new DnsEncodeResult();
        encoded.Success = true;
        encoded.Bytes = result;
        encoded.BytesWritten = result.Length;
        return encoded;
    }

    static DnsEncodeResult BuildTimerMacInput(
        byte[] messageBytes,
        DnsTsigData tsig,
        byte[] priorMac,
        List<byte[]> precedingMessages)
    {
        if (messageBytes == null || tsig == null || priorMac == null ||
            precedingMessages == null || priorMac.Length == 0 ||
            priorMac.Length > 65535 || tsig.TimeSigned > 281474976710655 ||
            tsig.Fudge < 0 || tsig.Fudge > 65535)
        {
            return EncodeFailure(
                DnsDiagnosticCode.InvalidRecordData,
                "tsig.chain",
                "A chained TSIG requires a prior MAC, message bytes, and valid timers.");
        }

        long totalLength = priorMac.Length + messageBytes.Length + 10;
        int index = 0;
        while (index < precedingMessages.Count)
        {
            byte[] preceding = precedingMessages[index];
            if (preceding == null)
            {
                return EncodeFailure(
                    DnsDiagnosticCode.InvalidRecordData,
                    "tsig.chain.messages",
                    "A preceding transfer message cannot be null.");
            }

            totalLength = totalLength + preceding.Length;
            if (totalLength > int.MaxValue)
            {
                return EncodeFailure(
                    DnsDiagnosticCode.ResourceLimitExceeded,
                    "tsig.chain.messages",
                    "The chained TSIG MAC input exceeds the supported length.");
            }

            index = index + 1;
        }

        byte[] result = new byte[(int)totalLength];
        int offset = 0;
        WriteUnsigned16(result, offset, priorMac.Length);
        offset = offset + 2;
        offset = CopyBytes(priorMac, result, offset);
        index = 0;
        while (index < precedingMessages.Count)
        {
            offset = CopyBytes(
                precedingMessages[index],
                result,
                offset);
            index = index + 1;
        }

        offset = CopyBytes(messageBytes, result, offset);
        WriteUnsigned48(result, offset, tsig.TimeSigned);
        offset = offset + 6;
        WriteUnsigned16(result, offset, tsig.Fudge);
        DnsEncodeResult encoded = new DnsEncodeResult();
        encoded.Success = true;
        encoded.Bytes = result;
        encoded.BytesWritten = result.Length;
        return encoded;
    }

    static byte[] ComputeMac(
        DnsTsigAlgorithmInfo algorithm,
        byte[] secret,
        byte[] data)
    {
        if (algorithm.Kind == 1)
        {
            return HMACSHA1.HashData(secret, data);
        }

        if (algorithm.Kind == 2)
        {
            return HMACSHA256.HashData(secret, data);
        }

        if (algorithm.Kind == 3)
        {
            return HMACSHA384.HashData(secret, data);
        }

        return HMACSHA512.HashData(secret, data);
    }

    static DnsResourceRecord CreateTsigRecord(
        DnsName keyName,
        DnsTsigData tsig)
    {
        DnsRecordData data = new DnsRecordData();
        data.Tsig = tsig;
        DnsResourceRecord result = new DnsResourceRecord();
        result.Name = RecordSetOperations.CopyName(keyName, true);
        result.RecordType = DnsRecordType.Tsig;
        result.RecordTypeValue = 250;
        result.Class = DnsClass.Any;
        result.ClassValue = 255;
        result.TimeToLive = 0;
        result.Data = data;
        return result;
    }

    static ulong ReadUnixTime(
        DnsTsigResult result,
        DnsClockValue now)
    {
        if (now.UnixTimeMilliseconds < 0)
        {
            Fail(
                result,
                DnsDiagnosticCode.ValueOutOfRange,
                "now.unixTimeMilliseconds",
                "TSIG requires a nonnegative Unix time.");
            return 0;
        }

        ulong seconds = (ulong)(now.UnixTimeMilliseconds / 1000);
        if (seconds > 281474976710655)
        {
            Fail(
                result,
                DnsDiagnosticCode.ValueOutOfRange,
                "now.unixTimeMilliseconds",
                "The TSIG signing time must fit in 48 bits.");
            return 0;
        }

        return seconds;
    }

    static bool TimeIsAccepted(
        ulong currentTime,
        ulong signedTime,
        int fudge)
    {
        ulong difference;
        if (currentTime >= signedTime)
        {
            difference = currentTime - signedTime;
        }
        else
        {
            difference = signedTime - currentTime;
        }

        return difference <= (ulong)fudge;
    }

    static bool MessageIsResponse(DnsMessage message)
    {
        int flags = (int)message.Header.Flags;
        if (flags == 0)
        {
            flags = message.Header.FlagsValue;
        }

        return (flags & (int)DnsHeaderFlag.Response) != 0;
    }

    static void PopulatePeerResult(
        DnsTsigResult result,
        DnsMessage message,
        DnsResourceRecord record,
        DnsTsigData tsig)
    {
        result.Message = message;
        result.Tsig = tsig;
        result.KeyName = RecordSetOperations.CopyName(record.Name, true);
        if (tsig.Mac != null)
        {
            result.Mac = RecordSetOperations.CopyBytes(tsig.Mac);
        }

        result.HasTimeSigned = true;
        result.TimeSigned = tsig.TimeSigned;
    }

    static void ReadServerTime(
        DnsTsigResult result,
        DnsTsigData tsig,
        int errorValue)
    {
        if (errorValue != (int)DnsTsigError.BadTime ||
            tsig.OtherData.Length != 6)
        {
            return;
        }

        result.HasServerTime = true;
        result.ServerTime = ((ulong)tsig.OtherData[0] << 40) |
            ((ulong)tsig.OtherData[1] << 32) |
            ((ulong)tsig.OtherData[2] << 24) |
            ((ulong)tsig.OtherData[3] << 16) |
            ((ulong)tsig.OtherData[4] << 8) |
            tsig.OtherData[5];
    }

    static byte[] CopyPrefix(byte[] source, int length)
    {
        byte[] result = new byte[length];
        System.Array.Copy(source, 0, result, 0, length);
        return result;
    }

    static int CopyBytes(byte[] source, byte[] destination, int offset)
    {
        System.Array.Copy(source, 0, destination, offset, source.Length);
        return offset + source.Length;
    }

    static void WriteUnsigned16(byte[] destination, int offset, int value)
    {
        destination[offset] = (byte)((value >> 8) & 255);
        destination[offset + 1] = (byte)(value & 255);
    }

    static void WriteUnsigned32(byte[] destination, int offset, uint value)
    {
        destination[offset] = (byte)((value >> 24) & 255);
        destination[offset + 1] = (byte)((value >> 16) & 255);
        destination[offset + 2] = (byte)((value >> 8) & 255);
        destination[offset + 3] = (byte)(value & 255);
    }

    static void WriteUnsigned48(byte[] destination, int offset, ulong value)
    {
        destination[offset] = (byte)((value >> 40) & 255);
        destination[offset + 1] = (byte)((value >> 32) & 255);
        destination[offset + 2] = (byte)((value >> 24) & 255);
        destination[offset + 3] = (byte)((value >> 16) & 255);
        destination[offset + 4] = (byte)((value >> 8) & 255);
        destination[offset + 5] = (byte)(value & 255);
    }

    static DnsTsigResult CreateResult(
        DnsTsigKey key,
        byte[] priorMac)
    {
        DnsTsigResult result = new DnsTsigResult();
        if (priorMac != null)
        {
            result.PriorMac = RecordSetOperations.CopyBytes(priorMac);
        }

        if (key != null && key.Name != null)
        {
            RawDnsNameResult name = RawDnsName.ValidateName(key.Name);
            if (name.Success && name.Name != null)
            {
                result.KeyName = RecordSetOperations.CopyName(
                    name.Name,
                    true);
            }
        }

        return result;
    }

    static DnsTsigResult CompleteError(
        DnsTsigResult result,
        DnsTsigError error,
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        result.Success = true;
        result.Error = error;
        result.ErrorValue = (int)error;
        AddDiagnostic(result, code, field, reason);
        return result;
    }

    static DnsTsigResult Fail(
        DnsTsigResult result,
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        result.Success = false;
        AddDiagnostic(result, code, field, reason);
        return result;
    }

    static void AddDiagnostic(
        DnsTsigResult result,
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = code;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        result.Diagnostics.Add(diagnostic);
    }

    static void CopyDiagnostics(
        DnsTsigResult result,
        List<DnsDiagnostic> diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            result.Diagnostics.Add(diagnostics[index]);
            index = index + 1;
        }
    }

    static DnsEncodeResult EncodeFailure(
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        DnsEncodeResult result = new DnsEncodeResult();
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = code;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        result.Diagnostics.Add(diagnostic);
        return result;
    }
}
