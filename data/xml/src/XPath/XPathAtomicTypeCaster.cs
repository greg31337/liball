namespace xml;

internal class XPathAtomicTypeCaster
{
    internal XdmTypeConverter Converter;

    internal XPathAtomicTypeCaster()
    {
        Converter = new XdmTypeConverter();
    }

    internal XdmConversionResult TryCastXPathBuiltInAtomic(XdmAtomicValue source, XdmAtomicType target, XPathStaticContext context)
    {
        return Converter.TryCastXdmAtomic(source, target, context);
    }

    internal XdmConversionResult TryCastXPathSchemaAtomic(XdmAtomicValue source, CompiledXmlSchemaSimpleType target, XPathStaticContext context)
    {
        if (target.Variety == XmlSchemaSimpleVariety.Union)
        {
            return TryCastXPathSchemaUnion(source, target, context);
        }

        if (target.Variety != XmlSchemaSimpleVariety.Atomic || !target.HasAtomicType || target.AtomicType == XdmAtomicType.AnyAtomicType || target.AtomicType == XdmAtomicType.Notation)
        {
            return new XdmConversionResult(false, true, null, XPathErrorCodes.XPTY0004, "The schema type is not a permitted atomic cast target.");
        }

        XdmConversionResult converted = Converter.TryCastXdmAtomic(source, target.AtomicType, context);
        if (!converted.Success || converted.Value == null)
        {
            return converted;
        }

        return ApplyXPathSchemaAtomicFacets(converted.Value, target);
    }

    internal XdmConversionResult TryCastXPathSchemaUnion(XdmAtomicValue source, CompiledXmlSchemaSimpleType target, XPathStaticContext context)
    {
        int index = 0;
        while (index < target.MemberTypes.Length)
        {
            CompiledXmlSchemaSimpleType member = target.MemberTypes[index];
            XdmConversionResult converted = TryCastXPathSchemaAtomic(source, member, context);
            if (converted.Success && converted.Value != null)
            {
                XdmAtomicValue value = converted.Value;
                XmlName? memberName = value.TypeInfo == null ? member.Name : value.TypeInfo.TypeName;
                XdmConversionResult constrained = ApplyXPathSchemaAtomicFacets(value, target);
                if (constrained.Success && constrained.Value != null)
                {
                    XmlSchemaTypeInfo? annotation = constrained.Value.TypeInfo;
                    if (annotation == null)
                    {
                        return new XdmConversionResult(false, true, null, XPathErrorCodes.XPTY0004, "The union cast did not produce schema type information.");
                    }

                    annotation.MemberTypeName = memberName == null ? null : new XdmQNameDatatype().CopyXdmExpandedName(memberName);
                    return constrained;
                }
            }

            index = index + 1;
        }

        return new XdmConversionResult(false, false, null, XPathErrorCodes.FORG0001, "The value cannot be cast to any member of the target union type.");
    }

    internal XdmConversionResult ApplyXPathSchemaAtomicFacets(XdmAtomicValue value, CompiledXmlSchemaSimpleType target)
    {
        string lexical = Converter.ReadXdmAtomicStringValue(value);
        XmlSchemaSimpleTypeValidationResult validation = new XmlSchemaSimpleTypeValidationResult();
        validation.Success = true;
        validation.NormalizedValue = lexical;
        validation.CanonicalValue = value.CanonicalValue == null ? lexical : value.CanonicalValue;
        validation.AtomicValue = value;
        validation.AtomicValues = [value];
        new XmlSchemaSimpleTypeValidator().ApplyXmlSchemaValueFacets(target, validation, target.Node);
        if (!validation.Success)
        {
            return new XdmConversionResult(false, false, null, XPathErrorCodes.FORG0001, "The value does not satisfy the target schema type facets.");
        }

        XmlSchemaTypeInfo annotation = new XmlSchemaTypeInfo();
        annotation.TypeName = target.Name == null ? null : new XdmQNameDatatype().CopyXdmExpandedName(target.Name);
        annotation.TypeDefinition = target;
        annotation.AtomicType = value.Type;
        annotation.Validity = XmlSchemaValidity.Valid;
        value.TypeInfo = annotation;
        return new XdmConversionResult(true, false, value, null, null);
    }

    internal XdmAtomicValue RequireXPathAtomicCastResult(XdmConversionResult result)
    {
        return Converter.RequireXdmConversionResult(result);
    }
}
