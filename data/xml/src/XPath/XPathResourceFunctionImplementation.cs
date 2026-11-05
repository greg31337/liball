using System;
using System.Threading.Tasks;

namespace xml;

internal class XPathResourceUriResult
{
    internal bool Success;
    internal string? Uri;
    internal bool MissingBaseUri;

    internal XPathResourceUriResult(bool success, string? uri, bool missingBaseUri)
    {
        Success = success;
        Uri = uri;
        MissingBaseUri = missingBaseUri;
    }
}

internal class XPathResourceFunctionImplementation : IXPathFunction
{
    internal string LocalName;
    internal SourceRange Range;
    internal XPathStaticContext StaticContext;
    internal XPathRuntime Runtime;
    internal XdmSequenceOperations Sequences;
    internal XdmTypeConverter Converter;
    internal XdmFactory Factory;

    internal XPathResourceFunctionImplementation(string localName, SourceRange range, XPathStaticContext staticContext, XPathRuntime runtime)
    {
        LocalName = localName;
        Range = CopyXPathResourceFunctionRange(range);
        StaticContext = staticContext;
        Runtime = runtime;
        Sequences = new XdmSequenceOperations();
        Converter = new XdmTypeConverter();
        Factory = new XdmFactory();
    }

    public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
    {
        if (LocalName == "doc" || LocalName == "doc-available")
        {
            return EvaluateXPathDocumentFunction(arguments, context, LocalName == "doc-available");
        }

        if (LocalName == "collection" || LocalName == "uri-collection")
        {
            return EvaluateXPathCollectionFunction(arguments, context, LocalName == "uri-collection");
        }

        return EvaluateXPathUnparsedTextFunction(arguments, context);
    }

    internal XdmSequence EvaluateXPathDocumentFunction(XdmSequence[] arguments, XPathDynamicContext context, bool available)
    {
        string? supplied = ReadXPathOptionalResourceString(arguments[0]);
        if (supplied == null)
        {
            return available ? CreateXPathResourceBoolean(false) : Sequences.CreateEmptyXdmSequence();
        }

        XPathResourceUriResult resolved = ResolveXPathResourceUri(supplied, StaticContext.BaseUri);
        if (!resolved.Success || resolved.Uri == null)
        {
            if (available)
            {
                return CreateXPathResourceBoolean(false);
            }

            string code = resolved.MissingBaseUri ? XPathErrorCodes.FODC0002 : XPathErrorCodes.FODC0005;
            throw CreateXPathResourceFunctionError("The document URI is invalid or cannot be resolved against the static base URI.", code);
        }

        string absolute = resolved.Uri;
        Task<XmlDocument> pending = Task.Run(() => Runtime.Resources.ResolveXPathDocumentResource(absolute));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion || pending.Result.DocumentNode == null)
        {
            if (available)
            {
                return CreateXPathResourceBoolean(false);
            }

            ThrowXPathResolverFailure(pending.Exception);
            throw CreateXPathResourceFunctionError("The requested document resource is unavailable.", XPathErrorCodes.FODC0002);
        }

        return available ? CreateXPathResourceBoolean(true) : Sequences.CreateXdmNodeSequence(pending.Result.DocumentNode);
    }

    internal XdmSequence EvaluateXPathCollectionFunction(XdmSequence[] arguments, XPathDynamicContext context, bool uriCollection)
    {
        string? supplied = arguments.Length == 0 ? null : ReadXPathOptionalResourceString(arguments[0]);
        string? requested = supplied == null ? context.DefaultCollectionUri : supplied;
        string? absolute = null;
        if (requested != null)
        {
            XPathResourceUriResult resolved = ResolveXPathResourceUri(requested, StaticContext.BaseUri);
            if (!resolved.Success || resolved.Uri == null)
            {
                string code = resolved.MissingBaseUri ? XPathErrorCodes.FODC0002 : XPathErrorCodes.FODC0004;
                throw CreateXPathResourceFunctionError("The collection URI is invalid or cannot be resolved against the static base URI.", code);
            }

            absolute = resolved.Uri;
        }

        if (!uriCollection)
        {
            Task<XdmSequence> pending = Task.Run(() => Runtime.Resources.ResolveXPathCollectionResource(absolute));
            ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
            if (pending.Status != TaskStatus.RanToCompletion)
            {
                ThrowXPathResolverFailure(pending.Exception);
                throw CreateXPathResourceFunctionError("The requested collection is unavailable.", XPathErrorCodes.FODC0002);
            }

            return pending.Result;
        }

        Task<string[]> uriPending = Task.Run(() => Runtime.Resources.ResolveXPathUriCollectionResource(absolute));
        ((IAsyncResult)uriPending).AsyncWaitHandle.WaitOne();
        if (uriPending.Status != TaskStatus.RanToCompletion)
        {
            ThrowXPathResolverFailure(uriPending.Exception);
            throw CreateXPathResourceFunctionError("The requested URI collection is unavailable.", XPathErrorCodes.FODC0002);
        }

        XdmSequenceBuilder builder = new XdmSequenceBuilder();
        string[] uris = uriPending.Result;
        int index = 0;
        while (index < uris.Length)
        {
            builder.AddXdmAtomicValue(Factory.CreateXdmAnyUriAtomicValue(uris[index]));
            index = index + 1;
        }

        return builder.BuildImmutableXdmSequence();
    }

    internal void ThrowXPathResolverFailure(AggregateException? aggregate)
    {
        Exception? failure = aggregate;
        while (failure != null && failure.GetType().FullName == "System.AggregateException")
        {
            AggregateException current = (AggregateException)failure;
            failure = current.InnerExceptions.Count == 0 ? null : current.InnerExceptions[0];
        }

        if (failure != null && failure.GetType().FullName == "xml.XmlLimitException")
        {
            throw (XmlLimitException)failure;
        }

        if (failure != null && failure.GetType().FullName == "xml.XPathDynamicException")
        {
            throw (XPathDynamicException)failure;
        }
    }

    internal XdmSequence EvaluateXPathUnparsedTextFunction(XdmSequence[] arguments, XPathDynamicContext context)
    {
        string? supplied = ReadXPathOptionalResourceString(arguments[0]);
        bool available = LocalName == "unparsed-text-available";
        if (supplied == null)
        {
            return available ? CreateXPathResourceBoolean(false) : Sequences.CreateEmptyXdmSequence();
        }

        string? encoding = arguments.Length == 2 ? ReadXPathRequiredResourceString(arguments[1]) : null;
        XPathResourceUriResult resolved = ResolveXPathResourceUri(supplied, StaticContext.BaseUri);
        if (!resolved.Success || resolved.Uri == null || UriTools.HasFragment(resolved.Uri))
        {
            if (available)
            {
                return CreateXPathResourceBoolean(false);
            }

            throw CreateXPathResourceFunctionError("The unparsed text URI is invalid or contains a fragment identifier.", XPathErrorCodes.FOUT1170);
        }

        XPathUnparsedTextResult loaded = Runtime.Resources.ResolveXPathUnparsedTextResource(resolved.Uri, encoding);
        if (!loaded.Success || loaded.Text == null)
        {
            if (available)
            {
                return CreateXPathResourceBoolean(false);
            }

            string code = loaded.ErrorCode == null ? XPathErrorCodes.FOUT1170 : loaded.ErrorCode;
            string message = loaded.ErrorMessage == null ? "The unparsed text resource is unavailable." : loaded.ErrorMessage;
            throw CreateXPathResourceFunctionError(message, code);
        }

        if (available)
        {
            return CreateXPathResourceBoolean(true);
        }

        if (LocalName == "unparsed-text-lines")
        {
            return CreateXPathUnparsedTextLines(loaded.Text);
        }

        XdmAtomicValue value = Factory.CreateXdmStringAtomicValue(loaded.Text, XdmAtomicType.String);
        return Sequences.CreateXdmAtomicSequence(value);
    }

    internal XdmSequence CreateXPathUnparsedTextLines(string text)
    {
        XdmSequenceBuilder builder = new XdmSequenceBuilder();
        int start = 0;
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == '\n' || text[index] == '\r')
            {
                builder.AddXdmAtomicValue(Factory.CreateXdmStringAtomicValue(text.Substring(start, index - start), XdmAtomicType.String));
                if (text[index] == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
                {
                    index = index + 1;
                }

                start = index + 1;
            }

            index = index + 1;
        }

        if (start < text.Length)
        {
            builder.AddXdmAtomicValue(Factory.CreateXdmStringAtomicValue(text.Substring(start), XdmAtomicType.String));
        }

        return builder.BuildImmutableXdmSequence();
    }

    internal string? ReadXPathOptionalResourceString(XdmSequence supplied)
    {
        XdmSequenceType expected = new XdmSequenceType();
        expected.ItemKind = XdmItemKind.Atomic;
        expected.AtomicType = XdmAtomicType.String;
        expected.Occurrence = XdmOccurrence.ZeroOrOne;
        XdmSequence converted = Converter.ApplyXdmFunctionConversion(supplied, expected, StaticContext);
        if (converted.Count == 0)
        {
            return null;
        }

        XdmAtomicValue? value = Sequences.ReadXdmSequenceItem(converted, 0).AtomicValue;
        return value == null ? null : Converter.ReadXdmAtomicStringValue(value);
    }

    internal string ReadXPathRequiredResourceString(XdmSequence supplied)
    {
        XdmSequenceType expected = new XdmSequenceType();
        expected.ItemKind = XdmItemKind.Atomic;
        expected.AtomicType = XdmAtomicType.String;
        expected.Occurrence = XdmOccurrence.ExactlyOne;
        XdmSequence converted = Converter.ApplyXdmFunctionConversion(supplied, expected, StaticContext);
        XdmAtomicValue? value = Sequences.ReadXdmSequenceItem(converted, 0).AtomicValue;
        if (value == null)
        {
            throw CreateXPathResourceFunctionError("A required resource argument is absent.", XPathErrorCodes.XPTY0004);
        }

        return Converter.ReadXdmAtomicStringValue(value);
    }

    internal XPathResourceUriResult ResolveXPathResourceUri(string reference, string? baseUri)
    {
        Task<XPathResourceUriResult> pending = Task.Run(() => ResolveXPathResourceUriCore(reference, baseUri));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        return pending.Status == TaskStatus.RanToCompletion ? pending.Result : new XPathResourceUriResult(false, null, false);
    }

    internal XPathResourceUriResult ResolveXPathResourceUriCore(string reference, string? baseUri)
    {
        int colon = reference.IndexOf(':');
        int slash = reference.IndexOf('/');
        int question = reference.IndexOf('?');
        int hash = reference.IndexOf('#');
        int firstDelimiter = reference.Length;
        if (slash >= 0 && slash < firstDelimiter)
        {
            firstDelimiter = slash;
        }

        if (question >= 0 && question < firstDelimiter)
        {
            firstDelimiter = question;
        }

        if (hash >= 0 && hash < firstDelimiter)
        {
            firstDelimiter = hash;
        }

        if (colon >= 0 && colon < firstDelimiter && !IsValidXPathUriScheme(reference, colon))
        {
            return new XPathResourceUriResult(false, null, false);
        }

        Uri value = UriTools.CreateReference(reference);
        if (!value.IsAbsoluteUri)
        {
            if (baseUri == null || baseUri.Length == 0)
            {
                return new XPathResourceUriResult(false, null, true);
            }

            value = UriTools.ResolveReference(UriTools.CreateAbsolute(baseUri), reference);
        }

        return new XPathResourceUriResult(true, UriTools.CanonicalAbsoluteUri(value), false);
    }

    internal bool IsValidXPathUriScheme(string reference, int colon)
    {
        if (colon == 0 || !IsXPathUriSchemeLetter(reference[0]))
        {
            return false;
        }

        int index = 1;
        while (index < colon)
        {
            char character = reference[index];
            if (!IsXPathUriSchemeLetter(character) && (character < '0' || character > '9') && character != '+' && character != '-' && character != '.')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool IsXPathUriSchemeLetter(char character)
    {
        return character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z';
    }

    internal XdmSequence CreateXPathResourceBoolean(bool value)
    {
        XdmAtomicValue result = Factory.CreateXdmBooleanAtomicValue(value ? "true" : "false");
        return Sequences.CreateXdmAtomicSequence(result);
    }

    internal XPathDynamicException CreateXPathResourceFunctionError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XPathDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = CopyXPathResourceFunctionRange(Range);
        return new XPathDynamicException(message, diagnostic, diagnostic.Range.Start);
    }

    internal SourceRange CopyXPathResourceFunctionRange(SourceRange source)
    {
        SourceRange result = new SourceRange();
        result.Start = CopyXPathResourceFunctionLocation(source.Start);
        result.End = CopyXPathResourceFunctionLocation(source.End);
        return result;
    }

    internal SourceLocation? CopyXPathResourceFunctionLocation(SourceLocation? source)
    {
        if (source == null)
        {
            return null;
        }

        SourceLocation result = new SourceLocation();
        result.SourceUri = source.SourceUri;
        result.CharacterOffset = source.CharacterOffset;
        result.ByteOffset = source.ByteOffset;
        result.Line = source.Line;
        result.Column = source.Column;
        return result;
    }
}
