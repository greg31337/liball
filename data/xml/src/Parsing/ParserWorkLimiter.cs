using System;

namespace xml;

internal class ParserWorkLimiter
{
    internal int MaximumDepth;
    internal long MaximumNodeCount;
    internal int MaximumAttributesPerElement;
    internal int MaximumNameLength;
    internal long MaximumTextLength;
    internal int MaximumTokenLength;
    internal long MaximumInputCharacters;
    internal long MaximumInputBytes;
    internal long MaximumDtdCharacters;
    internal int MaximumEntityDepth;
    internal long MaximumEntityCount;
    internal long MaximumEntityCharacters;
    internal long MaximumEntityAmplification;
    internal int MaximumExternalResources;
    internal int MaximumResourceDepth;
    internal long MaximumResourceBytes;
    internal int CancellationPollingInterval;
    internal IXmlCancellation? Cancellation;
    internal IXmlErrorReporter? ErrorReporter;
    internal long NodeCount;
    internal long InputCharacterCount;
    internal long InputByteCount;
    internal long WorkSinceCancellationPoll;
    internal long DtdCharacterCount;
    internal int EntityDepth;
    internal long EntityCount;
    internal long EntityCharacterCount;
    internal long EntityReferenceCharacterCount;
    internal int ExternalResourceCount;

    internal ParserWorkLimiter(XmlSecurityLimits limits, IXmlCancellation? cancellation)
    {
        Guard.NotNull(limits, "limits");
        Guard.NonNegativeInt(limits.MaximumDepth, "limits.MaximumDepth");
        Guard.NonNegative(limits.MaximumNodeCount, "limits.MaximumNodeCount");
        Guard.NonNegativeInt(limits.MaximumAttributesPerElement, "limits.MaximumAttributesPerElement");
        Guard.NonNegativeInt(limits.MaximumNameLength, "limits.MaximumNameLength");
        Guard.NonNegative(limits.MaximumTextLength, "limits.MaximumTextLength");
        Guard.NonNegativeInt(limits.MaximumTokenLength, "limits.MaximumTokenLength");
        Guard.NonNegative(limits.MaximumInputCharacters, "limits.MaximumInputCharacters");
        Guard.NonNegative(limits.MaximumInputBytes, "limits.MaximumInputBytes");
        Guard.NonNegative(limits.MaximumDtdCharacters, "limits.MaximumDtdCharacters");
        Guard.NonNegativeInt(limits.MaximumEntityDepth, "limits.MaximumEntityDepth");
        Guard.NonNegative(limits.MaximumEntityCount, "limits.MaximumEntityCount");
        Guard.NonNegative(limits.MaximumEntityCharacters, "limits.MaximumEntityCharacters");
        Guard.NonNegative(limits.MaximumEntityAmplification, "limits.MaximumEntityAmplification");
        Guard.NonNegativeInt(limits.MaximumExternalResources, "limits.MaximumExternalResources");
        Guard.NonNegativeInt(limits.MaximumResourceDepth, "limits.MaximumResourceDepth");
        Guard.NonNegative(limits.MaximumResourceBytes, "limits.MaximumResourceBytes");
        Guard.Positive(limits.CancellationPollingInterval, "limits.CancellationPollingInterval");

        MaximumDepth = limits.MaximumDepth;
        MaximumNodeCount = limits.MaximumNodeCount;
        MaximumAttributesPerElement = limits.MaximumAttributesPerElement;
        MaximumNameLength = limits.MaximumNameLength;
        MaximumTextLength = limits.MaximumTextLength;
        MaximumTokenLength = limits.MaximumTokenLength;
        MaximumInputCharacters = limits.MaximumInputCharacters;
        MaximumInputBytes = limits.MaximumInputBytes;
        MaximumDtdCharacters = limits.MaximumDtdCharacters;
        MaximumEntityDepth = limits.MaximumEntityDepth;
        MaximumEntityCount = limits.MaximumEntityCount;
        MaximumEntityCharacters = limits.MaximumEntityCharacters;
        MaximumEntityAmplification = limits.MaximumEntityAmplification;
        MaximumExternalResources = limits.MaximumExternalResources;
        MaximumResourceDepth = limits.MaximumResourceDepth;
        MaximumResourceBytes = limits.MaximumResourceBytes;
        CancellationPollingInterval = limits.CancellationPollingInterval;
        Cancellation = cancellation;
        ErrorReporter = null;
        NodeCount = 0;
        InputCharacterCount = 0;
        InputByteCount = 0;
        WorkSinceCancellationPoll = 0;
        DtdCharacterCount = 0;
        EntityDepth = 0;
        EntityCount = 0;
        EntityCharacterCount = 0;
        EntityReferenceCharacterCount = 0;
        ExternalResourceCount = 0;
    }

    internal void ObserveXmlInput(long characterCount, long byteCount)
    {
        Guard.NonNegative(characterCount, "characterCount");
        Guard.NonNegative(byteCount, "byteCount");
        long attemptedCharacters = characterCount > long.MaxValue - InputCharacterCount ? long.MaxValue : InputCharacterCount + characterCount;
        if (characterCount > MaximumInputCharacters - InputCharacterCount)
        {
            throw CreateXmlParserLimitException("total input characters", MaximumInputCharacters, attemptedCharacters);
        }

        long attemptedBytes = byteCount > long.MaxValue - InputByteCount ? long.MaxValue : InputByteCount + byteCount;
        if (byteCount > MaximumInputBytes - InputByteCount)
        {
            throw CreateXmlParserLimitException("total input bytes", MaximumInputBytes, attemptedBytes);
        }

        InputCharacterCount = InputCharacterCount + characterCount;
        InputByteCount = InputByteCount + byteCount;
    }

    internal void ObserveXmlToken(XmlToken token)
    {
        Guard.NotNull(token, "token");
        Guard.NonNegativeInt(token.Utf16Length, "token.Utf16Length");
        int lexicalLength = token.Utf16Length;
        if (token.Lexeme.Length > lexicalLength)
        {
            lexicalLength = token.Lexeme.Length;
        }

        CheckXmlTokenLength(lexicalLength);
        if (token.Name != null)
        {
            CheckXmlNameLength(token.Name);
        }

        AdvanceXmlParserWork(lexicalLength);
    }

    internal void ObserveXmlDtdCharacters(long amount)
    {
        Guard.NonNegative(amount, "amount");
        long attempted = SaturatingXmlWorkSum(DtdCharacterCount, amount);
        if (amount > MaximumDtdCharacters - DtdCharacterCount)
        {
            throw CreateXmlParserLimitException("DTD character count", MaximumDtdCharacters, attempted);
        }

        DtdCharacterCount = DtdCharacterCount + amount;
        AdvanceXmlParserWork(amount);
    }

    internal void BeginXmlEntityActivation(int referenceLength, bool documentReference)
    {
        Guard.NonNegativeInt(referenceLength, "referenceLength");
        long attemptedCount = SaturatingXmlWorkSum(EntityCount, 1);
        if (EntityCount >= MaximumEntityCount)
        {
            throw CreateXmlParserLimitException("entity activation count", MaximumEntityCount, attemptedCount);
        }

        long attemptedDepth = (long)EntityDepth + 1;
        if (attemptedDepth > MaximumEntityDepth)
        {
            throw CreateXmlParserLimitException("entity nesting depth", MaximumEntityDepth, attemptedDepth);
        }

        EntityCount = EntityCount + 1;
        EntityDepth = EntityDepth + 1;
        if (documentReference)
        {
            EntityReferenceCharacterCount = SaturatingXmlWorkSum(EntityReferenceCharacterCount, referenceLength);
        }

        AdvanceXmlParserWork(referenceLength);
    }

    internal void EndXmlEntityActivation()
    {
        if (EntityDepth <= 0)
        {
            throw new InvalidOperationException("No XML entity activation is open.");
        }

        EntityDepth = EntityDepth - 1;
    }

    internal void ConsumeXmlEntityCharacters(long amount)
    {
        Guard.NonNegative(amount, "amount");
        long attempted = SaturatingXmlWorkSum(EntityCharacterCount, amount);
        if (amount > MaximumEntityCharacters - EntityCharacterCount)
        {
            throw CreateXmlParserLimitException("expanded entity character count", MaximumEntityCharacters, attempted);
        }

        long amplifiedMaximum = SaturatingXmlWorkProduct(EntityReferenceCharacterCount, MaximumEntityAmplification);
        if (attempted > amplifiedMaximum)
        {
            throw CreateXmlParserLimitException("entity amplification", amplifiedMaximum, attempted);
        }

        EntityCharacterCount = EntityCharacterCount + amount;
        AdvanceXmlParserWork(amount);
    }

    internal void BeginXmlExternalResource()
    {
        long attemptedCount = (long)ExternalResourceCount + 1;
        if (attemptedCount > MaximumExternalResources)
        {
            throw CreateXmlParserLimitException("external resource count", MaximumExternalResources, attemptedCount);
        }

        int effectiveDepth = EntityDepth > 0 ? EntityDepth : 1;
        if (effectiveDepth > MaximumResourceDepth)
        {
            throw CreateXmlParserLimitException("external resource depth", MaximumResourceDepth, effectiveDepth);
        }

        ExternalResourceCount = ExternalResourceCount + 1;
        AdvanceXmlParserWork(1);
    }

    internal void CheckXmlExternalResourceBytes(long byteCount)
    {
        Guard.NonNegative(byteCount, "byteCount");
        if (byteCount > MaximumResourceBytes)
        {
            throw CreateXmlParserLimitException("external resource bytes", MaximumResourceBytes, byteCount);
        }

        AdvanceXmlParserWork(byteCount);
    }

    internal long SaturatingXmlWorkSum(long left, long right)
    {
        if (right > long.MaxValue - left)
        {
            return long.MaxValue;
        }

        return left + right;
    }

    internal long SaturatingXmlWorkProduct(long left, long right)
    {
        if (left == 0 || right == 0)
        {
            return 0;
        }

        if (left > long.MaxValue / right)
        {
            return long.MaxValue;
        }

        return left * right;
    }

    internal void ObserveXmlElementTag(XmlElementTag tag)
    {
        Guard.NotNull(tag, "tag");
        CheckXmlDepth(tag.Depth);
        CheckXmlNameLength(tag.Name);
        if (!tag.IsEndElement)
        {
            CheckXmlAttributeCount(tag.Attributes.Length);
            int index = 0;
            while (index < tag.Attributes.Length)
            {
                CheckXmlNameLength(tag.Attributes[index].Name);
                index = index + 1;
            }
        }

        AdvanceXmlParserWork(tag.Attributes.Length + 1);
    }

    internal void ObserveXmlEvent(XmlEvent item)
    {
        Guard.NotNull(item, "item");
        if (item.Kind == XmlEventKind.StartElement || item.Kind == XmlEventKind.EndElement)
        {
            CheckXmlDepth(item.Depth);
        }

        if (item.Name != null)
        {
            string? eventName = item.Name.QualifiedName == null ? item.Name.LocalName : item.Name.QualifiedName;
            if (eventName != null)
            {
                CheckXmlNameLength(eventName);
            }
        }

        int attributeCount = item.Attributes == null ? 0 : item.Attributes.Length;
        if (item.Kind == XmlEventKind.StartElement)
        {
            CheckXmlAttributeCount(attributeCount);
            int attributeIndex = 0;
            while (attributeIndex < attributeCount)
            {
                XmlAttributeData attribute = item.Attributes == null ? new XmlAttributeData() : item.Attributes[attributeIndex];
                if (attribute.Name != null)
                {
                    string? attributeName = attribute.Name.QualifiedName == null ? attribute.Name.LocalName : attribute.Name.QualifiedName;
                    if (attributeName != null)
                    {
                        CheckXmlNameLength(attributeName);
                    }
                }

                attributeIndex = attributeIndex + 1;
            }
        }

        if (item.Kind == XmlEventKind.Text || item.Kind == XmlEventKind.IgnorableWhitespace || item.Kind == XmlEventKind.CData)
        {
            CheckXmlTextLength(item.Value == null ? "" : item.Value);
        }

        long addedNodes = CountXmlEventNodes(item.Kind, attributeCount);
        ConsumeXmlNodes(addedNodes);
        long work = 1;
        if (item.Value != null)
        {
            work = work + item.Value.Length;
        }

        AdvanceXmlParserWork(work);
    }

    internal void CheckXmlDepth(int zeroBasedDepth)
    {
        Guard.NonNegativeInt(zeroBasedDepth, "zeroBasedDepth");
        long actualDepth = (long)zeroBasedDepth + 1;
        if (actualDepth > MaximumDepth)
        {
            throw CreateXmlParserLimitException("element depth", MaximumDepth, actualDepth);
        }
    }

    internal void CheckXmlAttributeCount(int attributeCount)
    {
        Guard.NonNegativeInt(attributeCount, "attributeCount");
        if (attributeCount > MaximumAttributesPerElement)
        {
            throw CreateXmlParserLimitException("attributes per element", MaximumAttributesPerElement, attributeCount);
        }
    }

    internal void CheckXmlNameLength(string name)
    {
        Guard.NotNull(name, "name");
        if (name.Length > MaximumNameLength)
        {
            throw CreateXmlParserLimitException("XML name length", MaximumNameLength, name.Length);
        }
    }

    internal void CheckXmlTextLength(string text)
    {
        Guard.NotNull(text, "text");
        if (text.Length > MaximumTextLength)
        {
            throw CreateXmlParserLimitException("text node length", MaximumTextLength, text.Length);
        }
    }

    internal void CheckXmlTokenLength(int tokenLength)
    {
        Guard.NonNegativeInt(tokenLength, "tokenLength");
        if (tokenLength > MaximumTokenLength)
        {
            throw CreateXmlParserLimitException("lexical token length", MaximumTokenLength, tokenLength);
        }
    }

    internal void ConsumeXmlNodes(long amount)
    {
        Guard.NonNegative(amount, "amount");
        long attempted = amount > long.MaxValue - NodeCount ? long.MaxValue : NodeCount + amount;
        if (amount > MaximumNodeCount - NodeCount)
        {
            throw CreateXmlParserLimitException("node count", MaximumNodeCount, attempted);
        }

        NodeCount = NodeCount + amount;
    }

    internal long CountXmlEventNodes(XmlEventKind kind, int attributeCount)
    {
        if (kind == XmlEventKind.StartElement)
        {
            return (long)attributeCount + 1;
        }

        if (kind == XmlEventKind.StartDocument || kind == XmlEventKind.StartEntity || kind == XmlEventKind.StartDtd || kind == XmlEventKind.StartNamespace)
        {
            return 1;
        }

        if (kind == XmlEventKind.Text || kind == XmlEventKind.IgnorableWhitespace || kind == XmlEventKind.CData || kind == XmlEventKind.Comment || kind == XmlEventKind.ProcessingInstruction || kind == XmlEventKind.SkippedEntity)
        {
            return 1;
        }

        return 0;
    }

    internal void AdvanceXmlParserWork(long workUnits)
    {
        Guard.NonNegative(workUnits, "workUnits");
        if (Cancellation == null)
        {
            long remainder = workUnits % CancellationPollingInterval;
            WorkSinceCancellationPoll = WorkSinceCancellationPoll + remainder;
            if (WorkSinceCancellationPoll >= CancellationPollingInterval)
            {
                WorkSinceCancellationPoll = WorkSinceCancellationPoll - CancellationPollingInterval;
            }

            return;
        }

        long remaining = workUnits;
        while (remaining > 0)
        {
            long untilPoll = CancellationPollingInterval - WorkSinceCancellationPoll;
            if (remaining < untilPoll)
            {
                WorkSinceCancellationPoll = WorkSinceCancellationPoll + remaining;
                return;
            }

            remaining = remaining - untilPoll;
            WorkSinceCancellationPoll = 0;
            PollXmlCancellation();
        }
    }

    internal void PollXmlCancellation()
    {
        IXmlCancellation? optionalCancellation = Cancellation;
        if (optionalCancellation != null && optionalCancellation.IsXmlCancellationRequested())
        {
            throw new OperationCanceledException("XML processing was canceled.");
        }
    }

    internal XmlLimitException CreateXmlParserLimitException(string limitName, long maximum, long attempted)
    {
        string message = "The configured XML " + limitName + " limit of " + maximum + " was exceeded by an attempted value of " + attempted + ".";
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XmlErrorCodes.LimitExceeded;
        diagnostic.Category = XmlErrorCategory.Limit;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        if (ErrorReporter != null)
        {
            ErrorReporter.ReportXmlDiagnostic(diagnostic);
        }

        return new XmlLimitException(message, diagnostic, null);
    }
}
