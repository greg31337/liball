using System;
using System.Globalization;
using System.IO;
using System.Threading;

namespace html;

internal class HtmlSerializationWriter
{
    internal TextWriter? writer;
    internal HtmlSerializerOptions options;
    internal CancellationToken cancellationToken;
    internal HtmlBudget budget;
    internal HtmlLimits entityLimits;
    internal int depth;
    internal bool countOnly;
    internal bool limitExceeded;
    internal bool suppressLimitException;

    internal HtmlSerializationWriter(TextWriter? writerValue,
        HtmlSerializerOptions optionsValue,
        CancellationToken cancellationTokenValue, HtmlBudget? budgetValue,
        bool countOnlyValue)
    {
        this.writer = ValidateWriter(writerValue, countOnlyValue);
        this.options = CopyOptions(optionsValue);
        this.cancellationToken = cancellationTokenValue;
        this.budget = CreateBudget(this.options, cancellationTokenValue,
            budgetValue);
        this.entityLimits = CreateEntityLimits();
        this.countOnly = countOnlyValue;
    }

    internal static HtmlSerializationWriter Create(TextWriter writer,
        HtmlSerializerOptions options, CancellationToken cancellationToken)
    {
        return new HtmlSerializationWriter(writer, options,
            cancellationToken, null, false);
    }

    internal static HtmlSerializationWriter Create(TextWriter writer,
        HtmlSerializerOptions options, CancellationToken cancellationToken,
        HtmlBudget budget)
    {
        return new HtmlSerializationWriter(writer, options,
            cancellationToken, budget, false);
    }

    internal static HtmlSerializationWriter CreateCounter(
        HtmlSerializerOptions options, CancellationToken cancellationToken)
    {
        return new HtmlSerializationWriter(null, options,
            cancellationToken, null, true);
    }

    internal static HtmlSerializationWriter CreateCounter(
        HtmlSerializerOptions options, CancellationToken cancellationToken,
        HtmlBudget budget)
    {
        return new HtmlSerializationWriter(null, options,
            cancellationToken, budget, true);
    }

    internal static TextWriter? ValidateWriter(TextWriter? writer,
        bool countOnly)
    {
        if (writer == null && !countOnly)
        {
            throw new ArgumentNullException("writer");
        }

        return writer;
    }

    internal static HtmlSerializerOptions CopyOptions(
        HtmlSerializerOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.Validate();
        return options.Copy();
    }

    internal static HtmlBudget CreateBudget(HtmlSerializerOptions options,
        CancellationToken cancellationToken, HtmlBudget? budget)
    {
        return budget == null
            ? new HtmlBudget(options.GetLimits(), cancellationToken)
            : budget;
    }

    internal static HtmlLimits CreateEntityLimits()
    {
        HtmlLimits result = HtmlLimits.CreateDefault();
        result.SetMaximumInputCharacters(int.MaxValue);
        result.SetMaximumOutputCharacters(int.MaxValue);
        return result;
    }

    internal bool WasLimitExceeded()
    {
        return this.limitExceeded;
    }

    internal int GetCharactersWritten()
    {
        return this.budget.GetUsed(HtmlLimitKind.OutputCharacters);
    }

    internal void SetSuppressLimitException()
    {
        this.suppressLimitException = true;
    }

    internal void WriteOuter(HtmlNode? node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        this.cancellationToken.ThrowIfCancellationRequested();
        HtmlNodeKind kind = node.GetKindStorage();
        switch (kind)
        {
            case HtmlNodeKind.Document:
            case HtmlNodeKind.Fragment:
                this.WriteChildren(node);
                break;
            case HtmlNodeKind.DocumentType:
                this.WriteDocumentType(node);
                break;
            case HtmlNodeKind.Element:
                this.WriteElement(node);
                break;
            case HtmlNodeKind.Text:
                this.WriteText(node);
                break;
            case HtmlNodeKind.Comment:
                this.WriteString("<!--");
                this.WriteRaw(node.GetDataStorage());
                this.WriteString("-->");
                break;
            default:
                throw new InvalidOperationException("Unknown stored node kind.");
        }
    }

    internal void WriteInner(HtmlNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        this.cancellationToken.ThrowIfCancellationRequested();
        this.WriteChildren(node);
    }

    internal void WriteElement(HtmlNode node)
    {
        this.cancellationToken.ThrowIfCancellationRequested();
        string name = this.GetElementName(node);
        bool omitStart = this.options.GetOmitOptionalTags()
            && HtmlOptionalTags.CanOmitStartTag(node);
        if (!omitStart)
        {
            this.Write('<');
            this.WriteString(name);
            HtmlAttribute[] attributes = node.GetAttributesStorage();
            if (this.options.GetSortAttributes())
            {
                this.SortAttributes(node, attributes);
            }

            int attributeIndex = 0;
            while (attributeIndex < attributes.Length)
            {
                this.cancellationToken.ThrowIfCancellationRequested();
                HtmlAttribute attribute = attributes[attributeIndex];
                this.Write(' ');
                this.WriteString(this.GetAttributeName(node, attribute));
                if (!IsMinimizedBooleanAttribute(node, attribute))
                {
                    this.WriteAttributeValue(attribute.GetValue());
                }

                attributeIndex = attributeIndex + 1;
            }

            if (this.ShouldSelfClose(node))
            {
                this.WriteString(" />");
                return;
            }

            this.Write('>');
        }

        if (SerializesAsVoid(node))
        {
            return;
        }

        this.WriteChildren(node);
        if (!this.options.GetOmitOptionalTags()
            || !HtmlOptionalTags.CanOmitEndTag(node))
        {
            this.WriteString("</");
            this.WriteString(name);
            this.Write('>');
        }
    }

    internal void WriteChildren(HtmlNode node)
    {
        this.cancellationToken.ThrowIfCancellationRequested();
        if (SerializesAsVoid(node))
        {
            return;
        }

        HtmlNode container = node;
        HtmlNode? templateContent = node.GetTemplateContentStorage();
        if (node.GetKindStorage() == HtmlNodeKind.Element
            && templateContent != null)
        {
            container = templateContent;
        }

        HtmlNode[] children = container.GetChildrenStorage();
        bool pretty = this.options.GetPretty()
            && !this.options.GetOmitOptionalTags()
            && HtmlPrettyPrinter.ShouldFormatChildren(node, children);
        bool elementContainer = node.GetKindStorage() == HtmlNodeKind.Element;
        int parentDepth = this.depth;
        int childDepth = elementContainer ? parentDepth + 1 : parentDepth;
        int index = 0;
        while (index < children.Length)
        {
            if (pretty && (elementContainer || index > 0))
            {
                this.WriteString(this.options.GetNewLine());
            }

            if (pretty)
            {
                this.WriteIndentation(childDepth);
            }

            this.depth = childDepth;
            this.WriteOuter(children[index]);
            this.depth = parentDepth;
            index = index + 1;
        }

        if (pretty && elementContainer)
        {
            this.WriteString(this.options.GetNewLine());
            this.WriteIndentation(parentDepth);
        }
    }

    internal void WriteIndentation(int count)
    {
        string indentation = this.options.GetIndentation();
        int index = 0;
        while (index < count)
        {
            this.WriteString(indentation);
            index = index + 1;
        }
    }

    internal void WriteDocumentType(HtmlNode node)
    {
        this.cancellationToken.ThrowIfCancellationRequested();
        this.WriteString("<!DOCTYPE");
        if (node.HasDoctypeNameStorage())
        {
            this.Write(' ');
            string name = node.GetNameStorage();
            this.WriteString(this.options.GetNameMode()
                == HtmlSerializerNameMode.Canonical
                ? FoldAscii(name) : name);
        }

        this.Write('>');
    }

    internal void WriteText(HtmlNode node)
    {
        this.cancellationToken.ThrowIfCancellationRequested();
        if (HasRawTextParent(node))
        {
            this.WriteRaw(node.GetDataStorage());
            return;
        }

        this.WriteEscapedText(node.GetDataStorage());
    }

    internal void WriteEscapedText(string value)
    {
        this.cancellationToken.ThrowIfCancellationRequested();
        HtmlEntityEncoding encoding = this.options.GetAsciiOnly()
            ? HtmlEntityEncoding.AsciiOnly : HtmlEntityEncoding.Minimal;
        HtmlEntityResult result = HtmlEntities.EncodeText(value, encoding,
            this.entityLimits, this.cancellationToken);
        this.cancellationToken.ThrowIfCancellationRequested();
        this.WriteString(result.GetString());
    }

    internal void WriteAttributeValue(string value)
    {
        this.cancellationToken.ThrowIfCancellationRequested();
        HtmlEntityEncoding encoding = this.options.GetAsciiOnly()
            ? HtmlEntityEncoding.AsciiOnly : HtmlEntityEncoding.Minimal;
        HtmlEntityResult result = HtmlEntities.EncodeText(value, encoding,
            this.entityLimits, this.cancellationToken);
        string escaped = result.GetString();
        char quote = this.GetQuote(escaped);
        string quoted = quote == '"'
            ? escaped.Replace("\"", "&quot;")
            : escaped.Replace("'", "&#x27;");
        this.Write('=');
        this.Write(quote);
        this.WriteString(quoted);
        this.Write(quote);
    }

    internal void WriteRaw(string value)
    {
        if (!this.options.GetAsciiOnly())
        {
            this.WriteString(value);
            return;
        }

        int index = 0;
        while (index < value.Length)
        {
            this.cancellationToken.ThrowIfCancellationRequested();
            char current = value[index];
            int scalar = current;
            int length = 1;
            if (UnicodeTools.IsHighSurrogate(current)
                && index + 1 < value.Length
                && UnicodeTools.IsLowSurrogate(value[index + 1]))
            {
                scalar = UnicodeTools.CombineSurrogates(current,
                    value[index + 1]);
                length = 2;
            }
            else if (UnicodeTools.IsHighSurrogate(current)
                || UnicodeTools.IsLowSurrogate(current))
            {
                scalar = UnicodeTools.ReplacementCharacter;
            }

            if (scalar <= 127)
            {
                this.Write(current);
            }
            else
            {
                this.WriteString("&#x" + scalar.ToString("X",
                    CultureInfo.InvariantCulture) + ";");
            }

            index = index + length;
        }
    }

    internal char GetQuote(string value)
    {
        HtmlSerializerQuoteMode mode = this.options.GetQuoteMode();
        if (mode == HtmlSerializerQuoteMode.Double)
        {
            return '"';
        }

        if (mode == HtmlSerializerQuoteMode.Single)
        {
            return '\'';
        }

        int doubles = Count(value, '"');
        int singles = Count(value, '\'');
        return singles < doubles ? '\'' : '"';
    }

    internal string GetElementName(HtmlNode node)
    {
        HtmlNamespace namespaceKind = node.GetNamespaceStorage();
        string name;
        if (namespaceKind == HtmlNamespace.Html
            || namespaceKind == HtmlNamespace.Svg
            || namespaceKind == HtmlNamespace.MathMl)
        {
            name = node.GetLocalNameStorage();
        }
        else
        {
            name = node.GetNameStorage();
        }

        return this.options.GetNameMode() == HtmlSerializerNameMode.Canonical
            && namespaceKind == HtmlNamespace.Html ? FoldAscii(name) : name;
    }

    internal string GetAttributeName(HtmlNode element, HtmlAttribute attribute)
    {
        string namespaceUri = attribute.GetNamespaceUri();
        string localName = attribute.GetLocalName();
        if (namespaceUri.Length == 0)
        {
            return this.options.GetNameMode()
                    == HtmlSerializerNameMode.Canonical
                    && element.GetNamespaceStorage() == HtmlNamespace.Html
                ? FoldAscii(localName) : localName;
        }

        if (namespaceUri == HtmlNamespaceData.GetUri(HtmlNamespace.Xml))
        {
            return "xml:" + localName;
        }

        if (namespaceUri == HtmlNamespaceData.GetUri(HtmlNamespace.Xmlns))
        {
            return localName == "xmlns" ? "xmlns" : "xmlns:" + localName;
        }

        if (namespaceUri == HtmlNamespaceData.GetUri(HtmlNamespace.XLink))
        {
            return "xlink:" + localName;
        }

        return attribute.GetName();
    }

    internal void SortAttributes(HtmlNode element, HtmlAttribute[] attributes)
    {
        int index = 1;
        while (index < attributes.Length)
        {
            HtmlAttribute value = attributes[index];
            int insertion = index;
            while (insertion > 0
                && this.CompareAttributes(element,
                    attributes[insertion - 1], value) > 0)
            {
                attributes[insertion] = attributes[insertion - 1];
                insertion = insertion - 1;
            }

            attributes[insertion] = value;
            index = index + 1;
        }
    }

    internal int CompareAttributes(HtmlNode element, HtmlAttribute left,
        HtmlAttribute right)
    {
        int comparison = string.CompareOrdinal(
            this.GetAttributeName(element, left),
            this.GetAttributeName(element, right));
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = string.CompareOrdinal(left.GetNamespaceUri(),
            right.GetNamespaceUri());
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = string.CompareOrdinal(left.GetLocalName(),
            right.GetLocalName());
        return comparison != 0 ? comparison
            : string.CompareOrdinal(left.GetValue(), right.GetValue());
    }

    internal bool ShouldSelfClose(HtmlNode node)
    {
        return this.options.GetSelfCloseForeignElements()
            && node.GetNamespaceStorage() != HtmlNamespace.Html
            && node.GetChildrenStorage().Length == 0;
    }

    internal void Write(char value)
    {
        this.cancellationToken.ThrowIfCancellationRequested();
        if (this.limitExceeded)
        {
            return;
        }

        if (!this.budget.TryConsume(HtmlLimitKind.OutputCharacters, 1))
        {
            if (this.countOnly || this.suppressLimitException)
            {
                this.limitExceeded = true;
                return;
            }

            throw this.CreateLimitException();
        }

        if (!this.countOnly)
        {
            this.GetWriter().Write(value);
        }
    }

    internal void WriteString(string value)
    {
        this.cancellationToken.ThrowIfCancellationRequested();
        if (this.limitExceeded)
        {
            return;
        }

        if (!this.budget.TryConsume(HtmlLimitKind.OutputCharacters,
            value.Length))
        {
            if (this.countOnly || this.suppressLimitException)
            {
                this.limitExceeded = true;
                return;
            }

            throw this.CreateLimitException();
        }

        if (!this.countOnly)
        {
            this.GetWriter().Write(value);
        }
    }

    internal HtmlSerializationLimitException CreateLimitException()
    {
        HtmlLimits limits = this.options.GetLimits();
        return new HtmlSerializationLimitException(
            limits.GetMaximumOutputCharacters());
    }

    internal TextWriter GetWriter()
    {
        TextWriter? result = this.writer;
        if (result == null)
        {
            throw new InvalidOperationException(
                "A non-counting serializer requires a writer.");
        }

        return result;
    }

    internal static int Count(string value, char expected)
    {
        int count = 0;
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == expected)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal static string FoldAscii(string value)
    {
        char[] result = new char[value.Length];
        int index = 0;
        while (index < value.Length)
        {
            result[index] = Ascii.ToLower(value[index]);
            index = index + 1;
        }

        return new string(result);
    }

    internal static bool IsMinimizedBooleanAttribute(HtmlNode element,
        HtmlAttribute attribute)
    {
        string name = FoldAscii(attribute.GetLocalName());
        return element.GetNamespaceStorage() == HtmlNamespace.Html
            && attribute.GetNamespaceUri().Length == 0
            && HtmlNames.IsBooleanAttribute(name.AsSpan());
    }

    internal static bool SerializesAsVoid(HtmlNode node)
    {
        if (node.GetKindStorage() != HtmlNodeKind.Element
            || node.GetNamespaceStorage() != HtmlNamespace.Html)
        {
            return false;
        }

        string name = FoldAscii(node.GetLocalNameStorage());
        return HtmlNames.IsVoidElement(name.AsSpan())
            || name == "basefont" || name == "bgsound" || name == "frame"
            || name == "keygen" || name == "param";
    }

    internal static bool HasRawTextParent(HtmlNode node)
    {
        HtmlNode? parent = node.GetParentStorage();
        if (parent == null || parent.GetKindStorage() != HtmlNodeKind.Element
            || parent.GetNamespaceStorage() != HtmlNamespace.Html)
        {
            return false;
        }

        string name = FoldAscii(parent.GetLocalNameStorage());
        return HtmlNames.IsRawTextElement(name.AsSpan())
            || name == "plaintext";
    }
}
