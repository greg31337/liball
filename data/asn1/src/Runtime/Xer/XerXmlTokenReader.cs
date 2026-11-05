namespace asn1;

internal class XerXmlTokenReader
{
    internal AsnResourceLimits Limits;
    internal ResourceCounter BufferResources;
    internal ByteAccumulator Input = new ByteAccumulator(0);
    internal int EmittedTokenCount;
    internal bool Terminal;
    internal XerXmlReadResult TerminalResult = new XerXmlReadResult();
    internal string Text = "";
    internal int TextByteBase;
    internal int Position;
    internal bool FinalBlock;
    internal bool ParseIncomplete;
    internal bool ParseFailed;
    internal XerXmlReadResult ParseFailure = new XerXmlReadResult();
    internal System.Collections.Generic.List<XerXmlToken> ParsedTokens =
        new System.Collections.Generic.List<XerXmlToken>();
    internal ResourceCounter ParseResources;
    internal XerXmlCharacters Characters = new XerXmlCharacters();
    internal XerXmlNamespaceResolver NamespaceResolver =
        new XerXmlNamespaceResolver();
    internal System.Collections.Generic.List<XerXmlElementFrame> ElementStack =
        new System.Collections.Generic.List<XerXmlElementFrame>();
    internal bool RootSeen;
    internal bool RootClosed;
    internal bool DeclarationSeen;

    internal XerXmlTokenReader(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
        this.BufferResources = new ResourceCounter(limits);
        this.ParseResources = new ResourceCounter(limits);
    }

    internal XerXmlReadResult ConsumeXml(AsnIncrementalInput input)
    {
        if (input == null)
        {
            throw new System.ArgumentNullException("input");
        }

        if (input.Data == null)
        {
            throw new System.ArgumentException(
                "The incremental XML input data cannot be null.",
                "input");
        }

        int count = input.HasCount
            ? input.Count
            : input.Data.Length - input.Offset;
        if (!CursorMath.IsRangeValid(input.Offset, count, input.Data.Length))
        {
            throw new System.ArgumentOutOfRangeException("input");
        }

        if (this.Terminal)
        {
            return this.CreateReadError(
                "ASNXER0001",
                "The XML token reader is terminal until ResetXml is called.",
                false,
                this.Input.Count,
                0);
        }

        if (!this.BufferResources.TryChargeAllocationBytes(count))
        {
            return this.SetTerminalReadError(
                "ASNR0005",
                "Buffering incremental XML input exceeds the configured allocation limit.",
                true,
                this.Input.Count,
                0);
        }

        this.Input.AppendSegment(input.Data, input.Offset, count);
        return this.ParseBufferedXml(count, input.IsFinalBlock);
    }

    internal XerXmlReadResult CompleteXml()
    {
        if (this.Terminal)
        {
            return this.CopyTerminalReadResult();
        }

        return this.ParseBufferedXml(0, true);
    }

    internal void ResetXml()
    {
        this.Input.Clear();
        this.BufferResources = new ResourceCounter(this.Limits);
        this.ParseResources = new ResourceCounter(this.Limits);
        this.EmittedTokenCount = 0;
        this.Terminal = false;
        this.TerminalResult = new XerXmlReadResult();
        this.Text = "";
        this.TextByteBase = 0;
        this.Position = 0;
        this.FinalBlock = false;
        this.ParseIncomplete = false;
        this.ParseFailed = false;
        this.ParseFailure = new XerXmlReadResult();
        this.ParsedTokens.Clear();
        this.ElementStack.Clear();
        this.RootSeen = false;
        this.RootClosed = false;
        this.DeclarationSeen = false;
    }

    internal XerXmlReadResult ParseBufferedXml(
        int consumedCount,
        bool finalBlock)
    {
        XerXmlReadResult utf8 = this.ValidateUtf8(finalBlock);
        if (utf8.Status == AsnDecodeStatus.Error)
        {
            utf8.ConsumedCount = consumedCount;
            this.SetTerminalReadResult(utf8);
            return utf8;
        }

        if (utf8.Status == AsnDecodeStatus.NeedMoreData)
        {
            return new XerXmlReadResult
            {
                Status = AsnDecodeStatus.NeedMoreData,
                ConsumedCount = consumedCount
            };
        }

        byte[] bytes = this.Input.ToArray();
        this.TextByteBase = bytes.Length >= 3
            && bytes[0] == 239
            && bytes[1] == 187
            && bytes[2] == 191
                ? 3
                : 0;
        this.Text = System.Text.Encoding.UTF8.GetString(
            bytes,
            this.TextByteBase,
            bytes.Length - this.TextByteBase);
        if (!new ResourceCounter(this.Limits).AllowsTextCharacters(
            this.Text.Length))
        {
            return this.SetTerminalReadError(
                "ASNR0009",
                "The XML input exceeds the configured text-character limit.",
                true,
                this.GetTextCharacterLimitOffset(),
                consumedCount);
        }

        this.Position = 0;
        this.FinalBlock = finalBlock;
        this.ParseIncomplete = false;
        this.ParseFailed = false;
        this.ParseFailure = new XerXmlReadResult();
        this.ParsedTokens = new System.Collections.Generic.List<XerXmlToken>();
        this.ParseResources = new ResourceCounter(this.Limits);
        this.ElementStack =
            new System.Collections.Generic.List<XerXmlElementFrame>();
        this.RootSeen = false;
        this.RootClosed = false;
        this.DeclarationSeen = false;
        if (!this.ParseResources.TryChargeAllocationBytes(this.Input.Count)
            || !this.ParseResources.TryChargeAllocationBytes(
                (long)this.Text.Length * 2))
        {
            return this.SetTerminalReadError(
                "ASNR0005",
                "Parsing XML text exceeds the configured allocation limit.",
                true,
                0,
                consumedCount);
        }

        this.ParseTokens();
        if (!this.ParseIncomplete && !this.ParseFailed && finalBlock)
        {
            this.ValidateCompleteDocument();
        }
        if (this.ParseFailed)
        {
            this.ParseFailure.ConsumedCount = consumedCount;
            this.SetTerminalReadResult(this.ParseFailure);
            return this.ParseFailure;
        }

        XerXmlToken[] emitted = this.GetNewTokens();
        if (this.ParseIncomplete || !finalBlock)
        {
            return new XerXmlReadResult
            {
                Status = AsnDecodeStatus.NeedMoreData,
                Tokens = emitted,
                ConsumedCount = consumedCount
            };
        }

        XerXmlReadResult result = new XerXmlReadResult
        {
            Status = AsnDecodeStatus.Done,
            Tokens = emitted,
            ConsumedCount = consumedCount,
            Complete = true
        };
        this.SetTerminalReadResult(result);
        return result;
    }

    internal XerXmlReadResult ValidateUtf8(bool finalBlock)
    {
        int index = 0;
        while (index < this.Input.Count)
        {
            int first = this.Input.Buffer[index];
            int length = this.GetUtf8SequenceLength(first);
            if (length == 0)
            {
                return this.CreateReadError(
                    "ASNXER0002",
                    "The XML input contains malformed UTF-8.",
                    false,
                    index,
                    0);
            }

            if (length > this.Input.Count - index)
            {
                return finalBlock
                    ? this.CreateReadError(
                        "ASNXER0003",
                        "The final XML input ends within a UTF-8 scalar.",
                        false,
                        this.Input.Count,
                        0)
                    : new XerXmlReadResult
                    {
                        Status = AsnDecodeStatus.NeedMoreData
                    };
            }

            if (!this.IsValidUtf8Sequence(index, length))
            {
                return this.CreateReadError(
                    "ASNXER0002",
                    "The XML input contains malformed UTF-8.",
                    false,
                    index,
                    0);
            }

            index = index + length;
        }

        return new XerXmlReadResult { Status = AsnDecodeStatus.Done };
    }

    internal int GetUtf8SequenceLength(int first)
    {
        if (first <= 127)
        {
            return 1;
        }

        if (first >= 194 && first <= 223)
        {
            return 2;
        }

        if (first >= 224 && first <= 239)
        {
            return 3;
        }

        return first >= 240 && first <= 244 ? 4 : 0;
    }

    internal bool IsValidUtf8Sequence(int offset, int length)
    {
        int first = this.Input.Buffer[offset];
        int second = length > 1 ? this.Input.Buffer[offset + 1] : 0;
        int index = 1;
        while (index < length)
        {
            int current = this.Input.Buffer[offset + index];
            if (current < 128 || current > 191)
            {
                return false;
            }

            index = index + 1;
        }

        if (length == 3
            && (first == 224 && second < 160
                || first == 237 && second > 159))
        {
            return false;
        }

        return length != 4
            || !(first == 240 && second < 144
                || first == 244 && second > 143);
    }

    internal void ParseTokens()
    {
        while (this.Position < this.Text.Length
            && !this.ParseIncomplete
            && !this.ParseFailed)
        {
            if (this.Text[this.Position] != '<')
            {
                this.ParseTextToken();
            }
            else if (this.StartsWithAt("<!--", this.Position))
            {
                this.ParseCommentToken();
            }
            else if (this.StartsWithAt("<![CDATA[", this.Position))
            {
                this.ParseCDataToken();
            }
            else if (this.StartsWithAt("<?", this.Position))
            {
                this.ParseProcessingInstructionToken();
            }
            else if (this.StartsWithAt("</", this.Position))
            {
                this.ParseEndElementToken();
            }
            else if (this.IsPossibleMarkupPrefix())
            {
                this.ParseIncomplete = true;
            }
            else if (this.StartsWithAt("<!", this.Position))
            {
                this.FailParse(
                    "ASNXER0004",
                    "The XER XML subset does not permit this declaration.",
                    this.Position,
                    false);
            }
            else
            {
                this.ParseStartElementToken();
            }
        }
    }

    internal void ParseTextToken()
    {
        int start = this.Position;
        int marker = this.Text.IndexOf('<', start);
        int end = marker < 0 ? this.Text.Length : marker;
        string raw = this.Text.Substring(start, end - start);
        if (marker < 0 && !this.FinalBlock)
        {
            int ampersand = raw.LastIndexOf('&');
            int semicolon = raw.LastIndexOf(';');
            if (ampersand > semicolon)
            {
                this.ParseIncomplete = true;
                return;
            }

            this.ParseIncomplete = true;
            return;
        }

        if (raw.Contains("]]>", System.StringComparison.Ordinal))
        {
            this.FailParse(
                "ASNXER0005",
                "XML character data cannot contain the CDATA closing delimiter.",
                start + raw.IndexOf("]]>", System.StringComparison.Ordinal),
                false);
            return;
        }

        XerXmlDecodedText decoded = this.DecodeReferences(raw, start);
        if (!decoded.Success)
        {
            this.ApplyDecodedFailure(decoded);
            return;
        }

        if (!this.CheckTextValue(decoded.Value, start))
        {
            return;
        }

        if (this.ElementStack.Count == 0
            && !this.IsOnlyXmlWhitespace(decoded.Value))
        {
            this.FailParse(
                "ASNXER0040",
                "XML character data is not permitted outside the document element.",
                start,
                false);
            return;
        }

        this.Position = end;
        this.AddToken(this.CreateValueToken(
            XerXmlTokenKind.Text,
            decoded.Value,
            start,
            end));
    }

    internal void ParseCommentToken()
    {
        int start = this.Position;
        int endMarker = this.Text.IndexOf(
            "-->",
            start + 4,
            System.StringComparison.Ordinal);
        if (endMarker < 0)
        {
            this.HandleUnterminated(
                "ASNXER0006",
                "The XML comment is unterminated.",
                start);
            return;
        }

        string value = this.Text.Substring(start + 4, endMarker - start - 4);
        if (value.Contains("--", System.StringComparison.Ordinal)
            || value.EndsWith("-", System.StringComparison.Ordinal))
        {
            this.FailParse(
                "ASNXER0007",
                "An XML comment cannot contain a double hyphen or end in a hyphen.",
                start + 4,
                false);
            return;
        }

        if (!this.CheckTextValue(value, start + 4))
        {
            return;
        }

        int end = endMarker + 3;
        this.Position = end;
        this.AddToken(this.CreateValueToken(
            XerXmlTokenKind.Comment,
            value,
            start,
            end));
    }

    internal void ParseCDataToken()
    {
        int start = this.Position;
        if (this.ElementStack.Count == 0)
        {
            this.FailParse(
                "ASNXER0040",
                "An XML CDATA section is not permitted outside the document element.",
                start,
                false);
            return;
        }
        int endMarker = this.Text.IndexOf(
            "]]>",
            start + 9,
            System.StringComparison.Ordinal);
        if (endMarker < 0)
        {
            this.HandleUnterminated(
                "ASNXER0008",
                "The XML CDATA section is unterminated.",
                start);
            return;
        }

        string value = this.Text.Substring(start + 9, endMarker - start - 9);
        if (!this.CheckTextValue(value, start + 9))
        {
            return;
        }

        int end = endMarker + 3;
        this.Position = end;
        this.AddToken(this.CreateValueToken(
            XerXmlTokenKind.CData,
            value,
            start,
            end));
    }

    internal void ParseProcessingInstructionToken()
    {
        int start = this.Position;
        this.Position = this.Position + 2;
        string name = this.ReadName();
        if (this.ParseIncomplete || this.ParseFailed)
        {
            return;
        }

        if (name.Length == 0)
        {
            this.FailParse(
                "ASNXER0009",
                "An XML processing instruction requires a valid target name.",
                start + 2,
                false);
            return;
        }

        if (string.Equals(
            name,
            "xml",
            System.StringComparison.OrdinalIgnoreCase))
        {
            if (name == "xml"
                && start == 0
                && !this.DeclarationSeen
                && this.ParsedTokens.Count == 0)
            {
                this.ParseXmlDeclarationToken(start);
                return;
            }

            this.FailParse(
                "ASNXER0041",
                "The reserved XML processing-instruction target is only valid in the XML declaration.",
                start + 2,
                false);
            return;
        }

        int contentStart = this.Position;
        if (this.Position < this.Text.Length
            && this.IsXmlWhitespace(this.Text[this.Position]))
        {
            this.SkipXmlWhitespace();
            contentStart = this.Position;
        }
        else if (!this.StartsWithAt("?>", this.Position))
        {
            this.HandleUnterminated(
                "ASNXER0010",
                "A processing-instruction target must be followed by whitespace or its terminator.",
                this.Position);
            return;
        }

        int endMarker = this.Text.IndexOf(
            "?>",
            this.Position,
            System.StringComparison.Ordinal);
        if (endMarker < 0)
        {
            this.HandleUnterminated(
                "ASNXER0010",
                "The XML processing instruction is unterminated.",
                start);
            return;
        }

        string value = this.Text.Substring(
            contentStart,
            endMarker - contentStart);
        if (!this.CheckTextValue(value, contentStart))
        {
            return;
        }

        int end = endMarker + 2;
        XerXmlToken token = this.CreateNamedToken(
            XerXmlTokenKind.ProcessingInstruction,
            name,
            start,
            end);
        token.Value = value;
        this.Position = end;
        this.AddToken(token);
    }

    internal void ParseEndElementToken()
    {
        int start = this.Position;
        this.Position = this.Position + 2;
        string name = this.ReadName();
        if (this.ParseIncomplete || this.ParseFailed)
        {
            return;
        }

        this.SkipXmlWhitespace();
        if (this.Position >= this.Text.Length)
        {
            this.HandleUnterminated(
                "ASNXER0011",
                "The XML end element is unterminated.",
                start);
            return;
        }

        if (this.Text[this.Position] != '>')
        {
            this.FailParse(
                "ASNXER0012",
                "An XML end element contains unexpected content.",
                this.Position,
                false);
            return;
        }

        this.Position = this.Position + 1;
        if (this.ElementStack.Count == 0)
        {
            this.FailParse(
                "ASNXER0042",
                "The XML end element has no matching start element.",
                start,
                false);
            return;
        }

        XerXmlElementFrame frame =
            this.ElementStack[this.ElementStack.Count - 1];
        XerXmlQualifiedName resolved =
            this.NamespaceResolver.ResolveElementName(
                name,
                frame.NamespaceBindings);
        if (!resolved.Success)
        {
            this.FailParse(
                resolved.ErrorCode,
                resolved.ErrorMessage,
                start + 2,
                false);
            return;
        }

        if (name != frame.Name
            || resolved.LocalName != frame.LocalName
            || resolved.NamespaceUri != frame.NamespaceUri)
        {
            this.FailParse(
                "ASNXER0043",
                "The XML end element does not match its start element.",
                start,
                false);
            return;
        }

        XerXmlToken token = this.CreateNamedToken(
            XerXmlTokenKind.EndElement,
            name,
            start,
            this.Position);
        token.Prefix = resolved.Prefix;
        token.LocalName = resolved.LocalName;
        token.NamespaceUri = resolved.NamespaceUri;
        token.NamespaceBindings = frame.NamespaceBindings;
        this.ElementStack.RemoveAt(this.ElementStack.Count - 1);
        this.ParseResources.LeaveNesting();
        if (this.ElementStack.Count == 0)
        {
            this.RootClosed = true;
        }

        this.AddToken(token);
    }

    internal void ParseStartElementToken()
    {
        int start = this.Position;
        this.Position = this.Position + 1;
        string name = this.ReadName();
        if (this.ParseIncomplete || this.ParseFailed)
        {
            return;
        }

        if (name.Length == 0)
        {
            this.FailParse(
                "ASNXER0013",
                "An XML start element requires a valid name.",
                start + 1,
                false);
            return;
        }

        System.Collections.Generic.List<XerXmlAttribute> attributes =
            new System.Collections.Generic.List<XerXmlAttribute>();
        while (!this.ParseIncomplete && !this.ParseFailed)
        {
            this.SkipXmlWhitespace();
            if (this.Position >= this.Text.Length)
            {
                this.HandleUnterminated(
                    "ASNXER0014",
                    "The XML start element is unterminated.",
                    start);
                return;
            }

            bool empty = this.StartsWithAt("/>", this.Position);
            if (empty || this.Text[this.Position] == '>')
            {
                this.Position = this.Position + (empty ? 2 : 1);
                if (!empty && !this.ParseResources.TryEnterNesting())
                {
                    this.FailParse(
                        "ASNR0001",
                        "The XML nesting depth exceeds the configured limit.",
                        start,
                        true);
                    return;
                }

                XerXmlToken token = this.CreateNamedToken(
                    empty
                        ? XerXmlTokenKind.EmptyElement
                        : XerXmlTokenKind.StartElement,
                    name,
                    start,
                    this.Position);
                token.Attributes = attributes.ToArray();
                if (!this.ResolveStartElement(token, start, empty))
                {
                    return;
                }

                this.AddToken(token);
                return;
            }

            if (this.Text[this.Position] == '/'
                && this.Position + 1 >= this.Text.Length)
            {
                this.HandleUnterminated(
                    "ASNXER0014",
                    "The XML empty-element tag is unterminated.",
                    start);
                return;
            }

            if (!this.ParseResources.AllowsCollectionElements(
                attributes.Count + 1))
            {
                this.FailParse(
                    "ASNR0011",
                    "The XML attribute count exceeds the configured limit.",
                    this.Position,
                    true);
                return;
            }

            XerXmlAttribute? attribute = this.ParseAttribute();
            if (attribute != null)
            {
                attributes.Add(attribute);
            }
        }
    }

    internal XerXmlAttribute? ParseAttribute()
    {
        int start = this.Position;
        string name = this.ReadName();
        if (this.ParseIncomplete || this.ParseFailed)
        {
            return null;
        }

        if (name.Length == 0)
        {
            this.FailParse(
                "ASNXER0015",
                "An XML attribute requires a valid name.",
                start,
                false);
            return null;
        }

        this.SkipXmlWhitespace();
        if (this.Position >= this.Text.Length)
        {
            this.HandleUnterminated(
                "ASNXER0016",
                "The XML attribute is missing '='.",
                start);
            return null;
        }

        if (this.Text[this.Position] != '=')
        {
            this.FailParse(
                "ASNXER0016",
                "The XML attribute is missing '='.",
                this.Position,
                false);
            return null;
        }

        this.Position = this.Position + 1;
        this.SkipXmlWhitespace();
        if (this.Position >= this.Text.Length)
        {
            this.HandleUnterminated(
                "ASNXER0017",
                "The XML attribute value is missing.",
                start);
            return null;
        }

        char quote = this.Text[this.Position];
        if (quote != '\'' && quote != '"')
        {
            this.FailParse(
                "ASNXER0017",
                "An XML attribute value must be quoted.",
                this.Position,
                false);
            return null;
        }

        this.Position = this.Position + 1;
        int valueStart = this.Position;
        int end = this.Text.IndexOf(quote, valueStart);
        if (end < 0)
        {
            this.HandleUnterminated(
                "ASNXER0018",
                "The XML attribute value is unterminated.",
                start);
            return null;
        }

        string raw = this.Text.Substring(valueStart, end - valueStart);
        int lessThan = raw.IndexOf('<');
        if (lessThan >= 0)
        {
            this.FailParse(
                "ASNXER0019",
                "An XML attribute value cannot contain '<'.",
                valueStart + lessThan,
                false);
            return null;
        }

        XerXmlDecodedText decoded = this.DecodeReferences(raw, valueStart);
        if (!decoded.Success)
        {
            this.ApplyDecodedFailure(decoded);
            return null;
        }

        if (!this.CheckTextValue(decoded.Value, valueStart))
        {
            return null;
        }

        this.Position = end + 1;
        XerXmlAttribute attribute = new XerXmlAttribute
        {
            Name = name,
            Value = decoded.Value
        };
        this.SplitAttributeName(name, attribute);
        if (name == "xmlns")
        {
            attribute.NamespaceDeclaration = true;
        }
        else if (attribute.Prefix == "xmlns")
        {
            attribute.NamespaceDeclaration = true;
            attribute.NamespacePrefix = attribute.LocalName;
        }

        return attribute;
    }

    internal void ParseXmlDeclarationToken(int start)
    {
        int endMarker = this.Text.IndexOf(
            "?>",
            this.Position,
            System.StringComparison.Ordinal);
        if (endMarker < 0)
        {
            this.HandleUnterminated(
                "ASNXER0044",
                "The XML declaration is unterminated.",
                start);
            return;
        }

        string content = this.Text.Substring(
            this.Position,
            endMarker - this.Position);
        if (content.Length == 0
            || !this.IsXmlWhitespace(content[0]))
        {
            this.FailParse(
                "ASNXER0045",
                "The XML declaration requires version information.",
                this.Position,
                false);
            return;
        }

        int offset = 0;
        XerXmlDeclarationAttribute version =
            this.ReadDeclarationAttribute(content, offset);
        if (!version.Success
            || version.Name != "version"
            || version.Value != "1.0")
        {
            this.FailParse(
                "ASNXER0045",
                "The XML declaration must begin with version='1.0'.",
                this.Position,
                false);
            return;
        }

        offset = version.NextOffset;
        string encoding = "";
        bool hasStandalone = false;
        bool standalone = false;
        XerXmlDeclarationAttribute next =
            this.ReadDeclarationAttribute(content, offset);
        if (next.Success && next.Name == "encoding")
        {
            if (!string.Equals(
                next.Value,
                "UTF-8",
                System.StringComparison.OrdinalIgnoreCase))
            {
                this.FailParse(
                    "ASNXER0046",
                    "The XML declaration encoding must be UTF-8.",
                    this.Position + offset,
                    false);
                return;
            }

            encoding = next.Value;
            offset = next.NextOffset;
            next = this.ReadDeclarationAttribute(content, offset);
        }

        if (next.Success && next.Name == "standalone")
        {
            if (next.Value != "yes" && next.Value != "no")
            {
                this.FailParse(
                    "ASNXER0047",
                    "The XML declaration standalone value must be 'yes' or 'no'.",
                    this.Position + offset,
                    false);
                return;
            }

            hasStandalone = true;
            standalone = next.Value == "yes";
            offset = next.NextOffset;
            next = this.ReadDeclarationAttribute(content, offset);
        }

        int trailing = offset;
        while (trailing < content.Length
            && this.IsXmlWhitespace(content[trailing]))
        {
            trailing = trailing + 1;
        }

        if (next.Success || trailing != content.Length)
        {
            this.FailParse(
                "ASNXER0048",
                "The XML declaration contains an invalid or out-of-order pseudo-attribute.",
                this.Position + offset,
                false);
            return;
        }

        int end = endMarker + 2;
        XerXmlToken token = new XerXmlToken
        {
            Kind = XerXmlTokenKind.XmlDeclaration,
            Value = content,
            XmlVersion = "1.0",
            XmlEncoding = encoding,
            HasStandalone = hasStandalone,
            Standalone = standalone,
            ByteOffset = this.ToByteOffset(start),
            ByteLength = this.ToByteOffset(end) - this.ToByteOffset(start)
        };
        this.DeclarationSeen = true;
        this.Position = end;
        this.AddToken(token);
    }

    internal XerXmlDeclarationAttribute ReadDeclarationAttribute(
        string content,
        int offset)
    {
        int index = offset;
        bool separated = false;
        while (index < content.Length
            && this.IsXmlWhitespace(content[index]))
        {
            separated = true;
            index = index + 1;
        }

        if (!separated || index >= content.Length)
        {
            return new XerXmlDeclarationAttribute { NextOffset = index };
        }

        int nameStart = index;
        while (index < content.Length
            && (content[index] >= 'a' && content[index] <= 'z'
                || content[index] >= 'A' && content[index] <= 'Z'))
        {
            index = index + 1;
        }

        if (index == nameStart)
        {
            return new XerXmlDeclarationAttribute { NextOffset = index };
        }

        string name = content.Substring(nameStart, index - nameStart);
        while (index < content.Length
            && this.IsXmlWhitespace(content[index]))
        {
            index = index + 1;
        }

        if (index >= content.Length || content[index] != '=')
        {
            return new XerXmlDeclarationAttribute { NextOffset = index };
        }

        index = index + 1;
        while (index < content.Length
            && this.IsXmlWhitespace(content[index]))
        {
            index = index + 1;
        }

        if (index >= content.Length
            || content[index] != '\'' && content[index] != '"')
        {
            return new XerXmlDeclarationAttribute { NextOffset = index };
        }

        char quote = content[index];
        index = index + 1;
        int valueStart = index;
        int valueEnd = content.IndexOf(quote, valueStart);
        if (valueEnd < 0)
        {
            return new XerXmlDeclarationAttribute { NextOffset = index };
        }

        return new XerXmlDeclarationAttribute
        {
            Success = true,
            Name = name,
            Value = content.Substring(valueStart, valueEnd - valueStart),
            NextOffset = valueEnd + 1
        };
    }

    internal bool ResolveStartElement(
        XerXmlToken token,
        int start,
        bool empty)
    {
        XerXmlNamespaceBinding[] inherited = [];
        if (this.ElementStack.Count > 0)
        {
            inherited = this.ElementStack[
                this.ElementStack.Count - 1].NamespaceBindings;
        }

        XerXmlNamespaceContext context =
            new XerXmlNamespaceContext(inherited);
        System.Collections.Generic.List<string> declaredPrefixes =
            new System.Collections.Generic.List<string>();
        int index = 0;
        while (index < token.Attributes.Length)
        {
            XerXmlAttribute attribute = token.Attributes[index];
            if (attribute.NamespaceDeclaration)
            {
                string declaredPrefix = attribute.NamespacePrefix;
                if (!this.ValidateNamespaceDeclaration(
                    declaredPrefix,
                    attribute.Value,
                    declaredPrefixes,
                    start))
                {
                    return false;
                }

                declaredPrefixes.Add(declaredPrefix);
                context.SetBinding(declaredPrefix, attribute.Value);
                attribute.NamespaceUri = XerXmlNamespaceContext.XmlnsNamespace;
            }

            index = index + 1;
        }

        XerXmlNamespaceBinding[] bindings = context.CopyBindings();
        XerXmlQualifiedName element =
            this.NamespaceResolver.ResolveElementName(token.Name, bindings);
        if (!element.Success)
        {
            this.FailParse(
                element.ErrorCode,
                element.ErrorMessage,
                start + 1,
                false);
            return false;
        }

        token.Prefix = element.Prefix;
        token.LocalName = element.LocalName;
        token.NamespaceUri = element.NamespaceUri;
        token.NamespaceBindings = bindings;
        System.Collections.Generic.List<string> attributeNames =
            new System.Collections.Generic.List<string>();
        index = 0;
        while (index < token.Attributes.Length)
        {
            XerXmlAttribute attribute = token.Attributes[index];
            string duplicateKey;
            if (attribute.NamespaceDeclaration)
            {
                duplicateKey = XerXmlNamespaceContext.XmlnsNamespace
                    + "\u0001" + attribute.LocalName;
            }
            else
            {
                XerXmlQualifiedName resolved =
                    this.NamespaceResolver.ResolveAttributeName(
                        attribute.Name,
                        bindings);
                if (!resolved.Success)
                {
                    this.FailParse(
                        resolved.ErrorCode,
                        resolved.ErrorMessage,
                        start,
                        false);
                    return false;
                }

                attribute.Prefix = resolved.Prefix;
                attribute.LocalName = resolved.LocalName;
                attribute.NamespaceUri = resolved.NamespaceUri;
                duplicateKey = resolved.NamespaceUri
                    + "\u0001" + resolved.LocalName;
            }

            if (attributeNames.Contains(duplicateKey))
            {
                this.FailParse(
                    "ASNXER0049",
                    "The XML start element contains duplicate attributes.",
                    start,
                    false);
                return false;
            }

            attributeNames.Add(duplicateKey);
            index = index + 1;
        }

        bool documentLevel = this.ElementStack.Count == 0;
        if (documentLevel && this.RootSeen)
        {
            this.FailParse(
                "ASNXER0050",
                "An XML document must contain exactly one document element.",
                start,
                false);
            return false;
        }

        if (documentLevel)
        {
            this.RootSeen = true;
        }

        if (!empty)
        {
            this.ElementStack.Add(new XerXmlElementFrame
            {
                Name = token.Name,
                Prefix = token.Prefix,
                LocalName = token.LocalName,
                NamespaceUri = token.NamespaceUri,
                NamespaceBindings = bindings
            });
        }
        else if (documentLevel)
        {
            this.RootClosed = true;
        }

        return true;
    }

    internal bool ValidateNamespaceDeclaration(
        string prefix,
        string namespaceUri,
        System.Collections.Generic.List<string> declaredPrefixes,
        int offset)
    {
        if (declaredPrefixes.Contains(prefix))
        {
            this.FailParse(
                "ASNXER0049",
                "The XML start element declares a namespace prefix more than once.",
                offset,
                false);
            return false;
        }

        if (prefix.Length > 0 && !this.Characters.IsValidNcName(prefix)
            || prefix == "xmlns"
            || namespaceUri == XerXmlNamespaceContext.XmlnsNamespace
            || prefix == "xml"
                && namespaceUri != XerXmlNamespaceContext.XmlNamespace
            || prefix != "xml"
                && namespaceUri == XerXmlNamespaceContext.XmlNamespace
            || prefix.Length > 0 && namespaceUri.Length == 0)
        {
            this.FailParse(
                "ASNXER0051",
                "The XML namespace declaration violates reserved-prefix rules.",
                offset,
                false);
            return false;
        }

        return true;
    }

    internal void ValidateCompleteDocument()
    {
        if (!this.RootSeen)
        {
            this.FailParse(
                "ASNXER0052",
                "An XML document requires a document element.",
                this.Position,
                false);
        }
        else if (this.ElementStack.Count > 0 || !this.RootClosed)
        {
            this.FailParse(
                "ASNXER0053",
                "The XML document ends before all elements are closed.",
                this.Position,
                false);
        }
    }

    internal bool IsOnlyXmlWhitespace(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (!this.IsXmlWhitespace(value[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal string ReadName()
    {
        if (this.Position >= this.Text.Length)
        {
            this.HandleUnterminated(
                "ASNXER0020",
                "The XML name is incomplete.",
                this.Position);
            return "";
        }

        int start = this.Position;
        XerXmlScalar first = this.Characters.ReadScalar(
            this.Text,
            this.Position);
        if (!first.Success || !this.Characters.IsNameStart(first.Value))
        {
            return "";
        }

        this.Position = this.Position + first.CharacterCount;
        while (this.Position < this.Text.Length)
        {
            XerXmlScalar current = this.Characters.ReadScalar(
                this.Text,
                this.Position);
            if (!current.Success)
            {
                this.FailParse(
                    "ASNXER0021",
                    "The XML name contains an invalid Unicode scalar.",
                    this.Position,
                    false);
                return "";
            }

            if (!this.Characters.IsNameCharacter(current.Value))
            {
                break;
            }

            this.Position = this.Position + current.CharacterCount;
        }

        return this.Text.Substring(start, this.Position - start);
    }

    internal XerXmlDecodedText DecodeReferences(
        string raw,
        int absoluteStart)
    {
        TextAccumulator output = new TextAccumulator(raw.Length);
        int index = 0;
        while (index < raw.Length)
        {
            if (raw[index] != '&')
            {
                output.AppendCharacter(raw[index]);
                index = index + 1;
                continue;
            }

            int semicolon = raw.IndexOf(';', index + 1);
            if (semicolon < 0)
            {
                return this.FinalBlock
                    ? this.CreateDecodedError(
                        "ASNXER0022",
                        "The XML reference is unterminated.",
                        absoluteStart + index)
                    : new XerXmlDecodedText { NeedMoreData = true };
            }

            string reference = raw.Substring(
                index + 1,
                semicolon - index - 1);
            XerXmlDecodedText decoded = this.DecodeReference(
                reference,
                absoluteStart + index);
            if (!decoded.Success)
            {
                return decoded;
            }

            output.AppendText(decoded.Value);
            index = semicolon + 1;
        }

        return new XerXmlDecodedText
        {
            Success = true,
            Value = output.ToText()
        };
    }

    internal XerXmlDecodedText DecodeReference(
        string reference,
        int violationOffset)
    {
        if (reference == "amp")
        {
            return this.CreateDecodedSuccess("&");
        }

        if (reference == "lt")
        {
            return this.CreateDecodedSuccess("<");
        }

        if (reference == "gt")
        {
            return this.CreateDecodedSuccess(">");
        }

        if (reference == "apos")
        {
            return this.CreateDecodedSuccess("'");
        }

        if (reference == "quot")
        {
            return this.CreateDecodedSuccess("\"");
        }

        bool hexadecimal = reference.StartsWith(
            "#x",
            System.StringComparison.Ordinal);
        bool decimalReference = reference.StartsWith(
            "#",
            System.StringComparison.Ordinal);
        if (!hexadecimal && !decimalReference)
        {
            return this.CreateDecodedError(
                "ASNXER0023",
                "The XML entity reference is not predefined.",
                violationOffset);
        }

        int digitStart = hexadecimal ? 2 : 1;
        int radix = hexadecimal ? 16 : 10;
        if (digitStart >= reference.Length)
        {
            return this.CreateDecodedError(
                "ASNXER0024",
                "The XML character reference has no digits.",
                violationOffset);
        }

        int value = 0;
        int index = digitStart;
        while (index < reference.Length)
        {
            int digit = this.GetReferenceDigit(reference[index], radix);
            if (digit < 0 || value > (1114111 - digit) / radix)
            {
                return this.CreateDecodedError(
                    "ASNXER0024",
                    "The XML character reference is invalid.",
                    violationOffset);
            }

            value = value * radix + digit;
            index = index + 1;
        }

        if (!this.Characters.IsXmlCharacter(value))
        {
            return this.CreateDecodedError(
                "ASNXER0025",
                "The XML character reference denotes a forbidden XML character.",
                violationOffset);
        }

        return this.CreateDecodedSuccess(
            System.Char.ConvertFromUtf32(value));
    }

    internal int GetReferenceDigit(char value, int radix)
    {
        int digit = value >= '0' && value <= '9'
            ? value - '0'
            : value >= 'A' && value <= 'F'
                ? value - 'A' + 10
                : value >= 'a' && value <= 'f'
                    ? value - 'a' + 10
                    : -1;
        return digit >= 0 && digit < radix ? digit : -1;
    }

    internal bool CheckTextValue(string value, int offset)
    {
        if (!this.Characters.IsValidText(value))
        {
            this.FailParse(
                "ASNXER0026",
                "The XML value contains a forbidden Unicode character.",
                offset,
                false);
            return false;
        }

        if (!this.ParseResources.AllowsTextCharacters(value.Length))
        {
            this.FailParse(
                "ASNR0009",
                "The XML value exceeds the configured text-character limit.",
                offset,
                true);
            return false;
        }

        return true;
    }

    internal void AddToken(XerXmlToken token)
    {
        if (this.ParseFailed)
        {
            return;
        }

        if (!this.ParseResources.AllowsCollectionElements(
            this.ParsedTokens.Count + 1))
        {
            this.FailParse(
                "ASNR0011",
                "The XML token count exceeds the configured limit.",
                this.Position,
                true);
            return;
        }

        this.ParsedTokens.Add(token);
    }

    internal XerXmlToken CreateNamedToken(
        XerXmlTokenKind kind,
        string name,
        int start,
        int end)
    {
        XerXmlToken token = new XerXmlToken
        {
            Kind = kind,
            Name = name,
            ByteOffset = this.ToByteOffset(start),
            ByteLength = this.ToByteOffset(end) - this.ToByteOffset(start)
        };
        this.SplitTokenName(name, token);
        return token;
    }

    internal XerXmlToken CreateValueToken(
        XerXmlTokenKind kind,
        string value,
        int start,
        int end)
    {
        return new XerXmlToken
        {
            Kind = kind,
            Value = value,
            ByteOffset = this.ToByteOffset(start),
            ByteLength = this.ToByteOffset(end) - this.ToByteOffset(start)
        };
    }

    internal void SplitTokenName(string name, XerXmlToken token)
    {
        int separator = name.IndexOf(':');
        token.Prefix = separator < 0 ? "" : name.Substring(0, separator);
        token.LocalName = separator < 0
            ? name
            : name.Substring(separator + 1);
    }

    internal void SplitAttributeName(
        string name,
        XerXmlAttribute attribute)
    {
        int separator = name.IndexOf(':');
        attribute.Prefix = separator < 0
            ? ""
            : name.Substring(0, separator);
        attribute.LocalName = separator < 0
            ? name
            : name.Substring(separator + 1);
    }

    internal int ToByteOffset(int characterOffset)
    {
        if (characterOffset <= 0)
        {
            return this.TextByteBase;
        }

        return this.TextByteBase + System.Text.Encoding.UTF8.GetByteCount(
            this.Text.Substring(0, characterOffset));
    }

    internal int GetTextCharacterLimitOffset()
    {
        int characterOffset = this.Limits.MaximumTextCharacters;
        if (characterOffset < 0)
        {
            characterOffset = 0;
        }
        else if (characterOffset > this.Text.Length)
        {
            characterOffset = this.Text.Length;
        }

        return this.ToByteOffset(characterOffset);
    }

    internal XerXmlToken[] GetNewTokens()
    {
        if (this.ParsedTokens.Count <= this.EmittedTokenCount)
        {
            return [];
        }

        int count = this.ParsedTokens.Count - this.EmittedTokenCount;
        XerXmlToken[] result = new XerXmlToken[count];
        int index = 0;
        while (index < count)
        {
            result[index] = this.ParsedTokens[this.EmittedTokenCount + index];
            index = index + 1;
        }

        this.EmittedTokenCount = this.ParsedTokens.Count;
        return result;
    }

    internal bool StartsWithAt(string marker, int offset)
    {
        if (offset < 0 || marker.Length > this.Text.Length - offset)
        {
            return false;
        }

        return string.CompareOrdinal(
            this.Text,
            offset,
            marker,
            0,
            marker.Length) == 0;
    }

    internal bool IsPossibleMarkupPrefix()
    {
        string remaining = this.Text.Substring(this.Position);
        return !this.FinalBlock
            && ("<!--".StartsWith(
                    remaining,
                    System.StringComparison.Ordinal)
                || "<![CDATA[".StartsWith(
                    remaining,
                    System.StringComparison.Ordinal)
                || "<?".StartsWith(
                    remaining,
                    System.StringComparison.Ordinal)
                || "</".StartsWith(
                    remaining,
                    System.StringComparison.Ordinal));
    }

    internal void SkipXmlWhitespace()
    {
        while (this.Position < this.Text.Length
            && this.IsXmlWhitespace(this.Text[this.Position]))
        {
            this.Position = this.Position + 1;
        }
    }

    internal bool IsXmlWhitespace(char value)
    {
        return value == ' ' || value == '\t'
            || value == '\r' || value == '\n';
    }

    internal void HandleUnterminated(
        string code,
        string message,
        int offset)
    {
        if (this.FinalBlock)
        {
            this.FailParse(code, message, offset, false);
        }
        else
        {
            this.ParseIncomplete = true;
        }
    }

    internal void ApplyDecodedFailure(XerXmlDecodedText decoded)
    {
        if (decoded.NeedMoreData && !this.FinalBlock)
        {
            this.ParseIncomplete = true;
            return;
        }

        this.FailParse(
            decoded.ErrorCode,
            decoded.ErrorMessage,
            decoded.ViolationOffset,
            false);
    }

    internal XerXmlDecodedText CreateDecodedSuccess(string value)
    {
        return new XerXmlDecodedText
        {
            Success = true,
            Value = value
        };
    }

    internal XerXmlDecodedText CreateDecodedError(
        string code,
        string message,
        int violationOffset)
    {
        return new XerXmlDecodedText
        {
            ErrorCode = code,
            ErrorMessage = message,
            ViolationOffset = violationOffset
        };
    }

    internal void FailParse(
        string code,
        string message,
        int characterOffset,
        bool limitExceeded)
    {
        if (this.ParseFailed)
        {
            return;
        }

        this.ParseFailed = true;
        this.ParseFailure = this.CreateReadError(
            code,
            message,
            limitExceeded,
            this.ToByteOffset(characterOffset),
            0);
    }

    internal XerXmlReadResult CreateReadError(
        string code,
        string message,
        bool limitExceeded,
        int violationOffset,
        int consumedCount)
    {
        return new XerXmlReadResult
        {
            Status = AsnDecodeStatus.Error,
            ConsumedCount = consumedCount,
            LimitExceeded = limitExceeded,
            ErrorCode = code,
            ErrorMessage = message,
            ViolationOffset = violationOffset
        };
    }

    internal XerXmlReadResult SetTerminalReadError(
        string code,
        string message,
        bool limitExceeded,
        int violationOffset,
        int consumedCount)
    {
        XerXmlReadResult result = this.CreateReadError(
            code,
            message,
            limitExceeded,
            violationOffset,
            consumedCount);
        this.SetTerminalReadResult(result);
        return result;
    }

    internal void SetTerminalReadResult(XerXmlReadResult result)
    {
        this.Terminal = true;
        this.TerminalResult = result;
    }

    internal XerXmlReadResult CopyTerminalReadResult()
    {
        return new XerXmlReadResult
        {
            Status = this.TerminalResult.Status,
            Complete = this.TerminalResult.Complete,
            LimitExceeded = this.TerminalResult.LimitExceeded,
            ErrorCode = this.TerminalResult.ErrorCode,
            ErrorMessage = this.TerminalResult.ErrorMessage,
            ViolationOffset = this.TerminalResult.ViolationOffset
        };
    }
}
