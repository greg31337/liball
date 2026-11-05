using System;

namespace fake;

internal static class ProductGenerator
{
    const string ProfileMarker = "fake.synthetic-product.v1";

    internal static string Component(GenerationContext context, FakeRandom random)
    {
        return Value(context, random, ProductData.Components, "productComponent");
    }

    internal static string Adjective(GenerationContext context, FakeRandom random)
    {
        return Value(context, random, ProductData.Adjectives, "productAdjective");
    }

    internal static string Category(GenerationContext context, FakeRandom random)
    {
        return Value(context, random, ProductData.Categories, "productCategory");
    }

    internal static string Brand(GenerationContext context, FakeRandom random)
    {
        return Value(context, random, ProductData.Brands, "productBrand");
    }

    internal static string Material(GenerationContext context, FakeRandom random)
    {
        return Value(context, random, ProductData.Materials, "productMaterial");
    }

    internal static string Color(GenerationContext context, FakeRandom random)
    {
        return Value(context, random, ProductData.Colors, "productColor");
    }

    internal static string Name(GenerationContext context, FakeRandom random)
    {
        ValidateValues(context, ProductData.Adjectives, "productName");
        ValidateValues(context, ProductData.Materials, "productName");
        ValidateValues(context, ProductData.Components, "productName");
        Guard.OutputSize(
            MaximumLength(ProductData.Adjectives) +
                MaximumLength(ProductData.Materials) +
                MaximumLength(ProductData.Components) + 2,
            context.CapturedOptions.MaximumOutputSize,
            "productName");
        string result = ChooseValue(random, ProductData.Adjectives) + " " +
            ChooseValue(random, ProductData.Materials) + " " +
            ChooseValue(random, ProductData.Components);
        return Normalize(context, result);
    }

    internal static string Description(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(context, ProductData.Adjectives, "productDescription");
        ValidateValues(context, ProductData.Materials, "productDescription");
        ValidateValues(context, ProductData.Components, "productDescription");
        ValidateValues(context, ProductData.Categories, "productDescription");
        Guard.OutputSize(
            MaximumLength(ProductData.Adjectives) +
                MaximumLength(ProductData.Materials) +
                MaximumLength(ProductData.Components) +
                MaximumLength(ProductData.Categories) + 10,
            context.CapturedOptions.MaximumOutputSize,
            "productDescription");
        string result = "A " +
            ChooseValue(random, ProductData.Adjectives).ToLowerInvariant() + " " +
            ChooseValue(random, ProductData.Materials).ToLowerInvariant() + " " +
            ChooseValue(random, ProductData.Components).ToLowerInvariant() +
            " for " + ChooseValue(random, ProductData.Categories) + ".";
        return Normalize(context, result);
    }

    internal static string Gtin(
        GenerationContext context,
        FakeRandom random,
        int length)
    {
        return Gs1TradeItemIdentifier.Generate(
            random,
            length,
            context.CapturedOptions.MaximumCount,
            context.CapturedOptions.MaximumOutputSize);
    }

    internal static bool ValidateGtin(GenerationContext context, string value)
    {
        Guard.NotNull(value, "value");
        return Gs1TradeItemIdentifier.Validate(
            value.AsSpan(),
            context.CapturedOptions.MaximumCount);
    }

    internal static decimal Dimension(FakeRandom random)
    {
        return random.IntFromMinimumAndMaximum(10, 2001) / 10m;
    }

    internal static ProductDimensions Dimensions(
        GenerationContext context,
        FakeRandom random)
    {
        Guard.OutputSize(
            "centimeters".Length,
            context.CapturedOptions.MaximumOutputSize,
            "productDimensions");
        ProductDimensions result = new ProductDimensions();
        result.Length = Dimension(random);
        result.Width = Dimension(random);
        result.Height = Dimension(random);
        result.Volume = decimal.Round(
            result.Length * result.Width * result.Height,
            3,
            MidpointRounding.ToEven);
        result.Unit = "centimeters";
        return result;
    }

    internal static decimal Weight(FakeRandom random)
    {
        return random.IntFromMinimumAndMaximum(5, 5001) / 100m;
    }

    internal static decimal Price(FakeRandom random)
    {
        return MoneyGenerator.DefaultPrice(random);
    }

    internal static int Inventory(FakeRandom random)
    {
        return random.IntFromMinimumAndMaximum(0, 1001);
    }

    internal static ProductProfile Profile(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(context, ProductData.Adjectives, "productProfile");
        ValidateValues(context, ProductData.Materials, "productProfile");
        ValidateValues(context, ProductData.Components, "productProfile");
        ValidateValues(context, ProductData.Categories, "productProfile");
        ValidateValues(context, ProductData.Brands, "productProfile");
        ValidateValues(context, ProductData.Colors, "productProfile");
        string adjective = ChooseValue(random, ProductData.Adjectives);
        string material = ChooseValue(random, ProductData.Materials);
        string component = ChooseValue(random, ProductData.Components);
        string category = ChooseValue(random, ProductData.Categories);
        ProductProfile result = new ProductProfile();
        result.Component = Normalize(context, component);
        result.Adjective = Normalize(context, adjective);
        result.Name = Normalize(
            context,
            adjective + " " + material + " " + component);
        result.Category = Normalize(context, category);
        result.Brand = Normalize(
            context,
            ChooseValue(random, ProductData.Brands));
        result.Description = Normalize(
            context,
            "A " + adjective.ToLowerInvariant() + " " +
                material.ToLowerInvariant() + " " +
                component.ToLowerInvariant() + " for " + category + ".");
        result.Material = Normalize(context, material);
        result.Color = Normalize(
            context,
            ChooseValue(random, ProductData.Colors));
        result.Sku = GenericIdentifierGenerator.Generate(
            context,
            random,
            "commerce.sku.masks",
            GenericIdentifierGenerator.SkuMask);
        result.Gtin = Gtin(context, random, 14);
        result.Dimensions = Dimensions(context, random);
        result.Weight = Weight(random);
        result.Price = Price(random);
        result.CurrencyCode = "USD";
        result.Inventory = Inventory(random);
        result.IsSynthetic = true;
        result.TestMarker = ProfileMarker;
        result.RepresentsRealProduct = false;
        result.IdentifiersAreAllocated = false;
        result.RepresentsMarketPrice = false;
        Guard.OutputSize(
            ProfileSize(result),
            context.CapturedOptions.MaximumOutputSize,
            "productProfile");
        return result;
    }

    static string Value(
        GenerationContext context,
        FakeRandom random,
        string[] values,
        string parameterName)
    {
        ValidateValues(context, values, parameterName);
        return Normalize(context, ChooseValue(random, values));
    }

    static void ValidateValues(
        GenerationContext context,
        string[] values,
        string parameterName)
    {
        Guard.Count(
            values.Length,
            context.CapturedOptions.MaximumCount,
            "productData");
        int index = 0;
        while (index < values.Length)
        {
            string normalized = Normalize(context, values[index]);
            Guard.OutputSize(
                normalized.Length,
                context.CapturedOptions.MaximumOutputSize,
                parameterName);
            index = index + 1;
        }
    }

    static string Normalize(GenerationContext context, string value)
    {
        return UnicodeText.Normalize(
            value,
            context.NormalizationModeValue,
            context.CapturedOptions.MaximumOutputSize);
    }

    static string ChooseValue(FakeRandom random, string[] values)
    {
        return values[random.IntFromMaximumExclusive(values.Length)];
    }

    static int MaximumLength(string[] values)
    {
        int result = 0;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index].Length > result)
            {
                result = values[index].Length;
            }
            index = index + 1;
        }
        return result;
    }

    static int ProfileSize(ProductProfile value)
    {
        return value.Component.Length + value.Adjective.Length +
            value.Name.Length + value.Category.Length + value.Brand.Length +
            value.Description.Length + value.Material.Length + value.Color.Length +
            value.Sku.Length + value.Gtin.Length + value.Dimensions.Unit.Length +
            value.CurrencyCode.Length + value.TestMarker.Length;
    }
}
