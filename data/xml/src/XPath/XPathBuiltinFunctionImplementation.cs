using System;
using System.Globalization;

namespace xml;

internal class XPathBuiltinFunctionImplementation : IXPathFunction
{
    internal string LocalName;
    internal SourceRange Range;
    internal XPathStaticContext StaticContext;
    internal XdmSequenceOperations Sequences;
    internal XdmFactory Factory;

    internal XPathBuiltinFunctionImplementation(string localName, SourceRange range, XPathStaticContext staticContext)
    {
        LocalName = localName;
        Range = CopyXPathBuiltinRange(range);
        StaticContext = staticContext;
        Sequences = new XdmSequenceOperations();
        Factory = new XdmFactory();
    }

    public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
    {
        if (LocalName == "true")
        {
            return CreateXPathBuiltinBooleanResult(true);
        }

        if (LocalName == "false")
        {
            return CreateXPathBuiltinBooleanResult(false);
        }

        if (LocalName == "boolean" || LocalName == "not")
        {
            bool value = new XdmTypeConverter().EffectiveBooleanValueXdmSequence(arguments[0]);
            return CreateXPathBuiltinBooleanResult(LocalName == "not" ? !value : value);
        }

        if (LocalName == "position" || LocalName == "last")
        {
            int value = LocalName == "position" ? context.Position : context.Size;
            if (value <= 0)
            {
                throw CreateXPathBuiltinError("The XPath focus is absent.", XPathErrorCodes.XPDY0002);
            }

            return CreateXPathBuiltinIntegerResult(value);
        }

        if (LocalName == "current-dateTime" || LocalName == "current-date" || LocalName == "current-time")
        {
            return CreateXPathBuiltinCurrentDateTimeResult(context);
        }

        if (LocalName == "implicit-timezone")
        {
            string lexical = CreateXPathImplicitTimezoneLexical(context.ImplicitTimezoneMinutes);
            return CreateXPathBuiltinAtomicResult(Factory.CreateXdmDurationAtomicValue(lexical, XdmAtomicType.DayTimeDuration));
        }

        if (LocalName == "default-collation")
        {
            string value = StaticContext.DefaultCollation == null ? XPathNamespaces.CodepointCollation : StaticContext.DefaultCollation;
            return CreateXPathBuiltinAtomicResult(Factory.CreateXdmStringAtomicValue(value, XdmAtomicType.String));
        }

        if (LocalName == "default-language")
        {
            string? supplied = context.DefaultLanguage;
            string value = supplied == null || supplied.Length == 0 ? "en" : supplied;
            return CreateXPathBuiltinAtomicResult(Factory.CreateXdmStringAtomicValue(value, XdmAtomicType.Language));
        }

        if (LocalName == "static-base-uri")
        {
            string? value = StaticContext.BaseUri;
            return value == null ? Sequences.CreateEmptyXdmSequence() : CreateXPathBuiltinAtomicResult(Factory.CreateXdmAnyUriAtomicValue(value));
        }

        if (LocalName == "count")
        {
            return CreateXPathBuiltinIntegerResult(arguments[0].Count);
        }

        if (LocalName == "empty" || LocalName == "exists")
        {
            bool value = arguments[0].Count == 0;
            return CreateXPathBuiltinBooleanResult(LocalName == "empty" ? value : !value);
        }

        if (LocalName == "zero-or-one")
        {
            if (arguments[0].Count > 1)
            {
                throw CreateXPathBuiltinError("fn:zero-or-one requires a sequence containing at most one item.", XPathErrorCodes.FORG0003);
            }

            return Sequences.FreezeXdmSequenceStorage(arguments[0]);
        }

        if (LocalName == "one-or-more")
        {
            if (arguments[0].Count == 0)
            {
                throw CreateXPathBuiltinError("fn:one-or-more requires a non-empty sequence.", XPathErrorCodes.FORG0004);
            }

            return Sequences.FreezeXdmSequenceStorage(arguments[0]);
        }

        if (LocalName == "exactly-one")
        {
            if (arguments[0].Count != 1)
            {
                throw CreateXPathBuiltinError("fn:exactly-one requires a sequence containing exactly one item.", XPathErrorCodes.FORG0005);
            }

            return Sequences.FreezeXdmSequenceStorage(arguments[0]);
        }

        if (LocalName == "head" || LocalName == "tail")
        {
            return CreateXPathBuiltinSequencePart(arguments[0], LocalName == "head");
        }

        throw new InvalidOperationException("The XPath built-in function is not implemented.");
    }

    internal XdmSequence CreateXPathBuiltinCurrentDateTimeResult(XPathDynamicContext context)
    {
        TimeSpan offset = TimeSpan.FromMinutes(context.ImplicitTimezoneMinutes);
        DateTimeOffset value = context.CurrentDateTime.ToOffset(offset);
        string lexical;
        XdmAtomicType type;
        if (LocalName == "current-dateTime")
        {
            lexical = value.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz", CultureInfo.InvariantCulture);
            type = XdmAtomicType.DateTimeStamp;
        }
        else if (LocalName == "current-date")
        {
            lexical = value.ToString("yyyy-MM-ddzzz", CultureInfo.InvariantCulture);
            type = XdmAtomicType.Date;
        }
        else
        {
            lexical = value.ToString("HH:mm:ss.FFFFFFFzzz", CultureInfo.InvariantCulture);
            type = XdmAtomicType.Time;
        }

        return CreateXPathBuiltinAtomicResult(Factory.CreateXdmDateTimeAtomicValue(lexical, type));
    }

    internal string CreateXPathImplicitTimezoneLexical(int minutes)
    {
        int absolute = minutes < 0 ? -minutes : minutes;
        if (absolute == 0)
        {
            return "PT0S";
        }

        string lexical = minutes < 0 ? "-PT" : "PT";
        int hours = absolute / 60;
        int remainingMinutes = absolute % 60;
        if (hours > 0)
        {
            lexical = lexical + hours.ToString(CultureInfo.InvariantCulture) + "H";
        }

        if (remainingMinutes > 0)
        {
            lexical = lexical + remainingMinutes.ToString(CultureInfo.InvariantCulture) + "M";
        }

        return lexical;
    }

    internal XdmSequence CreateXPathBuiltinSequencePart(XdmSequence sequence, bool head)
    {
        XdmSequenceBuilder builder = new XdmSequenceBuilder();
        XdmSequenceCursor cursor = new XdmSequenceCursor(sequence);
        int position = 0;
        while (cursor.MoveNextXdmSequenceItem())
        {
            bool include = head ? position == 0 : position > 0;
            if (include)
            {
                AppendXPathBuiltinItem(builder, cursor.ReadCurrentXdmSequenceItem());
            }

            if (head)
            {
                break;
            }

            position = position + 1;
        }

        return builder.BuildImmutableXdmSequence();
    }

    internal void AppendXPathBuiltinItem(XdmSequenceBuilder builder, XdmSequenceItem item)
    {
        if (item.Kind == XdmItemKind.Node && item.Node != null)
        {
            builder.AddXdmNode(item.Node);
        }
        else if (item.Kind == XdmItemKind.Atomic && item.AtomicValue != null)
        {
            builder.AddXdmAtomicValue(item.AtomicValue);
        }
        else if (item.Kind == XdmItemKind.Function && item.Function != null)
        {
            builder.AddXdmFunction(item.Function);
        }
        else if (item.Kind == XdmItemKind.Map && item.Map != null)
        {
            builder.AddXdmMap(item.Map);
        }
        else if (item.Kind == XdmItemKind.Array && item.Array != null)
        {
            builder.AddXdmArray(item.Array);
        }
        else
        {
            throw new InvalidOperationException("The XPath sequence item is incomplete.");
        }
    }

    internal XdmSequence CreateXPathBuiltinBooleanResult(bool value)
    {
        return CreateXPathBuiltinAtomicResult(Factory.CreateXdmBooleanAtomicValue(value ? "true" : "false"));
    }

    internal XdmSequence CreateXPathBuiltinIntegerResult(int value)
    {
        return CreateXPathBuiltinAtomicResult(Factory.CreateXdmIntegerAtomicValue(value.ToString(CultureInfo.InvariantCulture), XdmAtomicType.Integer));
    }

    internal XdmSequence CreateXPathBuiltinAtomicResult(XdmAtomicValue value)
    {
        return Sequences.CreateXdmAtomicSequence(value);
    }

    internal XPathDynamicException CreateXPathBuiltinError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XPathDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = CopyXPathBuiltinRange(Range);
        return new XPathDynamicException(message, diagnostic, diagnostic.Range.Start);
    }

    internal SourceRange CopyXPathBuiltinRange(SourceRange source)
    {
        SourceRange result = new SourceRange();
        result.Start = CopyXPathBuiltinLocation(source.Start);
        result.End = CopyXPathBuiltinLocation(source.End);
        return result;
    }

    internal SourceLocation? CopyXPathBuiltinLocation(SourceLocation? source)
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
