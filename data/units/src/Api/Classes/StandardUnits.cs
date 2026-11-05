namespace units;

/// <summary>
/// Provides and registers the unit definitions supplied by the library.
/// </summary>
public class StandardUnits : IStandardUnits
{
    /// <summary>
    /// Initializes a standard-unit catalog.
    /// </summary>
    public StandardUnits()
    {
    }

    /// <summary>
    /// Creates the meter unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Meter()
    {
        return Unit("meter", "meter", "meters", "m", new string[] { "metre" }, new StandardDimensions().Length(), new RationalNumber(1, 1), true, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the kilogram unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Kilogram()
    {
        return Unit("kilogram", "kilogram", "kilograms", "kg", new string[0], new StandardDimensions().Mass(), new RationalNumber(1, 1), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the second unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Second()
    {
        return Unit("second", "second", "seconds", "s", new string[0], new StandardDimensions().Time(), new RationalNumber(1, 1), true, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the ampere unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Ampere()
    {
        return Unit("ampere", "ampere", "amperes", "A", new string[] { "amp" }, new StandardDimensions().ElectricCurrent(), new RationalNumber(1, 1), true, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the kelvin unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Kelvin()
    {
        return Unit("kelvin", "kelvin", "kelvins", "K", new string[0], new StandardDimensions().ThermodynamicTemperature(), new RationalNumber(1, 1), true, UnitConversionKind.Affine);
    }

    /// <summary>
    /// Creates the mole unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Mole()
    {
        return Unit("mole", "mole", "moles", "mol", new string[0], new StandardDimensions().AmountOfSubstance(), new RationalNumber(1, 1), true, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the candela unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Candela()
    {
        return Unit("candela", "candela", "candelas", "cd", new string[0], new StandardDimensions().LuminousIntensity(), new RationalNumber(1, 1), true, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the newton unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Newton()
    {
        return Unit("newton", "newton", "newtons", "N", new string[0], new StandardDimensions().Mass().Multiply(new StandardDimensions().Length()).Divide(new StandardDimensions().Time().Pow(2)), new RationalNumber(1, 1), true, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the pascal unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Pascal()
    {
        return Unit("pascal", "pascal", "pascals", "Pa", new string[0], new StandardDimensions().Mass().Divide(new StandardDimensions().Length()).Divide(new StandardDimensions().Time().Pow(2)), new RationalNumber(1, 1), true, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the joule unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Joule()
    {
        return Unit("joule", "joule", "joules", "J", new string[0], new StandardDimensions().Mass().Multiply(new StandardDimensions().Length().Pow(2)).Divide(new StandardDimensions().Time().Pow(2)), new RationalNumber(1, 1), true, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the watt unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Watt()
    {
        return Unit("watt", "watt", "watts", "W", new string[0], new StandardDimensions().Mass().Multiply(new StandardDimensions().Length().Pow(2)).Divide(new StandardDimensions().Time().Pow(3)), new RationalNumber(1, 1), true, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the coulomb unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Coulomb()
    {
        return Unit("coulomb", "coulomb", "coulombs", "C", new string[0], new StandardDimensions().ElectricCurrent().Multiply(new StandardDimensions().Time()), new RationalNumber(1, 1), true, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the volt unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Volt()
    {
        return Unit("volt", "volt", "volts", "V", new string[0], new StandardDimensions().Mass().Multiply(new StandardDimensions().Length().Pow(2)).Divide(new StandardDimensions().Time().Pow(3)).Divide(new StandardDimensions().ElectricCurrent()), new RationalNumber(1, 1), true, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the ohm unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Ohm()
    {
        return Unit("ohm", "ohm", "ohms", "\u03a9", new string[0], new StandardDimensions().Mass().Multiply(new StandardDimensions().Length().Pow(2)).Divide(new StandardDimensions().Time().Pow(3)).Divide(new StandardDimensions().ElectricCurrent().Pow(2)), new RationalNumber(1, 1), true, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the farad unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Farad()
    {
        return Unit("farad", "farad", "farads", "F", new string[0], new StandardDimensions().ElectricCurrent().Pow(2).Multiply(new StandardDimensions().Time().Pow(4)).Divide(new StandardDimensions().Mass()).Divide(new StandardDimensions().Length().Pow(2)), new RationalNumber(1, 1), true, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the tesla unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Tesla()
    {
        return Unit("tesla", "tesla", "teslas", "T", new string[0], new StandardDimensions().Mass().Divide(new StandardDimensions().Time().Pow(2)).Divide(new StandardDimensions().ElectricCurrent()), new RationalNumber(1, 1), true, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the hertz unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Hertz()
    {
        return Unit("hertz", "hertz", "hertz", "Hz", new string[0], new StandardDimensions().Time().Pow(-1), new RationalNumber(1, 1), true, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the radian unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Radian()
    {
        return Unit("radian", "radian", "radians", "rad", new string[0], new StandardDimensions().PlaneAngle(), new RationalNumber(1, 1), true, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the steradian unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Steradian()
    {
        return Unit("steradian", "steradian", "steradians", "sr", new string[0], new StandardDimensions().SolidAngle(), new RationalNumber(1, 1), true, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the lumen unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Lumen()
    {
        return Unit("lumen", "lumen", "lumens", "lm", new string[0], new StandardDimensions().LuminousIntensity().Multiply(new StandardDimensions().SolidAngle()), new RationalNumber(1, 1), true, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the lux unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Lux()
    {
        return Unit("lux", "lux", "lux", "lx", new string[0], new StandardDimensions().LuminousIntensity().Multiply(new StandardDimensions().SolidAngle()).Divide(new StandardDimensions().Length().Pow(2)), new RationalNumber(1, 1), true, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the gram unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Gram()
    {
        return Unit("gram", "gram", "grams", "g", new string[] { "gramme" }, new StandardDimensions().Mass(), new RationalNumber(1, 1000), true, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the liter unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Liter()
    {
        return Unit("liter", "liter", "liters", "L", new string[] { "litre" }, new StandardDimensions().Length().Pow(3), new RationalNumber(1, 1000), true, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the tonne unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Tonne()
    {
        return Unit("tonne", "tonne", "tonnes", "t", new string[] { "metric ton" }, new StandardDimensions().Mass(), new RationalNumber(1000, 1), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the hectare unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Hectare()
    {
        return Unit("hectare", "hectare", "hectares", "ha", new string[0], new StandardDimensions().Length().Pow(2), new RationalNumber(10000, 1), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the minute unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Minute()
    {
        return Unit("minute", "minute", "minutes", "min", new string[0], new StandardDimensions().Time(), new RationalNumber(60, 1), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the hour unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Hour()
    {
        return Unit("hour", "hour", "hours", "h", new string[0], new StandardDimensions().Time(), new RationalNumber(3600, 1), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the day unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Day()
    {
        return Unit("day", "day", "days", "d", new string[0], new StandardDimensions().Time(), new RationalNumber(86400, 1), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the electronvolt unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Electronvolt()
    {
        return Unit("electronvolt", "electronvolt", "electronvolts", "eV", new string[] { "electron volt" }, Joule().GetDimension(), ElectronvoltScale(), true, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the kilometer unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Kilometer()
    {
        return Unit("kilometer", "kilometer", "kilometers", "km", new string[] { "kilometre", "kilometres" }, new StandardDimensions().Length(), new RationalNumber(1000, 1), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the kilonewton unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Kilonewton()
    {
        return Unit("kilo_newton", "kilonewton", "kilonewtons", "kN", new string[0], Newton().GetDimension(), new RationalNumber(1000, 1), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the kilopascal unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Kilopascal()
    {
        return Unit("kilo_pascal", "kilopascal", "kilopascals", "kPa", new string[0], Pascal().GetDimension(), new RationalNumber(1000, 1), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the kilojoule unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Kilojoule()
    {
        return Unit("kilo_joule", "kilojoule", "kilojoules", "kJ", new string[0], Joule().GetDimension(), new RationalNumber(1000, 1), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the kilowatt unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Kilowatt()
    {
        return Unit("kilo_watt", "kilowatt", "kilowatts", "kW", new string[0], Watt().GetDimension(), new RationalNumber(1000, 1), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the kilohertz unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Kilohertz()
    {
        return Unit("kilo_hertz", "kilohertz", "kilohertz", "kHz", new string[0], Hertz().GetDimension(), new RationalNumber(1000, 1), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the megahertz unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Megahertz()
    {
        return Unit("mega_hertz", "megahertz", "megahertz", "MHz", new string[0], Hertz().GetDimension(), new RationalNumber(1000000, 1), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the degree Celsius temperature-point definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition DegreeCelsius()
    {
        return AffineUnit("degree_celsius", "degree Celsius", "degrees Celsius", "degC", new string[0], new RationalNumber(1, 1), new RationalNumber(27315, 100));
    }

    /// <summary>
    /// Creates the degree Fahrenheit temperature-point definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition DegreeFahrenheit()
    {
        return AffineUnit("degree_fahrenheit", "degree Fahrenheit", "degrees Fahrenheit", "degF", new string[0], new RationalNumber(5, 9), new RationalNumber(45967, 180));
    }

    /// <summary>
    /// Creates the degree Rankine temperature-point definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Rankine()
    {
        return AffineUnit("rankine", "degree Rankine", "degrees Rankine", "degR", new string[0], new RationalNumber(5, 9), new RationalNumber(0, 1));
    }

    /// <summary>
    /// Creates the kelvin temperature-interval definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition KelvinInterval()
    {
        return Unit("kelvin_interval", "kelvin interval", "kelvin intervals", "delta_K", new string[0], new StandardDimensions().ThermodynamicTemperature(), new RationalNumber(1, 1), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the Celsius temperature-interval definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition CelsiusInterval()
    {
        return Unit("celsius_interval", "Celsius interval", "Celsius intervals", "delta_degC", new string[0], new StandardDimensions().ThermodynamicTemperature(), new RationalNumber(1, 1), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the Fahrenheit temperature-interval definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition FahrenheitInterval()
    {
        return Unit("fahrenheit_interval", "Fahrenheit interval", "Fahrenheit intervals", "delta_degF", new string[0], new StandardDimensions().ThermodynamicTemperature(), new RationalNumber(5, 9), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the Rankine temperature-interval definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition RankineInterval()
    {
        return Unit("rankine_interval", "Rankine interval", "Rankine intervals", "delta_degR", new string[0], new StandardDimensions().ThermodynamicTemperature(), new RationalNumber(5, 9), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the dimensionless identity unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition One()
    {
        return Unit("one", "one", "one", "1", new string[0], new StandardDimensions().Dimensionless(), new RationalNumber(1, 1), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the percent unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Percent()
    {
        return Unit("percent", "percent", "percent", "%", new string[0], new StandardDimensions().Dimensionless(), new RationalNumber(1, 100), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the count unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Count()
    {
        return Unit("count", "count", "counts", "count", new string[0], new StandardDimensions().Count(), new RationalNumber(1, 1), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the generic currency unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition CurrencyUnit()
    {
        return Unit("currency_unit", "currency unit", "currency units", "currency", new string[0], new StandardDimensions().Currency(), new RationalNumber(1, 1), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the centimeter unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Centimeter()
    {
        return Unit("centimeter", "centimeter", "centimeters", "cm", new string[] { "centimetre", "centimetres" }, new StandardDimensions().Length(), new RationalNumber(1, 100), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the dyne unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Dyne()
    {
        return Unit("dyne", "dyne", "dynes", "dyn", new string[0], Newton().GetDimension(), new RationalNumber(1, 100000), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the barye unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Barye()
    {
        return Unit("barye", "barye", "baryes", "Ba", new string[0], Pascal().GetDimension(), new RationalNumber(1, 10), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the erg unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Erg()
    {
        return Unit("erg", "erg", "ergs", "erg", new string[0], Joule().GetDimension(), new RationalNumber(1, 10000000), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the statcoulomb unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Statcoulomb()
    {
        return Unit("statcoulomb", "statcoulomb", "statcoulombs", "statC", new string[] { "esu" }, Coulomb().GetDimension(), StatcoulombScale(), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the statampere unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Statampere()
    {
        return Unit("statampere", "statampere", "statamperes", "statA", new string[0], Ampere().GetDimension(), StatcoulombScale(), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the statvolt unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Statvolt()
    {
        return Unit("statvolt", "statvolt", "statvolts", "statV", new string[0], Volt().GetDimension(), StatvoltScale(), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the statohm unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Statohm()
    {
        return Unit("statohm", "statohm", "statohms", "statOhm", new string[0], Ohm().GetDimension(), StatvoltScale().Divide(StatcoulombScale()), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the statfarad unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Statfarad()
    {
        return Unit("statfarad", "statfarad", "statfarads", "statF", new string[0], Farad().GetDimension(), StatcoulombScale().Divide(StatvoltScale()), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the inch unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Inch()
    {
        return Unit("inch", "inch", "inches", "in", new string[0], new StandardDimensions().Length(), InchScale(), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the foot unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Foot()
    {
        return Unit("foot", "foot", "feet", "ft", new string[0], new StandardDimensions().Length(), FootScale(), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the yard unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Yard()
    {
        return Unit("yard", "yard", "yards", "yd", new string[0], new StandardDimensions().Length(), new RationalNumber(1143, 1250), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the mile unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Mile()
    {
        return Unit("mile", "mile", "miles", "mi", new string[0], new StandardDimensions().Length(), new RationalNumber(201168, 125), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the pound unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Pound()
    {
        return Unit("pound", "pound", "pounds", "lb", new string[] { "avoirdupois pound" }, new StandardDimensions().Mass(), PoundScale(), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the ounce unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Ounce()
    {
        return Unit("ounce", "ounce", "ounces", "oz", new string[] { "avoirdupois ounce" }, new StandardDimensions().Mass(), OunceScale(), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the pound-force unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition PoundForce()
    {
        return Unit("pound_force", "pound-force", "pounds-force", "lbf", new string[] { "pound force" }, Newton().GetDimension(), PoundForceScale(), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the gallon unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Gallon()
    {
        return UsGallon();
    }

    /// <summary>
    /// Creates the US liquid gallon unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition UsGallon()
    {
        return Unit("us_gallon", "US gallon", "US gallons", "gal", new string[] { "gallon" }, new StandardDimensions().Length().Pow(3), GallonScale(), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the quart unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Quart()
    {
        return UsQuart();
    }

    /// <summary>
    /// Creates the US liquid quart unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition UsQuart()
    {
        return Unit("us_quart", "US quart", "US quarts", "qt", new string[] { "quart" }, new StandardDimensions().Length().Pow(3), QuartScale(), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the pint unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Pint()
    {
        return UsPint();
    }

    /// <summary>
    /// Creates the US liquid pint unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition UsPint()
    {
        return Unit("us_pint", "US pint", "US pints", "pt", new string[] { "pint" }, new StandardDimensions().Length().Pow(3), PintScale(), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the cup unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Cup()
    {
        return UsCup();
    }

    /// <summary>
    /// Creates the US customary cup unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition UsCup()
    {
        return Unit("us_cup", "US cup", "US cups", "cup", new string[] { "cup" }, new StandardDimensions().Length().Pow(3), CupScale(), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the US liquid fluid-ounce unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition UsFluidOunce()
    {
        return Unit("us_fluid_ounce", "US fluid ounce", "US fluid ounces", "fl_oz_us", new string[0], new StandardDimensions().Length().Pow(3), GallonScale().Divide(new RationalNumber(128, 1)), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the Imperial gallon unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition ImperialGallon()
    {
        return Unit("imperial_gallon", "Imperial gallon", "Imperial gallons", "gal_imp", new string[] { "British gallon" }, new StandardDimensions().Length().Pow(3), ImperialGallonScale(), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the Imperial quart unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition ImperialQuart()
    {
        return Unit("imperial_quart", "Imperial quart", "Imperial quarts", "qt_imp", new string[] { "British quart" }, new StandardDimensions().Length().Pow(3), ImperialGallonScale().Divide(new RationalNumber(4, 1)), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the Imperial pint unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition ImperialPint()
    {
        return Unit("imperial_pint", "Imperial pint", "Imperial pints", "pt_imp", new string[] { "British pint" }, new StandardDimensions().Length().Pow(3), ImperialGallonScale().Divide(new RationalNumber(8, 1)), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the Imperial fluid-ounce unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition ImperialFluidOunce()
    {
        return Unit("imperial_fluid_ounce", "Imperial fluid ounce", "Imperial fluid ounces", "fl_oz_imp", new string[] { "British fluid ounce" }, new StandardDimensions().Length().Pow(3), ImperialGallonScale().Divide(new RationalNumber(160, 1)), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the acre unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Acre()
    {
        return Unit("acre", "acre", "acres", "ac", new string[0], new StandardDimensions().Length().Pow(2), AcreScale(), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the psi unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Psi()
    {
        return Unit("psi", "pound per square inch", "pounds per square inch", "psi", new string[] { "pound-force per square inch" }, Pascal().GetDimension(), PoundForceScale().Divide(InchScale().Multiply(InchScale())), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the horsepower unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Horsepower()
    {
        return Unit("horsepower", "horsepower", "horsepower", "hp", new string[] { "mechanical horsepower" }, Watt().GetDimension(), FootScale().Multiply(PoundForceScale()).Multiply(new RationalNumber(550, 1)), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the btu unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Btu()
    {
        return Unit("british_thermal_unit", "British thermal unit", "British thermal units", "BTU", new string[] { "Btu" }, Joule().GetDimension(), new RationalNumber(52752792631, 50000000), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the bit unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Bit()
    {
        return Unit("bit", "bit", "bits", "bit", new string[0], new StandardDimensions().Information(), new RationalNumber(1, 1), true, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the byte unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Byte()
    {
        return Unit("byte", "byte", "bytes", "B", new string[] { "octet" }, new StandardDimensions().Information(), new RationalNumber(8, 1), true, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the kilobyte unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Kilobyte()
    {
        return Unit("kilobyte", "kilobyte", "kilobytes", "kB", new string[0], new StandardDimensions().Information(), new RationalNumber(8000, 1), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the kibibyte unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Kibibyte()
    {
        return Unit("kibibyte", "kibibyte", "kibibytes", "KiB", new string[0], new StandardDimensions().Information(), new RationalNumber(8192, 1), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the megabyte unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Megabyte()
    {
        return Unit("megabyte", "megabyte", "megabytes", "MB", new string[0], new StandardDimensions().Information(), new RationalNumber(8000000, 1), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the mebibyte unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Mebibyte()
    {
        return Unit("mebibyte", "mebibyte", "mebibytes", "MiB", new string[0], new StandardDimensions().Information(), new RationalNumber(8388608, 1), false, UnitConversionKind.Scale);
    }

    /// <summary>
    /// Creates the square-meter compound unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition SquareMeter()
    {
        return Meter().Pow(2);
    }

    /// <summary>
    /// Creates the cubic-meter compound unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition CubicMeter()
    {
        return Meter().Pow(3);
    }

    /// <summary>
    /// Creates the meter-per-second compound unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition MeterPerSecond()
    {
        return Meter().Divide(Second());
    }

    /// <summary>
    /// Creates the kilometer-per-hour compound unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition KilometerPerHour()
    {
        return Kilometer().Divide(Hour());
    }

    /// <summary>
    /// Creates the mile-per-hour compound unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition MilePerHour()
    {
        return Mile().Divide(Hour());
    }

    /// <summary>
    /// Creates the meter-per-second-squared compound unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition MeterPerSecondSquared()
    {
        return Meter().Divide(Second().Pow(2));
    }

    /// <summary>
    /// Creates the foot-per-second-squared compound unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition FootPerSecondSquared()
    {
        return Foot().Divide(Second().Pow(2));
    }

    /// <summary>
    /// Creates the radian-per-second compound unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition RadianPerSecond()
    {
        return Radian().Divide(Second());
    }

    /// <summary>
    /// Creates the kilogram-per-cubic-meter compound unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition KilogramPerCubicMeter()
    {
        return Kilogram().Divide(CubicMeter());
    }

    /// <summary>
    /// Creates the cubic-meter-per-second compound unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition CubicMeterPerSecond()
    {
        return CubicMeter().Divide(Second());
    }

    /// <summary>
    /// Creates the square-centimeter compound unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition SquareCentimeter()
    {
        return Centimeter().Pow(2);
    }

    /// <summary>
    /// Creates the cubic-centimeter compound unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition CubicCentimeter()
    {
        return Centimeter().Pow(3);
    }

    /// <summary>
    /// Creates the centimeter-per-second compound unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition CentimeterPerSecond()
    {
        return Centimeter().Divide(Second());
    }

    /// <summary>
    /// Creates the centimeter-per-second-squared compound unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition CentimeterPerSecondSquared()
    {
        return Centimeter().Divide(Second().Pow(2));
    }

    /// <summary>
    /// Creates the erg-per-second compound unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition ErgPerSecond()
    {
        return Erg().Divide(Second());
    }

    /// <summary>
    /// Creates the gram-per-cubic-centimeter compound unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition GramPerCubicCentimeter()
    {
        return Gram().Divide(CubicCentimeter());
    }

    /// <summary>
    /// Creates the cubic-centimeter-per-second compound unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition CubicCentimeterPerSecond()
    {
        return CubicCentimeter().Divide(Second());
    }

    /// <summary>
    /// Creates the square-foot compound unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition SquareFoot()
    {
        return Foot().Pow(2);
    }

    /// <summary>
    /// Creates the cubic-foot compound unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition CubicFoot()
    {
        return Foot().Pow(3);
    }

    /// <summary>
    /// Creates the foot-per-second compound unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition FootPerSecond()
    {
        return Foot().Divide(Second());
    }

    /// <summary>
    /// Creates the pound-per-cubic-foot compound unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition PoundPerCubicFoot()
    {
        return Pound().Divide(CubicFoot());
    }

    /// <summary>
    /// Creates the cubic-foot-per-second compound unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition CubicFootPerSecond()
    {
        return CubicFoot().Divide(Second());
    }

    /// <summary>
    /// Creates the US-gallon-per-minute compound unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition UsGallonPerMinute()
    {
        return UsGallon().Divide(Minute());
    }

    /// <summary>
    /// Creates the kilowatt-hour compound unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition KilowattHour()
    {
        return Kilowatt().Multiply(Hour());
    }

    /// <summary>
    /// Creates the bit-per-second compound unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition BitPerSecond()
    {
        return Bit().Divide(Second());
    }

    /// <summary>
    /// Creates the byte-per-second compound unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition BytePerSecond()
    {
        return Byte().Divide(Second());
    }

    /// <summary>
    /// Creates the kilobyte-per-second compound unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition KilobytePerSecond()
    {
        return Kilobyte().Divide(Second());
    }

    /// <summary>
    /// Creates the kibibyte-per-second compound unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition KibibytePerSecond()
    {
        return Kibibyte().Divide(Second());
    }

    /// <summary>
    /// Creates the megabyte-per-second compound unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition MegabytePerSecond()
    {
        return Megabyte().Divide(Second());
    }

    /// <summary>
    /// Creates the mebibyte-per-second compound unit definition.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition MebibytePerSecond()
    {
        return Mebibyte().Divide(Second());
    }

    /// <summary>
    /// Creates the collection of SI base units.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitDefinition[] CreateSiBaseUnits()
    {
        return new UnitDefinition[]
        {
            Meter(),
            Kilogram(),
            Second(),
            Ampere(),
            Kelvin(),
            Mole(),
            Candela()
        };
    }

    /// <summary>
    /// Creates the collection of SI derived units.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitDefinition[] CreateSiDerivedUnits()
    {
        return new UnitDefinition[]
        {
            Newton(),
            Pascal(),
            Joule(),
            Watt(),
            Coulomb(),
            Volt(),
            Ohm(),
            Farad(),
            Tesla(),
            Hertz(),
            Radian(),
            Steradian(),
            Lumen(),
            Lux()
        };
    }

    /// <summary>
    /// Creates the collection of metric units.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitDefinition[] CreateMetricUnits()
    {
        return new UnitDefinition[]
        {
            Gram(),
            Liter(),
            Tonne(),
            Hectare(),
            Minute(),
            Hour(),
            Day(),
            Electronvolt(),
            Kilometer(),
            Kilonewton(),
            Kilopascal(),
            Kilojoule(),
            Kilowatt(),
            Kilohertz(),
            Megahertz()
        };
    }

    /// <summary>
    /// Creates the collection of temperature-point and temperature-interval units not already present among the SI base units.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitDefinition[] CreateTemperatureUnits()
    {
        return new UnitDefinition[]
        {
            DegreeCelsius(),
            DegreeFahrenheit(),
            Rankine(),
            KelvinInterval(),
            CelsiusInterval(),
            FahrenheitInterval(),
            RankineInterval()
        };
    }

    /// <summary>
    /// Creates the collection of dimensionless, count, and currency units.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitDefinition[] CreateDimensionlessUnits()
    {
        return new UnitDefinition[]
        {
            One(),
            Percent(),
            Count(),
            CurrencyUnit()
        };
    }

    /// <summary>
    /// Creates the collection of named CGS units not already present in the SI and metric collections.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitDefinition[] CreateCgsUnits()
    {
        return new UnitDefinition[]
        {
            Centimeter(),
            Dyne(),
            Barye(),
            Erg(),
            Statcoulomb(),
            Statampere(),
            Statvolt(),
            Statohm(),
            Statfarad()
        };
    }

    /// <summary>
    /// Creates the collection of imperial and US customary units.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitDefinition[] CreateImperialAndUsCustomaryUnits()
    {
        return new UnitDefinition[]
        {
            Inch(),
            Foot(),
            Yard(),
            Mile(),
            Pound(),
            Ounce(),
            PoundForce(),
            UsGallon(),
            UsQuart(),
            UsPint(),
            UsCup(),
            UsFluidOunce(),
            ImperialGallon(),
            ImperialQuart(),
            ImperialPint(),
            ImperialFluidOunce(),
            Acre(),
            Psi(),
            Horsepower(),
            Btu()
        };
    }

    /// <summary>
    /// Creates the collection of data units.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitDefinition[] CreateDataUnits()
    {
        return new UnitDefinition[]
        {
            Bit(),
            Byte(),
            Kilobyte(),
            Kibibyte(),
            Megabyte(),
            Mebibyte()
        };
    }

    /// <summary>
    /// Creates every registered compound unit referenced by standard quantity kinds, facades, and unit systems.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitDefinition[] CreateCompoundUnits()
    {
        return new UnitDefinition[]
        {
            SquareMeter(),
            CubicMeter(),
            MeterPerSecond(),
            KilometerPerHour(),
            MilePerHour(),
            MeterPerSecondSquared(),
            FootPerSecondSquared(),
            RadianPerSecond(),
            KilogramPerCubicMeter(),
            CubicMeterPerSecond(),
            SquareCentimeter(),
            CubicCentimeter(),
            CentimeterPerSecond(),
            CentimeterPerSecondSquared(),
            ErgPerSecond(),
            GramPerCubicCentimeter(),
            CubicCentimeterPerSecond(),
            SquareFoot(),
            CubicFoot(),
            FootPerSecond(),
            PoundPerCubicFoot(),
            CubicFootPerSecond(),
            UsGallonPerMinute(),
            KilowattHour(),
            BitPerSecond(),
            BytePerSecond(),
            KilobytePerSecond(),
            KibibytePerSecond(),
            MegabytePerSecond(),
            MebibytePerSecond()
        };
    }

    /// <summary>
    /// Registers SI base units in the supplied registry.
    /// </summary>
    /// <param name="registry">The registry that supplies definitions and metadata.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult RegisterSiBaseUnits(UnitRegistry registry)
    {
        if (registry == null)
        {
            throw new System.ArgumentNullException("registry");
        }

        UnitDefinition[] units = CreateSiBaseUnits();
        int index = 0;

        while (index < units.Length)
        {
            OperationResult result = registry.AddUnit(units[index]);
            if (result.IsFailure())
            {
                return result;
            }

            index = index + 1;
        }

        return OperationResult.CreateSuccess();
    }

    /// <summary>
    /// Registers SI derived units in the supplied registry.
    /// </summary>
    /// <param name="registry">The registry that supplies definitions and metadata.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult RegisterSiDerivedUnits(UnitRegistry registry)
    {
        if (registry == null)
        {
            throw new System.ArgumentNullException("registry");
        }

        UnitDefinition[] units = CreateSiDerivedUnits();
        int index = 0;

        while (index < units.Length)
        {
            OperationResult result = registry.AddUnit(units[index]);
            if (result.IsFailure())
            {
                return result;
            }

            index = index + 1;
        }

        return OperationResult.CreateSuccess();
    }

    /// <summary>
    /// Registers metric units in the supplied registry.
    /// </summary>
    /// <param name="registry">The registry that supplies definitions and metadata.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult RegisterMetricUnits(UnitRegistry registry)
    {
        if (registry == null)
        {
            throw new System.ArgumentNullException("registry");
        }

        UnitDefinition[] units = CreateMetricUnits();
        int index = 0;

        while (index < units.Length)
        {
            OperationResult result = registry.AddUnit(units[index]);
            if (result.IsFailure())
            {
                return result;
            }

            index = index + 1;
        }

        return OperationResult.CreateSuccess();
    }

    /// <summary>
    /// Registers temperature-point and temperature-interval units in the supplied registry.
    /// </summary>
    /// <param name="registry">The registry that supplies definitions and metadata.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult RegisterTemperatureUnits(UnitRegistry registry)
    {
        return RegisterUnits(registry, CreateTemperatureUnits());
    }

    /// <summary>
    /// Registers dimensionless, count, and currency units in the supplied registry.
    /// </summary>
    /// <param name="registry">The registry that supplies definitions and metadata.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult RegisterDimensionlessUnits(UnitRegistry registry)
    {
        return RegisterUnits(registry, CreateDimensionlessUnits());
    }

    /// <summary>
    /// Registers named CGS units in the supplied registry.
    /// </summary>
    /// <param name="registry">The registry that supplies definitions and metadata.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult RegisterCgsUnits(UnitRegistry registry)
    {
        return RegisterUnits(registry, CreateCgsUnits());
    }

    /// <summary>
    /// Registers imperial and US customary units in the supplied registry.
    /// </summary>
    /// <param name="registry">The registry that supplies definitions and metadata.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult RegisterImperialAndUsCustomaryUnits(UnitRegistry registry)
    {
        if (registry == null)
        {
            throw new System.ArgumentNullException("registry");
        }

        UnitDefinition[] units = CreateImperialAndUsCustomaryUnits();
        int index = 0;

        while (index < units.Length)
        {
            OperationResult result = registry.AddUnit(units[index]);
            if (result.IsFailure())
            {
                return result;
            }

            index = index + 1;
        }

        return OperationResult.CreateSuccess();
    }

    /// <summary>
    /// Registers data units in the supplied registry.
    /// </summary>
    /// <param name="registry">The registry that supplies definitions and metadata.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult RegisterDataUnits(UnitRegistry registry)
    {
        if (registry == null)
        {
            throw new System.ArgumentNullException("registry");
        }

        UnitDefinition[] units = CreateDataUnits();
        int index = 0;

        while (index < units.Length)
        {
            OperationResult result = registry.AddUnit(units[index]);
            if (result.IsFailure())
            {
                return result;
            }

            index = index + 1;
        }

        return OperationResult.CreateSuccess();
    }

    /// <summary>
    /// Registers standard compound units in the supplied registry.
    /// </summary>
    /// <param name="registry">The registry that supplies definitions and metadata.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult RegisterCompoundUnits(UnitRegistry registry)
    {
        return RegisterUnits(registry, CreateCompoundUnits());
    }

    internal static OperationResult RegisterUnits(UnitRegistry registry, UnitDefinition[] units)
    {
        if (registry == null)
        {
            throw new System.ArgumentNullException("registry");
        }

        int index = 0;
        while (index < units.Length)
        {
            OperationResult result = registry.AddUnit(units[index]);
            if (result.IsFailure())
            {
                return result;
            }

            index = index + 1;
        }

        return OperationResult.CreateSuccess();
    }

    internal static UnitDefinition Unit(string id, string name, string pluralName, string symbol, string[] aliases, DimensionVector dimension, RationalNumber scale, bool prefixable, UnitConversionKind conversionKind)
    {
        return new UnitDefinition(
            id,
            name,
            pluralName,
            symbol,
            aliases,
            dimension,
            scale,
            new RationalNumber(0, 1),
            prefixable,
            conversionKind);
    }

    internal static UnitDefinition AffineUnit(string id, string name, string pluralName, string symbol, string[] aliases, RationalNumber scale, RationalNumber offset)
    {
        return new UnitDefinition(
            id,
            name,
            pluralName,
            symbol,
            aliases,
            new StandardDimensions().ThermodynamicTemperature(),
            scale,
            offset,
            false,
            UnitConversionKind.Affine);
    }

    internal static RationalNumber ElectronvoltScale()
    {
        return RationalNumber.FromBigIntegers(new System.Numerics.BigInteger(1602176634), System.Numerics.BigInteger.Pow(new System.Numerics.BigInteger(10), 28));
    }

    internal static RationalNumber InchScale()
    {
        return new RationalNumber(127, 5000);
    }

    internal static RationalNumber FootScale()
    {
        return new RationalNumber(381, 1250);
    }

    internal static RationalNumber PoundScale()
    {
        return new RationalNumber(45359237, 100000000);
    }

    internal static RationalNumber OunceScale()
    {
        return new RationalNumber(45359237, 1600000000);
    }

    internal static RationalNumber GallonScale()
    {
        return new RationalNumber(473176473, 125000000000);
    }

    internal static RationalNumber ImperialGallonScale()
    {
        return new RationalNumber(454609, 100000000);
    }

    internal static RationalNumber QuartScale()
    {
        return new RationalNumber(473176473, 500000000000);
    }

    internal static RationalNumber PintScale()
    {
        return new RationalNumber(473176473, 1000000000000);
    }

    internal static RationalNumber CupScale()
    {
        return new RationalNumber(473176473, 2000000000000);
    }

    internal static RationalNumber AcreScale()
    {
        return new RationalNumber(316160658, 78125);
    }

    internal static RationalNumber PoundForceScale()
    {
        return PoundScale().Multiply(new RationalNumber(196133, 20000));
    }

    internal static RationalNumber StatcoulombScale()
    {
        return new RationalNumber(1, 1).Divide(new RationalNumber(2997924580, 1));
    }

    internal static RationalNumber StatvoltScale()
    {
        return new RationalNumber(299792458, 1000000);
    }
}
