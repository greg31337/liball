using fake;

namespace fake.Tests;

[TestFixture]
public class CommerceExperienceTests
{
    [Test]
    public void ProductProfilesAreDeterministicCorrelatedSyntheticFixtures()
    {
        const long seed = 11090;
        ProductProfile first = Fake.Create(seed).Commerce().Profile();
        ProductProfile second = Fake.Create(seed).Commerce().Profile();

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.Not.SameAs(first));
            AssertProfiles(first, second);
            Assert.That(first.Name,
                Is.EqualTo(first.Adjective + " " + first.Material + " " + first.Component));
            Assert.That(first.Description, Is.EqualTo(
                "A " + first.Adjective.ToLowerInvariant() + " " +
                first.Material.ToLowerInvariant() + " " +
                first.Component.ToLowerInvariant() + " for " + first.Category + "."));
            Assert.That(first.Sku, Does.Match("^SKU-[A-Za-z0-9]{4}-[0-9]{4}$"));
            Assert.That(Fake.Create(seed).Commerce().ValidateGtin(first.Gtin), Is.True);
            Assert.That(first.Gtin, Has.Length.EqualTo(14));
            Assert.That(first.Dimensions.Volume, Is.EqualTo(decimal.Round(
                first.Dimensions.Length * first.Dimensions.Width * first.Dimensions.Height,
                3)));
            Assert.That(first.Price, Is.InRange(0.01m, 1000m));
            Assert.That(first.CurrencyCode, Is.EqualTo("USD"));
            Assert.That(first.Inventory, Is.InRange(0, 1000));
            Assert.That(first.IsSynthetic, Is.True);
            Assert.That(first.TestMarker, Is.EqualTo("fake.synthetic-product.v1"));
            Assert.That(first.RepresentsRealProduct, Is.False);
            Assert.That(first.IdentifiersAreAllocated, Is.False);
            Assert.That(first.RepresentsMarketPrice, Is.False);
        });
    }

    [Test]
    public void ReviewsKeepTitlesAndTextConsistentWithRatings()
    {
        CommerceProvider commerce = Fake.Create(11091).Commerce();
        string[] titles = new string[]
        {
            "Needs improvement", "Below expectations", "Met expectations",
            "Worked well", "Excellent for my needs"
        };
        string[][] texts = new string[][]
        {
            new string[]
            {
                "The fixture was not a good match for my example scenario.",
                "I would choose a different option for this test case."
            },
            new string[]
            {
                "The fixture covered some needs but left room for improvement.",
                "It was usable, though another option may suit the scenario better."
            },
            new string[]
            {
                "The fixture matched the basic expectations of my sample scenario.",
                "It provided a straightforward result for an ordinary test case."
            },
            new string[]
            {
                "The fixture worked well for the example scenario I had in mind.",
                "It was a practical choice for this synthetic use case."
            },
            new string[]
            {
                "The fixture was an excellent match for my example scenario.",
                "It handled the synthetic use case exactly as expected."
            }
        };

        int index = 0;
        while (index < 128)
        {
            ProductReview review = commerce.Review();
            Assert.Multiple(() =>
            {
                Assert.That(review.Rating, Is.InRange(1, 5));
                Assert.That(review.Title, Is.EqualTo(titles[review.Rating - 1]));
                Assert.That(review.Text, Is.AnyOf(texts[review.Rating - 1]));
                Assert.That(review.IsSynthetic, Is.True);
                Assert.That(review.TestMarker,
                    Is.EqualTo("fake.synthetic-product-review.v1"));
                Assert.That(review.RepresentsVerifiedPurchase, Is.False);
            });
            index++;
        }
    }

    [Test]
    public void StandaloneRatingAndReviewTextApisAreDeterministic()
    {
        const long seed = 11092;
        CommerceProvider components = Fake.Create(seed).Commerce();
        int expectedRating = components.Rating();
        string[] titles = new string[]
        {
            "Needs improvement", "Below expectations", "Met expectations",
            "Worked well", "Excellent for my needs"
        };
        string firstText = Fake.Create(seed).Commerce().ReviewText();
        string secondText = Fake.Create(seed).Commerce().ReviewText();

        Assert.Multiple(() =>
        {
            Assert.That(Fake.Create(seed).Commerce().ReviewTitle(),
                Is.EqualTo(titles[expectedRating - 1]));
            Assert.That(firstText, Is.EqualTo(secondText));
        });
    }

    [Test]
    public void PromotionsUseVisibleNonRedeemableTestCoupons()
    {
        CommerceProvider commerce = Fake.Create(11093).Commerce();

        int index = 0;
        while (index < 128)
        {
            PromotionRecord promotion = commerce.Promotion();
            Assert.Multiple(() =>
            {
                Assert.That(promotion.Name, Is.AnyOf(
                    "Catalog test discount", "Fixture bundle savings",
                    "Sample seasonal offer", "Synthetic customer offer",
                    "Test checkout promotion"));
                Assert.That(promotion.DiscountPercent, Is.InRange(5, 50));
                Assert.That(promotion.DiscountPercent % 5, Is.Zero);
                Assert.That(promotion.CouponCode,
                    Does.StartWith("TEST" + promotion.DiscountPercent + "-"));
                Assert.That(promotion.CouponCode, Does.Match("^TEST[0-9]{1,2}-[0-9]{4}$"));
                Assert.That(promotion.IsSynthetic, Is.True);
                Assert.That(promotion.TestMarker,
                    Is.EqualTo("fake.synthetic-promotion.v1"));
                Assert.That(promotion.RepresentsRedeemableOffer, Is.False);
            });
            index++;
        }

        string coupon = Fake.Create(11093).Commerce().Coupon();
        Assert.Multiple(() =>
        {
            Assert.That(coupon, Does.Match("^TEST(5|10|15|20|25|30|35|40|45|50)-[0-9]{4}$"));
            Assert.That(Fake.Create(11093).Commerce().CouponCode(), Is.EqualTo(coupon));
        });
    }

    [Test]
    public void SubscriptionPlansHaveCorrelatedIntervalPricesAndSafetyMetadata()
    {
        CommerceProvider commerce = Fake.Create(11094).Commerce();

        int index = 0;
        while (index < 128)
        {
            SubscriptionPlan plan = commerce.SubscriptionPlan();
            Assert.Multiple(() =>
            {
                Assert.That(plan.Name,
                    Is.AnyOf("Starter", "Standard", "Expanded", "Complete"));
                Assert.That(plan.Status, Is.AnyOf(
                    SubscriptionStatus.Trial, SubscriptionStatus.Active,
                    SubscriptionStatus.Paused, SubscriptionStatus.Cancelled,
                    SubscriptionStatus.Expired));
                Assert.That(plan.Interval, Is.AnyOf(
                    SubscriptionInterval.Weekly, SubscriptionInterval.Monthly,
                    SubscriptionInterval.Quarterly, SubscriptionInterval.Yearly));
                Assert.That(plan.Price,
                    Is.EqualTo(ExpectedSubscriptionPrice(plan.Name, plan.Interval)));
                Assert.That(plan.CurrencyCode, Is.EqualTo("USD"));
                Assert.That(plan.IsSynthetic, Is.True);
                Assert.That(plan.TestMarker,
                    Is.EqualTo("fake.synthetic-subscription.v1"));
                Assert.That(plan.RepresentsRealSubscription, Is.False);
            });
            index++;
        }
    }

    [Test]
    public void SubscriptionComponentsAndReturnReasonsUseDeclaredSets()
    {
        CommerceProvider commerce = Fake.Create(11095).Commerce();

        int index = 0;
        while (index < 64)
        {
            Assert.Multiple(() =>
            {
                Assert.That(commerce.SubscriptionPlanName(),
                    Is.AnyOf("Starter", "Standard", "Expanded", "Complete"));
                Assert.That(commerce.SubscriptionStatus(), Is.AnyOf(
                    SubscriptionStatus.Trial, SubscriptionStatus.Active,
                    SubscriptionStatus.Paused, SubscriptionStatus.Cancelled,
                    SubscriptionStatus.Expired));
                Assert.That(commerce.SubscriptionInterval(), Is.AnyOf(
                    SubscriptionInterval.Weekly, SubscriptionInterval.Monthly,
                    SubscriptionInterval.Quarterly, SubscriptionInterval.Yearly));
                Assert.That(commerce.ReturnReason(), Is.AnyOf(
                    "Changed preference", "Different size needed", "No longer needed",
                    "Packaging was damaged", "Product did not fit", "Wrong item selected"));
                Assert.That(commerce.RefundReason(), Is.AnyOf(
                    "Billing correction requested", "Duplicate order placed",
                    "Item was returned", "Order was cancelled",
                    "Shipment did not arrive", "Wrong item received"));
            });
            index++;
        }
    }

    [Test]
    public void ExperienceApisHonorLimitsBeforeConsumingEntropy()
    {
        const long seed = 11096;
        FakeOptions reviewOptions = FakeOptions.Create(seed);
        reviewOptions.MaximumOutputSize = 80;
        FakeOptions countOptions = FakeOptions.Create(seed);
        countOptions.MaximumCount = 9;
        FakeOptions reasonOptions = FakeOptions.Create(seed);
        reasonOptions.MaximumCount = 5;
        CommerceProvider inspected = Fake.Create(reviewOptions).Commerce();
        CommerceProvider untouched = Fake.Create(reviewOptions).Commerce();

        Assert.Multiple(() =>
        {
            Assert.That(() => inspected.Review(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(inspected.RandomValue.Long(), Is.EqualTo(untouched.RandomValue.Long()));
            Assert.That(() => Fake.Create(countOptions).Commerce().Coupon(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => Fake.Create(reasonOptions).Commerce().ReturnReason(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => Fake.Create(reasonOptions).Commerce().RefundReason(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void ProductExperienceGenerationDoesNotPerturbOtherProviderStreams()
    {
        Fake inspected = Fake.Create(11097);
        Fake untouched = Fake.Create(11097);

        int index = 0;
        while (index < 16)
        {
            inspected.Commerce().Profile();
            inspected.Commerce().Review();
            inspected.Commerce().Promotion();
            inspected.Commerce().SubscriptionPlan();
            inspected.Commerce().ReturnReason();
            inspected.Commerce().RefundReason();
            index++;
        }

        Assert.That(inspected.Person().FullName(), Is.EqualTo(untouched.Person().FullName()));
    }

    static decimal ExpectedSubscriptionPrice(
        string name,
        SubscriptionInterval interval)
    {
        decimal monthly = name == "Starter"
            ? 9m
            : name == "Standard"
                ? 19m
                : name == "Expanded"
                    ? 39m
                    : 79m;
        return interval == SubscriptionInterval.Weekly
            ? decimal.Round(monthly / 4m, 2)
            : interval == SubscriptionInterval.Quarterly
                ? decimal.Round(monthly * 2.7m, 2)
                : interval == SubscriptionInterval.Yearly
                    ? decimal.Round(monthly * 9.6m, 2)
                    : monthly;
    }

    static void AssertProfiles(ProductProfile expected, ProductProfile actual)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Component, Is.EqualTo(expected.Component));
            Assert.That(actual.Adjective, Is.EqualTo(expected.Adjective));
            Assert.That(actual.Name, Is.EqualTo(expected.Name));
            Assert.That(actual.Category, Is.EqualTo(expected.Category));
            Assert.That(actual.Brand, Is.EqualTo(expected.Brand));
            Assert.That(actual.Description, Is.EqualTo(expected.Description));
            Assert.That(actual.Material, Is.EqualTo(expected.Material));
            Assert.That(actual.Color, Is.EqualTo(expected.Color));
            Assert.That(actual.Sku, Is.EqualTo(expected.Sku));
            Assert.That(actual.Gtin, Is.EqualTo(expected.Gtin));
            Assert.That(actual.Dimensions.Length, Is.EqualTo(expected.Dimensions.Length));
            Assert.That(actual.Dimensions.Width, Is.EqualTo(expected.Dimensions.Width));
            Assert.That(actual.Dimensions.Height, Is.EqualTo(expected.Dimensions.Height));
            Assert.That(actual.Dimensions.Volume, Is.EqualTo(expected.Dimensions.Volume));
            Assert.That(actual.Dimensions.Unit, Is.EqualTo(expected.Dimensions.Unit));
            Assert.That(actual.Weight, Is.EqualTo(expected.Weight));
            Assert.That(actual.Price, Is.EqualTo(expected.Price));
            Assert.That(actual.CurrencyCode, Is.EqualTo(expected.CurrencyCode));
            Assert.That(actual.Inventory, Is.EqualTo(expected.Inventory));
        });
    }
}
