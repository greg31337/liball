using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class QuantityFactoryTests
{
    [Test]
    public void CreateFromUnitId_ResolvesThroughCatalogAndPreservesValue()
    {
        IQuantityFactory factory = new QuantityFactory(new UnitParser());
        IUnitCatalog catalog = new StandardRegistry().Create();

        QuantityConstructionResult result = factory.CreateFromUnitId(12.5d, "kilometer", catalog, null);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.IsFailure(), Is.False);
        Assert.That(result.GetQuantity().GetValue(), Is.EqualTo(12.5d));
        Assert.That(result.GetQuantity().GetUnit().GetId(), Is.EqualTo("kilometer"));
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.None));
        Assert.That(result.GetMessage(), Is.Empty);
        Assert.That(result.GetCandidates(), Is.Empty);
        Assert.That(result, Is.AssignableTo<IQuantityConstructionResult>());
    }

    [Test]
    public void CreateFromUnitSymbol_ReturnsAmbiguityAndUsesQuantityKindToNarrowIt()
    {
        QuantityFactory factory = new QuantityFactory(new UnitParser());
        UnitRegistry registry = CreateAmbiguousRegistry();
        QuantityKindDefinition alphaKind = new QuantityKindDefinition("alpha_length", "Alpha length", DimensionVector.Length(), "alpha", new string[0], new string[0]);

        QuantityConstructionResult ambiguous = factory.CreateFromUnitSymbol(2.0d, "u", registry, null);
        QuantityConstructionResult narrowed = factory.CreateFromUnitSymbol(3.0d, "u", registry, alphaKind);

        Assert.That(ambiguous.IsFailure(), Is.True);
        Assert.That(ambiguous.GetErrorCode(), Is.EqualTo(UnitErrorCode.LookupAmbiguous));
        Assert.That(ambiguous.GetCandidates(), Has.Length.EqualTo(2));
        Assert.Throws<System.InvalidOperationException>(() => ambiguous.GetQuantity());
        Assert.That(narrowed.IsSuccess(), Is.True);
        Assert.That(narrowed.GetQuantity().GetValue(), Is.EqualTo(3.0d));
        Assert.That(narrowed.GetQuantity().GetUnit().GetId(), Is.EqualTo("alpha"));
    }

    [Test]
    public void CreateFromUnitExpression_ParsesCompoundUnitAndValidatesQuantityKind()
    {
        QuantityFactory factory = new QuantityFactory(new UnitParser());
        UnitRegistry registry = new StandardRegistry().Create();

        QuantityConstructionResult result = factory.CreateFromUnitExpression(9.81d, "kg*m/s^2", registry, new StandardQuantityKinds().Force());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetQuantity().GetValue(), Is.EqualTo(9.81d));
        Assert.That(result.GetQuantity().GetUnit().GetDimension().EqualsDimension(new StandardUnits().Newton().GetDimension()), Is.True);
        Assert.That(result.GetQuantity().GetUnit().GetScaleToSystemUnit().EqualsValue(new RationalNumber(1, 1)), Is.True);
    }

    [Test]
    public void CreateFromUnitExpression_AllowsCompatiblePrefixedScaleUnitForQuantityKind()
    {
        QuantityFactory factory = new QuantityFactory(new UnitParser());
        UnitRegistry registry = new StandardRegistry().Create();

        QuantityConstructionResult result = factory.CreateFromUnitExpression(4.0d, "km", registry, new StandardQuantityKinds().Length());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetQuantity().GetUnit().GetId(), Is.EqualTo("kilometer"));
    }

    [Test]
    public void CreateMethods_ReturnStructuredLookupParseAndQuantityKindFailures()
    {
        QuantityFactory factory = new QuantityFactory(new UnitParser());
        UnitRegistry registry = new StandardRegistry().Create();

        QuantityConstructionResult missingId = factory.CreateFromUnitId(1.0d, "missing", registry, null);
        QuantityConstructionResult missingSymbol = factory.CreateFromUnitSymbol(1.0d, "missing", registry, null);
        QuantityConstructionResult malformedExpression = factory.CreateFromUnitExpression(1.0d, "m/", registry, null);
        QuantityConstructionResult incompatibleKind = factory.CreateFromUnitSymbol(1.0d, "s", registry, new StandardQuantityKinds().Length());

        Assert.That(missingId.GetErrorCode(), Is.EqualTo(UnitErrorCode.LookupNotFound));
        Assert.That(missingSymbol.GetErrorCode(), Is.EqualTo(UnitErrorCode.LookupNotFound));
        Assert.That(malformedExpression.GetErrorCode(), Is.EqualTo(UnitErrorCode.ParseInvalidInput));
        Assert.That(incompatibleKind.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionIncompatibleDimensions));
        Assert.That(incompatibleKind.GetMessage(), Does.Contain("length"));
        Assert.That(incompatibleKind.GetCandidates(), Is.Empty);
    }

    [Test]
    public void CreateMethods_ReturnInvalidInputFailuresWithoutThrowing()
    {
        QuantityFactory factory = new QuantityFactory(new UnitParser());
        UnitRegistry registry = new StandardRegistry().Create();

        QuantityConstructionResult nullText = factory.CreateFromUnitId(1.0d, null!, registry, null);
        QuantityConstructionResult emptyText = factory.CreateFromUnitSymbol(1.0d, "  ", registry, null);
        QuantityConstructionResult nullCatalog = factory.CreateFromUnitExpression(1.0d, "m", null!, null);

        Assert.That(nullText.GetErrorCode(), Is.EqualTo(UnitErrorCode.ParseInvalidInput));
        Assert.That(emptyText.GetErrorCode(), Is.EqualTo(UnitErrorCode.ParseInvalidInput));
        Assert.That(nullCatalog.GetErrorCode(), Is.EqualTo(UnitErrorCode.ParseInvalidInput));
    }

    [Test]
    public void OrThrowMethods_ReturnQuantitiesForEveryResolutionRoute()
    {
        IQuantityFactory factory = new QuantityFactory(new UnitParser());
        UnitRegistry registry = new StandardRegistry().Create();

        QuantityValue byId = factory.CreateFromUnitIdOrThrow(1.0d, "meter", registry, null);
        QuantityValue bySymbol = factory.CreateFromUnitSymbolOrThrow(2.0d, "kg", registry, new StandardQuantityKinds().Mass());
        QuantityValue byExpression = factory.CreateFromUnitExpressionOrThrow(3.0d, "m/s", registry, new StandardQuantityKinds().Speed());

        Assert.That(byId.GetUnit().GetId(), Is.EqualTo("meter"));
        Assert.That(bySymbol.GetUnit().GetId(), Is.EqualTo("kilogram"));
        Assert.That(byExpression.GetUnit().GetId(), Is.EqualTo("(meter/second)"));
    }

    [Test]
    public void OrThrowMethods_RejectInvalidUnresolvedAndIncompatibleInputs()
    {
        QuantityFactory factory = new QuantityFactory(new UnitParser());
        UnitRegistry registry = new StandardRegistry().Create();

        Assert.Throws<System.ArgumentNullException>(() => new QuantityFactory(null!));
        Assert.Throws<System.ArgumentNullException>(() => factory.CreateFromUnitIdOrThrow(1.0d, null!, registry, null));
        Assert.Throws<System.ArgumentException>(() => factory.CreateFromUnitSymbolOrThrow(1.0d, " ", registry, null));
        Assert.Throws<System.ArgumentNullException>(() => factory.CreateFromUnitExpressionOrThrow(1.0d, "m", null!, null));
        Assert.Throws<System.InvalidOperationException>(() => factory.CreateFromUnitIdOrThrow(1.0d, "missing", registry, null));
        Assert.Throws<System.ArgumentException>(() => factory.CreateFromUnitExpressionOrThrow(1.0d, "m/", registry, null));
        Assert.Throws<System.InvalidOperationException>(() => factory.CreateFromUnitSymbolOrThrow(1.0d, "s", registry, new StandardQuantityKinds().Length()));
    }

    [Test]
    public void AmbiguityCandidates_AreReturnedAsDefensiveCopies()
    {
        QuantityFactory factory = new QuantityFactory(new UnitParser());
        UnitRegistry registry = CreateAmbiguousRegistry();

        QuantityConstructionResult result = factory.CreateFromUnitSymbol(1.0d, "u", registry, null);
        UnitDefinition[] candidates = result.GetCandidates();
        UnitDefinition expectedFirst = candidates[0];
        candidates[0] = candidates[1];

        Assert.That(result.GetCandidates()[0], Is.SameAs(expectedFirst));
    }

    static UnitRegistry CreateAmbiguousRegistry()
    {
        UnitRegistry registry = new UnitRegistry();
        registry.AddUnit(CreateContextUnit("alpha", "u"));
        registry.AddUnit(CreateContextUnit("beta", "u"));
        return registry;
    }

    static UnitDefinition CreateContextUnit(string id, string symbol)
    {
        return new UnitDefinition(id, id, id, symbol, new string[0], DimensionVector.Length(), new RationalNumber(1, 1), new RationalNumber(0, 1), true, UnitConversionKind.Scale);
    }
}
