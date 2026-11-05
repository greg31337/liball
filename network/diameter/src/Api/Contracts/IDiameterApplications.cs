using System;
using System.Collections.Generic;

namespace diameter;

/// <summary>Registers, removes, and inspects Diameter application modules.</summary>
public interface IDiameterApplicationHost
{
    /// <summary>Atomically registers one application module.</summary>
    /// <param name="application">The application module to register.</param>
    /// <returns>An owned snapshot of the active registration.</returns>
    DiameterApplicationRegistration RegisterDiameterApplication(
        IDiameterApplication? application);

    /// <summary>Removes one application module by its stable registration name.</summary>
    /// <param name="registrationName">The stable application registration name.</param>
    /// <param name="drain">Allows queued and active work to drain before removal.</param>
    /// <returns><c>true</c> when a registered module was removed.</returns>
    bool UnregisterDiameterApplication(
        string? registrationName,
        bool drain);

    /// <summary>Finds the application registered for one dispatch key.</summary>
    /// <param name="applicationId">The Diameter application identifier.</param>
    /// <param name="commandCode">The Diameter command code.</param>
    /// <param name="request">Indicates the request rather than answer direction.</param>
    /// <returns>An owned registration snapshot, or <c>null</c> when absent.</returns>
    DiameterApplicationRegistration? FindDiameterApplicationRegistration(
        uint applicationId,
        uint commandCode,
        bool request);

    /// <summary>Reads all active application registrations.</summary>
    /// <returns>Owned registration snapshots in deterministic registration order.</returns>
    List<DiameterApplicationRegistration> ReadDiameterApplicationRegistrations();
}

/// <summary>Provides application registration, handlers, interceptors, and lifecycle.</summary>
public interface IDiameterApplication
{
    /// <summary>Reads the module registration supplied to the application host.</summary>
    /// <returns>An owned application registration.</returns>
    DiameterApplicationRegistration ReadDiameterApplicationRegistration();

    /// <summary>Finds the request handler for one registered command code.</summary>
    /// <param name="commandCode">The registered Diameter command code.</param>
    /// <returns>The request handler, or <c>null</c> when none is supplied.</returns>
    IDiameterRequestHandler? FindDiameterApplicationRequestHandler(
        uint commandCode);

    /// <summary>Reads the interceptors contributed by the module.</summary>
    /// <returns>The application interceptors in arbitrary source order.</returns>
    List<IMessageInterceptor> ReadDiameterApplicationInterceptors();

    /// <summary>Starts application work after registration becomes active.</summary>
    void StartDiameterApplication();

    /// <summary>Stops accepting new work and drains accepted application work.</summary>
    void DrainDiameterApplication();

    /// <summary>Stops application work and releases module resources.</summary>
    void StopDiameterApplication();
}

/// <summary>Processes one owned inbound Diameter application request.</summary>
public interface IDiameterRequestHandler
{
    /// <summary>Processes an inbound request and returns one terminal decision.</summary>
    /// <param name="context">The validated or raw request context.</param>
    /// <returns>The application processing decision.</returns>
    DiameterApplicationResult HandleDiameterApplicationRequest(
        DiameterRequestContext context);
}

/// <summary>Adapts raw application messages and AVPs through dictionary-selected codecs.</summary>
public interface IDiameterApplicationAdapter
{
    /// <summary>Decodes known AVPs while retaining owned raw values and ordering.</summary>
    /// <param name="message">The raw application message.</param>
    /// <param name="referenceTime">The reference used to select a Diameter Time era.</param>
    /// <returns>The owned raw and typed message representation.</returns>
    DiameterTypedMessage DecodeDiameterTypedMessage(
        DiameterMessage? message,
        DateTimeOffset referenceTime);

    /// <summary>Encodes typed known AVPs and forwards unknown raw AVPs unchanged.</summary>
    /// <param name="message">The typed application message.</param>
    /// <returns>The owned raw Diameter message.</returns>
    DiameterMessage EncodeDiameterTypedMessage(DiameterTypedMessage? message);

    /// <summary>Decodes one known AVP or retains one unknown raw AVP.</summary>
    /// <param name="avp">The raw AVP.</param>
    /// <param name="referenceTime">The reference used to select a Diameter Time era.</param>
    /// <returns>The owned raw and typed AVP representation.</returns>
    DiameterTypedAvp DecodeDiameterTypedAvp(
        DiameterAvp? avp,
        DateTimeOffset referenceTime);

    /// <summary>Encodes one typed known AVP or forwards one unknown raw AVP unchanged.</summary>
    /// <param name="avp">The typed AVP representation.</param>
    /// <returns>The owned raw AVP.</returns>
    DiameterAvp EncodeDiameterTypedAvp(DiameterTypedAvp? avp);
}

/// <summary>Processes ordered inbound and outbound Diameter messages.</summary>
public interface IMessageInterceptor
{
    /// <summary>Reads the stable interceptor name used for diagnostics and ordering.</summary>
    /// <returns>The stable interceptor name.</returns>
    string ReadDiameterMessageInterceptorName();

    /// <summary>Reads the lower-valued interceptor execution order.</summary>
    /// <returns>The configured interceptor order.</returns>
    int ReadDiameterMessageInterceptorOrder();

    /// <summary>Processes one inbound message pipeline stage.</summary>
    /// <param name="context">The inbound interceptor context.</param>
    /// <returns>The interceptor processing decision.</returns>
    DiameterApplicationResult InterceptInboundDiameterMessage(
        DiameterInterceptorContext context);

    /// <summary>Processes one outbound message pipeline stage.</summary>
    /// <param name="context">The outbound interceptor context.</param>
    /// <returns>The interceptor processing decision.</returns>
    DiameterApplicationResult InterceptOutboundDiameterMessage(
        DiameterInterceptorContext context);
}
