using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class AromaticityConfigurationTests
{
    [Test]
    public void Dekekulize_ConvertsAlternatingBenzeneOnIndependentCopy()
    {
        Molecule source = BuildAlternatingCarbonRing(6);
        IAromaticityModel aromaticity = new AromaticityModel();

        AromaticityPerceptionResult result = aromaticity.Dekekulize(source, null);

        Assert.That(result.Success, Is.True);
        Assert.That(result.UsedFallback, Is.False);
        Assert.That(result.Model, Is.EqualTo(AromaticityModelKind.DaylightLike));
        Assert.That(result.AromaticAtomIds, Has.Count.EqualTo(6));
        Assert.That(result.AromaticBondIds, Has.Count.EqualTo(6));
        Assert.That(result.RejectedRingIds, Is.Empty);
        Assert.That(result.Molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Aromatic));
        Assert.That(result.Molecule.Bonds[0].IsAromatic, Is.True);
        Assert.That(source.Bonds[0].Order, Is.EqualTo(BondOrder.Double));
        Assert.That(source.Bonds[0].IsAromatic, Is.False);
    }

    [Test]
    public void Dekekulize_UsesSelectedModelAndReportsDeterministicFallback()
    {
        Molecule source = BuildAlternatingFuran();
        AromaticityOptions daylightOptions = new AromaticityOptions();
        AromaticityOptions mdlOptions = new AromaticityOptions();
        mdlOptions.Model = AromaticityModelKind.MdlLike;
        IAromaticityModel aromaticity = new AromaticityModel();

        AromaticityPerceptionResult daylight = aromaticity.Dekekulize(source, daylightOptions);
        AromaticityPerceptionResult mdl = aromaticity.Dekekulize(source, mdlOptions);

        Assert.That(daylight.Success, Is.True);
        Assert.That(daylight.AromaticBondIds, Has.Count.EqualTo(5));
        Assert.That(daylight.UsedFallback, Is.False);
        Assert.That(mdl.Success, Is.True);
        Assert.That(mdl.Model, Is.EqualTo(AromaticityModelKind.MdlLike));
        Assert.That(mdl.AromaticBondIds, Is.Empty);
        Assert.That(mdl.RejectedRingIds, Is.EqualTo(new int[] { 0 }));
        Assert.That(mdl.UsedFallback, Is.True);
        Assert.That(mdl.Report.Errors, Has.Count.EqualTo(1));
        Assert.That(mdl.Report.Errors[0].Code, Is.EqualTo("AROMATICITY_REJECTED"));
        Assert.That(mdl.Report.Errors[0].Severity, Is.EqualTo(ErrorSeverity.Warning));
        Assert.That(mdl.Molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Single));
        Assert.That(mdl.Molecule.Bonds[1].Order, Is.EqualTo(BondOrder.Double));
    }

    [Test]
    public void Dekekulize_FailsRejectedConjugatedRingWhenFallbackDisabled()
    {
        Molecule source = BuildAlternatingCarbonRing(4);
        AromaticityOptions options = new AromaticityOptions();
        options.UseDeterministicFallback = false;
        IAromaticityModel aromaticity = new AromaticityModel();

        AromaticityPerceptionResult result = aromaticity.Dekekulize(source, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.UsedFallback, Is.False);
        Assert.That(result.RejectedRingIds, Is.EqualTo(new int[] { 0 }));
        Assert.That(result.Report.Errors, Has.Count.EqualTo(1));
        Assert.That(result.Report.Errors[0].Code, Is.EqualTo("AROMATICITY_MODEL_REJECTED"));
        Assert.That(result.Report.Errors[0].BondId, Is.EqualTo(0));
        Assert.That(result.Molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Double));
        Assert.That(source.Bonds[0].Order, Is.EqualTo(BondOrder.Double));
    }

    [Test]
    public void KekulizeWithOptions_ReportsFailureWithoutChangingRejectedRing()
    {
        Molecule source = BuildAromaticCarbonRing(5);
        KekulizationOptions options = new KekulizationOptions();
        options.UseDeterministicFallback = false;
        IKekulizer kekulizer = new Kekulizer();

        KekulizationResult result = kekulizer.KekulizeWithOptions(source, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.UsedFallback, Is.False);
        Assert.That(result.FailedRingIds, Is.EqualTo(new int[] { 0 }));
        Assert.That(result.KekulizedBondIds, Is.Empty);
        Assert.That(result.Report.Errors, Has.Count.EqualTo(1));
        Assert.That(result.Report.Errors[0].Code, Is.EqualTo("KEKULIZATION_FAILED"));
        Assert.That(result.Molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Aromatic));
        Assert.That(result.Molecule.Bonds[0].IsAromatic, Is.True);
        Assert.That(source.Bonds[0].Order, Is.EqualTo(BondOrder.Aromatic));
    }

    [Test]
    public void KekulizeWithOptions_ValidatesAgainstSelectedAromaticityModel()
    {
        Molecule source = BuildAromaticFuran();
        KekulizationOptions daylightOptions = new KekulizationOptions();
        KekulizationOptions mdlOptions = new KekulizationOptions();
        mdlOptions.Model = AromaticityModelKind.MdlLike;
        mdlOptions.UseDeterministicFallback = false;
        IKekulizer kekulizer = new Kekulizer();

        KekulizationResult daylight = kekulizer.KekulizeWithOptions(source, daylightOptions);
        KekulizationResult mdl = kekulizer.KekulizeWithOptions(source, mdlOptions);

        Assert.That(daylight.Success, Is.True);
        Assert.That(daylight.UsedFallback, Is.False);
        Assert.That(daylight.KekulizedBondIds, Has.Count.EqualTo(5));
        Assert.That(mdl.Success, Is.False);
        Assert.That(mdl.Model, Is.EqualTo(AromaticityModelKind.MdlLike));
        Assert.That(mdl.FailedRingIds, Is.EqualTo(new int[] { 0 }));
        Assert.That(mdl.Molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Aromatic));
    }

    [Test]
    public void DekekulizeAndKekulize_ProvideDeterministicRoundTrip()
    {
        Molecule source = BuildAlternatingCarbonRing(6);
        IAromaticityModel aromaticity = new AromaticityModel();
        IKekulizer kekulizer = new Kekulizer();

        AromaticityPerceptionResult aromatic = aromaticity.Dekekulize(source, null);
        KekulizationResult kekule = kekulizer.KekulizeWithOptions(aromatic.Molecule, null);

        Assert.That(aromatic.Success, Is.True);
        Assert.That(kekule.Success, Is.True);
        Assert.That(kekule.UsedFallback, Is.False);
        AssertBondOrders(kekule.Molecule, new BondOrder[] {
            BondOrder.Double,
            BondOrder.Single,
            BondOrder.Double,
            BondOrder.Single,
            BondOrder.Double,
            BondOrder.Single
        });
    }

    [Test]
    public void ConfiguredOperations_ReportNullInputAndUnsupportedModel()
    {
        IAromaticityModel aromaticity = new AromaticityModel();
        IKekulizer kekulizer = new Kekulizer();
        AromaticityOptions invalidAromaticity = new AromaticityOptions();
        invalidAromaticity.Model = (AromaticityModelKind)99;
        KekulizationOptions invalidKekulization = new KekulizationOptions();
        invalidKekulization.Model = (AromaticityModelKind)99;

        AromaticityPerceptionResult nullResult = aromaticity.Dekekulize(null, null);
        AromaticityPerceptionResult invalidResult = aromaticity.PerceiveWithOptions(BuildAlternatingCarbonRing(6), invalidAromaticity);
        KekulizationResult invalidKekule = kekulizer.KekulizeWithOptions(BuildAromaticCarbonRing(6), invalidKekulization);

        Assert.That(nullResult.Success, Is.False);
        Assert.That(nullResult.Report.Errors[0].Code, Is.EqualTo("NULL_MOLECULE"));
        Assert.That(invalidResult.Success, Is.False);
        Assert.That(invalidResult.Report.Errors[0].Code, Is.EqualTo("AROMATICITY_INVALID_MODEL"));
        Assert.That(invalidKekule.Success, Is.False);
        Assert.That(invalidKekule.Report.Errors[0].Code, Is.EqualTo("KEKULIZATION_INVALID_MODEL"));
    }

    [Test]
    public void AromaticityAccessorAndInPlaceOperations_ReportAssignedAndInvalidStates()
    {
        IAromaticityModel aromaticity = new AromaticityModel();
        IKekulizer kekulizer = new Kekulizer();
        Molecule aromatic = BuildAromaticCarbonRing(6);
        Molecule nonaromatic = BuildAlternatingCarbonRing(6);
        AromaticityOptions invalidOptions = new AromaticityOptions();
        invalidOptions.Model = (AromaticityModelKind)99;

        Assert.That(aromaticity.GetAromaticity(aromatic, 0), Is.EqualTo(Aromaticity.Aromatic));
        Assert.That(aromaticity.GetAromaticity(nonaromatic, 0), Is.EqualTo(Aromaticity.NonAromatic));
        Assert.That(aromaticity.GetAromaticity(null, 0), Is.EqualTo(Aromaticity.Unknown));
        Assert.That(aromaticity.GetAromaticity(aromatic, 99), Is.EqualTo(Aromaticity.Unknown));

        AromaticityPerceptionResult nullAromaticity = aromaticity.DekekulizeInPlace(null, null);
        AromaticityPerceptionResult invalidAromaticity = aromaticity.DekekulizeInPlace(nonaromatic, invalidOptions);
        KekulizationResult nullKekulization = kekulizer.KekulizeInPlace(null);

        Assert.That(nullAromaticity.Report.Errors[0].Code, Is.EqualTo("NULL_MOLECULE"));
        Assert.That(invalidAromaticity.Report.Errors[0].Code, Is.EqualTo("AROMATICITY_INVALID_MODEL"));
        Assert.That(nullKekulization.Report.Errors[0].Code, Is.EqualTo("NULL_MOLECULE"));
    }

    static Molecule BuildAlternatingCarbonRing(int size)
    {
        MoleculeBuilder builder = BuildCarbonAtoms(size, false);
        AddAlternatingRingBonds(builder, size);
        return builder.Build();
    }

    static Molecule BuildAromaticCarbonRing(int size)
    {
        MoleculeBuilder builder = BuildCarbonAtoms(size, true);
        AddAromaticRingBonds(builder, size);
        return builder.Build();
    }

    static Molecule BuildAlternatingFuran()
    {
        MoleculeBuilder builder = BuildFuranAtoms(false);
        builder.AddBond(0, 1, new BondSpec());
        builder.AddBond(1, 2, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(2, 3, new BondSpec());
        builder.AddBond(3, 4, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(4, 0, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildAromaticFuran()
    {
        MoleculeBuilder builder = BuildFuranAtoms(true);
        AddAromaticRingBonds(builder, 5);
        return builder.Build();
    }

    static MoleculeBuilder BuildCarbonAtoms(int count, bool aromatic)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int index = 0;
        while (index < count)
        {
            builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, aromatic, "", "C"));
            index = index + 1;
        }

        return builder;
    }

    static MoleculeBuilder BuildFuranAtoms(bool aromatic)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, aromatic, "", "O"));
        int index = 1;
        while (index < 5)
        {
            builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, aromatic, "", "C"));
            index = index + 1;
        }

        return builder;
    }

    static void AddAlternatingRingBonds(MoleculeBuilder builder, int size)
    {
        int index = 0;
        while (index < size)
        {
            int next = index + 1;
            if (next == size)
            {
                next = 0;
            }

            BondOrder order = BondOrder.Single;
            if (index % 2 == 0)
            {
                order = BondOrder.Double;
            }

            builder.AddBond(index, next, BondSpec.Create(order, false, BondStereo.None, BondDirection.None));
            index = index + 1;
        }
    }

    static void AddAromaticRingBonds(MoleculeBuilder builder, int size)
    {
        int index = 0;
        while (index < size)
        {
            int next = index + 1;
            if (next == size)
            {
                next = 0;
            }

            builder.AddBond(index, next, BondSpec.Create(BondOrder.Aromatic, true, BondStereo.None, BondDirection.None));
            index = index + 1;
        }
    }

    static void AssertBondOrders(Molecule molecule, BondOrder[] orders)
    {
        int index = 0;
        while (index < orders.Length)
        {
            Assert.That(molecule.Bonds[index].Order, Is.EqualTo(orders[index]));
            index = index + 1;
        }
    }
}
