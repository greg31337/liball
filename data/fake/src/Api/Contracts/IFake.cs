namespace fake;
/// <summary>Defines the public operations exposed by <see cref="Fake"/>.</summary>
public interface IFake
{
    /// <summary>
    /// Returns the cached low-level random provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    FakeRandom Random();
    /// <summary>
    /// Returns the cached primitive provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    PrimitiveProvider Primitive();
    /// <summary>
    /// Returns the cached text provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    TextProvider Text();
    /// <summary>
    /// Returns the cached template provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    TemplateProvider Template();
    /// <summary>
    /// Returns the cached checksum provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    ChecksumProvider Checksum();
    /// <summary>
    /// Returns the cached identifier provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    IdentifierProvider Identifier();
    /// <summary>
    /// Returns the cached date and time provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    DateProvider Date();
    /// <summary>
    /// Returns the cached locale provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    LocaleProvider Locale();
    /// <summary>
    /// Returns the cached person provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    PersonProvider Person();
    /// <summary>
    /// Returns the cached address provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    AddressProvider Address();
    /// <summary>
    /// Returns the cached internet provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    InternetProvider Internet();
    /// <summary>
    /// Returns the cached phone provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    PhoneProvider Phone();
    /// <summary>
    /// Returns the cached device provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    DeviceProvider Device();
    /// <summary>
    /// Returns the cached finance provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    FinanceProvider Finance();
    /// <summary>
    /// Returns the cached company provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    CompanyProvider Company();
    /// <summary>
    /// Returns the cached commerce provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    CommerceProvider Commerce();
    /// <summary>
    /// Returns the cached system provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    SystemProvider System();
    /// <summary>
    /// Returns the cached database provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    DatabaseProvider Database();
    /// <summary>
    /// Returns the cached source-control provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    GitProvider Git();
    /// <summary>
    /// Returns the cached color provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    ColorProvider Color();
    /// <summary>
    /// Returns the cached vehicle provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    VehicleProvider Vehicle();
    /// <summary>
    /// Returns the cached airline provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    AirlineProvider Airline();
    /// <summary>
    /// Returns the cached book provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    BookProvider Book();
    /// <summary>
    /// Returns the cached synthetic-media provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    MediaProvider Media();
    /// <summary>
    /// Returns the cached food provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    FoodProvider Food();
    /// <summary>
    /// Returns the cached animal provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    AnimalProvider Animal();
    /// <summary>
    /// Returns the cached science provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    ScienceProvider Science();
    /// <summary>
    /// Returns the cached health provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    HealthProvider Health();
    /// <summary>
    /// Returns the cached education provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    EducationProvider Education();
    /// <summary>
    /// Returns the cached culture provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    CultureProvider Culture();
    /// <summary>
    /// Returns the cached correlated-profile provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    ProfileProvider Profile();
    /// <summary>
    /// Returns the cached schema generator.
    /// </summary>
    /// <returns>The generator for this instance.</returns>
    SchemaGenerator Schema();
    /// <summary>
    /// Returns the cached edge-case provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    EdgeProvider Edge();
    /// <summary>
    /// Returns the cached export provider.
    /// </summary>
    /// <returns>The provider for this instance.</returns>
    ExportProvider Export();
    /// <summary>
    /// Restarts the deterministic root random stream from its current seed.
    /// Cryptographic mode continues from the platform random source.
    /// </summary>
    void Reset();
    /// <summary>
    /// Captures the exact current position and reset seed of the deterministic root random stream.
    /// </summary>
    /// <returns>An independent deterministic-state snapshot.</returns>
    /// <exception cref="System.NotSupportedException">Thrown in cryptographic mode.</exception>
    FakeState Snapshot();
    /// <summary>
    /// Returns an independent copy of the configuration captured by this generator.
    /// </summary>
    /// <returns>A copy of the captured options.</returns>
    FakeOptions OptionsSnapshot();
    /// <summary>
    /// Restores the deterministic root random stream from a state snapshot.
    /// </summary>
    /// <param name="state">The snapshot to restore.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>state</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the snapshot algorithm or state is invalid.</exception>
    /// <exception cref="System.NotSupportedException">Thrown in cryptographic mode.</exception>
    void Restore(FakeState state);
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
    Fake Fork(string name);
}
