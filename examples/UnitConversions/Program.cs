using System.Globalization;
using units;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
var standard = new StandardUnits();
var math = new QuantityMath();
Console.WriteLine("Everyday unit conversions");
var mile = new QuantityValue(1, standard.Mile());
Console.WriteLine($"1 mile             = {mile.As(standard.Meter()):F3} m");
var temperature = new QuantityValue(20, standard.DegreeCelsius());
Console.WriteLine($"20 degrees Celsius = {temperature.As(standard.DegreeFahrenheit()):F2} degrees Fahrenheit");
Console.WriteLine($"20 degrees Celsius = {temperature.As(standard.Kelvin()):F2} K");
var length = math.Add(new QuantityValue(3, standard.Foot()), new QuantityValue(12, standard.Inch()));
Console.WriteLine($"3 feet + 12 inches = {length.As(standard.Foot()):F2} feet");

// Quantity arithmetic carries dimensions through division and unit conversion.
var speed = math.Divide(new QuantityValue(150, standard.Kilometer()), new QuantityValue(2, standard.Hour()));
Console.WriteLine(
    $"150 km in 2 hours  = {speed.As(standard.KilometerPerHour()):F2} km/h " +
    $"({speed.As(standard.MeterPerSecond()):F4} m/s)");
Console.WriteLine(
    $"1 MiB              = {new QuantityValue(1, standard.Mebibyte()).As(standard.Byte()):N0} bytes");

// Parse a compound acceleration unit, then multiply by mass to derive force.
var registry = new StandardRegistry().Create();
var acceleration = new QuantityParser(new UnitParser()).ParseOrThrow("9.81 m/s^2", registry);
var force = math.Multiply(new QuantityValue(80, standard.Kilogram()), acceleration);
Console.WriteLine($"80 kg * 9.81 m/s^2 = {force.As(standard.Newton()):F2} N");
Console.WriteLine($"Length compatible with time: {mile.IsCompatibleWithUnit(standard.Second())}");
