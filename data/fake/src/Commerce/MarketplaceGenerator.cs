namespace fake;

internal static class MarketplaceGenerator
{
    const string Marker = "fake.synthetic-marketplace.v1";
    const string SellerMask = "SLR-########";
    const string PurchaseOrderMask = "PO-########";

    internal static string RetailDepartment(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateRetailDepartments(context);
        string result = MarketplaceData.RetailDepartments[
            random.IntFromMaximumExclusive(MarketplaceData.RetailDepartments.Length)];
        Guard.OutputSize(
            result.Length,
            context.CapturedOptions.MaximumOutputSize,
            "retailDepartment");
        return result;
    }

    internal static string PurchaseOrderNumber(
        GenerationContext context,
        FakeRandom random)
    {
        Guard.Count(8, context.CapturedOptions.MaximumCount, "purchaseOrderNumber");
        return MaskFormatter.Format(
            random,
            PurchaseOrderMask,
            context.CapturedOptions.MaximumOutputSize);
    }

    internal static MarketplaceSellerRecord Seller(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateRetailDepartments(context);
        Guard.Count(8, context.CapturedOptions.MaximumCount, "sellerIdentifier");
        CompanyProfile company = CompanyGenerator.Profile(context, random);
        MarketplaceSellerRecord result = new MarketplaceSellerRecord();
        result.Identifier = MaskFormatter.Format(
            random,
            SellerMask,
            FakeLimits.DefaultMaximumOutputSize);
        result.DisplayName = company.Name;
        result.RetailDepartment = RetailDepartment(context, random);
        result.StorefrontDomain = company.Domain;
        result.SupportEmail = company.Email;
        result.Company = company;
        result.IsSynthetic = true;
        result.TestMarker = Marker;
        result.RepresentsActiveSeller = false;
        result.RepresentsVerifiedSeller = false;
        Guard.OutputSize(
            SellerSize(result),
            context.CapturedOptions.MaximumOutputSize,
            "marketplaceSeller");
        return result;
    }

    internal static void AttachOrder(
        GenerationContext context,
        FakeRandom random,
        CommerceOrder order)
    {
        MarketplaceSellerRecord seller = Seller(context, random);
        string purchaseOrderNumber = PurchaseOrderNumber(context, random);
        order.Seller = seller;
        order.SellerIdentifier = seller.Identifier;
        order.PurchaseOrderNumber = purchaseOrderNumber;
        LogisticsGenerator.LinkShipment(
            order.Shipment,
            order.Number,
            purchaseOrderNumber,
            seller.Identifier,
            context.CapturedOptions.MaximumOutputSize);
    }

    internal static int OrderLinkageSize(CommerceOrder order)
    {
        return SellerSize(order.Seller) + order.SellerIdentifier.Length +
            order.PurchaseOrderNumber.Length;
    }

    internal static int SellerSize(MarketplaceSellerRecord value)
    {
        return value.Identifier.Length + value.DisplayName.Length +
            value.RetailDepartment.Length + value.StorefrontDomain.Length +
            value.SupportEmail.Length + CompanyGenerator.ProfileOutputSize(value.Company) +
            value.TestMarker.Length;
    }

    static void ValidateRetailDepartments(GenerationContext context)
    {
        Guard.Count(
            MarketplaceData.RetailDepartments.Length,
            context.CapturedOptions.MaximumCount,
            "retailDepartments");
        int index = 0;
        while (index < MarketplaceData.RetailDepartments.Length)
        {
            Guard.OutputSize(
                MarketplaceData.RetailDepartments[index].Length,
                context.CapturedOptions.MaximumOutputSize,
                "retailDepartment");
            index = index + 1;
        }
    }
}
