using System;
using System.Collections.Generic;

namespace fake;

internal class CommerceCalculation
{
    internal CommerceLineItem[] Items = new CommerceLineItem[0];
    internal int Quantity;
    internal string CurrencyCode = string.Empty;
    internal decimal Subtotal;
    internal decimal DiscountRate;
    internal decimal DiscountAmount;
    internal decimal TaxableAmount;
    internal decimal TaxRate;
    internal decimal TaxAmount;
    internal decimal ShippingAmount;
    internal decimal Total;

    internal CommerceCalculation()
    {
    }
}

internal static class CommerceRecordGenerator
{
    const string Marker = "fake.synthetic-commerce.v1";
    const string ReturnMarker = "fake.synthetic-commerce-return.v1";
    const string ReturnMask = "RET-########";
    const string DefaultCurrencyCode = "USD";
    const int DefaultLineOptions = 5;

    internal static CommerceLineItem LineItem(
        GenerationContext context,
        FakeRandom random)
    {
        Guard.Count(
            DefaultLineOptions,
            context.CapturedOptions.MaximumCount,
            "quantityOptions");
        return LineItem(context, random, random.IntFromMinimumAndMaximum(1, 6), DefaultCurrencyCode);
    }

    internal static CommerceLineItem LineItem(
        GenerationContext context,
        FakeRandom random,
        string currencyCode)
    {
        Guard.Count(
            DefaultLineOptions,
            context.CapturedOptions.MaximumCount,
            "quantityOptions");
        CurrencyRecord currency = Currency(currencyCode);
        return LineItemUnchecked(
            context,
            random,
            random.IntFromMinimumAndMaximum(1, 6),
            currency);
    }

    internal static CommerceLineItem LineItem(
        GenerationContext context,
        FakeRandom random,
        int quantity,
        string currencyCode)
    {
        RequireQuantity(context, quantity);
        CurrencyRecord currency = Currency(currencyCode);
        return LineItemUnchecked(context, random, quantity, currency);
    }

    internal static CommerceCart Cart(
        GenerationContext context,
        FakeRandom random)
    {
        Guard.Count(
            DefaultLineOptions,
            context.CapturedOptions.MaximumCount,
            "lineCountOptions");
        return Cart(
            context,
            random,
            random.IntFromMinimumAndMaximum(1, DefaultLineOptions + 1),
            DefaultCurrencyCode);
    }

    internal static CommerceCart Cart(
        GenerationContext context,
        FakeRandom random,
        int lineCount,
        string currencyCode)
    {
        CommerceCalculation calculation = Calculate(
            context,
            random,
            lineCount,
            currencyCode);
        CommerceCart result = new CommerceCart();
        Copy(result, calculation);
        Guard.OutputSize(
            CalculationSize(calculation) + Marker.Length,
            context.CapturedOptions.MaximumOutputSize,
            "commerceCart");
        return result;
    }

    internal static CommerceOrder Order(
        GenerationContext context,
        FakeRandom random)
    {
        RequireOrderReference(context);
        Guard.Count(
            DefaultLineOptions,
            context.CapturedOptions.MaximumCount,
            "lineCountOptions");
        return Order(
            context,
            random,
            random.IntFromMinimumAndMaximum(1, DefaultLineOptions + 1),
            DefaultCurrencyCode);
    }

    internal static CommerceOrder Order(
        GenerationContext context,
        FakeRandom random,
        int lineCount,
        string currencyCode)
    {
        RequireOrderReference(context);
        RequireLineCount(context, lineCount);
        Currency(currencyCode);
        string number = GenericIdentifierGenerator.Generate(
            context,
            random,
            "commerce.order.masks",
            GenericIdentifierGenerator.OrderMask);
        CommerceCalculation calculation = Calculate(
            context,
            random,
            lineCount,
            currencyCode);
        CommerceOrder result = new CommerceOrder();
        result.Number = number;
        Copy(result, calculation);
        ShipmentRecord shipment = LogisticsGenerator.Shipment(context, random);
        result.ShippingAddress = shipment.ShippingAddress;
        result.BillingAddress = shipment.BillingAddress;
        result.BillingMatchesShipping = shipment.BillingMatchesShipping;
        result.Shipment = shipment;
        ApplyOrderStatus(
            result,
            (CommerceOrderStatus)random.IntFromMaximumExclusive(5),
            context.ReferenceTime());
        MarketplaceGenerator.AttachOrder(context, random, result);
        Guard.OutputSize(
            number.Length + CalculationSize(calculation) + Marker.Length +
                LogisticsGenerator.ShipmentSize(shipment) +
                MarketplaceGenerator.OrderLinkageSize(result),
            context.CapturedOptions.MaximumOutputSize,
            "commerceOrder");
        return result;
    }

    internal static CommerceOrder Order(
        GenerationContext context,
        FakeRandom random,
        CommerceLineItem[] items,
        string currencyCode)
    {
        RequireOrderReference(context);
        CommerceLineItem[] supplied = ValidateAndCopy(
            context,
            items,
            currencyCode);
        string number = GenericIdentifierGenerator.Generate(
            context,
            random,
            "commerce.order.masks",
            GenericIdentifierGenerator.OrderMask);
        CommerceCalculation calculation = Calculate(
            context,
            random,
            supplied,
            currencyCode);
        CommerceOrder result = new CommerceOrder();
        result.Number = number;
        Copy(result, calculation);
        ShipmentRecord shipment = LogisticsGenerator.Shipment(context, random);
        result.ShippingAddress = shipment.ShippingAddress;
        result.BillingAddress = shipment.BillingAddress;
        result.BillingMatchesShipping = shipment.BillingMatchesShipping;
        result.Shipment = shipment;
        ApplyOrderStatus(
            result,
            (CommerceOrderStatus)random.IntFromMaximumExclusive(5),
            context.ReferenceTime());
        MarketplaceGenerator.AttachOrder(context, random, result);
        Guard.OutputSize(
            number.Length + CalculationSize(calculation) + Marker.Length +
                LogisticsGenerator.ShipmentSize(shipment) +
                MarketplaceGenerator.OrderLinkageSize(result),
            context.CapturedOptions.MaximumOutputSize,
            "commerceOrder");
        return result;
    }

    internal static CommerceOrderStatus OrderStatus(FakeRandom random)
    {
        return (CommerceOrderStatus)random.IntFromMaximumExclusive(5);
    }

    internal static CommerceReturn Return(
        GenerationContext context,
        FakeRandom random)
    {
        RequireReturnReference(context);
        Guard.Count(8, context.CapturedOptions.MaximumCount, "returnNumber");
        string number = MaskFormatter.Format(
            random,
            ReturnMask,
            context.CapturedOptions.MaximumOutputSize);
        CommerceOrder order = Order(context, random);
        ApplyOrderStatus(
            order,
            CommerceOrderStatus.Delivered,
            context.ReferenceTime());
        CommerceLineItem item = order.Items[random.IntFromMaximumExclusive(order.Items.Length)];
        int quantity = random.IntFromMinimumAndMaximum(1, item.Quantity + 1);
        CommerceReturnStatus status = (CommerceReturnStatus)random.IntFromMaximumExclusive(4);
        string reason = ProductExperienceGenerator.ReturnReason(context, random);
        string refundReason = status == CommerceReturnStatus.Refunded
            ? ProductExperienceGenerator.RefundReason(context, random)
            : string.Empty;
        CommerceReturn result = new CommerceReturn();
        result.Number = number;
        result.Order = order;
        result.OrderNumber = order.Number;
        result.Sku = item.Sku;
        result.Quantity = quantity;
        result.CurrencyCode = order.CurrencyCode;
        result.Reason = reason;
        result.RefundReason = refundReason;
        result.Status = status;
        result.RequestedAt = order.DeliveredAt.AddDays(1);
        result.HasResolvedAt = status != CommerceReturnStatus.Requested;
        result.ResolvedAt = result.HasResolvedAt
            ? result.RequestedAt.AddDays(1)
            : default(DateTimeOffset);
        result.RefundAmount = status == CommerceReturnStatus.Refunded
            ? Round(item.UnitPrice * quantity, Currency(order.CurrencyCode))
            : decimal.Zero;
        result.IsSynthetic = true;
        result.TestMarker = ReturnMarker;
        result.RepresentsReceivedGoods = false;
        result.RepresentsSettledRefund = false;
        Guard.OutputSize(
            number.Length + order.Number.Length + item.Sku.Length +
                order.CurrencyCode.Length + reason.Length + refundReason.Length +
                ReturnMarker.Length,
            context.CapturedOptions.MaximumOutputSize,
            "commerceReturn");
        return result;
    }

    internal static CommerceInvoice Invoice(
        GenerationContext context,
        FakeRandom random)
    {
        Guard.Count(
            DefaultLineOptions,
            context.CapturedOptions.MaximumCount,
            "lineCountOptions");
        return Invoice(
            context,
            random,
            random.IntFromMinimumAndMaximum(1, DefaultLineOptions + 1),
            DefaultCurrencyCode);
    }

    internal static CommerceInvoice Invoice(
        GenerationContext context,
        FakeRandom random,
        int lineCount,
        string currencyCode)
    {
        RequireLineCount(context, lineCount);
        Currency(currencyCode);
        string orderNumber = GenericIdentifierGenerator.Generate(
            context,
            random,
            "commerce.order.masks",
            GenericIdentifierGenerator.OrderMask);
        string invoiceNumber = GenericIdentifierGenerator.Generate(
            context,
            random,
            "commerce.invoice.masks",
            GenericIdentifierGenerator.InvoiceMask);
        CommerceCalculation calculation = Calculate(
            context,
            random,
            lineCount,
            currencyCode);
        CommerceInvoice result = new CommerceInvoice();
        result.Number = invoiceNumber;
        result.OrderNumber = orderNumber;
        Copy(result, calculation);
        ShipmentRecord shipment = LogisticsGenerator.Shipment(context, random);
        result.ShippingAddress = shipment.ShippingAddress;
        result.BillingAddress = shipment.BillingAddress;
        result.BillingMatchesShipping = shipment.BillingMatchesShipping;
        result.Shipment = shipment;
        Guard.OutputSize(
            invoiceNumber.Length + orderNumber.Length +
                CalculationSize(calculation) + Marker.Length +
                LogisticsGenerator.ShipmentSize(shipment),
            context.CapturedOptions.MaximumOutputSize,
            "commerceInvoice");
        return result;
    }

    static CommerceCalculation Calculate(
        GenerationContext context,
        FakeRandom random,
        int lineCount,
        string currencyCode)
    {
        RequireLineCount(context, lineCount);
        Guard.Count(
            DefaultLineOptions,
            context.CapturedOptions.MaximumCount,
            "quantityOptions");
        CurrencyRecord currency = Currency(currencyCode);
        CommerceLineItem[] items = new CommerceLineItem[lineCount];
        decimal subtotal = decimal.Zero;
        int quantity = 0;
        HashSet<string> skus = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        while (index < items.Length)
        {
            items[index] = LineItemUnchecked(
                context,
                random,
                random.IntFromMinimumAndMaximum(1, 6),
                currency);
            EnsureUniqueSku(context, random, items[index], skus);
            subtotal = subtotal + items[index].Subtotal;
            quantity = quantity + items[index].Quantity;
            index = index + 1;
        }
        return FinishCalculation(random, items, quantity, subtotal, currency);
    }

    static CommerceCalculation Calculate(
        GenerationContext context,
        FakeRandom random,
        CommerceLineItem[] items,
        string currencyCode)
    {
        CurrencyRecord currency = Currency(currencyCode);
        decimal subtotal = decimal.Zero;
        int quantity = 0;
        int index = 0;
        while (index < items.Length)
        {
            subtotal = subtotal + items[index].Subtotal;
            quantity = quantity + items[index].Quantity;
            index = index + 1;
        }
        return FinishCalculation(random, items, quantity, subtotal, currency);
    }

    static CommerceCalculation FinishCalculation(
        FakeRandom random,
        CommerceLineItem[] items,
        int quantity,
        decimal subtotal,
        CurrencyRecord currency)
    {
        decimal discountRate = FinancialGenerator.DiscountRate(random);
        decimal discountAmount = Round(subtotal * discountRate, currency);
        decimal taxableAmount = subtotal - discountAmount;
        decimal taxRate = FinancialGenerator.TaxRate(random);
        decimal taxAmount = Round(taxableAmount * taxRate, currency);
        decimal shippingAmount = MoneyGenerator.Amount(
            random,
            decimal.Zero,
            25m,
            MoneyGenerator.Places(currency),
            MoneyRoundingMode.ToEven);
        Allocate(
            items,
            discountAmount,
            taxAmount,
            subtotal,
            taxableAmount,
            currency);
        CommerceCalculation result = new CommerceCalculation();
        result.Items = items;
        result.Quantity = quantity;
        result.CurrencyCode = currency.Code;
        result.Subtotal = subtotal;
        result.DiscountRate = discountRate;
        result.DiscountAmount = discountAmount;
        result.TaxableAmount = taxableAmount;
        result.TaxRate = taxRate;
        result.TaxAmount = taxAmount;
        result.ShippingAmount = shippingAmount;
        result.Total = Round(
            taxableAmount + taxAmount + shippingAmount,
            currency);
        return result;
    }

    static CommerceLineItem LineItemUnchecked(
        GenerationContext context,
        FakeRandom random,
        int quantity,
        CurrencyRecord currency)
    {
        string name = ProductGenerator.Name(context, random);
        string sku = GenericIdentifierGenerator.Generate(
            context,
            random,
            "commerce.sku.masks",
            GenericIdentifierGenerator.SkuMask);
        decimal unitPrice = MoneyGenerator.Price(
            random,
            0.01m,
            1000m,
            MoneyGenerator.Places(currency),
            MoneyRoundingMode.ToEven);
        CommerceLineItem result = new CommerceLineItem();
        result.Sku = sku;
        result.ProductName = name;
        result.Quantity = quantity;
        result.UnitPrice = unitPrice;
        result.Subtotal = Round(unitPrice * quantity, currency);
        result.DiscountAmount = decimal.Zero;
        result.TaxAmount = decimal.Zero;
        result.Total = result.Subtotal;
        result.CurrencyCode = currency.Code;
        result.IsSynthetic = true;
        result.TestMarker = Marker;
        Guard.OutputSize(
            LineSize(result),
            context.CapturedOptions.MaximumOutputSize,
            "commerceLineItem");
        return result;
    }

    static void Allocate(
        CommerceLineItem[] items,
        decimal discountAmount,
        decimal taxAmount,
        decimal subtotal,
        decimal taxableAmount,
        CurrencyRecord currency)
    {
        decimal remainingDiscount = discountAmount;
        decimal remainingTax = taxAmount;
        int index = 0;
        while (index < items.Length)
        {
            decimal lineDiscount = index == items.Length - 1
                ? remainingDiscount
                : Round(
                    discountAmount * items[index].Subtotal /
                        subtotal,
                    currency);
            if (lineDiscount > remainingDiscount)
            {
                lineDiscount = remainingDiscount;
            }
            items[index].DiscountAmount = lineDiscount;
            remainingDiscount = remainingDiscount - lineDiscount;
            decimal lineTax = index == items.Length - 1
                ? remainingTax
                : Round(
                    taxAmount *
                        (items[index].Subtotal - lineDiscount) /
                        taxableAmount,
                    currency);
            if (lineTax > remainingTax)
            {
                lineTax = remainingTax;
            }
            items[index].TaxAmount = lineTax;
            remainingTax = remainingTax - lineTax;
            items[index].Total = items[index].Subtotal - lineDiscount + lineTax;
            index = index + 1;
        }
    }

    static void EnsureUniqueSku(
        GenerationContext context,
        FakeRandom random,
        CommerceLineItem item,
        HashSet<string> skus)
    {
        int attempts = 0;
        while (skus.Contains(item.Sku) &&
            attempts < context.CapturedOptions.UniquenessRetryLimit)
        {
            item.Sku = GenericIdentifierGenerator.Generate(
                context,
                random,
                "commerce.sku.masks",
                GenericIdentifierGenerator.SkuMask);
            attempts = attempts + 1;
        }
        if (skus.Contains(item.Sku))
        {
            Guard.Exhausted("commerce line SKU", attempts);
        }
        skus.Add(item.Sku);
    }

    static CommerceLineItem[] ValidateAndCopy(
        GenerationContext context,
        CommerceLineItem[] items,
        string currencyCode)
    {
        Guard.NotNull(items, "items");
        if (items.Length == 0)
        {
            throw new ArgumentException(
                "items must contain at least one line.",
                "items");
        }
        Guard.Count(
            items.Length,
            context.CapturedOptions.MaximumCount,
            "items");
        CurrencyRecord currency = Currency(currencyCode);
        CommerceLineItem[] result = new CommerceLineItem[items.Length];
        HashSet<string> skus = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        while (index < items.Length)
        {
            if (items[index] == null)
            {
                throw new ArgumentException(
                    "items cannot contain null lines.",
                    "items");
            }
            RequireQuantity(context, items[index].Quantity);
            Guard.NotNullOrWhiteSpace(items[index].Sku, "items");
            Guard.NotNullOrWhiteSpace(items[index].ProductName, "items");
            Guard.NotNullOrWhiteSpace(items[index].CurrencyCode, "items");
            if (items[index].CurrencyCode != currency.Code)
            {
                throw new ArgumentException(
                    "Every line currency must match currencyCode.",
                    "items");
            }
            if (items[index].UnitPrice <= decimal.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    "items",
                    items[index].UnitPrice,
                    "Every line unit price must be greater than zero.");
            }
            if (!skus.Add(items[index].Sku))
            {
                throw new ArgumentException(
                    "items cannot contain duplicate SKUs.",
                    "items");
            }
            result[index] = CopyLine(items[index], currency);
            index = index + 1;
        }
        return result;
    }

    static CommerceLineItem CopyLine(
        CommerceLineItem value,
        CurrencyRecord currency)
    {
        CommerceLineItem result = new CommerceLineItem();
        result.Sku = value.Sku;
        result.ProductName = value.ProductName;
        result.Quantity = value.Quantity;
        result.UnitPrice = Round(value.UnitPrice, currency);
        result.Subtotal = Round(result.UnitPrice * result.Quantity, currency);
        result.DiscountAmount = decimal.Zero;
        result.TaxAmount = decimal.Zero;
        result.Total = result.Subtotal;
        result.CurrencyCode = currency.Code;
        result.IsSynthetic = true;
        result.TestMarker = Marker;
        return result;
    }

    static void ApplyOrderStatus(
        CommerceOrder result,
        CommerceOrderStatus status,
        DateTimeOffset reference)
    {
        result.Status = status;
        result.CreatedAt = reference;
        result.HasConfirmedAt = false;
        result.ConfirmedAt = default(DateTimeOffset);
        result.HasShippedAt = false;
        result.ShippedAt = default(DateTimeOffset);
        result.HasDeliveredAt = false;
        result.DeliveredAt = default(DateTimeOffset);
        result.HasCancelledAt = false;
        result.CancelledAt = default(DateTimeOffset);
        switch (status)
        {
            case CommerceOrderStatus.Confirmed:
                result.HasConfirmedAt = true;
                result.ConfirmedAt = reference.AddHours(1);
                break;
            case CommerceOrderStatus.Shipped:
                result.HasConfirmedAt = true;
                result.ConfirmedAt = reference.AddHours(1);
                result.HasShippedAt = true;
                result.ShippedAt = reference.AddDays(1);
                break;
            case CommerceOrderStatus.Delivered:
                result.HasConfirmedAt = true;
                result.ConfirmedAt = reference.AddHours(1);
                result.HasShippedAt = true;
                result.ShippedAt = reference.AddDays(1);
                result.HasDeliveredAt = true;
                result.DeliveredAt = result.Shipment.EstimatedDelivery.EndsAt;
                break;
            case CommerceOrderStatus.Cancelled:
                result.HasCancelledAt = true;
                result.CancelledAt = reference.AddHours(1);
                break;
        }
    }

    static void RequireOrderReference(GenerationContext context)
    {
        if (context.ReferenceTime() > DateTimeOffset.MaxValue.AddDays(-8))
        {
            throw FakeException.Create(
                "ReferenceTime is too close to DateTimeOffset.MaxValue for an order timeline.");
        }
    }

    static void RequireReturnReference(GenerationContext context)
    {
        if (context.ReferenceTime() > DateTimeOffset.MaxValue.AddDays(-10))
        {
            throw FakeException.Create(
                "ReferenceTime is too close to DateTimeOffset.MaxValue for a return timeline.");
        }
    }

    static void Copy(CommerceCart result, CommerceCalculation value)
    {
        result.Items = value.Items;
        result.LineCount = value.Items.Length;
        result.Quantity = value.Quantity;
        result.CurrencyCode = value.CurrencyCode;
        result.Subtotal = value.Subtotal;
        result.DiscountRate = value.DiscountRate;
        result.DiscountAmount = value.DiscountAmount;
        result.TaxableAmount = value.TaxableAmount;
        result.TaxRate = value.TaxRate;
        result.TaxAmount = value.TaxAmount;
        result.ShippingAmount = value.ShippingAmount;
        result.Total = value.Total;
        result.IsSynthetic = true;
        result.TestMarker = Marker;
    }

    static void Copy(CommerceOrder result, CommerceCalculation value)
    {
        result.Items = value.Items;
        result.LineCount = value.Items.Length;
        result.Quantity = value.Quantity;
        result.CurrencyCode = value.CurrencyCode;
        result.Subtotal = value.Subtotal;
        result.DiscountRate = value.DiscountRate;
        result.DiscountAmount = value.DiscountAmount;
        result.TaxableAmount = value.TaxableAmount;
        result.TaxRate = value.TaxRate;
        result.TaxAmount = value.TaxAmount;
        result.ShippingAmount = value.ShippingAmount;
        result.Total = value.Total;
        result.IsSynthetic = true;
        result.TestMarker = Marker;
        result.IsSubmitted = false;
    }

    static void Copy(CommerceInvoice result, CommerceCalculation value)
    {
        result.Items = value.Items;
        result.LineCount = value.Items.Length;
        result.Quantity = value.Quantity;
        result.CurrencyCode = value.CurrencyCode;
        result.Subtotal = value.Subtotal;
        result.DiscountRate = value.DiscountRate;
        result.DiscountAmount = value.DiscountAmount;
        result.TaxableAmount = value.TaxableAmount;
        result.TaxRate = value.TaxRate;
        result.TaxAmount = value.TaxAmount;
        result.ShippingAmount = value.ShippingAmount;
        result.Total = value.Total;
        result.AmountDue = value.Total;
        result.IsPaid = false;
        result.IsSynthetic = true;
        result.TestMarker = Marker;
    }

    static void RequireLineCount(GenerationContext context, int lineCount)
    {
        if (lineCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                "lineCount",
                lineCount,
                "lineCount must be greater than zero.");
        }
        Guard.Count(
            lineCount,
            context.CapturedOptions.MaximumCount,
            "lineCount");
    }

    static void RequireQuantity(GenerationContext context, int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                "quantity",
                quantity,
                "quantity must be greater than zero.");
        }
        Guard.Count(
            quantity,
            context.CapturedOptions.MaximumCount,
            "quantity");
    }

    static CurrencyRecord Currency(string currencyCode)
    {
        return CurrencyRegistryStore.Current().Find(
            currencyCode,
            FakeLimits.DefaultMaximumOutputSize);
    }

    static decimal Round(decimal amount, CurrencyRecord currency)
    {
        return decimal.Round(
            amount,
            MoneyGenerator.Places(currency),
            MidpointRounding.ToEven);
    }

    static int LineSize(CommerceLineItem value)
    {
        return value.Sku.Length + value.ProductName.Length +
            value.CurrencyCode.Length + value.TestMarker.Length;
    }

    static int CalculationSize(CommerceCalculation value)
    {
        int result = value.CurrencyCode.Length;
        int index = 0;
        while (index < value.Items.Length)
        {
            result = result + LineSize(value.Items[index]);
            index = index + 1;
        }
        return result;
    }
}
