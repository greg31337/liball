using System;
using System.Collections.Generic;

namespace xml;

internal interface IDtdAttributeDeclarationSource
{
    XmlAttributeDeclaration? FindDtdAttributeDeclaration(string elementName, string attributeName);
    XmlAttributeDeclaration[] FindDtdAttributeDeclarationsForElement(string elementName);
    XmlAttributeDeclaration[] FindAllDtdAttributeDeclarations();
    bool IsDtdAttributeDeclarationExternal(string elementName, string attributeName);
    XmlEntityDeclaration? FindDtdAttributeEntity(string name);
    bool DoesDtdAttributeDefaultUseExternalEntity(string elementName, string attributeName);
    bool HasDtdAttributeNotationDeclaration(string name);
    XmlElementDeclaration? FindDtdAttributeElementDeclaration(string name);
}

internal class DtdIdReference
{
    internal string Value;
    internal int Offset;

    internal DtdIdReference(string value, int offset)
    {
        Value = value;
        Offset = offset;
    }
}

internal class DtdAttributeProcessor
{
    internal IDtdAttributeDeclarationSource Source;
    internal XmlAttributeValueNormalizer Normalizer;
    internal DtdValidationContext ValidationContext;
    internal bool Validate;
    internal bool Standalone;
    internal Dictionary<string, int> Identifiers;
    internal List<DtdIdReference> IdReferences;

    internal DtdAttributeProcessor(IDtdAttributeDeclarationSource source, XmlAttributeValueNormalizer normalizer, DtdValidationContext validationContext, bool validate, bool standalone)
    {
        Guard.NotNull(source, "source");
        Guard.NotNull(normalizer, "normalizer");
        Guard.NotNull(validationContext, "validationContext");
        Source = source;
        Normalizer = normalizer;
        ValidationContext = validationContext;
        Validate = validate;
        Standalone = standalone;
        Identifiers = new Dictionary<string, int>(StringComparer.Ordinal);
        IdReferences = new List<DtdIdReference>();
        if (validate)
        {
            ValidateDtdAttributeDeclarations(source.FindAllDtdAttributeDeclarations());
        }
    }

    internal XmlAttributeData[] ProcessDtdElementAttributes(string elementName, XmlAttributeData[] sourceAttributes, int offset)
    {
        Guard.NotNullOrEmpty(elementName, "elementName");
        Guard.NotNull(sourceAttributes, "sourceAttributes");
        List<XmlAttributeData> attributes = new List<XmlAttributeData>();
        int index = 0;
        while (index < sourceAttributes.Length)
        {
            XmlAttributeData attribute = sourceAttributes[index];
            string attributeName = DtdAttributeQualifiedName(attribute);
            XmlAttributeDeclaration? declaration = Source.FindDtdAttributeDeclaration(elementName, attributeName);
            string rawValue = attribute.RawValue == null ? "" : attribute.RawValue;
            string basicValue = Normalizer.NormalizeXmlAttributeValue(rawValue);
            if (declaration == null)
            {
                if (Validate)
                {
                    ValidationContext.ReportDtdValidationError("Attribute '" + attributeName + "' on element '" + elementName + "' has no DTD declaration", offset);
                }

                attribute.Value = basicValue;
            }
            else
            {
                string typeName = declaration.TypeName == null ? "CDATA" : declaration.TypeName;
                string normalizedValue = typeName == "CDATA" ? basicValue : Normalizer.CollapseDtdAttributeSpaces(basicValue);
                attribute.Value = normalizedValue;
                attribute.IsFixed = declaration.DefaultKind == "#FIXED";
                if (Validate)
                {
                    if (Standalone && typeName != "CDATA" && Source.IsDtdAttributeDeclarationExternal(elementName, attributeName) && basicValue != normalizedValue)
                    {
                        ValidationContext.ReportDtdValidationError("Standalone document attribute '" + attributeName + "' depends on an external non-CDATA declaration for normalization", offset);
                    }

                    ValidateDtdAttributeValue(declaration, normalizedValue, offset);
                    if (declaration.DefaultKind == "#FIXED" && declaration.DefaultValue != null && normalizedValue != declaration.DefaultValue)
                    {
                        ValidationContext.ReportDtdValidationError("Attribute '" + attributeName + "' does not match its fixed DTD value", offset);
                    }
                }
            }

            attributes.Add(attribute);
            index = index + 1;
        }

        XmlAttributeDeclaration[] declarations = Source.FindDtdAttributeDeclarationsForElement(elementName);
        index = 0;
        while (index < declarations.Length)
        {
            XmlAttributeDeclaration declaration = declarations[index];
            string attributeName = DtdAttributeDeclarationName(declaration);
            if (!ContainsDtdAttributeName(attributes, attributeName))
            {
                if (declaration.DefaultKind == "#REQUIRED")
                {
                    if (Validate)
                    {
                        ValidationContext.ReportDtdValidationError("Required attribute '" + attributeName + "' is missing from element '" + elementName + "'", offset);
                    }
                }
                else if (declaration.DefaultKind != "#IMPLIED" && declaration.DefaultValue != null)
                {
                    if (Validate && Standalone && (Source.IsDtdAttributeDeclarationExternal(elementName, attributeName) || Source.DoesDtdAttributeDefaultUseExternalEntity(elementName, attributeName)))
                    {
                        ValidationContext.ReportDtdValidationError("Standalone document element '" + elementName + "' depends on an externally declared default attribute '" + attributeName + "'", offset);
                    }

                    if (Validate)
                    {
                        ValidateDtdAttributeValue(declaration, declaration.DefaultValue, offset);
                    }

                    attributes.Add(CreateDefaultedDtdAttribute(declaration));
                }
            }

            index = index + 1;
        }

        return attributes.ToArray();
    }

    internal void ValidateNormalizedDtdElementAttributes(string elementName, XmlAttributeData[] attributes, int offset)
    {
        Guard.NotNullOrEmpty(elementName, "elementName");
        Guard.NotNull(attributes, "attributes");
        int index = 0;
        while (index < attributes.Length)
        {
            XmlAttributeData attribute = attributes[index];
            string attributeName = DtdAttributeQualifiedName(attribute);
            XmlAttributeDeclaration? declaration = Source.FindDtdAttributeDeclaration(elementName, attributeName);
            if (declaration == null)
            {
                ValidationContext.ReportDtdValidationError("Attribute '" + attributeName + "' on element '" + elementName + "' has no DTD declaration", offset);
            }
            else
            {
                string value = attribute.Value == null ? "" : attribute.Value;
                ValidateDtdAttributeValue(declaration, value, offset);
                if (declaration.DefaultKind == "#FIXED" && declaration.DefaultValue != null && value != declaration.DefaultValue)
                {
                    ValidationContext.ReportDtdValidationError("Attribute '" + attributeName + "' does not match its fixed DTD value", offset);
                }
            }

            index = index + 1;
        }

        XmlAttributeDeclaration[] declarations = Source.FindDtdAttributeDeclarationsForElement(elementName);
        index = 0;
        while (index < declarations.Length)
        {
            XmlAttributeDeclaration declaration = declarations[index];
            string attributeName = DtdAttributeDeclarationName(declaration);
            if (declaration.DefaultKind == "#REQUIRED" && !ContainsDtdAttributeDataName(attributes, attributeName))
            {
                ValidationContext.ReportDtdValidationError("Required attribute '" + attributeName + "' is missing from element '" + elementName + "'", offset);
            }

            index = index + 1;
        }
    }

    internal void ValidateDtdAttributeDeclarations(XmlAttributeDeclaration[] declarations)
    {
        Dictionary<string, string> idAttributes = new Dictionary<string, string>(StringComparer.Ordinal);
        Dictionary<string, string> notationAttributes = new Dictionary<string, string>(StringComparer.Ordinal);
        int index = 0;
        while (index < declarations.Length)
        {
            XmlAttributeDeclaration declaration = declarations[index];
            string typeName = declaration.TypeName == null ? "CDATA" : declaration.TypeName;
            if (typeName == "ID")
            {
                string elementName = declaration.ElementName == null || declaration.ElementName.QualifiedName == null ? "" : declaration.ElementName.QualifiedName;
                if (idAttributes.ContainsKey(elementName))
                {
                    ValidationContext.ReportDtdValidationError("Element type '" + elementName + "' cannot declare more than one ID attribute", 0);
                }
                else
                {
                    idAttributes.Add(elementName, DtdAttributeDeclarationName(declaration));
                }

                if (declaration.DefaultKind != "#REQUIRED" && declaration.DefaultKind != "#IMPLIED")
                {
                    ValidationContext.ReportDtdValidationError("ID attribute '" + DtdAttributeDeclarationName(declaration) + "' must be #REQUIRED or #IMPLIED", 0);
                }
            }
            else if (typeName == "NOTATION")
            {
                string elementName = declaration.ElementName == null || declaration.ElementName.QualifiedName == null ? "" : declaration.ElementName.QualifiedName;
                if (notationAttributes.ContainsKey(elementName))
                {
                    ValidationContext.ReportDtdValidationError("Element type '" + elementName + "' cannot declare more than one NOTATION attribute", 0);
                }
                else
                {
                    notationAttributes.Add(elementName, DtdAttributeDeclarationName(declaration));
                }

                XmlElementDeclaration? element = Source.FindDtdAttributeElementDeclaration(elementName);
                if (element != null && element.IsEmpty)
                {
                    ValidationContext.ReportDtdValidationError("Element type '" + elementName + "' is EMPTY and cannot declare a NOTATION attribute", 0);
                }

                string[] notationNames = declaration.AllowedValues == null ? [] : declaration.AllowedValues;
                int notationIndex = 0;
                while (notationIndex < notationNames.Length)
                {
                    if (!Source.HasDtdAttributeNotationDeclaration(notationNames[notationIndex]))
                    {
                        ValidationContext.ReportDtdValidationError("NOTATION attribute '" + DtdAttributeDeclarationName(declaration) + "' names undeclared notation '" + notationNames[notationIndex] + "'", 0);
                    }

                    notationIndex = notationIndex + 1;
                }
            }

            if (declaration.DefaultValue != null)
            {
                ValidateDtdAttributeSyntax(declaration, declaration.DefaultValue, 0);
            }

            index = index + 1;
        }
    }

    internal void ValidateDtdAttributeValue(XmlAttributeDeclaration declaration, string value, int offset)
    {
        ValidateDtdAttributeSyntax(declaration, value, offset);
        string typeName = declaration.TypeName == null ? "CDATA" : declaration.TypeName;
        if (typeName == "ID")
        {
            if (Identifiers.ContainsKey(value))
            {
                ValidationContext.ReportDtdValidationError("ID value '" + value + "' occurs more than once", offset);
            }
            else
            {
                Identifiers.Add(value, offset);
            }
        }
        else if (typeName == "IDREF")
        {
            IdReferences.Add(new DtdIdReference(value, offset));
        }
        else if (typeName == "IDREFS")
        {
            AddDtdIdReferences(value, offset);
        }
        else if (typeName == "ENTITY")
        {
            ValidateDtdUnparsedEntity(value, declaration, offset);
        }
        else if (typeName == "ENTITIES")
        {
            string[] names = value.Split(' ');
            int index = 0;
            while (index < names.Length)
            {
                ValidateDtdUnparsedEntity(names[index], declaration, offset);
                index = index + 1;
            }
        }
    }

    internal void ValidateDtdAttributeSyntax(XmlAttributeDeclaration declaration, string value, int offset)
    {
        string typeName = declaration.TypeName == null ? "CDATA" : declaration.TypeName;
        bool valid = true;
        if (typeName == "ID" || typeName == "IDREF" || typeName == "ENTITY")
        {
            valid = IsValidDtdName(value);
        }
        else if (typeName == "IDREFS" || typeName == "ENTITIES")
        {
            valid = IsValidDtdTokenList(value, true);
        }
        else if (typeName == "NMTOKEN")
        {
            valid = IsValidDtdNmtoken(value);
        }
        else if (typeName == "NMTOKENS")
        {
            valid = IsValidDtdTokenList(value, false);
        }

        if (!valid)
        {
            ValidationContext.ReportDtdValidationError("Attribute '" + DtdAttributeDeclarationName(declaration) + "' does not match its declared " + typeName + " syntax", offset);
        }

        ValidateDtdEnumeratedAttribute(declaration, value, offset);
    }

    internal bool IsValidDtdName(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        UnicodeScalarScanner scanner = new UnicodeScalarScanner(value.AsMemory());
        UnicodeScalar scalar = scanner.ReadUnicodeScalar();
        bool validStart = Normalizer.Version == XmlVersion.Xml10 ? Xml10CharacterRules.IsXml10NameStartCharacter(scalar.Value) : Xml11CharacterRules.IsXml11NameStartCharacter(scalar.Value);
        if (!validStart)
        {
            return false;
        }

        while (!scanner.IsUnicodeScalarEnd())
        {
            scalar = scanner.ReadUnicodeScalar();
            bool valid = Normalizer.Version == XmlVersion.Xml10 ? Xml10CharacterRules.IsXml10NameCharacter(scalar.Value) : Xml11CharacterRules.IsXml11NameCharacter(scalar.Value);
            if (!valid)
            {
                return false;
            }
        }

        return true;
    }

    internal bool IsValidDtdNmtoken(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        UnicodeScalarScanner scanner = new UnicodeScalarScanner(value.AsMemory());
        while (!scanner.IsUnicodeScalarEnd())
        {
            UnicodeScalar scalar = scanner.ReadUnicodeScalar();
            bool valid = Normalizer.Version == XmlVersion.Xml10 ? Xml10CharacterRules.IsXml10NameCharacter(scalar.Value) : Xml11CharacterRules.IsXml11NameCharacter(scalar.Value);
            if (!valid)
            {
                return false;
            }
        }

        return true;
    }

    internal bool IsValidDtdTokenList(string value, bool names)
    {
        if (value.Length == 0)
        {
            return false;
        }

        string[] tokens = value.Split(' ');
        int index = 0;
        while (index < tokens.Length)
        {
            bool valid = names ? IsValidDtdName(tokens[index]) : IsValidDtdNmtoken(tokens[index]);
            if (!valid)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal void AddDtdIdReferences(string value, int offset)
    {
        string[] references = value.Split(' ');
        int index = 0;
        while (index < references.Length)
        {
            IdReferences.Add(new DtdIdReference(references[index], offset));
            index = index + 1;
        }
    }

    internal void ValidateDtdUnparsedEntity(string name, XmlAttributeDeclaration declaration, int offset)
    {
        XmlEntityDeclaration? entity = Source.FindDtdAttributeEntity(name);
        if (entity == null || entity.IsParameter || entity.IsParsed || entity.NotationName == null)
        {
            ValidationContext.ReportDtdValidationError("Attribute '" + DtdAttributeDeclarationName(declaration) + "' must name a declared unparsed entity", offset);
        }
    }

    internal void CompleteDtdAttributeValidation()
    {
        if (!Validate)
        {
            return;
        }

        int index = 0;
        while (index < IdReferences.Count)
        {
            DtdIdReference reference = IdReferences[index];
            if (!Identifiers.ContainsKey(reference.Value))
            {
                ValidationContext.ReportDtdValidationError("ID reference '" + reference.Value + "' does not match any ID value", reference.Offset);
            }

            index = index + 1;
        }
    }

    internal void ValidateDtdEnumeratedAttribute(XmlAttributeDeclaration declaration, string value, int offset)
    {
        if (declaration.TypeName != "ENUMERATION" && declaration.TypeName != "NOTATION")
        {
            return;
        }

        string[] values = declaration.AllowedValues == null ? [] : declaration.AllowedValues;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == value)
            {
                return;
            }

            index = index + 1;
        }

        ValidationContext.ReportDtdValidationError("Attribute '" + DtdAttributeDeclarationName(declaration) + "' has a value outside its declared token list", offset);
    }

    internal XmlAttributeData CreateDefaultedDtdAttribute(XmlAttributeDeclaration declaration)
    {
        XmlAttributeData attribute = new XmlAttributeData();
        attribute.Name = CopyDtdAttributeName(declaration.Name);
        attribute.Value = declaration.DefaultValue;
        attribute.RawValue = declaration.DefaultValue;
        attribute.Quote = '\0';
        attribute.IsSpecified = false;
        attribute.IsDefaulted = true;
        attribute.IsFixed = declaration.DefaultKind == "#FIXED";
        attribute.IsNamespaceDeclaration = declaration.IsNamespaceDeclaration;
        attribute.Range = null;
        return attribute;
    }

    internal XmlName CopyDtdAttributeName(XmlName? sourceName)
    {
        XmlName name = new XmlName();
        if (sourceName != null)
        {
            name.Prefix = sourceName.Prefix;
            name.LocalName = sourceName.LocalName;
            name.NamespaceUri = sourceName.NamespaceUri;
            name.QualifiedName = sourceName.QualifiedName;
        }

        return name;
    }

    internal bool ContainsDtdAttributeName(List<XmlAttributeData> attributes, string attributeName)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            if (DtdAttributeQualifiedName(attributes[index]) == attributeName)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool ContainsDtdAttributeDataName(XmlAttributeData[] attributes, string attributeName)
    {
        int index = 0;
        while (index < attributes.Length)
        {
            if (DtdAttributeQualifiedName(attributes[index]) == attributeName)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal string DtdAttributeQualifiedName(XmlAttributeData attribute)
    {
        XmlName? name = attribute.Name;
        if (name == null)
        {
            return "";
        }

        if (name.QualifiedName != null)
        {
            return name.QualifiedName;
        }

        if (name.LocalName == null)
        {
            return "";
        }

        return name.Prefix == null ? name.LocalName : name.Prefix + ":" + name.LocalName;
    }

    internal string DtdAttributeDeclarationName(XmlAttributeDeclaration declaration)
    {
        XmlName? name = declaration.Name;
        return name == null || name.QualifiedName == null ? "" : name.QualifiedName;
    }

}
