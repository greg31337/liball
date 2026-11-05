using System;
using System.Collections.Generic;

namespace tftp;

/// <summary>
/// Encodes and decodes TFTP packets.
/// </summary>
public class TftpPacketCodec : ITftpPacketCodec
{
    /// <summary>
    /// Initializes a packet codec.
    /// </summary>
    public TftpPacketCodec()
    {
    }

    /// <summary>
    /// Gets the number of bytes required to encode a packet.
    /// </summary>
    /// <param name="packet">The packet whose encoded length is required.</param>
    /// <returns>The encoded packet length in bytes.</returns>
    public int GetEncodedLength(TftpPacket packet)
    {
        if (packet == null)
        {
            throw new TftpProtocolException("The packet cannot be null.", false, 0);
        }

        if (packet.Opcode == TftpOpcode.Data)
        {
            if (packet.Payload == null)
            {
                throw new TftpProtocolException("The DATA payload cannot be null.", false, 0);
            }

            if (packet.Payload.Length > TftpProtocolValues.MaximumBlockSize)
            {
                throw new TftpProtocolException(
                    "The DATA payload exceeds the protocol maximum block size.",
                    false,
                    0);
            }

            return TftpProtocolValues.DataHeaderLength + packet.Payload.Length;
        }

        if (packet.Opcode == TftpOpcode.Acknowledgement)
        {
            return TftpProtocolValues.AckPacketLength;
        }

        if (packet.Opcode == TftpOpcode.Error)
        {
            int messageLength = ZeroTerminatedAscii.GetEncodedLength(
                packet.ErrorMessage,
                TftpProtocolValues.MaximumRequestPacketLength,
                true);
            return TftpProtocolValues.DataHeaderLength + messageLength;
        }

        if (packet.Opcode == TftpOpcode.OptionAcknowledgement)
        {
            if (packet.Options == null)
            {
                throw new TftpProtocolException("The OACK option list cannot be null.", false, 0);
            }

            if (packet.Options.Count == 0)
            {
                throw new TftpProtocolException(
                    "An OACK packet must contain at least one option.",
                    false,
                    0);
            }

            int optionPacketLength = TftpProtocolValues.OpcodeLength;
            HashSet<string> acknowledgedOptionNames = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);
            foreach (TftpOption acknowledgedOption in packet.Options)
            {
                if (acknowledgedOption == null)
                {
                    throw new TftpProtocolException("An OACK option cannot be null.", false, 0);
                }

                int optionFieldLength = ZeroTerminatedAscii.GetEncodedLength(
                    acknowledgedOption.Name,
                    TftpProtocolValues.MaximumRequestPacketLength,
                    false);
                if (!acknowledgedOptionNames.Add(acknowledgedOption.Name))
                {
                    throw new TftpProtocolException(
                        "An OACK packet cannot contain duplicate option names.",
                        false,
                        0);
                }

                optionPacketLength = AddOptionAcknowledgementFieldLength(
                    optionPacketLength,
                    optionFieldLength);
                optionFieldLength = ZeroTerminatedAscii.GetEncodedLength(
                    acknowledgedOption.Value,
                    TftpProtocolValues.MaximumRequestPacketLength,
                    false);
                optionPacketLength = AddOptionAcknowledgementFieldLength(
                    optionPacketLength,
                    optionFieldLength);
            }

            return optionPacketLength;
        }

        if (packet.Opcode != TftpOpcode.ReadRequest &&
            packet.Opcode != TftpOpcode.WriteRequest)
        {
            throw new TftpProtocolException(
                "The packet opcode cannot currently be encoded.",
                false,
                0);
        }

        int encodedLength = TftpProtocolValues.OpcodeLength;
        int fieldLength = ZeroTerminatedAscii.GetEncodedLength(
            packet.Filename,
            TftpProtocolValues.MaximumRequestPacketLength,
            false);
        encodedLength = AddRequestFieldLength(encodedLength, fieldLength);

        fieldLength = ZeroTerminatedAscii.GetEncodedLength(
            packet.Mode,
            TftpProtocolValues.MaximumRequestPacketLength,
            false);
        ValidateRequestMode(packet.Mode);
        encodedLength = AddRequestFieldLength(encodedLength, fieldLength);

        if (packet.Options == null)
        {
            throw new TftpProtocolException("The request option list cannot be null.", false, 0);
        }

        HashSet<string> optionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (TftpOption option in packet.Options)
        {
            if (option == null)
            {
                throw new TftpProtocolException("A request option cannot be null.", false, 0);
            }

            fieldLength = ZeroTerminatedAscii.GetEncodedLength(
                option.Name,
                TftpProtocolValues.MaximumRequestPacketLength,
                false);
            if (!optionNames.Add(option.Name))
            {
                throw new TftpProtocolException(
                    "A request cannot contain duplicate option names.",
                    false,
                    0);
            }

            encodedLength = AddRequestFieldLength(encodedLength, fieldLength);
            fieldLength = ZeroTerminatedAscii.GetEncodedLength(
                option.Value,
                TftpProtocolValues.MaximumRequestPacketLength,
                false);
            encodedLength = AddRequestFieldLength(encodedLength, fieldLength);
        }

        return encodedLength;
    }

    /// <summary>
    /// Encodes a packet into a caller-provided destination.
    /// </summary>
    /// <param name="packet">The packet to encode.</param>
    /// <param name="destination">The destination that receives the encoded bytes.</param>
    /// <returns>The number of bytes written.</returns>
    public int Encode(TftpPacket packet, Span<byte> destination)
    {
        int encodedLength = GetEncodedLength(packet);
        if (destination.Length < encodedLength)
        {
            throw new TftpProtocolException(
                "The destination is too small for the encoded packet.",
                false,
                0);
        }

        BigEndianCodec.WriteUInt16(destination, 0, (ushort)packet.Opcode);
        if (packet.Opcode == TftpOpcode.Data)
        {
            BigEndianCodec.WriteUInt16(
                destination,
                TftpProtocolValues.OpcodeLength,
                packet.BlockNumber);
            packet.Payload.AsSpan().CopyTo(destination.Slice(TftpProtocolValues.DataHeaderLength));
            return encodedLength;
        }

        if (packet.Opcode == TftpOpcode.Acknowledgement)
        {
            BigEndianCodec.WriteUInt16(
                destination,
                TftpProtocolValues.OpcodeLength,
                packet.BlockNumber);
            return encodedLength;
        }

        if (packet.Opcode == TftpOpcode.Error)
        {
            int errorCodeValue = (int)packet.ErrorCode;
            ushort wireErrorCode = 0;
            if (errorCodeValue >= (int)TftpErrorCode.Undefined &&
                errorCodeValue <= (int)TftpErrorCode.OptionNegotiationFailed)
            {
                wireErrorCode = (ushort)errorCodeValue;
            }

            BigEndianCodec.WriteUInt16(
                destination,
                TftpProtocolValues.OpcodeLength,
                wireErrorCode);
            return ZeroTerminatedAscii.WriteField(
                packet.ErrorMessage,
                destination,
                TftpProtocolValues.DataHeaderLength,
                TftpProtocolValues.MaximumRequestPacketLength,
                true);
        }

        if (packet.Opcode == TftpOpcode.OptionAcknowledgement)
        {
            int optionOffset = TftpProtocolValues.OpcodeLength;
            foreach (TftpOption acknowledgedOption in packet.Options)
            {
                optionOffset = ZeroTerminatedAscii.WriteField(
                    acknowledgedOption.Name,
                    destination,
                    optionOffset,
                    TftpProtocolValues.MaximumRequestPacketLength,
                    false);
                optionOffset = ZeroTerminatedAscii.WriteField(
                    acknowledgedOption.Value,
                    destination,
                    optionOffset,
                    TftpProtocolValues.MaximumRequestPacketLength,
                    false);
            }

            return optionOffset;
        }

        int offset = TftpProtocolValues.OpcodeLength;
        offset = ZeroTerminatedAscii.WriteField(
            packet.Filename,
            destination,
            offset,
            TftpProtocolValues.MaximumRequestPacketLength,
            false);
        offset = ZeroTerminatedAscii.WriteField(
            packet.Mode,
            destination,
            offset,
            TftpProtocolValues.MaximumRequestPacketLength,
            false);

        foreach (TftpOption option in packet.Options)
        {
            offset = ZeroTerminatedAscii.WriteField(
                option.Name,
                destination,
                offset,
                TftpProtocolValues.MaximumRequestPacketLength,
                false);
            offset = ZeroTerminatedAscii.WriteField(
                option.Value,
                destination,
                offset,
                TftpProtocolValues.MaximumRequestPacketLength,
                false);
        }

        return offset;
    }

    /// <summary>
    /// Decodes a packet and throws when the source is invalid.
    /// </summary>
    /// <param name="source">The encoded packet bytes.</param>
    /// <param name="maximumDataLength">The maximum accepted DATA payload length.</param>
    /// <returns>The decoded packet.</returns>
    public TftpPacket Decode(ReadOnlySpan<byte> source, int maximumDataLength)
    {
        ushort opcodeValue = BigEndianCodec.ReadUInt16(source, 0);
        if (opcodeValue == (ushort)TftpOpcode.Data)
        {
            return DecodeDataPacket(source, maximumDataLength);
        }

        if (opcodeValue == (ushort)TftpOpcode.Acknowledgement)
        {
            return DecodeAcknowledgementPacket(source);
        }

        if (opcodeValue == (ushort)TftpOpcode.Error)
        {
            return DecodeErrorPacket(source);
        }

        if (opcodeValue == (ushort)TftpOpcode.OptionAcknowledgement)
        {
            return DecodeOptionAcknowledgementPacket(source);
        }

        if (opcodeValue != (ushort)TftpOpcode.ReadRequest &&
            opcodeValue != (ushort)TftpOpcode.WriteRequest)
        {
            throw new TftpProtocolException(
                "The packet opcode cannot currently be decoded.",
                true,
                0);
        }

        if (source.Length > TftpProtocolValues.MaximumRequestPacketLength)
        {
            throw new TftpProtocolException(
                "The request packet exceeds the 512-byte maximum length.",
                true,
                TftpProtocolValues.MaximumRequestPacketLength);
        }

        TftpPacket packet = new TftpPacket();
        packet.Opcode = (TftpOpcode)opcodeValue;
        int offset = TftpProtocolValues.OpcodeLength;
        ZeroTerminatedAsciiReadResult filename = ZeroTerminatedAscii.ReadField(
            source,
            offset,
            TftpProtocolValues.MaximumRequestPacketLength,
            false);
        packet.Filename = filename.Value;
        offset = filename.NextOffset;

        int modeOffset = offset;
        ZeroTerminatedAsciiReadResult mode = ZeroTerminatedAscii.ReadField(
            source,
            offset,
            TftpProtocolValues.MaximumRequestPacketLength,
            false);
        if (!IsRequestModeRecognized(mode.Value))
        {
            throw new TftpProtocolException(
                "The request mode is not recognized.",
                true,
                modeOffset);
        }

        packet.Mode = mode.Value;
        offset = mode.NextOffset;

        HashSet<string> optionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (offset < source.Length)
        {
            int optionOffset = offset;
            ZeroTerminatedAsciiReadResult optionName = ZeroTerminatedAscii.ReadField(
                source,
                offset,
                TftpProtocolValues.MaximumRequestPacketLength,
                false);
            offset = optionName.NextOffset;
            if (offset == source.Length)
            {
                throw new TftpProtocolException(
                    "The request contains an option without a value.",
                    true,
                    offset);
            }

            ZeroTerminatedAsciiReadResult optionValue = ZeroTerminatedAscii.ReadField(
                source,
                offset,
                TftpProtocolValues.MaximumRequestPacketLength,
                false);
            if (!optionNames.Add(optionName.Value))
            {
                throw new TftpProtocolException(
                    "A request cannot contain duplicate option names.",
                    true,
                    optionOffset);
            }

            TftpOption option = new TftpOption();
            option.Name = optionName.Value;
            option.Value = optionValue.Value;
            packet.Options.Add(option);
            offset = optionValue.NextOffset;
        }

        return packet;
    }

    internal TftpPacket DecodeDataPacket(
        ReadOnlySpan<byte> source,
        int maximumDataLength)
    {
        if (maximumDataLength < 0 ||
            maximumDataLength > TftpProtocolValues.MaximumBlockSize)
        {
            throw new TftpProtocolException(
                "The maximum DATA payload length is outside the protocol range.",
                false,
                0);
        }

        if (source.Length < TftpProtocolValues.DataHeaderLength)
        {
            throw new TftpProtocolException(
                "A DATA packet requires a four-byte header.",
                true,
                source.Length);
        }

        int payloadLength = source.Length - TftpProtocolValues.DataHeaderLength;
        if (payloadLength > maximumDataLength)
        {
            throw new TftpProtocolException(
                "The DATA payload exceeds the configured maximum length.",
                true,
                TftpProtocolValues.DataHeaderLength + maximumDataLength);
        }

        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Data;
        packet.BlockNumber = BigEndianCodec.ReadUInt16(
            source,
            TftpProtocolValues.OpcodeLength);
        packet.Payload = source.Slice(TftpProtocolValues.DataHeaderLength).ToArray();
        return packet;
    }

    internal TftpPacket DecodeAcknowledgementPacket(ReadOnlySpan<byte> source)
    {
        if (source.Length != TftpProtocolValues.AckPacketLength)
        {
            int failureOffset = source.Length;
            if (failureOffset > TftpProtocolValues.AckPacketLength)
            {
                failureOffset = TftpProtocolValues.AckPacketLength;
            }

            throw new TftpProtocolException(
                "An ACK packet must contain exactly four bytes.",
                true,
                failureOffset);
        }

        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Acknowledgement;
        packet.BlockNumber = BigEndianCodec.ReadUInt16(
            source,
            TftpProtocolValues.OpcodeLength);
        return packet;
    }

    internal TftpPacket DecodeErrorPacket(ReadOnlySpan<byte> source)
    {
        if (source.Length < TftpProtocolValues.DataHeaderLength)
        {
            throw new TftpProtocolException(
                "An ERROR packet requires a four-byte header.",
                true,
                source.Length);
        }

        ushort wireErrorCode = BigEndianCodec.ReadUInt16(
            source,
            TftpProtocolValues.OpcodeLength);
        TftpErrorCode errorCode = TftpErrorCode.Undefined;
        if (wireErrorCode <= (ushort)TftpErrorCode.OptionNegotiationFailed)
        {
            errorCode = (TftpErrorCode)wireErrorCode;
        }

        ZeroTerminatedAsciiReadResult message = ZeroTerminatedAscii.ReadField(
            source,
            TftpProtocolValues.DataHeaderLength,
            TftpProtocolValues.MaximumRequestPacketLength,
            true);
        if (message.NextOffset != source.Length)
        {
            throw new TftpProtocolException(
                "An ERROR packet cannot contain data after its message terminator.",
                true,
                message.NextOffset);
        }

        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Error;
        packet.ErrorCode = errorCode;
        packet.ErrorMessage = message.Value;
        return packet;
    }

    internal TftpPacket DecodeOptionAcknowledgementPacket(ReadOnlySpan<byte> source)
    {
        if (source.Length > TftpProtocolValues.MaximumRequestPacketLength)
        {
            throw new TftpProtocolException(
                "The OACK packet exceeds the 512-byte maximum length.",
                true,
                TftpProtocolValues.MaximumRequestPacketLength);
        }

        if (source.Length == TftpProtocolValues.OpcodeLength)
        {
            throw new TftpProtocolException(
                "An OACK packet must contain at least one option.",
                true,
                source.Length);
        }

        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.OptionAcknowledgement;
        int offset = TftpProtocolValues.OpcodeLength;
        HashSet<string> optionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (offset < source.Length)
        {
            int optionOffset = offset;
            ZeroTerminatedAsciiReadResult optionName = ZeroTerminatedAscii.ReadField(
                source,
                offset,
                TftpProtocolValues.MaximumRequestPacketLength,
                false);
            offset = optionName.NextOffset;
            if (offset == source.Length)
            {
                throw new TftpProtocolException(
                    "The OACK packet contains an option without a value.",
                    true,
                    offset);
            }

            ZeroTerminatedAsciiReadResult optionValue = ZeroTerminatedAscii.ReadField(
                source,
                offset,
                TftpProtocolValues.MaximumRequestPacketLength,
                false);
            if (!optionNames.Add(optionName.Value))
            {
                throw new TftpProtocolException(
                    "An OACK packet cannot contain duplicate option names.",
                    true,
                    optionOffset);
            }

            TftpOption option = new TftpOption();
            option.Name = optionName.Value;
            option.Value = optionValue.Value;
            packet.Options.Add(option);
            offset = optionValue.NextOffset;
        }

        return packet;
    }

    /// <summary>
    /// Attempts to decode a packet without reporting validation failures through exceptions.
    /// </summary>
    /// <param name="source">The encoded packet bytes.</param>
    /// <param name="maximumDataLength">The maximum accepted DATA payload length.</param>
    /// <returns>The decode outcome.</returns>
    public TftpPacketDecodeResult TryDecode(ReadOnlySpan<byte> source, int maximumDataLength)
    {
        if (source.Length < TftpProtocolValues.OpcodeLength)
        {
            return CreateDecodeFailure(
                "Two bytes are required to read an unsigned 16-bit value.");
        }

        int opcodeValue = (source[0] << 8) | source[1];
        if (opcodeValue == (int)TftpOpcode.Data)
        {
            if (maximumDataLength < 0 ||
                maximumDataLength > TftpProtocolValues.MaximumBlockSize)
            {
                return CreateDecodeFailure(
                    "The maximum DATA payload length is outside the protocol range.");
            }

            if (source.Length < TftpProtocolValues.DataHeaderLength)
            {
                return CreateDecodeFailure("A DATA packet requires a four-byte header.");
            }

            int payloadLength = source.Length - TftpProtocolValues.DataHeaderLength;
            if (payloadLength > maximumDataLength)
            {
                return CreateDecodeFailure(
                    "The DATA payload exceeds the configured maximum length.");
            }

            return CreateDecodeSuccess(source, maximumDataLength);
        }

        if (opcodeValue == (int)TftpOpcode.Acknowledgement)
        {
            if (source.Length != TftpProtocolValues.AckPacketLength)
            {
                return CreateDecodeFailure(
                    "An ACK packet must contain exactly four bytes.");
            }

            return CreateDecodeSuccess(source, maximumDataLength);
        }

        if (opcodeValue == (int)TftpOpcode.Error)
        {
            if (source.Length < TftpProtocolValues.DataHeaderLength)
            {
                return CreateDecodeFailure("An ERROR packet requires a four-byte header.");
            }

            ZeroTerminatedAsciiDecodeResult errorMessage = ZeroTerminatedAscii.TryReadField(
                source,
                TftpProtocolValues.DataHeaderLength,
                TftpProtocolValues.MaximumRequestPacketLength,
                true);
            if (!errorMessage.Success)
            {
                return CreateDecodeFailure(errorMessage.FailureMessage);
            }

            if (errorMessage.NextOffset != source.Length)
            {
                return CreateDecodeFailure(
                    "An ERROR packet cannot contain data after its message terminator.");
            }

            return CreateDecodeSuccess(source, maximumDataLength);
        }

        if (opcodeValue == (int)TftpOpcode.OptionAcknowledgement)
        {
            if (source.Length > TftpProtocolValues.MaximumRequestPacketLength)
            {
                return CreateDecodeFailure(
                    "The OACK packet exceeds the 512-byte maximum length.");
            }

            if (source.Length == TftpProtocolValues.OpcodeLength)
            {
                return CreateDecodeFailure(
                    "An OACK packet must contain at least one option.");
            }

            HashSet<string> acknowledgedOptionNames = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);
            int acknowledgedOptionOffset = TftpProtocolValues.OpcodeLength;
            while (acknowledgedOptionOffset < source.Length)
            {
                ZeroTerminatedAsciiDecodeResult acknowledgedOptionName =
                    ZeroTerminatedAscii.TryReadField(
                        source,
                        acknowledgedOptionOffset,
                        TftpProtocolValues.MaximumRequestPacketLength,
                        false);
                if (!acknowledgedOptionName.Success)
                {
                    return CreateDecodeFailure(acknowledgedOptionName.FailureMessage);
                }

                acknowledgedOptionOffset = acknowledgedOptionName.NextOffset;
                if (acknowledgedOptionOffset == source.Length)
                {
                    return CreateDecodeFailure(
                        "The OACK packet contains an option without a value.");
                }

                ZeroTerminatedAsciiDecodeResult acknowledgedOptionValue =
                    ZeroTerminatedAscii.TryReadField(
                        source,
                        acknowledgedOptionOffset,
                        TftpProtocolValues.MaximumRequestPacketLength,
                        false);
                if (!acknowledgedOptionValue.Success)
                {
                    return CreateDecodeFailure(acknowledgedOptionValue.FailureMessage);
                }

                if (!acknowledgedOptionNames.Add(acknowledgedOptionName.Value))
                {
                    return CreateDecodeFailure(
                        "An OACK packet cannot contain duplicate option names.");
                }

                acknowledgedOptionOffset = acknowledgedOptionValue.NextOffset;
            }

            return CreateDecodeSuccess(source, maximumDataLength);
        }

        if (opcodeValue != (int)TftpOpcode.ReadRequest &&
            opcodeValue != (int)TftpOpcode.WriteRequest)
        {
            return CreateDecodeFailure("The packet opcode cannot currently be decoded.");
        }

        if (source.Length > TftpProtocolValues.MaximumRequestPacketLength)
        {
            return CreateDecodeFailure(
                "The request packet exceeds the 512-byte maximum length.");
        }

        int offset = TftpProtocolValues.OpcodeLength;
        ZeroTerminatedAsciiDecodeResult filename = ZeroTerminatedAscii.TryReadField(
            source,
            offset,
            TftpProtocolValues.MaximumRequestPacketLength,
            false);
        if (!filename.Success)
        {
            return CreateDecodeFailure(filename.FailureMessage);
        }

        offset = filename.NextOffset;
        ZeroTerminatedAsciiDecodeResult mode = ZeroTerminatedAscii.TryReadField(
            source,
            offset,
            TftpProtocolValues.MaximumRequestPacketLength,
            false);
        if (!mode.Success)
        {
            return CreateDecodeFailure(mode.FailureMessage);
        }

        if (!IsRequestModeRecognized(mode.Value))
        {
            return CreateDecodeFailure("The request mode is not recognized.");
        }

        offset = mode.NextOffset;
        HashSet<string> optionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (offset < source.Length)
        {
            ZeroTerminatedAsciiDecodeResult optionName = ZeroTerminatedAscii.TryReadField(
                source,
                offset,
                TftpProtocolValues.MaximumRequestPacketLength,
                false);
            if (!optionName.Success)
            {
                return CreateDecodeFailure(optionName.FailureMessage);
            }

            offset = optionName.NextOffset;
            if (offset == source.Length)
            {
                return CreateDecodeFailure(
                    "The request contains an option without a value.");
            }

            ZeroTerminatedAsciiDecodeResult optionValue = ZeroTerminatedAscii.TryReadField(
                source,
                offset,
                TftpProtocolValues.MaximumRequestPacketLength,
                false);
            if (!optionValue.Success)
            {
                return CreateDecodeFailure(optionValue.FailureMessage);
            }

            if (!optionNames.Add(optionName.Value))
            {
                return CreateDecodeFailure(
                    "A request cannot contain duplicate option names.");
            }

            offset = optionValue.NextOffset;
        }

        return CreateDecodeSuccess(source, maximumDataLength);
    }

    internal TftpPacketDecodeResult CreateDecodeSuccess(
        ReadOnlySpan<byte> source,
        int maximumDataLength)
    {
        TftpPacketDecodeResult result = new TftpPacketDecodeResult();
        result.Success = true;
        result.Packet = Decode(source, maximumDataLength);
        result.ConsumedLength = source.Length;
        return result;
    }

    internal TftpPacketDecodeResult CreateDecodeFailure(string failureMessage)
    {
        TftpPacketDecodeResult result = new TftpPacketDecodeResult();
        result.FailureMessage = failureMessage;
        return result;
    }

    internal int AddRequestFieldLength(int encodedLength, int fieldLength)
    {
        if (fieldLength > TftpProtocolValues.MaximumRequestPacketLength - encodedLength)
        {
            throw new TftpProtocolException(
                "The request packet exceeds the 512-byte maximum length.",
                false,
                0);
        }

        return encodedLength + fieldLength;
    }

    internal int AddOptionAcknowledgementFieldLength(int encodedLength, int fieldLength)
    {
        if (fieldLength > TftpProtocolValues.MaximumRequestPacketLength - encodedLength)
        {
            throw new TftpProtocolException(
                "The OACK packet exceeds the 512-byte maximum length.",
                false,
                0);
        }

        return encodedLength + fieldLength;
    }

    internal void ValidateRequestMode(string mode)
    {
        if (!IsRequestModeRecognized(mode))
        {
            throw new TftpProtocolException("The request mode is not recognized.", false, 0);
        }
    }

    internal bool IsRequestModeRecognized(string mode)
    {
        bool recognized = string.Equals(
            mode,
            TftpProtocolValues.NetasciiMode,
            StringComparison.OrdinalIgnoreCase);
        if (!recognized)
        {
            recognized = string.Equals(
                mode,
                TftpProtocolValues.OctetMode,
                StringComparison.OrdinalIgnoreCase);
        }

        if (!recognized)
        {
            recognized = string.Equals(
                mode,
                TftpProtocolValues.MailMode,
                StringComparison.OrdinalIgnoreCase);
        }

        return recognized;
    }
}
