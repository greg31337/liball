using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace xml;

/// <summary>Resolves explicitly allowed file and HTTP resources.</summary>
public class XmlDefaultResolver : IXmlResourceResolver
{
    internal static readonly HttpClient XmlHttpClient = CreateXmlHttpClient();

    /// <summary>Creates a default XML resource resolver.</summary>
    public XmlDefaultResolver()
    {
    }

    /// <summary>Resolves an XML resource request.</summary>
    public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
    {
        Guard.NotNull(policy, "policy");
        ResolverGuard guard = new ResolverGuard(policy, null);
        if (systemIdentifier == null || systemIdentifier.Length == 0)
        {
            throw guard.CreateXmlResourceError("The default resolver requires a system identifier or URI", XmlErrorCodes.ResourceNotFound, baseUri);
        }

        using (ResolverGuardScope scope = guard.BeginXmlResource(systemIdentifier, baseUri))
        {
            if (scope.CurrentUri.IsFile)
            {
                return ResolveXmlFileResource(guard, scope, kind, publicIdentifier, systemIdentifier);
            }

            if (scope.CurrentUri.Scheme == Uri.UriSchemeHttp || scope.CurrentUri.Scheme == Uri.UriSchemeHttps)
            {
                return ResolveXmlHttpResource(guard, scope, kind, publicIdentifier, systemIdentifier);
            }

            throw guard.CreateXmlResourceError("The default resolver does not support URI scheme '" + scope.CurrentUri.Scheme + "'", XmlErrorCodes.ResourceDenied, scope.CurrentUri.AbsoluteUri);
        }
    }

    internal XmlResolvedResource ResolveXmlFileResource(ResolverGuard guard, ResolverGuardScope scope, XmlResourceKind kind, string? publicIdentifier, string systemIdentifier)
    {
        string path = scope.CurrentUri.LocalPath;
        if (!File.Exists(path))
        {
            throw guard.CreateXmlResourceError("The file resource was not found at '" + scope.CurrentUri.AbsoluteUri + "'", XmlErrorCodes.ResourceNotFound, scope.CurrentUri.AbsoluteUri);
        }

        byte[] bytes;
        using (FileStream stream = File.OpenRead(path))
        {
            bytes = ReadXmlResourceBytes(stream, guard, scope);
        }

        string? mediaType = InferXmlResourceMediaType(scope.CurrentUri.AbsolutePath);
        return CreateXmlResolvedByteResource(guard, scope, kind, publicIdentifier, systemIdentifier, bytes, mediaType, null);
    }

    internal XmlResolvedResource ResolveXmlHttpResource(ResolverGuard guard, ResolverGuardScope scope, XmlResourceKind kind, string? publicIdentifier, string systemIdentifier)
    {
        while (true)
        {
            using (HttpRequestMessage request = CreateXmlHttpRequest(guard, scope))
            using (HttpResponseMessage response = SendXmlHttpRequest(request, guard, scope))
            {
                if (IsXmlHttpRedirect(response.StatusCode))
                {
                    Uri? location = response.Headers.Location;
                    if (location == null)
                    {
                        throw guard.CreateXmlResourceError("An HTTP redirect response did not contain a Location header", XmlErrorCodes.ResourceNotFound, scope.CurrentUri.AbsoluteUri);
                    }

                    guard.FollowXmlResourceRedirect(scope, location.OriginalString);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    int status = (int)response.StatusCode;
                    throw guard.CreateXmlResourceError("HTTP resource request failed with status " + status, XmlErrorCodes.ResourceNotFound, scope.CurrentUri.AbsoluteUri);
                }

                byte[] bytes;
                using (Stream stream = response.Content.ReadAsStream())
                {
                    bytes = ReadXmlResourceBytes(stream, guard, scope);
                }

                string? mediaType = response.Content.Headers.ContentType == null ? null : response.Content.Headers.ContentType.MediaType;
                string? encodingName = response.Content.Headers.ContentType == null ? null : response.Content.Headers.ContentType.CharSet;
                if (encodingName != null)
                {
                    encodingName = encodingName.Trim('"');
                }

                return CreateXmlResolvedByteResource(guard, scope, kind, publicIdentifier, systemIdentifier, bytes, mediaType, encodingName);
            }
        }
    }

    internal HttpRequestMessage CreateXmlHttpRequest(ResolverGuard guard, ResolverGuardScope scope)
    {
        string? accept = guard.Policy.Accept;
        string? language = guard.Policy.AcceptLanguage;
        ValidateXmlHttpHeaderMetadata(accept, "Accept", guard, scope);
        ValidateXmlHttpHeaderMetadata(language, "Accept-Language", guard, scope);
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, scope.CurrentUri);
        if (accept != null)
        {
            request.Headers.TryAddWithoutValidation("Accept", accept);
        }

        if (language != null)
        {
            request.Headers.TryAddWithoutValidation("Accept-Language", language);
        }

        return request;
    }

    internal void ValidateXmlHttpHeaderMetadata(string? value, string name, ResolverGuard guard, ResolverGuardScope scope)
    {
        if (value == null)
        {
            return;
        }

        int index = 0;
        while (index < value.Length)
        {
            int character = value[index];
            if (character < 0x20 || character > 0x7E)
            {
                throw guard.CreateXmlResourceError("The " + name + " metadata contains a character that is not permitted in an HTTP header", XmlErrorCodes.ResourceDenied, scope.CurrentUri.AbsoluteUri);
            }

            index = index + 1;
        }
    }

    internal HttpResponseMessage SendXmlHttpRequest(HttpRequestMessage request, ResolverGuard guard, ResolverGuardScope scope)
    {
        Task<HttpResponseMessage> pending = XmlHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion)
        {
            throw guard.CreateXmlResourceError("The HTTP resource could not be retrieved", XmlErrorCodes.ResourceNotFound, scope.CurrentUri.AbsoluteUri);
        }

        return pending.Result;
    }

    internal XmlResolvedResource CreateXmlResolvedByteResource(ResolverGuard guard, ResolverGuardScope scope, XmlResourceKind kind, string? publicIdentifier, string systemIdentifier, byte[] bytes, string? mediaType, string? encodingName)
    {
        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.ByteMemory;
        input.Bytes = bytes;
        XmlResolvedResource resource = new XmlResolvedResource();
        resource.Kind = kind;
        resource.Access = XmlResourceAccess.Allowed;
        resource.Input = input;
        resource.RequestedUri = scope.InitialUri.AbsoluteUri;
        resource.AbsoluteUri = scope.CurrentUri.AbsoluteUri;
        resource.PublicIdentifier = publicIdentifier;
        resource.SystemIdentifier = systemIdentifier;
        resource.MediaType = mediaType;
        resource.EncodingName = encodingName;
        resource.OwnsResource = false;
        guard.ValidateXmlResolvedResource(scope, kind, resource);
        return resource;
    }

    internal byte[] ReadXmlResourceBytes(Stream stream, ResolverGuard guard, ResolverGuardScope scope)
    {
        Guard.NotNull(stream, "stream");
        byte[] buffer = new byte[4096];
        using (MemoryStream collected = new MemoryStream())
        {
            while (true)
            {
                int count = stream.Read(buffer, 0, buffer.Length);
                if (count == 0)
                {
                    return collected.ToArray();
                }

                guard.ObserveXmlResourceBytes(scope, count);
                collected.Write(buffer, 0, count);
            }
        }
    }

    internal string? InferXmlResourceMediaType(string path)
    {
        string extension = Path.GetExtension(path).ToLowerInvariant();
        if (extension == ".xml" || extension == ".xsd" || extension == ".xsl" || extension == ".xslt")
        {
            return "application/xml";
        }

        if (extension == ".dtd" || extension == ".ent" || extension == ".txt")
        {
            return "text/plain";
        }

        if (extension == ".json")
        {
            return "application/json";
        }

        return null;
    }

    internal bool IsXmlHttpRedirect(HttpStatusCode statusCode)
    {
        return statusCode == HttpStatusCode.MovedPermanently || statusCode == HttpStatusCode.Redirect || statusCode == HttpStatusCode.SeeOther || statusCode == HttpStatusCode.TemporaryRedirect || (int)statusCode == 308;
    }

    internal static HttpClient CreateXmlHttpClient()
    {
        HttpClientHandler handler = new HttpClientHandler();
        handler.AllowAutoRedirect = false;
        handler.AutomaticDecompression = DecompressionMethods.None;
        return new HttpClient(handler, true);
    }
}
