using System;
using System.Threading.Tasks;

namespace xml;

internal class XsltDocumentRuntime
{
    internal XsltSequenceConstructorRuntime Runtime;
    internal XsltTransformationState State;
    internal XdmSequenceOperations Sequences;

    internal XsltDocumentRuntime(XsltSequenceConstructorRuntime runtime)
    {
        Runtime = runtime;
        State = runtime.State;
        Sequences = new XdmSequenceOperations();
    }

    internal XdmSequence EvaluateXsltSourceDocument(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        XsltInstructionPlan instruction = item.Instruction == null ? throw new InvalidOperationException("A source-document instruction plan is required.") : item.Instruction;
        string reference = Runtime.EvaluateXsltAttributeTemplate(Runtime.FindXsltAttribute(instruction, "href"), invocation, "").Trim();
        string uri = ResolveXsltDocumentUri(reference, instruction);
        XmlDocument document = LoadXsltDocument(uri, instruction);
        XmlNode root = document.DocumentNode == null ? throw CreateXsltDocumentError("The source document has no document node.", instruction.Instruction) : document.DocumentNode;
        XsltStreamedNodeRuntime? previousStream = State.StreamedNodeRuntime;
        XPathDynamicContext? callerContext = invocation.Frame == null ? null : invocation.Frame.Context;
        bool streamable = Runtime.ReadXsltBooleanAttribute(instruction, "streamable", false);
        if (streamable && previousStream != null)
        {
            XsltStreamedNodeRuntime nested = new XsltStreamedNodeRuntime(previousStream.MaximumGroundedNodes);
            nested.RegisterXsltStreamFocus(root);
            State.StreamedNodeRuntime = nested;
            State.DynamicContext.XsltStreamedNodes = nested;
            if (callerContext != null)
            {
                callerContext.XsltStreamedNodes = nested;
            }
        }

        XdmSequence contextItem = Sequences.CreateXdmNodeSequence(root);
        XPathDynamicContext basis = invocation.Frame == null ? State.DynamicContext : invocation.Frame.Context;
        XPathDynamicContext context = Runtime.Templates.CreateXsltTemplateFocusContext(basis, contextItem, 1, 1);
        XsltVariableFrame frame = new XsltVariableFrame(invocation.Frame, invocation.Frame == null ? [] : invocation.Frame.TunnelParameters, context);
        XsltTemplateInvocation focused = Runtime.CreateXsltFocusedInvocation(invocation, contextItem, frame, 1, 1);
        XdmSequence result = item.Content == null ? Sequences.CreateEmptyXdmSequence() : Runtime.EvaluateXsltSequenceConstructor(item.Content, focused);
        if (streamable && previousStream != null)
        {
            State.StreamedNodeRuntime = previousStream;
            State.DynamicContext.XsltStreamedNodes = previousStream;
            if (callerContext != null)
            {
                callerContext.XsltStreamedNodes = previousStream;
            }
        }

        return result;
    }

    internal string ResolveXsltDocumentUri(string reference, XsltInstructionPlan instruction)
    {
        if (!Uri.IsWellFormedUriString(reference, UriKind.RelativeOrAbsolute))
        {
            throw CreateXsltDocumentUriError("The source document href is not a valid URI reference.", XPathErrorCodes.FODC0005, instruction.Instruction);
        }

        Uri absolute;
        if (Uri.IsWellFormedUriString(reference, UriKind.Absolute))
        {
            absolute = new Uri(reference, UriKind.Absolute);
        }
        else
        {
            string? baseUri = instruction.StaticContext.BaseUri;
            if (baseUri == null || !Uri.IsWellFormedUriString(baseUri, UriKind.Absolute))
            {
                throw CreateXsltDocumentUriError("The relative source document href has no absolute base URI.", XPathErrorCodes.FODC0002, instruction.Instruction);
            }

            absolute = new Uri(new Uri(baseUri, UriKind.Absolute), reference);
        }

        if (absolute.Fragment.Length != 0)
        {
            throw CreateXsltDocumentError("A source document URI must not contain a fragment identifier.", instruction.Instruction);
        }

        return UriTools.CanonicalAbsoluteUri(absolute);
    }

    internal XmlDocument LoadXsltDocument(string uri, XsltInstructionPlan instruction)
    {
        if (State.ResultDocumentRuntime != null)
        {
            State.ResultDocumentRuntime.EnsureXsltResultUriReadable(uri, instruction.Instruction);
        }

        XmlDocument? existing = State.DocumentPool.FindXPathDocumentResource(uri);
        if (existing != null)
        {
            return existing;
        }

        IXmlResourceResolver? optionalResolver = State.DocumentPool.ResourceResolver;
        if (optionalResolver == null)
        {
            throw CreateXsltDocumentError("No XML resource resolver is available for '" + uri + "'.", instruction.Instruction);
        }

        IXmlResourceResolver resolver = optionalResolver;
        Task<XmlResolvedResource> pending = Task.Run(() => ResolveXsltDocumentResource(resolver, uri));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion || pending.Result == null || pending.Result.Access != XmlResourceAccess.Allowed || pending.Result.Input == null)
        {
            State.DocumentPool.RegisterUnavailableXPathDocumentResource(uri);
            throw CreateXsltDocumentError("The source document resource '" + uri + "' is unavailable.", instruction.Instruction);
        }

        XmlResolvedResource resource = pending.Result;
        XmlInput input = CopyXsltDocumentInput(resource, uri);
        XmlParserOptions options = CreateXsltDocumentParserOptions();
        Task<XmlDocument> parsing = Task.Run(() => new XmlParser().ParseXmlDocument(input, options, resolver, null, State.DynamicContext.Cancellation));
        ((IAsyncResult)parsing).AsyncWaitHandle.WaitOne();
        if (parsing.Status != TaskStatus.RanToCompletion || parsing.Result == null)
        {
            State.DocumentPool.RegisterUnavailableXPathDocumentResource(uri);
            throw CreateXsltDocumentError("The source document resource '" + uri + "' could not be parsed.", instruction.Instruction);
        }

        XmlDocument document = parsing.Result;
        document.BaseUri = uri;
        document.DocumentUri = uri;
        State.DocumentPool.RegisterXPathDocumentResource(uri, document);
        return document;
    }

    internal XmlResolvedResource ResolveXsltDocumentResource(IXmlResourceResolver resolver, string uri)
    {
        ResolverGuard guard = new ResolverGuard(State.DocumentPool.Policy, null);
        using (ResolverGuardScope scope = guard.BeginXmlResource(uri, null))
        {
            XmlResolvedResource resource = resolver.ResolveXmlResource(XmlResourceKind.Document, null, uri, null, State.DocumentPool.Policy);
            guard.ValidateXmlResolvedResource(scope, XmlResourceKind.Document, resource);
            return resource;
        }
    }

    internal XmlInput CopyXsltDocumentInput(XmlResolvedResource resource, string uri)
    {
        XmlInput source = resource.Input == null ? new XmlInput() : resource.Input;
        XmlInput result = new XmlInput();
        result.Kind = source.Kind;
        result.Text = source.Text;
        result.Characters = source.Characters;
        result.Bytes = source.Bytes;
        result.ByteStream = source.ByteStream;
        result.TextReader = source.TextReader;
        result.BaseUri = resource.AbsoluteUri == null || resource.AbsoluteUri.Length == 0 ? uri : resource.AbsoluteUri;
        result.MediaType = resource.MediaType == null ? source.MediaType : resource.MediaType;
        result.EncodingName = resource.EncodingName == null ? source.EncodingName : resource.EncodingName;
        result.Encoding = source.Encoding;
        result.OwnsResource = resource.OwnsResource || source.OwnsResource;
        return result;
    }

    internal XmlParserOptions CreateXsltDocumentParserOptions()
    {
        XmlParserOptions result = new XmlParserOptions();
        result.Version = XmlVersion.Xml10;
        result.NamespaceHandling = XmlNamespaceHandling.Namespaces10;
        result.DtdProcessing = XmlDtdProcessing.Prohibit;
        result.EntityHandling = XmlEntityHandling.Expand;
        result.ValidationMode = XmlValidationMode.None;
        result.PreserveComments = true;
        result.PreserveProcessingInstructions = true;
        result.PreservePrefixes = true;
        result.PreserveSourceLocations = true;
        result.RetainNamespaceDeclarations = true;
        result.SecurityLimits = State.DynamicContext.SecurityLimits;
        result.ResolverPolicy = State.DynamicContext.ResolverPolicy;
        return result;
    }

    internal XsltDynamicException CreateXsltDocumentError(string message, XmlNode node)
    {
        return CreateXsltDocumentUriError(message, XPathErrorCodes.FODC0002, node);
    }

    internal XsltDynamicException CreateXsltDocumentUriError(string message, string code, XmlNode node)
    {
        return Runtime.CreateXsltConstructionError(message, code, node);
    }
}
