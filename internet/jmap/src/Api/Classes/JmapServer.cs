using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace jmap;

/// <summary>
/// Processes every JMAP HTTP endpoint through host-neutral request and response models.
/// </summary>
public class JmapServer : IJmapServer
{
    internal static readonly HttpClient SharedPushClient = new HttpClient();
    internal JmapServerOptions Options;
    internal JmapServerServices Services;
    internal PushDeliveryManager PushDelivery;
    internal EventSourceHub Events;
    internal ServerStatePublisher StatePublisher;
    internal RequestDispatcher Dispatcher;
    internal EndpointProcessor Endpoints;

    /// <summary>Creates a server over the supplied protocol options and backend services.</summary>
    /// <param name="options">The protocol and resource limits.</param>
    /// <param name="services">The caller-supplied backend services and policy hooks.</param>
    public JmapServer(JmapServerOptions options, JmapServerServices services)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (services == null)
        {
            throw new ArgumentNullException("services");
        }

        Options = options;
        Services = services;
        HttpClient pushClient = services.PushHttpClient == null ?
            SharedPushClient : services.PushHttpClient;
        PushDelivery = new PushDeliveryManager(pushClient, options, services.Clock);
        Events = new EventSourceHub(options, services.Clock, 64, 256);
        StatePublisher = new ServerStatePublisher(
            Events, services.StatePublisher, PushDelivery);
        services.StatePublisher = StatePublisher;
        Dispatcher = new RequestDispatcher(options, services, PushDelivery);
        Dispatcher.Push.PushDelivery = PushDelivery;
        Dispatcher.Submissions.Errors = Dispatcher.Errors;
        Endpoints = new EndpointProcessor(options, services, Dispatcher, Events);
    }

    /// <summary>Gets the active server options.</summary>
    /// <returns>The server options supplied to the constructor.</returns>
    public JmapServerOptions GetServerOptions()
    {
        return Options;
    }

    /// <summary>Gets the caller-supplied backend service bundle.</summary>
    /// <returns>The backend service bundle supplied to the constructor.</returns>
    public JmapServerServices GetServerServices()
    {
        return Services;
    }

    /// <summary>Processes a Session resource request.</summary>
    /// <param name="request">The host-neutral endpoint request.</param>
    /// <param name="cancellationToken">A token that can cancel processing.</param>
    /// <returns>A task containing the host-neutral endpoint response.</returns>
    public Task<JmapEndpointResponse> ProcessSessionEndpointAsync(
        JmapEndpointRequest request,
        CancellationToken cancellationToken)
    {
        return Endpoints.ProcessSessionEndpointAsync(request, cancellationToken);
    }

    /// <summary>Processes a JMAP API request.</summary>
    /// <param name="request">The host-neutral endpoint request.</param>
    /// <param name="cancellationToken">A token that can cancel processing.</param>
    /// <returns>A task containing the host-neutral endpoint response.</returns>
    public Task<JmapEndpointResponse> ProcessApiEndpointAsync(
        JmapEndpointRequest request,
        CancellationToken cancellationToken)
    {
        return Endpoints.ProcessApiEndpointAsync(request, cancellationToken);
    }

    /// <summary>Processes a streaming binary upload request.</summary>
    /// <param name="request">The host-neutral endpoint request.</param>
    /// <param name="cancellationToken">A token that can cancel processing.</param>
    /// <returns>A task containing the host-neutral endpoint response.</returns>
    public Task<JmapEndpointResponse> ProcessUploadEndpointAsync(
        JmapEndpointRequest request,
        CancellationToken cancellationToken)
    {
        return Endpoints.ProcessUploadEndpointAsync(request, cancellationToken);
    }

    /// <summary>Processes a streaming binary download request.</summary>
    /// <param name="request">The host-neutral endpoint request.</param>
    /// <param name="cancellationToken">A token that can cancel processing.</param>
    /// <returns>A task containing the host-neutral endpoint response.</returns>
    public Task<JmapEndpointResponse> ProcessDownloadEndpointAsync(
        JmapEndpointRequest request,
        CancellationToken cancellationToken)
    {
        return Endpoints.ProcessDownloadEndpointAsync(request, cancellationToken);
    }

    /// <summary>Processes a long-running EventSource request.</summary>
    /// <param name="request">The host-neutral endpoint request.</param>
    /// <param name="cancellationToken">A token that can cancel processing.</param>
    /// <returns>A task containing the host-neutral streaming response.</returns>
    public Task<JmapEndpointResponse> ProcessEventSourceEndpointAsync(
        JmapEndpointRequest request,
        CancellationToken cancellationToken)
    {
        return Endpoints.ProcessEventSourceEndpointAsync(request, cancellationToken);
    }
}
