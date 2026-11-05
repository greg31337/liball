namespace asn1;

internal class XerUsefulTypeSchemas
{
    internal AsnTypeDescriptor? CreateAssociatedSchema(AsnTypeKind kind)
    {
        if (kind == AsnTypeKind.InstanceOf)
        {
            return this.CreateInstanceOfSchema();
        }

        string dataName = kind == AsnTypeKind.CharacterString
            ? "string-value"
            : kind == AsnTypeKind.External
                || kind == AsnTypeKind.EmbeddedPdv
                ? "data-value"
                : "";
        if (dataName.Length == 0)
        {
            return null;
        }

        AsnFieldDescriptor identification = this.CreateField(
            "identification",
            0,
            this.CreateIdentificationSchema());
        AsnFieldDescriptor data = this.CreateField(
            dataName,
            kind == AsnTypeKind.CharacterString ? 1 : 2,
            this.CreatePrimitiveType(AsnTypeKind.OctetString));
        AsnFieldDescriptor[] fields;
        if (kind == AsnTypeKind.External)
        {
            AsnFieldDescriptor descriptor = this.CreateField(
                "data-value-descriptor",
                1,
                this.CreatePrimitiveType(AsnTypeKind.GraphicString));
            descriptor.Optional = true;
            fields = [identification, descriptor, data];
        }
        else
        {
            fields = [identification, data];
        }

        return new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Sequence,
            Fields = fields,
            RootFields = fields
        };
    }

    internal AsnTypeDescriptor CreateIdentificationSchema()
    {
        AsnTypeDescriptor objectIdentifier = this.CreatePrimitiveType(
            AsnTypeKind.ObjectIdentifier);
        AsnTypeDescriptor integer = this.CreatePrimitiveType(
            AsnTypeKind.Integer);
        AsnFieldDescriptor[] fields =
        [
            this.CreateField(
                "syntaxes",
                0,
                this.CreatePairSchema(
                    "abstract",
                    objectIdentifier,
                    "transfer",
                    this.CreatePrimitiveType(
                        AsnTypeKind.ObjectIdentifier))),
            this.CreateField("syntax", 1, objectIdentifier),
            this.CreateField("presentation-context-id", 2, integer),
            this.CreateField(
                "context-negotiation",
                3,
                this.CreatePairSchema(
                    "presentation-context-id",
                    integer,
                    "transfer-syntax",
                    this.CreatePrimitiveType(
                        AsnTypeKind.ObjectIdentifier))),
            this.CreateField(
                "transfer-syntax",
                4,
                this.CreatePrimitiveType(AsnTypeKind.ObjectIdentifier)),
            this.CreateField(
                "fixed",
                5,
                this.CreatePrimitiveType(AsnTypeKind.Null))
        ];
        return new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Choice,
            Fields = fields,
            RootFields = fields
        };
    }

    internal AsnTypeDescriptor CreateInstanceOfSchema()
    {
        AsnFieldDescriptor[] fields =
        [
            this.CreateField(
                "type-id",
                0,
                this.CreatePrimitiveType(AsnTypeKind.ObjectIdentifier)),
            this.CreateField(
                "value",
                1,
                this.CreatePrimitiveType(AsnTypeKind.OpenType))
        ];
        return new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Sequence,
            Fields = fields,
            RootFields = fields
        };
    }

    internal AsnTypeDescriptor CreatePairSchema(
        string firstName,
        AsnTypeDescriptor firstType,
        string secondName,
        AsnTypeDescriptor secondType)
    {
        AsnFieldDescriptor[] fields =
        [
            this.CreateField(firstName, 0, firstType),
            this.CreateField(secondName, 1, secondType)
        ];
        return new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Sequence,
            Fields = fields,
            RootFields = fields
        };
    }

    internal AsnTypeDescriptor CreatePrimitiveType(AsnTypeKind kind)
    {
        return new AsnTypeDescriptor { Kind = kind };
    }

    internal AsnFieldDescriptor CreateField(
        string name,
        int index,
        AsnTypeDescriptor type)
    {
        return new AsnFieldDescriptor
        {
            Name = name,
            Index = index,
            Type = type
        };
    }
}
