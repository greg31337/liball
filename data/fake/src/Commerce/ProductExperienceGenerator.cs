using System;
using System.Globalization;

namespace fake;

internal static class ProductExperienceGenerator
{
    const string ReviewMarker = "fake.synthetic-product-review.v1";
    const string PromotionMarker = "fake.synthetic-promotion.v1";
    const string SubscriptionMarker = "fake.synthetic-subscription.v1";

    internal static int Rating(FakeRandom random)
    {
        return random.IntFromMinimumAndMaximum(1, 6);
    }

    internal static string ReviewTitle(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(context, ProductData.ReviewTitles, "reviewTitle");
        return Normalize(
            context,
            ProductData.ReviewTitles[Rating(random) - 1]);
    }

    internal static string ReviewText(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(context, ProductData.ReviewTexts, "reviewText");
        int rating = Rating(random);
        return Normalize(context, ReviewTextUnchecked(random, rating));
    }

    internal static ProductReview Review(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(context, ProductData.ReviewTitles, "productReview");
        ValidateValues(context, ProductData.ReviewTexts, "productReview");
        Guard.OutputSize(
            MaximumLength(ProductData.ReviewTitles) +
                MaximumLength(ProductData.ReviewTexts) + ReviewMarker.Length,
            context.CapturedOptions.MaximumOutputSize,
            "productReview");
        int rating = Rating(random);
        ProductReview result = new ProductReview();
        result.Rating = rating;
        result.Title = Normalize(
            context,
            ProductData.ReviewTitles[rating - 1]);
        result.Text = Normalize(
            context,
            ReviewTextUnchecked(random, rating));
        result.IsSynthetic = true;
        result.TestMarker = ReviewMarker;
        result.RepresentsVerifiedPurchase = false;
        return result;
    }

    internal static string PromotionName(
        GenerationContext context,
        FakeRandom random)
    {
        return Value(
            context,
            random,
            ProductData.PromotionNames,
            "promotionName");
    }

    internal static int DiscountPercent(
        GenerationContext context,
        FakeRandom random)
    {
        Guard.Count(10, context.CapturedOptions.MaximumCount, "discountOptions");
        return random.IntFromMinimumAndMaximum(1, 11) * 5;
    }

    internal static string Coupon(
        GenerationContext context,
        FakeRandom random)
    {
        Guard.Count(10, context.CapturedOptions.MaximumCount, "discountOptions");
        Guard.Count(4, context.CapturedOptions.MaximumCount, "couponDigits");
        Guard.OutputSize(
            11,
            context.CapturedOptions.MaximumOutputSize,
            "couponCode");
        int discount = DiscountPercent(context, random);
        return CouponUnchecked(context, random, discount);
    }

    internal static PromotionRecord Promotion(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(context, ProductData.PromotionNames, "promotion");
        Guard.Count(10, context.CapturedOptions.MaximumCount, "discountOptions");
        Guard.Count(4, context.CapturedOptions.MaximumCount, "couponDigits");
        Guard.OutputSize(
            MaximumLength(ProductData.PromotionNames) + 11 +
                PromotionMarker.Length,
            context.CapturedOptions.MaximumOutputSize,
            "promotion");
        int discount = DiscountPercent(context, random);
        PromotionRecord result = new PromotionRecord();
        result.Name = Normalize(
            context,
            ChooseValue(random, ProductData.PromotionNames));
        result.CouponCode = CouponUnchecked(context, random, discount);
        result.DiscountPercent = discount;
        result.IsSynthetic = true;
        result.TestMarker = PromotionMarker;
        result.RepresentsRedeemableOffer = false;
        return result;
    }

    internal static string SubscriptionPlanName(
        GenerationContext context,
        FakeRandom random)
    {
        return Value(
            context,
            random,
            ProductData.SubscriptionPlanNames,
            "subscriptionPlanName");
    }

    internal static SubscriptionStatus GenerateSubscriptionStatus(
        GenerationContext context,
        FakeRandom random)
    {
        Guard.Count(5, context.CapturedOptions.MaximumCount, "subscriptionStatuses");
        return (SubscriptionStatus)random.IntFromMaximumExclusive(5);
    }

    internal static SubscriptionInterval GenerateSubscriptionInterval(
        GenerationContext context,
        FakeRandom random)
    {
        Guard.Count(4, context.CapturedOptions.MaximumCount, "subscriptionIntervals");
        return (SubscriptionInterval)random.IntFromMaximumExclusive(4);
    }

    internal static SubscriptionPlan SubscriptionPlan(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(
            context,
            ProductData.SubscriptionPlanNames,
            "subscriptionPlan");
        Guard.Count(5, context.CapturedOptions.MaximumCount, "subscriptionStatuses");
        Guard.Count(4, context.CapturedOptions.MaximumCount, "subscriptionIntervals");
        Guard.OutputSize(
            MaximumLength(ProductData.SubscriptionPlanNames) + 3 +
                SubscriptionMarker.Length,
            context.CapturedOptions.MaximumOutputSize,
            "subscriptionPlan");
        int planIndex = random.IntFromMaximumExclusive(ProductData.SubscriptionPlanNames.Length);
        SubscriptionInterval interval = GenerateSubscriptionInterval(context, random);
        SubscriptionPlan result = new SubscriptionPlan();
        result.Name = Normalize(
            context,
            ProductData.SubscriptionPlanNames[planIndex]);
        result.Status = GenerateSubscriptionStatus(context, random);
        result.Interval = interval;
        result.Price = SubscriptionPrice(planIndex, interval);
        result.CurrencyCode = "USD";
        result.IsSynthetic = true;
        result.TestMarker = SubscriptionMarker;
        result.RepresentsRealSubscription = false;
        return result;
    }

    internal static string ReturnReason(
        GenerationContext context,
        FakeRandom random)
    {
        return Value(
            context,
            random,
            ProductData.ReturnReasons,
            "returnReason");
    }

    internal static string RefundReason(
        GenerationContext context,
        FakeRandom random)
    {
        return Value(
            context,
            random,
            ProductData.RefundReasons,
            "refundReason");
    }

    static string ReviewTextUnchecked(FakeRandom random, int rating)
    {
        int start = (rating - 1) * 2;
        return ProductData.ReviewTexts[start + random.IntFromMaximumExclusive(2)];
    }

    static string CouponUnchecked(
        GenerationContext context,
        FakeRandom random,
        int discount)
    {
        Guard.Count(4, context.CapturedOptions.MaximumCount, "couponDigits");
        Guard.OutputSize(
            11,
            context.CapturedOptions.MaximumOutputSize,
            "couponCode");
        return "TEST" + discount.ToString(CultureInfo.InvariantCulture) + "-" +
            NumericStringGenerator.Digits(
                random,
                4,
                context.CapturedOptions.MaximumOutputSize);
    }

    static decimal SubscriptionPrice(
        int planIndex,
        SubscriptionInterval interval)
    {
        decimal monthly = 9m;
        switch (planIndex)
        {
            case 1:
                monthly = 19m;
                break;
            case 2:
                monthly = 39m;
                break;
            case 3:
                monthly = 79m;
                break;
        }
        switch (interval)
        {
            case SubscriptionInterval.Weekly:
                return decimal.Round(monthly / 4m, 2, MidpointRounding.ToEven);
            case SubscriptionInterval.Quarterly:
                return decimal.Round(monthly * 2.7m, 2, MidpointRounding.ToEven);
            case SubscriptionInterval.Yearly:
                return decimal.Round(monthly * 9.6m, 2, MidpointRounding.ToEven);
        }
        return monthly;
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
            "productExperienceData");
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
}
