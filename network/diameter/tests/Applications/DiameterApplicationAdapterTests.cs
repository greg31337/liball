using System;
using System.Collections.Generic;
using System.Net;

namespace diameter.Tests;

public class DiameterApplicationAdapterTests
{
    [Test]
    public void DictionarySelectedValuesCanBeEditedWhileUnknownAvpsRoundTrip()
    {
        DiameterDictionary dictionary = CreateDictionary();
        IDiameterAvpCodec codec = new DiameterAvpCodec();
        IDiameterApplicationAdapter adapter =
            new DiameterApplicationAdapter(
                dictionary,
                codec,
                new DiameterLimitConfiguration());
        DiameterMessage message = new DiameterMessage();
        message.Flags = DiameterMessageFlags.Request;
        message.CommandCode = 300U;
        message.ApplicationId = 9000U;
        message.HopByHopIdentifier = 11U;
        message.EndToEndIdentifier = 12U;
        message.Avps.Add(CreateAvp(
            1U,
            4242U,
            DiameterAvpFlags.VendorSpecific |
                DiameterAvpFlags.Mandatory,
            EncodeText(codec, "original")));
        message.Avps.Add(CreateAvp(
            2U,
            4242U,
            DiameterAvpFlags.VendorSpecific,
            EncodeUnsigned32(codec, 7U)));
        message.Avps.Add(CreateAvp(
            900U,
            9999U,
            DiameterAvpFlags.VendorSpecific |
                DiameterAvpFlags.Protected,
            new byte[] { 9, 8, 7 }));

        DiameterTypedMessage typed = adapter.DecodeDiameterTypedMessage(
            message,
            DateTimeOffset.UtcNow);
        message.Avps[0].Data[0] = 0;
        message.Avps[2].Data[0] = 0;
        typed.Avps[0].Value.Text = "changed";
        typed.Avps[1].Value.Unsigned32 = 99U;
        typed.Avps[0].Definition!.Name = "mutated-snapshot";

        DiameterMessage encoded =
            adapter.EncodeDiameterTypedMessage(typed);
        DiameterValueResult text = codec.DecodeUtf8String(
            encoded.Avps[0].Data);
        DiameterValueResult count = codec.DecodeUnsigned32(
            encoded.Avps[1].Data);
        DiameterAvpDefinition? retainedDefinition =
            dictionary.FindDictionaryAvpByCode(4242U, 1U);

        Assert.Multiple(() =>
        {
            Assert.That(typed.RawMessage.Avps[0].Data[0],
                Is.EqualTo((byte)'o'));
            Assert.That(typed.Avps[0].Known, Is.True);
            Assert.That(typed.Avps[0].Decoded, Is.True);
            Assert.That(typed.Avps[0].PreserveRaw, Is.False);
            Assert.That(typed.Avps[0].Value.Text,
                Is.EqualTo("changed"));
            Assert.That(typed.Avps[1].Value.Unsigned32,
                Is.EqualTo(99U));
            Assert.That(typed.Avps[2].Known, Is.False);
            Assert.That(typed.Avps[2].Decoded, Is.False);
            Assert.That(typed.Avps[2].PreserveRaw, Is.True);
            Assert.That(typed.Avps[2].Value.Data,
                Is.EqualTo(new byte[] { 9, 8, 7 }));
            Assert.That(encoded.HopByHopIdentifier, Is.EqualTo(11U));
            Assert.That(encoded.EndToEndIdentifier, Is.EqualTo(12U));
            Assert.That(encoded.Avps, Has.Count.EqualTo(3));
            Assert.That(text.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(text.Text, Is.EqualTo("changed"));
            Assert.That(count.Unsigned32, Is.EqualTo(99U));
            Assert.That(encoded.Avps[2].Code, Is.EqualTo(900U));
            Assert.That(encoded.Avps[2].VendorId, Is.EqualTo(9999U));
            Assert.That(encoded.Avps[2].Flags,
                Is.EqualTo(
                    DiameterAvpFlags.VendorSpecific |
                    DiameterAvpFlags.Protected));
            Assert.That(encoded.Avps[2].Data,
                Is.EqualTo(new byte[] { 9, 8, 7 }));
            Assert.That(retainedDefinition!.Name,
                Is.EqualTo("Vendor-Text"));
        });
    }

    [Test]
    public void GroupedValuesRetainUnknownChildrenAcrossTypedChildEdits()
    {
        DiameterDictionary dictionary = CreateDictionary();
        IDiameterAvpCodec codec = new DiameterAvpCodec();
        IDiameterApplicationAdapter adapter =
            new DiameterApplicationAdapter(
                dictionary,
                codec,
                new DiameterLimitConfiguration());
        List<DiameterAvp> children = new List<DiameterAvp>();
        children.Add(CreateAvp(
            1U,
            4242U,
            DiameterAvpFlags.VendorSpecific |
                DiameterAvpFlags.Mandatory,
            EncodeText(codec, "inside")));
        children.Add(CreateAvp(
            901U,
            9999U,
            DiameterAvpFlags.VendorSpecific,
            new byte[] { 4, 3, 2, 1 }));
        List<DiameterAvp> groupedTypes = CreateGroupedTypes();
        DiameterValueResult measurement = codec.MeasureGrouped(
            children,
            groupedTypes,
            new DiameterLimitConfiguration());
        byte[] groupedData = new byte[measurement.RequiredCount];
        DiameterValueResult groupEncoding = codec.EncodeGrouped(
            groupedData,
            children,
            groupedTypes,
            new DiameterLimitConfiguration());
        DiameterAvp grouped = CreateAvp(
            3U,
            4242U,
            DiameterAvpFlags.VendorSpecific |
                DiameterAvpFlags.Mandatory,
            groupedData);

        DiameterTypedAvp typedGroup = adapter.DecodeDiameterTypedAvp(
            grouped,
            DateTimeOffset.UtcNow);
        DiameterTypedAvp typedChild = adapter.DecodeDiameterTypedAvp(
            typedGroup.Value.Avps[0],
            DateTimeOffset.UtcNow);
        typedChild.Value.Text = "updated-child";
        typedGroup.Value.Avps[0] =
            adapter.EncodeDiameterTypedAvp(typedChild);

        DiameterAvp encodedGroup =
            adapter.EncodeDiameterTypedAvp(typedGroup);
        DiameterValueResult decodedGroup = codec.DecodeGrouped(
            encodedGroup.Data,
            groupedTypes,
            new DiameterLimitConfiguration());
        DiameterValueResult decodedChild = codec.DecodeUtf8String(
            decodedGroup.Avps[0].Data);

        Assert.Multiple(() =>
        {
            Assert.That(groupEncoding.Status,
                Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(typedGroup.Known, Is.True);
            Assert.That(typedGroup.Decoded, Is.True);
            Assert.That(typedGroup.Value.Avps, Has.Count.EqualTo(2));
            Assert.That(decodedGroup.Status,
                Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(decodedChild.Text, Is.EqualTo("updated-child"));
            Assert.That(decodedGroup.Avps[1].Code, Is.EqualTo(901U));
            Assert.That(decodedGroup.Avps[1].VendorId, Is.EqualTo(9999U));
            Assert.That(decodedGroup.Avps[1].Flags,
                Is.EqualTo(DiameterAvpFlags.VendorSpecific));
            Assert.That(decodedGroup.Avps[1].Data,
                Is.EqualTo(new byte[] { 4, 3, 2, 1 }));
        });
    }

    [Test]
    public void UndecodableKnownValueFallsBackToOwnedRawRepresentation()
    {
        DiameterDictionary dictionary = CreateDictionary();
        IDiameterApplicationAdapter adapter =
            new DiameterApplicationAdapter(
                dictionary,
                new DiameterAvpCodec(),
                new DiameterLimitConfiguration());
        DiameterAvp malformed = CreateAvp(
            1U,
            4242U,
            DiameterAvpFlags.VendorSpecific |
                DiameterAvpFlags.Mandatory,
            new byte[] { 0xC0, 0xAF });

        DiameterTypedAvp typed = adapter.DecodeDiameterTypedAvp(
            malformed,
            DateTimeOffset.UtcNow);
        malformed.Data[0] = 0;
        DiameterAvp encoded = adapter.EncodeDiameterTypedAvp(typed);

        Assert.Multiple(() =>
        {
            Assert.That(typed.Known, Is.True);
            Assert.That(typed.Decoded, Is.False);
            Assert.That(typed.PreserveRaw, Is.True);
            Assert.That(typed.Value.Status,
                Is.EqualTo(DiameterCodecStatus.Invalid));
            Assert.That(encoded.Code, Is.EqualTo(1U));
            Assert.That(encoded.VendorId, Is.EqualTo(4242U));
            Assert.That(encoded.Data,
                Is.EqualTo(new byte[] { 0xC0, 0xAF }));
        });
    }

    [Test]
    public void PreserveRawKeepsKnownValueBytesUnchanged()
    {
        IDiameterApplicationAdapter adapter =
            new DiameterApplicationAdapter(
                new DiameterDictionary(),
                new DiameterAvpCodec(),
                new DiameterLimitConfiguration());
        DiameterAvp originHost = CreateAvp(
            DiameterAvpCodes.OriginHost,
            DiameterVendorIds.Ietf,
            DiameterAvpFlags.Mandatory,
            new byte[]
            {
                (byte)'P', (byte)'E', (byte)'E', (byte)'R',
                (byte)'.', (byte)'E', (byte)'x', (byte)'a',
                (byte)'m', (byte)'p', (byte)'l', (byte)'e'
            });

        DiameterTypedAvp typed = adapter.DecodeDiameterTypedAvp(
            originHost,
            DateTimeOffset.UtcNow);
        typed.PreserveRaw = true;
        DiameterAvp encoded = adapter.EncodeDiameterTypedAvp(typed);

        Assert.Multiple(() =>
        {
            Assert.That(typed.Known, Is.True);
            Assert.That(typed.Decoded, Is.True);
            Assert.That(typed.Value.Identity!.Value,
                Is.EqualTo("peer.example"));
            Assert.That(encoded.Data, Is.EqualTo(originHost.Data));
        });
    }

    [Test]
    public void PublicAdapterGuardsRejectEveryIncompleteInputShape()
    {
        DiameterDictionary dictionary = CreateDictionary();
        DiameterAvpCodec codec = new DiameterAvpCodec();
        DiameterLimitConfiguration limits = new DiameterLimitConfiguration();

        Assert.Multiple(() =>
        {
            Assert.Throws<DiameterApplicationException>(() =>
                new DiameterApplicationAdapter(null, codec, limits));
            Assert.Throws<DiameterApplicationException>(() =>
                new DiameterApplicationAdapter(dictionary, null, limits));
            Assert.Throws<DiameterApplicationException>(() =>
                new DiameterApplicationAdapter(dictionary, codec, null));
        });

        DiameterApplicationAdapter adapter =
            new DiameterApplicationAdapter(dictionary, codec, limits);
        DiameterMessage messageWithoutAvps = new DiameterMessage();
        messageWithoutAvps.Avps = null!;
        DiameterTypedMessage messageWithoutRaw = new DiameterTypedMessage();
        messageWithoutRaw.RawMessage = null!;
        DiameterTypedMessage messageWithoutTypedAvps =
            new DiameterTypedMessage();
        messageWithoutTypedAvps.Avps = null!;
        DiameterTypedMessage messageWithNullAvp = new DiameterTypedMessage();
        messageWithNullAvp.Avps.Add(null!);
        DiameterAvp avpWithoutData = new DiameterAvp();
        avpWithoutData.Data = null!;

        Assert.Multiple(() =>
        {
            Assert.Throws<DiameterApplicationException>(() =>
                adapter.DecodeDiameterTypedMessage(null, DateTimeOffset.UtcNow));
            Assert.Throws<DiameterApplicationException>(() =>
                adapter.DecodeDiameterTypedMessage(
                    messageWithoutAvps,
                    DateTimeOffset.UtcNow));
            Assert.Throws<DiameterApplicationException>(() =>
                adapter.EncodeDiameterTypedMessage(null));
            Assert.Throws<DiameterApplicationException>(() =>
                adapter.EncodeDiameterTypedMessage(messageWithoutRaw));
            Assert.Throws<DiameterApplicationException>(() =>
                adapter.EncodeDiameterTypedMessage(messageWithoutTypedAvps));
            Assert.Throws<DiameterApplicationException>(() =>
                adapter.EncodeDiameterTypedMessage(messageWithNullAvp));
            Assert.Throws<DiameterApplicationException>(() =>
                adapter.DecodeDiameterTypedAvp(null, DateTimeOffset.UtcNow));
            Assert.Throws<DiameterApplicationException>(() =>
                adapter.DecodeDiameterTypedAvp(
                    avpWithoutData,
                    DateTimeOffset.UtcNow));
            Assert.Throws<DiameterApplicationException>(() =>
                adapter.EncodeDiameterTypedAvp(null));
        });
    }

    [Test]
    public void TypedAvpStorageGuardsRejectEveryMissingOwnedValue()
    {
        DiameterApplicationAdapter adapter = new DiameterApplicationAdapter(
            CreateDictionary(),
            new DiameterAvpCodec(),
            new DiameterLimitConfiguration());
        DiameterTypedAvp missingRaw = CreateCompleteTypedAvp();
        missingRaw.RawAvp = null!;
        DiameterTypedAvp missingRawData = CreateCompleteTypedAvp();
        missingRawData.RawAvp.Data = null!;
        DiameterTypedAvp missingValue = CreateCompleteTypedAvp();
        missingValue.Value = null!;
        DiameterTypedAvp missingData = CreateCompleteTypedAvp();
        missingData.Value.Data = null!;
        DiameterTypedAvp missingAvps = CreateCompleteTypedAvp();
        missingAvps.Value.Avps = null!;
        DiameterTypedAvp missingText = CreateCompleteTypedAvp();
        missingText.Value.Text = null!;
        DiameterTypedAvp missingIssues = CreateCompleteTypedAvp();
        missingIssues.Value.Issues = null!;
        DiameterTypedAvp missingDefinition = CreateCompleteTypedAvp();
        missingDefinition.Definition = null;
        DiameterTypedAvp invalidText = CreateCompleteTypedAvp();
        invalidText.Value.Text = "\uD800";
        DiameterTypedAvp ietfValue = CreateCompleteTypedAvp();
        ietfValue.Definition!.VendorId = DiameterVendorIds.Ietf;
        ietfValue.Definition.TypeName = "OctetString";
        ietfValue.Definition.BaseType = DiameterAvpBaseType.OctetString;

        Assert.Multiple(() =>
        {
            Assert.Throws<DiameterApplicationException>(() =>
                adapter.EncodeDiameterTypedAvp(missingRaw));
            Assert.Throws<DiameterApplicationException>(() =>
                adapter.EncodeDiameterTypedAvp(missingRawData));
            Assert.Throws<DiameterApplicationException>(() =>
                adapter.EncodeDiameterTypedAvp(missingValue));
            Assert.Throws<DiameterApplicationException>(() =>
                adapter.EncodeDiameterTypedAvp(missingData));
            Assert.Throws<DiameterApplicationException>(() =>
                adapter.EncodeDiameterTypedAvp(missingAvps));
            Assert.Throws<DiameterApplicationException>(() =>
                adapter.EncodeDiameterTypedAvp(missingText));
            Assert.Throws<DiameterApplicationException>(() =>
                adapter.EncodeDiameterTypedAvp(missingIssues));
            Assert.Throws<DiameterApplicationException>(() =>
                adapter.EncodeDiameterTypedAvp(missingDefinition));
            Assert.Throws<DiameterApplicationException>(() =>
                adapter.EncodeDiameterTypedAvp(invalidText));
            Assert.That(adapter.EncodeDiameterTypedAvp(ietfValue).VendorId,
                Is.EqualTo(DiameterVendorIds.Ietf));
            Assert.Throws<DiameterApplicationException>(() =>
                adapter.ValidateApplicationAdapterMessage(null));
            Assert.Throws<DiameterApplicationException>(() =>
                adapter.ValidateApplicationAdapterAvp(null));
        });
    }

    [Test]
    public void AdapterValueDispatchCoversAllRemainingCodecChoices()
    {
        DiameterApplicationAdapter adapter = new DiameterApplicationAdapter(
            CreateDictionary(),
            new DiameterAvpCodec(),
            new DiameterLimitConfiguration());
        DiameterDictionarySnapshot snapshot =
            adapter.ReadApplicationAdapterSnapshot();
        DiameterAvpDefinition octets = CreateDefinition(
            50U,
            "Matrix",
            "OctetString",
            DiameterAvpBaseType.OctetString,
            -1);
        DiameterAvpDefinition float32 = CreateDefinition(
            51U,
            "Float32-Matrix",
            "Float32",
            DiameterAvpBaseType.Float32,
            4);
        DiameterAvpDefinition float64 = CreateDefinition(
            52U,
            "Float64-Matrix",
            "Float64",
            DiameterAvpBaseType.Float64,
            8);
        DiameterValueResult value = new DiameterValueResult();
        value.Status = DiameterCodecStatus.Success;
        value.Data = new byte[] { 1, 2, 3 };
        value.Text = "permit in ip from any to any";
        value.Address = IPAddress.Loopback;
        value.Identity = new DiameterIdentity();
        value.Identity.Value = "peer.example";
        value.Uri = new DiameterUri();
        value.Uri.Scheme = "aaa";
        value.Uri.Host.Value = "peer.example";
        value.Uri.Transport = "tcp";
        value.Uri.Protocol = "diameter";
        value.Float32 = 1.5F;
        value.Float64 = 2.5D;
        value.Integer32 = 2;
        DiameterValueResult invalidGrouped = new DiameterValueResult();
        DiameterAvp missingGroupedData = new DiameterAvp();
        missingGroupedData.Data = null!;
        invalidGrouped.Avps.Add(missingGroupedData);

        byte[] float32Data = new byte[4];
        byte[] float64Data = new byte[8];
        adapter.Codec.EncodeFloat32(float32Data, value.Float32);
        adapter.Codec.EncodeFloat64(float64Data, value.Float64);

        Assert.Multiple(() =>
        {
            Assert.That(adapter.DecodeApplicationAdapterValue(
                new byte[] { 0, 1, 127, 0, 0, 1 },
                octets,
                "Address",
                DateTimeOffset.UtcNow,
                snapshot).Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(adapter.DecodeApplicationAdapterValue(
                EncodeText(adapter.Codec, "aaa://peer.example:3868;transport=tcp;protocol=diameter"),
                octets,
                "DiameterURI",
                DateTimeOffset.UtcNow,
                snapshot).Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(adapter.DecodeApplicationAdapterValue(
                float32Data,
                octets,
                "Float32",
                DateTimeOffset.UtcNow,
                snapshot).Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(adapter.DecodeApplicationAdapterValue(
                float32Data,
                float32,
                "Other",
                DateTimeOffset.UtcNow,
                snapshot).Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(adapter.DecodeApplicationAdapterValue(
                float64Data,
                octets,
                "Float64",
                DateTimeOffset.UtcNow,
                snapshot).Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(adapter.DecodeApplicationAdapterValue(
                float64Data,
                float64,
                "Other",
                DateTimeOffset.UtcNow,
                snapshot).Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(adapter.EncodeApplicationAdapterValue(
                value, octets, "Float32", snapshot).Status,
                Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(adapter.EncodeApplicationAdapterValue(
                value, float32, "Other", snapshot).Status,
                Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(adapter.EncodeApplicationAdapterValue(
                value, octets, "Float64", snapshot).Status,
                Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(adapter.EncodeApplicationAdapterValue(
                value, float64, "Other", snapshot).Status,
                Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(adapter.EncodeApplicationAdapterValue(
                value, octets, "Enumerated", snapshot).Status,
                Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(adapter.EncodeApplicationAdapterValue(
                invalidGrouped, octets, "Grouped", snapshot).Status,
                Is.Not.EqualTo(DiameterCodecStatus.Success));
            Assert.That(adapter.EncodeApplicationAdapterValue(
                value, octets, "Unsupported", snapshot).Status,
                Is.EqualTo(DiameterCodecStatus.Invalid));
        });

        string[] variableTypes = new string[]
        {
            "Address", "UTF8String", "DiameterIdentity", "DiameterURI",
            "IPFilterRule", "QoSFilterRule", "Unsupported"
        };
        int index = 0;
        while (index < variableTypes.Length)
        {
            DiameterValueResult measured =
                adapter.MeasureApplicationAdapterVariableValue(
                    value,
                    variableTypes[index]);
            int count = measured.RequiredCount;
            if (count < 1)
            {
                count = 1;
            }

            DiameterValueResult encoded =
                adapter.EncodeApplicationAdapterVariableValue(
                    new byte[count],
                    value,
                    variableTypes[index]);
            Assert.That(measured.Status, Is.AnyOf(
                DiameterCodecStatus.Success,
                DiameterCodecStatus.DestinationTooSmall,
                DiameterCodecStatus.Invalid));
            Assert.That(encoded.Status, Is.AnyOf(
                DiameterCodecStatus.Success,
                DiameterCodecStatus.DestinationTooSmall,
                DiameterCodecStatus.Invalid));
            index = index + 1;
        }

        DiameterValueResult emptyText = new DiameterValueResult();
        emptyText.Status = DiameterCodecStatus.Success;
        Assert.That(adapter.EncodeApplicationAdapterValue(
            emptyText,
            octets,
            "UTF8String",
            snapshot).Status, Is.EqualTo(DiameterCodecStatus.Success));
    }

    [Test]
    public void EncodingFailureDescriptionHandlesEveryIssueShape()
    {
        DiameterApplicationAdapter adapter = new DiameterApplicationAdapter(
            CreateDictionary(),
            new DiameterAvpCodec(),
            new DiameterLimitConfiguration());
        DiameterValueResult nullIssues = new DiameterValueResult();
        nullIssues.Issues = null!;
        DiameterValueResult nullIssue = new DiameterValueResult();
        nullIssue.Issues.Add(null!);
        DiameterValueResult emptyMessage = new DiameterValueResult();
        emptyMessage.Issues.Add(new DiameterCodecIssue());
        DiameterValueResult described = new DiameterValueResult();
        DiameterCodecIssue issue = new DiameterCodecIssue();
        issue.Message = "specific failure";
        described.Issues.Add(issue);

        Assert.Multiple(() =>
        {
            Assert.That(adapter.ReadApplicationAdapterEncodingFailure(
                nullIssues), Does.Contain("could not"));
            Assert.That(adapter.ReadApplicationAdapterEncodingFailure(
                new DiameterValueResult()), Does.Contain("could not"));
            Assert.That(adapter.ReadApplicationAdapterEncodingFailure(
                nullIssue), Does.Contain("could not"));
            Assert.That(adapter.ReadApplicationAdapterEncodingFailure(
                emptyMessage), Does.Contain("could not"));
            Assert.That(adapter.ReadApplicationAdapterEncodingFailure(
                described), Is.EqualTo("specific failure"));
        });
    }

    private static DiameterTypedAvp CreateCompleteTypedAvp()
    {
        DiameterTypedAvp result = new DiameterTypedAvp();
        result.Known = true;
        result.Decoded = true;
        result.Definition = CreateDefinition(
            1U,
            "Vendor-Text",
            "UTF8String",
            DiameterAvpBaseType.OctetString,
            -1);
        result.Value.Status = DiameterCodecStatus.Success;
        result.Value.Text = "value";
        return result;
    }

    private static DiameterDictionary CreateDictionary()
    {
        DiameterDictionary dictionary = new DiameterDictionary();
        DiameterDictionaryRegistrationResult registration =
            dictionary.RegisterDictionaryModule(CreateModule());
        Assert.That(registration.Status,
            Is.EqualTo(DiameterDictionaryRegistrationStatus.Success));
        return dictionary;
    }

    private static DiameterDictionaryModule CreateModule()
    {
        DiameterDictionaryModule module = new DiameterDictionaryModule();
        module.Name = "typed-adapter-test";
        module.Version = "1";
        module.Dependencies.Add("rfc6733");
        DiameterVendorDefinition vendor = new DiameterVendorDefinition();
        vendor.Id = 4242U;
        vendor.Name = "Adapter-Test-Vendor";
        module.Vendors.Add(vendor);

        DiameterTypeDefinition textType = new DiameterTypeDefinition();
        textType.Name = "VendorText";
        textType.VendorId = 4242U;
        textType.BaseType = DiameterAvpBaseType.OctetString;
        textType.ParentTypeName = "UTF8String";
        textType.ParentTypeVendorId = DiameterVendorIds.Ietf;
        module.Types.Add(textType);

        DiameterTypeDefinition countType = new DiameterTypeDefinition();
        countType.Name = "VendorCounter";
        countType.VendorId = 4242U;
        countType.BaseType = DiameterAvpBaseType.Unsigned32;
        countType.ParentTypeName = "Unsigned32";
        countType.ParentTypeVendorId = DiameterVendorIds.Ietf;
        countType.FixedDataLength = 4;
        module.Types.Add(countType);

        module.Avps.Add(CreateDefinition(
            1U,
            "Vendor-Text",
            "VendorText",
            DiameterAvpBaseType.OctetString,
            -1));
        module.Avps.Add(CreateDefinition(
            2U,
            "Vendor-Counter",
            "VendorCounter",
            DiameterAvpBaseType.Unsigned32,
            4));
        module.Avps.Add(CreateDefinition(
            3U,
            "Vendor-Group",
            "Grouped",
            DiameterAvpBaseType.Grouped,
            -1));
        return module;
    }

    private static DiameterAvpDefinition CreateDefinition(
        uint code,
        string name,
        string typeName,
        DiameterAvpBaseType baseType,
        int fixedDataLength)
    {
        DiameterAvpDefinition definition =
            new DiameterAvpDefinition();
        definition.Code = code;
        definition.VendorId = 4242U;
        definition.Name = name;
        definition.TypeName = typeName;
        definition.BaseType = baseType;
        definition.RequiredFlags =
            DiameterAvpFlags.VendorSpecific;
        definition.FixedDataLength = fixedDataLength;
        return definition;
    }

    private static DiameterAvp CreateAvp(
        uint code,
        uint vendorId,
        DiameterAvpFlags flags,
        byte[] data)
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = code;
        avp.VendorId = vendorId;
        avp.Flags = flags;
        avp.Data = data;
        return avp;
    }

    private static byte[] EncodeText(
        IDiameterAvpCodec codec,
        string value)
    {
        DiameterValueResult measurement = codec.EncodeUtf8String(
            Span<byte>.Empty,
            value);
        byte[] data = new byte[measurement.RequiredCount];
        DiameterValueResult encoded = codec.EncodeUtf8String(data, value);
        Assert.That(encoded.Status,
            Is.EqualTo(DiameterCodecStatus.Success));
        return data;
    }

    private static byte[] EncodeUnsigned32(
        IDiameterAvpCodec codec,
        uint value)
    {
        byte[] data = new byte[4];
        DiameterValueResult encoded = codec.EncodeUnsigned32(data, value);
        Assert.That(encoded.Status,
            Is.EqualTo(DiameterCodecStatus.Success));
        return data;
    }

    private static List<DiameterAvp> CreateGroupedTypes()
    {
        DiameterAvp marker = new DiameterAvp();
        marker.Code = 3U;
        marker.VendorId = 4242U;
        return new List<DiameterAvp> { marker };
    }
}
