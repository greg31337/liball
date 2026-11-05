using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace xml;

internal class XPathUnparsedTextResult
{
    internal bool Success;
    internal string? Text;
    internal string? ErrorCode;
    internal string? ErrorMessage;

    internal XPathUnparsedTextResult(bool success, string? text, string? errorCode, string? errorMessage)
    {
        Success = success;
        Text = text;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }
}

internal class XPathResourceRegistry
{
    internal IXPathCollectionResolver? Resolver;
    internal IXmlResourceResolver? ResourceResolver;
    internal XmlResolverPolicy Policy;
    internal int MaximumResources;
    internal long MaximumResourceBytes;
    internal int ResourceCount;
    internal long ResolverWorkUnits;
    internal bool EnforcePolicy;
    internal List<string> DocumentUris;
    internal List<XmlDocument> Documents;
    internal List<string> UnavailableDocumentUris;
    internal List<string> CollectionUris;
    internal List<XdmSequence> Collections;
    internal List<string> UriCollectionUris;
    internal List<string[]> UriCollections;
    internal List<string> UnparsedTextKeys;
    internal List<XPathUnparsedTextResult> UnparsedTexts;

    internal XPathResourceRegistry(IXPathCollectionResolver? resolver, XmlResolverPolicy policy, int maximumResources, bool enforcePolicy)
    {
        if (maximumResources <= 0)
        {
            throw new ArgumentOutOfRangeException("maximumResources");
        }

        Resolver = resolver;
        ResourceResolver = null;
        Policy = policy;
        MaximumResources = maximumResources;
        MaximumResourceBytes = 67108864;
        ResourceCount = 0;
        ResolverWorkUnits = 0;
        EnforcePolicy = enforcePolicy;
        DocumentUris = new List<string>();
        Documents = new List<XmlDocument>();
        UnavailableDocumentUris = new List<string>();
        CollectionUris = new List<string>();
        Collections = new List<XdmSequence>();
        UriCollectionUris = new List<string>();
        UriCollections = new List<string[]>();
        UnparsedTextKeys = new List<string>();
        UnparsedTexts = new List<XPathUnparsedTextResult>();
    }

    internal XmlDocument ResolveXPathDocumentResource(string uri)
    {
        string key = CanonicalXPathResourceKey(uri);
        XmlDocument? existing = FindXPathDocumentResource(key);
        if (existing != null)
        {
            return existing;
        }

        if (FindXPathResourceKey(UnavailableDocumentUris, key) >= 0)
        {
            throw CreateXPathResourceError("The XPath document resource '" + key + "' is unavailable.", XPathErrorCodes.FODC0002);
        }

        if (Resolver == null)
        {
            RegisterUnavailableXPathDocumentResource(key);
            throw CreateXPathResourceError("No XPath document resolver is available for '" + key + "'.", XPathErrorCodes.FODC0002);
        }

        IXPathCollectionResolver resolver = Resolver;
        ValidateXPathResolverAccess(key);
        CountXPathResolverWork(key.Length + 1);
        Task<XmlDocument?> pending = Task.Run(() => resolver.ResolveXPathDocument(key, Policy));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion || pending.Result == null)
        {
            RegisterUnavailableXPathDocumentResource(key);
            throw CreateXPathResourceError("The XPath document resource '" + key + "' is unavailable.", XPathErrorCodes.FODC0002);
        }

        RegisterXPathDocumentResource(key, pending.Result);
        CountXPathResolverWork(1);
        return pending.Result;
    }

    internal XdmSequence ResolveXPathCollectionResource(string? uri)
    {
        string key = CanonicalXPathOptionalResourceKey(uri);
        XdmSequence? existing = FindXPathCollectionResource(key);
        if (existing != null)
        {
            return existing;
        }

        if (Resolver == null)
        {
            throw CreateXPathResourceError("No XPath collection resolver is available.", XPathErrorCodes.FODC0002);
        }

        ValidateXPathResolverAccess(key);
        CountXPathResolverWork(key.Length + 1);
        IXPathCollectionResolver resolver = Resolver;
        Task<XdmSequence> pending = Task.Run(() => resolver.ResolveXPathCollection(uri, Policy));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion || pending.Result == null)
        {
            throw CreateXPathResourceError("The XPath collection resource is unavailable.", XPathErrorCodes.FODC0002);
        }

        XdmSequence resolved = pending.Result;
        CountXPathResolverWork(resolved.Count + 1);

        RegisterXPathCollectionResource(key, resolved);
        XdmSequence? registered = FindXPathCollectionResource(key);
        if (registered == null)
        {
            throw CreateXPathResourceError("The XPath collection resource could not be registered.", XPathErrorCodes.FODC0002);
        }

        return registered;
    }

    internal string[] ResolveXPathUriCollectionResource(string? uri)
    {
        string key = CanonicalXPathOptionalResourceKey(uri);
        string[]? existing = FindXPathUriCollectionResource(key);
        if (existing != null)
        {
            return CopyXPathResourceUris(existing);
        }

        if (Resolver == null)
        {
            throw CreateXPathResourceError("No XPath URI collection resolver is available.", XPathErrorCodes.FODC0002);
        }

        ValidateXPathResolverAccess(key);
        CountXPathResolverWork(key.Length + 1);
        IXPathCollectionResolver resolver = Resolver;
        Task<string[]> pending = Task.Run(() => resolver.ResolveXPathUriCollection(uri, Policy));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion || pending.Result == null)
        {
            throw CreateXPathResourceError("The XPath URI collection resource is unavailable.", XPathErrorCodes.FODC0002);
        }

        string[] resolved = pending.Result;
        CountXPathResolverWork(resolved.Length + 1);

        RegisterXPathUriCollectionResource(key, resolved);
        string[]? registered = FindXPathUriCollectionResource(key);
        if (registered == null)
        {
            throw CreateXPathResourceError("The XPath URI collection resource could not be registered.", XPathErrorCodes.FODC0002);
        }

        return CopyXPathResourceUris(registered);
    }

    internal XPathUnparsedTextResult ResolveXPathUnparsedTextResource(string uri, string? encoding)
    {
        string key = CanonicalXPathResourceKey(uri) + "\n" + (encoding == null ? "" : encoding.ToLowerInvariant());
        int existing = FindXPathResourceKey(UnparsedTextKeys, key);
        if (existing >= 0)
        {
            return UnparsedTexts[existing];
        }

        CountXPathResource();
        XPathUnparsedTextResult result = LoadXPathUnparsedTextResource(uri, encoding);
        UnparsedTextKeys.Add(key);
        UnparsedTexts.Add(result);
        return result;
    }

    internal XPathUnparsedTextResult LoadXPathUnparsedTextResource(string uri, string? encoding)
    {
        IXmlResourceResolver? resolver = ResourceResolver;
        if (resolver == null)
        {
            return new XPathUnparsedTextResult(false, null, XPathErrorCodes.FOUT1170, "No unparsed text resolver is available.");
        }

        if (encoding != null)
        {
            XIncludeEncodingChoice encodingChoice = new XIncludeTextDecoder(new XmlInput(), null, encoding, null, XmlVersion.Xml10, MaximumResourceBytes, false).ResolveXIncludeEncodingName(encoding);
            if (encodingChoice.Encoding == null)
            {
                return new XPathUnparsedTextResult(false, null, XPathErrorCodes.FOUT1190, "The requested unparsed text encoding is not supported: " + encoding + ".");
            }
        }

        ValidateXPathResolverAccess(uri);
        CountXPathResolverWork(uri.Length + 1);
        Task<XmlResolvedResource> pending = Task.Run(() => ResolveXPathUnparsedTextExternalResource(resolver, uri));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion)
        {
            return new XPathUnparsedTextResult(false, null, XPathErrorCodes.FOUT1170, "The unparsed text resource could not be retrieved.");
        }

        XmlResolvedResource resource = pending.Result;
        if (resource == null || resource.Access != XmlResourceAccess.Allowed || resource.Input == null)
        {
            return new XPathUnparsedTextResult(false, null, XPathErrorCodes.FOUT1170, "The unparsed text resource is unavailable.");
        }

        XmlInput input = CopyXPathUnparsedTextInput(resource);
        string? mediaType = input.MediaType;
        XIncludeTextDecoder decoder = new XIncludeTextDecoder(input, mediaType, encoding, "utf-8", XmlVersion.Xml10, MaximumResourceBytes, false);
        bool encodingInformation = HasXPathUnparsedTextEncodingInformation(input, mediaType, decoder);
        XIncludeTextDecoding decoded = decoder.DecodeXIncludeText();
        if (decoded.FatalError != null)
        {
            string code = encoding == null && !encodingInformation ? XPathErrorCodes.FOUT1200 : XPathErrorCodes.FOUT1190;
            return new XPathUnparsedTextResult(false, null, code, decoded.FatalError);
        }

        if (decoded.ResourceError != null || decoded.Text == null)
        {
            string message = decoded.ResourceError == null ? "The unparsed text resource could not be decoded." : decoded.ResourceError;
            string code = message.IndexOf("encoding", StringComparison.OrdinalIgnoreCase) >= 0 ? XPathErrorCodes.FOUT1190 : XPathErrorCodes.FOUT1170;
            return new XPathUnparsedTextResult(false, null, code, message);
        }

        return new XPathUnparsedTextResult(true, decoded.Text, null, null);
    }

    internal XmlResolvedResource ResolveXPathUnparsedTextExternalResource(IXmlResourceResolver resolver, string uri)
    {
        ResolverGuard guard = new ResolverGuard(CreateXPathGuardPolicy(), null);
        using (ResolverGuardScope scope = guard.BeginXmlResource(uri, null))
        {
            XmlResolvedResource resource = resolver.ResolveXmlResource(XmlResourceKind.UnparsedText, null, uri, null, Policy);
            guard.ValidateXmlResolvedResource(scope, XmlResourceKind.UnparsedText, resource);
            return resource;
        }
    }

    internal XmlResolverPolicy CreateXPathGuardPolicy()
    {
        XmlResolverPolicy result = new XmlResolverPolicy();
        result.Access = Policy.Access;
        result.AllowedSchemes = Policy.AllowedSchemes;
        result.AllowedRoots = Policy.AllowedRoots;
        result.AllowRedirects = Policy.AllowRedirects;
        result.MaximumRedirects = Policy.MaximumRedirects;
        result.MaximumDepth = Policy.MaximumDepth > 0 ? Policy.MaximumDepth : 16;
        result.MaximumResourceBytes = Policy.MaximumResourceBytes > 0 ? Policy.MaximumResourceBytes : MaximumResourceBytes;
        result.MaximumWorkUnits = Policy.MaximumWorkUnits > 0 ? Policy.MaximumWorkUnits : 134217728;
        return result;
    }

    internal void ValidateXPathResolverAccess(string uri)
    {
        if (EnforcePolicy && Policy.Access != XmlResourceAccess.Allowed)
        {
            throw CreateXPathResourceError("External resolver access is denied by policy.", XmlErrorCodes.ResourceDenied);
        }

        if (uri.Length == 0)
        {
            return;
        }

        string accessUri = uri;
        int fragment = accessUri.IndexOf('#');
        if (fragment >= 0)
        {
            accessUri = accessUri.Substring(0, fragment);
        }

        if (EnforcePolicy)
        {
            new ResolverGuard(CreateXPathGuardPolicy(), null).ValidateXmlResourceUriAccess(UriTools.CreateAbsolute(accessUri));
        }
    }

    internal void CountXPathResolverWork(long work)
    {
        long maximum = Policy.MaximumWorkUnits;
        if (maximum <= 0)
        {
            return;
        }

        if (work > maximum - ResolverWorkUnits)
        {
            throw CreateXPathResourceError("External resolver work exceeded the configured limit.", XmlErrorCodes.LimitExceeded);
        }

        ResolverWorkUnits = ResolverWorkUnits + work;
    }

    internal bool HasXPathUnparsedTextEncodingInformation(XmlInput input, string? mediaType, XIncludeTextDecoder decoder)
    {
        if (input.Encoding != null || input.EncodingName != null && input.EncodingName.Length > 0)
        {
            return true;
        }

        return decoder.FindXIncludeMediaTypeEncoding(mediaType) != null || decoder.IsXIncludeXmlMediaType(mediaType);
    }

    internal XmlInput CopyXPathUnparsedTextInput(XmlResolvedResource resource)
    {
        XmlInput source = resource.Input == null ? new XmlInput() : resource.Input;
        XmlInput result = new XmlInput();
        result.Kind = source.Kind;
        result.Text = source.Text;
        result.Characters = source.Characters;
        result.Bytes = source.Bytes;
        result.ByteStream = source.ByteStream;
        result.TextReader = source.TextReader;
        result.BaseUri = resource.AbsoluteUri == null ? source.BaseUri : resource.AbsoluteUri;
        result.MediaType = resource.MediaType == null ? source.MediaType : resource.MediaType;
        result.EncodingName = resource.EncodingName == null ? source.EncodingName : resource.EncodingName;
        result.Encoding = source.Encoding;
        result.OwnsResource = resource.OwnsResource || source.OwnsResource;
        return result;
    }

    internal void RegisterXPathDocumentResource(string uri, XmlDocument document)
    {
        Guard.NotNull(document, "document");
        string key = CanonicalXPathResourceKey(uri);
        int index = FindXPathResourceKey(DocumentUris, key);
        if (index >= 0)
        {
            Documents[index] = document;
            return;
        }

        CountXPathResource();
        DocumentUris.Add(key);
        Documents.Add(document);
    }

    internal void RegisterUnavailableXPathDocumentResource(string uri)
    {
        string key = CanonicalXPathResourceKey(uri);
        if (FindXPathResourceKey(UnavailableDocumentUris, key) >= 0)
        {
            return;
        }

        CountXPathResource();
        UnavailableDocumentUris.Add(key);
    }

    internal void RegisterXPathCollectionResource(string key, XdmSequence collection)
    {
        Guard.NotNull(collection, "collection");
        string normalized = CanonicalXPathOptionalResourceKey(key.Length == 0 ? null : key);
        XdmSequence snapshot = new XdmSequenceOperations().FreezeXdmSequenceStorage(collection);
        int index = FindXPathResourceKey(CollectionUris, normalized);
        if (index >= 0)
        {
            Collections[index] = snapshot;
            return;
        }

        CountXPathResource();
        CollectionUris.Add(normalized);
        Collections.Add(snapshot);
    }

    internal void RegisterXPathUriCollectionResource(string key, string[] uris)
    {
        Guard.NotNull(uris, "uris");
        string normalized = CanonicalXPathOptionalResourceKey(key.Length == 0 ? null : key);
        string[] snapshot = CopyXPathResourceUris(uris);
        int index = FindXPathResourceKey(UriCollectionUris, normalized);
        if (index >= 0)
        {
            UriCollections[index] = snapshot;
            return;
        }

        CountXPathResource();
        UriCollectionUris.Add(normalized);
        UriCollections.Add(snapshot);
    }

    internal XmlDocument? FindXPathDocumentResource(string key)
    {
        int index = FindXPathResourceKey(DocumentUris, key);
        return index < 0 ? null : Documents[index];
    }

    internal XdmSequence? FindXPathCollectionResource(string key)
    {
        int index = FindXPathResourceKey(CollectionUris, key);
        return index < 0 ? null : Collections[index];
    }

    internal string[]? FindXPathUriCollectionResource(string key)
    {
        int index = FindXPathResourceKey(UriCollectionUris, key);
        return index < 0 ? null : UriCollections[index];
    }

    internal int FindXPathResourceKey(List<string> keys, string key)
    {
        int index = 0;
        while (index < keys.Count)
        {
            if (keys[index] == key)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal void CountXPathResource()
    {
        if (ResourceCount >= MaximumResources)
        {
            string message = "The configured XPath external resource limit of " + MaximumResources + " was exceeded.";
            XmlDiagnostic diagnostic = new XmlDiagnostic();
            diagnostic.Code = XmlErrorCodes.LimitExceeded;
            diagnostic.Category = XmlErrorCategory.Limit;
            diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
            diagnostic.Message = message;
            throw new XmlLimitException(message, diagnostic, null);
        }

        ResourceCount = ResourceCount + 1;
    }

    internal string CanonicalXPathResourceKey(string uri)
    {
        return UriTools.CanonicalAbsoluteUri(UriTools.CreateAbsolute(uri));
    }

    internal string CanonicalXPathOptionalResourceKey(string? uri)
    {
        return uri == null || uri.Length == 0 ? "" : CanonicalXPathResourceKey(uri);
    }

    internal string[] CopyXPathResourceUris(string[] uris)
    {
        string[] result = new string[uris.Length];
        int index = 0;
        while (index < uris.Length)
        {
            string? uri = uris[index];
            if (uri == null)
            {
                throw new ArgumentException("An XPath URI collection cannot contain null entries.", "uris");
            }

            result[index] = uri;
            index = index + 1;
        }

        return result;
    }

    internal XPathDynamicException CreateXPathResourceError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XPathDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XPathDynamicException(message, diagnostic, null);
    }
}
