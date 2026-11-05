namespace asn1;

internal class XsdMappingValidator
{
    internal AsnResourceLimits Limits = new AsnResourceLimits();
    internal System.Collections.Generic.List<AsnDiagnostic> Diagnostics =
        new System.Collections.Generic.List<AsnDiagnostic>();
    internal System.Collections.Generic.List<string> SeenDocuments =
        new System.Collections.Generic.List<string>();
    internal XsdSimpleTypeMapper SimpleMapper = new XsdSimpleTypeMapper();

    internal XsdMappingValidationResult ValidateGraph(
        XsdSchemaGraphResult graph,
        AsnXsdImportOptions options)
    {
        if (graph == null || options == null)
        {
            throw new System.ArgumentNullException("mapping validation input");
        }
        this.Limits = options.ResourceLimits == null
            ? new AsnResourceLimits()
            : options.ResourceLimits;
        this.Diagnostics =
            new System.Collections.Generic.List<AsnDiagnostic>();
        this.SeenDocuments = new System.Collections.Generic.List<string>();
        int index = 0;
        while (index < graph.DocumentInstances.Length)
        {
            XsdLoadedDocument? document =
                graph.DocumentInstances[index].Document;
            if (document != null && !this.HasSeenDocument(document.StableId))
            {
                this.SeenDocuments.Add(document.StableId);
                this.ValidateDocument(document);
            }
            index = index + 1;
        }
        return new XsdMappingValidationResult
        {
            Success = this.Diagnostics.Count == 0,
            Diagnostics = this.Diagnostics.ToArray()
        };
    }

    internal bool HasSeenDocument(string stableId)
    {
        int index = 0;
        while (index < this.SeenDocuments.Count)
        {
            if (this.SeenDocuments[index] == stableId)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal void ValidateDocument(XsdLoadedDocument document)
    {
        int index = 0;
        while (index < document.Tokens.Length)
        {
            XerXmlToken token = document.Tokens[index];
            if ((token.Kind == XerXmlTokenKind.StartElement
                    || token.Kind == XerXmlTokenKind.EmptyElement)
                && token.NamespaceUri == XsdDocumentLoader.SchemaNamespace)
            {
                this.ValidateUnsupportedConstructs(document, token);
                if (token.LocalName == "element"
                    || token.LocalName == "attribute")
                {
                    this.ValidateDeclaration(document, token);
                }
                else if (token.LocalName == "complexType")
                {
                    this.ValidateComplexType(document, token);
                }
                else if (token.LocalName == "any"
                    || token.LocalName == "anyAttribute")
                {
                    this.ValidateWildcard(document, token);
                }
                else if (token.LocalName == "all")
                {
                    this.ValidateAll(document, token);
                }
            }
            index = index + 1;
        }
    }

    internal void ValidateUnsupportedConstructs(
        XsdLoadedDocument document,
        XerXmlToken token)
    {
        if (this.IsUnsupportedElement(token.LocalName))
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.UnmappableConstruct,
                "The XML Schema construct '" + token.LocalName
                    + "' is not mappable by X.694.",
                this.CreateSpan(document, token),
                null,
                "");
        }
        int index = 0;
        while (index < token.Attributes.Length)
        {
            XerXmlAttribute attribute = token.Attributes[index];
            if (!attribute.NamespaceDeclaration
                && (attribute.NamespaceUri.Length == 0
                    || attribute.NamespaceUri
                        == XsdDocumentLoader.SchemaNamespace)
                && this.IsUnsupportedAttribute(attribute.LocalName))
            {
                this.AddDiagnostic(
                    XsdDiagnosticCodes.UnmappableConstruct,
                    "The XML Schema attribute '" + attribute.LocalName
                        + "' on '" + token.LocalName
                        + "' is not mappable by X.694.",
                    this.CreateSpan(document, token),
                    null,
                    "");
            }
            index = index + 1;
        }
    }

    internal bool IsUnsupportedElement(string localName)
    {
        return localName == "assert"
            || localName == "alternative"
            || localName == "openContent"
            || localName == "defaultOpenContent"
            || localName == "override"
            || localName == "notation"
            || localName == "unique"
            || localName == "key"
            || localName == "keyref";
    }

    internal bool IsUnsupportedAttribute(string localName)
    {
        return localName == "notNamespace"
            || localName == "notQName"
            || localName == "xpathDefaultNamespace"
            || localName == "defaultAttributes"
            || localName == "inheritable";
    }

    internal void ValidateDeclaration(
        XsdLoadedDocument document,
        XerXmlToken token)
    {
        XerXmlAttribute? typeAttribute = this.FindAttribute(token, "type");
        XerXmlToken? simpleType = this.SimpleMapper.FindDirectChild(
            document,
            token,
            "simpleType");
        XerXmlToken? complexType = this.SimpleMapper.FindDirectChild(
            document,
            token,
            "complexType");
        if (typeAttribute != null && simpleType != null)
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.InvalidSchemaStructure,
                "An XML Schema declaration cannot specify both a type attribute and an inline simpleType.",
                this.CreateSpan(document, simpleType),
                this.CreateSpan(document, token),
                "The conflicting type attribute is on this declaration.");
        }
        if (typeAttribute != null && complexType != null)
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.InvalidSchemaStructure,
                "An XML Schema declaration cannot specify both a type attribute and an inline complexType.",
                this.CreateSpan(document, complexType),
                this.CreateSpan(document, token),
                "The conflicting type attribute is on this declaration.");
        }
        if (simpleType != null && complexType != null)
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.InvalidSchemaStructure,
                "An XML Schema declaration cannot contain both simpleType and complexType children.",
                this.CreateSpan(document, complexType),
                this.CreateSpan(document, simpleType),
                "The competing inline type is declared here.");
        }
    }

    internal void ValidateComplexType(
        XsdLoadedDocument document,
        XerXmlToken token)
    {
        XerXmlToken[] children = this.SimpleMapper.GetDirectChildren(
            document,
            token);
        XerXmlToken? firstContent = null;
        int index = 0;
        while (index < children.Length)
        {
            XerXmlToken child = children[index];
            if (child.NamespaceUri == XsdDocumentLoader.SchemaNamespace
                && this.IsComplexContentForm(child.LocalName))
            {
                if (firstContent == null)
                {
                    firstContent = child;
                }
                else
                {
                    this.AddDiagnostic(
                        XsdDiagnosticCodes.InvalidSchemaStructure,
                        "An XML Schema complexType cannot contain competing content models.",
                        this.CreateSpan(document, child),
                        this.CreateSpan(document, firstContent),
                        "The first content model is declared here.");
                }
            }
            index = index + 1;
        }
    }

    internal bool IsComplexContentForm(string localName)
    {
        return localName == "simpleContent"
            || localName == "complexContent"
            || localName == "sequence"
            || localName == "choice"
            || localName == "all"
            || localName == "group";
    }

    internal void ValidateWildcard(
        XsdLoadedDocument document,
        XerXmlToken token)
    {
        XerXmlAttribute? attribute = this.FindAttribute(token, "namespace");
        if (attribute == null)
        {
            return;
        }
        string[] values = attribute.Value.Split(
            (char[]?)null,
            System.StringSplitOptions.RemoveEmptyEntries);
        bool invalid = values.Length == 0;
        int index = 0;
        while (index < values.Length)
        {
            string value = values[index];
            if ((value == "##any" || value == "##other")
                && values.Length != 1)
            {
                invalid = true;
            }
            else if (value.StartsWith("##", System.StringComparison.Ordinal)
                && value != "##any"
                && value != "##other"
                && value != "##targetNamespace"
                && value != "##local")
            {
                invalid = true;
            }
            int duplicateIndex = 0;
            while (duplicateIndex < index)
            {
                if (values[duplicateIndex] == value)
                {
                    invalid = true;
                }
                duplicateIndex = duplicateIndex + 1;
            }
            index = index + 1;
        }
        if (invalid)
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.InvalidNamespaceConstraint,
                "The XML Schema wildcard namespace constraint '"
                    + attribute.Value + "' is invalid or not mappable by X.694.",
                this.CreateSpan(document, token),
                null,
                "");
        }
    }

    internal void ValidateAll(
        XsdLoadedDocument document,
        XerXmlToken token)
    {
        this.ValidateAllOccurrence(document, token, false);
        XerXmlToken[] children = this.SimpleMapper.GetDirectChildren(
            document,
            token);
        int index = 0;
        while (index < children.Length)
        {
            XerXmlToken child = children[index];
            if (child.NamespaceUri == XsdDocumentLoader.SchemaNamespace
                && child.LocalName == "element")
            {
                this.ValidateAllOccurrence(document, child, true);
            }
            index = index + 1;
        }
    }

    internal void ValidateAllOccurrence(
        XsdLoadedDocument document,
        XerXmlToken token,
        bool child)
    {
        XerXmlAttribute? minimum = this.FindAttribute(token, "minOccurs");
        XerXmlAttribute? maximum = this.FindAttribute(token, "maxOccurs");
        bool invalidMinimum = minimum != null
            && minimum.Value != "0" && minimum.Value != "1";
        bool invalidMaximum = maximum != null && maximum.Value != "1";
        if (invalidMinimum || invalidMaximum)
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.InvalidSchemaStructure,
                child
                    ? "An element inside an all model group must have minOccurs of zero or one and maxOccurs of one."
                    : "An all model group must have minOccurs of zero or one and maxOccurs of one.",
                this.CreateSpan(document, token),
                null,
                "");
        }
    }

    internal XerXmlAttribute? FindAttribute(
        XerXmlToken token,
        string localName)
    {
        int index = 0;
        while (index < token.Attributes.Length)
        {
            XerXmlAttribute attribute = token.Attributes[index];
            if (!attribute.NamespaceDeclaration
                && attribute.LocalName == localName)
            {
                return attribute;
            }
            index = index + 1;
        }
        return null;
    }

    internal AsnSourceSpan? CreateSpan(
        XsdLoadedDocument document,
        XerXmlToken token)
    {
        return document.Input == null
            ? null
            : new XsdDocumentLoader().CreateTokenSpan(document.Input, token);
    }

    internal void AddDiagnostic(
        int code,
        string message,
        AsnSourceSpan? span,
        AsnSourceSpan? relatedSpan,
        string relatedMessage)
    {
        if (this.Diagnostics.Count >= this.Limits.MaximumDiagnostics)
        {
            return;
        }
        AsnDiagnosticRelatedLocation[] relatedLocations = [];
        if (relatedSpan != null)
        {
            relatedLocations =
            [
                new AsnDiagnosticRelatedLocation
                {
                    Message = relatedMessage,
                    Span = relatedSpan
                }
            ];
        }
        this.Diagnostics.Add(new AsnDiagnostic
        {
            Severity = AsnDiagnosticSeverity.Error,
            Category = AsnDiagnosticCategory.Xsd,
            Code = DiagnosticBag.CreateCode(AsnDiagnosticCategory.Xsd, code),
            Message = message,
            Span = span,
            RelatedLocations = relatedLocations
        });
    }
}
