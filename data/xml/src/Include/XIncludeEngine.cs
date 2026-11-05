using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace xml;

internal class XIncludeResolution
{
    internal XmlResolvedResource? Resource;
    internal string? ErrorMessage;

    internal XIncludeResolution(XmlResolvedResource? resource, string? errorMessage)
    {
        Resource = resource;
        ErrorMessage = errorMessage;
    }
}

internal class XIncludeEngine
{
    internal XmlIncludeOptions Options;
    internal IXmlResourceResolver Resolver;
    internal XmlResolverPolicy Policy;
    internal TreeMutation Mutation;
    internal TreeNavigation Navigation;
    internal XPointerEvaluator PointerEvaluator;
    internal List<string> ActiveIncludes;
    internal List<XmlDocument> KnownDocuments;
    internal List<XmlDocument> SourceDocuments;
    internal List<XmlDocument> SourceSnapshots;
    internal int MaximumIncludeDepth;
    internal long MaximumIncludeBytes;
    internal int CurrentIncludeDepth;

    internal XIncludeEngine(XmlIncludeOptions options, IXmlResourceResolver resolver)
    {
        Guard.NotNull(options, "options");
        Guard.NotNull(resolver, "resolver");
        Guard.NonNegativeInt(options.MaximumDepth, "options.MaximumDepth");
        Guard.NonNegative(options.MaximumResourceBytes, "options.MaximumResourceBytes");
        Options = options;
        XmlResolverPolicy configuredPolicy = options.ResolverPolicy == null ? CreateDefaultXIncludeResolverPolicy(options) : options.ResolverPolicy;
        Policy = options.CatalogEntries == null ? configuredPolicy : CopyXIncludeResolverPolicyWithCatalogs(configuredPolicy, options.CatalogEntries);
        XmlCatalogEntry[]? catalogs = Policy.CatalogEntries;
        Resolver = catalogs == null || catalogs.Length == 0 ? resolver : new ResourceManager(resolver, Policy);
        Mutation = new TreeMutation();
        Navigation = new TreeNavigation();
        PointerEvaluator = new XPointerEvaluator();
        ActiveIncludes = new List<string>();
        KnownDocuments = new List<XmlDocument>();
        SourceDocuments = new List<XmlDocument>();
        SourceSnapshots = new List<XmlDocument>();
        MaximumIncludeDepth = options.MaximumDepth > 0 ? options.MaximumDepth : Policy.MaximumDepth > 0 ? Policy.MaximumDepth : 64;
        MaximumIncludeBytes = options.MaximumResourceBytes > 0 ? options.MaximumResourceBytes : Policy.MaximumResourceBytes > 0 ? Policy.MaximumResourceBytes : 67108864;
        CurrentIncludeDepth = 0;
    }

    internal XmlDocument ProcessXmlIncludeDocument(XmlDocument document)
    {
        Guard.NotNull(document, "document");
        XmlNode? container = document.DocumentNode;
        if (container == null)
        {
            return document;
        }

        Mutation.Invariants.ValidateXmlDocumentTree(document);
        RegisterXIncludeDocument(document);
        ProcessXmlIncludeChildren(container, document);
        Mutation.Invariants.ValidateXmlDocumentTree(document);
        return document;
    }

    internal void ProcessXmlIncludeChildren(XmlNode parent, XmlDocument document)
    {
        int index = 0;
        while (true)
        {
            XmlNode[] children = parent.Children == null ? [] : parent.Children;
            if (index >= children.Length)
            {
                return;
            }

            XmlNode child = children[index];
            if (IsXIncludeElement(child, "include"))
            {
                string? parse = FindXIncludeAttribute(child, "parse");
                if (parse == null || string.Equals(parse, "xml", StringComparison.Ordinal))
                {
                    EnterXIncludeDepth(child);
                    int inserted = ProcessXmlIncludeElement(parent, child, document);
                    LeaveXIncludeDepth();
                    index = index + inserted;
                    continue;
                }

                if (!string.Equals(parse, "text", StringComparison.Ordinal))
                {
                    throw CreateXIncludeError(child, XmlErrorCodes.IncludeFailure, "The XInclude parse attribute must be 'xml' or 'text'.");
                }

                EnterXIncludeDepth(child);
                int textInserted = ProcessXIncludeTextElement(parent, child, document);
                LeaveXIncludeDepth();
                index = index + textInserted;
                continue;
            }

            if (IsXIncludeElement(child, "fallback"))
            {
                throw CreateXIncludeError(child, XmlErrorCodes.IncludeFailure, "An XInclude fallback element must be the direct child of an include element.");
            }

            ProcessXmlIncludeChildren(child, document);
            index = index + 1;
        }
    }

    internal int ProcessXmlIncludeElement(XmlNode parent, XmlNode include, XmlDocument document)
    {
        XmlNode? fallback = ValidateXIncludeElementContent(include);
        ValidateXIncludeAcceptMetadata(include);
        XmlResolverPolicy requestPolicy = CreateXIncludeRequestPolicy(include);
        string? hrefAttribute = FindXIncludeAttribute(include, "href");
        string href = hrefAttribute == null ? "" : hrefAttribute;
        string? pointer = FindXIncludeAttribute(include, "xpointer");
        if (hrefAttribute == null && pointer == null)
        {
            throw CreateXIncludeError(include, XmlErrorCodes.IncludeFailure, "An XML XInclude without an href attribute requires an xpointer attribute.");
        }

        if (UriTools.HasFragment(href))
        {
            throw CreateXIncludeError(include, XmlErrorCodes.IncludeFailure, "An XInclude href attribute cannot contain a fragment identifier.");
        }

        XmlDocument? acquired = null;
        string location;
        if (href.Length == 0)
        {
            acquired = FindXIncludeSourceSnapshot(document);
            location = XIncludeDocumentLocation(document);
        }
        else
        {
            string? baseUri = Navigation.GetXmlNodeBaseUriOperation(include);
            XIncludeResolution resolution = ResolveXIncludeXmlResource(href, baseUri, requestPolicy);
            XmlResolvedResource? resource = resolution.Resource;
            if (resource == null)
            {
                return ReplaceXmlIncludeWithFallback(parent, include, fallback, document, resolution.ErrorMessage);
            }

            location = ResolveXIncludeResourceLocation(resource, href, baseUri);
            string key = CreateXIncludeKey(location, pointer);
            EnsureXIncludeNotActive(include, key);
            XmlInput input = CreateXIncludeInput(resource, location);
            string? sizeError = FindXIncludeResourceLimitError(input);
            if (sizeError != null)
            {
                return ReplaceXmlIncludeWithFallback(parent, include, fallback, document, sizeError);
            }

            ActiveIncludes.Add(key);
            acquired = ParseXIncludeXmlResource(input, document.Version);
            RegisterXIncludeDocument(acquired);
            if (Options.ProcessNestedIncludes)
            {
                XmlNode? acquiredContainer = acquired.DocumentNode;
                if (acquiredContainer != null)
                {
                    ProcessXmlIncludeChildren(acquiredContainer, acquired);
                }
            }

            RemoveActiveXInclude(key);
        }

        string includeLocation = location;
        string includeKey = CreateXIncludeKey(includeLocation, pointer);
        if (href.Length == 0)
        {
            EnsureXIncludeNotActive(include, includeKey);
            ActiveIncludes.Add(includeKey);
        }

        XmlNode[] selected;
        if (pointer == null)
        {
            selected = SelectXIncludeDocumentChildren(acquired);
        }
        else
        {
            XPointerEvaluation evaluation = PointerEvaluator.EvaluateXPointer(acquired, pointer);
            if (evaluation.IsError || evaluation.Node == null)
            {
                if (href.Length == 0)
                {
                    RemoveActiveXInclude(includeKey);
                }

                return ReplaceXmlIncludeWithFallback(parent, include, fallback, document, evaluation.ErrorMessage);
            }

            selected = [evaluation.Node];
        }

        XmlNode[] replacements = ImportXIncludeNodes(document, selected);
        ApplyXIncludeTopLevelFixups(parent, selected, replacements);
        if (href.Length == 0 && Options.ProcessNestedIncludes)
        {
            replacements = ProcessXIncludeReplacementNodes(document, replacements);
        }

        if (href.Length == 0)
        {
            RemoveActiveXInclude(includeKey);
        }

        PrependXIncludeHistoryToNodes(include, replacements);
        Mutation.ReplaceXmlTreeNodeWithSequenceOperation(parent, include, replacements);
        return replacements.Length;
    }

    internal int ProcessXIncludeTextElement(XmlNode parent, XmlNode include, XmlDocument document)
    {
        XmlNode? fallback = ValidateXIncludeElementContent(include);
        ValidateXIncludeAcceptMetadata(include);
        XmlResolverPolicy requestPolicy = CreateXIncludeRequestPolicy(include);
        string? pointer = FindXIncludeAttribute(include, "xpointer");
        if (pointer != null)
        {
            throw CreateXIncludeError(include, XmlErrorCodes.IncludeFailure, "The xpointer attribute is not permitted when XInclude parse mode is 'text'.");
        }

        string? hrefAttribute = FindXIncludeAttribute(include, "href");
        if (hrefAttribute == null || hrefAttribute.Length == 0)
        {
            return ReplaceXmlIncludeWithFallback(parent, include, fallback, document, "A text XInclude requires a non-empty href resource location.");
        }

        if (UriTools.HasFragment(hrefAttribute))
        {
            throw CreateXIncludeError(include, XmlErrorCodes.IncludeFailure, "An XInclude href attribute cannot contain a fragment identifier.");
        }

        string? baseUri = Navigation.GetXmlNodeBaseUriOperation(include);
        XIncludeResolution resolution = ResolveXIncludeTextResource(hrefAttribute, baseUri, requestPolicy);
        XmlResolvedResource? resource = resolution.Resource;
        if (resource == null)
        {
            return ReplaceXmlIncludeWithFallback(parent, include, fallback, document, resolution.ErrorMessage);
        }

        string location = ResolveXIncludeResourceLocation(resource, hrefAttribute, baseUri);
        XmlInput input = CreateXIncludeInput(resource, location);
        string? encoding = FindXIncludeAttribute(include, "encoding");
        XIncludeTextDecoder decoder = new XIncludeTextDecoder(input, input.MediaType, encoding, Options.TextEncoding, document.Version, MaximumIncludeBytes, true);
        XIncludeTextDecoding decoded = decoder.DecodeXIncludeText();
        if (decoded.ResourceError != null)
        {
            return ReplaceXmlIncludeWithFallback(parent, include, fallback, document, decoded.ResourceError);
        }

        if (decoded.FatalError != null)
        {
            throw CreateXIncludeError(include, XmlErrorCodes.IncludeFailure, decoded.FatalError);
        }

        string text = decoded.Text == null ? "" : decoded.Text;
        if (parent.Kind == XmlNodeKind.Document)
        {
            throw CreateXIncludeError(include, XmlErrorCodes.IncludeFailure, "XInclude text cannot replace the document element.");
        }

        XmlNode[] replacements;
        if (text.Length == 0)
        {
            replacements = [];
        }
        else
        {
            XmlNode textNode = Mutation.CreateXmlTreeNodeOperation(document, XmlNodeKind.Text, null, text);
            PrependXIncludeHistory(include, textNode);
            replacements = [textNode];
        }

        Mutation.ReplaceXmlTreeNodeWithSequenceOperation(parent, include, replacements);
        return replacements.Length;
    }

    internal XmlNode? ValidateXIncludeElementContent(XmlNode include)
    {
        XmlNode? fallback = null;
        XmlNode[] children = include.Children == null ? [] : include.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if (IsXIncludeNamespaceElement(child))
            {
                if (!IsXIncludeElement(child, "fallback") || fallback != null)
                {
                    throw CreateXIncludeError(child, XmlErrorCodes.IncludeFailure, "An include element may contain at most one XInclude fallback element and no other XInclude children.");
                }

                fallback = child;
            }

            index = index + 1;
        }

        return fallback;
    }

    internal XIncludeResolution ResolveXIncludeXmlResource(string href, string? baseUri, XmlResolverPolicy requestPolicy)
    {
        Task<XmlResolvedResource> pending = Task.Run(() => Resolver.ResolveXmlResource(XmlResourceKind.XIncludeXml, null, href, baseUri, requestPolicy));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion)
        {
            string message = "The XInclude XML resource could not be resolved.";
            AggregateException? aggregate = pending.Exception;
            if (aggregate != null && aggregate.InnerException != null && aggregate.InnerException.Message.Length > 0)
            {
                message = aggregate.InnerException.Message;
            }

            return new XIncludeResolution(null, message);
        }

        XmlResolvedResource resource = pending.Result;
        if (resource == null)
        {
            return new XIncludeResolution(null, "The XInclude resolver returned no resource.");
        }

        if (resource.Access != XmlResourceAccess.Allowed)
        {
            return new XIncludeResolution(null, "Access to the XInclude XML resource was denied.");
        }

        if (resource.Kind != XmlResourceKind.XIncludeXml)
        {
            return new XIncludeResolution(null, "The XInclude resolver returned a resource of the wrong kind.");
        }

        if (resource.Input == null)
        {
            return new XIncludeResolution(null, "The XInclude resolver returned a resource without input.");
        }

        return new XIncludeResolution(resource, null);
    }

    internal XIncludeResolution ResolveXIncludeTextResource(string href, string? baseUri, XmlResolverPolicy requestPolicy)
    {
        Task<XmlResolvedResource> pending = Task.Run(() => Resolver.ResolveXmlResource(XmlResourceKind.XIncludeText, null, href, baseUri, requestPolicy));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion)
        {
            string message = "The XInclude text resource could not be resolved.";
            AggregateException? aggregate = pending.Exception;
            if (aggregate != null && aggregate.InnerException != null && aggregate.InnerException.Message.Length > 0)
            {
                message = aggregate.InnerException.Message;
            }

            return new XIncludeResolution(null, message);
        }

        XmlResolvedResource resource = pending.Result;
        if (resource == null)
        {
            return new XIncludeResolution(null, "The XInclude resolver returned no text resource.");
        }

        if (resource.Access != XmlResourceAccess.Allowed)
        {
            return new XIncludeResolution(null, "Access to the XInclude text resource was denied.");
        }

        if (resource.Kind != XmlResourceKind.XIncludeText)
        {
            return new XIncludeResolution(null, "The XInclude resolver returned a resource of the wrong kind for text inclusion.");
        }

        if (resource.Input == null)
        {
            return new XIncludeResolution(null, "The XInclude resolver returned a text resource without input.");
        }

        return new XIncludeResolution(resource, null);
    }

    internal XmlInput CreateXIncludeInput(XmlResolvedResource resource, string location)
    {
        XmlInput source = resource.Input == null ? new XmlInput() : resource.Input;
        XmlInput input = new XmlInput();
        input.Kind = source.Kind;
        input.Text = source.Text;
        input.Characters = source.Characters;
        input.Bytes = source.Bytes;
        input.ByteStream = source.ByteStream;
        input.TextReader = source.TextReader;
        input.BaseUri = location.Length == 0 ? source.BaseUri : location;
        input.MediaType = resource.MediaType == null ? source.MediaType : resource.MediaType;
        input.EncodingName = resource.EncodingName == null ? source.EncodingName : resource.EncodingName;
        input.Encoding = source.Encoding;
        input.OwnsResource = resource.OwnsResource || source.OwnsResource;
        return input;
    }

    internal XmlDocument ParseXIncludeXmlResource(XmlInput input, XmlVersion version)
    {
        XmlParserOptions options = new XmlParserOptions();
        options.Version = version;
        options.NamespaceHandling = version == XmlVersion.Xml11 ? XmlNamespaceHandling.Namespaces11 : XmlNamespaceHandling.Namespaces10;
        options.DtdProcessing = XmlDtdProcessing.Ignore;
        options.EntityHandling = XmlEntityHandling.Expand;
        options.ValidationMode = XmlValidationMode.None;
        options.PreserveCData = true;
        options.PreserveComments = true;
        options.PreserveProcessingInstructions = true;
        options.PreservePrefixes = true;
        options.PreserveIgnorableWhitespace = true;
        options.PreserveSourceLocations = true;
        options.RetainNamespaceDeclarations = true;
        options.ResolverPolicy = Policy;
        options.SecurityLimits = CreateXIncludeParserLimits();
        return new XmlParser().ParseXmlDocument(input, options, Resolver, null, null);
    }

    internal XmlNode[] SelectXIncludeDocumentChildren(XmlDocument document)
    {
        XmlNode? container = document.DocumentNode;
        if (container == null || container.Children == null)
        {
            return [];
        }

        List<XmlNode> selected = new List<XmlNode>();
        int index = 0;
        while (index < container.Children.Length)
        {
            XmlNode child = container.Children[index];
            if (child.Kind != XmlNodeKind.DocumentType)
            {
                selected.Add(child);
            }

            index = index + 1;
        }

        return selected.ToArray();
    }

    internal XmlNode[] ImportXIncludeNodes(XmlDocument document, XmlNode[] selected)
    {
        XmlNode[] result = new XmlNode[selected.Length];
        int index = 0;
        while (index < selected.Length)
        {
            result[index] = Mutation.ImportXmlTreeNodeOperation(document, selected[index], true);
            PreserveXIncludeTopLevelNamespaces(selected[index], result[index]);
            index = index + 1;
        }

        return result;
    }

    internal XmlNode[] ProcessXIncludeReplacementNodes(XmlDocument document, XmlNode[] nodes)
    {
        XmlNode wrapper = Mutation.CreateXmlTreeNodeOperation(document, XmlNodeKind.DocumentFragment, null, null);
        int index = 0;
        while (index < nodes.Length)
        {
            Mutation.AppendXmlTreeChildOperation(wrapper, nodes[index]);
            index = index + 1;
        }

        ProcessXmlIncludeChildren(wrapper, document);
        XmlNode[] result = wrapper.Children == null ? [] : wrapper.Children;
        wrapper.Children = [];
        index = 0;
        while (index < result.Length)
        {
            result[index].Parent = null;
            index = index + 1;
        }

        return result;
    }

    internal int ReplaceXmlIncludeWithFallback(XmlNode parent, XmlNode include, XmlNode? fallback, XmlDocument document, string? cause)
    {
        if (fallback == null)
        {
            string message = cause == null ? "The XInclude resource failed and no fallback was supplied." : cause + " No XInclude fallback was supplied.";
            throw CreateXIncludeError(include, XmlErrorCodes.IncludeFallbackMissing, message);
        }

        ValidateXIncludeFallbackContent(fallback);
        XmlNode[] selected = Options.PreserveFallbackNodes ? [fallback] : fallback.Children == null ? [] : fallback.Children;
        XmlNode[] replacements = ImportXIncludeNodes(document, selected);
        ApplyXIncludeTopLevelFixups(parent, selected, replacements);
        if (Options.ProcessNestedIncludes)
        {
            if (Options.PreserveFallbackNodes && replacements.Length == 1)
            {
                ProcessXmlIncludeChildren(replacements[0], document);
            }
            else
            {
                replacements = ProcessXIncludeReplacementNodes(document, replacements);
            }
        }

        PrependXIncludeHistoryToNodes(include, replacements);
        Mutation.ReplaceXmlTreeNodeWithSequenceOperation(parent, include, replacements);
        return replacements.Length;
    }

    internal void PreserveXIncludeTopLevelNamespaces(XmlNode source, XmlNode copy)
    {
        if (source.Kind != XmlNodeKind.Element || copy.Kind != XmlNodeKind.Element)
        {
            return;
        }

        copy.NamespaceBindings = Navigation.SelectXmlNamespacesOperation(source);
    }

    internal void ApplyXIncludeTopLevelFixups(XmlNode parent, XmlNode[] sources, XmlNode[] copies)
    {
        string? parentBase = Options.FixupBaseUri ? Navigation.GetXmlNodeBaseUriOperation(parent) : null;
        string? parentLanguage = Options.FixupLanguage ? Navigation.GetXmlNodeLanguageOperation(parent) : null;
        int index = 0;
        while (index < sources.Length && index < copies.Length)
        {
            XmlNode source = sources[index];
            XmlNode copy = copies[index];
            if (source.Kind == XmlNodeKind.Element && copy.Kind == XmlNodeKind.Element)
            {
                if (Options.FixupBaseUri)
                {
                    string? sourceBase = Navigation.GetXmlNodeBaseUriOperation(source);
                    if (!string.Equals(sourceBase, parentBase, StringComparison.Ordinal))
                    {
                        SetXIncludeXmlAttribute(copy, "base", sourceBase == null ? "" : sourceBase);
                    }
                }

                if (Options.FixupLanguage)
                {
                    string? sourceLanguage = Navigation.GetXmlNodeLanguageOperation(source);
                    bool sameLanguage = string.Equals(sourceLanguage, parentLanguage, StringComparison.OrdinalIgnoreCase);
                    if (!sameLanguage || parent.Kind == XmlNodeKind.Document && sourceLanguage != null)
                    {
                        SetXIncludeXmlAttribute(copy, "lang", sourceLanguage == null ? "" : sourceLanguage);
                    }
                }
            }

            index = index + 1;
        }
    }

    internal void SetXIncludeXmlAttribute(XmlNode element, string localName, string value)
    {
        XmlDocument? optionalDocument = element.OwnerDocument;
        if (optionalDocument == null)
        {
            throw new ArgumentException("An included element must have an owner document.", "element");
        }

        XmlName name = new XmlName();
        name.Prefix = "xml";
        name.LocalName = localName;
        name.NamespaceUri = XmlNamespaces.Xml;
        name.QualifiedName = "xml:" + localName;
        XmlNode attribute = Mutation.CreateXmlTreeNodeOperation(optionalDocument, XmlNodeKind.Attribute, name, value);
        TreeContentMutation content = new TreeContentMutation(Mutation);
        content.SetXmlTreeAttributeOperation(element, attribute);
    }

    internal void PrependXIncludeHistoryToNodes(XmlNode include, XmlNode[] nodes)
    {
        int index = 0;
        while (index < nodes.Length)
        {
            PrependXIncludeHistory(include, nodes[index]);
            index = index + 1;
        }
    }

    internal void PrependXIncludeHistory(XmlNode include, XmlNode node)
    {
        XmlNode[] existing = node.IncludeHistory == null ? [] : node.IncludeHistory;
        XmlNode[] history = new XmlNode[existing.Length + 1];
        history[0] = include;
        if (existing.Length > 0)
        {
            Array.Copy(existing, 0, history, 1, existing.Length);
        }

        node.IncludeHistory = history;
    }

    internal void ValidateXIncludeFallbackContent(XmlNode fallback)
    {
        XmlNode[] children = fallback.Children == null ? [] : fallback.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if (IsXIncludeNamespaceElement(child) && !IsXIncludeElement(child, "include"))
            {
                throw CreateXIncludeError(child, XmlErrorCodes.IncludeFailure, "An XInclude fallback can contain include elements but no other XInclude elements.");
            }

            index = index + 1;
        }
    }

    internal string? FindXIncludeAttribute(XmlNode element, string localName)
    {
        XmlNode[] attributes = element.Attributes == null ? [] : element.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            XmlName? name = attributes[index].Name;
            if (name != null && string.Equals(name.LocalName, localName, StringComparison.Ordinal) && (name.NamespaceUri == null || name.NamespaceUri.Length == 0))
            {
                return attributes[index].Value == null ? "" : attributes[index].Value;
            }

            index = index + 1;
        }

        return null;
    }

    internal bool IsXIncludeNamespaceElement(XmlNode node)
    {
        return node.Kind == XmlNodeKind.Element && node.Name != null && string.Equals(node.Name.NamespaceUri, XmlNamespaces.XInclude, StringComparison.Ordinal);
    }

    internal bool IsXIncludeElement(XmlNode node, string localName)
    {
        return IsXIncludeNamespaceElement(node) && node.Name != null && string.Equals(node.Name.LocalName, localName, StringComparison.Ordinal);
    }

    internal void ValidateXIncludeAcceptMetadata(XmlNode include)
    {
        string? accept = FindXIncludeRequestMetadata(include, "accept", Options.Accept, Policy.Accept);

        ValidateXIncludeHeaderValue(include, accept, "accept");
        string? language = FindXIncludeRequestMetadata(include, "accept-language", Options.AcceptLanguage, Policy.AcceptLanguage);

        ValidateXIncludeHeaderValue(include, language, "accept-language");
    }

    internal string? FindXIncludeRequestMetadata(XmlNode include, string name, string? configured, string? inherited)
    {
        string? value = FindXIncludeAttribute(include, name);
        if (value != null)
        {
            return value;
        }

        return configured == null ? inherited : configured;
    }

    internal XmlResolverPolicy CreateXIncludeRequestPolicy(XmlNode include)
    {
        XmlResolverPolicy result = new XmlResolverPolicy();
        result.Access = Policy.Access;
        result.AllowedSchemes = Policy.AllowedSchemes;
        result.AllowedRoots = Policy.AllowedRoots;
        result.AllowRedirects = Policy.AllowRedirects;
        result.MaximumRedirects = Policy.MaximumRedirects;
        result.MaximumDepth = Policy.MaximumDepth;
        result.MaximumResourceBytes = Policy.MaximumResourceBytes;
        result.MaximumWorkUnits = Policy.MaximumWorkUnits;
        result.CacheResources = Policy.CacheResources;
        result.CatalogEntries = Policy.CatalogEntries;
        result.Accept = FindXIncludeRequestMetadata(include, "accept", Options.Accept, Policy.Accept);
        result.AcceptLanguage = FindXIncludeRequestMetadata(include, "accept-language", Options.AcceptLanguage, Policy.AcceptLanguage);
        return result;
    }

    internal void ValidateXIncludeHeaderValue(XmlNode include, string? value, string name)
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
                throw CreateXIncludeError(include, XmlErrorCodes.IncludeFailure, "The XInclude " + name + " metadata contains a character that is not permitted in an HTTP header.");
            }

            index = index + 1;
        }
    }

    internal string ResolveXIncludeResourceLocation(XmlResolvedResource resource, string href, string? baseUri)
    {
        if (resource.AbsoluteUri != null && resource.AbsoluteUri.Length > 0)
        {
            return resource.AbsoluteUri;
        }

        if (resource.Input != null && resource.Input.BaseUri != null && resource.Input.BaseUri.Length > 0)
        {
            return resource.Input.BaseUri;
        }

        if (baseUri != null && baseUri.Length > 0)
        {
            return UriTools.CanonicalAbsoluteUri(UriTools.ResolveReference(UriTools.CreateAbsolute(baseUri), href));
        }

        return href;
    }

    internal string XIncludeDocumentLocation(XmlDocument document)
    {
        string? location = document.DocumentUri == null ? document.BaseUri : document.DocumentUri;
        if (location != null && location.Length > 0)
        {
            return location;
        }

        int index = 0;
        while (index < KnownDocuments.Count)
        {
            if (KnownDocuments[index] == document)
            {
                return "urn:xml-document:" + index;
            }

            index = index + 1;
        }

        KnownDocuments.Add(document);
        return "urn:xml-document:" + (KnownDocuments.Count - 1);
    }

    internal void RegisterXIncludeDocument(XmlDocument document)
    {
        int index = 0;
        while (index < KnownDocuments.Count)
        {
            if (KnownDocuments[index] == document)
            {
                return;
            }

            index = index + 1;
        }

        KnownDocuments.Add(document);
        SourceDocuments.Add(document);
        SourceSnapshots.Add(CreateXIncludeSourceSnapshot(document));
    }

    internal XmlDocument FindXIncludeSourceSnapshot(XmlDocument document)
    {
        int index = 0;
        while (index < SourceDocuments.Count)
        {
            if (SourceDocuments[index] == document)
            {
                return SourceSnapshots[index];
            }

            index = index + 1;
        }

        RegisterXIncludeDocument(document);
        return SourceSnapshots[SourceSnapshots.Count - 1];
    }

    internal XmlDocument CreateXIncludeSourceSnapshot(XmlDocument document)
    {
        XmlDocument snapshot = new XmlDocument();
        snapshot.Version = document.Version;
        snapshot.Standalone = document.Standalone;
        snapshot.DocumentUri = document.DocumentUri;
        snapshot.BaseUri = document.BaseUri;
        snapshot.EncodingName = document.EncodingName;
        snapshot.Diagnostics = document.Diagnostics;
        snapshot.MutationVersion = document.MutationVersion;
        snapshot.NextIdentity = document.NextIdentity;
        snapshot.DocumentType = document.DocumentType;
        XmlNode? container = document.DocumentNode;
        if (container != null)
        {
            XmlNode copy = Mutation.ImportXmlTreeNodeOperation(snapshot, container, true);
            snapshot.DocumentNode = copy;
            XmlNode[] children = copy.Children == null ? [] : copy.Children;
            int index = 0;
            while (index < children.Length)
            {
                if (children[index].Kind == XmlNodeKind.Element)
                {
                    snapshot.DocumentElement = children[index];
                }

                index = index + 1;
            }
        }

        snapshot.IsReadOnly = true;
        return snapshot;
    }

    internal string CreateXIncludeKey(string location, string? pointer)
    {
        string suffix = pointer == null ? "" : pointer;
        return location.Length + ":" + location + ":" + suffix.Length + ":" + suffix;
    }

    internal void EnsureXIncludeNotActive(XmlNode include, string key)
    {
        int index = 0;
        while (index < ActiveIncludes.Count)
        {
            if (string.Equals(ActiveIncludes[index], key, StringComparison.Ordinal))
            {
                throw CreateXIncludeError(include, XmlErrorCodes.IncludeCycle, "An XInclude recursion loop was detected.");
            }

            index = index + 1;
        }
    }

    internal void RemoveActiveXInclude(string key)
    {
        int index = ActiveIncludes.Count - 1;
        if (index >= 0 && string.Equals(ActiveIncludes[index], key, StringComparison.Ordinal))
        {
            ActiveIncludes.RemoveAt(index);
            return;
        }

        while (index >= 0)
        {
            if (string.Equals(ActiveIncludes[index], key, StringComparison.Ordinal))
            {
                ActiveIncludes.RemoveAt(index);
                return;
            }

            index = index - 1;
        }
    }

    internal void EnterXIncludeDepth(XmlNode include)
    {
        int attempted = CurrentIncludeDepth + 1;
        if (attempted > MaximumIncludeDepth)
        {
            throw CreateXIncludeLimitError(include, "include depth", MaximumIncludeDepth, attempted);
        }

        CurrentIncludeDepth = attempted;
    }

    internal void LeaveXIncludeDepth()
    {
        if (CurrentIncludeDepth > 0)
        {
            CurrentIncludeDepth = CurrentIncludeDepth - 1;
        }
    }

    internal string? FindXIncludeResourceLimitError(XmlInput input)
    {
        long size = -1;
        if (input.Kind == XmlInputKind.ByteMemory)
        {
            size = input.Bytes.Length;
        }
        else if (input.Kind == XmlInputKind.String && input.Text != null)
        {
            size = input.Text.Length;
        }
        else if (input.Kind == XmlInputKind.CharacterMemory)
        {
            size = input.Characters.Length;
        }

        if (size > MaximumIncludeBytes)
        {
            return "The XInclude resource size of " + size + " exceeds the configured limit of " + MaximumIncludeBytes + ".";
        }

        return null;
    }

    internal XmlSecurityLimits CreateXIncludeParserLimits()
    {
        XmlSecurityLimits limits = new XmlSecurityLimits();
        limits.MaximumDepth = 256;
        limits.MaximumNodeCount = 1000000;
        limits.MaximumAttributesPerElement = 10000;
        limits.MaximumNameLength = 16384;
        limits.MaximumTextLength = MaximumIncludeBytes;
        limits.MaximumTokenLength = MaximumIncludeBytes > int.MaxValue ? int.MaxValue : (int)MaximumIncludeBytes;
        limits.MaximumInputCharacters = MaximumIncludeBytes;
        limits.MaximumInputBytes = MaximumIncludeBytes;
        limits.MaximumDtdCharacters = 16777216;
        limits.MaximumEntityDepth = 32;
        limits.MaximumEntityCount = 100000;
        limits.MaximumEntityCharacters = 67108864;
        limits.MaximumEntityAmplification = 100;
        limits.MaximumExternalResources = 64;
        limits.MaximumResourceDepth = 16;
        limits.MaximumResourceBytes = MaximumIncludeBytes;
        limits.CancellationPollingInterval = 1024;
        return limits;
    }

    internal XmlLimitException CreateXIncludeLimitError(XmlNode node, string limitName, long maximum, long attempted)
    {
        string message = "The configured XInclude " + limitName + " limit of " + maximum + " was exceeded by an attempted value of " + attempted + ".";
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XmlErrorCodes.LimitExceeded;
        diagnostic.Category = XmlErrorCategory.Limit;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = node.Range;
        diagnostic.IncludeStack = ActiveIncludes.ToArray();
        SourceLocation? location = node.Range == null ? null : node.Range.Start;
        return new XmlLimitException(message, diagnostic, location);
    }

    internal XmlResourceException CreateXIncludeError(XmlNode node, string code, string message)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.Include;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = node.Range;
        diagnostic.IncludeStack = ActiveIncludes.ToArray();
        SourceLocation? location = node.Range == null ? null : node.Range.Start;
        return new XmlResourceException(message, diagnostic, location);
    }

    internal XmlResolverPolicy CreateDefaultXIncludeResolverPolicy(XmlIncludeOptions options)
    {
        XmlResolverPolicy policy = new XmlResolverPolicy();
        policy.Access = XmlResourceAccess.Denied;
        policy.AllowRedirects = false;
        policy.MaximumRedirects = 0;
        policy.MaximumDepth = options.MaximumDepth > 0 ? options.MaximumDepth : 64;
        policy.MaximumResourceBytes = options.MaximumResourceBytes > 0 ? options.MaximumResourceBytes : 67108864;
        policy.MaximumWorkUnits = 67108864;
        policy.CacheResources = false;
        policy.CatalogEntries = options.CatalogEntries;
        policy.Accept = options.Accept;
        policy.AcceptLanguage = options.AcceptLanguage;
        return policy;
    }

    internal XmlResolverPolicy CopyXIncludeResolverPolicyWithCatalogs(XmlResolverPolicy source, XmlCatalogEntry[] catalogs)
    {
        XmlResolverPolicy policy = new XmlResolverPolicy();
        policy.Access = source.Access;
        policy.AllowedSchemes = source.AllowedSchemes;
        policy.AllowedRoots = source.AllowedRoots;
        policy.AllowRedirects = source.AllowRedirects;
        policy.MaximumRedirects = source.MaximumRedirects;
        policy.MaximumDepth = source.MaximumDepth;
        policy.MaximumResourceBytes = source.MaximumResourceBytes;
        policy.MaximumWorkUnits = source.MaximumWorkUnits;
        policy.CacheResources = source.CacheResources;
        policy.CatalogEntries = catalogs;
        policy.Accept = source.Accept;
        policy.AcceptLanguage = source.AcceptLanguage;
        return policy;
    }
}
