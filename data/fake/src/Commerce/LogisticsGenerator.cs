using System;

namespace fake;

internal static class LogisticsGenerator
{
    const string Marker = "fake.synthetic-shipment.v1";
    const string BinMask = "?-##-###";
    const string PackageMask = "PKG-**********";

    internal static string Carrier(
        GenerationContext context,
        FakeRandom random)
    {
        return Value(context, random, LogisticsData.Carriers, "carrier");
    }

    internal static string ShippingMethod(
        GenerationContext context,
        FakeRandom random)
    {
        return Value(context, random, LogisticsData.Methods, "shippingMethod");
    }

    internal static ShippingService Service(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateServiceData(context);
        int methodIndex = random.IntFromMaximumExclusive(LogisticsData.Methods.Length);
        ShippingService result = new ShippingService();
        result.Carrier = Normalize(
            context,
            LogisticsData.Carriers[random.IntFromMaximumExclusive(LogisticsData.Carriers.Length)]);
        result.Method = Normalize(context, LogisticsData.Methods[methodIndex]);
        result.MinimumTransitDays = LogisticsData.MinimumTransitDays[methodIndex];
        result.MaximumTransitDays = LogisticsData.MaximumTransitDays[methodIndex];
        result.IsSynthetic = true;
        Guard.OutputSize(
            result.Carrier.Length + result.Method.Length,
            context.CapturedOptions.MaximumOutputSize,
            "shippingService");
        return result;
    }

    internal static string WarehouseBin(
        GenerationContext context,
        FakeRandom random)
    {
        Guard.Count(6, context.CapturedOptions.MaximumCount, "warehouseBin");
        return MaskFormatter.Format(
            random,
            BinMask,
            context.CapturedOptions.MaximumOutputSize).ToUpperInvariant();
    }

    internal static WarehouseRecord Warehouse(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(context, LogisticsData.Warehouses, "warehouse");
        Guard.Count(3, context.CapturedOptions.MaximumCount, "warehouseCode");
        Guard.Count(6, context.CapturedOptions.MaximumCount, "warehouseBin");
        AddressRecord address = AddressRecordGenerator.AddressRecord(context, random);
        string suffix = NumericStringGenerator.Digits(
            random,
            3,
            context.CapturedOptions.MaximumOutputSize);
        WarehouseRecord result = new WarehouseRecord();
        result.Code = "WH-" + address.CountryCode + "-" + suffix;
        result.Name = Normalize(
            context,
            ChooseValue(random, LogisticsData.Warehouses));
        result.Bin = WarehouseBin(context, random);
        result.Address = address;
        result.IsSynthetic = true;
        result.RepresentsRealFacility = false;
        Guard.OutputSize(
            result.Code.Length + result.Name.Length + result.Bin.Length,
            context.CapturedOptions.MaximumOutputSize,
            "warehouse");
        return result;
    }

    internal static PackageRecord Package(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(context, LogisticsData.PackageTypes, "package");
        Guard.Count(10, context.CapturedOptions.MaximumCount, "packageIdentifier");
        PackageRecord result = new PackageRecord();
        result.Identifier = MaskFormatter.Format(
            random,
            PackageMask,
            context.CapturedOptions.MaximumOutputSize);
        result.Type = Normalize(
            context,
            ChooseValue(random, LogisticsData.PackageTypes));
        result.Dimensions = ProductGenerator.Dimensions(context, random);
        result.Weight = ProductGenerator.Weight(random);
        result.IsSynthetic = true;
        Guard.OutputSize(
            result.Identifier.Length + result.Type.Length +
                result.Dimensions.Unit.Length,
            context.CapturedOptions.MaximumOutputSize,
            "package");
        return result;
    }

    internal static ShippingRoute Route(
        GenerationContext context,
        FakeRandom random)
    {
        WarehouseRecord warehouse = Warehouse(context, random);
        AddressRecord destination = AddressRecordGenerator.AddressRecord(
            context,
            random);
        return Route(context, warehouse, destination);
    }

    internal static DeliveryInterval DeliveryInterval(
        GenerationContext context,
        FakeRandom random)
    {
        RequireReferenceTime(context);
        ShippingService service = Service(context, random);
        return DeliveryInterval(context, service);
    }

    internal static ShipmentRecord Shipment(
        GenerationContext context,
        FakeRandom random)
    {
        RequireReferenceTime(context);
        ValidateServiceData(context);
        AddressRecord shipping = AddressRecordGenerator.AddressRecord(
            context,
            random);
        bool matching = random.Bool();
        AddressRecord billing = matching
            ? shipping
            : AddressRecordGenerator.AddressRecord(context, random);
        ShippingService service = Service(context, random);
        WarehouseRecord warehouse = Warehouse(context, random);
        PackageRecord package = Package(context, random);
        string tracking = GenericIdentifierGenerator.Generate(
            context,
            random,
            "commerce.tracking.masks",
            GenericIdentifierGenerator.TrackingMask);
        ContainerIdentifierRecord container = ContainerIdentifierGenerator.RecordFromSource(
            context,
            package.Identifier + tracking);
        ShipmentRecord result = new ShipmentRecord();
        result.ShippingAddress = shipping;
        result.BillingAddress = billing;
        result.BillingMatchesShipping = matching;
        result.Service = service;
        result.Warehouse = warehouse;
        result.Package = package;
        result.Container = container;
        result.ContainerIdentifier = container.Identifier;
        result.TrackingNumber = tracking;
        result.OrderNumber = string.Empty;
        result.PurchaseOrderNumber = string.Empty;
        result.SellerIdentifier = string.Empty;
        result.Route = Route(context, warehouse, shipping);
        result.EstimatedDelivery = DeliveryInterval(context, service);
        result.IsSynthetic = true;
        result.TestMarker = Marker;
        result.RepresentsCarrierAcceptance = false;
        result.RepresentsDeliverableShipment = false;
        Guard.OutputSize(
            ShipmentSize(result),
            context.CapturedOptions.MaximumOutputSize,
            "shipment");
        return result;
    }

    internal static int ShipmentSize(ShipmentRecord value)
    {
        return AddressSize(value.ShippingAddress) +
            (value.BillingMatchesShipping ? 0 : AddressSize(value.BillingAddress)) +
            value.Service.Carrier.Length + value.Service.Method.Length +
            value.Warehouse.Code.Length + value.Warehouse.Name.Length +
            value.Warehouse.Bin.Length + AddressSize(value.Warehouse.Address) +
            value.Package.Identifier.Length + value.Package.Type.Length +
            value.Package.Dimensions.Unit.Length + value.ContainerIdentifier.Length +
            value.Container.Identifier.Length + value.Container.OwnerCode.Length +
            value.Container.EquipmentCategory.Length +
            value.Container.SerialNumber.Length + value.Container.TestMarker.Length +
            value.TrackingNumber.Length +
            value.OrderNumber.Length + value.PurchaseOrderNumber.Length +
            value.SellerIdentifier.Length +
            value.Route.OriginWarehouseCode.Length +
            value.Route.OriginCountryCode.Length +
            value.Route.DestinationCountryCode.Length +
            value.Route.DestinationSubdivisionCode.Length +
            value.Route.DestinationPostalCode.Length + value.Route.Label.Length +
            value.EstimatedDelivery.Method.Length + value.TestMarker.Length;
    }

    internal static void LinkShipment(
        ShipmentRecord shipment,
        string orderNumber,
        string purchaseOrderNumber,
        string sellerIdentifier,
        int maximumOutputSize)
    {
        Guard.NotNull(shipment, "shipment");
        Guard.NotNullOrWhiteSpace(orderNumber, "orderNumber");
        Guard.NotNullOrWhiteSpace(purchaseOrderNumber, "purchaseOrderNumber");
        Guard.NotNullOrWhiteSpace(sellerIdentifier, "sellerIdentifier");
        Guard.OutputSize(
            orderNumber.Length + purchaseOrderNumber.Length +
                sellerIdentifier.Length,
            maximumOutputSize,
            "shipmentLinkage");
        shipment.OrderNumber = orderNumber;
        shipment.PurchaseOrderNumber = purchaseOrderNumber;
        shipment.SellerIdentifier = sellerIdentifier;
    }

    static ShippingRoute Route(
        GenerationContext context,
        WarehouseRecord warehouse,
        AddressRecord destination)
    {
        ShippingRoute result = new ShippingRoute();
        result.OriginWarehouseCode = warehouse.Code;
        result.OriginCountryCode = warehouse.Address.CountryCode;
        result.DestinationCountryCode = destination.CountryCode;
        result.DestinationSubdivisionCode = destination.SubdivisionCode;
        result.DestinationPostalCode = destination.PostalCode;
        result.Label = warehouse.Code + ">" + destination.SubdivisionCode;
        result.IsInternational = warehouse.Address.CountryCode !=
            destination.CountryCode;
        Guard.OutputSize(
            result.OriginWarehouseCode.Length + result.OriginCountryCode.Length +
                result.DestinationCountryCode.Length +
                result.DestinationSubdivisionCode.Length +
                result.DestinationPostalCode.Length + result.Label.Length,
            context.CapturedOptions.MaximumOutputSize,
            "shippingRoute");
        return result;
    }

    static DeliveryInterval DeliveryInterval(
        GenerationContext context,
        ShippingService service)
    {
        DateTimeOffset reference = context.ReferenceTime();
        DeliveryInterval result = new DeliveryInterval();
        result.Method = service.Method;
        result.ReferenceTime = reference;
        result.StartsAt = reference.AddDays(service.MinimumTransitDays);
        result.EndsAt = reference.AddDays(service.MaximumTransitDays);
        result.MinimumTransitDays = service.MinimumTransitDays;
        result.MaximumTransitDays = service.MaximumTransitDays;
        result.IsGuaranteed = false;
        return result;
    }

    static void RequireReferenceTime(GenerationContext context)
    {
        if (context.ReferenceTime() > DateTimeOffset.MaxValue.AddDays(-8))
        {
            throw FakeException.Create(
                "ReferenceTime is too close to DateTimeOffset.MaxValue for a delivery interval.");
        }
    }

    static void ValidateServiceData(GenerationContext context)
    {
        ValidateValues(context, LogisticsData.Carriers, "shippingService");
        ValidateValues(context, LogisticsData.Methods, "shippingService");
        Guard.Count(
            LogisticsData.MinimumTransitDays.Length,
            context.CapturedOptions.MaximumCount,
            "shippingServiceData");
        Guard.Count(
            LogisticsData.MaximumTransitDays.Length,
            context.CapturedOptions.MaximumCount,
            "shippingServiceData");
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
            "logisticsData");
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

    static int AddressSize(AddressRecord value)
    {
        return value.BuildingNumber.Length + value.StreetName.Length +
            value.StreetType.Length + value.Street.Length +
            value.StreetAddress.Length + value.Unit.Length +
            value.District.Length + value.City.Length + value.County.Length +
            value.Subdivision.Length + value.SubdivisionAbbreviation.Length +
            value.SubdivisionCode.Length + value.PostalCode.Length +
            value.Country.Length + value.CountryAbbreviation.Length +
            value.CountryCode.Length + value.TimeZoneId.Length;
    }
}
