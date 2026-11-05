namespace xml;

internal class XPathSchemaNodeTest
{
    internal CompiledXmlSchemaSet SchemaSet;
    internal XmlSchemaContentModelValidator Declarations;
    internal XmlSchemaAssessmentEngine Types;

    internal XPathSchemaNodeTest(XmlSchemaSet schemaSet)
    {
        Guard.NotNull(schemaSet, "schemaSet");
        CompiledXmlSchemaSet? compiled = schemaSet.CompiledSchemaSet;
        object? handle = compiled == null ? schemaSet.CompiledSet : compiled;
        if (compiled == null && (!schemaSet.IsImmutable || handle == null))
        {
            throw new System.ArgumentException("Schema-aware node tests require an immutable compiled schema set.", "schemaSet");
        }

        if (handle == null)
        {
            throw new System.ArgumentException("Schema-aware node tests require an immutable compiled schema set.", "schemaSet");
        }

        SchemaSet = (CompiledXmlSchemaSet)handle;
        Declarations = new XmlSchemaContentModelValidator(SchemaSet);
        Types = new XmlSchemaAssessmentEngine(SchemaSet, new XmlSchemaValidationOptions(), new XmlDefaultResolver());
    }

    internal bool HasXPathSchemaElementDeclaration(XmlName name)
    {
        Guard.NotNull(name, "name");
        return Declarations.FindGlobalXmlSchemaElement(name) != null;
    }

    internal bool HasXPathSchemaAttributeDeclaration(XmlName name)
    {
        Guard.NotNull(name, "name");
        return Declarations.FindGlobalXmlSchemaAttribute(name) != null;
    }

    internal bool HasXPathSchemaTypeDefinition(XmlName name)
    {
        Guard.NotNull(name, "name");
        XmlSchemaSelectedType selected = Types.FindXmlSchemaNamedType(name);
        return selected.SimpleType != null || selected.ComplexType != null;
    }

    internal bool IsXPathSchemaSequenceType(XmlName name)
    {
        Guard.NotNull(name, "name");
        XmlSchemaSelectedType selected = Types.FindXmlSchemaNamedType(name);
        CompiledXmlSchemaSimpleType? type = selected.SimpleType;
        return type != null && (type.Variety == XmlSchemaSimpleVariety.Atomic || type.Variety == XmlSchemaSimpleVariety.Union);
    }

    internal bool IsXPathSchemaAtomicType(XmlName name)
    {
        CompiledXmlSchemaSimpleType? type = FindXPathSchemaSimpleType(name);
        return type != null && type.Variety == XmlSchemaSimpleVariety.Atomic && type.HasAtomicType && type.AtomicType != XdmAtomicType.AnyAtomicType && type.AtomicType != XdmAtomicType.Notation;
    }

    internal bool IsXPathSchemaCastType(XmlName name)
    {
        CompiledXmlSchemaSimpleType? type = FindXPathSchemaSimpleType(name);
        if (type == null || type.Variety != XmlSchemaSimpleVariety.Atomic && type.Variety != XmlSchemaSimpleVariety.Union)
        {
            return false;
        }

        return type.Variety == XmlSchemaSimpleVariety.Union || type.HasAtomicType && type.AtomicType != XdmAtomicType.AnyAtomicType && type.AtomicType != XdmAtomicType.Notation;
    }

    internal CompiledXmlSchemaSimpleType? FindXPathSchemaSimpleType(XmlName name)
    {
        Guard.NotNull(name, "name");
        return Types.FindXmlSchemaNamedType(name).SimpleType;
    }

    internal bool MatchesXPathSchemaAtomicType(XdmAtomicValue candidate, XmlName typeName)
    {
        Guard.NotNull(candidate, "candidate");
        Guard.NotNull(typeName, "typeName");
        XmlSchemaTypeInfo? annotation = candidate.TypeInfo;
        if (annotation == null || annotation.Validity != XmlSchemaValidity.Valid)
        {
            return false;
        }

        XmlSchemaSelectedType expected = Types.FindXmlSchemaNamedType(typeName);
        XmlSchemaSelectedType actual = SelectXPathSchemaAnnotationType(annotation);
        if (expected.SimpleType == null || actual.SimpleType == null)
        {
            return false;
        }

        if (Types.IsXmlSchemaTypeDerivedFrom(actual, expected, XmlSchemaDerivationMethod.None))
        {
            return true;
        }

        XmlName? memberName = annotation.MemberTypeName;
        if (memberName == null)
        {
            return false;
        }

        XmlSchemaSelectedType member = Types.FindXmlSchemaNamedType(memberName);
        return member.SimpleType != null && Types.IsXmlSchemaTypeDerivedFrom(member, expected, XmlSchemaDerivationMethod.None);
    }

    internal bool MatchesXPathSchemaElement(XmlNode candidate, XmlName declarationName)
    {
        Guard.NotNull(candidate, "candidate");
        Guard.NotNull(declarationName, "declarationName");
        CompiledXmlSchemaElementDeclaration? expected = Declarations.FindGlobalXmlSchemaElement(declarationName);
        if (expected == null)
        {
            throw CreateXPathSchemaStaticError("The schema-element test does not name an in-scope element declaration.", XPathErrorCodes.XPST0008);
        }

        XmlName? candidateName = candidate.Name;
        XmlSchemaTypeInfo? annotation = candidate.TypeInfo;
        if (candidate.Kind != XmlNodeKind.Element || candidateName == null || annotation == null || annotation.Validity != XmlSchemaValidity.Valid)
        {
            return false;
        }

        CompiledXmlSchemaElementDeclaration? actual = Declarations.FindGlobalXmlSchemaElement(candidateName);
        if (actual == null || annotation.Declaration != actual || actual.IsAbstract || !IsXPathSchemaElementSubstitutable(actual, expected))
        {
            return false;
        }

        if (annotation.IsNilled && !actual.IsNillable)
        {
            return false;
        }

        return IsXPathSchemaAnnotationDerivedFrom(annotation, actual.SimpleType, actual.ComplexType);
    }

    internal bool MatchesXPathSchemaAttribute(XmlNode candidate, XmlName declarationName)
    {
        Guard.NotNull(candidate, "candidate");
        Guard.NotNull(declarationName, "declarationName");
        CompiledXmlSchemaAttributeDeclaration? declaration = Declarations.FindGlobalXmlSchemaAttribute(declarationName);
        if (declaration == null)
        {
            throw CreateXPathSchemaStaticError("The schema-attribute test does not name an in-scope attribute declaration.", XPathErrorCodes.XPST0008);
        }

        XmlName? candidateName = candidate.Name;
        XmlSchemaTypeInfo? annotation = candidate.TypeInfo;
        if (candidate.Kind != XmlNodeKind.Attribute || candidateName == null || annotation == null || annotation.Validity != XmlSchemaValidity.Valid)
        {
            return false;
        }

        if (!Declarations.SameXmlSchemaName(candidateName, declaration.Name) || annotation.Declaration != declaration)
        {
            return false;
        }

        return IsXPathSchemaAnnotationDerivedFrom(annotation, declaration.Type, null);
    }

    internal bool MatchesXPathSchemaType(XmlNode candidate, XmlName typeName)
    {
        Guard.NotNull(candidate, "candidate");
        Guard.NotNull(typeName, "typeName");
        XmlSchemaSelectedType expected = Types.FindXmlSchemaNamedType(typeName);
        if (expected.SimpleType == null && expected.ComplexType == null)
        {
            throw CreateXPathSchemaStaticError("The schema type test does not name an in-scope type definition.", XPathErrorCodes.XPST0051);
        }

        XmlSchemaTypeInfo? annotation = candidate.TypeInfo;
        if ((candidate.Kind != XmlNodeKind.Element && candidate.Kind != XmlNodeKind.Attribute) || annotation == null || annotation.Validity != XmlSchemaValidity.Valid)
        {
            return false;
        }

        XmlSchemaSelectedType actual = SelectXPathSchemaAnnotationType(annotation);
        if (actual.SimpleType == null && actual.ComplexType == null)
        {
            return false;
        }

        return Types.IsXmlSchemaTypeDerivedFrom(actual, expected, XmlSchemaDerivationMethod.None);
    }

    internal bool IsXPathSchemaElementSubstitutable(CompiledXmlSchemaElementDeclaration actual, CompiledXmlSchemaElementDeclaration expected)
    {
        if (actual == expected)
        {
            return true;
        }

        if (((int)expected.Block & (int)XmlSchemaDerivationMethod.Substitution) != 0)
        {
            return false;
        }

        CompiledXmlSchemaElementDeclaration? current = actual.SubstitutionGroupHead;
        while (current != null)
        {
            if (current == expected)
            {
                return true;
            }

            current = current.SubstitutionGroupHead;
        }

        return false;
    }

    internal bool IsXPathSchemaAnnotationDerivedFrom(XmlSchemaTypeInfo annotation, CompiledXmlSchemaSimpleType? simpleType, CompiledXmlSchemaComplexType? complexType)
    {
        XmlSchemaSelectedType actual = SelectXPathSchemaAnnotationType(annotation);
        XmlSchemaSelectedType expected = new XmlSchemaSelectedType();
        expected.SimpleType = simpleType;
        expected.ComplexType = complexType;
        if (actual.SimpleType == null && actual.ComplexType == null)
        {
            return false;
        }

        return Types.IsXmlSchemaTypeDerivedFrom(actual, expected, XmlSchemaDerivationMethod.None);
    }

    internal XmlSchemaSelectedType SelectXPathSchemaAnnotationType(XmlSchemaTypeInfo annotation)
    {
        XmlSchemaSelectedType result = new XmlSchemaSelectedType();
        object? definition = annotation.TypeDefinition;
        int simpleIndex = 0;
        while (simpleIndex < SchemaSet.SimpleTypes.Length)
        {
            if (SchemaSet.SimpleTypes[simpleIndex] == definition)
            {
                result.SimpleType = SchemaSet.SimpleTypes[simpleIndex];
                return result;
            }

            simpleIndex = simpleIndex + 1;
        }

        int complexIndex = 0;
        while (complexIndex < SchemaSet.ComplexTypes.Length)
        {
            if (SchemaSet.ComplexTypes[complexIndex] == definition)
            {
                result.ComplexType = SchemaSet.ComplexTypes[complexIndex];
                return result;
            }

            complexIndex = complexIndex + 1;
        }

        XmlName? typeName = annotation.TypeName;
        return typeName == null ? result : Types.FindXmlSchemaNamedType(typeName);
    }

    internal XPathStaticException CreateXPathSchemaStaticError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XPathStatic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XPathStaticException(message, diagnostic, null);
    }
}
