using xml;

namespace xml.Tests;

[TestFixture]
public class XdmTypeSystemTests
{
    [Test]
    public void RegistersEveryBuiltInAtomicTypeWithItsSchemaName()
    {
        XdmTypeSystem typeSystem = new XdmTypeSystem();
        XdmAtomicType[] enumValues = Enum.GetValues<XdmAtomicType>();
        XdmAtomicTypeDefinition[] definitions = typeSystem.GetAllXdmAtomicTypeDefinitions();

        Assert.That(definitions, Has.Length.EqualTo(enumValues.Length));
        Assert.That(definitions, Has.Length.EqualTo(46));
        for (int index = 0; index < definitions.Length; index++)
        {
            XdmAtomicTypeDefinition definition = definitions[index];
            Assert.Multiple(() =>
            {
                Assert.That(definition, Is.Not.Null);
                Assert.That(definition.Type, Is.EqualTo(enumValues[index]));
                Assert.That(definition.Name.Prefix, Is.EqualTo("xs"));
                Assert.That(definition.Name.NamespaceUri, Is.EqualTo(XPathNamespaces.XmlSchema));
                Assert.That(definition.Name.QualifiedName, Is.EqualTo("xs:" + definition.Name.LocalName));
                Assert.That(typeSystem.GetXdmAtomicTypeDefinition(definition.Type), Is.SameAs(definition));
                Assert.That(typeSystem.FindXdmAtomicTypeDefinition(definition.Name), Is.SameAs(definition));
            });
        }

        definitions[0] = definitions[1];
        Assert.That(typeSystem.GetAllXdmAtomicTypeDefinitions()[0].Type, Is.EqualTo(XdmAtomicType.AnyAtomicType));
    }

    [Test]
    public void ModelsImmediateBaseTypesAcrossEveryDerivedFamily()
    {
        XdmTypeSystem typeSystem = new XdmTypeSystem();

        Assert.Multiple(() =>
        {
            Assert.That(typeSystem.GetXdmBaseAtomicTypeDefinition(XdmAtomicType.AnyAtomicType), Is.Null);
            AssertBase(typeSystem, XdmAtomicType.UntypedAtomic, XdmAtomicType.AnyAtomicType);
            AssertBase(typeSystem, XdmAtomicType.String, XdmAtomicType.AnyAtomicType);
            AssertBase(typeSystem, XdmAtomicType.NormalizedString, XdmAtomicType.String);
            AssertBase(typeSystem, XdmAtomicType.Token, XdmAtomicType.NormalizedString);
            AssertBase(typeSystem, XdmAtomicType.Language, XdmAtomicType.Token);
            AssertBase(typeSystem, XdmAtomicType.Name, XdmAtomicType.Token);
            AssertBase(typeSystem, XdmAtomicType.NcName, XdmAtomicType.Name);
            AssertBase(typeSystem, XdmAtomicType.Nmtoken, XdmAtomicType.Token);
            AssertBase(typeSystem, XdmAtomicType.Id, XdmAtomicType.NcName);
            AssertBase(typeSystem, XdmAtomicType.Idref, XdmAtomicType.NcName);
            AssertBase(typeSystem, XdmAtomicType.Entity, XdmAtomicType.NcName);
            AssertBase(typeSystem, XdmAtomicType.Integer, XdmAtomicType.Decimal);
            AssertBase(typeSystem, XdmAtomicType.NonPositiveInteger, XdmAtomicType.Integer);
            AssertBase(typeSystem, XdmAtomicType.NegativeInteger, XdmAtomicType.NonPositiveInteger);
            AssertBase(typeSystem, XdmAtomicType.Long, XdmAtomicType.Integer);
            AssertBase(typeSystem, XdmAtomicType.Int, XdmAtomicType.Long);
            AssertBase(typeSystem, XdmAtomicType.Short, XdmAtomicType.Int);
            AssertBase(typeSystem, XdmAtomicType.Byte, XdmAtomicType.Short);
            AssertBase(typeSystem, XdmAtomicType.NonNegativeInteger, XdmAtomicType.Integer);
            AssertBase(typeSystem, XdmAtomicType.UnsignedLong, XdmAtomicType.NonNegativeInteger);
            AssertBase(typeSystem, XdmAtomicType.UnsignedInt, XdmAtomicType.UnsignedLong);
            AssertBase(typeSystem, XdmAtomicType.UnsignedShort, XdmAtomicType.UnsignedInt);
            AssertBase(typeSystem, XdmAtomicType.UnsignedByte, XdmAtomicType.UnsignedShort);
            AssertBase(typeSystem, XdmAtomicType.PositiveInteger, XdmAtomicType.NonNegativeInteger);
            AssertBase(typeSystem, XdmAtomicType.YearMonthDuration, XdmAtomicType.Duration);
            AssertBase(typeSystem, XdmAtomicType.DayTimeDuration, XdmAtomicType.Duration);
            AssertBase(typeSystem, XdmAtomicType.DateTimeStamp, XdmAtomicType.DateTime);
        });

        XdmAtomicType[] directPrimitives =
        [
            XdmAtomicType.Boolean, XdmAtomicType.Decimal, XdmAtomicType.Float, XdmAtomicType.Double,
            XdmAtomicType.Duration, XdmAtomicType.DateTime, XdmAtomicType.Date, XdmAtomicType.Time,
            XdmAtomicType.GYearMonth, XdmAtomicType.GYear, XdmAtomicType.GMonthDay, XdmAtomicType.GMonth,
            XdmAtomicType.GDay, XdmAtomicType.HexBinary, XdmAtomicType.Base64Binary, XdmAtomicType.AnyUri,
            XdmAtomicType.QName, XdmAtomicType.Notation
        ];
        foreach (XdmAtomicType type in directPrimitives)
        {
            AssertBase(typeSystem, type, XdmAtomicType.AnyAtomicType);
        }
    }

    [Test]
    public void ResolvesPrimitiveTypesAndTransitiveSubtypeRelationships()
    {
        XdmTypeSystem typeSystem = new XdmTypeSystem();

        Assert.Multiple(() =>
        {
            Assert.That(typeSystem.GetXdmPrimitiveAtomicTypeDefinition(XdmAtomicType.AnyAtomicType), Is.Null);
            Assert.That(typeSystem.GetXdmPrimitiveAtomicTypeDefinition(XdmAtomicType.UntypedAtomic)!.Type, Is.EqualTo(XdmAtomicType.UntypedAtomic));
            Assert.That(typeSystem.GetXdmPrimitiveAtomicTypeDefinition(XdmAtomicType.Language)!.Type, Is.EqualTo(XdmAtomicType.String));
            Assert.That(typeSystem.GetXdmPrimitiveAtomicTypeDefinition(XdmAtomicType.Byte)!.Type, Is.EqualTo(XdmAtomicType.Decimal));
            Assert.That(typeSystem.GetXdmPrimitiveAtomicTypeDefinition(XdmAtomicType.YearMonthDuration)!.Type, Is.EqualTo(XdmAtomicType.Duration));
            Assert.That(typeSystem.GetXdmPrimitiveAtomicTypeDefinition(XdmAtomicType.DateTimeStamp)!.Type, Is.EqualTo(XdmAtomicType.DateTime));
            Assert.That(typeSystem.IsXdmAtomicSubtypeOf(XdmAtomicType.Byte, XdmAtomicType.Decimal), Is.True);
            Assert.That(typeSystem.IsXdmAtomicSubtypeOf(XdmAtomicType.Byte, XdmAtomicType.AnyAtomicType), Is.True);
            Assert.That(typeSystem.IsXdmAtomicSubtypeOf(XdmAtomicType.Id, XdmAtomicType.Token), Is.True);
            Assert.That(typeSystem.IsXdmAtomicSubtypeOf(XdmAtomicType.DateTimeStamp, XdmAtomicType.DateTime), Is.True);
            Assert.That(typeSystem.IsXdmAtomicSubtypeOf(XdmAtomicType.String, XdmAtomicType.String), Is.True);
            Assert.That(typeSystem.IsXdmAtomicSubtypeOf(XdmAtomicType.String, XdmAtomicType.Decimal), Is.False);
            Assert.That(typeSystem.IsXdmAtomicSubtypeOf(XdmAtomicType.AnyAtomicType, XdmAtomicType.String), Is.False);
            Assert.That(typeSystem.IsXdmPrimitiveAtomicType(XdmAtomicType.String), Is.True);
            Assert.That(typeSystem.IsXdmPrimitiveAtomicType(XdmAtomicType.Token), Is.False);
            Assert.That(typeSystem.GetXdmAtomicTypeDefinition(XdmAtomicType.AnyAtomicType).IsAbstract, Is.True);
        });
    }

    [Test]
    public void ClassifiesAtomicFamiliesAndXPathUntypedTypes()
    {
        XdmTypeSystem typeSystem = new XdmTypeSystem();
        XmlName untyped = typeSystem.GetXdmUntypedNodeTypeName();
        untyped.LocalName = "changed";
        XmlName freshUntyped = typeSystem.GetXdmUntypedNodeTypeName();

        Assert.Multiple(() =>
        {
            Assert.That(freshUntyped.QualifiedName, Is.EqualTo("xs:untyped"));
            Assert.That(typeSystem.IsXdmUntypedNodeTypeName(freshUntyped), Is.True);
            Assert.That(typeSystem.IsXdmUntypedNodeTypeName(new XmlName { LocalName = "untyped", NamespaceUri = "urn:other" }), Is.False);
            Assert.That(typeSystem.IsXdmStringDerivedAtomicType(XdmAtomicType.Id), Is.True);
            Assert.That(typeSystem.IsXdmStringDerivedAtomicType(XdmAtomicType.AnyUri), Is.False);
            Assert.That(typeSystem.IsXdmNumericAtomicType(XdmAtomicType.UnsignedByte), Is.True);
            Assert.That(typeSystem.IsXdmNumericAtomicType(XdmAtomicType.Float), Is.True);
            Assert.That(typeSystem.IsXdmNumericAtomicType(XdmAtomicType.String), Is.False);
            Assert.That(typeSystem.IsXdmDurationDerivedAtomicType(XdmAtomicType.DayTimeDuration), Is.True);
            Assert.That(typeSystem.IsXdmDurationDerivedAtomicType(XdmAtomicType.DateTime), Is.False);
            Assert.That(typeSystem.IsXdmNamespaceSensitiveAtomicType(XdmAtomicType.QName), Is.True);
            Assert.That(typeSystem.IsXdmNamespaceSensitiveAtomicType(XdmAtomicType.Notation), Is.True);
            Assert.That(typeSystem.IsXdmNamespaceSensitiveAtomicType(XdmAtomicType.String), Is.False);
        });
    }

    [Test]
    public void RejectsUnknownOrNonAtomicTypeNames()
    {
        XdmTypeSystem typeSystem = new XdmTypeSystem();

        Assert.Multiple(() =>
        {
            Assert.That(typeSystem.FindXdmAtomicTypeDefinition(new XmlName { LocalName = "string", NamespaceUri = "urn:other" }), Is.Null);
            Assert.That(typeSystem.FindXdmAtomicTypeDefinition(new XmlName { LocalName = "unknown", NamespaceUri = XPathNamespaces.XmlSchema }), Is.Null);
            Assert.That(typeSystem.FindXdmAtomicTypeDefinition(new XmlName { LocalName = "untyped", NamespaceUri = XPathNamespaces.XmlSchema }), Is.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() => typeSystem.GetXdmAtomicTypeDefinition((XdmAtomicType)99));
            Assert.Throws<ArgumentOutOfRangeException>(() => typeSystem.IsXdmAtomicSubtypeOf(XdmAtomicType.String, (XdmAtomicType)99));
        });
    }

    [Test]
    public void CreatesAndValidatesStorageForEveryConcreteAtomicType()
    {
        XdmFactory factory = new XdmFactory();
        XdmAtomicTypeDefinition[] definitions = factory.TypeSystem.GetAllXdmAtomicTypeDefinitions();
        int validated = 0;

        foreach (XdmAtomicTypeDefinition definition in definitions)
        {
            if (definition.IsAbstract)
            {
                Assert.Throws<ArgumentException>(() => factory.CreateXdmAtomicValue(definition.Type));
                continue;
            }

            XdmAtomicValue value = factory.CreateXdmAtomicValue(definition.Type);
            PopulateStorage(value, factory.TypeSystem);
            factory.ValidateXdmAtomicValue(value);
            XdmAtomicValue snapshot = factory.FreezeXdmAtomicValue(value);
            Assert.Multiple(() =>
            {
                Assert.That(snapshot.Type, Is.EqualTo(definition.Type));
                Assert.That(snapshot.IsImmutable, Is.True);
                Assert.That(value.IsImmutable, Is.False);
            });
            validated++;
        }

        Assert.That(validated, Is.EqualTo(45));
    }

    [Test]
    public void FrozenAtomicValuesDetachMutableBinaryAndNameStorage()
    {
        XdmFactory factory = new XdmFactory();
        XmlSchemaTypeInfo annotation = new XmlSchemaTypeInfo { AtomicType = XdmAtomicType.QName };
        object extended = new object();
        XdmAtomicValue source = new XdmAtomicValue
        {
            Type = XdmAtomicType.QName,
            LexicalValue = "p:item",
            CanonicalValue = "p:item",
            StringValue = "p:item",
            BooleanValue = true,
            DecimalValue = 1.5m,
            IntegerValue = 7,
            FloatingValue = 2.5,
            BinaryValue = [1, 2],
            NameValue = new XmlName { Prefix = "p", LocalName = "item", NamespaceUri = "urn:p", QualifiedName = "p:item" },
            ExtendedValue = extended,
            TypeInfo = annotation
        };

        XdmAtomicValue snapshot = factory.FreezeXdmAtomicValue(source);
        source.BinaryValue![0] = 9;
        source.NameValue!.LocalName = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.LexicalValue, Is.EqualTo("p:item"));
            Assert.That(snapshot.CanonicalValue, Is.EqualTo("p:item"));
            Assert.That(snapshot.StringValue, Is.EqualTo("p:item"));
            Assert.That(snapshot.BooleanValue, Is.True);
            Assert.That(snapshot.DecimalValue, Is.EqualTo(1.5m));
            Assert.That(snapshot.IntegerValue, Is.EqualTo(7));
            Assert.That(snapshot.FloatingValue, Is.EqualTo(2.5));
            Assert.That(snapshot.BinaryValue, Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(snapshot.BinaryValue, Is.Not.SameAs(source.BinaryValue));
            Assert.That(snapshot.NameValue!.LocalName, Is.EqualTo("item"));
            Assert.That(snapshot.NameValue, Is.Not.SameAs(source.NameValue));
            Assert.That(snapshot.ExtendedValue, Is.SameAs(extended));
            Assert.That(snapshot.TypeInfo, Is.SameAs(annotation));
            Assert.That(snapshot.IsImmutable, Is.True);
        });
    }

    [Test]
    public void RejectsIncompleteOrAbstractAtomicStorage()
    {
        XdmFactory factory = new XdmFactory();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => factory.ValidateXdmAtomicValue(new XdmAtomicValue { Type = XdmAtomicType.AnyAtomicType }));
            Assert.Throws<ArgumentException>(() => factory.ValidateXdmAtomicValue(new XdmAtomicValue { Type = XdmAtomicType.String }));
            Assert.Throws<ArgumentException>(() => factory.ValidateXdmAtomicValue(new XdmAtomicValue { Type = XdmAtomicType.HexBinary }));
            Assert.Throws<ArgumentException>(() => factory.ValidateXdmAtomicValue(new XdmAtomicValue { Type = XdmAtomicType.QName }));
            Assert.Throws<ArgumentOutOfRangeException>(() => factory.ValidateXdmAtomicValue(new XdmAtomicValue { Type = (XdmAtomicType)99 }));
        });
    }

    private static void AssertBase(XdmTypeSystem typeSystem, XdmAtomicType type, XdmAtomicType expectedBase)
    {
        Assert.That(typeSystem.GetXdmBaseAtomicTypeDefinition(type)!.Type, Is.EqualTo(expectedBase), type.ToString());
    }

    private static void PopulateStorage(XdmAtomicValue value, XdmTypeSystem typeSystem)
    {
        if (value.Type == XdmAtomicType.HexBinary || value.Type == XdmAtomicType.Base64Binary)
        {
            value.BinaryValue = [];
            return;
        }

        if (typeSystem.IsXdmNamespaceSensitiveAtomicType(value.Type))
        {
            value.NameValue = new XmlName { LocalName = "name", NamespaceUri = "urn:test", QualifiedName = "name" };
            return;
        }

        if (value.Type == XdmAtomicType.Boolean || typeSystem.IsXdmNumericAtomicType(value.Type))
        {
            return;
        }

        value.StringValue = "value";
    }
}
