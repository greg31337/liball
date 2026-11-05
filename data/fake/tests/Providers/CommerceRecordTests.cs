using fake;

namespace fake.Tests;

[TestFixture]
public class CommerceRecordTests
{
    [Test]
    public void LineItemsAreDeterministicAndUseCurrencyMinorUnits()
    {
        const long seed = 11100;
        CommerceLineItem first = Fake.Create(seed).Commerce().LineItemFromQuantityAndCurrencyCode(3, "USD");
        CommerceLineItem second = Fake.Create(seed).Commerce().LineItemFromQuantityAndCurrencyCode(3, "USD");
        CommerceLineItem yen = Fake.Create(seed).Commerce().LineItemFromQuantityAndCurrencyCode(2, "JPY");
        CommerceLineItem dinar = Fake.Create(seed).Commerce().LineItemFromQuantityAndCurrencyCode(4, "kwd");

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.Not.SameAs(first));
            AssertLineEquivalent(first, second);
            AssertLine(first, 2);
            Assert.That(first.Quantity, Is.EqualTo(3));
            AssertLine(yen, 0);
            Assert.That(yen.CurrencyCode, Is.EqualTo("JPY"));
            AssertLine(dinar, 3);
            Assert.That(dinar.CurrencyCode, Is.EqualTo("KWD"));
            Assert.That(first.IsSynthetic, Is.True);
            Assert.That(first.TestMarker, Is.EqualTo("fake.synthetic-commerce.v1"));
        });
    }

    [Test]
    public void GeneratedCartsHaveConsistentQuantitiesAndTotals()
    {
        CommerceProvider commerce = Fake.Create(11101).Commerce();

        int index = 0;
        while (index < 64)
        {
            CommerceCart cart = commerce.Cart();
            Assert.Multiple(() =>
            {
                Assert.That(cart.LineCount, Is.InRange(1, 5));
                Assert.That(cart.Items, Has.Length.EqualTo(cart.LineCount));
                AssertCalculation(
                    cart.Items,
                    cart.Quantity,
                    cart.CurrencyCode,
                    cart.Subtotal,
                    cart.DiscountRate,
                    cart.DiscountAmount,
                    cart.TaxableAmount,
                    cart.TaxRate,
                    cart.TaxAmount,
                    cart.ShippingAmount,
                    cart.Total,
                    2);
                Assert.That(cart.IsSynthetic, Is.True);
                Assert.That(cart.TestMarker,
                    Is.EqualTo("fake.synthetic-commerce.v1"));
            });
            index++;
        }
    }

    [Test]
    public void ExactCartLineCountsAndCurrenciesArePreserved()
    {
        CommerceCart yen = Fake.Create(11102).Commerce().CartFromLineCountAndCurrencyCode(3, "JPY");
        CommerceCart dinar = Fake.Create(11102).Commerce().CartFromLineCountAndCurrencyCode(4, "KWD");

        Assert.Multiple(() =>
        {
            Assert.That(yen.LineCount, Is.EqualTo(3));
            AssertCalculation(
                yen.Items, yen.Quantity, yen.CurrencyCode, yen.Subtotal,
                yen.DiscountRate, yen.DiscountAmount, yen.TaxableAmount,
                yen.TaxRate, yen.TaxAmount, yen.ShippingAmount, yen.Total, 0);
            Assert.That(dinar.LineCount, Is.EqualTo(4));
            AssertCalculation(
                dinar.Items, dinar.Quantity, dinar.CurrencyCode, dinar.Subtotal,
                dinar.DiscountRate, dinar.DiscountAmount, dinar.TaxableAmount,
                dinar.TaxRate, dinar.TaxAmount, dinar.ShippingAmount, dinar.Total, 3);
        });
    }

    [Test]
    public void OrdersAndInvoicesHaveIdentifiersAndConsistentArithmetic()
    {
        CommerceOrder order = Fake.Create(11103).Commerce().OrderDetailsFromLineCountAndCurrencyCode(3, "KWD");
        CommerceInvoice invoice = Fake.Create(11104).Commerce().InvoiceDetailsFromLineCountAndCurrencyCode(2, "JPY");

        Assert.Multiple(() =>
        {
            Assert.That(order.Number, Does.Match("^ORD-[0-9]{10}$"));
            Assert.That(order.LineCount, Is.EqualTo(3));
            AssertCalculation(
                order.Items, order.Quantity, order.CurrencyCode, order.Subtotal,
                order.DiscountRate, order.DiscountAmount, order.TaxableAmount,
                order.TaxRate, order.TaxAmount, order.ShippingAmount, order.Total, 3);
            Assert.That(order.IsSynthetic, Is.True);
            Assert.That(order.IsSubmitted, Is.False);
            Assert.That(order.TestMarker,
                Is.EqualTo("fake.synthetic-commerce.v1"));

            Assert.That(invoice.Number, Does.Match("^INV-[0-9]{8}$"));
            Assert.That(invoice.OrderNumber, Does.Match("^ORD-[0-9]{10}$"));
            Assert.That(invoice.LineCount, Is.EqualTo(2));
            AssertCalculation(
                invoice.Items, invoice.Quantity, invoice.CurrencyCode,
                invoice.Subtotal, invoice.DiscountRate, invoice.DiscountAmount,
                invoice.TaxableAmount, invoice.TaxRate, invoice.TaxAmount,
                invoice.ShippingAmount, invoice.Total, 0);
            Assert.That(invoice.AmountDue, Is.EqualTo(invoice.Total));
            Assert.That(invoice.IsPaid, Is.False);
            Assert.That(invoice.IsSynthetic, Is.True);
            Assert.That(invoice.TestMarker,
                Is.EqualTo("fake.synthetic-commerce.v1"));
        });
    }

    [Test]
    public void AggregateModelsAreDeterministicNewInstances()
    {
        const long seed = 11105;
        CommerceCart firstCart = Fake.Create(seed).Commerce().CartFromLineCount(2);
        CommerceCart secondCart = Fake.Create(seed).Commerce().CartFromLineCount(2);
        CommerceOrder firstOrder = Fake.Create(seed).Commerce().OrderDetailsFromLineCount(2);
        CommerceOrder secondOrder = Fake.Create(seed).Commerce().OrderDetailsFromLineCount(2);
        CommerceInvoice firstInvoice = Fake.Create(seed).Commerce().InvoiceDetailsFromLineCount(2);
        CommerceInvoice secondInvoice = Fake.Create(seed).Commerce().InvoiceDetailsFromLineCount(2);

        Assert.Multiple(() =>
        {
            Assert.That(secondCart, Is.Not.SameAs(firstCart));
            Assert.That(secondCart.Items, Is.Not.SameAs(firstCart.Items));
            Assert.That(secondCart.Total, Is.EqualTo(firstCart.Total));
            Assert.That(secondCart.Items[0].Sku, Is.EqualTo(firstCart.Items[0].Sku));
            Assert.That(secondOrder, Is.Not.SameAs(firstOrder));
            Assert.That(secondOrder.Number, Is.EqualTo(firstOrder.Number));
            Assert.That(secondOrder.Total, Is.EqualTo(firstOrder.Total));
            Assert.That(secondInvoice, Is.Not.SameAs(firstInvoice));
            Assert.That(secondInvoice.Number, Is.EqualTo(firstInvoice.Number));
            Assert.That(secondInvoice.OrderNumber, Is.EqualTo(firstInvoice.OrderNumber));
            Assert.That(secondInvoice.Total, Is.EqualTo(firstInvoice.Total));
        });
    }

    [Test]
    public void InvalidCountsCurrenciesAndConfiguredLimitsFailBeforeEntropy()
    {
        const long seed = 11106;
        CommerceProvider inspected = Fake.Create(seed).Commerce();
        CommerceProvider untouched = Fake.Create(seed).Commerce();
        FakeOptions countOptions = FakeOptions.Create(seed);
        countOptions.MaximumCount = 4;
        CommerceProvider limited = Fake.Create(countOptions).Commerce();
        CommerceProvider limitedUntouched = Fake.Create(countOptions).Commerce();

        Assert.Multiple(() =>
        {
            Assert.That(() => inspected.LineItemFromQuantityAndCurrencyCode(0, "USD"),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.CartFromLineCount(0),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.OrderDetailsFromLineCountAndCurrencyCode(2, "ZZZ"),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => inspected.InvoiceDetailsFromLineCountAndCurrencyCode(0, "USD"),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(inspected.RandomValue.Long(), Is.EqualTo(untouched.RandomValue.Long()));
            Assert.That(() => limited.Cart(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(limited.RandomValue.Long(),
                Is.EqualTo(limitedUntouched.RandomValue.Long()));
        });
    }

    [Test]
    public void CommerceRecordGenerationDoesNotPerturbOtherProviderStreams()
    {
        Fake inspected = Fake.Create(11107);
        Fake untouched = Fake.Create(11107);

        int index = 0;
        while (index < 16)
        {
            inspected.Commerce().LineItem();
            inspected.Commerce().Cart();
            inspected.Commerce().OrderDetails();
            inspected.Commerce().InvoiceDetails();
            index++;
        }

        Assert.That(inspected.Person().FullName(), Is.EqualTo(untouched.Person().FullName()));
    }

    static void AssertLine(CommerceLineItem line, int decimalPlaces)
    {
        Assert.Multiple(() =>
        {
            Assert.That(line.Quantity, Is.GreaterThan(0));
            Assert.That(line.UnitPrice, Is.GreaterThan(0m));
            Assert.That(line.UnitPrice,
                Is.EqualTo(decimal.Round(line.UnitPrice, decimalPlaces)));
            Assert.That(line.Subtotal, Is.EqualTo(decimal.Round(
                line.UnitPrice * line.Quantity,
                decimalPlaces,
                MidpointRounding.ToEven)));
        });
    }

    static void AssertCalculation(
        CommerceLineItem[] items,
        int quantity,
        string currencyCode,
        decimal subtotal,
        decimal discountRate,
        decimal discountAmount,
        decimal taxableAmount,
        decimal taxRate,
        decimal taxAmount,
        decimal shippingAmount,
        decimal total,
        int decimalPlaces)
    {
        decimal expectedSubtotal = decimal.Zero;
        int expectedQuantity = 0;
        int index = 0;
        while (index < items.Length)
        {
            Assert.That(items[index].CurrencyCode, Is.EqualTo(currencyCode));
            AssertLine(items[index], decimalPlaces);
            expectedSubtotal += items[index].Subtotal;
            expectedQuantity += items[index].Quantity;
            index++;
        }
        decimal expectedDiscount = decimal.Round(
            expectedSubtotal * discountRate,
            decimalPlaces,
            MidpointRounding.ToEven);
        decimal expectedTaxable = expectedSubtotal - expectedDiscount;
        decimal expectedTax = decimal.Round(
            expectedTaxable * taxRate,
            decimalPlaces,
            MidpointRounding.ToEven);
        decimal expectedTotal = decimal.Round(
            expectedTaxable + expectedTax + shippingAmount,
            decimalPlaces,
            MidpointRounding.ToEven);
        Assert.Multiple(() =>
        {
            Assert.That(quantity, Is.EqualTo(expectedQuantity));
            Assert.That(subtotal, Is.EqualTo(expectedSubtotal));
            Assert.That(discountRate, Is.InRange(0m, 0.3m));
            Assert.That(discountAmount, Is.EqualTo(expectedDiscount));
            Assert.That(taxableAmount, Is.EqualTo(expectedTaxable));
            Assert.That(taxRate, Is.InRange(0m, 0.25m));
            Assert.That(taxAmount, Is.EqualTo(expectedTax));
            Assert.That(shippingAmount, Is.InRange(0m, 25m));
            Assert.That(shippingAmount,
                Is.EqualTo(decimal.Round(shippingAmount, decimalPlaces)));
            Assert.That(total, Is.EqualTo(expectedTotal));
        });
    }

    static void AssertLineEquivalent(
        CommerceLineItem expected,
        CommerceLineItem actual)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Sku, Is.EqualTo(expected.Sku));
            Assert.That(actual.ProductName, Is.EqualTo(expected.ProductName));
            Assert.That(actual.Quantity, Is.EqualTo(expected.Quantity));
            Assert.That(actual.UnitPrice, Is.EqualTo(expected.UnitPrice));
            Assert.That(actual.Subtotal, Is.EqualTo(expected.Subtotal));
            Assert.That(actual.CurrencyCode, Is.EqualTo(expected.CurrencyCode));
        });
    }
}
