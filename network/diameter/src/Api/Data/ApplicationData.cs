using System;
using System.Collections.Generic;

namespace diameter;

/// <summary>Describes one command direction handled by an application module.</summary>
public class DiameterCommandRegistration
{
    /// <summary>The Diameter command code.</summary>
    public uint CommandCode;

    /// <summary>The canonical command name.</summary>
    public string Name = string.Empty;

    /// <summary>Indicates that the request form is registered.</summary>
    public bool IsRequest = true;

    /// <summary>Requires dictionary validation before application dispatch.</summary>
    public bool RequireValidation = true;

    /// <summary>Indicates that the command participates in session behavior.</summary>
    public bool SessionAware;

    internal void ValidateCommandRegistration()
    {
        if (CommandCode == 0U ||
            CommandCode > DiameterProtocolConstants.MaximumCommandCode)
        {
            throw new ArgumentOutOfRangeException("CommandCode");
        }

        if (string.IsNullOrWhiteSpace(Name) ||
            ValueCodecs.FindInvalidUtf16Index(Name) >= 0)
        {
            throw new ArgumentException(
                "A valid command registration name is required.");
        }
    }
}

/// <summary>Describes one application module and its dispatch policy.</summary>
public class DiameterApplicationRegistration
{
    /// <summary>The stable application registration name.</summary>
    public string Name = string.Empty;

    /// <summary>The Diameter application identifier.</summary>
    public uint ApplicationId;

    /// <summary>The owning Vendor-ID, or zero for an IETF application.</summary>
    public uint VendorId;

    /// <summary>Advertises the identifier as an authentication application.</summary>
    public bool AdvertiseAuthentication;

    /// <summary>Advertises the identifier as an accounting application.</summary>
    public bool AdvertiseAccounting;

    /// <summary>The client and server roles implemented by the module.</summary>
    public DiameterNodeRole Roles = DiameterNodeRole.Server;

    /// <summary>The command directions handled by the module.</summary>
    public List<DiameterCommandRegistration> Commands =
        new List<DiameterCommandRegistration>();

    /// <summary>The dictionary modules registered with the application.</summary>
    public List<DiameterDictionaryModule> DictionaryModules =
        new List<DiameterDictionaryModule>();

    /// <summary>The positive maximum number of concurrent handler calls.</summary>
    public int MaximumConcurrency = 1;

    /// <summary>The positive bounded work-queue capacity.</summary>
    public int QueueCapacity = 1024;

    /// <summary>The positive graceful-drain timeout in milliseconds.</summary>
    public int DrainTimeoutMilliseconds = 30000;

    internal void ValidateApplicationRegistration()
    {
        if (string.IsNullOrWhiteSpace(Name) ||
            ValueCodecs.FindInvalidUtf16Index(Name) >= 0)
        {
            throw new ArgumentException(
                "A valid application registration name is required.");
        }

        int roleValue = (int)Roles;
        int applicationRoles =
            (int)DiameterNodeRole.Client | (int)DiameterNodeRole.Server;
        if (roleValue == 0 || (roleValue & ~applicationRoles) != 0)
        {
            throw new ArgumentOutOfRangeException("Roles");
        }

        if (Commands == null || DictionaryModules == null)
        {
            throw new ArgumentException(
                "Application command and dictionary collections are required.");
        }

        if (Commands.Count == 0)
        {
            throw new ArgumentException(
                "An application registration requires at least one command.");
        }

        if (MaximumConcurrency <= 0 || QueueCapacity <= 0 ||
            DrainTimeoutMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(
                "Application concurrency, queue, or drain limit");
        }

        ValidateApplicationCommands();
        ValidateApplicationDictionaries();
    }

    internal void ValidateApplicationCommands()
    {
        int index = 0;
        while (index < Commands.Count)
        {
            DiameterCommandRegistration command = Commands[index];
            if (command == null)
            {
                throw new ArgumentException(
                    "Application commands must not contain null values.");
            }

            command.ValidateCommandRegistration();
            int comparisonIndex = index + 1;
            while (comparisonIndex < Commands.Count)
            {
                DiameterCommandRegistration comparison =
                    Commands[comparisonIndex];
                if (comparison == null)
                {
                    throw new ArgumentException(
                        "Application commands must not contain null values.");
                }

                if (command.CommandCode == comparison.CommandCode &&
                    command.IsRequest == comparison.IsRequest)
                {
                    throw new ArgumentException(
                        "Application command directions must be unique.");
                }

                comparisonIndex = comparisonIndex + 1;
            }

            index = index + 1;
        }
    }

    internal void ValidateApplicationDictionaries()
    {
        int index = 0;
        while (index < DictionaryModules.Count)
        {
            DiameterDictionaryModule module = DictionaryModules[index];
            if (module == null || string.IsNullOrWhiteSpace(module.Name) ||
                ValueCodecs.FindInvalidUtf16Index(module.Name) >= 0)
            {
                throw new ArgumentException(
                    "Application dictionary modules require valid names.");
            }

            int comparisonIndex = index + 1;
            while (comparisonIndex < DictionaryModules.Count)
            {
                DiameterDictionaryModule comparison =
                    DictionaryModules[comparisonIndex];
                if (comparison == null ||
                    string.Equals(
                        module.Name,
                        comparison.Name,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException(
                        "Application dictionary module names must be unique.");
                }

                comparisonIndex = comparisonIndex + 1;
            }

            index = index + 1;
        }
    }
}

/// <summary>Contains immutable-by-convention metadata for one inbound request.</summary>
public class DiameterRequestContext
{
    /// <summary>The application registration selected for dispatch.</summary>
    public DiameterApplicationRegistration Registration =
        new DiameterApplicationRegistration();

    /// <summary>The command registration selected for dispatch.</summary>
    public DiameterCommandRegistration Command =
        new DiameterCommandRegistration();

    /// <summary>The owned inbound request message.</summary>
    public DiameterMessage Request = new DiameterMessage();

    /// <summary>The peer that delivered the request.</summary>
    public DiameterIdentity PeerIdentity = new DiameterIdentity();

    /// <summary>The selected route identifier, or an empty string.</summary>
    public string RouteIdentifier = string.Empty;

    /// <summary>The decoded Session-Id, or an empty string when absent.</summary>
    public string SessionId = string.Empty;

    /// <summary>The dictionary validation result, or <c>null</c> for raw dispatch.</summary>
    public DiameterValidationResult? ValidationResult;

    /// <summary>The UTC time at which the request was received.</summary>
    public DateTimeOffset ReceivedAt;

    /// <summary>Indicates that the duplicate key was observed previously.</summary>
    public bool Duplicate;

    /// <summary>Indicates that the request carries the retransmitted flag.</summary>
    public bool Retransmission;

    /// <summary>Indicates that request processing was cancelled.</summary>
    public bool Cancelled;

    /// <summary>Indicates that the node is shutting down.</summary>
    public bool ShuttingDown;

    internal void ValidateRequestContext()
    {
        if (Registration == null || Command == null || Request == null ||
            Request.Avps == null || PeerIdentity == null)
        {
            throw new ArgumentException(
                "Request context registration, command, message, and peer are required.");
        }

        Registration.ValidateApplicationRegistration();
        Command.ValidateCommandRegistration();

        if (Request.Version != DiameterProtocolConstants.Version ||
            Request.CommandCode != Command.CommandCode ||
            Request.ApplicationId != Registration.ApplicationId ||
            !Command.IsRequest ||
            (Request.Flags & DiameterMessageFlags.Request) ==
                DiameterMessageFlags.None ||
            (Request.Flags & DiameterMessageFlags.Error) !=
                DiameterMessageFlags.None)
        {
            throw new ArgumentException(
                "The request header must match the selected registrations.");
        }

        if (ValueCodecs.NormalizeDiameterIdentity(PeerIdentity).Status !=
                DiameterCodecStatus.Success ||
            RouteIdentifier == null || SessionId == null ||
            ValueCodecs.FindInvalidUtf16Index(RouteIdentifier) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(SessionId) >= 0)
        {
            throw new ArgumentException(
                "Request peer and correlation text must be valid.");
        }

        if (ReceivedAt == default(DateTimeOffset))
        {
            throw new ArgumentException(
                "A request receive time is required.");
        }

        if (Command.RequireValidation &&
            (ValidationResult == null ||
            ValidationResult.Status != DiameterValidationStatus.Valid))
        {
            throw new ArgumentException(
                "Validated dispatch requires a successful validation result.");
        }
    }
}

/// <summary>Contains one dictionary-adapted AVP and its owned raw representation.</summary>
public class DiameterTypedAvp
{
    /// <summary>The owned raw AVP retained for lossless forwarding.</summary>
    public DiameterAvp RawAvp = new DiameterAvp();

    /// <summary>The matching dictionary definition, or <c>null</c> when unknown.</summary>
    public DiameterAvpDefinition? Definition;

    /// <summary>The typed codec result, or owned raw data for an unknown AVP.</summary>
    public DiameterValueResult Value = new DiameterValueResult();

    /// <summary>Indicates that the dictionary contains the AVP definition.</summary>
    public bool Known;

    /// <summary>Indicates that the dictionary-selected value codec succeeded.</summary>
    public bool Decoded;

    /// <summary>Uses <c>RawAvp</c> instead of encoding the typed value.</summary>
    public bool PreserveRaw;
}

/// <summary>Contains a raw Diameter message and its ordered dictionary-adapted AVPs.</summary>
public class DiameterTypedMessage
{
    /// <summary>The owned raw message header and AVPs.</summary>
    public DiameterMessage RawMessage = new DiameterMessage();

    /// <summary>The ordered known and unknown adapted AVPs.</summary>
    public List<DiameterTypedAvp> Avps = new List<DiameterTypedAvp>();
}

/// <summary>Contains an application or interceptor processing decision.</summary>
public class DiameterApplicationResult
{
    /// <summary>The request context associated with the result, or <c>null</c>.</summary>
    public DiameterRequestContext? RequestContext;

    /// <summary>The interceptor context that produced the result, or <c>null</c>.</summary>
    public DiameterInterceptorContext? InterceptorContext;

    /// <summary>The replacement or answer message, or <c>null</c>.</summary>
    public DiameterMessage? OutputMessage;

    /// <summary>Continues processing with the current message.</summary>
    public bool Continue = true;

    /// <summary>Continues processing with <c>OutputMessage</c>.</summary>
    public bool Replace;

    /// <summary>Completes request processing with <c>OutputMessage</c> as the answer.</summary>
    public bool Answer;

    /// <summary>Silently drops the message.</summary>
    public bool Drop;

    /// <summary>Completes processing with redirect targets.</summary>
    public bool Redirect;

    /// <summary>Completes processing with a mapped application failure.</summary>
    public bool Fail;

    /// <summary>The redirect targets returned by a redirect decision.</summary>
    public List<DiameterUri> RedirectHosts = new List<DiameterUri>();

    /// <summary>The Diameter Result-Code used for mapped failure, or zero.</summary>
    public uint ResultCode;

    /// <summary>The stable machine-readable outcome code.</summary>
    public string Code = string.Empty;

    /// <summary>The human-readable outcome description.</summary>
    public string Description = string.Empty;

    internal void ValidateApplicationResult()
    {
        int actionCount = 0;
        if (Continue)
        {
            actionCount = actionCount + 1;
        }

        if (Replace)
        {
            actionCount = actionCount + 1;
        }

        if (Answer)
        {
            actionCount = actionCount + 1;
        }

        if (Drop)
        {
            actionCount = actionCount + 1;
        }

        if (Redirect)
        {
            actionCount = actionCount + 1;
        }

        if (Fail)
        {
            actionCount = actionCount + 1;
        }

        if (actionCount != 1)
        {
            throw new ArgumentException(
                "An application result requires exactly one action.");
        }

        if (RedirectHosts == null || Code == null || Description == null ||
            ValueCodecs.FindInvalidUtf16Index(Code) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(Description) >= 0)
        {
            throw new ArgumentException(
                "Application result collections and text must be valid.");
        }

        if (Replace &&
            (OutputMessage == null || OutputMessage.Avps == null))
        {
            throw new ArgumentException(
                "Replace and answer results require an output message.");
        }

        if (Answer)
        {
            if (OutputMessage == null || OutputMessage.Avps == null)
            {
                throw new ArgumentException(
                    "Replace and answer results require an output message.");
            }

            if ((OutputMessage.Flags & DiameterMessageFlags.Request) !=
                DiameterMessageFlags.None)
            {
                throw new ArgumentException(
                    "An application answer result requires an answer message.");
            }
        }

        if (!Replace && !Answer && OutputMessage != null)
        {
            throw new ArgumentException(
                "Only replace and answer results may carry an output message.");
        }

        if (Redirect != (RedirectHosts.Count > 0))
        {
            throw new ArgumentException(
                "Only redirect results require redirect hosts.");
        }

        ValidateApplicationRedirectHosts();

        if (ResultCode != 0U &&
            (ResultCode < 1000U || ResultCode > 5999U))
        {
            throw new ArgumentOutOfRangeException("ResultCode");
        }

        if (Fail && ResultCode == 0U)
        {
            throw new ArgumentException(
                "A failure result requires a Diameter Result-Code.");
        }
    }

    internal void ValidateApplicationRedirectHosts()
    {
        int index = 0;
        while (index < RedirectHosts.Count)
        {
            DiameterUri redirect = RedirectHosts[index];
            if (redirect == null)
            {
                throw new ArgumentException(
                    "Redirect results require valid Diameter redirect hosts.");
            }

            DiameterValueResult normalized =
                ValueCodecs.NormalizeDiameterUri(redirect);
            if (normalized.Status != DiameterCodecStatus.Success ||
                normalized.Uri == null ||
                normalized.Uri.Protocol != "diameter")
            {
                throw new ArgumentException(
                    "Redirect results require valid Diameter redirect hosts.");
            }

            index = index + 1;
        }
    }
}

/// <summary>Contains one inbound or outbound interceptor invocation.</summary>
public class DiameterInterceptorContext
{
    /// <summary>The message presented to the interceptor.</summary>
    public DiameterMessage Message = new DiameterMessage();

    /// <summary>The request context for inbound request processing, or <c>null</c>.</summary>
    public DiameterRequestContext? RequestContext;

    /// <summary>The preceding pipeline decision, or <c>null</c>.</summary>
    public DiameterApplicationResult? PreviousResult;

    /// <summary>The selected peer, or <c>null</c> before peer selection.</summary>
    public DiameterIdentity? PeerIdentity;

    /// <summary>The selected route identifier, or an empty string.</summary>
    public string RouteIdentifier = string.Empty;

    /// <summary>The associated Session-Id, or an empty string when absent.</summary>
    public string SessionId = string.Empty;

    /// <summary>The UTC time at which interceptor processing began.</summary>
    public DateTimeOffset InvokedAt;

    /// <summary>Indicates inbound rather than outbound processing.</summary>
    public bool Inbound;

    /// <summary>Indicates that the message is a duplicate request.</summary>
    public bool Duplicate;

    /// <summary>Indicates that the message carries the retransmitted flag.</summary>
    public bool Retransmission;

    /// <summary>Indicates that message processing was cancelled.</summary>
    public bool Cancelled;

    /// <summary>Indicates that the node is shutting down.</summary>
    public bool ShuttingDown;

    internal void ValidateInterceptorContext()
    {
        if (Message == null || Message.Avps == null ||
            Message.Version != DiameterProtocolConstants.Version ||
            Message.CommandCode == 0U ||
            Message.CommandCode > DiameterProtocolConstants.MaximumCommandCode)
        {
            throw new ArgumentException(
                "An interceptor context requires a valid Diameter message.");
        }

        if (PeerIdentity != null &&
            ValueCodecs.NormalizeDiameterIdentity(PeerIdentity).Status !=
                DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "The interceptor peer identity is invalid.");
        }

        if (RouteIdentifier == null || SessionId == null ||
            ValueCodecs.FindInvalidUtf16Index(RouteIdentifier) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(SessionId) >= 0 ||
            InvokedAt == default(DateTimeOffset))
        {
            throw new ArgumentException(
                "Interceptor correlation text and invocation time must be valid.");
        }

        if (RequestContext != null &&
            (RequestContext.Request.CommandCode != Message.CommandCode ||
            RequestContext.Request.ApplicationId != Message.ApplicationId ||
            (Message.Flags & DiameterMessageFlags.Request) ==
                DiameterMessageFlags.None))
        {
            throw new ArgumentException(
                "The interceptor message must match its request context.");
        }
    }
}
