namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="StandardUnits"/>.
/// </summary>
public interface IStandardUnits
{
    /// <summary>
    /// Creates the meter unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Meter();
    /// <summary>
    /// Creates the kilogram unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Kilogram();
    /// <summary>
    /// Creates the second unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Second();
    /// <summary>
    /// Creates the ampere unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Ampere();
    /// <summary>
    /// Creates the kelvin unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Kelvin();
    /// <summary>
    /// Creates the mole unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Mole();
    /// <summary>
    /// Creates the candela unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Candela();
    /// <summary>
    /// Creates the newton unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Newton();
    /// <summary>
    /// Creates the pascal unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Pascal();
    /// <summary>
    /// Creates the joule unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Joule();
    /// <summary>
    /// Creates the watt unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Watt();
    /// <summary>
    /// Creates the coulomb unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Coulomb();
    /// <summary>
    /// Creates the volt unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Volt();
    /// <summary>
    /// Creates the ohm unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Ohm();
    /// <summary>
    /// Creates the farad unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Farad();
    /// <summary>
    /// Creates the tesla unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Tesla();
    /// <summary>
    /// Creates the hertz unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Hertz();
    /// <summary>
    /// Creates the radian unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Radian();
    /// <summary>
    /// Creates the steradian unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Steradian();
    /// <summary>
    /// Creates the lumen unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Lumen();
    /// <summary>
    /// Creates the lux unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Lux();
    /// <summary>
    /// Creates the gram unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Gram();
    /// <summary>
    /// Creates the liter unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Liter();
    /// <summary>
    /// Creates the tonne unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Tonne();
    /// <summary>
    /// Creates the hectare unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Hectare();
    /// <summary>
    /// Creates the minute unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Minute();
    /// <summary>
    /// Creates the hour unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Hour();
    /// <summary>
    /// Creates the day unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Day();
    /// <summary>
    /// Creates the electronvolt unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Electronvolt();
    /// <summary>Creates the kilometer unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Kilometer();
    /// <summary>Creates the kilonewton unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Kilonewton();
    /// <summary>Creates the kilopascal unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Kilopascal();
    /// <summary>Creates the kilojoule unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Kilojoule();
    /// <summary>Creates the kilowatt unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Kilowatt();
    /// <summary>Creates the kilohertz unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Kilohertz();
    /// <summary>Creates the megahertz unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Megahertz();
    /// <summary>Creates the degree Celsius temperature-point definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition DegreeCelsius();
    /// <summary>Creates the degree Fahrenheit temperature-point definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition DegreeFahrenheit();
    /// <summary>Creates the degree Rankine temperature-point definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Rankine();
    /// <summary>Creates the kelvin temperature-interval definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition KelvinInterval();
    /// <summary>Creates the Celsius temperature-interval definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition CelsiusInterval();
    /// <summary>Creates the Fahrenheit temperature-interval definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition FahrenheitInterval();
    /// <summary>Creates the Rankine temperature-interval definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition RankineInterval();
    /// <summary>Creates the dimensionless identity unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition One();
    /// <summary>Creates the percent unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Percent();
    /// <summary>Creates the count unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Count();
    /// <summary>Creates the generic currency unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition CurrencyUnit();
    /// <summary>Creates the centimeter unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Centimeter();
    /// <summary>Creates the dyne unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Dyne();
    /// <summary>Creates the barye unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Barye();
    /// <summary>Creates the erg unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Erg();
    /// <summary>Creates the statcoulomb unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Statcoulomb();
    /// <summary>Creates the statampere unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Statampere();
    /// <summary>Creates the statvolt unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Statvolt();
    /// <summary>Creates the statohm unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Statohm();
    /// <summary>Creates the statfarad unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Statfarad();
    /// <summary>
    /// Creates the inch unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Inch();
    /// <summary>
    /// Creates the foot unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Foot();
    /// <summary>
    /// Creates the yard unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Yard();
    /// <summary>
    /// Creates the mile unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Mile();
    /// <summary>
    /// Creates the pound unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Pound();
    /// <summary>
    /// Creates the ounce unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Ounce();
    /// <summary>Creates the pound-force unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition PoundForce();
    /// <summary>
    /// Creates the gallon unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Gallon();
    /// <summary>Creates the US liquid gallon unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition UsGallon();
    /// <summary>
    /// Creates the quart unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Quart();
    /// <summary>Creates the US liquid quart unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition UsQuart();
    /// <summary>
    /// Creates the pint unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Pint();
    /// <summary>Creates the US liquid pint unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition UsPint();
    /// <summary>
    /// Creates the cup unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Cup();
    /// <summary>Creates the US customary cup unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition UsCup();
    /// <summary>Creates the US liquid fluid-ounce unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition UsFluidOunce();
    /// <summary>Creates the Imperial gallon unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition ImperialGallon();
    /// <summary>Creates the Imperial quart unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition ImperialQuart();
    /// <summary>Creates the Imperial pint unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition ImperialPint();
    /// <summary>Creates the Imperial fluid-ounce unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition ImperialFluidOunce();
    /// <summary>
    /// Creates the acre unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Acre();
    /// <summary>
    /// Creates the psi unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Psi();
    /// <summary>
    /// Creates the horsepower unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Horsepower();
    /// <summary>
    /// Creates the btu unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Btu();
    /// <summary>
    /// Creates the bit unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Bit();
    /// <summary>
    /// Creates the byte unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Byte();
    /// <summary>
    /// Creates the kilobyte unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Kilobyte();
    /// <summary>
    /// Creates the kibibyte unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Kibibyte();
    /// <summary>
    /// Creates the megabyte unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Megabyte();
    /// <summary>
    /// Creates the mebibyte unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Mebibyte();
    /// <summary>Creates the square-meter compound unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition SquareMeter();
    /// <summary>Creates the cubic-meter compound unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition CubicMeter();
    /// <summary>Creates the meter-per-second compound unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition MeterPerSecond();
    /// <summary>Creates the kilometer-per-hour compound unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition KilometerPerHour();
    /// <summary>Creates the mile-per-hour compound unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition MilePerHour();
    /// <summary>Creates the meter-per-second-squared compound unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition MeterPerSecondSquared();
    /// <summary>Creates the foot-per-second-squared compound unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition FootPerSecondSquared();
    /// <summary>Creates the radian-per-second compound unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition RadianPerSecond();
    /// <summary>Creates the kilogram-per-cubic-meter compound unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition KilogramPerCubicMeter();
    /// <summary>Creates the cubic-meter-per-second compound unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition CubicMeterPerSecond();
    /// <summary>Creates the square-centimeter compound unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition SquareCentimeter();
    /// <summary>Creates the cubic-centimeter compound unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition CubicCentimeter();
    /// <summary>Creates the centimeter-per-second compound unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition CentimeterPerSecond();
    /// <summary>Creates the centimeter-per-second-squared compound unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition CentimeterPerSecondSquared();
    /// <summary>Creates the erg-per-second compound unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition ErgPerSecond();
    /// <summary>Creates the gram-per-cubic-centimeter compound unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition GramPerCubicCentimeter();
    /// <summary>Creates the cubic-centimeter-per-second compound unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition CubicCentimeterPerSecond();
    /// <summary>Creates the square-foot compound unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition SquareFoot();
    /// <summary>Creates the cubic-foot compound unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition CubicFoot();
    /// <summary>Creates the foot-per-second compound unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition FootPerSecond();
    /// <summary>Creates the pound-per-cubic-foot compound unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition PoundPerCubicFoot();
    /// <summary>Creates the cubic-foot-per-second compound unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition CubicFootPerSecond();
    /// <summary>Creates the US-gallon-per-minute compound unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition UsGallonPerMinute();
    /// <summary>Creates the kilowatt-hour compound unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition KilowattHour();
    /// <summary>Creates the bit-per-second compound unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition BitPerSecond();
    /// <summary>Creates the byte-per-second compound unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition BytePerSecond();
    /// <summary>Creates the kilobyte-per-second compound unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition KilobytePerSecond();
    /// <summary>Creates the kibibyte-per-second compound unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition KibibytePerSecond();
    /// <summary>Creates the megabyte-per-second compound unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition MegabytePerSecond();
    /// <summary>Creates the mebibyte-per-second compound unit definition.</summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition MebibytePerSecond();
    /// <summary>
    /// Creates the collection of SI base units.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitDefinition[] CreateSiBaseUnits();
    /// <summary>
    /// Creates the collection of SI derived units.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitDefinition[] CreateSiDerivedUnits();
    /// <summary>
    /// Creates the collection of metric units.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitDefinition[] CreateMetricUnits();
    /// <summary>Creates the collection of temperature-point and temperature-interval units.</summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitDefinition[] CreateTemperatureUnits();
    /// <summary>Creates the collection of dimensionless, count, and currency units.</summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitDefinition[] CreateDimensionlessUnits();
    /// <summary>Creates the collection of named CGS units.</summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitDefinition[] CreateCgsUnits();
    /// <summary>
    /// Creates the collection of imperial and US customary units.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitDefinition[] CreateImperialAndUsCustomaryUnits();
    /// <summary>
    /// Creates the collection of data units.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitDefinition[] CreateDataUnits();
    /// <summary>Creates every standard compound unit referenced by public metadata and facades.</summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitDefinition[] CreateCompoundUnits();
    /// <summary>
    /// Registers SI base units in the supplied registry.
    /// </summary>
    /// <param name="registry">The registry that supplies definitions and metadata.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult RegisterSiBaseUnits(UnitRegistry registry);
    /// <summary>
    /// Registers SI derived units in the supplied registry.
    /// </summary>
    /// <param name="registry">The registry that supplies definitions and metadata.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult RegisterSiDerivedUnits(UnitRegistry registry);
    /// <summary>
    /// Registers metric units in the supplied registry.
    /// </summary>
    /// <param name="registry">The registry that supplies definitions and metadata.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult RegisterMetricUnits(UnitRegistry registry);
    /// <summary>Registers temperature-point and temperature-interval units.</summary>
    /// <param name="registry">The registry that supplies definitions and metadata.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult RegisterTemperatureUnits(UnitRegistry registry);
    /// <summary>Registers dimensionless, count, and currency units.</summary>
    /// <param name="registry">The registry that supplies definitions and metadata.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult RegisterDimensionlessUnits(UnitRegistry registry);
    /// <summary>Registers named CGS units.</summary>
    /// <param name="registry">The registry that supplies definitions and metadata.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult RegisterCgsUnits(UnitRegistry registry);
    /// <summary>
    /// Registers imperial and US customary units in the supplied registry.
    /// </summary>
    /// <param name="registry">The registry that supplies definitions and metadata.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult RegisterImperialAndUsCustomaryUnits(UnitRegistry registry);
    /// <summary>
    /// Registers data units in the supplied registry.
    /// </summary>
    /// <param name="registry">The registry that supplies definitions and metadata.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult RegisterDataUnits(UnitRegistry registry);
    /// <summary>Registers standard compound units.</summary>
    /// <param name="registry">The registry that supplies definitions and metadata.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult RegisterCompoundUnits(UnitRegistry registry);
}
