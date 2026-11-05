using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class PhysicalConstantSourceMetadataTests
{
    [Test]
    public void SiDefiningConstants_IdentifyCurrentBrochureEditionAndZeroUncertainty()
    {
        PhysicalConstants catalog = new PhysicalConstants();
        PhysicalConstantDefinition[] constants = new PhysicalConstantDefinition[]
        {
            catalog.Caesium133HyperfineTransitionFrequency(),
            catalog.SpeedOfLight(),
            catalog.PlanckConstant(),
            catalog.ElementaryCharge(),
            catalog.BoltzmannConstant(),
            catalog.AvogadroConstant(),
            catalog.LuminousEfficacy()
        };

        int index = 0;
        while (index < constants.Length)
        {
            Assert.That(constants[index].IsExact(), Is.True);
            Assert.That(constants[index].GetSourceLabel(), Is.EqualTo("BIPM SI Brochure"));
            Assert.That(constants[index].GetAdjustmentOrEdition(), Is.EqualTo("9th edition, version 4.01 (2026)"));
            Assert.That(constants[index].GetStandardUncertainty(), Is.EqualTo(0.0d));
            index = index + 1;
        }
    }

    [Test]
    public void GravitationalConstant_UsesCodata2022ValueAndUncertainty()
    {
        PhysicalConstantDefinition constant = new PhysicalConstants().GravitationalConstant();

        Assert.That(constant.IsExact(), Is.False);
        Assert.That(constant.GetQuantity().GetValue(), Is.EqualTo(6.67430E-11d));
        Assert.That(constant.GetStandardUncertainty(), Is.EqualTo(1.5E-15d));
        Assert.That(constant.GetSourceLabel(), Is.EqualTo("NIST Fundamental Physical Constants"));
        Assert.That(constant.GetAdjustmentOrEdition(), Is.EqualTo("2022 CODATA adjustment"));
    }

    [Test]
    public void StandardRegistry_PreservesSourceAndUncertaintyMetadata()
    {
        UnitRegistry registry = new StandardRegistry().Create();

        PhysicalConstantDefinition boltzmann = registry.FindPhysicalConstant("boltzmann_constant").GetPhysicalConstant();
        PhysicalConstantDefinition gravitational = registry.FindPhysicalConstant("G").GetPhysicalConstant();

        Assert.That(boltzmann.GetSourceLabel(), Is.EqualTo("BIPM SI Brochure"));
        Assert.That(boltzmann.GetStandardUncertainty(), Is.EqualTo(0.0d));
        Assert.That(gravitational.GetAdjustmentOrEdition(), Is.EqualTo("2022 CODATA adjustment"));
        Assert.That(gravitational.GetStandardUncertainty(), Is.EqualTo(1.5E-15d));
    }
}
