using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>Defines the public operations exposed by <see cref="CommerceProvider"/>.</summary>
public interface ICommerceProvider
{
    /// <summary>Generates a project-authored generic product component.</summary>
    /// <returns>A generic product component such as a basket or notebook.</returns>
    string Product();
    /// <summary>Generates a project-authored generic product component.</summary>
    /// <returns>A generic product component such as a basket or notebook.</returns>
    string ProductComponent();
    /// <summary>Generates a project-authored product adjective.</summary>
    /// <returns>A generic product adjective.</returns>
    string ProductAdjective();
    /// <summary>Generates a product name by composing authored adjective, material, and component vocabularies.</summary>
    /// <returns>A fictional generic product name.</returns>
    string ProductName();
    /// <summary>Generates a project-authored generic product category.</summary>
    /// <returns>A generic product category.</returns>
    string Category();
    /// <summary>Generates a project-authored generic product category.</summary>
    /// <returns>A generic product category.</returns>
    string ProductCategory();
    /// <summary>Generates an original fictional brand name.</summary>
    /// <remarks>The result does not establish trademark clearance or a real commercial entity.</remarks>
    /// <returns>A fictional brand name.</returns>
    string Brand();
    /// <summary>Generates a description from authored adjective, material, component, and category vocabularies.</summary>
    /// <remarks>The result is generic synthetic text and makes no safety or performance claim.</remarks>
    /// <returns>A fictional generic product description.</returns>
    string ProductDescription();
    /// <summary>Generates a project-authored generic product material.</summary>
    /// <returns>A generic material label.</returns>
    string ProductMaterial();
    /// <summary>Generates a project-authored generic product material.</summary>
    /// <returns>A generic material label.</returns>
    string Material();
    /// <summary>Generates a project-authored generic product color.</summary>
    /// <returns>A generic color label.</returns>
    string Color();
    /// <summary>Generates a project-authored generic product color.</summary>
    /// <returns>A generic color label.</returns>
    string ProductColor();
    /// <summary>Generates a checksum-valid synthetic GTIN-14.</summary>
    /// <remarks>The value is not allocated, licensed, or verified by GS1.</remarks>
    /// <returns>A fourteen-digit GTIN shape.</returns>
    string Gtin();
    /// <summary>Generates a checksum-valid synthetic GTIN with a supported length.</summary>
    /// <remarks>Supported lengths are 8, 12, 13, and 14. Valid structure does not establish GS1 allocation.</remarks>
    /// <param name="length">The complete identifier length.</param>
    /// <returns>A compact GTIN shape.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the length or a configured limit is invalid.</exception>
    string GtinFromLength(int length);
    /// <summary>Reports whether a value is a checksum-valid supported compact GTIN shape.</summary>
    /// <remarks>This is structural validation only and does not establish GS1 allocation.</remarks>
    /// <param name="value">The value to validate.</param>
    /// <returns>True for a checksum-valid GTIN-8, GTIN-12, GTIN-13, or GTIN-14 shape.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    bool ValidateGtin(string value);
    /// <summary>Generates one product dimension from 1.0 through 200.0 centimeters.</summary>
    /// <returns>A decimal dimension with one fractional digit of precision.</returns>
    decimal Dimension();
    /// <summary>Generates correlated rectangular product dimensions in centimeters.</summary>
    /// <returns>Length, width, height, volume, and unit values.</returns>
    ProductDimensions Dimensions();
    /// <summary>Generates a product weight from 0.05 through 50.00 kilograms.</summary>
    /// <returns>A decimal weight with two fractional digits of precision.</returns>
    decimal Weight();
    /// <summary>Generates a nonnegative product price from 0.01 through 1000 with two decimal places.</summary>
    /// <returns>A deterministic decimal price rounded to even.</returns>
    decimal Price();
    /// <summary>Generates an inventory quantity from zero through 1000 units.</summary>
    /// <returns>A nonnegative inventory quantity.</returns>
    int Inventory();
    /// <summary>Generates a correlated, explicitly synthetic product profile.</summary>
    /// <remarks>The profile does not identify a real product, allocated identifier, market price, or inventory holding.</remarks>
    /// <returns>A product fixture with correlated name, description, identifiers, measurements, price, and inventory.</returns>
    ProductProfile Profile();
    /// <summary>Generates an integer product rating from one through five.</summary>
    /// <returns>A synthetic star-rating value.</returns>
    int Rating();
    /// <summary>Generates a project-authored review title correlated with a generated rating.</summary>
    /// <returns>A generic synthetic review title.</returns>
    string ReviewTitle();
    /// <summary>Generates project-authored review text correlated with a generated rating.</summary>
    /// <remarks>The text does not describe a real purchase or product experience.</remarks>
    /// <returns>A generic synthetic review sentence.</returns>
    string ReviewText();
    /// <summary>Generates an explicitly synthetic product-review fixture.</summary>
    /// <remarks>The rating, title, and text agree in sentiment and do not represent a verified purchase.</remarks>
    /// <returns>A correlated rating and project-authored review.</returns>
    ProductReview Review();
    /// <summary>Generates a project-authored synthetic promotion name.</summary>
    /// <returns>A generic promotion label.</returns>
    string PromotionName();
    /// <summary>Generates a whole-number discount percentage from five through fifty.</summary>
    /// <returns>A discount percentage in increments of five.</returns>
    int DiscountPercent();
    /// <summary>Generates a visibly synthetic, non-redeemable coupon-shaped value.</summary>
    /// <returns>A value beginning with <c>TEST</c> and containing its discount percentage.</returns>
    string Coupon();
    /// <summary>Generates a visibly synthetic, non-redeemable coupon-shaped value.</summary>
    /// <returns>A value beginning with <c>TEST</c> and containing its discount percentage.</returns>
    string CouponCode();
    /// <summary>Generates an explicitly synthetic promotion with a correlated coupon and discount.</summary>
    /// <remarks>The fixture does not represent an active or redeemable commercial offer.</remarks>
    /// <returns>A promotion name, coupon code, discount, and safety metadata.</returns>
    PromotionRecord Promotion();
    /// <summary>Generates a project-authored synthetic subscription-plan name.</summary>
    /// <returns>A generic plan label.</returns>
    string SubscriptionPlanName();
    /// <summary>Generates a synthetic subscription status.</summary>
    /// <returns>A trial, active, paused, cancelled, or expired status.</returns>
    SubscriptionStatus SubscriptionStatus();
    /// <summary>Generates a synthetic subscription billing interval.</summary>
    /// <returns>A weekly, monthly, quarterly, or yearly interval.</returns>
    SubscriptionInterval SubscriptionInterval();
    /// <summary>Generates an explicitly synthetic subscription-plan fixture.</summary>
    /// <remarks>The fixture does not represent an account, entitlement, billing instruction, or purchasable offer.</remarks>
    /// <returns>A correlated plan name, status, interval, price, currency, and safety metadata.</returns>
    SubscriptionPlan SubscriptionPlan();
    /// <summary>Generates a project-authored return reason.</summary>
    /// <returns>A generic synthetic reason for returning a product.</returns>
    string ReturnReason();
    /// <summary>Generates a project-authored refund reason.</summary>
    /// <returns>A generic synthetic reason for requesting a refund.</returns>
    string RefundReason();
    /// <summary>Selects a project-authored retail department.</summary>
    /// <returns>A deterministic generic retail department label.</returns>
    string RetailDepartment();
    /// <summary>Generates an explicitly synthetic marketplace seller fixture.</summary>
    /// <remarks>The result does not represent an active, verified, registered, or reachable seller.</remarks>
    /// <returns>A seller identifier, retail department, and correlated fictional company profile.</returns>
    MarketplaceSellerRecord MarketplaceSeller();
    /// <summary>Generates a synthetic purchase-order-number shape for commerce linkage.</summary>
    /// <returns>A value beginning with <c>PO-</c> followed by eight digits.</returns>
    string PurchaseOrderNumber();
    /// <summary>Generates a checksum-valid synthetic ISO 6346-shaped container identifier.</summary>
    /// <remarks>The value is not claimed to be registered, assigned, or attached to a real container.</remarks>
    /// <returns>An eleven-character owner, category, serial, and check-digit shape.</returns>
    string ContainerIdentifier();
    /// <summary>Reports whether a container identifier has a valid ISO 6346 shape and check digit.</summary>
    /// <param name="value">The compact eleven-character identifier to inspect.</param>
    /// <returns><c>true</c> when the structure and check digit are valid; otherwise <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when value is null.</exception>
    bool IsContainerIdentifierValid(string value);
    /// <summary>Generates a parsed synthetic ISO 6346-shaped container identifier record.</summary>
    /// <returns>Owner, equipment category, serial, check digit, and synthetic safety fields.</returns>
    ContainerIdentifierRecord ContainerIdentifierDetails();
    /// <summary>Generates a project-fictional carrier name.</summary>
    /// <returns>An authored carrier label that does not identify a real carrier.</returns>
    string Carrier();
    /// <summary>Generates a generic shipping method.</summary>
    /// <returns>An economy, standard, two-day, or next-day method label.</returns>
    string ShippingMethod();
    /// <summary>Generates a fictional carrier service with method-correlated transit bounds.</summary>
    /// <returns>A carrier, method, and minimum and maximum transit days.</returns>
    ShippingService ShippingService();
    /// <summary>Generates a warehouse-bin-shaped value.</summary>
    /// <returns>An uppercase letter and five digits grouped with hyphens.</returns>
    string WarehouseBin();
    /// <summary>Generates an explicitly fictional warehouse and bin fixture.</summary>
    /// <remarks>The address does not claim that a logistics facility exists.</remarks>
    /// <returns>A warehouse code, name, bin, address, and safety metadata.</returns>
    WarehouseRecord Warehouse();
    /// <summary>Generates a synthetic package with dimensions and weight.</summary>
    /// <returns>A package identifier, type, dimensions, and weight.</returns>
    PackageRecord Package();
    /// <summary>Generates a route correlated with a fictional warehouse and synthetic destination.</summary>
    /// <remarks>The value does not represent carrier routing or geographic verification.</remarks>
    /// <returns>Correlated origin, destination, label, and international-route metadata.</returns>
    ShippingRoute Route();
    /// <summary>Generates an estimated-delivery interval derived from a shipping method.</summary>
    /// <remarks>The interval is not a carrier commitment or delivery guarantee.</remarks>
    /// <returns>A reference time, method, transit bounds, and ordered interval.</returns>
    DeliveryInterval DeliveryInterval();
    /// <summary>Generates a correlated, explicitly synthetic shipment fixture.</summary>
    /// <remarks>The fixture does not represent carrier acceptance, registered tracking, address deliverability, or a delivery commitment.</remarks>
    /// <returns>Correlated addresses, service, warehouse, package, tracking, route, and delivery interval data.</returns>
    ShipmentRecord Shipment();
    /// <summary>Generates one arithmetically consistent synthetic line item in USD.</summary>
    /// <returns>A product, quantity, unit price, and rounded subtotal.</returns>
    CommerceLineItem LineItem();
    /// <summary>Generates one synthetic line item in a requested current currency.</summary>
    /// <param name="currencyCode">The case-insensitive ISO 4217 currency code.</param>
    /// <returns>A product, quantity, unit price, and currency-minor-unit subtotal.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the code is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the code is malformed or absent from the current registry.</exception>
    CommerceLineItem LineItemFromCurrencyCode(string currencyCode);
    /// <summary>Generates one synthetic line item with an exact positive quantity.</summary>
    /// <param name="quantity">The positive unit quantity.</param>
    /// <param name="currencyCode">The case-insensitive ISO 4217 currency code.</param>
    /// <returns>A product, quantity, unit price, and currency-minor-unit subtotal.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when quantity exceeds a configured limit or is not positive.</exception>
    /// <exception cref="System.ArgumentNullException">Thrown when the code is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the code is malformed or absent from the current registry.</exception>
    CommerceLineItem LineItemFromQuantityAndCurrencyCode(int quantity, string currencyCode);
    /// <summary>Generates an arithmetically consistent synthetic USD shopping cart.</summary>
    /// <returns>A cart containing between one and five generated lines.</returns>
    CommerceCart Cart();
    /// <summary>Generates a synthetic USD shopping cart with an exact positive line count.</summary>
    /// <param name="lineCount">The positive number of independently generated lines.</param>
    /// <returns>An arithmetically consistent shopping cart.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when lineCount exceeds a configured limit or is not positive.</exception>
    CommerceCart CartFromLineCount(int lineCount);
    /// <summary>Generates a synthetic shopping cart with an exact line count and current currency.</summary>
    /// <param name="lineCount">The positive number of independently generated lines.</param>
    /// <param name="currencyCode">The case-insensitive ISO 4217 currency code.</param>
    /// <returns>An arithmetically consistent shopping cart.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when lineCount exceeds a configured limit or is not positive.</exception>
    /// <exception cref="System.ArgumentNullException">Thrown when the code is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the code is malformed or absent from the current registry.</exception>
    CommerceCart CartFromLineCountAndCurrencyCode(int lineCount, string currencyCode);
    /// <summary>Generates an arithmetically consistent synthetic USD order model.</summary>
    /// <remarks>The model is not submitted and does not represent a purchase or fulfillment request.</remarks>
    /// <returns>An order containing between one and five generated lines.</returns>
    CommerceOrder OrderDetails();
    /// <summary>Generates a synthetic USD order model with an exact positive line count.</summary>
    /// <param name="lineCount">The positive number of independently generated lines.</param>
    /// <returns>An arithmetically consistent order model.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when lineCount exceeds a configured limit or is not positive.</exception>
    CommerceOrder OrderDetailsFromLineCount(int lineCount);
    /// <summary>Generates a synthetic order model with an exact line count and current currency.</summary>
    /// <param name="lineCount">The positive number of independently generated lines.</param>
    /// <param name="currencyCode">The case-insensitive ISO 4217 currency code.</param>
    /// <returns>An arithmetically consistent order model.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when lineCount exceeds a configured limit or is not positive.</exception>
    /// <exception cref="System.ArgumentNullException">Thrown when the code is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the code is malformed or absent from the current registry.</exception>
    CommerceOrder OrderDetailsFromLineCountAndCurrencyCode(int lineCount, string currencyCode);
    /// <summary>Builds a synthetic USD order from supplied synthetic line items.</summary>
    /// <remarks>Lines are copied. Empty arrays, invalid quantities, mixed currencies, and duplicate SKUs are rejected before generation.</remarks>
    /// <param name="items">One or more positive-quantity synthetic line items with distinct SKUs.</param>
    /// <returns>An arithmetically consistent order with allocated line discounts and tax.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when items is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when items is empty or contains invalid, mixed-currency, or duplicate lines.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an item count, quantity, or price is invalid.</exception>
    CommerceOrder OrderDetailsFromItems(CommerceLineItem[] items);
    /// <summary>Builds a synthetic order from supplied synthetic line items in one current currency.</summary>
    /// <remarks>Lines are copied. Empty arrays, invalid quantities, mixed currencies, and duplicate SKUs are rejected before generation.</remarks>
    /// <param name="items">One or more positive-quantity synthetic line items with distinct SKUs.</param>
    /// <param name="currencyCode">The ISO 4217 code required on every supplied line.</param>
    /// <returns>An arithmetically consistent order with allocated line discounts and tax.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when items or currencyCode is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when inputs are empty, malformed, mixed-currency, or contain duplicate lines.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an item count, quantity, or price is invalid.</exception>
    CommerceOrder OrderDetailsFromItemsAndCurrencyCode(CommerceLineItem[] items, string currencyCode);
    /// <summary>Generates a correlated synthetic USD marketplace order.</summary>
    /// <returns>An order with linked seller, purchase-order, and shipment identifiers.</returns>
    CommerceOrder MarketplaceOrder();
    /// <summary>Generates a correlated synthetic marketplace order with explicit line count and currency.</summary>
    /// <param name="lineCount">The positive number of independently generated lines.</param>
    /// <param name="currencyCode">The case-insensitive ISO 4217 currency code.</param>
    /// <returns>An order with linked seller, purchase-order, and shipment identifiers.</returns>
    CommerceOrder MarketplaceOrderFromLineCountAndCurrencyCode(int lineCount, string currencyCode);
    /// <summary>Generates a synthetic order lifecycle status.</summary>
    /// <returns>A pending, confirmed, shipped, delivered, or cancelled status.</returns>
    CommerceOrderStatus OrderStatus();
    /// <summary>Generates a correlated synthetic merchandise-return fixture.</summary>
    /// <remarks>The nested order is delivered before the return request, and no status represents real goods or settled funds.</remarks>
    /// <returns>A delivered order, selected line, return reason, lifecycle dates, and optional refund calculation.</returns>
    CommerceReturn ReturnDetails();
    /// <summary>Generates an arithmetically consistent synthetic USD commerce invoice model.</summary>
    /// <remarks>The model is unpaid and is not a demand for payment or evidence of a sale.</remarks>
    /// <returns>An invoice containing between one and five generated lines.</returns>
    CommerceInvoice InvoiceDetails();
    /// <summary>Generates a synthetic USD commerce invoice model with an exact positive line count.</summary>
    /// <param name="lineCount">The positive number of independently generated lines.</param>
    /// <returns>An arithmetically consistent invoice model.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when lineCount exceeds a configured limit or is not positive.</exception>
    CommerceInvoice InvoiceDetailsFromLineCount(int lineCount);
    /// <summary>Generates a synthetic commerce invoice model with an exact line count and current currency.</summary>
    /// <param name="lineCount">The positive number of independently generated lines.</param>
    /// <param name="currencyCode">The case-insensitive ISO 4217 currency code.</param>
    /// <returns>An arithmetically consistent invoice model.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when lineCount exceeds a configured limit or is not positive.</exception>
    /// <exception cref="System.ArgumentNullException">Thrown when the code is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the code is malformed or absent from the current registry.</exception>
    CommerceInvoice InvoiceDetailsFromLineCountAndCurrencyCode(int lineCount, string currencyCode);
    /// <summary>
    /// Generates a commerce SKU from the resolved <c>commerce.sku.masks</c> locale field or the built-in fallback mask.
    /// </summary>
    /// <returns>A generated SKU-shaped value.</returns>
    string Sku();
    /// <summary>
    /// Generates a commerce SKU from an explicit format mask.
    /// </summary>
    /// <param name="mask">A mask using <c>#</c>, <c>?</c>, and <c>*</c> replacement tokens.</param>
    /// <returns>A generated SKU-shaped value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the mask is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the mask is malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the result exceeds a configured limit.</exception>
    string SkuFromMask(string mask);
    /// <summary>
    /// Generates a commerce order number from the resolved <c>commerce.order.masks</c> locale field or the built-in fallback mask.
    /// </summary>
    /// <returns>A generated order-number-shaped value.</returns>
    string Order();
    /// <summary>
    /// Generates a commerce order number from an explicit format mask.
    /// </summary>
    /// <param name="mask">A mask using <c>#</c>, <c>?</c>, and <c>*</c> replacement tokens.</param>
    /// <returns>A generated order-number-shaped value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the mask is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the mask is malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the result exceeds a configured limit.</exception>
    string OrderFromMask(string mask);
    /// <summary>
    /// Generates a commerce invoice number from the resolved <c>commerce.invoice.masks</c> locale field or the built-in fallback mask.
    /// </summary>
    /// <returns>A generated invoice-number-shaped value.</returns>
    string Invoice();
    /// <summary>
    /// Generates a commerce invoice number from an explicit format mask.
    /// </summary>
    /// <param name="mask">A mask using <c>#</c>, <c>?</c>, and <c>*</c> replacement tokens.</param>
    /// <returns>A generated invoice-number-shaped value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the mask is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the mask is malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the result exceeds a configured limit.</exception>
    string InvoiceFromMask(string mask);
    /// <summary>
    /// Generates a commerce tracking number from the resolved <c>commerce.tracking.masks</c> locale field or the built-in fallback mask.
    /// </summary>
    /// <remarks>The result is synthetic and is not claimed to belong to or be accepted by any carrier.</remarks>
    /// <returns>A generated tracking-number-shaped value.</returns>
    string Tracking();
    /// <summary>
    /// Generates a commerce tracking number from an explicit format mask.
    /// </summary>
    /// <remarks>The result is synthetic and is not claimed to belong to or be accepted by any carrier.</remarks>
    /// <param name="mask">A mask using <c>#</c>, <c>?</c>, and <c>*</c> replacement tokens.</param>
    /// <returns>A generated tracking-number-shaped value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the mask is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the mask is malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the result exceeds a configured limit.</exception>
    string TrackingFromMask(string mask);
}
