using System;

namespace fake;

/// <summary>
/// Contains the configuration captured by a <see cref="Fake"/> instance.
/// </summary>
/// <remarks>
/// Fields may be changed while preparing an instance. A <see cref="Fake"/>
/// validates and copies the supplied options, so later caller changes do not
/// alter that instance.
/// </remarks>
public class FakeOptions
{
    /// <summary>
    /// The requested BCP 47 locale tag.
    /// </summary>
    public string Locale = "en-US";

    /// <summary>
    /// The configured BCP 47 fallback locale tag.
    /// </summary>
    public string FallbackLocale = "en";

    /// <summary>
    /// The seed used by deterministic random streams.
    /// </summary>
    public long Seed;

    /// <summary>
    /// The deterministic reference time used by date-related providers.
    /// </summary>
    public DateTimeOffset ReferenceTime = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// The source of random values.
    /// </summary>
    public RandomMode RandomMode = RandomMode.Deterministic;

    /// <summary>
    /// The safety policy for contactable and allocated-looking data.
    /// </summary>
    public SafetyMode SafetyMode = SafetyMode.Reserved;

    /// <summary>
    /// The broad category of generated values.
    /// </summary>
    public GenerationMode GenerationMode = GenerationMode.Typical;

    /// <summary>
    /// The locale fallback policy.
    /// </summary>
    public StrictnessMode StrictnessMode = StrictnessMode.AllowFallback;

    /// <summary>
    /// The Unicode normalization applied to generated text.
    /// </summary>
    public NormalizationMode NormalizationMode = NormalizationMode.None;

    /// <summary>
    /// The maximum number of values accepted by bounded collection operations.
    /// </summary>
    public int MaximumCount = FakeLimits.DefaultMaximumCount;

    /// <summary>
    /// The maximum number of output units accepted by bounded output operations.
    /// </summary>
    public int MaximumOutputSize = FakeLimits.DefaultMaximumOutputSize;

    /// <summary>
    /// The maximum number of attempts made by bounded uniqueness operations.
    /// </summary>
    public int UniquenessRetryLimit = 100;

    /// <summary>
    /// The requested bundled-data version.
    /// </summary>
    public string DataVersion = "1";

    /// <summary>
    /// Creates options with deterministic, reserved-safe defaults and a seed.
    /// </summary>
    /// <param name="seed">The deterministic seed.</param>
    /// <returns>A new options instance.</returns>
    internal static FakeOptions Create(long seed)
    {
        FakeOptions options = new FakeOptions();
        options.Seed = seed;
        return options;
    }

    /// <summary>
    /// Creates options with deterministic, reserved-safe defaults and a locale.
    /// </summary>
    /// <param name="locale">The requested BCP 47 locale tag.</param>
    /// <returns>A new options instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>locale</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>locale</c> is empty or whitespace.</exception>
    internal static FakeOptions Create(string locale)
    {
        Guard.NotNullOrWhiteSpace(locale, "locale");
        FakeOptions options = new FakeOptions();
        options.Locale = locale;
        return options;
    }

    /// <summary>
    /// Creates options with deterministic, reserved-safe defaults, a locale, and a seed.
    /// </summary>
    /// <param name="locale">The requested BCP 47 locale tag.</param>
    /// <param name="seed">The deterministic seed.</param>
    /// <returns>A new options instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>locale</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>locale</c> is empty or whitespace.</exception>
    internal static FakeOptions Create(string locale, long seed)
    {
        Guard.NotNullOrWhiteSpace(locale, "locale");
        FakeOptions options = new FakeOptions();
        options.Locale = locale;
        options.Seed = seed;
        return options;
    }

    /// <summary>
    /// Creates options with explicit locale and fallback tags and a seed.
    /// </summary>
    /// <param name="locale">The requested BCP 47 locale tag.</param>
    /// <param name="fallbackLocale">The configured BCP 47 fallback locale tag.</param>
    /// <param name="seed">The deterministic seed.</param>
    /// <returns>A new options instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a locale argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when a locale argument is empty or whitespace.</exception>
    internal static FakeOptions Create(string locale, string fallbackLocale, long seed)
    {
        Guard.NotNullOrWhiteSpace(locale, "locale");
        Guard.NotNullOrWhiteSpace(fallbackLocale, "fallbackLocale");
        FakeOptions options = new FakeOptions();
        options.Locale = locale;
        options.FallbackLocale = fallbackLocale;
        options.Seed = seed;
        return options;
    }

    /// <summary>
    /// Creates a field-for-field copy of existing options.
    /// </summary>
    /// <param name="options">The options to copy.</param>
    /// <returns>A new options instance containing the same values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>options</c> is null.</exception>
    internal static FakeOptions Create(FakeOptions options)
    {
        Guard.NotNull(options, "options");
        return options.Copy();
    }

    /// <summary>
    /// Creates an independent field-for-field copy of these options.
    /// </summary>
    /// <returns>A new options instance containing the same values.</returns>
    internal FakeOptions Copy()
    {
        FakeOptions copy = new FakeOptions();
        copy.CopyFields(this);
        return copy;
    }

    /// <summary>
    /// Validates all option fields.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when a required string is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when a required string is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a limit or enum value is invalid.</exception>
    internal void Validate()
    {
        Guard.NotNullOrWhiteSpace(Locale, "Locale");
        Guard.NotNullOrWhiteSpace(FallbackLocale, "FallbackLocale");
        Guard.NotNullOrWhiteSpace(DataVersion, "DataVersion");
        LanguageTagParser.Parse(Locale, "Locale");
        LanguageTagParser.Parse(FallbackLocale, "FallbackLocale");
        Guard.Positive(MaximumCount, "MaximumCount");
        Guard.Positive(MaximumOutputSize, "MaximumOutputSize");
        Guard.Positive(UniquenessRetryLimit, "UniquenessRetryLimit");
        ValidateRandomMode(RandomMode);
        ValidateSafetyMode(SafetyMode);
        ValidateGenerationMode(GenerationMode);
        ValidateStrictnessMode(StrictnessMode);
        ValidateNormalizationMode(NormalizationMode);
    }

    internal static FakeOptions Capture(FakeOptions options)
    {
        Guard.NotNull(options, "options");
        FakeOptions captured = options.Copy();
        captured.Validate();
        captured.Locale = LanguageTagParser.Parse(captured.Locale, "Locale").Normalized;
        captured.FallbackLocale = LanguageTagParser.Parse(captured.FallbackLocale, "FallbackLocale").Normalized;
        return captured;
    }

    internal void CopyFields(FakeOptions options)
    {
        Locale = options.Locale;
        FallbackLocale = options.FallbackLocale;
        Seed = options.Seed;
        ReferenceTime = options.ReferenceTime;
        RandomMode = options.RandomMode;
        SafetyMode = options.SafetyMode;
        GenerationMode = options.GenerationMode;
        StrictnessMode = options.StrictnessMode;
        NormalizationMode = options.NormalizationMode;
        MaximumCount = options.MaximumCount;
        MaximumOutputSize = options.MaximumOutputSize;
        UniquenessRetryLimit = options.UniquenessRetryLimit;
        DataVersion = options.DataVersion;
    }

    internal static void ValidateRandomMode(RandomMode value)
    {
        switch (value)
        {
            case RandomMode.Deterministic:
            case RandomMode.Cryptographic:
                return;
            default:
                throw new ArgumentOutOfRangeException("RandomMode", value, "RandomMode is not supported.");
        }
    }

    internal static void ValidateSafetyMode(SafetyMode value)
    {
        switch (value)
        {
            case SafetyMode.Reserved:
            case SafetyMode.SyntheticPlausible:
            case SafetyMode.UnrestrictedShape:
                return;
            default:
                throw new ArgumentOutOfRangeException("SafetyMode", value, "SafetyMode is not supported.");
        }
    }

    internal static void ValidateGenerationMode(GenerationMode value)
    {
        switch (value)
        {
            case GenerationMode.Typical:
            case GenerationMode.Boundary:
            case GenerationMode.ValidUnusual:
            case GenerationMode.Invalid:
            case GenerationMode.Mixed:
            case GenerationMode.Adversarial:
                return;
            default:
                throw new ArgumentOutOfRangeException("GenerationMode", value, "GenerationMode is not supported.");
        }
    }

    internal static void ValidateStrictnessMode(StrictnessMode value)
    {
        switch (value)
        {
            case StrictnessMode.AllowFallback:
            case StrictnessMode.Strict:
                return;
            default:
                throw new ArgumentOutOfRangeException("StrictnessMode", value, "StrictnessMode is not supported.");
        }
    }

    internal static void ValidateNormalizationMode(NormalizationMode value)
    {
        switch (value)
        {
            case NormalizationMode.None:
            case NormalizationMode.Nfc:
            case NormalizationMode.Nfd:
            case NormalizationMode.Nfkc:
            case NormalizationMode.Nfkd:
                return;
            default:
                throw new ArgumentOutOfRangeException("NormalizationMode", value, "NormalizationMode is not supported.");
        }
    }
}
