using System;
using System.Collections.Generic;

namespace diameter;

/// <summary>Adapts raw application messages and AVPs through dictionary-selected codecs.</summary>
public class DiameterApplicationAdapter : IDiameterApplicationAdapter
{
    internal IDiameterDictionary Dictionary;
    internal IDiameterAvpCodec Codec;
    internal DiameterLimitConfiguration Limits;

    /// <summary>Initializes an application adapter with dictionary, codec, and resource limits.</summary>
    /// <param name="dictionary">The dictionary used to discover AVP definitions.</param>
    /// <param name="codec">The codec used for typed AVP values.</param>
    /// <param name="limits">The limits applied to Grouped values.</param>
    public DiameterApplicationAdapter(
        IDiameterDictionary? dictionary,
        IDiameterAvpCodec? codec,
        DiameterLimitConfiguration? limits)
    {
        if (dictionary == null || codec == null || limits == null)
        {
            throw new DiameterApplicationException(
                "An application adapter requires a dictionary, codec, and limits.");
        }

        limits.ValidateConfiguration();
        Dictionary = dictionary;
        Codec = codec;
        Limits = CloneApplicationAdapterLimits(limits);
    }

    /// <summary>Decodes known AVPs while retaining owned raw values and ordering.</summary>
    /// <param name="message">The raw application message.</param>
    /// <param name="referenceTime">The reference used to select a Diameter Time era.</param>
    /// <returns>The owned raw and typed message representation.</returns>
    public DiameterTypedMessage DecodeDiameterTypedMessage(
        DiameterMessage? message,
        DateTimeOffset referenceTime)
    {
        if (message == null)
        {
            throw new DiameterApplicationException(
                "A raw application message and its AVPs are required.");
        }

        ValidateApplicationAdapterMessage(message);
        DiameterDictionarySnapshot snapshot = ReadApplicationAdapterSnapshot();
        DiameterTypedMessage result = new DiameterTypedMessage();
        result.RawMessage = CloneApplicationAdapterMessage(message);
        int index = 0;
        while (index < message.Avps.Count)
        {
            result.Avps.Add(DecodeApplicationTypedAvp(
                message.Avps[index],
                referenceTime,
                snapshot));
            index = index + 1;
        }

        return result;
    }

    /// <summary>Encodes typed known AVPs and forwards unknown raw AVPs unchanged.</summary>
    /// <param name="message">The typed application message.</param>
    /// <returns>The owned raw Diameter message.</returns>
    public DiameterMessage EncodeDiameterTypedMessage(
        DiameterTypedMessage? message)
    {
        if (message == null || message.RawMessage == null ||
            message.Avps == null)
        {
            throw new DiameterApplicationException(
                "A typed application message and its AVPs are required.");
        }

        DiameterDictionarySnapshot snapshot = ReadApplicationAdapterSnapshot();
        DiameterMessage result =
            CloneApplicationAdapterHeader(message.RawMessage);
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterTypedAvp item = message.Avps[index];
            if (item == null)
            {
                throw new DiameterApplicationException(
                    "Typed application AVPs must not contain null values.");
            }

            result.Avps.Add(EncodeApplicationTypedAvp(item, snapshot));
            index = index + 1;
        }

        return result;
    }

    /// <summary>Decodes one known AVP or retains one unknown raw AVP.</summary>
    /// <param name="avp">The raw AVP.</param>
    /// <param name="referenceTime">The reference used to select a Diameter Time era.</param>
    /// <returns>The owned raw and typed AVP representation.</returns>
    public DiameterTypedAvp DecodeDiameterTypedAvp(
        DiameterAvp? avp,
        DateTimeOffset referenceTime)
    {
        if (avp == null)
        {
            throw new DiameterApplicationException(
                "A raw application AVP and its data are required.");
        }

        ValidateApplicationAdapterAvp(avp);
        return DecodeApplicationTypedAvp(
            avp,
            referenceTime,
            ReadApplicationAdapterSnapshot());
    }

    /// <summary>Encodes one typed known AVP or forwards one unknown raw AVP unchanged.</summary>
    /// <param name="avp">The typed AVP representation.</param>
    /// <returns>The owned raw AVP.</returns>
    public DiameterAvp EncodeDiameterTypedAvp(DiameterTypedAvp? avp)
    {
        if (avp == null)
        {
            throw new DiameterApplicationException(
                "A typed application AVP is required.");
        }

        return EncodeApplicationTypedAvp(
            avp,
            ReadApplicationAdapterSnapshot());
    }

    internal DiameterTypedAvp DecodeApplicationTypedAvp(
        DiameterAvp avp,
        DateTimeOffset referenceTime,
        DiameterDictionarySnapshot snapshot)
    {
        ValidateApplicationAdapterAvp(avp);
        DiameterTypedAvp result = new DiameterTypedAvp();
        result.RawAvp = CloneApplicationAdapterAvp(avp);
        DiameterAvpDefinition? definition =
            new DictionaryIndexes(snapshot).FindAvpByCode(
                avp.VendorId,
                avp.Code);
        if (definition == null)
        {
            result.Value = Codec.DecodeOctetString(avp.Data);
            result.PreserveRaw = true;
            return result;
        }

        result.Known = true;
        result.Definition =
            DictionaryDefinitionComparer.CloneAvp(definition);
        string valueType = ReadApplicationAdapterValueType(
            definition,
            snapshot);
        result.Value = DecodeApplicationAdapterValue(
            avp.Data,
            definition,
            valueType,
            referenceTime,
            snapshot);
        result.Decoded =
            result.Value.Status == DiameterCodecStatus.Success;
        result.PreserveRaw = !result.Decoded;
        return result;
    }

    internal DiameterAvp EncodeApplicationTypedAvp(
        DiameterTypedAvp avp,
        DiameterDictionarySnapshot snapshot)
    {
        if (avp.RawAvp == null || avp.RawAvp.Data == null ||
            avp.Value == null || avp.Value.Data == null ||
            avp.Value.Avps == null || avp.Value.Text == null ||
            avp.Value.Issues == null)
        {
            throw new DiameterApplicationException(
                "A typed application AVP requires raw and value storage.");
        }

        if (!avp.Known || avp.PreserveRaw || !avp.Decoded)
        {
            return CloneApplicationAdapterAvp(avp.RawAvp);
        }

        if (avp.Definition == null)
        {
            throw new DiameterApplicationException(
                "A known typed application AVP requires a dictionary definition.");
        }

        string valueType = ReadApplicationAdapterValueType(
            avp.Definition,
            snapshot);
        DiameterValueResult encoding = EncodeApplicationAdapterValue(
            avp.Value,
            avp.Definition,
            valueType,
            snapshot);
        if (encoding.Status != DiameterCodecStatus.Success)
        {
            throw new DiameterApplicationException(
                ReadApplicationAdapterEncodingFailure(encoding));
        }

        DiameterAvp result = new DiameterAvp();
        result.Code = avp.Definition.Code;
        result.VendorId = avp.Definition.VendorId;
        result.Flags = avp.RawAvp.Flags |
            avp.Definition.RequiredFlags;
        result.Flags = result.Flags &
            ~avp.Definition.ProhibitedFlags;
        if (result.VendorId == DiameterVendorIds.Ietf)
        {
            result.Flags = result.Flags &
                ~DiameterAvpFlags.VendorSpecific;
        }
        else
        {
            result.Flags = result.Flags |
                DiameterAvpFlags.VendorSpecific;
        }

        result.Data = encoding.Data;
        return result;
    }

    internal DiameterValueResult DecodeApplicationAdapterValue(
        byte[] data,
        DiameterAvpDefinition definition,
        string valueType,
        DateTimeOffset referenceTime,
        DiameterDictionarySnapshot snapshot)
    {
        if (valueType == "Address")
        {
            return Codec.DecodeAddress(data);
        }

        if (valueType == "Time")
        {
            return Codec.DecodeTime(data, referenceTime);
        }

        if (valueType == "UTF8String")
        {
            return Codec.DecodeUtf8String(data);
        }

        if (valueType == "DiameterIdentity")
        {
            return Codec.DecodeDiameterIdentity(data);
        }

        if (valueType == "DiameterURI")
        {
            return Codec.DecodeDiameterUri(data);
        }

        if (valueType == "IPFilterRule")
        {
            return Codec.DecodeIpFilterRule(data);
        }

        if (valueType == "QoSFilterRule")
        {
            return Codec.DecodeQosFilterRule(data);
        }

        if (valueType == "Enumerated")
        {
            return Codec.DecodeEnumerated(data);
        }

        if (valueType == "Grouped" ||
            definition.BaseType == DiameterAvpBaseType.Grouped)
        {
            return Codec.DecodeGrouped(
                data,
                CreateApplicationAdapterGroupedTypes(snapshot),
                Limits);
        }

        if (valueType == "Integer32" ||
            definition.BaseType == DiameterAvpBaseType.Integer32)
        {
            return Codec.DecodeInteger32(data);
        }

        if (valueType == "Integer64" ||
            definition.BaseType == DiameterAvpBaseType.Integer64)
        {
            return Codec.DecodeInteger64(data);
        }

        if (valueType == "Unsigned32" ||
            definition.BaseType == DiameterAvpBaseType.Unsigned32)
        {
            return Codec.DecodeUnsigned32(data);
        }

        if (valueType == "Unsigned64" ||
            definition.BaseType == DiameterAvpBaseType.Unsigned64)
        {
            return Codec.DecodeUnsigned64(data);
        }

        if (valueType == "Float32" ||
            definition.BaseType == DiameterAvpBaseType.Float32)
        {
            return Codec.DecodeFloat32(data);
        }

        if (valueType == "Float64" ||
            definition.BaseType == DiameterAvpBaseType.Float64)
        {
            return Codec.DecodeFloat64(data);
        }

        return Codec.DecodeOctetString(data);
    }

    internal DiameterValueResult EncodeApplicationAdapterValue(
        DiameterValueResult value,
        DiameterAvpDefinition definition,
        string valueType,
        DiameterDictionarySnapshot snapshot)
    {
        if (valueType == "Integer32" ||
            definition.BaseType == DiameterAvpBaseType.Integer32)
        {
            byte[] data = new byte[4];
            return CompleteApplicationAdapterEncoding(
                Codec.EncodeInteger32(data, value.Integer32),
                data);
        }

        if (valueType == "Integer64" ||
            definition.BaseType == DiameterAvpBaseType.Integer64)
        {
            byte[] data = new byte[8];
            return CompleteApplicationAdapterEncoding(
                Codec.EncodeInteger64(data, value.Integer64),
                data);
        }

        if (valueType == "Unsigned32" ||
            definition.BaseType == DiameterAvpBaseType.Unsigned32)
        {
            byte[] data = new byte[4];
            return CompleteApplicationAdapterEncoding(
                Codec.EncodeUnsigned32(data, value.Unsigned32),
                data);
        }

        if (valueType == "Unsigned64" ||
            definition.BaseType == DiameterAvpBaseType.Unsigned64)
        {
            byte[] data = new byte[8];
            return CompleteApplicationAdapterEncoding(
                Codec.EncodeUnsigned64(data, value.Unsigned64),
                data);
        }

        if (valueType == "Float32" ||
            definition.BaseType == DiameterAvpBaseType.Float32)
        {
            byte[] data = new byte[4];
            return CompleteApplicationAdapterEncoding(
                Codec.EncodeFloat32(data, value.Float32),
                data);
        }

        if (valueType == "Float64" ||
            definition.BaseType == DiameterAvpBaseType.Float64)
        {
            byte[] data = new byte[8];
            return CompleteApplicationAdapterEncoding(
                Codec.EncodeFloat64(data, value.Float64),
                data);
        }

        if (valueType == "Enumerated")
        {
            byte[] data = new byte[4];
            return CompleteApplicationAdapterEncoding(
                Codec.EncodeEnumerated(data, value.Integer32),
                data);
        }

        if (valueType == "Time")
        {
            byte[] data = new byte[4];
            return CompleteApplicationAdapterEncoding(
                Codec.EncodeTime(data, value.Time),
                data);
        }

        if (valueType == "Grouped" ||
            definition.BaseType == DiameterAvpBaseType.Grouped)
        {
            List<DiameterAvp> groupedTypes =
                CreateApplicationAdapterGroupedTypes(snapshot);
            DiameterValueResult measurement = Codec.MeasureGrouped(
                value.Avps,
                groupedTypes,
                Limits);
            if (measurement.Status != DiameterCodecStatus.Success)
            {
                return measurement;
            }

            byte[] data = new byte[measurement.RequiredCount];
            return CompleteApplicationAdapterEncoding(
                Codec.EncodeGrouped(
                    data,
                    value.Avps,
                    groupedTypes,
                    Limits),
                data);
        }

        if (valueType == "OctetString")
        {
            byte[] data = new byte[value.Data.Length];
            return CompleteApplicationAdapterEncoding(
                Codec.EncodeOctetString(data, value.Data),
                data);
        }

        DiameterValueResult sizing =
            MeasureApplicationAdapterVariableValue(value, valueType);
        if (sizing.Status != DiameterCodecStatus.Success &&
            sizing.Status != DiameterCodecStatus.DestinationTooSmall)
        {
            return sizing;
        }

        byte[] variableData = new byte[sizing.RequiredCount];
        DiameterValueResult encoding =
            EncodeApplicationAdapterVariableValue(
                variableData,
                value,
                valueType);
        return CompleteApplicationAdapterEncoding(
            encoding,
            variableData);
    }

    internal DiameterValueResult MeasureApplicationAdapterVariableValue(
        DiameterValueResult value,
        string valueType)
    {
        Span<byte> destination = Span<byte>.Empty;
        if (valueType == "Address")
        {
            return Codec.EncodeAddress(destination, value.Address);
        }

        if (valueType == "UTF8String")
        {
            return Codec.EncodeUtf8String(destination, value.Text);
        }

        if (valueType == "DiameterIdentity")
        {
            return Codec.EncodeDiameterIdentity(
                destination,
                value.Identity);
        }

        if (valueType == "DiameterURI")
        {
            return Codec.EncodeDiameterUri(destination, value.Uri);
        }

        if (valueType == "IPFilterRule")
        {
            return Codec.EncodeIpFilterRule(destination, value.Text);
        }

        if (valueType == "QoSFilterRule")
        {
            return Codec.EncodeQosFilterRule(destination, value.Text);
        }

        return ValueCodecs.CreateValueFailure(
            DiameterCodecStatus.Invalid,
            0,
            0,
            "unsupported-adapter-value-type",
            "The dictionary value type is not supported by the application adapter.");
    }

    internal DiameterValueResult EncodeApplicationAdapterVariableValue(
        Span<byte> destination,
        DiameterValueResult value,
        string valueType)
    {
        if (valueType == "Address")
        {
            return Codec.EncodeAddress(destination, value.Address);
        }

        if (valueType == "UTF8String")
        {
            return Codec.EncodeUtf8String(destination, value.Text);
        }

        if (valueType == "DiameterIdentity")
        {
            return Codec.EncodeDiameterIdentity(
                destination,
                value.Identity);
        }

        if (valueType == "DiameterURI")
        {
            return Codec.EncodeDiameterUri(destination, value.Uri);
        }

        if (valueType == "IPFilterRule")
        {
            return Codec.EncodeIpFilterRule(destination, value.Text);
        }

        if (valueType == "QoSFilterRule")
        {
            return Codec.EncodeQosFilterRule(destination, value.Text);
        }

        return ValueCodecs.CreateValueFailure(
            DiameterCodecStatus.Invalid,
            0,
            0,
            "unsupported-adapter-value-type",
            "The dictionary value type is not supported by the application adapter.");
    }

    internal DiameterValueResult CompleteApplicationAdapterEncoding(
        DiameterValueResult result,
        byte[] data)
    {
        if (result.Status == DiameterCodecStatus.Success)
        {
            result.Data = data;
        }

        return result;
    }

    internal string ReadApplicationAdapterValueType(
        DiameterAvpDefinition definition,
        DiameterDictionarySnapshot snapshot)
    {
        return new AvpValidator(snapshot, Limits).ResolveValueType(
            definition);
    }

    internal List<DiameterAvp> CreateApplicationAdapterGroupedTypes(
        DiameterDictionarySnapshot snapshot)
    {
        List<DiameterAvp> result = new List<DiameterAvp>();
        int index = 0;
        while (index < snapshot.Avps.Count)
        {
            DiameterAvpDefinition definition = snapshot.Avps[index];
            string valueType = ReadApplicationAdapterValueType(
                definition,
                snapshot);
            if (valueType == "Grouped" ||
                definition.BaseType == DiameterAvpBaseType.Grouped)
            {
                DiameterAvp marker = new DiameterAvp();
                marker.Code = definition.Code;
                marker.VendorId = definition.VendorId;
                result.Add(marker);
            }

            index = index + 1;
        }

        return result;
    }

    internal DiameterDictionarySnapshot ReadApplicationAdapterSnapshot()
    {
        DiameterDictionarySnapshot snapshot =
            Dictionary.ReadDictionarySnapshot();
        if (snapshot == null || snapshot.Avps == null ||
            snapshot.Types == null)
        {
            throw new DiameterApplicationException(
                "The application dictionary did not provide a usable snapshot.");
        }

        return snapshot;
    }

    internal void ValidateApplicationAdapterMessage(DiameterMessage? message)
    {
        if (message == null || message.Avps == null)
        {
            throw new DiameterApplicationException(
                "A raw application message and its AVPs are required.");
        }

        int index = 0;
        while (index < message.Avps.Count)
        {
            ValidateApplicationAdapterAvp(message.Avps[index]);
            index = index + 1;
        }
    }

    internal void ValidateApplicationAdapterAvp(DiameterAvp? avp)
    {
        if (avp == null || avp.Data == null)
        {
            throw new DiameterApplicationException(
                "A raw application AVP and its data are required.");
        }
    }

    internal DiameterMessage CloneApplicationAdapterHeader(
        DiameterMessage source)
    {
        DiameterMessage result = new DiameterMessage();
        result.Version = source.Version;
        result.Flags = source.Flags;
        result.CommandCode = source.CommandCode;
        result.ApplicationId = source.ApplicationId;
        result.HopByHopIdentifier = source.HopByHopIdentifier;
        result.EndToEndIdentifier = source.EndToEndIdentifier;
        return result;
    }

    internal DiameterMessage CloneApplicationAdapterMessage(
        DiameterMessage source)
    {
        DiameterMessage result = CloneApplicationAdapterHeader(source);
        int index = 0;
        while (index < source.Avps.Count)
        {
            result.Avps.Add(CloneApplicationAdapterAvp(
                source.Avps[index]));
            index = index + 1;
        }

        return result;
    }

    internal DiameterAvp CloneApplicationAdapterAvp(DiameterAvp source)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = source.Code;
        result.Flags = source.Flags;
        result.VendorId = source.VendorId;
        result.Data = new byte[source.Data.Length];
        Array.Copy(source.Data, result.Data, source.Data.Length);
        return result;
    }

    internal DiameterLimitConfiguration CloneApplicationAdapterLimits(
        DiameterLimitConfiguration source)
    {
        DiameterLimitConfiguration result =
            new DiameterLimitConfiguration();
        result.MaximumMessageSize = source.MaximumMessageSize;
        result.MaximumAvpCount = source.MaximumAvpCount;
        result.MaximumGroupedDepth = source.MaximumGroupedDepth;
        result.MaximumGroupedAvpCount = source.MaximumGroupedAvpCount;
        result.MaximumGroupedBytes = source.MaximumGroupedBytes;
        result.MaximumCodecIssueCount = source.MaximumCodecIssueCount;
        result.MaximumConnectionCount = source.MaximumConnectionCount;
        result.MaximumPendingRequests = source.MaximumPendingRequests;
        result.IngressQueueCapacity = source.IngressQueueCapacity;
        result.EgressQueueCapacity = source.EgressQueueCapacity;
        result.ApplicationQueueCapacity = source.ApplicationQueueCapacity;
        return result;
    }

    internal string ReadApplicationAdapterEncodingFailure(
        DiameterValueResult result)
    {
        if (result.Issues != null && result.Issues.Count > 0 &&
            result.Issues[0] != null &&
            !string.IsNullOrWhiteSpace(result.Issues[0].Message))
        {
            return result.Issues[0].Message;
        }

        return "The typed application AVP value could not be encoded.";
    }
}
