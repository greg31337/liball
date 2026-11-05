using System;
using System.Collections.Generic;

namespace rtp;

internal static class RtpHeaderExtensionWireCodec
{
    internal static int GetEnvelopeEncodedSize(RtpHeaderExtension extension)
    {
        string? validationError = GetEnvelopeValidationError(extension);
        if (validationError != null)
        {
            throw new RtpPacketFormatException(validationError);
        }

        RtpHeaderExtension selectedExtension = GetSelectedEncodingExtension(extension);
        if (selectedExtension.Format == RtpHeaderExtensionFormat.OneByte)
        {
            return GetOneByteEnvelopeEncodedSize(selectedExtension);
        }

        return GetTwoByteEnvelopeEncodedSize(selectedExtension);
    }

    internal static int EncodeEnvelope(
        RtpHeaderExtension extension,
        Span<byte> destination)
    {
        string? validationError = GetEnvelopeValidationError(extension);
        if (validationError != null)
        {
            throw new RtpPacketFormatException(validationError);
        }

        RtpHeaderExtension selectedExtension = GetSelectedEncodingExtension(extension);
        if (selectedExtension.Format == RtpHeaderExtensionFormat.OneByte)
        {
            return EncodeOneByteEnvelope(selectedExtension, destination);
        }

        return EncodeTwoByteEnvelope(selectedExtension, destination);
    }

    internal static RtpCodecResult TryEncodeEnvelope(
        RtpHeaderExtension extension,
        Span<byte> destination)
    {
        string? validationError = GetEnvelopeValidationError(extension);
        if (validationError != null)
        {
            return CreateExtensionFailure(validationError);
        }

        RtpHeaderExtension selectedExtension = GetSelectedEncodingExtension(extension);
        if (selectedExtension.Format == RtpHeaderExtensionFormat.OneByte)
        {
            return TryEncodeOneByteEnvelope(selectedExtension, destination);
        }

        return TryEncodeTwoByteEnvelope(selectedExtension, destination);
    }

    internal static bool UsesElementEncoding(RtpHeaderExtension extension)
    {
        if (extension.Format == RtpHeaderExtensionFormat.OneByte ||
            extension.Format == RtpHeaderExtensionFormat.TwoByte)
        {
            return true;
        }

        return IsAutomaticElementExtension(extension);
    }

    internal static string? GetEnvelopeValidationError(RtpHeaderExtension extension)
    {
        if (extension == null)
        {
            return "The RTP header extension is required.";
        }

        if (extension.Format == RtpHeaderExtensionFormat.OneByte)
        {
            return GetOneByteValidationError(extension);
        }

        if (extension.Format == RtpHeaderExtensionFormat.TwoByte)
        {
            return GetTwoByteValidationError(extension);
        }

        if (IsAutomaticElementExtension(extension))
        {
            string? elementError = GetGeneralElementsValidationError(extension);
            if (elementError != null)
            {
                return elementError;
            }

            RtpHeaderExtension selectedExtension =
                GetSelectedEncodingExtension(extension);
            if (selectedExtension.Format == RtpHeaderExtensionFormat.OneByte)
            {
                return GetOneByteValidationError(selectedExtension);
            }

            return GetTwoByteValidationError(selectedExtension);
        }

        return "The RTP header-extension format is not supported by the element codec.";
    }

    internal static RtpHeaderExtension DecodeEnvelope(ReadOnlySpan<byte> source)
    {
        RtpCodecResult result = TryDecodeEnvelope(source);
        if (!result.Success)
        {
            string errorMessage = "The RTP header extension could not be decoded.";
            if (result.ErrorMessage != null)
            {
                errorMessage = result.ErrorMessage;
            }

            throw new RtpPacketFormatException(errorMessage);
        }

        RtpHeaderExtension? extension = result.HeaderExtension;
        if (extension == null)
        {
            throw new RtpPacketFormatException(
                "The RTP header-extension decoder returned no extension.");
        }

        return extension;
    }

    internal static RtpCodecResult TryDecodeEnvelope(ReadOnlySpan<byte> source)
    {
        if (source.Length < RtpProtocolConstants.HeaderExtensionEnvelopeSize)
        {
            return CreateExtensionFailure(
                "The RTP header-extension envelope is truncated.");
        }

        ushort profile = NetworkBinaryReader.ReadUInt16(source, 0);
        if (profile == RtpHeaderExtensionProfiles.OneByte)
        {
            return TryDecodeOneByteEnvelope(source);
        }

        if ((profile & RtpHeaderExtensionProfiles.TwoByteProfileMask) ==
            RtpHeaderExtensionProfiles.TwoByte)
        {
            return TryDecodeTwoByteEnvelope(source);
        }

        return CreateExtensionFailure(
            "The RTP header-extension profile is not supported by the element codec.");
    }

    internal static int GetOneByteEnvelopeEncodedSize(RtpHeaderExtension extension)
    {
        string? validationError = GetOneByteValidationError(extension);
        if (validationError != null)
        {
            throw new RtpPacketFormatException(validationError);
        }

        long dataSize = CalculateOneByteDataSize(extension);
        return RtpProtocolConstants.HeaderExtensionEnvelopeSize + (int)dataSize;
    }

    internal static int EncodeOneByteEnvelope(
        RtpHeaderExtension extension,
        Span<byte> destination)
    {
        int encodedSize = GetOneByteEnvelopeEncodedSize(extension);
        if (destination.Length < encodedSize)
        {
            throw new RtpBufferCapacityException(
                "The destination is too small for the RTP header extension.");
        }

        EncodeValidatedOneByteEnvelope(extension, destination);
        return encodedSize;
    }

    internal static RtpCodecResult TryEncodeOneByteEnvelope(
        RtpHeaderExtension extension,
        Span<byte> destination)
    {
        RtpCodecResult result = new RtpCodecResult();
        string? validationError = GetOneByteValidationError(extension);
        if (validationError != null)
        {
            result.ErrorMessage = validationError;
            return result;
        }

        int encodedSize = RtpProtocolConstants.HeaderExtensionEnvelopeSize +
            (int)CalculateOneByteDataSize(extension);
        if (destination.Length < encodedSize)
        {
            result.ErrorMessage =
                "The destination is too small for the RTP header extension.";
            return result;
        }

        EncodeValidatedOneByteEnvelope(extension, destination);
        result.Success = true;
        result.WrittenCount = encodedSize;
        return result;
    }

    internal static RtpHeaderExtension DecodeOneByteEnvelope(ReadOnlySpan<byte> source)
    {
        RtpCodecResult result = TryDecodeOneByteEnvelope(source);
        if (!result.Success)
        {
            string errorMessage = "The RTP header extension could not be decoded.";
            if (result.ErrorMessage != null)
            {
                errorMessage = result.ErrorMessage;
            }

            throw new RtpPacketFormatException(errorMessage);
        }

        RtpHeaderExtension? extension = result.HeaderExtension;
        if (extension == null)
        {
            throw new RtpPacketFormatException(
                "The RTP header-extension decoder returned no extension.");
        }

        return extension;
    }

    internal static RtpCodecResult TryDecodeOneByteEnvelope(ReadOnlySpan<byte> source)
    {
        if (source.Length < RtpProtocolConstants.HeaderExtensionEnvelopeSize)
        {
            return CreateExtensionFailure(
                "The RTP header-extension envelope is truncated.");
        }

        ushort profile = NetworkBinaryReader.ReadUInt16(source, 0);
        if (profile != RtpHeaderExtensionProfiles.OneByte)
        {
            return CreateExtensionFailure(
                "The RTP header extension does not use the one-byte profile.");
        }

        int wordCount = NetworkBinaryReader.ReadUInt16(source, 2);
        int dataLength = wordCount * RtpProtocolConstants.HeaderExtensionWordSize;
        if (dataLength > source.Length - RtpProtocolConstants.HeaderExtensionEnvelopeSize)
        {
            return CreateExtensionFailure(
                "The RTP header-extension data is truncated.");
        }

        ReadOnlySpan<byte> data = NetworkBinaryReader.ReadSlice(
            source,
            RtpProtocolConstants.HeaderExtensionEnvelopeSize,
            dataLength);
        RtpCodecResult result = ParseOneByteData(profile, data);
        if (result.Success)
        {
            result.ConsumedCount = RtpProtocolConstants.HeaderExtensionEnvelopeSize + dataLength;
        }

        return result;
    }

    internal static RtpCodecResult ParseOneByteData(
        ushort profile,
        ReadOnlySpan<byte> data)
    {
        if (profile != RtpHeaderExtensionProfiles.OneByte)
        {
            return CreateExtensionFailure(
                "The RTP header extension does not use the one-byte profile.");
        }

        if (data.Length % RtpProtocolConstants.HeaderExtensionWordSize != 0)
        {
            return CreateExtensionFailure(
                "The RTP header-extension data is not aligned to four bytes.");
        }

        List<RtpHeaderExtensionElement> elements = new List<RtpHeaderExtensionElement>();
        int offset = 0;
        bool terminated = false;
        while (offset < data.Length && !terminated)
        {
            byte elementHeader = NetworkBinaryReader.ReadByte(data, offset);
            int identifier = elementHeader >> 4;
            if (identifier == RtpHeaderExtensionProfiles.PaddingIdentifier)
            {
                if (elementHeader != 0)
                {
                    return CreateExtensionFailure(
                        "One-byte RTP header-extension padding must be zero.");
                }

                offset = offset + 1;
            }
            else if (identifier == RtpHeaderExtensionProfiles.OneByteReservedIdentifier)
            {
                terminated = true;
            }
            else
            {
                int dataLength = (elementHeader & 15) + 1;
                int dataOffset = offset + RtpHeaderExtensionProfiles.OneByteElementHeaderSize;
                if (dataLength > data.Length - dataOffset)
                {
                    return CreateExtensionFailure(
                        "A one-byte RTP header-extension element has truncated data.");
                }

                if (ContainsElementIdentifier(elements, identifier))
                {
                    return CreateExtensionFailure(
                        "The RTP header extension contains a duplicate element identifier.");
                }

                RtpHeaderExtensionElement element = new RtpHeaderExtensionElement();
                element.Identifier = identifier;
                element.Data = NetworkBinaryReader.ReadSlice(
                    data,
                    dataOffset,
                    dataLength).ToArray();
                elements.Add(element);
                offset = dataOffset + dataLength;
            }
        }

        RtpHeaderExtension extension = new RtpHeaderExtension();
        extension.Profile = profile;
        extension.Format = RtpHeaderExtensionFormat.OneByte;
        extension.Elements = elements.ToArray();
        extension.Data = data.ToArray();

        RtpCodecResult result = new RtpCodecResult();
        result.Success = true;
        result.ConsumedCount = data.Length;
        result.HeaderExtension = extension;
        return result;
    }

    internal static string? GetOneByteValidationError(RtpHeaderExtension extension)
    {
        if (extension == null)
        {
            return "The RTP header extension is required.";
        }

        if (extension.Format != RtpHeaderExtensionFormat.OneByte)
        {
            return "The RTP header extension must use the one-byte format.";
        }

        if (extension.Profile != RtpHeaderExtensionProfiles.OneByte)
        {
            return "The RTP header extension must use the one-byte profile.";
        }

        if (extension.ApplicationBits != 0)
        {
            return "A one-byte RTP header extension cannot contain application bits.";
        }

        if (extension.Elements == null)
        {
            return "The RTP header-extension element array is required.";
        }

        if (extension.Data == null)
        {
            return "The RTP header-extension data array is required.";
        }

        long unalignedSize = 0;
        int elementIndex = 0;
        while (elementIndex < extension.Elements.Length)
        {
            RtpHeaderExtensionElement? element = extension.Elements[elementIndex];
            if (element == null)
            {
                return "An RTP header-extension element is required.";
            }

            if (element.Identifier < RtpHeaderExtensionProfiles.OneByteIdentifierMinimum ||
                element.Identifier > RtpHeaderExtensionProfiles.OneByteIdentifierMaximum)
            {
                return "A one-byte RTP header-extension identifier must be between 1 and 14.";
            }

            if (element.Data == null)
            {
                return "The RTP header-extension element data array is required.";
            }

            if (element.Data.Length < RtpHeaderExtensionProfiles.OneByteDataLengthMinimum ||
                element.Data.Length > RtpHeaderExtensionProfiles.OneByteDataLengthMaximum)
            {
                return "One-byte RTP header-extension element data must contain 1 to 16 bytes.";
            }

            unalignedSize = unalignedSize +
                RtpHeaderExtensionProfiles.OneByteElementHeaderSize + element.Data.Length;
            elementIndex = elementIndex + 1;
        }

        string? duplicateError = GetDuplicateElementError(extension.Elements);
        if (duplicateError != null)
        {
            return duplicateError;
        }

        long padding = (4 - (unalignedSize & 3)) & 3;
        if (unalignedSize + padding > RtpProtocolConstants.MaximumHeaderExtensionDataSize)
        {
            return "The encoded RTP header-extension data is too large.";
        }

        return null;
    }

    internal static int GetTwoByteEnvelopeEncodedSize(RtpHeaderExtension extension)
    {
        string? validationError = GetTwoByteValidationError(extension);
        if (validationError != null)
        {
            throw new RtpPacketFormatException(validationError);
        }

        long dataSize = CalculateTwoByteDataSize(extension);
        return RtpProtocolConstants.HeaderExtensionEnvelopeSize + (int)dataSize;
    }

    internal static int EncodeTwoByteEnvelope(
        RtpHeaderExtension extension,
        Span<byte> destination)
    {
        int encodedSize = GetTwoByteEnvelopeEncodedSize(extension);
        if (destination.Length < encodedSize)
        {
            throw new RtpBufferCapacityException(
                "The destination is too small for the RTP header extension.");
        }

        EncodeValidatedTwoByteEnvelope(extension, destination);
        return encodedSize;
    }

    internal static RtpCodecResult TryEncodeTwoByteEnvelope(
        RtpHeaderExtension extension,
        Span<byte> destination)
    {
        RtpCodecResult result = new RtpCodecResult();
        string? validationError = GetTwoByteValidationError(extension);
        if (validationError != null)
        {
            result.ErrorMessage = validationError;
            return result;
        }

        int encodedSize = RtpProtocolConstants.HeaderExtensionEnvelopeSize +
            (int)CalculateTwoByteDataSize(extension);
        if (destination.Length < encodedSize)
        {
            result.ErrorMessage =
                "The destination is too small for the RTP header extension.";
            return result;
        }

        EncodeValidatedTwoByteEnvelope(extension, destination);
        result.Success = true;
        result.WrittenCount = encodedSize;
        return result;
    }

    internal static RtpHeaderExtension DecodeTwoByteEnvelope(ReadOnlySpan<byte> source)
    {
        RtpCodecResult result = TryDecodeTwoByteEnvelope(source);
        if (!result.Success)
        {
            string errorMessage = "The two-byte RTP header extension could not be decoded.";
            if (result.ErrorMessage != null)
            {
                errorMessage = result.ErrorMessage;
            }

            throw new RtpPacketFormatException(errorMessage);
        }

        RtpHeaderExtension? extension = result.HeaderExtension;
        if (extension == null)
        {
            throw new RtpPacketFormatException(
                "The two-byte RTP header-extension decoder returned no extension.");
        }

        return extension;
    }

    internal static RtpCodecResult TryDecodeTwoByteEnvelope(ReadOnlySpan<byte> source)
    {
        if (source.Length < RtpProtocolConstants.HeaderExtensionEnvelopeSize)
        {
            return CreateExtensionFailure(
                "The RTP header-extension envelope is truncated.");
        }

        ushort profile = NetworkBinaryReader.ReadUInt16(source, 0);
        if ((profile & RtpHeaderExtensionProfiles.TwoByteProfileMask) !=
            RtpHeaderExtensionProfiles.TwoByte)
        {
            return CreateExtensionFailure(
                "The RTP header extension does not use a two-byte profile.");
        }

        int wordCount = NetworkBinaryReader.ReadUInt16(source, 2);
        int dataLength = wordCount * RtpProtocolConstants.HeaderExtensionWordSize;
        if (dataLength > source.Length - RtpProtocolConstants.HeaderExtensionEnvelopeSize)
        {
            return CreateExtensionFailure(
                "The RTP header-extension data is truncated.");
        }

        ReadOnlySpan<byte> data = NetworkBinaryReader.ReadSlice(
            source,
            RtpProtocolConstants.HeaderExtensionEnvelopeSize,
            dataLength);
        RtpCodecResult result = ParseTwoByteData(profile, data);
        if (result.Success)
        {
            result.ConsumedCount = RtpProtocolConstants.HeaderExtensionEnvelopeSize + dataLength;
        }

        return result;
    }

    internal static RtpCodecResult ParseTwoByteData(
        ushort profile,
        ReadOnlySpan<byte> data)
    {
        if ((profile & RtpHeaderExtensionProfiles.TwoByteProfileMask) !=
            RtpHeaderExtensionProfiles.TwoByte)
        {
            return CreateExtensionFailure(
                "The RTP header extension does not use a two-byte profile.");
        }

        if (data.Length % RtpProtocolConstants.HeaderExtensionWordSize != 0)
        {
            return CreateExtensionFailure(
                "The RTP header-extension data is not aligned to four bytes.");
        }

        List<RtpHeaderExtensionElement> elements = new List<RtpHeaderExtensionElement>();
        int offset = 0;
        while (offset < data.Length)
        {
            int identifier = NetworkBinaryReader.ReadByte(data, offset);
            if (identifier == RtpHeaderExtensionProfiles.PaddingIdentifier)
            {
                offset = offset + 1;
            }
            else
            {
                int lengthOffset = offset + 1;
                if (lengthOffset >= data.Length)
                {
                    return CreateExtensionFailure(
                        "A two-byte RTP header-extension element is missing its length byte.");
                }

                int dataLength = NetworkBinaryReader.ReadByte(data, lengthOffset);
                int dataOffset = offset + RtpHeaderExtensionProfiles.TwoByteElementHeaderSize;
                if (dataLength > data.Length - dataOffset)
                {
                    return CreateExtensionFailure(
                        "A two-byte RTP header-extension element has truncated data.");
                }

                if (ContainsElementIdentifier(elements, identifier))
                {
                    return CreateExtensionFailure(
                        "The RTP header extension contains a duplicate element identifier.");
                }

                RtpHeaderExtensionElement element = new RtpHeaderExtensionElement();
                element.Identifier = identifier;
                element.Data = NetworkBinaryReader.ReadSlice(
                    data,
                    dataOffset,
                    dataLength).ToArray();
                elements.Add(element);
                offset = dataOffset + dataLength;
            }
        }

        RtpHeaderExtension extension = new RtpHeaderExtension();
        extension.Profile = profile;
        extension.Format = RtpHeaderExtensionFormat.TwoByte;
        extension.ApplicationBits = (byte)(profile &
            RtpHeaderExtensionProfiles.TwoByteApplicationBitsMask);
        extension.Elements = elements.ToArray();
        extension.Data = data.ToArray();

        RtpCodecResult result = new RtpCodecResult();
        result.Success = true;
        result.ConsumedCount = data.Length;
        result.HeaderExtension = extension;
        return result;
    }

    internal static string? GetTwoByteValidationError(RtpHeaderExtension extension)
    {
        if (extension == null)
        {
            return "The RTP header extension is required.";
        }

        if (extension.Format != RtpHeaderExtensionFormat.TwoByte)
        {
            return "The RTP header extension must use the two-byte format.";
        }

        if (extension.ApplicationBits >
            RtpHeaderExtensionProfiles.TwoByteApplicationBitsMask)
        {
            return "Two-byte RTP header-extension application bits must fit in four bits.";
        }

        if ((extension.Profile & RtpHeaderExtensionProfiles.TwoByteProfileMask) !=
            RtpHeaderExtensionProfiles.TwoByte ||
            (extension.Profile & RtpHeaderExtensionProfiles.TwoByteApplicationBitsMask) !=
            extension.ApplicationBits)
        {
            return "The two-byte RTP header-extension profile and application bits disagree.";
        }

        if (extension.Elements == null)
        {
            return "The RTP header-extension element array is required.";
        }

        if (extension.Data == null)
        {
            return "The RTP header-extension data array is required.";
        }

        long unalignedSize = 0;
        int elementIndex = 0;
        while (elementIndex < extension.Elements.Length)
        {
            RtpHeaderExtensionElement? element = extension.Elements[elementIndex];
            if (element == null)
            {
                return "An RTP header-extension element is required.";
            }

            if (element.Identifier < RtpHeaderExtensionProfiles.TwoByteIdentifierMinimum ||
                element.Identifier > RtpHeaderExtensionProfiles.TwoByteIdentifierMaximum)
            {
                return "A two-byte RTP header-extension identifier must be between 1 and 255.";
            }

            if (element.Data == null)
            {
                return "The RTP header-extension element data array is required.";
            }

            if (element.Data.Length < RtpHeaderExtensionProfiles.TwoByteDataLengthMinimum ||
                element.Data.Length > RtpHeaderExtensionProfiles.TwoByteDataLengthMaximum)
            {
                return "Two-byte RTP header-extension element data cannot exceed 255 bytes.";
            }

            unalignedSize = unalignedSize +
                RtpHeaderExtensionProfiles.TwoByteElementHeaderSize + element.Data.Length;
            elementIndex = elementIndex + 1;
        }

        string? duplicateError = GetDuplicateElementError(extension.Elements);
        if (duplicateError != null)
        {
            return duplicateError;
        }

        long padding = (4 - (unalignedSize & 3)) & 3;
        if (unalignedSize + padding > RtpProtocolConstants.MaximumHeaderExtensionDataSize)
        {
            return "The encoded RTP header-extension data is too large.";
        }

        return null;
    }

    internal static RtpHeaderExtension AddElementCopy(
        RtpHeaderExtension extension,
        RtpHeaderExtensionElement element)
    {
        string? extensionError = GetEditableExtensionError(extension);
        if (extensionError != null)
        {
            throw new RtpPacketFormatException(extensionError);
        }

        string? elementError = GetElementOperationError(element);
        if (elementError != null)
        {
            throw new RtpPacketFormatException(elementError);
        }

        int existingIndex = FindElementIndex(extension.Elements, element.Identifier);
        if (existingIndex >= 0)
        {
            throw new RtpPacketFormatException(
                "The RTP header extension already contains the element identifier.");
        }

        RtpHeaderExtensionElement[] elements =
            new RtpHeaderExtensionElement[extension.Elements.Length + 1];
        int index = 0;
        while (index < extension.Elements.Length)
        {
            elements[index] = extension.Elements[index];
            index = index + 1;
        }

        elements[elements.Length - 1] = element;
        return CreateCanonicalExtension(elements, extension.ApplicationBits);
    }

    internal static RtpHeaderExtension ReplaceElementCopy(
        RtpHeaderExtension extension,
        RtpHeaderExtensionElement element)
    {
        string? extensionError = GetEditableExtensionError(extension);
        if (extensionError != null)
        {
            throw new RtpPacketFormatException(extensionError);
        }

        string? elementError = GetElementOperationError(element);
        if (elementError != null)
        {
            throw new RtpPacketFormatException(elementError);
        }

        int replacementIndex = FindElementIndex(extension.Elements, element.Identifier);
        if (replacementIndex < 0)
        {
            throw new RtpPacketFormatException(
                "The RTP header extension does not contain the element identifier.");
        }

        RtpHeaderExtensionElement[] elements =
            new RtpHeaderExtensionElement[extension.Elements.Length];
        int index = 0;
        while (index < extension.Elements.Length)
        {
            elements[index] = extension.Elements[index];
            index = index + 1;
        }

        elements[replacementIndex] = element;
        return CreateCanonicalExtension(elements, extension.ApplicationBits);
    }

    internal static RtpHeaderExtension RemoveElementCopy(
        RtpHeaderExtension extension,
        int identifier)
    {
        string? extensionError = GetEditableExtensionError(extension);
        if (extensionError != null)
        {
            throw new RtpPacketFormatException(extensionError);
        }

        if (identifier < RtpHeaderExtensionProfiles.TwoByteIdentifierMinimum ||
            identifier > RtpHeaderExtensionProfiles.TwoByteIdentifierMaximum)
        {
            throw new RtpPacketFormatException(
                "An RTP header-extension identifier must be between 1 and 255.");
        }

        int removalIndex = FindElementIndex(extension.Elements, identifier);
        if (removalIndex < 0)
        {
            throw new RtpPacketFormatException(
                "The RTP header extension does not contain the element identifier.");
        }

        RtpHeaderExtensionElement[] elements =
            new RtpHeaderExtensionElement[extension.Elements.Length - 1];
        int sourceIndex = 0;
        int destinationIndex = 0;
        while (sourceIndex < extension.Elements.Length)
        {
            if (sourceIndex != removalIndex)
            {
                elements[destinationIndex] = extension.Elements[sourceIndex];
                destinationIndex = destinationIndex + 1;
            }

            sourceIndex = sourceIndex + 1;
        }

        return CreateCanonicalExtension(elements, extension.ApplicationBits);
    }

    internal static RtpHeaderExtensionElement? FindElementCopy(
        RtpHeaderExtension extension,
        int identifier)
    {
        string? extensionError = GetEditableExtensionError(extension);
        if (extensionError != null)
        {
            throw new RtpPacketFormatException(extensionError);
        }

        if (identifier < RtpHeaderExtensionProfiles.TwoByteIdentifierMinimum ||
            identifier > RtpHeaderExtensionProfiles.TwoByteIdentifierMaximum)
        {
            throw new RtpPacketFormatException(
                "An RTP header-extension identifier must be between 1 and 255.");
        }

        int elementIndex = FindElementIndex(extension.Elements, identifier);
        if (elementIndex < 0)
        {
            return null;
        }

        return extension.Elements[elementIndex].Copy();
    }

    static string? GetEditableExtensionError(RtpHeaderExtension extension)
    {
        if (extension == null)
        {
            return "The RTP header extension is required.";
        }

        if (extension.Format == RtpHeaderExtensionFormat.OneByte)
        {
            return GetOneByteValidationError(extension);
        }

        if (extension.Format == RtpHeaderExtensionFormat.TwoByte)
        {
            return GetTwoByteValidationError(extension);
        }

        if (extension.Format == RtpHeaderExtensionFormat.Raw &&
            extension.Profile == 0 && extension.Data != null &&
            extension.Data.Length == 0)
        {
            return GetGeneralElementsValidationError(extension);
        }

        return "Opaque RTP header extensions do not support element operations.";
    }

    static string? GetGeneralElementsValidationError(RtpHeaderExtension extension)
    {
        if (extension.ApplicationBits >
            RtpHeaderExtensionProfiles.TwoByteApplicationBitsMask)
        {
            return "RTP header-extension application bits must fit in four bits.";
        }

        if (extension.Elements == null)
        {
            return "The RTP header-extension element array is required.";
        }

        if (extension.Data == null)
        {
            return "The RTP header-extension data array is required.";
        }

        long dataSize = 0;
        int elementIndex = 0;
        while (elementIndex < extension.Elements.Length)
        {
            string? elementError = GetElementOperationError(
                extension.Elements[elementIndex]);
            if (elementError != null)
            {
                return elementError;
            }

            dataSize = dataSize + RtpHeaderExtensionProfiles.TwoByteElementHeaderSize +
                extension.Elements[elementIndex].Data.Length;
            elementIndex = elementIndex + 1;
        }

        string? duplicateError = GetDuplicateElementError(extension.Elements);
        if (duplicateError != null)
        {
            return duplicateError;
        }

        long padding = (4 - (dataSize & 3)) & 3;
        if (dataSize + padding > RtpProtocolConstants.MaximumHeaderExtensionDataSize)
        {
            return "The encoded RTP header-extension data is too large.";
        }

        return null;
    }

    static string? GetElementOperationError(RtpHeaderExtensionElement element)
    {
        if (element == null)
        {
            return "The RTP header-extension element is required.";
        }

        if (element.Identifier < RtpHeaderExtensionProfiles.TwoByteIdentifierMinimum ||
            element.Identifier > RtpHeaderExtensionProfiles.TwoByteIdentifierMaximum)
        {
            return "An RTP header-extension identifier must be between 1 and 255.";
        }

        if (element.Data == null)
        {
            return "The RTP header-extension element data array is required.";
        }

        if (element.Data.Length > RtpHeaderExtensionProfiles.TwoByteDataLengthMaximum)
        {
            return "RTP header-extension element data cannot exceed 255 bytes.";
        }

        return null;
    }

    static bool IsAutomaticElementExtension(RtpHeaderExtension extension)
    {
        return extension.Format == RtpHeaderExtensionFormat.Raw &&
            extension.Profile == 0 && extension.Data != null &&
            extension.Data.Length == 0 && extension.Elements != null &&
            extension.Elements.Length > 0;
    }

    internal static RtpHeaderExtension GetSelectedEncodingExtension(
        RtpHeaderExtension extension)
    {
        if (!IsAutomaticElementExtension(extension))
        {
            return extension;
        }

        return CreateCanonicalExtension(extension.Elements, extension.ApplicationBits);
    }

    static RtpHeaderExtension CreateCanonicalExtension(
        RtpHeaderExtensionElement[] elements,
        byte applicationBits)
    {
        bool supportsOneByte = applicationBits == 0;
        int elementIndex = 0;
        while (elementIndex < elements.Length)
        {
            RtpHeaderExtensionElement element = elements[elementIndex];
            if (element.Identifier > RtpHeaderExtensionProfiles.OneByteIdentifierMaximum ||
                element.Data.Length < RtpHeaderExtensionProfiles.OneByteDataLengthMinimum ||
                element.Data.Length > RtpHeaderExtensionProfiles.OneByteDataLengthMaximum)
            {
                supportsOneByte = false;
            }

            elementIndex = elementIndex + 1;
        }

        RtpHeaderExtension extension = new RtpHeaderExtension();
        extension.Elements = new RtpHeaderExtensionElement[elements.Length];
        elementIndex = 0;
        while (elementIndex < elements.Length)
        {
            extension.Elements[elementIndex] = elements[elementIndex].Copy();
            elementIndex = elementIndex + 1;
        }

        extension.Elements = GetOrderedElements(extension.Elements);
        if (supportsOneByte)
        {
            extension.Format = RtpHeaderExtensionFormat.OneByte;
            extension.Profile = RtpHeaderExtensionProfiles.OneByte;
        }
        else
        {
            extension.Format = RtpHeaderExtensionFormat.TwoByte;
            extension.ApplicationBits = applicationBits;
            extension.Profile = (ushort)(RtpHeaderExtensionProfiles.TwoByte | applicationBits);
        }

        return extension;
    }

    static int FindElementIndex(
        RtpHeaderExtensionElement[] elements,
        int identifier)
    {
        int elementIndex = 0;
        while (elementIndex < elements.Length)
        {
            if (elements[elementIndex].Identifier == identifier)
            {
                return elementIndex;
            }

            elementIndex = elementIndex + 1;
        }

        return -1;
    }

    static string? GetDuplicateElementError(RtpHeaderExtensionElement[] elements)
    {
        bool[] identifiers = new bool[256];
        int elementIndex = 0;
        while (elementIndex < elements.Length)
        {
            int identifier = elements[elementIndex].Identifier;
            if (identifiers[identifier])
            {
                return "The RTP header extension contains a duplicate element identifier.";
            }

            identifiers[identifier] = true;
            elementIndex = elementIndex + 1;
        }

        return null;
    }

    static bool ContainsElementIdentifier(
        List<RtpHeaderExtensionElement> elements,
        int identifier)
    {
        int elementIndex = 0;
        while (elementIndex < elements.Count)
        {
            if (elements[elementIndex].Identifier == identifier)
            {
                return true;
            }

            elementIndex = elementIndex + 1;
        }

        return false;
    }

    static RtpHeaderExtensionElement[] GetOrderedElements(
        RtpHeaderExtensionElement[] elements)
    {
        RtpHeaderExtensionElement[] ordered =
            new RtpHeaderExtensionElement[elements.Length];
        int elementIndex = 0;
        while (elementIndex < elements.Length)
        {
            ordered[elementIndex] = elements[elementIndex];
            elementIndex = elementIndex + 1;
        }

        elementIndex = 1;
        while (elementIndex < ordered.Length)
        {
            RtpHeaderExtensionElement element = ordered[elementIndex];
            int insertionIndex = elementIndex;
            while (insertionIndex > 0 &&
                ordered[insertionIndex - 1].Identifier > element.Identifier)
            {
                ordered[insertionIndex] = ordered[insertionIndex - 1];
                insertionIndex = insertionIndex - 1;
            }

            ordered[insertionIndex] = element;
            elementIndex = elementIndex + 1;
        }

        return ordered;
    }

    static long CalculateOneByteDataSize(RtpHeaderExtension extension)
    {
        long size = 0;
        int elementIndex = 0;
        while (elementIndex < extension.Elements.Length)
        {
            size = size + RtpHeaderExtensionProfiles.OneByteElementHeaderSize +
                extension.Elements[elementIndex].Data.Length;
            elementIndex = elementIndex + 1;
        }

        long padding = (4 - (size & 3)) & 3;
        return size + padding;
    }

    static long CalculateTwoByteDataSize(RtpHeaderExtension extension)
    {
        long size = 0;
        int elementIndex = 0;
        while (elementIndex < extension.Elements.Length)
        {
            size = size + RtpHeaderExtensionProfiles.TwoByteElementHeaderSize +
                extension.Elements[elementIndex].Data.Length;
            elementIndex = elementIndex + 1;
        }

        long padding = (4 - (size & 3)) & 3;
        return size + padding;
    }

    static void EncodeValidatedOneByteEnvelope(
        RtpHeaderExtension extension,
        Span<byte> destination)
    {
        int dataSize = (int)CalculateOneByteDataSize(extension);
        NetworkBinaryWriter.WriteUInt16(destination, 0, RtpHeaderExtensionProfiles.OneByte);
        NetworkBinaryWriter.WriteUInt16(
            destination,
            2,
            (ushort)(dataSize / RtpProtocolConstants.HeaderExtensionWordSize));

        int offset = RtpProtocolConstants.HeaderExtensionEnvelopeSize;
        RtpHeaderExtensionElement[] orderedElements = GetOrderedElements(extension.Elements);
        int elementIndex = 0;
        while (elementIndex < orderedElements.Length)
        {
            RtpHeaderExtensionElement element = orderedElements[elementIndex];
            byte elementHeader = (byte)((element.Identifier << 4) |
                (element.Data.Length - 1));
            NetworkBinaryWriter.WriteByte(destination, offset, elementHeader);
            offset = offset + RtpHeaderExtensionProfiles.OneByteElementHeaderSize;
            NetworkBinaryWriter.WriteBytes(destination, offset, element.Data);
            offset = offset + element.Data.Length;
            elementIndex = elementIndex + 1;
        }

        int endOffset = RtpProtocolConstants.HeaderExtensionEnvelopeSize + dataSize;
        while (offset < endOffset)
        {
            NetworkBinaryWriter.WriteByte(destination, offset, 0);
            offset = offset + 1;
        }
    }

    static void EncodeValidatedTwoByteEnvelope(
        RtpHeaderExtension extension,
        Span<byte> destination)
    {
        int dataSize = (int)CalculateTwoByteDataSize(extension);
        NetworkBinaryWriter.WriteUInt16(destination, 0, extension.Profile);
        NetworkBinaryWriter.WriteUInt16(
            destination,
            2,
            (ushort)(dataSize / RtpProtocolConstants.HeaderExtensionWordSize));

        int offset = RtpProtocolConstants.HeaderExtensionEnvelopeSize;
        RtpHeaderExtensionElement[] orderedElements = GetOrderedElements(extension.Elements);
        int elementIndex = 0;
        while (elementIndex < orderedElements.Length)
        {
            RtpHeaderExtensionElement element = orderedElements[elementIndex];
            NetworkBinaryWriter.WriteByte(destination, offset, (byte)element.Identifier);
            NetworkBinaryWriter.WriteByte(destination, offset + 1, (byte)element.Data.Length);
            offset = offset + RtpHeaderExtensionProfiles.TwoByteElementHeaderSize;
            NetworkBinaryWriter.WriteBytes(destination, offset, element.Data);
            offset = offset + element.Data.Length;
            elementIndex = elementIndex + 1;
        }

        int endOffset = RtpProtocolConstants.HeaderExtensionEnvelopeSize + dataSize;
        while (offset < endOffset)
        {
            NetworkBinaryWriter.WriteByte(destination, offset, 0);
            offset = offset + 1;
        }
    }

    static RtpCodecResult CreateExtensionFailure(string errorMessage)
    {
        RtpCodecResult result = new RtpCodecResult();
        result.ErrorMessage = errorMessage;
        return result;
    }
}
