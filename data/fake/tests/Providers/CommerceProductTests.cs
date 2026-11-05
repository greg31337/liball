using fake;

namespace fake.Tests;

[TestFixture]
public class CommerceProductTests
{
    [Test]
    public void ProductTextUsesAuthoredGenericVocabularies()
    {
        CommerceProvider commerce = Fake.Create(11080).Commerce();

        int index = 0;
        while (index < 128)
        {
            Assert.Multiple(() =>
            {
                Assert.That(commerce.Product(), Is.AnyOf(
                    "Basket", "Bottle", "Case", "Lamp", "Notebook", "Organizer",
                    "Stand", "Tray"));
                Assert.That(commerce.ProductAdjective(), Is.AnyOf(
                    "Compact", "Durable", "Lightweight", "Modular", "Portable",
                    "Practical", "Stackable", "Versatile"));
                Assert.That(commerce.Category(), Is.AnyOf(
                    "desk accessories", "home organization", "kitchen tools",
                    "office supplies", "outdoor accessories", "storage supplies",
                    "travel accessories", "workshop supplies"));
                Assert.That(commerce.Brand(), Is.AnyOf(
                    "Cedar & Finch", "Harbor Plain", "Lantern Field", "Northbridge Goods",
                    "Silver Meadow", "Stone & Grove", "Summit Workshop", "Willow Current"));
                Assert.That(commerce.ProductMaterial(), Is.AnyOf(
                    "Aluminum", "Bamboo", "Canvas", "Ceramic", "Cork",
                    "Recycled Plastic", "Stainless Steel", "Wood"));
                Assert.That(commerce.Color(), Is.AnyOf(
                    "Amber", "Charcoal", "Forest Green", "Ivory", "Navy", "Slate",
                    "Terracotta", "White"));
            });
            index++;
        }
    }

    [Test]
    public void ProductNamesAndDescriptionsAreDeterministicComponentCompositions()
    {
        const long seed = 11081;
        CommerceProvider nameParts = Fake.Create(seed).Commerce();
        string expectedName = nameParts.ProductAdjective() + " " +
            nameParts.ProductMaterial() + " " + nameParts.Product();
        CommerceProvider descriptionParts = Fake.Create(seed).Commerce();
        string expectedDescription = "A " +
            descriptionParts.ProductAdjective().ToLowerInvariant() + " " +
            descriptionParts.ProductMaterial().ToLowerInvariant() + " " +
            descriptionParts.Product().ToLowerInvariant() + " for " +
            descriptionParts.Category() + ".";

        Assert.Multiple(() =>
        {
            Assert.That(Fake.Create(seed).Commerce().ProductName(), Is.EqualTo(expectedName));
            Assert.That(Fake.Create(seed).Commerce().ProductDescription(),
                Is.EqualTo(expectedDescription));
            Assert.That(Fake.Create(seed).Commerce().ProductComponent(),
                Is.EqualTo(Fake.Create(seed).Commerce().Product()));
            Assert.That(Fake.Create(seed).Commerce().ProductCategory(),
                Is.EqualTo(Fake.Create(seed).Commerce().Category()));
            Assert.That(Fake.Create(seed).Commerce().Material(),
                Is.EqualTo(Fake.Create(seed).Commerce().ProductMaterial()));
            Assert.That(Fake.Create(seed).Commerce().ProductColor(),
                Is.EqualTo(Fake.Create(seed).Commerce().Color()));
        });
    }

    [Test]
    public void CommerceIdentifiersIncludeSkuAndChecksumValidGtins()
    {
        CommerceProvider commerce = Fake.Create(11082).Commerce();
        int[] lengths = new int[] { 8, 12, 13, 14 };

        Assert.That(commerce.Sku(), Does.Match("^SKU-[A-Za-z0-9]{4}-[0-9]{4}$"));
        int index = 0;
        while (index < lengths.Length)
        {
            string value = commerce.GtinFromLength(lengths[index]);
            Assert.Multiple(() =>
            {
                Assert.That(value, Does.Match("^[0-9]+$"));
                Assert.That(value.Length, Is.EqualTo(lengths[index]));
                Assert.That(commerce.ValidateGtin(value), Is.True);
            });
            index++;
        }
        string defaultValue = commerce.Gtin();
        string firstDeterministic = Fake.Create(11082).Commerce().Gtin();
        string secondDeterministic = Fake.Create(11082).Commerce().Gtin();
        Assert.Multiple(() =>
        {
            Assert.That(defaultValue.Length, Is.EqualTo(14));
            Assert.That(commerce.ValidateGtin(defaultValue), Is.True);
            Assert.That(commerce.ValidateGtin(defaultValue.Substring(0, 13) +
                (defaultValue[13] == '9' ? "0" : "9")), Is.False);
            Assert.That(firstDeterministic, Is.EqualTo(secondDeterministic));
        });
    }

    [Test]
    public void DimensionsWeightPriceAndInventoryAreBoundedAndDeterministic()
    {
        const long seed = 11083;
        CommerceProvider components = Fake.Create(seed).Commerce();
        ProductDimensions dimensions = Fake.Create(seed).Commerce().Dimensions();

        Assert.Multiple(() =>
        {
            Assert.That(dimensions.Length, Is.EqualTo(components.Dimension()));
            Assert.That(dimensions.Width, Is.EqualTo(components.Dimension()));
            Assert.That(dimensions.Height, Is.EqualTo(components.Dimension()));
            Assert.That(dimensions.Volume, Is.EqualTo(decimal.Round(
                dimensions.Length * dimensions.Width * dimensions.Height, 3)));
            Assert.That(dimensions.Unit, Is.EqualTo("centimeters"));
        });

        CommerceProvider commerce = Fake.Create(seed).Commerce();
        int index = 0;
        while (index < 256)
        {
            decimal dimension = commerce.Dimension();
            decimal weight = commerce.Weight();
            decimal price = commerce.Price();
            int inventory = commerce.Inventory();
            Assert.Multiple(() =>
            {
                Assert.That(dimension, Is.InRange(1.0m, 200.0m));
                Assert.That(dimension * 10m, Is.EqualTo(decimal.Truncate(dimension * 10m)));
                Assert.That(weight, Is.InRange(0.05m, 50.00m));
                Assert.That(weight * 100m, Is.EqualTo(decimal.Truncate(weight * 100m)));
                Assert.That(price, Is.InRange(0.01m, 1000.00m));
                Assert.That(price, Is.EqualTo(decimal.Round(price, 2)));
                Assert.That(inventory, Is.InRange(0, 1000));
            });
            index++;
        }

        decimal firstWeight = Fake.Create(seed).Commerce().Weight();
        decimal secondWeight = Fake.Create(seed).Commerce().Weight();
        decimal firstPrice = Fake.Create(seed).Commerce().Price();
        decimal secondPrice = Fake.Create(seed).Commerce().Price();
        int firstInventory = Fake.Create(seed).Commerce().Inventory();
        int secondInventory = Fake.Create(seed).Commerce().Inventory();
        Assert.Multiple(() =>
        {
            Assert.That(firstWeight, Is.EqualTo(secondWeight));
            Assert.That(firstPrice, Is.EqualTo(secondPrice));
            Assert.That(firstInventory, Is.EqualTo(secondInventory));
        });
    }

    [Test]
    public void ProductGenerationHonorsLimitsBeforeConsumingEntropy()
    {
        const long seed = 11084;
        FakeOptions countOptions = FakeOptions.Create(seed);
        countOptions.MaximumCount = 7;
        FakeOptions outputOptions = FakeOptions.Create(seed);
        outputOptions.MaximumOutputSize = 20;
        FakeOptions dimensionOptions = FakeOptions.Create(seed);
        dimensionOptions.MaximumOutputSize = 10;
        CommerceProvider inspected = Fake.Create(outputOptions).Commerce();
        CommerceProvider untouched = Fake.Create(outputOptions).Commerce();

        Assert.Multiple(() =>
        {
            Assert.That(() => Fake.Create(countOptions).Commerce().ProductName(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.ProductDescription(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(inspected.RandomValue.Long(), Is.EqualTo(untouched.RandomValue.Long()));
            Assert.That(() => Fake.Create(dimensionOptions).Commerce().Dimensions(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void InvalidGtinInputAndLimitsDoNotConsumeEntropy()
    {
        const long seed = 11085;
        CommerceProvider inspected = Fake.Create(seed).Commerce();
        CommerceProvider untouched = Fake.Create(seed).Commerce();
        FakeOptions countOptions = FakeOptions.Create(seed);
        countOptions.MaximumCount = 12;

        Assert.Multiple(() =>
        {
            Assert.That(() => inspected.GtinFromLength(9),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.ValidateGtin(null),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(inspected.RandomValue.Long(), Is.EqualTo(untouched.RandomValue.Long()));
            Assert.That(() => Fake.Create(countOptions).Commerce().Gtin(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void ProductGenerationDoesNotPerturbOtherProviderStreams()
    {
        Fake inspected = Fake.Create(11086);
        Fake untouched = Fake.Create(11086);

        int index = 0;
        while (index < 16)
        {
            inspected.Commerce().ProductName();
            inspected.Commerce().ProductDescription();
            inspected.Commerce().Dimensions();
            inspected.Commerce().Gtin();
            index++;
        }

        Assert.That(inspected.Person().FullName(), Is.EqualTo(untouched.Person().FullName()));
    }
}
