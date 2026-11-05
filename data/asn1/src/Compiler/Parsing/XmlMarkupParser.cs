namespace asn1;

internal class XmlMarkupParser
{
    internal string Text;
    internal DiagnosticBag Diagnostics;
    internal bool ReportDiagnostics;
    internal AsnToken Token;
    internal int Position;
    internal bool Failed;

    internal XmlMarkupParser(
        string text,
        AsnToken token,
        DiagnosticBag diagnostics,
        bool reportDiagnostics)
    {
        this.Text = text == null ? throw new System.ArgumentNullException("text") : text;
        this.Token = token == null ? throw new System.ArgumentNullException("token") : token;
        this.Diagnostics = diagnostics == null
            ? throw new System.ArgumentNullException("diagnostics")
            : diagnostics;
        this.ReportDiagnostics = reportDiagnostics;
    }

    internal XmlElementSyntax? ParseDocument()
    {
        System.Collections.Generic.List<XmlNamespaceBindingSyntax> bindings =
            new System.Collections.Generic.List<XmlNamespaceBindingSyntax>();
        XmlElementSyntax? root = this.ParseElement(bindings);
        if (root == null || this.Failed)
        {
            return null;
        }

        this.SkipWhitespace();
        if (this.Position != this.Text.Length)
        {
            this.AddDiagnostic(88, "Unexpected text follows the XML value.");
            return null;
        }

        return root;
    }

    internal XmlElementSyntax? ParseElement(
        System.Collections.Generic.List<XmlNamespaceBindingSyntax> inheritedBindings)
    {
        int start = this.Position;
        if (!this.ConsumeCharacter('<') || this.PeekCharacter(0) == '/')
        {
            this.AddDiagnostic(89, "Expected an XML start element.");
            return null;
        }

        string name = this.ParseName();
        if (name.Length == 0)
        {
            this.AddDiagnostic(89, "Expected an XML element name.");
            return null;
        }

        System.Collections.Generic.List<XmlNamespaceBindingSyntax> bindings =
            this.CopyBindings(inheritedBindings);
        System.Collections.Generic.List<XmlAttributeSyntax> attributes =
            new System.Collections.Generic.List<XmlAttributeSyntax>();
        this.SkipWhitespace();
        while (this.Position < this.Text.Length
            && this.PeekCharacter(0) != '>'
            && !(this.PeekCharacter(0) == '/' && this.PeekCharacter(1) == '>'))
        {
            XmlAttributeSyntax? attribute = this.ParseAttribute();
            if (attribute == null)
            {
                return null;
            }

            attributes.Add(attribute);
            if (attribute.Name == "xmlns")
            {
                this.SetBinding(bindings, "", attribute.Value);
            }
            else if (attribute.Prefix == "xmlns")
            {
                this.SetBinding(bindings, attribute.LocalName, attribute.Value);
            }

            this.SkipWhitespace();
        }

        string prefix = this.GetPrefix(name);
        string localName = this.GetLocalName(name);
        string namespaceUri = this.ResolveNamespace(bindings, prefix);
        if (prefix.Length > 0 && namespaceUri.Length == 0)
        {
            this.AddDiagnostic(90, "The XML element prefix is not declared.");
            return null;
        }

        int attributeIndex = 0;
        while (attributeIndex < attributes.Count)
        {
            XmlAttributeSyntax attribute = attributes[attributeIndex];
            if (attribute.Name == "xmlns" || attribute.Prefix == "xmlns")
            {
                attribute.NamespaceUri = "http://www.w3.org/2000/xmlns/";
            }
            else if (attribute.Prefix.Length > 0)
            {
                attribute.NamespaceUri = this.ResolveNamespace(bindings, attribute.Prefix);
                if (attribute.NamespaceUri.Length == 0)
                {
                    this.AddDiagnostic(91, "An XML attribute prefix is not declared.");
                    return null;
                }
            }

            attributeIndex = attributeIndex + 1;
        }

        bool empty = this.PeekCharacter(0) == '/' && this.PeekCharacter(1) == '>';
        if (empty)
        {
            this.Position = this.Position + 2;
            return new XmlElementSyntax
            {
                Name = name,
                Prefix = prefix,
                LocalName = localName,
                NamespaceUri = namespaceUri,
                Attributes = attributes.ToArray(),
                Empty = true,
                StartOffset = start,
                EndOffset = this.Position
            };
        }

        if (!this.ConsumeCharacter('>'))
        {
            this.AddDiagnostic(92, "An XML start element is missing '>'.");
            return null;
        }

        System.Text.StringBuilder text = new System.Text.StringBuilder();
        System.Collections.Generic.List<XmlElementSyntax> children =
            new System.Collections.Generic.List<XmlElementSyntax>();
        while (this.Position < this.Text.Length)
        {
            if (this.PeekCharacter(0) == '<' && this.PeekCharacter(1) == '/')
            {
                this.Position = this.Position + 2;
                string closingName = this.ParseName();
                this.SkipWhitespace();
                if (!this.ConsumeCharacter('>') || closingName != name)
                {
                    this.AddDiagnostic(93, "The XML closing element does not match its start element.");
                    return null;
                }

                return new XmlElementSyntax
                {
                    Name = name,
                    Prefix = prefix,
                    LocalName = localName,
                    NamespaceUri = namespaceUri,
                    Text = text.ToString(),
                    Attributes = attributes.ToArray(),
                    Children = children.ToArray(),
                    StartOffset = start,
                    EndOffset = this.Position
                };
            }

            if (this.PeekCharacter(0) == '<')
            {
                XmlElementSyntax? child = this.ParseElement(bindings);
                if (child == null)
                {
                    return null;
                }

                children.Add(child);
            }
            else
            {
                string content = this.ParseTextContent();
                if (this.Failed)
                {
                    return null;
                }

                text.Append(content);
            }
        }

        this.AddDiagnostic(94, "The XML element is missing its closing element.");
        return null;
    }

    internal XmlAttributeSyntax? ParseAttribute()
    {
        string name = this.ParseName();
        if (name.Length == 0)
        {
            this.AddDiagnostic(95, "Expected an XML attribute name.");
            return null;
        }

        this.SkipWhitespace();
        if (!this.ConsumeCharacter('='))
        {
            this.AddDiagnostic(96, "Expected '=' after an XML attribute name.");
            return null;
        }

        this.SkipWhitespace();
        char quote = this.PeekCharacter(0);
        if (quote != '\'' && quote != '"')
        {
            this.AddDiagnostic(97, "An XML attribute value must be quoted.");
            return null;
        }

        this.Position = this.Position + 1;
        System.Text.StringBuilder raw = new System.Text.StringBuilder();
        while (this.Position < this.Text.Length && this.PeekCharacter(0) != quote)
        {
            raw.Append(this.PeekCharacter(0));
            this.Position = this.Position + 1;
        }

        if (!this.ConsumeCharacter(quote))
        {
            this.AddDiagnostic(98, "An XML attribute value is unterminated.");
            return null;
        }

        return new XmlAttributeSyntax
        {
            Name = name,
            Prefix = this.GetPrefix(name),
            LocalName = this.GetLocalName(name),
            Value = this.DecodeEntities(raw.ToString())
        };
    }

    internal string ParseTextContent()
    {
        System.Text.StringBuilder raw = new System.Text.StringBuilder();
        while (this.Position < this.Text.Length && this.PeekCharacter(0) != '<')
        {
            raw.Append(this.PeekCharacter(0));
            this.Position = this.Position + 1;
        }

        return this.DecodeEntities(raw.ToString());
    }

    internal string DecodeEntities(string value)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] != '&')
            {
                result.Append(value[index]);
                index = index + 1;
                continue;
            }

            int semicolon = value.IndexOf(';', index + 1);
            if (semicolon < 0)
            {
                this.AddDiagnostic(99, "An XML entity reference is unterminated.");
                return "";
            }

            string entity = value.Substring(index + 1, semicolon - index - 1);
            string decoded = this.DecodeEntity(entity);
            if (this.Failed)
            {
                return "";
            }

            result.Append(decoded);
            index = semicolon + 1;
        }

        return result.ToString();
    }

    internal string DecodeEntity(string entity)
    {
        switch (entity)
        {
            case "amp":
                return "&";
            case "lt":
                return "<";
            case "gt":
                return ">";
            case "quot":
                return "\"";
            case "apos":
                return "'";
        }

        bool hexadecimal = entity.Length > 2 && entity[0] == '#' && entity[1] == 'x';
        int start = hexadecimal ? 2 : 1;
        if (entity.Length <= start || entity[0] != '#')
        {
            this.AddDiagnostic(100, "The XML entity reference is not recognized.");
            return "";
        }

        int scalar = 0;
        int index = start;
        while (index < entity.Length)
        {
            int digit = hexadecimal ? this.GetHexDigit(entity[index]) : this.GetDecimalDigit(entity[index]);
            if (digit < 0 || scalar > 1114111 / (hexadecimal ? 16 : 10))
            {
                this.AddDiagnostic(100, "The XML numeric entity is outside the Unicode range.");
                return "";
            }

            scalar = scalar * (hexadecimal ? 16 : 10) + digit;
            index = index + 1;
        }

        bool validXmlScalar = scalar == 9
            || scalar == 10
            || scalar == 13
            || scalar >= 32 && scalar <= 55295
            || scalar >= 57344 && scalar <= 65533
            || scalar >= 65536 && scalar <= 1114111;
        if (!validXmlScalar)
        {
            this.AddDiagnostic(100, "The XML numeric entity is outside the Unicode range.");
            return "";
        }

        return char.ConvertFromUtf32(scalar);
    }

    internal int GetHexDigit(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }

        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }

        return value >= 'a' && value <= 'f' ? value - 'a' + 10 : -1;
    }

    internal int GetDecimalDigit(char value)
    {
        return value >= '0' && value <= '9' ? value - '0' : -1;
    }

    internal string ParseName()
    {
        int start = this.Position;
        while (this.Position < this.Text.Length && this.IsNameCharacter(this.Text[this.Position]))
        {
            this.Position = this.Position + 1;
        }

        return this.Position == start ? "" : this.Text.Substring(start, this.Position - start);
    }

    internal bool IsNameCharacter(char value)
    {
        return value >= 'A' && value <= 'Z'
            || value >= 'a' && value <= 'z'
            || value >= '0' && value <= '9'
            || value == '_'
            || value == '-'
            || value == '.'
            || value == ':';
    }

    internal string GetPrefix(string name)
    {
        int colon = name.IndexOf(':');
        return colon < 0 ? "" : name.Substring(0, colon);
    }

    internal string GetLocalName(string name)
    {
        int colon = name.IndexOf(':');
        return colon < 0 ? name : name.Substring(colon + 1);
    }

    internal string ResolveNamespace(
        System.Collections.Generic.List<XmlNamespaceBindingSyntax> bindings,
        string prefix)
    {
        if (prefix == "xml")
        {
            return "http://www.w3.org/XML/1998/namespace";
        }

        int index = bindings.Count - 1;
        while (index >= 0)
        {
            if (bindings[index].Prefix == prefix)
            {
                return bindings[index].NamespaceUri;
            }

            index = index - 1;
        }

        return "";
    }

    internal void SetBinding(
        System.Collections.Generic.List<XmlNamespaceBindingSyntax> bindings,
        string prefix,
        string namespaceUri)
    {
        bindings.Add(new XmlNamespaceBindingSyntax
        {
            Prefix = prefix,
            NamespaceUri = namespaceUri
        });
    }

    internal System.Collections.Generic.List<XmlNamespaceBindingSyntax> CopyBindings(
        System.Collections.Generic.List<XmlNamespaceBindingSyntax> source)
    {
        System.Collections.Generic.List<XmlNamespaceBindingSyntax> result =
            new System.Collections.Generic.List<XmlNamespaceBindingSyntax>();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(source[index]);
            index = index + 1;
        }

        return result;
    }

    internal void SkipWhitespace()
    {
        while (this.Position < this.Text.Length && char.IsWhiteSpace(this.Text[this.Position]))
        {
            this.Position = this.Position + 1;
        }
    }

    internal bool ConsumeCharacter(char value)
    {
        if (this.PeekCharacter(0) != value)
        {
            return false;
        }

        this.Position = this.Position + 1;
        return true;
    }

    internal char PeekCharacter(int distance)
    {
        int index = this.Position + distance;
        return index < 0 || index >= this.Text.Length ? '\0' : this.Text[index];
    }

    internal void AddDiagnostic(int number, string message)
    {
        this.Failed = true;
        if (!this.ReportDiagnostics)
        {
            return;
        }

        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Syntax,
            number,
            message,
            this.Token.Span,
            null);
    }
}
