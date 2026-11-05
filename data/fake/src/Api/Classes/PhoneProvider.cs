using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>
/// Provides telephone generation operations.
/// </summary>
public class PhoneProvider : IPhoneProvider
{
    internal GenerationContext Context;
    internal FakeRandom RandomValue;
    internal PhoneProvider(GenerationContext context)
    {
        Context = context;
        RandomValue = context.ProviderRandom("phone");
    }

    /// <summary>
    /// Returns the locale-resolved phone-format metadata catalog.
    /// </summary>
    /// <remarks>The catalog describes fixture shapes and does not establish allocation, carrier, subscriber, location, or reachability.</remarks>
    /// <returns>Calling, trunk, area, extension, category, mask, and safe-range metadata.</returns>
    public PhoneLocaleMetadata Metadata()
    {
        PhoneLocaleMetadata result = PhoneLocaleDataParser.Resolve(Context);
        PhoneLocaleDataParser.ValidateOutput(result, Context.CapturedOptions.MaximumOutputSize);
        return result;
    }

    /// <summary>
    /// Returns the locale-resolved international calling code.
    /// </summary>
    /// <returns>A calling code including its leading plus sign.</returns>
    public string CallingCode()
    {
        return Metadata().CallingCode;
    }

    /// <summary>
    /// Returns the locale-resolved international calling code.
    /// </summary>
    /// <returns>A calling code including its leading plus sign.</returns>
    public string CountryCode()
    {
        return CallingCode();
    }

    /// <summary>
    /// Returns the prefix dialed from the locale before an international destination.
    /// </summary>
    /// <returns>The international dialing prefix.</returns>
    public string InternationalDialingPrefix()
    {
        return Metadata().InternationalDialingPrefix;
    }

    /// <summary>
    /// Returns the locale-resolved national dialing or trunk prefix.
    /// </summary>
    /// <returns>The national prefix, which may be empty for locales without one.</returns>
    public string NationalPrefix()
    {
        return Metadata().NationalPrefix;
    }

    /// <summary>
    /// Returns the locale's general area-code mask.
    /// </summary>
    /// <remarks>Within masks, <c>#</c> means a decimal digit and <c>N</c> means a digit from 2 through 9.</remarks>
    /// <returns>The general area-code mask.</returns>
    public string AreaCodeMask()
    {
        return Metadata().AreaCodeMask;
    }

    /// <summary>
    /// Returns the locale's general subscriber-number mask.
    /// </summary>
    /// <remarks>Within masks, <c>#</c> means a decimal digit and <c>N</c> means a digit from 2 through 9.</remarks>
    /// <returns>The general subscriber-number mask.</returns>
    public string SubscriberMask()
    {
        return Metadata().SubscriberMask;
    }

    /// <summary>
    /// Returns the locale's display prefix for phone extensions.
    /// </summary>
    /// <returns>The extension prefix, including authored spacing and punctuation.</returns>
    public string ExtensionPrefix()
    {
        return Metadata().ExtensionPrefix;
    }

    /// <summary>Reports whether the resolved locale supports alphabetic telephone-keypad formatting.</summary>
    /// <returns>True when phone-word formatting is available.</returns>
    public bool SupportsPhoneWords()
    {
        return Metadata().SupportsPhoneWords;
    }

    /// <summary>
    /// Returns every category defined by the locale phone catalog.
    /// </summary>
    /// <returns>A new array of category values in catalog order.</returns>
    public PhoneNumberCategory[] Categories()
    {
        PhoneFormatRule[] formats = Metadata().Formats;
        PhoneNumberCategory[] result = new PhoneNumberCategory[formats.Length];
        int index = 0;
        while (index < formats.Length)
        {
            result[index] = formats[index].Category;
            index = index + 1;
        }

        return result;
    }

    /// <summary>
    /// Returns the locale format rule for a requested phone category.
    /// </summary>
    /// <param name="category">The requested structural category.</param>
    /// <returns>The national, international, E.164-shaped, area-code, and extension rules.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the category is unavailable.</exception>
    public PhoneFormatRule Format(PhoneNumberCategory category)
    {
        PhoneFormatRule[] formats = Metadata().Formats;
        int index = 0;
        while (index < formats.Length)
        {
            if (formats[index].Category == category)
            {
                return formats[index];
            }

            index = index + 1;
        }

        throw new System.ArgumentOutOfRangeException("category", category, "category is not available in the resolved phone locale catalog.");
    }

    /// <summary>
    /// Returns the locale's authoritative safe-fiction phone ranges.
    /// </summary>
    /// <remarks>An empty array means the resolved locale has no admitted authoritative safe range.</remarks>
    /// <returns>A new array of safe-range metadata.</returns>
    public PhoneSafeRange[] SafeRanges()
    {
        return Metadata().SafeRanges;
    }

    /// <summary>
    /// Returns the safety policy applied to general phone-number generation.
    /// </summary>
    /// <returns>The requested mode, applied mode, safe range, and any explicit fallback.</returns>
    public PhoneSafetyMetadata SafetyMetadata()
    {
        return SafetyMetadataFromCategory(PhoneNumberCategory.General);
    }

    /// <summary>
    /// Returns the safety policy applied to a requested phone category.
    /// </summary>
    /// <param name="category">The requested structural category.</param>
    /// <returns>The requested mode, applied mode, safe range, and any explicit fallback.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the category is unavailable.</exception>
    public PhoneSafetyMetadata SafetyMetadataFromCategory(PhoneNumberCategory category)
    {
        PhoneSafetyMetadata result = PhoneSafetyResolver.Resolve(GenerationMetadata(), category, Context.SafetyModeValue);
        PhoneSafetyResolver.ValidateOutput(result, Context.CapturedOptions.MaximumOutputSize);
        return result;
    }

    /// <summary>Generates one explicit phone-number modeling edge case.</summary>
    /// <remarks>The result demonstrates structure only and makes no allocation, reachability, identity, or geography claim.</remarks>
    /// <param name="edgeCase">The requested variable-length, shared-code, leading-zero, extension, non-geographic, or ambiguity case.</param>
    /// <returns>A structured edge-case record.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the case or a configured limit is invalid.</exception>
    public PhoneEdgeCaseRecord EdgeCase(PhoneEdgeCase edgeCase)
    {
        return PhoneEdgeCaseGenerator.Generate(Context, RandomValue, edgeCase, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates every supported phone-number modeling edge case.</summary>
    /// <returns>New variable-length, shared-code, leading-zero, extension, non-geographic, and ambiguity records.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured collection or output limit is insufficient.</exception>
    public PhoneEdgeCaseRecord[] EdgeCases()
    {
        return PhoneEdgeCaseGenerator.GenerateAll(Context, RandomValue, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Generates a general national phone record with explicit parsing and safety metadata.
    /// </summary>
    /// <returns>A correlated phone record that makes no carrier or subscriber identity claim.</returns>
    public PhoneRecord Record()
    {
        return RecordFromCategoryAndFormat(PhoneNumberCategory.General, PhoneNumberFormat.National);
    }

    /// <summary>
    /// Generates a national phone record in a requested structural category.
    /// </summary>
    /// <param name="category">The requested structural category.</param>
    /// <returns>A correlated national phone record that preserves category metadata.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the category is unavailable.</exception>
    public PhoneRecord RecordFromCategory(PhoneNumberCategory category)
    {
        return RecordFromCategoryAndFormat(category, PhoneNumberFormat.National);
    }

    /// <summary>
    /// Generates a general phone record in a requested presentation.
    /// </summary>
    /// <param name="format">The requested national, international, or E.164 presentation.</param>
    /// <returns>A correlated general phone record that preserves presentation metadata.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the format is unsupported.</exception>
    public PhoneRecord RecordFromFormat(PhoneNumberFormat format)
    {
        return RecordFromCategoryAndFormat(PhoneNumberCategory.General, format);
    }

    /// <summary>
    /// Generates a phone record in a requested structural category and presentation.
    /// </summary>
    /// <param name="category">The requested structural category.</param>
    /// <param name="format">The requested national, international, or E.164 presentation.</param>
    /// <returns>A correlated phone record that preserves region, format, and category metadata.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the category or format is unsupported.</exception>
    public PhoneRecord RecordFromCategoryAndFormat(PhoneNumberCategory category, PhoneNumberFormat format)
    {
        return PhoneNumberGenerator.Record(Context, RandomValue, GenerationMetadata(), category, format, Context.SafetyModeValue, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates a general national record from an authoritative reserved-fiction range.</summary>
    /// <remarks>This operation explicitly applies reserved safety even when the provider was configured for another safety mode.</remarks>
    /// <returns>A correlated reserved-fiction phone record.</returns>
    /// <exception cref="FakeException">Thrown when the locale has no authoritative compatible range.</exception>
    public PhoneRecord ReservedFictionRecord()
    {
        return ReservedFictionRecordFromFormat(PhoneNumberFormat.National);
    }

    /// <summary>Generates a general record in a requested presentation from an authoritative reserved-fiction range.</summary>
    /// <remarks>This operation explicitly applies reserved safety even when the provider was configured for another safety mode.</remarks>
    /// <param name="format">The requested national, international, or E.164 presentation.</param>
    /// <returns>A correlated reserved-fiction phone record.</returns>
    /// <exception cref="FakeException">Thrown when the locale has no authoritative compatible range.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the format is unsupported.</exception>
    public PhoneRecord ReservedFictionRecordFromFormat(PhoneNumberFormat format)
    {
        return PhoneNumberGenerator.Record(
            Context,
            RandomValue,
            GenerationMetadata(),
            PhoneNumberCategory.General,
            format,
            SafetyMode.Reserved,
            Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Generates a general national phone record with an exact-length extension.
    /// </summary>
    /// <param name="extensionLength">The exact number of extension digits.</param>
    /// <returns>A correlated phone record including extension parsing metadata.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the extension length is outside locale or configured limits.</exception>
    public PhoneRecord RecordWithExtension(int extensionLength)
    {
        return RecordWithExtensionFromCategoryAndFormatAndExtensionLength(PhoneNumberCategory.General, PhoneNumberFormat.National, extensionLength);
    }

    /// <summary>
    /// Generates a category-specific national or international phone record with an extension.
    /// </summary>
    /// <param name="category">The requested structural category.</param>
    /// <param name="format">The national or international presentation.</param>
    /// <param name="extensionLength">The exact number of extension digits.</param>
    /// <returns>A correlated phone record including extension parsing metadata.</returns>
    /// <exception cref="System.ArgumentException">Thrown when E.164 output or a category without extensions is requested.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a category, format, or extension length is invalid.</exception>
    public PhoneRecord RecordWithExtensionFromCategoryAndFormatAndExtensionLength(PhoneNumberCategory category, PhoneNumberFormat format, int extensionLength)
    {
        return PhoneNumberGenerator.RecordWithExtension(Context, RandomValue, GenerationMetadata(), category, format, extensionLength, Context.SafetyModeValue, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Generates a phone fixture in the locale's general national format.
    /// </summary>
    /// <returns>A nationally formatted synthetic phone value.</returns>
    public string Number()
    {
        return National();
    }

    /// <summary>
    /// Generates a category-specific phone fixture in national format.
    /// </summary>
    /// <param name="category">The requested structural category.</param>
    /// <returns>A nationally formatted synthetic phone value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the category is unavailable.</exception>
    public string NumberFromCategory(PhoneNumberCategory category)
    {
        return NationalFromCategory(category);
    }

    /// <summary>
    /// Generates a general phone fixture in a requested presentation.
    /// </summary>
    /// <param name="format">The requested national, international, or E.164 presentation.</param>
    /// <returns>A formatted synthetic phone value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the format is unsupported.</exception>
    public string Formatted(PhoneNumberFormat format)
    {
        return FormattedFromCategoryAndFormat(PhoneNumberCategory.General, format);
    }

    /// <summary>
    /// Generates a category-specific phone fixture in a requested presentation.
    /// </summary>
    /// <param name="category">The requested structural category.</param>
    /// <param name="format">The requested national, international, or E.164 presentation.</param>
    /// <returns>A formatted synthetic phone value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the category or format is unsupported.</exception>
    public string FormattedFromCategoryAndFormat(PhoneNumberCategory category, PhoneNumberFormat format)
    {
        return PhoneNumberGenerator.Formatted(RandomValue, GenerationMetadata(), category, format, Context.SafetyModeValue, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates a general national phone fixture from an authoritative reserved-fiction range.</summary>
    /// <remarks>This operation explicitly applies reserved safety even when the provider was configured for another safety mode.</remarks>
    /// <returns>A nationally formatted reserved-fiction phone value.</returns>
    /// <exception cref="FakeException">Thrown when the locale has no authoritative compatible range.</exception>
    public string ReservedFiction()
    {
        return ReservedFictionFromFormat(PhoneNumberFormat.National);
    }

    /// <summary>Generates a general phone fixture in a requested presentation from an authoritative reserved-fiction range.</summary>
    /// <remarks>This operation explicitly applies reserved safety even when the provider was configured for another safety mode.</remarks>
    /// <param name="format">The requested national, international, or E.164 presentation.</param>
    /// <returns>A formatted reserved-fiction phone value.</returns>
    /// <exception cref="FakeException">Thrown when the locale has no authoritative compatible range.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the format is unsupported.</exception>
    public string ReservedFictionFromFormat(PhoneNumberFormat format)
    {
        return PhoneNumberGenerator.Formatted(
            RandomValue,
            GenerationMetadata(),
            PhoneNumberCategory.General,
            format,
            SafetyMode.Reserved,
            Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates a national reserved-fiction phone word using locale telephone-keypad rules.</summary>
    /// <returns>A phone word whose decoded numeric value remains in an authoritative safe-fiction range.</returns>
    /// <exception cref="FakeException">Thrown when the locale lacks phone-word support or an authoritative compatible range.</exception>
    public string PhoneWord()
    {
        return PhoneWordFromFormat(PhoneNumberFormat.National);
    }

    /// <summary>Generates a reserved-fiction phone word in a national or international presentation.</summary>
    /// <param name="format">The requested national or international presentation.</param>
    /// <returns>A phone word whose decoded numeric value remains in an authoritative safe-fiction range.</returns>
    /// <exception cref="FakeException">Thrown when the locale lacks phone-word support or an authoritative compatible range.</exception>
    /// <exception cref="System.ArgumentException">Thrown when E.164 output is requested.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the format is unsupported.</exception>
    public string PhoneWordFromFormat(PhoneNumberFormat format)
    {
        PhoneLocaleMetadata metadata = GenerationMetadata();
        PhoneWordGenerator.ValidateRequest(metadata, format);
        PhoneRecord record = PhoneNumberGenerator.Record(
            Context,
            RandomValue,
            metadata,
            PhoneNumberCategory.General,
            format,
            SafetyMode.Reserved,
            Context.CapturedOptions.MaximumOutputSize);
        return PhoneWordGenerator.Render(
            record,
            Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Generates a compact E.164-shaped general phone fixture.
    /// </summary>
    /// <returns>A plus sign followed by at most 15 decimal digits.</returns>
    public string E164()
    {
        return E164FromCategory(PhoneNumberCategory.General);
    }

    /// <summary>
    /// Generates a compact E.164-shaped category-specific phone fixture.
    /// </summary>
    /// <param name="category">The requested structural category.</param>
    /// <returns>A plus sign followed by at most 15 decimal digits.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the category is unavailable.</exception>
    public string E164FromCategory(PhoneNumberCategory category)
    {
        return FormattedFromCategoryAndFormat(category, PhoneNumberFormat.E164);
    }

    /// <summary>
    /// Generates a general phone fixture in national display format.
    /// </summary>
    /// <returns>A nationally formatted synthetic phone value.</returns>
    public string National()
    {
        return NationalFromCategory(PhoneNumberCategory.General);
    }

    /// <summary>
    /// Generates a category-specific phone fixture in national display format.
    /// </summary>
    /// <param name="category">The requested structural category.</param>
    /// <returns>A nationally formatted synthetic phone value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the category is unavailable.</exception>
    public string NationalFromCategory(PhoneNumberCategory category)
    {
        return FormattedFromCategoryAndFormat(category, PhoneNumberFormat.National);
    }

    /// <summary>
    /// Generates a general phone fixture in international display format.
    /// </summary>
    /// <returns>An internationally formatted synthetic phone value.</returns>
    public string International()
    {
        return InternationalFromCategory(PhoneNumberCategory.General);
    }

    /// <summary>
    /// Generates a category-specific phone fixture in international display format.
    /// </summary>
    /// <param name="category">The requested structural category.</param>
    /// <returns>An internationally formatted synthetic phone value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the category is unavailable.</exception>
    public string InternationalFromCategory(PhoneNumberCategory category)
    {
        return FormattedFromCategoryAndFormat(category, PhoneNumberFormat.International);
    }

    /// <summary>
    /// Generates a mobile-shaped fixture in national display format.
    /// </summary>
    /// <returns>A mobile-category phone fixture.</returns>
    public string Mobile()
    {
        return MobileFromFormat(PhoneNumberFormat.National);
    }

    /// <summary>
    /// Generates a mobile-shaped fixture in a requested presentation.
    /// </summary>
    /// <param name="format">The requested phone-number presentation.</param>
    /// <returns>A mobile-category phone fixture.</returns>
    public string MobileFromFormat(PhoneNumberFormat format)
    {
        return FormattedFromCategoryAndFormat(PhoneNumberCategory.Mobile, format);
    }

    /// <summary>
    /// Generates a landline-shaped fixture in national display format.
    /// </summary>
    /// <returns>A landline-category phone fixture.</returns>
    public string Landline()
    {
        return LandlineFromFormat(PhoneNumberFormat.National);
    }

    /// <summary>
    /// Generates a landline-shaped fixture in a requested presentation.
    /// </summary>
    /// <param name="format">The requested phone-number presentation.</param>
    /// <returns>A landline-category phone fixture.</returns>
    public string LandlineFromFormat(PhoneNumberFormat format)
    {
        return FormattedFromCategoryAndFormat(PhoneNumberCategory.Landline, format);
    }

    /// <summary>
    /// Generates a toll-free-shaped fixture in national display format.
    /// </summary>
    /// <returns>A toll-free-category phone fixture.</returns>
    public string TollFree()
    {
        return TollFreeFromFormat(PhoneNumberFormat.National);
    }

    /// <summary>
    /// Generates a toll-free-shaped fixture in a requested presentation.
    /// </summary>
    /// <param name="format">The requested phone-number presentation.</param>
    /// <returns>A toll-free-category phone fixture.</returns>
    public string TollFreeFromFormat(PhoneNumberFormat format)
    {
        return FormattedFromCategoryAndFormat(PhoneNumberCategory.TollFree, format);
    }

    /// <summary>
    /// Generates a premium-rate-shaped fixture in national display format.
    /// </summary>
    /// <returns>A premium-category phone fixture.</returns>
    public string Premium()
    {
        return PremiumFromFormat(PhoneNumberFormat.National);
    }

    /// <summary>
    /// Generates a premium-rate-shaped fixture in a requested presentation.
    /// </summary>
    /// <param name="format">The requested phone-number presentation.</param>
    /// <returns>A premium-category phone fixture.</returns>
    public string PremiumFromFormat(PhoneNumberFormat format)
    {
        return FormattedFromCategoryAndFormat(PhoneNumberCategory.Premium, format);
    }

    /// <summary>
    /// Generates an area or service code for the general phone category.
    /// </summary>
    /// <returns>A compact decimal area code.</returns>
    public string AreaCode()
    {
        return AreaCodeFromCategory(PhoneNumberCategory.General);
    }

    /// <summary>
    /// Generates an area or service code for a requested phone category.
    /// </summary>
    /// <param name="category">The requested structural category.</param>
    /// <returns>A compact decimal area or service code.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the category is unavailable.</exception>
    public string AreaCodeFromCategory(PhoneNumberCategory category)
    {
        return PhoneNumberGenerator.AreaCode(RandomValue, GenerationMetadata(), category, Context.SafetyModeValue, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Generates a decimal extension with a locale-supported random length.
    /// </summary>
    /// <returns>A synthetic extension without its display prefix.</returns>
    public string Extension()
    {
        PhoneLocaleMetadata metadata = GenerationMetadata();
        int length = RandomValue.IntFromMinimumAndMaximum(metadata.MinimumExtensionDigits, metadata.MaximumExtensionDigits + 1);
        return PhoneNumberGenerator.Extension(RandomValue, metadata, length, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Generates a decimal extension with an exact locale-supported length.
    /// </summary>
    /// <param name="length">The exact number of extension digits.</param>
    /// <returns>A synthetic extension without its display prefix.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the length is outside locale or configured limits.</exception>
    public string ExtensionFromLength(int length)
    {
        return PhoneNumberGenerator.Extension(RandomValue, GenerationMetadata(), length, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Generates a general national phone fixture with a locale-supported extension.
    /// </summary>
    /// <returns>A nationally formatted phone value followed by an extension.</returns>
    public string NumberWithExtension()
    {
        PhoneLocaleMetadata metadata = GenerationMetadata();
        return PhoneNumberGenerator.WithRandomExtension(RandomValue, metadata, PhoneNumberCategory.General, PhoneNumberFormat.National, Context.SafetyModeValue, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Generates a general national phone fixture with an exact-length extension.
    /// </summary>
    /// <param name="extensionLength">The exact number of extension digits.</param>
    /// <returns>A nationally formatted phone value followed by an extension.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the extension length is outside locale or configured limits.</exception>
    public string NumberWithExtensionFromExtensionLength(int extensionLength)
    {
        return FormattedWithExtension(PhoneNumberCategory.General, PhoneNumberFormat.National, extensionLength);
    }

    /// <summary>
    /// Generates a category-specific national or international phone fixture with an extension.
    /// </summary>
    /// <param name="category">The requested structural category.</param>
    /// <param name="format">The national or international presentation.</param>
    /// <param name="extensionLength">The exact number of extension digits.</param>
    /// <returns>A formatted phone value followed by the locale extension prefix and digits.</returns>
    /// <exception cref="System.ArgumentException">Thrown when E.164 output or a category without extensions is requested.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a category, format, or extension length is invalid.</exception>
    public string FormattedWithExtension(PhoneNumberCategory category, PhoneNumberFormat format, int extensionLength)
    {
        return PhoneNumberGenerator.WithExtension(RandomValue, GenerationMetadata(), category, format, extensionLength, Context.SafetyModeValue, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Returns the ITU-T E.118 revision used for ICCID structural rules.</summary>
    /// <returns>The revision identifier <c>09/2025</c>.</returns>
    public string ItuE118Version()
    {
        return SimIdentifierGenerator.E118Version;
    }

    /// <summary>Returns the ITU-T E.212 revision used for IMSI structural rules.</summary>
    /// <returns>The revision identifier <c>06/2024</c>.</returns>
    public string ItuE212Version()
    {
        return SimIdentifierGenerator.E212Version;
    }

    /// <summary>Generates a 19-digit synthetic ICCID shape with a Luhn check digit.</summary>
    /// <remarks>The value begins with telecommunications MII <c>89</c> but is not an assigned issuer or card identifier.</remarks>
    /// <returns>A decimal ICCID shape containing a valid trailing check digit.</returns>
    public string Iccid()
    {
        return SimIdentifierGenerator.GenerateIccid(RandomValue, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates a synthetic ICCID result with parsed components and independent validity metadata.</summary>
    /// <returns>A checksum-valid synthetic ICCID result with unknown allocation status.</returns>
    public IccidResult IccidResult()
    {
        return SimIdentifierGenerator.GenerateIccidResult(
            Context,
            RandomValue,
            Context.CapturedOptions.MaximumCount,
            Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Inspects an ICCID-shaped value without claiming issuer or card allocation.</summary>
    /// <param name="value">The value to inspect.</param>
    /// <returns>Parsed components when structurally valid and independent structure and checksum results.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit cannot accommodate the value.</exception>
    public IccidResult IccidResultFromValue(string value)
    {
        Guard.NotNull(value, "value");
        return SimIdentifierGenerator.InspectIccidResult(
            Context,
            value.AsSpan(),
            Context.CapturedOptions.MaximumCount,
            Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Appends an ICCID Luhn digit to a supplied telecommunications payload.</summary>
    /// <remarks>Syntax and checksum construction do not verify issuer assignment, card existence, ownership, or usability.</remarks>
    /// <param name="payload">Between 2 and 18 ASCII digits beginning with telecommunications MII <c>89</c>.</param>
    /// <returns>The supplied payload followed by its Luhn check digit.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>payload</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>payload</c> is malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit cannot accommodate the value.</exception>
    public string ComposeIccid(string payload)
    {
        Guard.NotNull(payload, "payload");
        return SimIdentifierGenerator.ComposeIccid(payload.AsSpan(), Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Reports whether a value has supported ICCID structure and a valid Luhn digit.</summary>
    /// <remarks>This is offline shape validation and does not query issuer or card allocation.</remarks>
    /// <param name="value">The ICCID-shaped value to validate.</param>
    /// <returns>True when the value has at most 19 ASCII digits, begins with <c>89</c>, and has a valid check digit.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    public bool ValidateIccid(string value)
    {
        Guard.NotNull(value, "value");
        return ValidateIccidFromValue(value.AsSpan());
    }

    /// <summary>Reports whether text has supported ICCID structure and a valid Luhn digit.</summary>
    /// <param name="value">The ICCID-shaped text to validate.</param>
    /// <returns>True when the text has at most 19 ASCII digits, begins with <c>89</c>, and has a valid check digit.</returns>
    public bool ValidateIccidFromValue(ReadOnlySpan<char> value)
    {
        return SimIdentifierGenerator.ValidateIccid(value, Context.CapturedOptions.MaximumCount);
    }

    /// <summary>Extracts the payload from a structurally valid ICCID shape.</summary>
    /// <param name="value">An ICCID shape with a valid trailing Luhn digit.</param>
    /// <returns>Every digit except the trailing check digit.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>value</c> is invalid.</exception>
    public string IccidPayload(string value)
    {
        Guard.NotNull(value, "value");
        return SimIdentifierGenerator.IccidPayload(value.AsSpan(), Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates a 15-digit private-network IMSI test shape using MCC <c>999</c> and MNC <c>99</c>.</summary>
    /// <remarks>The value is for fixture use only and must not be used for public-network routing, roaming, or subscriber identity.</remarks>
    /// <returns>A decimal private-network IMSI test shape.</returns>
    public string Imsi()
    {
        return ImsiFromMobileNetworkCodeLength(2);
    }

    /// <summary>Generates a synthetic private-network IMSI result using a two-digit test MNC.</summary>
    /// <returns>A parsed synthetic IMSI result with unknown allocation status.</returns>
    public ImsiResult ImsiResult()
    {
        return ImsiResultFromMobileNetworkCodeLength(2);
    }

    /// <summary>Generates a synthetic private-network IMSI result with a two- or three-digit test MNC.</summary>
    /// <param name="mobileNetworkCodeLength">The MNC length, either 2 or 3.</param>
    /// <returns>A parsed synthetic IMSI result with unknown allocation status.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the MNC length or a configured limit is invalid.</exception>
    public ImsiResult ImsiResultFromMobileNetworkCodeLength(int mobileNetworkCodeLength)
    {
        return SimIdentifierGenerator.GenerateImsiResult(
            Context,
            RandomValue,
            mobileNetworkCodeLength,
            Context.CapturedOptions.MaximumCount,
            Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Inspects an IMSI-shaped value using an explicit MNC field boundary.</summary>
    /// <param name="value">The value to inspect.</param>
    /// <param name="mobileNetworkCodeLength">The MNC length, either 2 or 3.</param>
    /// <returns>Parsed components when structurally valid and independent validity metadata.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the MNC length or a configured limit is invalid.</exception>
    public ImsiResult ImsiResultFromValueAndMobileNetworkCodeLength(
        string value,
        int mobileNetworkCodeLength)
    {
        Guard.NotNull(value, "value");
        return SimIdentifierGenerator.InspectImsiResult(
            Context,
            value.AsSpan(),
            mobileNetworkCodeLength,
            Context.CapturedOptions.MaximumCount,
            Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates a 15-digit private-network IMSI test shape with a two- or three-digit test MNC.</summary>
    /// <remarks>The value uses MCC <c>999</c> and MNC <c>99</c> or <c>999</c> as encouraged for tests by ITU-T E.212.</remarks>
    /// <param name="mobileNetworkCodeLength">The MNC length, either 2 or 3.</param>
    /// <returns>A decimal private-network IMSI test shape.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the MNC length or a configured limit is invalid.</exception>
    public string ImsiFromMobileNetworkCodeLength(int mobileNetworkCodeLength)
    {
        return SimIdentifierGenerator.GenerateImsi(RandomValue, mobileNetworkCodeLength, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates a 15-digit IMSI shape from caller-supplied MCC and MNC fields.</summary>
    /// <remarks>Supplying codes does not verify assignment or authorize network use.</remarks>
    /// <param name="mobileCountryCode">Exactly 3 ASCII decimal digits.</param>
    /// <param name="mobileNetworkCode">Exactly 2 or 3 ASCII decimal digits.</param>
    /// <returns>The supplied prefix followed by a random MSIN shape.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an input is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when an input is malformed.</exception>
    public string ImsiFromMobileCountryCodeAndMobileNetworkCode(string mobileCountryCode, string mobileNetworkCode)
    {
        Guard.NotNull(mobileCountryCode, "mobileCountryCode");
        Guard.NotNull(mobileNetworkCode, "mobileNetworkCode");
        return SimIdentifierGenerator.GenerateImsi(RandomValue, mobileCountryCode.AsSpan(), mobileNetworkCode.AsSpan(), Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Composes an IMSI shape from explicit MCC, MNC, and MSIN fields.</summary>
    /// <remarks>Composition validates structure only and does not verify allocation, uniqueness, ownership, or network usability.</remarks>
    /// <param name="mobileCountryCode">Exactly 3 ASCII decimal digits.</param>
    /// <param name="mobileNetworkCode">Exactly 2 or 3 ASCII decimal digits.</param>
    /// <param name="mobileSubscriptionIdentificationNumber">Between 1 and 10 ASCII decimal digits within the 15-digit total limit.</param>
    /// <returns>The concatenated IMSI shape without a checksum digit.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an input is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when an input is malformed.</exception>
    public string ComposeImsi(string mobileCountryCode, string mobileNetworkCode, string mobileSubscriptionIdentificationNumber)
    {
        Guard.NotNull(mobileCountryCode, "mobileCountryCode");
        Guard.NotNull(mobileNetworkCode, "mobileNetworkCode");
        Guard.NotNull(mobileSubscriptionIdentificationNumber, "mobileSubscriptionIdentificationNumber");
        return SimIdentifierGenerator.ComposeImsi(mobileCountryCode.AsSpan(), mobileNetworkCode.AsSpan(), mobileSubscriptionIdentificationNumber.AsSpan(), Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Reports whether a value is an ASCII decimal IMSI shape of 6 through 15 digits.</summary>
    /// <remarks>No allocation or field-boundary claim is made without an explicit MNC length.</remarks>
    /// <param name="value">The IMSI-shaped value to validate.</param>
    /// <returns>True when the value satisfies the supported syntax and total length.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    public bool ValidateImsi(string value)
    {
        Guard.NotNull(value, "value");
        return ValidateImsiFromValue(value.AsSpan());
    }

    /// <summary>Reports whether text is an ASCII decimal IMSI shape of 6 through 15 digits.</summary>
    /// <param name="value">The IMSI-shaped text to validate.</param>
    /// <returns>True when the text satisfies the supported syntax and total length.</returns>
    public bool ValidateImsiFromValue(ReadOnlySpan<char> value)
    {
        return SimIdentifierGenerator.ValidateImsi(value, Context.CapturedOptions.MaximumCount);
    }

    /// <summary>Reports whether an IMSI shape has valid field lengths for an explicit MNC length.</summary>
    /// <param name="value">The IMSI-shaped value to validate.</param>
    /// <param name="mobileNetworkCodeLength">The MNC length, either 2 or 3.</param>
    /// <returns>True when the total and component lengths are supported.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the MNC length is invalid.</exception>
    public bool ValidateImsiFromValueAndMobileNetworkCodeLength(string value, int mobileNetworkCodeLength)
    {
        Guard.NotNull(value, "value");
        return ValidateImsiFromValueAndMobileNetworkCodeLengthUsingReadOnlySpanOfCharAndInt32(value.AsSpan(), mobileNetworkCodeLength);
    }

    /// <summary>Reports whether IMSI-shaped text has valid field lengths for an explicit MNC length.</summary>
    /// <param name="value">The IMSI-shaped text to validate.</param>
    /// <param name="mobileNetworkCodeLength">The MNC length, either 2 or 3.</param>
    /// <returns>True when the total and component lengths are supported.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the MNC length is invalid.</exception>
    public bool ValidateImsiFromValueAndMobileNetworkCodeLengthUsingReadOnlySpanOfCharAndInt32(ReadOnlySpan<char> value, int mobileNetworkCodeLength)
    {
        return SimIdentifierGenerator.ValidateImsi(value, mobileNetworkCodeLength, Context.CapturedOptions.MaximumCount);
    }

    /// <summary>Extracts the three-digit MCC from a structurally valid IMSI shape.</summary>
    /// <param name="value">The IMSI-shaped value.</param>
    /// <param name="mobileNetworkCodeLength">The known MNC length, either 2 or 3.</param>
    /// <returns>The first three digits, retaining leading zeroes.</returns>
    public string ImsiMobileCountryCode(string value, int mobileNetworkCodeLength)
    {
        Guard.NotNull(value, "value");
        return SimIdentifierGenerator.ImsiMobileCountryCode(value.AsSpan(), mobileNetworkCodeLength, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Extracts a known two- or three-digit MNC from a structurally valid IMSI shape.</summary>
    /// <param name="value">The IMSI-shaped value.</param>
    /// <param name="mobileNetworkCodeLength">The known MNC length, either 2 or 3.</param>
    /// <returns>The MNC digits, retaining leading zeroes.</returns>
    public string ImsiMobileNetworkCode(string value, int mobileNetworkCodeLength)
    {
        Guard.NotNull(value, "value");
        return SimIdentifierGenerator.ImsiMobileNetworkCode(value.AsSpan(), mobileNetworkCodeLength, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Extracts the MSIN from a structurally valid IMSI shape using a known MNC length.</summary>
    /// <param name="value">The IMSI-shaped value.</param>
    /// <param name="mobileNetworkCodeLength">The known MNC length, either 2 or 3.</param>
    /// <returns>The remaining MSIN digits, retaining leading zeroes.</returns>
    public string ImsiMobileSubscriptionIdentificationNumber(string value, int mobileNetworkCodeLength)
    {
        Guard.NotNull(value, "value");
        return SimIdentifierGenerator.ImsiMobileSubscriptionIdentificationNumber(value.AsSpan(), mobileNetworkCodeLength, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    PhoneLocaleMetadata GenerationMetadata()
    {
        return PhoneLocaleDataParser.Resolve(Context);
    }
}
