namespace xml;

internal class XPathJsonOptions
{
    internal bool Liberal;
    internal string Duplicates;
    internal bool DuplicatesSpecified;
    internal bool Escape;
    internal XdmFunctionItem? Fallback;
    internal XdmFunctionItem? NumberParser;
    internal bool Validate;
    internal bool Indent;
    internal bool XmlOutput;

    internal XPathJsonOptions()
    {
        Duplicates = "use-first";
        Escape = false;
    }
}

internal class XPathJsonFunctionImplementation : IXPathFunction
{
    internal string LocalName;
    internal SourceRange Range;
    internal XPathStaticContext StaticContext;
    internal XPathRuntime Runtime;
    internal XdmSequenceOperations Sequences;
    internal XdmMapOperations Maps;
    internal XdmFunctionOperations Functions;
    internal XdmTypeConverter Converter;

    internal XPathJsonFunctionImplementation(string localName, SourceRange range, XPathStaticContext staticContext, XPathRuntime runtime)
    {
        LocalName = localName;
        Range = range;
        StaticContext = staticContext;
        Runtime = runtime;
        Sequences = new XdmSequenceOperations();
        Maps = new XdmMapOperations();
        Functions = new XdmFunctionOperations();
        Converter = new XdmTypeConverter();
    }

    public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
    {
        if (LocalName == "xml-to-json")
        {
            XPathJsonOptions serializationOptions = arguments.Length == 2 ? ReadXPathJsonOptions(arguments[1]) : new XPathJsonOptions();
            return new XPathJsonXmlConverter(Range).ConvertXPathXmlToJson(arguments[0], serializationOptions);
        }

        string? input = ReadXPathJsonOptionalString(arguments[0]);
        if (input == null)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        XPathJsonOptions options = arguments.Length == 2 ? ReadXPathJsonOptions(arguments[1]) : new XPathJsonOptions();
        if (LocalName == "json-to-xml")
        {
            options.XmlOutput = true;
            if (!options.DuplicatesSpecified)
            {
                options.Duplicates = options.Validate ? "reject" : "retain";
            }

            if (options.Validate && options.Duplicates == "retain")
            {
                throw CreateXPathJsonError("The retain duplicate policy cannot be used with validation.", XPathErrorCodes.FOJS0005);
            }
        }

        if (options.Escape && options.Fallback != null)
        {
            throw CreateXPathJsonError("The fallback option cannot be used when escape is true.", XPathErrorCodes.FOJS0005);
        }

        if (LocalName == "json-doc")
        {
            input = ReadXPathJsonDocument(input);
        }

        return new XPathJsonParser(input, options, context, StaticContext, Range).ParseXPathJsonText();
    }

    internal string? ReadXPathJsonOptionalString(XdmSequence supplied)
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

    internal XPathJsonOptions ReadXPathJsonOptions(XdmSequence supplied)
    {
        if (supplied.Count != 1)
        {
            throw CreateXPathJsonTypeError("JSON options must be supplied as one map.");
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(supplied, 0);
        if (item.Kind != XdmItemKind.Map || item.Map == null)
        {
            throw CreateXPathJsonTypeError("JSON options must be supplied as one map.");
        }

        XPathJsonOptions result = new XPathJsonOptions();
        Maps.ValidateXdmMapStorage(item.Map);
        XdmMapEntry[] entries = item.Map.Entries == null ? [] : item.Map.Entries;
        int index = 0;
        while (index < entries.Length)
        {
            XdmMapEntry entry = entries[index];
            XdmAtomicValue? key = entry.Key;
            XdmSequence? value = entry.Value;
            if (key != null && value != null && Maps.IsXdmStringMapKey(key.Type))
            {
                string name = Converter.ReadXdmAtomicStringValue(key);
                if (name == "liberal" && LocalName != "xml-to-json")
                {
                    result.Liberal = ReadXPathJsonBooleanOption(value, name);
                }
                else if (name == "duplicates" && LocalName != "xml-to-json")
                {
                    result.Duplicates = ReadXPathJsonStringOption(value, name);
                    result.DuplicatesSpecified = true;
                    bool valid = LocalName == "json-to-xml" ? result.Duplicates == "reject" || result.Duplicates == "use-first" || result.Duplicates == "retain" : result.Duplicates == "reject" || result.Duplicates == "use-first" || result.Duplicates == "use-last";
                    if (!valid)
                    {
                        throw CreateXPathJsonError("The duplicates option has an unsupported value.", XPathErrorCodes.FOJS0005);
                    }
                }
                else if (name == "escape" && LocalName != "xml-to-json")
                {
                    result.Escape = ReadXPathJsonBooleanOption(value, name);
                }
                else if (name == "fallback" && LocalName != "xml-to-json")
                {
                    result.Fallback = ReadXPathJsonFunctionOption(value, name);
                }
                else if (name == "number-parser")
                {
                    if (LocalName == "parse-json" || LocalName == "json-doc")
                    {
                        result.NumberParser = ReadXPathJsonFunctionOption(value, name);
                    }
                }
                else if (name == "validate" && LocalName == "json-to-xml")
                {
                    result.Validate = ReadXPathJsonBooleanOption(value, name);
                }
                else if (name == "indent" && LocalName == "xml-to-json")
                {
                    result.Indent = ReadXPathJsonBooleanOption(value, name);
                }
            }

            index = index + 1;
        }

        return result;
    }

    internal bool ReadXPathJsonBooleanOption(XdmSequence supplied, string name)
    {
        XdmSequenceType expected = new XdmSequenceType();
        expected.ItemKind = XdmItemKind.Atomic;
        expected.AtomicType = XdmAtomicType.Boolean;
        expected.Occurrence = XdmOccurrence.ExactlyOne;
        XdmSequence converted = Converter.ApplyXdmFunctionConversion(supplied, expected, StaticContext);
        XdmAtomicValue? value = Sequences.ReadXdmSequenceItem(converted, 0).AtomicValue;
        return value == null ? throw CreateXPathJsonTypeError("The " + name + " option must contain one boolean.") : value.BooleanValue;
    }

    internal string ReadXPathJsonStringOption(XdmSequence supplied, string name)
    {
        XdmSequenceType expected = new XdmSequenceType();
        expected.ItemKind = XdmItemKind.Atomic;
        expected.AtomicType = XdmAtomicType.String;
        expected.Occurrence = XdmOccurrence.ExactlyOne;
        XdmSequence converted = Converter.ApplyXdmFunctionConversion(supplied, expected, StaticContext);
        XdmAtomicValue? value = Sequences.ReadXdmSequenceItem(converted, 0).AtomicValue;
        return value == null ? throw CreateXPathJsonTypeError("The " + name + " option must contain one string.") : Converter.ReadXdmAtomicStringValue(value);
    }

    internal XdmFunctionItem ReadXPathJsonFunctionOption(XdmSequence supplied, string name)
    {
        if (supplied.Count != 1)
        {
            throw CreateXPathJsonTypeError("The " + name + " option must contain one unary function.");
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(supplied, 0);
        XdmFunctionItem? result = null;
        if (item.Kind == XdmItemKind.Function)
        {
            result = item.Function;
        }
        else if (item.Kind == XdmItemKind.Map && item.Map != null)
        {
            result = Functions.CreateXdmMapFunctionItem(item.Map);
        }
        else if (item.Kind == XdmItemKind.Array && item.Array != null)
        {
            result = Functions.CreateXdmArrayFunctionItem(item.Array);
        }

        if (result == null || result.Arity != 1)
        {
            throw CreateXPathJsonTypeError("The " + name + " option must contain one unary function.");
        }

        return result;
    }

    internal string ReadXPathJsonDocument(string reference)
    {
        XPathResourceFunctionImplementation resources = new XPathResourceFunctionImplementation("unparsed-text", Range, StaticContext, Runtime);
        XPathResourceUriResult resolved = resources.ResolveXPathResourceUri(reference, StaticContext.BaseUri);
        if (!resolved.Success || resolved.Uri == null || UriTools.HasFragment(resolved.Uri))
        {
            throw resources.CreateXPathResourceFunctionError("The JSON document URI is invalid or contains a fragment identifier.", XPathErrorCodes.FOUT1170);
        }

        XPathUnparsedTextResult loaded = Runtime.Resources.ResolveXPathUnparsedTextResource(resolved.Uri, null);
        if (!loaded.Success || loaded.Text == null)
        {
            string code = loaded.ErrorCode == null ? XPathErrorCodes.FOUT1170 : loaded.ErrorCode;
            string message = loaded.ErrorMessage == null ? "The JSON document resource is unavailable." : loaded.ErrorMessage;
            throw resources.CreateXPathResourceFunctionError(message, code);
        }

        return loaded.Text;
    }

    internal XPathDynamicException CreateXPathJsonError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XPathDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = Range;
        return new XPathDynamicException(message, diagnostic, Range.Start);
    }

    internal XPathTypeException CreateXPathJsonTypeError(string message)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XPathErrorCodes.XPTY0004;
        diagnostic.Category = XmlErrorCategory.XPathType;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = Range;
        return new XPathTypeException(message, diagnostic, Range.Start);
    }
}
