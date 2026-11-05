namespace units.Tests;

public class InterfaceArchitectureTests
{
    [Test]
    public void BuiltInTypes_ImplementPublicAbstractions()
    {
        Assert.That(new UnitRegistry(), Is.AssignableTo<IUnitCatalog>());
        Assert.That(new UnitParser(), Is.AssignableTo<IUnitParser>());
    }

    [Test]
    public void CustomCatalog_SupportsParsingAndSerializationWithoutMutableRegistry()
    {
        TestUnitCatalog catalog = new TestUnitCatalog();
        UnitParser parser = new UnitParser();
        QuantitySerializer serializer = new QuantitySerializer();

        UnitDefinition kilometer = parser.ParseOrThrow("km", catalog);
        QuantityValue quantity = serializer.FromDto(QuantityDto.Create(2.5d, "m", "length"), catalog);

        Assert.That(kilometer.GetId(), Is.EqualTo("kilo_meter"));
        Assert.That(kilometer.GetScaleToSystemUnit().EqualsValue(new RationalNumber(1000, 1)), Is.True);
        Assert.That(quantity.GetValue(), Is.EqualTo(2.5d));
        Assert.That(quantity.GetUnit().GetId(), Is.EqualTo("meter"));
    }

    [Test]
    public void QuantityParser_UsesInjectedUnitParser()
    {
        TestUnitCatalog catalog = new TestUnitCatalog();
        TestUnitParser unitParser = new TestUnitParser();
        QuantityParser parser = new QuantityParser(unitParser);

        QuantityValue quantity = parser.ParseOrThrow("12.5 arbitrary-unit", catalog);

        Assert.That(quantity.GetValue(), Is.EqualTo(12.5d));
        Assert.That(quantity.GetUnit().GetId(), Is.EqualTo("meter"));
        Assert.That(unitParser.Text, Is.EqualTo("arbitrary-unit"));
        Assert.That(unitParser.Catalog, Is.SameAs(catalog));
        Assert.Throws<System.ArgumentNullException>(() => new QuantityParser(null!));
    }

    [Test]
    public void UnitParser_ReusedConcurrently_KeepsParseStatePerCall()
    {
        TestUnitCatalog catalog = new TestUnitCatalog();
        UnitParser parser = new UnitParser();

        Parallel.For(0, 200, index =>
        {
            string text = index % 2 == 0 ? "m" : "km";
            string expectedId = index % 2 == 0 ? "meter" : "kilo_meter";
            UnitDefinition unit = parser.ParseOrThrow(text, catalog);
            Assert.That(unit.GetId(), Is.EqualTo(expectedId));
        });
    }

    class TestUnitParser : IUnitParser
    {
        public string Text;
        public IUnitCatalog Catalog;

        public TestUnitParser()
        {
            Text = "";
            Catalog = new TestUnitCatalog();
        }

        public UnitDefinition ParseOrThrow(string text, IUnitCatalog registry)
        {
            return Parse(text, registry).GetUnit();
        }

        public UnitParseResult Parse(string text, IUnitCatalog registry)
        {
            return ParseWithOptions(text, registry, UnitParseOptions.CreateDefault());
        }

        public UnitParseResult ParseWithOptions(string text, IUnitCatalog registry, UnitParseOptions options)
        {
            Text = text;
            Catalog = registry;
            return UnitParseResult.CreateSuccess(TestUnitCatalog.CreateMeter());
        }

        public UnitDefinition ParseOrThrowWithOptions(string text, IUnitCatalog registry, UnitParseOptions options)
        {
            return ParseWithOptions(text, registry, options).GetUnit();
        }
    }

    class TestUnitCatalog : IUnitCatalog
    {
        UnitDefinition _meter;
        PrefixDefinition _kilo;
        QuantityKindDefinition _length;

        public TestUnitCatalog()
        {
            _meter = CreateMeter();
            _kilo = new PrefixDefinition("kilo", "kilo", "k", new RationalNumber(1000, 1));
            _length = new QuantityKindDefinition("length", "Length", new StandardDimensions().Length(), "meter", new string[0], new string[] { "meter" });
        }

        public UnitLookupResult FindUnit(string text)
        {
            if (string.Equals(text, "m", System.StringComparison.Ordinal) || string.Equals(text, "meter", System.StringComparison.Ordinal))
            {
                return UnitLookupResult.FromMatches(new UnitDefinition[] { _meter }, text);
            }

            return UnitLookupResult.FromMatches(new UnitDefinition[0], text);
        }

        public UnitLookupResult FindUnitById(string id)
        {
            return FindUnit(id);
        }

        public PrefixDefinition[] GetPrefixes()
        {
            return new PrefixDefinition[] { _kilo };
        }

        public QuantityKindLookupResult FindQuantityKind(string idOrName)
        {
            if (string.Equals(idOrName, "length", System.StringComparison.Ordinal))
            {
                return QuantityKindLookupResult.FromMatches(new QuantityKindDefinition[] { _length }, idOrName);
            }

            return QuantityKindLookupResult.FromMatches(new QuantityKindDefinition[0], idOrName);
        }

        public EquivalencyDefinition? FindEquivalency(string idOrName)
        {
            return null;
        }

        public static UnitDefinition CreateMeter()
        {
            return new UnitDefinition(
                "meter",
                "meter",
                "meters",
                "m",
                new string[0],
                new StandardDimensions().Length(),
                new RationalNumber(1, 1),
                new RationalNumber(0, 1),
                true,
                UnitConversionKind.Scale);
        }
    }
}
