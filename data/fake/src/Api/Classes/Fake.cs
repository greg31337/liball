namespace fake;
/// <summary>
/// Provides the root entry point and cached providers for fake-data generation.
/// </summary>
public class Fake : IFake
{
    internal FakeOptions CapturedOptions;
    internal GenerationContext Context;
    FakeProviders Providers;
    /// <summary>
    /// Creates an instance from a validated snapshot of the supplied options.
    /// </summary>
    /// <param name="options">The options to validate and capture.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>options</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when a required option string is empty or whitespace.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an option limit or enum value is invalid.</exception>
    public Fake(FakeOptions options)
    {
        CapturedOptions = FakeOptions.Capture(options);
        Context = new GenerationContext(CapturedOptions);
        Providers = new FakeProviders(Context, this);
    }

    /// <summary>
    /// Creates an instance with deterministic, reserved-safe defaults.
    /// </summary>
    /// <returns>A new fake-data generator.</returns>
    internal static Fake Create()
    {
        return new Fake(new FakeOptions());
    }

    /// <summary>
    /// Creates an instance with deterministic, reserved-safe defaults and an integer seed.
    /// </summary>
    /// <param name="seed">The deterministic seed.</param>
    /// <returns>A new fake-data generator.</returns>
    internal static Fake Create(int seed)
    {
        return Create((long)seed);
    }

    /// <summary>
    /// Creates an instance with deterministic, reserved-safe defaults and a long seed.
    /// </summary>
    /// <param name="seed">The deterministic seed.</param>
    /// <returns>A new fake-data generator.</returns>
    internal static Fake Create(long seed)
    {
        return Create(FakeOptions.Create(seed));
    }

    /// <summary>
    /// Creates an instance with deterministic, reserved-safe defaults and a locale.
    /// </summary>
    /// <param name="locale">The requested BCP 47 locale tag.</param>
    /// <returns>A new fake-data generator.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>locale</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>locale</c> is empty or whitespace.</exception>
    internal static Fake Create(string locale)
    {
        return Create(FakeOptions.Create(locale));
    }

    /// <summary>
    /// Creates an instance with deterministic, reserved-safe defaults, a locale, and a seed.
    /// </summary>
    /// <param name="locale">The requested BCP 47 locale tag.</param>
    /// <param name="seed">The deterministic seed.</param>
    /// <returns>A new fake-data generator.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>locale</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>locale</c> is empty or whitespace.</exception>
    internal static Fake Create(string locale, long seed)
    {
        return Create(FakeOptions.Create(locale, seed));
    }

    /// <summary>
    /// Creates an instance from a validated snapshot of the supplied options.
    /// </summary>
    /// <param name="options">The options to validate and capture.</param>
    /// <returns>A new fake-data generator.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>options</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when a required option string is empty or whitespace.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an option limit or enum value is invalid.</exception>
    internal static Fake Create(FakeOptions options)
    {
        return new Fake(options);
    }

    /// <summary>
    /// Returns the cached low-level random provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    public FakeRandom Random()
    {
        return Providers.RandomValue;
    }

    /// <summary>
    /// Returns the cached primitive provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    public PrimitiveProvider Primitive()
    {
        return Providers.PrimitiveValue;
    }

    /// <summary>
    /// Returns the cached text provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    public TextProvider Text()
    {
        return Providers.TextValue;
    }

    /// <summary>
    /// Returns the cached template provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    public TemplateProvider Template()
    {
        return Providers.TemplateValue;
    }

    /// <summary>
    /// Returns the cached checksum provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    public ChecksumProvider Checksum()
    {
        return Providers.ChecksumValue;
    }

    /// <summary>
    /// Returns the cached identifier provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    public IdentifierProvider Identifier()
    {
        return Providers.IdentifierValue;
    }

    /// <summary>
    /// Returns the cached date and time provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    public DateProvider Date()
    {
        return Providers.DateValue;
    }

    /// <summary>
    /// Returns the cached locale provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    public LocaleProvider Locale()
    {
        return Providers.LocaleValue;
    }

    /// <summary>
    /// Returns the cached person provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    public PersonProvider Person()
    {
        return Providers.PersonValue;
    }

    /// <summary>
    /// Returns the cached address provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    public AddressProvider Address()
    {
        return Providers.AddressValue;
    }

    /// <summary>
    /// Returns the cached internet provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    public InternetProvider Internet()
    {
        return Providers.InternetValue;
    }

    /// <summary>
    /// Returns the cached phone provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    public PhoneProvider Phone()
    {
        return Providers.PhoneValue;
    }

    /// <summary>
    /// Returns the cached device provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    public DeviceProvider Device()
    {
        return Providers.DeviceValue;
    }

    /// <summary>
    /// Returns the cached finance provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    public FinanceProvider Finance()
    {
        return Providers.FinanceValue;
    }

    /// <summary>
    /// Returns the cached company provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    public CompanyProvider Company()
    {
        return Providers.CompanyValue;
    }

    /// <summary>
    /// Returns the cached commerce provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    public CommerceProvider Commerce()
    {
        return Providers.CommerceValue;
    }

    /// <summary>
    /// Returns the cached system provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    public SystemProvider System()
    {
        return Providers.SystemValue;
    }

    /// <summary>
    /// Returns the cached database provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    public DatabaseProvider Database()
    {
        return Providers.DatabaseValue;
    }

    /// <summary>
    /// Returns the cached source-control provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    public GitProvider Git()
    {
        return Providers.GitValue;
    }

    /// <summary>
    /// Returns the cached color provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    public ColorProvider Color()
    {
        return Providers.ColorValue;
    }

    /// <summary>
    /// Returns the cached vehicle provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    public VehicleProvider Vehicle()
    {
        return Providers.VehicleValue;
    }

    /// <summary>
    /// Returns the cached airline provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    public AirlineProvider Airline()
    {
        return Providers.AirlineValue;
    }

    /// <summary>
    /// Returns the cached book provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    public BookProvider Book()
    {
        return Providers.BookValue;
    }

    /// <summary>
    /// Returns the cached synthetic-media provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    public MediaProvider Media()
    {
        return Providers.MediaValue;
    }

    /// <summary>
    /// Returns the cached food provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    public FoodProvider Food()
    {
        return Providers.FoodValue;
    }

    /// <summary>
    /// Returns the cached animal provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    public AnimalProvider Animal()
    {
        return Providers.AnimalValue;
    }

    /// <summary>
    /// Returns the cached science provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    public ScienceProvider Science()
    {
        return Providers.ScienceValue;
    }

    /// <summary>
    /// Returns the cached health provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    public HealthProvider Health()
    {
        return Providers.HealthValue;
    }

    /// <summary>
    /// Returns the cached education provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    public EducationProvider Education()
    {
        return Providers.EducationValue;
    }

    /// <summary>
    /// Returns the cached culture provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    public CultureProvider Culture()
    {
        return Providers.CultureValue;
    }

    /// <summary>
    /// Returns the cached correlated-profile provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    public ProfileProvider Profile()
    {
        return Providers.ProfileValue;
    }

    /// <summary>
    /// Returns the cached schema generator.
    /// </summary>
    /// <returns>The generator for this instance.</returns>
    public SchemaGenerator Schema()
    {
        return Providers.SchemaValue;
    }

    /// <summary>
    /// Returns the cached edge-case provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    public EdgeProvider Edge()
    {
        return Providers.EdgeValue;
    }

    /// <summary>
    /// Returns the cached export provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    public ExportProvider Export()
    {
        return Providers.ExportValue;
    }

    /// <summary>
    /// Restarts the deterministic root random stream from its current seed.
    /// Cryptographic mode continues from the platform random source.
    /// </summary>
    public void Reset()
    {
        Providers.RandomValue.Reset();
    }

    /// <summary>
    /// Captures the exact current position and reset seed of the deterministic root random stream.
    /// </summary>
    /// <returns>An independent deterministic-state snapshot.</returns>
    /// <exception cref="System.NotSupportedException">Thrown in cryptographic mode.</exception>
    public FakeState Snapshot()
    {
        return Providers.RandomValue.Snapshot();
    }

    /// <summary>
    /// Restores the deterministic root random stream from a state snapshot.
    /// </summary>
    /// <param name="state">The snapshot to restore.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>state</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the snapshot algorithm or state is invalid.</exception>
    /// <exception cref="System.NotSupportedException">Thrown in cryptographic mode.</exception>
    public void Restore(FakeState state)
    {
        Providers.RandomValue.Restore(state);
    }

    /// <summary>
    /// Creates an independently configured generator with a stable stream name.
    /// </summary>
    /// <remarks>
    /// The child seed is derived from the root random stream's reset seed. Forking
    /// does not consume the parent. In deterministic mode, repeated names
    /// reproduce the same child. Cryptographic children remain non-reproducible.
    /// </remarks>
    /// <param name="name">The nonempty child-generator name.</param>
    /// <returns>A new generator at the start of its named stream.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>name</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    public Fake Fork(string name)
    {
        FakeOptions options = Context.OptionsSnapshot();
        options.Seed = StableSeed.ToLong(Providers.RandomValue.ForkSeed(name));
        return Create(options);
    }

    /// <summary>
    /// Returns an independent copy of the configuration captured by this generator.
    /// </summary>
    /// <returns>A copy of the captured options.</returns>
    public FakeOptions OptionsSnapshot()
    {
        return CapturedOptions.Copy();
    }
}
