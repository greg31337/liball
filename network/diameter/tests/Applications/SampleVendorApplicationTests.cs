using System;
using System.Collections.Generic;
using System.Threading;

namespace diameter.Tests;

public class SampleVendorApplicationTests
{
    [Test]
    public void RegistrationComposesVendorDictionaryAndAdvertisesCapability()
    {
        SampleVendorFixture fixture = CreateFixture();

        DiameterVendorDefinition? vendor = fixture.Dictionary
            .FindDictionaryVendorById(SampleVendorDictionary.VendorId);
        DiameterApplicationDefinition? application = fixture.Dictionary
            .FindDictionaryApplicationById(
                SampleVendorDictionary.VendorId,
                SampleVendorDictionary.ApplicationId);
        DiameterAvpDefinition? name = fixture.Dictionary
            .FindDictionaryAvpByCode(
                SampleVendorDictionary.VendorId,
                SampleVendorDictionary.DeviceNameAvpCode);
        DiameterAvpDefinition? profile = fixture.Dictionary
            .FindDictionaryAvpByCode(
                SampleVendorDictionary.VendorId,
                SampleVendorDictionary.DeviceProfileAvpCode);
        DiameterAvpRuleDefinition? modeRule = fixture.Dictionary
            .FindDictionaryGroupedRuleByCode(
                SampleVendorDictionary.VendorId,
                SampleVendorDictionary.DeviceProfileAvpCode,
                SampleVendorDictionary.VendorId,
                SampleVendorDictionary.DeviceModeAvpCode);
        DiameterCommandDefinition? request = fixture.Dictionary
            .FindDictionaryCommandByCode(
                SampleVendorDictionary.ApplicationId,
                SampleVendorDictionary.CommandCode,
                true);
        DiameterPeerCapabilities capabilities =
            fixture.Host.Registry.ReadApplicationCapabilities();
        DiameterApplicationRegistration? registration = fixture.Host
            .FindDiameterApplicationRegistration(
                SampleVendorDictionary.ApplicationId,
                SampleVendorDictionary.CommandCode,
                true);

        Assert.Multiple(() =>
        {
            Assert.That(vendor!.Name, Is.EqualTo("Sample Devices"));
            Assert.That(application!.Name,
                Is.EqualTo("Sample-Device-Control"));
            Assert.That(name!.TypeName, Is.EqualTo("Sample-Device-Name"));
            Assert.That(profile!.BaseType,
                Is.EqualTo(DiameterAvpBaseType.Grouped));
            Assert.That(profile.AllowUnlistedAvps, Is.False);
            Assert.That(modeRule!.MinimumCount, Is.EqualTo(1));
            Assert.That(modeRule.MaximumCount, Is.EqualTo(1));
            Assert.That(request!.Rules, Has.Count.EqualTo(2));
            Assert.That(registration!.Roles,
                Is.EqualTo(
                    DiameterNodeRole.Client |
                    DiameterNodeRole.Server));
            Assert.That(registration.Commands[0].SessionAware, Is.True);
            Assert.That(capabilities.SupportedVendorIds,
                Is.EqualTo(new uint[]
                {
                    SampleVendorDictionary.VendorId
                }));
            Assert.That(capabilities.VendorSpecificApplications,
                Has.Count.EqualTo(1));
            Assert.That(capabilities.VendorSpecificApplications[0].Id,
                Is.EqualTo(SampleVendorDictionary.ApplicationId));
            Assert.That(capabilities.VendorSpecificApplications[0].VendorId,
                Is.EqualTo(SampleVendorDictionary.VendorId));
            Assert.That(capabilities.VendorSpecificApplications[0]
                .IsAccounting, Is.False);
            Assert.That(fixture.Application.StartCount, Is.EqualTo(1));
        });

        Assert.That(fixture.Host.UnregisterDiameterApplication(
            "sample-device-control",
            true), Is.True);
    }

    [Test]
    public void ClientRequestUsesCustomAvpsAndServerPersistsSession()
    {
        SampleVendorFixture fixture = CreateFixture();
        DiameterMessage clientRequest = fixture.Application
            .CreateSampleDeviceRequest(
                "client.example;sample;1",
                "thermostat-7",
                3U,
                true);
        CountdownEvent completed = new CountdownEvent(1);
        SampleVendorCompletion completion =
            new SampleVendorCompletion(completed);

        ApplicationHostDispatchResult dispatch = fixture.Host
            .DispatchDiameterApplicationRequest(
                clientRequest,
                CreateIdentity("client.example"),
                "sample-route",
                false,
                false,
                false,
                completion);

        Assert.That(completed.Wait(TimeSpan.FromSeconds(5)), Is.True);
        DiameterSessionSnapshot? session = fixture.SessionManager
            .FindDiameterSession("client.example;sample;1");
        DiameterApplicationResult result = completion.Result!;
        DiameterTypedMessage typedAnswer = fixture.Application.Adapter
            .DecodeDiameterTypedMessage(
                result.OutputMessage,
                fixture.Clock.UtcTime);
        DiameterTypedAvp answerProfile = FindTypedAvp(
            typedAnswer,
            SampleVendorDictionary.DeviceProfileAvpCode);
        DiameterTypedAvp answerMode = fixture.Application.Adapter
            .DecodeDiameterTypedAvp(
                answerProfile.Value.Avps[1],
                fixture.Clock.UtcTime);

        Assert.Multiple(() =>
        {
            Assert.That(dispatch.Accepted, Is.True);
            Assert.That(dispatch.Immediate, Is.False);
            Assert.That(fixture.Application.ClientRequestCount,
                Is.EqualTo(1));
            Assert.That(fixture.Application.ServerRequestCount,
                Is.EqualTo(1));
            Assert.That(fixture.Application.LastValidationStatus,
                Is.EqualTo(DiameterValidationStatus.Valid));
            Assert.That(fixture.Application.LastDeviceName,
                Is.EqualTo("thermostat-7"));
            Assert.That(fixture.Application.LastDeviceMode,
                Is.EqualTo(3U));
            Assert.That(result.Answer, Is.True);
            Assert.That(ReadResultCode(result.OutputMessage!),
                Is.EqualTo(DiameterResultCodes.Success));
            Assert.That(answerProfile.Known, Is.True);
            Assert.That(answerProfile.Decoded, Is.True);
            Assert.That(answerMode.Value.Unsigned32, Is.EqualTo(4U));
            Assert.That(session, Is.Not.Null);
            Assert.That(session!.State,
                Is.EqualTo(DiameterSessionState.Open));
            Assert.That(session.Configuration.Role,
                Is.EqualTo(DiameterSessionRole.Server));
            Assert.That(session.Configuration.ApplicationId,
                Is.EqualTo(SampleVendorDictionary.ApplicationId));
            Assert.That(session.RouteIdentifier,
                Is.EqualTo("sample-route"));
            Assert.That(session.PeerIdentity!.Value,
                Is.EqualTo("client.example"));
            Assert.That(session.InitialAuthorizationCompleted, Is.True);
            Assert.That(session.ApplicationState,
                Is.EqualTo(new byte[] { 0, 0, 0, 3 }));
            Assert.That(session.LastRequest, Is.Not.Null);
        });

        Assert.That(fixture.Host.UnregisterDiameterApplication(
            "sample-device-control",
            true), Is.True);
    }

    [Test]
    public void GroupedRuleRejectsIncompleteClientRequestBeforeServerHandling()
    {
        SampleVendorFixture fixture = CreateFixture();
        DiameterMessage incomplete = fixture.Application
            .CreateSampleDeviceRequest(
                "client.example;sample;invalid",
                "thermostat-8",
                0U,
                false);
        CountdownEvent callbackSignal = new CountdownEvent(1);

        ApplicationHostDispatchResult dispatch = fixture.Host
            .DispatchDiameterApplicationRequest(
                incomplete,
                CreateIdentity("client.example"),
                string.Empty,
                false,
                false,
                false,
                new SampleVendorCompletion(callbackSignal));

        Assert.Multiple(() =>
        {
            Assert.That(dispatch.Immediate, Is.True);
            Assert.That(dispatch.Accepted, Is.False);
            Assert.That(dispatch.Result!.Answer, Is.True);
            Assert.That(dispatch.Result.Code,
                Is.EqualTo("application-request-validation-failed"));
            Assert.That(ReadResultCode(dispatch.Result.OutputMessage!),
                Is.EqualTo(DiameterResultCodes.MissingAvp));
            Assert.That(fixture.Application.ServerRequestCount, Is.Zero);
            Assert.That(fixture.SessionManager.ReadDiameterSessionSnapshots(),
                Is.Empty);
            Assert.That(callbackSignal.CurrentCount, Is.EqualTo(1));
        });

        Assert.That(fixture.Host.UnregisterDiameterApplication(
            "sample-device-control",
            true), Is.True);
    }

    private static SampleVendorFixture CreateFixture()
    {
        SampleVendorFixture result = new SampleVendorFixture();
        result.Dictionary = new DiameterDictionary();
        result.Clock = new TestDiameterClock(
            new DateTimeOffset(
                2026,
                8,
                29,
                12,
                0,
                0,
                TimeSpan.Zero),
            1000L);
        result.SessionManager = new DiameterSessionManager(
            CreateIdentity("server.example"),
            new DiameterInMemorySessionStore(),
            null,
            result.Clock,
            new DiameterIdentifierSource(result.Clock));
        result.Application = new SampleVendorApplication(
            result.Dictionary,
            result.SessionManager,
            result.Clock);
        result.Host = new DiameterApplicationHost(
            result.Dictionary,
            new DiameterValidator(),
            result.Clock,
            new DiameterLimitConfiguration(),
            CreateIdentity("server.example"),
            CreateIdentity("example"));
        result.Host.RegisterDiameterApplication(result.Application);
        return result;
    }

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }

    private static DiameterTypedAvp FindTypedAvp(
        DiameterTypedMessage message,
        uint code)
    {
        int index = 0;
        while (index < message.Avps.Count)
        {
            if (message.Avps[index].RawAvp.Code == code &&
                message.Avps[index].RawAvp.VendorId ==
                    SampleVendorDictionary.VendorId)
            {
                return message.Avps[index];
            }

            index = index + 1;
        }

        Assert.Fail("The sample vendor AVP was not present.");
        return new DiameterTypedAvp();
    }

    private static uint ReadResultCode(DiameterMessage message)
    {
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp.Code == DiameterAvpCodes.ResultCode &&
                avp.VendorId == DiameterVendorIds.Ietf)
            {
                return NetworkByteOrder.ReadUInt32(avp.Data);
            }

            index = index + 1;
        }

        Assert.Fail("Result-Code was not present.");
        return 0U;
    }
}

internal class SampleVendorFixture
{
    internal DiameterDictionary Dictionary = null!;
    internal TestDiameterClock Clock = null!;
    internal DiameterSessionManager SessionManager = null!;
    internal SampleVendorApplication Application = null!;
    internal DiameterApplicationHost Host = null!;
}

internal class SampleVendorApplication :
    IDiameterApplication,
    IDiameterRequestHandler
{
    internal IDiameterSessionManager SessionManager;
    internal IDiameterClock Clock;
    internal IDiameterAvpCodec Codec = new DiameterAvpCodec();
    internal DiameterApplicationAdapter Adapter;
    internal DiameterApplicationRegistration Registration;
    internal int StartCount;
    internal int DrainCount;
    internal int StopCount;
    internal int ClientRequestCount;
    internal int ServerRequestCount;
    internal DiameterValidationStatus LastValidationStatus;
    internal string LastDeviceName = string.Empty;
    internal uint LastDeviceMode;

    internal SampleVendorApplication(
        IDiameterDictionary dictionary,
        IDiameterSessionManager sessionManager,
        IDiameterClock clock)
    {
        SessionManager = sessionManager;
        Clock = clock;
        Adapter = new DiameterApplicationAdapter(
            dictionary,
            Codec,
            new DiameterLimitConfiguration());
        Registration = CreateSampleRegistration();
    }

    public DiameterApplicationRegistration
        ReadDiameterApplicationRegistration()
    {
        return Registration;
    }

    public IDiameterRequestHandler?
        FindDiameterApplicationRequestHandler(uint commandCode)
    {
        if (commandCode == SampleVendorDictionary.CommandCode)
        {
            return this;
        }

        return null;
    }

    public List<IMessageInterceptor>
        ReadDiameterApplicationInterceptors()
    {
        return new List<IMessageInterceptor>();
    }

    public void StartDiameterApplication()
    {
        StartCount = StartCount + 1;
    }

    public void DrainDiameterApplication()
    {
        DrainCount = DrainCount + 1;
    }

    public void StopDiameterApplication()
    {
        StopCount = StopCount + 1;
    }

    public DiameterApplicationResult HandleDiameterApplicationRequest(
        DiameterRequestContext context)
    {
        ServerRequestCount = ServerRequestCount + 1;
        LastValidationStatus = context.ValidationResult == null ?
            DiameterValidationStatus.Invalid :
            context.ValidationResult.Status;
        DiameterTypedMessage typed = Adapter.DecodeDiameterTypedMessage(
            context.Request,
            Clock.ReadUtcTime());
        DiameterTypedAvp profile = FindSampleProfile(typed);
        ReadSampleProfile(profile);
        PersistSampleSession(context);

        DiameterMessage answer = new DiameterMessage();
        answer.Flags = context.Request.Flags &
            DiameterMessageFlags.Proxyable;
        answer.CommandCode = context.Request.CommandCode;
        answer.ApplicationId = context.Request.ApplicationId;
        answer.HopByHopIdentifier =
            context.Request.HopByHopIdentifier;
        answer.EndToEndIdentifier =
            context.Request.EndToEndIdentifier;
        answer.Avps.Add(CreateSampleResultCodeAvp());
        answer.Avps.Add(CreateSampleProfileAvp(
            LastDeviceName + "-accepted",
            LastDeviceMode + 1U,
            true));
        DiameterApplicationResult result =
            new DiameterApplicationResult();
        result.Continue = false;
        result.Answer = true;
        result.OutputMessage = answer;
        result.Code = "sample-device-accepted";
        return result;
    }

    internal DiameterMessage CreateSampleDeviceRequest(
        string sessionId,
        string deviceName,
        uint mode,
        bool includeMode)
    {
        ClientRequestCount = ClientRequestCount + 1;
        DiameterMessage request = new DiameterMessage();
        request.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable;
        request.CommandCode = SampleVendorDictionary.CommandCode;
        request.ApplicationId = SampleVendorDictionary.ApplicationId;
        request.HopByHopIdentifier =
            (uint)(100 + ClientRequestCount);
        request.EndToEndIdentifier =
            (uint)(200 + ClientRequestCount);
        request.Avps.Add(
            new DiameterBaseAvpFactory().CreateSessionIdAvp(sessionId));
        request.Avps.Add(CreateSampleProfileAvp(
            deviceName,
            mode,
            includeMode));
        return request;
    }

    internal DiameterApplicationRegistration CreateSampleRegistration()
    {
        DiameterApplicationRegistration result =
            new DiameterApplicationRegistration();
        result.Name = "sample-device-control";
        result.ApplicationId = SampleVendorDictionary.ApplicationId;
        result.VendorId = SampleVendorDictionary.VendorId;
        result.AdvertiseAuthentication = true;
        result.Roles = DiameterNodeRole.Client |
            DiameterNodeRole.Server;
        DiameterCommandRegistration command =
            new DiameterCommandRegistration();
        command.CommandCode = SampleVendorDictionary.CommandCode;
        command.Name = "Sample-Device-Control";
        command.RequireValidation = true;
        command.SessionAware = true;
        result.Commands.Add(command);
        result.DictionaryModules.Add(
            SampleVendorDictionary.CreateModule());
        return result;
    }

    internal DiameterTypedAvp FindSampleProfile(
        DiameterTypedMessage message)
    {
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterTypedAvp avp = message.Avps[index];
            if (avp.RawAvp.Code ==
                    SampleVendorDictionary.DeviceProfileAvpCode &&
                avp.RawAvp.VendorId ==
                    SampleVendorDictionary.VendorId)
            {
                return avp;
            }

            index = index + 1;
        }

        throw new InvalidOperationException(
            "The validated sample request did not contain a profile.");
    }

    internal void ReadSampleProfile(DiameterTypedAvp profile)
    {
        int index = 0;
        while (index < profile.Value.Avps.Count)
        {
            DiameterTypedAvp child = Adapter.DecodeDiameterTypedAvp(
                profile.Value.Avps[index],
                Clock.ReadUtcTime());
            if (child.RawAvp.Code ==
                SampleVendorDictionary.DeviceNameAvpCode)
            {
                LastDeviceName = child.Value.Text;
            }

            if (child.RawAvp.Code ==
                SampleVendorDictionary.DeviceModeAvpCode)
            {
                LastDeviceMode = child.Value.Unsigned32;
            }

            index = index + 1;
        }
    }

    internal void PersistSampleSession(DiameterRequestContext context)
    {
        byte[] state = new byte[4];
        NetworkByteOrder.WriteUInt32(state, LastDeviceMode);
        DiameterSessionConfiguration configuration =
            new DiameterSessionConfiguration();
        configuration.SessionId = context.SessionId;
        configuration.Role = DiameterSessionRole.Server;
        configuration.ApplicationId =
            SampleVendorDictionary.ApplicationId;
        configuration.PeerIdentity = context.PeerIdentity;
        configuration.ApplicationState = state;
        DiameterSessionSnapshot created =
            SessionManager.CreateDiameterSession(configuration);
        DiameterSessionUpdate update = new DiameterSessionUpdate();
        update.SessionId = created.SessionId;
        update.ExpectedVersion = created.Version;
        update.UpdateState = true;
        update.State = DiameterSessionState.Open;
        update.UpdateRouteIdentifier = true;
        update.RouteIdentifier = context.RouteIdentifier;
        update.UpdateLastRequest = true;
        update.LastRequest = context.Request;
        update.UpdateInitialAuthorizationCompleted = true;
        update.InitialAuthorizationCompleted = true;
        update.UpdateApplicationState = true;
        update.ApplicationState = state;
        update.Code = "sample-device-session-opened";
        SessionManager.UpdateDiameterSession(update);
    }

    internal DiameterAvp CreateSampleProfileAvp(
        string deviceName,
        uint mode,
        bool includeMode)
    {
        List<DiameterAvp> children = new List<DiameterAvp>();
        children.Add(CreateSampleDeviceNameAvp(deviceName));
        if (includeMode)
        {
            children.Add(CreateSampleDeviceModeAvp(mode));
        }

        List<DiameterAvp> groupedTypes =
            CreateSampleGroupedTypes();
        DiameterLimitConfiguration limits =
            new DiameterLimitConfiguration();
        DiameterValueResult measurement = Codec.MeasureGrouped(
            children,
            groupedTypes,
            limits);
        if (measurement.Status != DiameterCodecStatus.Success)
        {
            throw new InvalidOperationException(
                "The sample profile could not be measured.");
        }

        byte[] data = new byte[measurement.RequiredCount];
        DiameterValueResult encoding = Codec.EncodeGrouped(
            data,
            children,
            groupedTypes,
            limits);
        if (encoding.Status != DiameterCodecStatus.Success)
        {
            throw new InvalidOperationException(
                "The sample profile could not be encoded.");
        }

        DiameterAvp result = new DiameterAvp();
        result.Code = SampleVendorDictionary.DeviceProfileAvpCode;
        result.VendorId = SampleVendorDictionary.VendorId;
        result.Flags = DiameterAvpFlags.VendorSpecific |
            DiameterAvpFlags.Mandatory;
        result.Data = data;
        return result;
    }

    internal DiameterAvp CreateSampleDeviceNameAvp(string value)
    {
        DiameterValueResult measurement = Codec.EncodeUtf8String(
            Span<byte>.Empty,
            value);
        byte[] data = new byte[measurement.RequiredCount];
        DiameterValueResult encoding = Codec.EncodeUtf8String(
            data,
            value);
        if (encoding.Status != DiameterCodecStatus.Success)
        {
            throw new InvalidOperationException(
                "The sample device name could not be encoded.");
        }

        DiameterAvp result = new DiameterAvp();
        result.Code = SampleVendorDictionary.DeviceNameAvpCode;
        result.VendorId = SampleVendorDictionary.VendorId;
        result.Flags = DiameterAvpFlags.VendorSpecific |
            DiameterAvpFlags.Mandatory;
        result.Data = data;
        return result;
    }

    internal DiameterAvp CreateSampleDeviceModeAvp(uint value)
    {
        byte[] data = new byte[4];
        DiameterValueResult encoding = Codec.EncodeUnsigned32(
            data,
            value);
        if (encoding.Status != DiameterCodecStatus.Success)
        {
            throw new InvalidOperationException(
                "The sample device mode could not be encoded.");
        }

        DiameterAvp result = new DiameterAvp();
        result.Code = SampleVendorDictionary.DeviceModeAvpCode;
        result.VendorId = SampleVendorDictionary.VendorId;
        result.Flags = DiameterAvpFlags.VendorSpecific |
            DiameterAvpFlags.Mandatory;
        result.Data = data;
        return result;
    }

    internal DiameterAvp CreateSampleResultCodeAvp()
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = DiameterAvpCodes.ResultCode;
        result.Flags = DiameterAvpFlags.Mandatory;
        result.Data = new byte[4];
        NetworkByteOrder.WriteUInt32(
            result.Data,
            DiameterResultCodes.Success);
        return result;
    }

    internal List<DiameterAvp> CreateSampleGroupedTypes()
    {
        DiameterAvp profile = new DiameterAvp();
        profile.Code = SampleVendorDictionary.DeviceProfileAvpCode;
        profile.VendorId = SampleVendorDictionary.VendorId;
        return new List<DiameterAvp> { profile };
    }
}

internal static class SampleVendorDictionary
{
    internal const uint VendorId = 55555U;
    internal const uint ApplicationId = 16790001U;
    internal const uint CommandCode = 5000U;
    internal const uint DeviceNameAvpCode = 1U;
    internal const uint DeviceModeAvpCode = 2U;
    internal const uint DeviceProfileAvpCode = 3U;

    internal static DiameterDictionaryModule CreateModule()
    {
        DiameterDictionaryModule result =
            new DiameterDictionaryModule();
        result.Name = "sample-device-control-dictionary";
        result.Version = "1";
        result.Dependencies.Add("rfc6733");
        DiameterVendorDefinition vendor =
            new DiameterVendorDefinition();
        vendor.Id = VendorId;
        vendor.Name = "Sample Devices";
        result.Vendors.Add(vendor);
        DiameterApplicationDefinition application =
            new DiameterApplicationDefinition();
        application.Id = ApplicationId;
        application.VendorId = VendorId;
        application.Name = "Sample-Device-Control";
        result.Applications.Add(application);
        result.Types.Add(CreateType(
            "Sample-Device-Name",
            "UTF8String",
            DiameterAvpBaseType.OctetString,
            -1));
        result.Types.Add(CreateType(
            "Sample-Device-Mode",
            "Unsigned32",
            DiameterAvpBaseType.Unsigned32,
            4));
        result.Avps.Add(CreateAvp(
            DeviceNameAvpCode,
            "Sample-Device-Name",
            "Sample-Device-Name",
            DiameterAvpBaseType.OctetString,
            -1));
        result.Avps.Add(CreateAvp(
            DeviceModeAvpCode,
            "Sample-Device-Mode",
            "Sample-Device-Mode",
            DiameterAvpBaseType.Unsigned32,
            4));
        DiameterAvpDefinition profile = CreateAvp(
            DeviceProfileAvpCode,
            "Sample-Device-Profile",
            "Grouped",
            DiameterAvpBaseType.Grouped,
            -1);
        profile.AllowUnlistedAvps = false;
        profile.GroupedRules.Add(CreateRule(
            DeviceNameAvpCode,
            "Sample-Device-Name",
            1,
            1));
        profile.GroupedRules.Add(CreateRule(
            DeviceModeAvpCode,
            "Sample-Device-Mode",
            1,
            1));
        result.Avps.Add(profile);
        result.Commands.Add(CreateCommand(true));
        result.Commands.Add(CreateCommand(false));
        return result;
    }

    internal static DiameterTypeDefinition CreateType(
        string name,
        string parent,
        DiameterAvpBaseType baseType,
        int fixedLength)
    {
        DiameterTypeDefinition result =
            new DiameterTypeDefinition();
        result.Name = name;
        result.VendorId = VendorId;
        result.ParentTypeName = parent;
        result.ParentTypeVendorId = DiameterVendorIds.Ietf;
        result.BaseType = baseType;
        result.FixedDataLength = fixedLength;
        return result;
    }

    internal static DiameterAvpDefinition CreateAvp(
        uint code,
        string name,
        string typeName,
        DiameterAvpBaseType baseType,
        int fixedLength)
    {
        DiameterAvpDefinition result =
            new DiameterAvpDefinition();
        result.Code = code;
        result.VendorId = VendorId;
        result.Name = name;
        result.TypeName = typeName;
        result.BaseType = baseType;
        result.RequiredFlags = DiameterAvpFlags.VendorSpecific |
            DiameterAvpFlags.Mandatory;
        result.FixedDataLength = fixedLength;
        return result;
    }

    internal static DiameterAvpRuleDefinition CreateRule(
        uint code,
        string name,
        int minimum,
        int maximum)
    {
        DiameterAvpRuleDefinition result =
            new DiameterAvpRuleDefinition();
        result.AvpCode = code;
        result.AvpVendorId = VendorId;
        result.AvpName = name;
        result.MinimumCount = minimum;
        result.MaximumCount = maximum;
        return result;
    }

    internal static DiameterCommandDefinition CreateCommand(bool request)
    {
        DiameterCommandDefinition result =
            new DiameterCommandDefinition();
        result.Code = CommandCode;
        result.ApplicationId = ApplicationId;
        result.Name = "Sample-Device-Control";
        result.IsRequest = request;
        result.ProhibitedFlags = request ?
            DiameterMessageFlags.Error :
            DiameterMessageFlags.Request;
        if (request)
        {
            result.RequiredFlags = DiameterMessageFlags.Request;
            DiameterAvpRuleDefinition session =
                new DiameterAvpRuleDefinition();
            session.AvpCode = DiameterAvpCodes.SessionId;
            session.AvpName = "Session-Id";
            session.MinimumCount = 1;
            session.MaximumCount = 1;
            result.Rules.Add(session);
        }
        else
        {
            DiameterAvpRuleDefinition resultCode =
                new DiameterAvpRuleDefinition();
            resultCode.AvpCode = DiameterAvpCodes.ResultCode;
            resultCode.AvpName = "Result-Code";
            resultCode.MinimumCount = 1;
            resultCode.MaximumCount = 1;
            result.Rules.Add(resultCode);
        }

        result.Rules.Add(CreateRule(
            DeviceProfileAvpCode,
            "Sample-Device-Profile",
            1,
            1));
        result.AllowUnlistedAvps = false;
        return result;
    }
}

internal class SampleVendorCompletion :
    IApplicationDispatchCompletionHandler
{
    internal CountdownEvent Completed;
    internal DiameterApplicationResult? Result;

    internal SampleVendorCompletion(CountdownEvent completed)
    {
        Completed = completed;
    }

    public void CompleteDiameterApplicationDispatch(
        DiameterApplicationResult result)
    {
        Result = result;
        Completed.Signal();
    }
}
