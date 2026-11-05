using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace xml;

internal class XsltResultDocumentRuntime
{
    internal XsltTransformationState State;
    internal IXsltResultDocumentResolver Resolver;
    internal List<XsltSecondaryResult> Results;
    internal List<string> ResultUris;

    internal XsltResultDocumentRuntime(XsltTransformationState state, IXsltResultDocumentResolver resolver)
    {
        State = state;
        Resolver = resolver;
        Results = new List<XsltSecondaryResult>();
        ResultUris = new List<string>();
    }

    internal XdmSequence EvaluateXsltResultDocument(XsltSequenceItemPlan item, XsltTemplateInvocation invocation, XsltSequenceConstructorRuntime runtime)
    {
        XsltInstructionPlan instruction = item.Instruction == null ? throw new InvalidOperationException("A result-document instruction plan is required.") : item.Instruction;
        if (IsXsltTemporaryOutputInstruction(instruction.Instruction))
        {
            throw CreateXsltResultDocumentError("xsl:result-document cannot be evaluated while constructing a temporary tree.", XsltErrorCodes.XTDE1480, instruction.Instruction);
        }

        XsltOutputFormatPlan format = SelectXsltResultOutputFormat(instruction, invocation, runtime);
        XmlSerializationOptions options = new XsltTransformationEngine().CopyXsltRuntimeSerializationOptions(format.Options);
        ApplyXsltResultSerializationAttributes(instruction, invocation, runtime, options);
        new SerializationParameterValidator().ValidateSerializationParameters(options);
        string uri = ResolveXsltResultUri(instruction, invocation, runtime);
        XsltSecondaryResult result = Resolver.AllocateXsltResultDocument(uri, options);
        if (result == null)
        {
            throw CreateXsltResultDocumentError("The result-document resolver did not allocate a destination.", XsltErrorCodes.XTDE1490, instruction.Instruction);
        }

        result.Uri = uri;
        result.SerializationOptions = options;
        result.Disposition = XsltResultDisposition.Pending;
        result.Succeeded = false;
        XmlDiagnostic? collision = FindXsltResultCollision(uri, instruction.Instruction);
        if (collision != null)
        {
            Resolver.RejectXsltResultDocument(result, collision);
            throw new XsltDynamicException(collision.Message == null ? "The result destination URI is already in use." : collision.Message, collision, collision.Range == null ? null : collision.Range.Start);
        }

        ResultUris.Add(uri);
        Results.Add(result);
        PublishXsltSecondaryResults();
        XPathDynamicContext basis = invocation.Frame == null ? State.DynamicContext : invocation.Frame.Context;
        XPathDynamicContext context = new XPathFunctionContextFactory().CopyXPathFunctionDynamicComponents(basis);
        context.XsltCurrentOutputUri = uri;
        XsltVariableFrame frame = new XsltVariableFrame(invocation.Frame, invocation.Frame == null ? [] : invocation.Frame.TunnelParameters, context);
        XsltTemplateInvocation focused = runtime.CreateXsltFocusedInvocation(invocation, invocation.ContextItem, frame, invocation.Position, invocation.Size);
        XdmSequence value = item.Content == null ? new XdmSequenceOperations().CreateEmptyXdmSequence() : runtime.EvaluateXsltSequenceConstructor(item.Content, focused);
        result.Value = new XdmSequenceOperations().FreezeXdmSequenceStorage(value);
        if (result.Output != null)
        {
            options.ExtensionState = State.DynamicContext.ExtensionState;
            new XmlSerializer().SerializeXdmSequence(result.Value, result.Output, options);
        }

        return new XdmSequenceOperations().CreateEmptyXdmSequence();
    }

    internal void CompleteXsltResultDocuments(bool success)
    {
        int index = 0;
        while (index < Results.Count)
        {
            XsltSecondaryResult result = Results[index];
            if (success)
            {
                CompleteXsltResultDocumentCallback(result, true);
                result.Disposition = XsltResultDisposition.Commit;
                result.Succeeded = true;
            }
            else
            {
                CompleteXsltResultDocumentCallback(result, false);
                result.Disposition = XsltResultDisposition.Rollback;
                result.Succeeded = false;
            }

            index = index + 1;
        }

        PublishXsltSecondaryResults();
    }

    internal void CompleteXsltResultDocumentCallback(XsltSecondaryResult result, bool commit)
    {
        Task pending;
        if (commit)
        {
            pending = Task.Run(() => Resolver.CommitXsltResultDocument(result));
        }
        else
        {
            pending = Task.Run(() => Resolver.RollbackXsltResultDocument(result));
        }

        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion)
        {
            string operation = commit ? "commit" : "rollback";
            string detail = ReadXsltResultDocumentCallbackFailure(pending.Exception);
            throw CreateXsltResultDocumentCallbackError("The result-document resolver failed to " + operation + " a result: " + detail);
        }
    }

    internal string ReadXsltResultDocumentCallbackFailure(AggregateException? aggregate)
    {
        Exception failure = aggregate == null ? new InvalidOperationException("The result-document resolver failed without an exception.") : aggregate;
        while (failure.GetType().FullName == "System.AggregateException")
        {
            AggregateException current = (AggregateException)failure;
            if (current.InnerExceptions.Count == 0)
            {
                break;
            }

            failure = current.InnerExceptions[0];
        }

        return failure.Message;
    }

    internal XsltDynamicException CreateXsltResultDocumentCallbackError(string message)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XsltErrorCodes.XTDE1490;
        diagnostic.Category = XmlErrorCategory.XsltDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XsltDynamicException(message, diagnostic, null);
    }

    internal XsltOutputFormatPlan SelectXsltResultOutputFormat(XsltInstructionPlan instruction, XsltTemplateInvocation invocation, XsltSequenceConstructorRuntime runtime)
    {
        XsltInstructionAttributePlan? attribute = runtime.FindXsltAttribute(instruction, "format");
        string name = "#default";
        if (attribute != null)
        {
            string lexical = runtime.EvaluateXsltNodeValueTemplate(attribute.Attribute, invocation, attribute.Source).Trim();
            XmlName? expanded = ResolveXsltResultEqName(lexical, instruction.StaticContext.Namespaces, false);
            if (expanded == null)
            {
                throw CreateXsltResultDocumentError("The result-document format is not a valid EQName.", XsltErrorCodes.XTDE1460, attribute.Attribute);
            }

            name = CreateXsltExpandedResultName(expanded);
        }

        int index = 0;
        while (index < State.Stylesheet.Outputs.Length)
        {
            if (State.Stylesheet.Outputs[index].Name == name)
            {
                return State.Stylesheet.Outputs[index];
            }

            index = index + 1;
        }

        throw CreateXsltResultDocumentError("The selected result-document output format does not exist.", XsltErrorCodes.XTDE1460, instruction.Instruction);
    }

    internal string ResolveXsltResultUri(XsltInstructionPlan instruction, XsltTemplateInvocation invocation, XsltSequenceConstructorRuntime runtime)
    {
        XsltInstructionAttributePlan? attribute = runtime.FindXsltAttribute(instruction, "href");
        string reference = attribute == null ? "" : runtime.EvaluateXsltNodeValueTemplate(attribute.Attribute, invocation, attribute.Source);
        string? outputBase = State.PrincipalDestination.BaseUri;
        string basis = outputBase == null || outputBase.Length == 0 ? instruction.StaticContext.BaseUri == null ? "" : instruction.StaticContext.BaseUri : outputBase;
        Task<string> pending = Task.Run(() => UriTools.CanonicalAbsoluteUri(UriTools.ResolveReference(UriTools.CreateAbsolute(basis), reference)));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion)
        {
            throw CreateXsltResultDocumentError("The result-document href is not a valid URI reference.", XsltErrorCodes.XTDE1490, attribute == null ? instruction.Instruction : attribute.Attribute);
        }

        return pending.Result;
    }

    internal XmlDiagnostic? FindXsltResultCollision(string uri, XmlNode node)
    {
        int index = 0;
        while (index < ResultUris.Count)
        {
            if (ResultUris[index] == uri)
            {
                return CreateXsltResultDocumentDiagnostic("Two result documents use the same absolute URI.", XsltErrorCodes.XTDE1490, node);
            }

            index = index + 1;
        }

        string? principal = State.PrincipalDestination.BaseUri;
        if (principal != null && principal.Length != 0)
        {
            string canonical = State.DocumentPool.CanonicalXPathResourceKey(principal);
            if (canonical == uri)
            {
                return CreateXsltResultDocumentDiagnostic("A secondary result uses the principal result URI.", XsltErrorCodes.XTDE1490, node);
            }
        }

        index = 0;
        while (index < State.DocumentPool.DocumentUris.Count)
        {
            if (State.DocumentPool.DocumentUris[index] == uri)
            {
                return CreateXsltResultDocumentDiagnostic("A result URI was also used to read a document.", XsltErrorCodes.XTDE1500, node);
            }

            index = index + 1;
        }

        return null;
    }

    internal void EnsureXsltResultUriReadable(string uri, XmlNode node)
    {
        int index = 0;
        while (index < ResultUris.Count)
        {
            if (ResultUris[index] == uri)
            {
                throw CreateXsltResultDocumentError("A result URI cannot also be used to read a document.", XsltErrorCodes.XTDE1500, node);
            }

            index = index + 1;
        }
    }

    internal void ApplyXsltResultSerializationAttributes(XsltInstructionPlan instruction, XsltTemplateInvocation invocation, XsltSequenceConstructorRuntime runtime, XmlSerializationOptions options)
    {
        int index = 0;
        while (index < instruction.Attributes.Length)
        {
            XsltInstructionAttributePlan attribute = instruction.Attributes[index];
            string name = attribute.Name;
            if (name != "href" && name != "format" && name != "validation" && name != "type" && name != "build-tree" && name != "parameter-document")
            {
                string value = name == "use-character-maps" ? attribute.Source : runtime.EvaluateXsltNodeValueTemplate(attribute.Attribute, invocation, attribute.Source);
                ApplyXsltResultSerializationAttribute(name, value, instruction.StaticContext.Namespaces, options, attribute.Attribute);
            }

            index = index + 1;
        }
    }

    internal void ApplyXsltResultSerializationAttribute(string name, string value, XmlNamespaceBinding[] namespaces, XmlSerializationOptions options, XmlNode node)
    {
        if (name == "method") SetXsltResultOutputMethod(value, namespaces, options, node, false);
        else if (name == "json-node-output-method") SetXsltResultOutputMethod(value, namespaces, options, node, true);
        else if (name == "allow-duplicate-names") options.AllowDuplicateNames = ReadXsltResultBoolean(value, node);
        else if (name == "byte-order-mark") options.ByteOrderMark = ReadXsltResultBoolean(value, node);
        else if (name == "escape-uri-attributes") options.EscapeUriAttributes = ReadXsltResultBoolean(value, node);
        else if (name == "include-content-type") options.IncludeContentType = ReadXsltResultBoolean(value, node);
        else if (name == "indent") options.Indent = ReadXsltResultBoolean(value, node);
        else if (name == "omit-xml-declaration") options.OmitXmlDeclaration = ReadXsltResultBoolean(value, node);
        else if (name == "undeclare-prefixes") options.UndeclarePrefixes = ReadXsltResultBoolean(value, node);
        else if (name == "cdata-section-elements") options.CDataSectionElements = MergeXsltResultNames(options.CDataSectionElements, ResolveXsltResultNameList(value, namespaces, true, node));
        else if (name == "suppress-indentation") options.SuppressIndentation = MergeXsltResultNames(options.SuppressIndentation, ResolveXsltResultNameList(value, namespaces, true, node));
        else if (name == "doctype-public") options.DoctypePublic = value.Length == 0 ? null : value;
        else if (name == "doctype-system") options.DoctypeSystem = value.Length == 0 ? null : value;
        else if (name == "encoding") options.Encoding = value;
        else if (name == "html-version") options.HtmlVersion = value.Trim();
        else if (name == "item-separator") options.ItemSeparator = value == "#absent" ? null : value;
        else if (name == "media-type") options.MediaType = value;
        else if (name == "normalization-form") options.NormalizationForm = value;
        else if (name == "output-version") options.Version = value;
        else if (name == "standalone") options.Standalone = ReadXsltResultStandalone(value, node);
        else if (name == "use-character-maps") options.UseCharacterMaps = MergeXsltResultCharacterMaps(options.UseCharacterMaps, ResolveXsltResultCharacterMaps(value, namespaces, node));
    }

    internal void SetXsltResultOutputMethod(string source, XmlNamespaceBinding[] namespaces, XmlSerializationOptions options, XmlNode node, bool jsonNodeMethod)
    {
        string value = source.Trim();
        XmlSerializationMethod method = value == "xml" ? XmlSerializationMethod.Xml : value == "xhtml" ? XmlSerializationMethod.Xhtml : value == "html" ? XmlSerializationMethod.Html : value == "text" ? XmlSerializationMethod.Text : value == "json" ? XmlSerializationMethod.Json : value == "adaptive" ? XmlSerializationMethod.Adaptive : XmlSerializationMethod.Extension;
        if (jsonNodeMethod && method != XmlSerializationMethod.Xml && method != XmlSerializationMethod.Xhtml && method != XmlSerializationMethod.Html && method != XmlSerializationMethod.Text)
        {
            throw CreateXsltResultDocumentError("The json-node-output-method value is invalid.", XsltErrorCodes.XTDE0030, node);
        }

        if (method == XmlSerializationMethod.Extension)
        {
            XmlName? expanded = ResolveXsltResultEqName(value, namespaces, false);
            if (expanded == null || expanded.NamespaceUri == null || expanded.NamespaceUri.Length == 0)
            {
                throw CreateXsltResultDocumentError("An extension output method requires a namespace-qualified EQName.", XsltErrorCodes.XTDE0030, node);
            }

            options.ExtensionMethodName = expanded;
        }

        if (jsonNodeMethod) options.JsonNodeOutputMethod = method;
        else options.Method = method;
    }

    internal bool ReadXsltResultBoolean(string source, XmlNode node)
    {
        string value = source.Trim();
        if (value == "yes" || value == "true" || value == "1") return true;
        if (value == "no" || value == "false" || value == "0") return false;
        throw CreateXsltResultDocumentError("A result-document boolean attribute has an invalid value.", XsltErrorCodes.XTDE0030, node);
    }

    internal XmlStandalone ReadXsltResultStandalone(string source, XmlNode node)
    {
        string value = source.Trim();
        if (value == "yes" || value == "true" || value == "1") return XmlStandalone.Yes;
        if (value == "no" || value == "false" || value == "0") return XmlStandalone.No;
        if (value == "omit") return XmlStandalone.Unspecified;
        throw CreateXsltResultDocumentError("The standalone attribute has an invalid value.", XsltErrorCodes.XTDE0030, node);
    }

    internal XmlName[] ResolveXsltResultNameList(string source, XmlNamespaceBinding[] namespaces, bool useDefault, XmlNode node)
    {
        string[] tokens = source.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        XmlName[] result = new XmlName[tokens.Length];
        int index = 0;
        while (index < tokens.Length)
        {
            XmlName? name = ResolveXsltResultEqName(tokens[index], namespaces, useDefault);
            if (name == null)
            {
                throw CreateXsltResultDocumentError("A result-document name list contains an invalid EQName.", XsltErrorCodes.XTDE0030, node);
            }

            result[index] = name;
            index = index + 1;
        }

        return result;
    }

    internal XmlName[] MergeXsltResultNames(XmlName[]? first, XmlName[] second)
    {
        XmlName[] basis = first == null ? [] : first;
        XmlName[] result = new XmlName[basis.Length + second.Length];
        global::System.Array.Copy(basis, result, basis.Length);
        global::System.Array.Copy(second, 0, result, basis.Length, second.Length);
        return result;
    }

    internal XmlCharacterMapEntry[] ResolveXsltResultCharacterMaps(string source, XmlNamespaceBinding[] namespaces, XmlNode node)
    {
        XmlName[] names = ResolveXsltResultNameList(source, namespaces, false, node);
        List<XmlCharacterMapEntry> entries = new List<XmlCharacterMapEntry>();
        int index = 0;
        while (index < names.Length)
        {
            string expanded = CreateXsltExpandedResultName(names[index]);
            int mapIndex = 0;
            XsltCharacterMapPlan? found = null;
            while (mapIndex < State.Stylesheet.CharacterMaps.Length)
            {
                if (State.Stylesheet.CharacterMaps[mapIndex].Name == expanded) found = State.Stylesheet.CharacterMaps[mapIndex];
                mapIndex = mapIndex + 1;
            }

            if (found == null)
            {
                throw CreateXsltResultDocumentError("A result-document references an unknown character map.", XsltErrorCodes.XTDE0030, node);
            }

            entries.AddRange(found.EffectiveEntries);
            index = index + 1;
        }

        return entries.ToArray();
    }

    internal XmlCharacterMapEntry[] MergeXsltResultCharacterMaps(XmlCharacterMapEntry[]? first, XmlCharacterMapEntry[] second)
    {
        XmlCharacterMapEntry[] basis = first == null ? [] : first;
        XmlCharacterMapEntry[] result = new XmlCharacterMapEntry[basis.Length + second.Length];
        global::System.Array.Copy(basis, result, basis.Length);
        global::System.Array.Copy(second, 0, result, basis.Length, second.Length);
        return result;
    }

    internal XmlName? ResolveXsltResultEqName(string source, XmlNamespaceBinding[] namespaces, bool useDefault)
    {
        string value = source.Trim();
        string prefix = "";
        string local = value;
        string? namespaceUri = "";
        if (value.StartsWith("Q{", StringComparison.Ordinal))
        {
            int close = value.IndexOf('}');
            if (close < 2 || close == value.Length - 1) return null;
            namespaceUri = value.Substring(2, close - 2);
            local = value.Substring(close + 1);
        }
        else
        {
            int colon = value.IndexOf(':');
            if (colon >= 0)
            {
                prefix = value.Substring(0, colon);
                local = value.Substring(colon + 1);
                namespaceUri = FindXsltResultNamespace(namespaces, prefix);
            }
            else if (useDefault)
            {
                namespaceUri = FindXsltResultNamespace(namespaces, "");
            }
        }

        if (namespaceUri == null || !new XdmStringDatatype().IsValidXdmName(local, true) || prefix.Length != 0 && !new XdmStringDatatype().IsValidXdmName(prefix, true)) return null;
        XmlName result = new XmlName();
        result.Prefix = prefix;
        result.LocalName = local;
        result.NamespaceUri = namespaceUri;
        result.QualifiedName = prefix.Length == 0 ? local : prefix + ":" + local;
        return result;
    }

    internal string? FindXsltResultNamespace(XmlNamespaceBinding[] namespaces, string prefix)
    {
        int index = namespaces.Length - 1;
        while (index >= 0)
        {
            string? optionalPrefix = namespaces[index].Prefix;
            string current = optionalPrefix == null ? "" : optionalPrefix;
            if (current == prefix && !namespaces[index].IsUndeclaration) return namespaces[index].NamespaceUri == null ? "" : namespaces[index].NamespaceUri;
            index = index - 1;
        }

        return prefix.Length == 0 ? "" : null;
    }

    internal string CreateXsltExpandedResultName(XmlName name)
    {
        return "Q{" + (name.NamespaceUri == null ? "" : name.NamespaceUri) + "}" + (name.LocalName == null ? "" : name.LocalName);
    }

    internal bool IsXsltTemporaryOutputInstruction(XmlNode node)
    {
        XmlNode? current = node.Parent;
        while (current != null)
        {
            string? namespaceUri = current.Name == null ? null : current.Name.NamespaceUri;
            string? localName = current.Name == null ? null : current.Name.LocalName;
            if (namespaceUri == XsltNamespaces.Xslt && (localName == "variable" || localName == "param" || localName == "with-param" || localName == "function" || localName == "sort" || localName == "merge-key" || localName == "key" || localName == "accumulator-rule")) return true;
            current = current.Parent;
        }

        return false;
    }

    internal void PublishXsltSecondaryResults()
    {
        State.Result.SecondaryResults = Results.ToArray();
    }

    internal XmlDiagnostic CreateXsltResultDocumentDiagnostic(string message, string code, XmlNode node)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XsltDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = node.Range;
        return diagnostic;
    }

    internal XsltDynamicException CreateXsltResultDocumentError(string message, string code, XmlNode node)
    {
        XmlDiagnostic diagnostic = CreateXsltResultDocumentDiagnostic(message, code, node);
        return new XsltDynamicException(message, diagnostic, diagnostic.Range == null ? null : diagnostic.Range.Start);
    }
}
