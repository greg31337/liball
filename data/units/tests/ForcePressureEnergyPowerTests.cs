using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class ForcePressureEnergyPowerTests
{
    [Test]
    public void ForceFactories_ConstructCommonForceUnits()
    {
        Force newtons = Force.FromNewtons(500.0d);
        Force kilonewtons = Force.FromKilonewtons(2.0d);

        Assert.That(newtons.GetUnit().GetId(), Is.EqualTo("newton"));
        Assert.That(newtons.GetUnit().GetDimension().EqualsDimension(new StandardUnits().Newton().GetDimension()), Is.True);
        Assert.That(kilonewtons.GetUnit().GetId(), Is.EqualTo("kilo_newton"));
        Assert.That(kilonewtons.Newtons(), Is.EqualTo(2000.0d));
    }

    [Test]
    public void ForceToUnitArithmeticAndBridge_ConvertAcrossForceUnits()
    {
        Force newtons = Force.FromNewtons(500.0d);
        Force kilonewton = Force.FromKilonewtons(1.0d);
        Force negativeKilonewtons = Force.FromKilonewtons(-3.0d);

        Force converted = kilonewton.ToUnit(new StandardUnits().Newton());
        Force sum = newtons.Add(kilonewton);
        Force difference = Force.FromKilonewtons(2.0d).Subtract(Force.FromNewtons(500.0d));
        Force scaled = negativeKilonewtons.Scale(2.0d);
        Force negated = negativeKilonewtons.Negate();
        Force absolute = negativeKilonewtons.AbsoluteValue();
        QuantityValue quantity = negativeKilonewtons.ToQuantityValue();

        Assert.That(converted.GetUnit().GetId(), Is.EqualTo("newton"));
        Assert.That(converted.GetValue(), Is.EqualTo(1000.0d));
        Assert.That(sum.GetUnit().GetId(), Is.EqualTo("newton"));
        Assert.That(sum.GetValue(), Is.EqualTo(1500.0d));
        Assert.That(difference.GetUnit().GetId(), Is.EqualTo("kilo_newton"));
        Assert.That(difference.GetValue(), Is.EqualTo(1.5d));
        Assert.That(scaled.GetValue(), Is.EqualTo(-6.0d));
        Assert.That(negated.GetValue(), Is.EqualTo(3.0d));
        Assert.That(absolute.GetValue(), Is.EqualTo(3.0d));
        Assert.That(negativeKilonewtons.Abs().GetValue(), Is.EqualTo(3.0d));
        Assert.That(quantity.GetValue(), Is.EqualTo(-3.0d));
        Assert.That(quantity.GetUnit().GetId(), Is.EqualTo("kilo_newton"));
    }

    [Test]
    public void Force_RejectsMissingOrNonForceInputs()
    {
        Force force = Force.FromNewtons(1.0d);

        Assert.Throws<System.ArgumentNullException>(() => new Force(1.0d, null!));
        Assert.Throws<System.ArgumentException>(() => new Force(1.0d, new StandardUnits().Meter()));
        Assert.Throws<System.ArgumentNullException>(() => force.Add(null!));
        Assert.Throws<System.ArgumentNullException>(() => force.Subtract(null!));
    }

    [Test]
    public void PressureFactories_ConstructCommonPressureUnits()
    {
        Pressure pascals = Pressure.FromPascals(1000.0d);
        Pressure kilopascals = Pressure.FromKilopascals(101.325d);
        Pressure psi = Pressure.FromPsi(1.0d);

        Assert.That(pascals.GetUnit().GetId(), Is.EqualTo("pascal"));
        Assert.That(pascals.Kilopascals(), Is.EqualTo(1.0d));
        Assert.That(kilopascals.Pascals(), Is.EqualTo(101325.0d));
        Assert.That(psi.Pascals(), Is.EqualTo(6894.757293168361d).Within(0.000000001d));
    }

    [Test]
    public void PressureToUnitArithmeticAndBridge_ConvertAcrossPressureUnits()
    {
        Pressure pascals = Pressure.FromPascals(500.0d);
        Pressure kilopascal = Pressure.FromKilopascals(1.0d);
        Pressure negativePsi = Pressure.FromPsi(-2.0d);

        Pressure converted = kilopascal.ToUnit(new StandardUnits().Psi());
        Pressure sum = pascals.Add(kilopascal);
        Pressure difference = Pressure.FromPsi(3.0d).Subtract(Pressure.FromPsi(1.0d));
        Pressure scaled = negativePsi.Scale(2.0d);
        Pressure negated = negativePsi.Negate();
        Pressure absolute = negativePsi.AbsoluteValue();
        QuantityValue quantity = negativePsi.ToQuantityValue();

        Assert.That(converted.GetUnit().GetId(), Is.EqualTo("psi"));
        Assert.That(converted.GetValue(), Is.EqualTo(0.14503773773020923d).Within(0.000000000001d));
        Assert.That(sum.GetUnit().GetId(), Is.EqualTo("pascal"));
        Assert.That(sum.GetValue(), Is.EqualTo(1500.0d));
        Assert.That(difference.GetUnit().GetId(), Is.EqualTo("psi"));
        Assert.That(difference.GetValue(), Is.EqualTo(2.0d).Within(0.000000000001d));
        Assert.That(scaled.GetValue(), Is.EqualTo(-4.0d));
        Assert.That(negated.GetValue(), Is.EqualTo(2.0d));
        Assert.That(absolute.GetValue(), Is.EqualTo(2.0d));
        Assert.That(negativePsi.Abs().GetValue(), Is.EqualTo(2.0d));
        Assert.That(quantity.GetValue(), Is.EqualTo(-2.0d));
        Assert.That(quantity.GetUnit().GetId(), Is.EqualTo("psi"));
    }

    [Test]
    public void Pressure_RejectsMissingOrNonPressureInputs()
    {
        Pressure pressure = Pressure.FromPascals(1.0d);

        Assert.Throws<System.ArgumentNullException>(() => new Pressure(1.0d, null!));
        Assert.Throws<System.ArgumentException>(() => new Pressure(1.0d, new StandardUnits().Newton()));
        Assert.Throws<System.ArgumentNullException>(() => pressure.Add(null!));
        Assert.Throws<System.ArgumentNullException>(() => pressure.Subtract(null!));
    }

    [Test]
    public void EnergyFactories_ConstructCommonEnergyUnits()
    {
        Energy joules = Energy.FromJoules(1000.0d);
        Energy kilojoules = Energy.FromKilojoules(2.0d);
        Energy kilowattHours = Energy.FromKilowattHours(1.0d);
        Energy btu = Energy.FromBritishThermalUnits(1.0d);
        Energy electronvolt = Energy.FromElectronvolts(1.0d);

        Assert.That(joules.GetUnit().GetId(), Is.EqualTo("joule"));
        Assert.That(joules.Kilojoules(), Is.EqualTo(1.0d));
        Assert.That(kilojoules.Joules(), Is.EqualTo(2000.0d));
        Assert.That(kilowattHours.Joules(), Is.EqualTo(3600000.0d));
        Assert.That(btu.Joules(), Is.EqualTo(1055.05585262d).Within(0.000000001d));
        Assert.That(electronvolt.Joules(), Is.EqualTo(1.602176634E-19d).Within(0.000000000000000000000000000001d));
    }

    [Test]
    public void EnergyToUnitArithmeticAndBridge_ConvertAcrossEnergyUnits()
    {
        Energy joules = Energy.FromJoules(500.0d);
        Energy kilojoule = Energy.FromKilojoules(1.0d);
        Energy negativeBtu = Energy.FromBritishThermalUnits(-2.0d);

        Energy converted = Energy.FromKilowattHours(1.0d).ToUnit(new StandardUnits().Joule());
        Energy sum = joules.Add(kilojoule);
        Energy difference = Energy.FromBritishThermalUnits(3.0d).Subtract(Energy.FromBritishThermalUnits(1.0d));
        Energy scaled = negativeBtu.Scale(2.0d);
        Energy negated = negativeBtu.Negate();
        Energy absolute = negativeBtu.AbsoluteValue();
        QuantityValue quantity = negativeBtu.ToQuantityValue();

        Assert.That(converted.GetUnit().GetId(), Is.EqualTo("joule"));
        Assert.That(converted.GetValue(), Is.EqualTo(3600000.0d));
        Assert.That(sum.GetUnit().GetId(), Is.EqualTo("joule"));
        Assert.That(sum.GetValue(), Is.EqualTo(1500.0d));
        Assert.That(difference.GetUnit().GetId(), Is.EqualTo("british_thermal_unit"));
        Assert.That(difference.GetValue(), Is.EqualTo(2.0d).Within(0.000000000001d));
        Assert.That(scaled.GetValue(), Is.EqualTo(-4.0d));
        Assert.That(negated.GetValue(), Is.EqualTo(2.0d));
        Assert.That(absolute.GetValue(), Is.EqualTo(2.0d));
        Assert.That(negativeBtu.Abs().GetValue(), Is.EqualTo(2.0d));
        Assert.That(quantity.GetValue(), Is.EqualTo(-2.0d));
        Assert.That(quantity.GetUnit().GetId(), Is.EqualTo("british_thermal_unit"));
    }

    [Test]
    public void Energy_RejectsMissingOrNonEnergyInputs()
    {
        Energy energy = Energy.FromJoules(1.0d);

        Assert.Throws<System.ArgumentNullException>(() => new Energy(1.0d, null!));
        Assert.Throws<System.ArgumentException>(() => new Energy(1.0d, new StandardUnits().Watt()));
        Assert.Throws<System.ArgumentNullException>(() => energy.Add(null!));
        Assert.Throws<System.ArgumentNullException>(() => energy.Subtract(null!));
    }

    [Test]
    public void PowerFactories_ConstructCommonPowerUnits()
    {
        Power watts = Power.FromWatts(1000.0d);
        Power kilowatts = Power.FromKilowatts(2.0d);
        Power horsepower = Power.FromHorsepower(1.0d);

        Assert.That(watts.GetUnit().GetId(), Is.EqualTo("watt"));
        Assert.That(watts.Kilowatts(), Is.EqualTo(1.0d));
        Assert.That(kilowatts.Watts(), Is.EqualTo(2000.0d));
        Assert.That(horsepower.Watts(), Is.EqualTo(745.6998715822702d).Within(0.000000001d));
    }

    [Test]
    public void PowerToUnitArithmeticAndBridge_ConvertAcrossPowerUnits()
    {
        Power watts = Power.FromWatts(500.0d);
        Power kilowatt = Power.FromKilowatts(1.0d);
        Power negativeHorsepower = Power.FromHorsepower(-2.0d);

        Power converted = kilowatt.ToUnit(new StandardUnits().Horsepower());
        Power sum = watts.Add(kilowatt);
        Power difference = Power.FromHorsepower(3.0d).Subtract(Power.FromHorsepower(1.0d));
        Power scaled = negativeHorsepower.Scale(2.0d);
        Power negated = negativeHorsepower.Negate();
        Power absolute = negativeHorsepower.AbsoluteValue();
        QuantityValue quantity = negativeHorsepower.ToQuantityValue();

        Assert.That(converted.GetUnit().GetId(), Is.EqualTo("horsepower"));
        Assert.That(converted.GetValue(), Is.EqualTo(1.341022089595028d).Within(0.000000000001d));
        Assert.That(sum.GetUnit().GetId(), Is.EqualTo("watt"));
        Assert.That(sum.GetValue(), Is.EqualTo(1500.0d));
        Assert.That(difference.GetUnit().GetId(), Is.EqualTo("horsepower"));
        Assert.That(difference.GetValue(), Is.EqualTo(2.0d).Within(0.000000000001d));
        Assert.That(scaled.GetValue(), Is.EqualTo(-4.0d));
        Assert.That(negated.GetValue(), Is.EqualTo(2.0d));
        Assert.That(absolute.GetValue(), Is.EqualTo(2.0d));
        Assert.That(negativeHorsepower.Abs().GetValue(), Is.EqualTo(2.0d));
        Assert.That(quantity.GetValue(), Is.EqualTo(-2.0d));
        Assert.That(quantity.GetUnit().GetId(), Is.EqualTo("horsepower"));
    }

    [Test]
    public void Power_RejectsMissingOrNonPowerInputs()
    {
        Power power = Power.FromWatts(1.0d);

        Assert.Throws<System.ArgumentNullException>(() => new Power(1.0d, null!));
        Assert.Throws<System.ArgumentException>(() => new Power(1.0d, new StandardUnits().Joule()));
        Assert.Throws<System.ArgumentNullException>(() => power.Add(null!));
        Assert.Throws<System.ArgumentNullException>(() => power.Subtract(null!));
    }
}
