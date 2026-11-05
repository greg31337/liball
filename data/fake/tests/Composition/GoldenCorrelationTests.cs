using System.Globalization;
using fake;

namespace fake.Tests;

[TestFixture]
public class GoldenCorrelationTests
{
    [Test]
    public void PersonProfileMatchesPinnedCorrelatedFixture()
    {
        PersonProfile person = Create(14500).Profile().Person();
        string personValue = String.Join("|", new string[]
        {
            person.FirstName,
            person.LastName,
            person.FullName,
            person.BirthDate.ToString("O", CultureInfo.InvariantCulture),
            person.Age.ToString(CultureInfo.InvariantCulture),
            person.Username,
            person.Email,
            person.Phone,
            person.Address,
            person.Employer,
            person.Locale,
            person.TimeZoneId
        });

        Assert.Multiple(() =>
        {
            Assert.That(personValue, Is.EqualTo(
                "Amara|Vale|Amara Vale|2014-09-12|30|" +
                "amara.vale.9239|amara.vale.9239@example.net|" +
                "+1-212-555-0119|8263 Placeholder Way, Sample City, " +
                "NY 00000|Example Research Group|en-US|America/New_York"));
            Assert.That(person.FullName,
                Is.EqualTo(person.FirstName + " " + person.LastName));
            Assert.That(person.Email,
                Does.StartWith(person.Username + "@example."));
            Assert.That(person.Age,
                Is.EqualTo(Create(14500).Date().Age(person.BirthDate)));
        });
    }

    [Test]
    public void ProductProfileMatchesPinnedCorrelatedFixture()
    {
        ProductProfile product = Create(14501).Profile().Product();
        string productValue = String.Join("|", new string[]
        {
            product.Name,
            product.Brand,
            product.Category,
            product.Material,
            product.Color,
            product.Sku,
            product.Gtin,
            product.Dimensions.Length.ToString(CultureInfo.InvariantCulture),
            product.Dimensions.Width.ToString(CultureInfo.InvariantCulture),
            product.Dimensions.Height.ToString(CultureInfo.InvariantCulture),
            product.Weight.ToString(CultureInfo.InvariantCulture),
            product.Price.ToString(CultureInfo.InvariantCulture),
            product.CurrencyCode,
            product.Inventory.ToString(CultureInfo.InvariantCulture)
        });

        Assert.Multiple(() =>
        {
            Assert.That(productValue, Is.EqualTo(
                "Practical Bamboo Notebook|Harbor Plain|" +
                "outdoor accessories|Bamboo|Navy|SKU-Q8PQ-9156|" +
                "37788950744901|23.4|84.3|186|44.02|833.91|USD|666"));
            Assert.That(Fake.Create(1).Commerce().ValidateGtin(product.Gtin),
                Is.True);
            Assert.That(product.Dimensions.Volume, Is.EqualTo(
                product.Dimensions.Length * product.Dimensions.Width *
                product.Dimensions.Height));
            Assert.That(product.IsSynthetic, Is.True);
            Assert.That(product.IdentifiersAreAllocated, Is.False);
        });
    }

    [Test]
    public void OrderProfileMatchesPinnedCorrelatedFixture()
    {
        OrderProfile order = Create(14502).Profile().Order();
        OrderLineRecord first = order.Items[0];
        string orderValue = String.Join("|", new string[]
        {
            order.Number,
            order.LineCount.ToString(CultureInfo.InvariantCulture),
            order.Quantity.ToString(CultureInfo.InvariantCulture),
            order.CurrencyCode,
            order.Subtotal.ToString(CultureInfo.InvariantCulture),
            order.DiscountAmount.ToString(CultureInfo.InvariantCulture),
            order.TaxAmount.ToString(CultureInfo.InvariantCulture),
            order.ShippingAmount.ToString(CultureInfo.InvariantCulture),
            order.Total.ToString(CultureInfo.InvariantCulture),
            order.Status.ToString(),
            order.CreatedAt.ToString("O", CultureInfo.InvariantCulture),
            first.Sku,
            first.ProductName,
            first.Quantity.ToString(CultureInfo.InvariantCulture),
            first.Total.ToString(CultureInfo.InvariantCulture),
            order.Shipment.TrackingNumber
        });

        Assert.Multiple(() =>
        {
            Assert.That(orderValue, Is.EqualTo(
                "ORD-7049335613|1|5|USD|2115.20|465.98|147.28|" +
                "4.51|1801.01|Confirmed|" +
                "2045-06-07T08:09:10.0000000+00:00|SKU-EcMr-0920|" +
                "Stackable Wood Lamp|5|1796.50|TRK-dbMXSd4GmVhtfVjW"));
            Assert.That(order.LineCount, Is.EqualTo(order.Items.Length));
            Assert.That(first.Total, Is.EqualTo(
                first.Subtotal - first.DiscountAmount + first.TaxAmount));
            Assert.That(order.Total, Is.EqualTo(
                first.Total + order.ShippingAmount));
            Assert.That(order.Shipment.ShippingAddress,
                Is.SameAs(order.ShippingAddress));
            Assert.That(order.IsSubmitted, Is.False);
        });
    }

    static Fake Create(long seed)
    {
        FakeOptions options = FakeOptions.Create("en-US", seed);
        options.ReferenceTime = new DateTimeOffset(
            2045,
            6,
            7,
            8,
            9,
            10,
            TimeSpan.Zero);
        return Fake.Create(options);
    }
}
