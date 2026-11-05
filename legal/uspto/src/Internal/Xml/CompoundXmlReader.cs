namespace uspto;

internal class CompoundXmlDocument
{
    internal byte[] Bytes = new byte[0];
    internal long DocumentNumber = 0L;
    internal long StartOffset = 0L;
    internal long EndOffset = 0L;
    internal long StartLineNumber = 0L;
    internal long EndLineNumber = 0L;
}

internal class CompoundXmlReader
{
    internal const int ContentMode = 0;
    internal const int PrefixMode = 1;
    internal const int StartTagMode = 2;
    internal const int EndTagMode = 3;
    internal const int ProcessingInstructionMode = 4;
    internal const int CommentMode = 5;
    internal const int CdataMode = 6;
    internal const int DeclarationMode = 7;

    internal global::System.IO.Stream Input;
    internal string? SourceName;
    internal byte[] Buffer;
    internal int BufferOffset = 0;
    internal int BufferCount = 0;
    internal long MaximumDocumentBytes;
    internal long StreamOffset = 0L;
    internal long PhysicalLineNumber = 1L;
    internal bool PreviousWasCarriageReturn = false;
    internal long DocumentsRead = 0L;

    internal bool DocumentActive = false;
    internal bool RootSeen = false;
    internal int RootDepth = 0;
    internal int Mode = ContentMode;
    internal byte Quote = 0;
    internal byte PreviousByte = 0;
    internal byte SecondPreviousByte = 0;
    internal byte LastTagByte = 0;
    internal int DeclarationBracketDepth = 0;
    internal byte[] Prefix = new byte[9];
    internal int PrefixLength = 0;
    internal int BomState = 0;
    internal bool DocumentComplete = false;
    internal long DocumentStartOffset = 0L;
    internal long DocumentStartLine = 0L;
    internal long DocumentEndLine = 0L;

    internal CompoundXmlReader(global::System.IO.Stream input, string? sourceName, int bufferSize, UsptoLimits limits)
    {
        if (input == null)
        {
            throw new ValidationException("A compound XML input stream is required.", "input", "REQUIRED", null);
        }

        if (!input.CanRead)
        {
            throw new ValidationException("The compound XML input stream must be readable.", "input", "STREAM_NOT_READABLE", null);
        }

        if (bufferSize <= 0)
        {
            throw new ValidationException("The compound XML buffer size must be positive.", "bufferSize", "INVALID_LIMIT", null);
        }

        if (limits == null)
        {
            throw new ValidationException("Compound XML limits are required.", "limits", "REQUIRED", null);
        }

        if (limits.MaximumResponseBytes <= 0L || limits.MaximumResponseBytes > global::System.Int32.MaxValue)
        {
            throw new ValidationException("The compound XML document limit must fit in a supported array.", "MaximumResponseBytes", "INVALID_LIMIT", null);
        }

        Input = input;
        SourceName = sourceName;
        Buffer = new byte[bufferSize];
        MaximumDocumentBytes = limits.MaximumResponseBytes;
    }

    internal CompoundXmlDocument? ReadDocument()
    {
        ResetDocumentState();
        global::System.IO.MemoryStream output = new global::System.IO.MemoryStream();
        while (!DocumentComplete)
        {
            if (BufferOffset == BufferCount)
            {
                BufferCount = Input.Read(Buffer, 0, Buffer.Length);
                BufferOffset = 0;
                if (BufferCount == 0)
                {
                    if (!DocumentActive && BomState == 0)
                    {
                        return null;
                    }

                    ThrowTruncatedDocument();
                }
            }

            global::System.ReadOnlySpan<byte> available = new global::System.ReadOnlySpan<byte>(Buffer, BufferOffset, BufferCount - BufferOffset);
            int consumed = ScanChunk(available, output);
            BufferOffset += consumed;
            StreamOffset += consumed;
        }

        DocumentsRead += 1L;
        CompoundXmlDocument document = new CompoundXmlDocument();
        document.Bytes = output.ToArray();
        document.DocumentNumber = DocumentsRead;
        document.StartOffset = DocumentStartOffset;
        document.EndOffset = StreamOffset;
        document.StartLineNumber = DocumentStartLine;
        document.EndLineNumber = DocumentEndLine;
        return document;
    }

    internal int ScanChunk(global::System.ReadOnlySpan<byte> bytes, global::System.IO.MemoryStream output)
    {
        int index = 0;
        while (index < bytes.Length && !DocumentComplete)
        {
            byte value = bytes[index];
            long absoluteOffset = StreamOffset + index;
            long lineNumber = PhysicalLineNumber;
            if (!DocumentActive)
            {
                ConsumeOutsideDocument(value, absoluteOffset, lineNumber);
                if (DocumentActive)
                {
                    AppendByte(output, value, absoluteOffset, lineNumber);
                }
            }
            else
            {
                AppendByte(output, value, absoluteOffset, lineNumber);
                ConsumeDocumentByte(value, absoluteOffset, lineNumber);
            }

            AdvanceLine(value);
            index += 1;
        }

        return index;
    }

    internal void ResetDocumentState()
    {
        DocumentActive = false;
        RootSeen = false;
        RootDepth = 0;
        Mode = ContentMode;
        Quote = 0;
        PreviousByte = 0;
        SecondPreviousByte = 0;
        LastTagByte = 0;
        DeclarationBracketDepth = 0;
        PrefixLength = 0;
        BomState = 0;
        DocumentComplete = false;
        DocumentStartOffset = 0L;
        DocumentStartLine = 0L;
        DocumentEndLine = 0L;
    }

    internal void ConsumeOutsideDocument(byte value, long absoluteOffset, long lineNumber)
    {
        if (BomState == 1)
        {
            if (value != 0xBB)
            {
                ThrowUnexpectedData(absoluteOffset, lineNumber);
            }

            BomState = 2;
            return;
        }

        if (BomState == 2)
        {
            if (value != 0xBF)
            {
                ThrowUnexpectedData(absoluteOffset, lineNumber);
            }

            BomState = 0;
            return;
        }

        if (value == 0xEF)
        {
            BomState = 1;
            return;
        }

        if (IsWhitespace(value))
        {
            return;
        }

        if (value != (byte)'<')
        {
            ThrowUnexpectedData(absoluteOffset, lineNumber);
        }

        DocumentActive = true;
        DocumentStartOffset = absoluteOffset;
        DocumentStartLine = lineNumber;
        Mode = PrefixMode;
        Prefix[0] = value;
        PrefixLength = 1;
    }

    internal void ConsumeDocumentByte(byte value, long absoluteOffset, long lineNumber)
    {
        if (Mode == ContentMode)
        {
            if (value == (byte)'<')
            {
                Mode = PrefixMode;
                Prefix[0] = value;
                PrefixLength = 1;
            }
            else if (!RootSeen && !IsWhitespace(value))
            {
                ThrowMalformedBoundary("Unexpected text appears before the XML root element.", absoluteOffset, lineNumber);
            }

            return;
        }

        if (Mode == PrefixMode)
        {
            if (PrefixLength < Prefix.Length)
            {
                Prefix[PrefixLength] = value;
                PrefixLength += 1;
            }

            AdvancePrefix(absoluteOffset, lineNumber);
            return;
        }

        if (Mode == StartTagMode)
        {
            ConsumeStartTag(value, absoluteOffset, lineNumber);
            return;
        }

        if (Mode == EndTagMode)
        {
            if (value == (byte)'>')
            {
                FinishEndTag(absoluteOffset, lineNumber);
            }

            return;
        }

        if (Mode == ProcessingInstructionMode)
        {
            if (PreviousByte == (byte)'?' && value == (byte)'>')
            {
                Mode = ContentMode;
            }

            PreviousByte = value;
            return;
        }

        if (Mode == CommentMode)
        {
            if (SecondPreviousByte == (byte)'-' && PreviousByte == (byte)'-' && value == (byte)'>')
            {
                Mode = ContentMode;
            }

            SecondPreviousByte = PreviousByte;
            PreviousByte = value;
            return;
        }

        if (Mode == CdataMode)
        {
            if (SecondPreviousByte == (byte)']' && PreviousByte == (byte)']' && value == (byte)'>')
            {
                Mode = ContentMode;
            }

            SecondPreviousByte = PreviousByte;
            PreviousByte = value;
            return;
        }

        if (Mode == DeclarationMode)
        {
            ConsumeDeclaration(value);
        }
    }

    internal void AdvancePrefix(long absoluteOffset, long lineNumber)
    {
        global::System.ReadOnlySpan<byte> prefix = new global::System.ReadOnlySpan<byte>(Prefix, 0, PrefixLength);
        if (PrefixLength == 2 && Prefix[1] == (byte)'?')
        {
            Mode = ProcessingInstructionMode;
            PreviousByte = (byte)'?';
            return;
        }

        if (PrefixLength == 2 && Prefix[1] == (byte)'/')
        {
            Mode = EndTagMode;
            return;
        }

        if (PrefixLength == 2 && Prefix[1] != (byte)'!')
        {
            Mode = StartTagMode;
            LastTagByte = Prefix[1];
            return;
        }

        if (MatchesToken(prefix, "<!--"))
        {
            Mode = CommentMode;
            PreviousByte = 0;
            SecondPreviousByte = 0;
            return;
        }

        if (MatchesToken(prefix, "<![CDATA["))
        {
            Mode = CdataMode;
            PreviousByte = 0;
            SecondPreviousByte = 0;
            return;
        }

        if (MatchesToken(prefix, "<!DOCTYPE"))
        {
            Mode = DeclarationMode;
            Quote = 0;
            DeclarationBracketDepth = 0;
            return;
        }

        bool possibleComment = IsTokenPrefix(prefix, "<!--");
        bool possibleCdata = IsTokenPrefix(prefix, "<![CDATA[");
        bool possibleDoctype = IsTokenPrefix(prefix, "<!DOCTYPE");
        if (possibleComment || possibleCdata || possibleDoctype)
        {
            return;
        }

        if (PrefixLength >= 2 && Prefix[1] == (byte)'!')
        {
            Mode = DeclarationMode;
            Quote = 0;
            DeclarationBracketDepth = 0;
            byte last = Prefix[PrefixLength - 1];
            if (last == (byte)'>')
            {
                Mode = ContentMode;
            }

            return;
        }

        ThrowMalformedBoundary("The XML markup boundary is malformed.", absoluteOffset, lineNumber);
    }

    internal void ConsumeStartTag(byte value, long absoluteOffset, long lineNumber)
    {
        if (Quote != 0)
        {
            if (value == Quote)
            {
                Quote = 0;
            }

            return;
        }

        if (value == (byte)'\'' || value == (byte)'"')
        {
            Quote = value;
            return;
        }

        if (value == (byte)'>')
        {
            bool selfClosing = LastTagByte == (byte)'/';
            FinishStartTag(selfClosing, absoluteOffset, lineNumber);
            return;
        }

        if (!IsWhitespace(value))
        {
            LastTagByte = value;
        }
    }

    internal void FinishStartTag(bool selfClosing, long absoluteOffset, long lineNumber)
    {
        if (!RootSeen)
        {
            RootSeen = true;
            if (selfClosing)
            {
                RootDepth = 0;
                DocumentComplete = true;
                DocumentEndLine = lineNumber;
            }
            else
            {
                RootDepth = 1;
            }
        }
        else if (!selfClosing)
        {
            if (RootDepth == global::System.Int32.MaxValue)
            {
                ThrowMalformedBoundary("The XML element depth is too large to scan safely.", absoluteOffset, lineNumber);
            }

            RootDepth += 1;
        }

        Mode = ContentMode;
        Quote = 0;
        LastTagByte = 0;
    }

    internal void FinishEndTag(long absoluteOffset, long lineNumber)
    {
        if (!RootSeen || RootDepth <= 0)
        {
            ThrowMalformedBoundary("An XML closing tag appears outside the root element.", absoluteOffset, lineNumber);
        }

        RootDepth -= 1;
        Mode = ContentMode;
        if (RootDepth == 0)
        {
            DocumentComplete = true;
            DocumentEndLine = lineNumber;
        }
    }

    internal void ConsumeDeclaration(byte value)
    {
        if (Quote != 0)
        {
            if (value == Quote)
            {
                Quote = 0;
            }

            return;
        }

        if (value == (byte)'\'' || value == (byte)'"')
        {
            Quote = value;
        }
        else if (value == (byte)'[')
        {
            DeclarationBracketDepth += 1;
        }
        else if (value == (byte)']' && DeclarationBracketDepth > 0)
        {
            DeclarationBracketDepth -= 1;
        }
        else if (value == (byte)'>' && DeclarationBracketDepth == 0)
        {
            Mode = ContentMode;
        }
    }

    internal void AppendByte(global::System.IO.MemoryStream output, byte value, long absoluteOffset, long lineNumber)
    {
        if (output.Length >= MaximumDocumentBytes)
        {
            ThrowDocumentLimit(absoluteOffset, lineNumber);
        }

        output.WriteByte(value);
    }

    internal void AdvanceLine(byte value)
    {
        if (value == (byte)'\r')
        {
            PhysicalLineNumber += 1L;
            PreviousWasCarriageReturn = true;
        }
        else if (value == (byte)'\n')
        {
            if (!PreviousWasCarriageReturn)
            {
                PhysicalLineNumber += 1L;
            }

            PreviousWasCarriageReturn = false;
        }
        else
        {
            PreviousWasCarriageReturn = false;
        }
    }

    internal bool IsWhitespace(byte value)
    {
        return value == (byte)' ' || value == (byte)'\t' || value == (byte)'\r' || value == (byte)'\n';
    }

    internal bool MatchesToken(global::System.ReadOnlySpan<byte> value, string token)
    {
        if (value.Length != token.Length)
        {
            return false;
        }

        for (int index = 0; index < value.Length; index += 1)
        {
            if (value[index] != (byte)token[index])
            {
                return false;
            }
        }

        return true;
    }

    internal bool IsTokenPrefix(global::System.ReadOnlySpan<byte> value, string token)
    {
        if (value.Length > token.Length)
        {
            return false;
        }

        for (int index = 0; index < value.Length; index += 1)
        {
            if (value[index] != (byte)token[index])
            {
                return false;
            }
        }

        return true;
    }

    internal string CreateDocumentPath()
    {
        long number = DocumentsRead + 1L;
        return "document[" + number.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]";
    }

    internal void ThrowUnexpectedData(long offset, long lineNumber)
    {
        ThrowParseFailure("Unexpected data appears between concatenated XML documents.", offset, lineNumber);
    }

    internal void ThrowMalformedBoundary(string message, long offset, long lineNumber)
    {
        ThrowParseFailure(message, offset, lineNumber);
    }

    internal void ThrowDocumentLimit(long offset, long lineNumber)
    {
        ThrowParseFailure("An XML document exceeds the configured byte limit.", offset, lineNumber);
    }

    internal void ThrowTruncatedDocument()
    {
        ThrowParseFailure("The compound XML input ended before the current document boundary.", StreamOffset, PhysicalLineNumber);
    }

    internal void ThrowParseFailure(string message, long offset, long lineNumber)
    {
        int boundedLineNumber = 0;
        bool hasLineNumber = lineNumber <= global::System.Int32.MaxValue;
        if (hasLineNumber)
        {
            boundedLineNumber = (int)lineNumber;
        }

        throw new ParseException(message, DocumentFormat.Xml, SourceName, CreateDocumentPath(), offset, true, boundedLineNumber, hasLineNumber, null);
    }
}
