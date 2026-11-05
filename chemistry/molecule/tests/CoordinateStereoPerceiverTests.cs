using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class CoordinateStereoPerceiverTests
{
    [Test]
    public void PerceiveFromCoordinates_CreatesClockwiseTetrahedralRecordFrom3DCoordinates()
    {
        Molecule molecule = BuildTetrahedral3D(false);
        StereoPerceiver perceiver = new StereoPerceiver();

        StereoPerceptionResult result = perceiver.PerceiveFromCoordinates(molecule);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Report.GetCount(), Is.EqualTo(0));
        Assert.That(result.Records.Count, Is.EqualTo(1));
        Assert.That(molecule.StereoRecords[0].IsTetrahedral(), Is.True);
        Assert.That(molecule.StereoRecords[0].CenterAtomId, Is.EqualTo(0));
        Assert.That(molecule.StereoRecords[0].Chirality, Is.EqualTo(Chirality.Clockwise));
        Assert.That(molecule.StereoRecords[0].LigandAtomIds, Is.EqualTo(new int[] { 1, 2, 3, 4 }));
        Assert.That(molecule.StereoRecords[0].Source, Is.EqualTo("coordinates3d"));

        StereoValidator validator = new StereoValidator();
        Assert.That(validator.Validate(molecule).GetCount(), Is.EqualTo(0));
    }

    [Test]
    public void PerceiveFromCoordinates_CreatesCounterClockwiseRecordFromMirrored3DCoordinates()
    {
        Molecule molecule = BuildTetrahedral3D(true);
        StereoPerceiver perceiver = new StereoPerceiver();

        StereoPerceptionResult result = perceiver.PerceiveFromCoordinates(molecule);

        Assert.That(result.Success, Is.True);
        Assert.That(molecule.StereoRecords[0].Chirality, Is.EqualTo(Chirality.CounterClockwise));
    }

    [Test]
    public void PerceiveFromCoordinates_CreatesCisDoubleBondRecordFrom2DCoordinates()
    {
        Molecule molecule = BuildCoordinateAlkene(true);
        StereoPerceiver perceiver = new StereoPerceiver();

        StereoPerceptionResult result = perceiver.PerceiveFromCoordinates(molecule);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Records.Count, Is.EqualTo(1));
        Assert.That(molecule.StereoRecords[0].IsDoubleBond(), Is.True);
        Assert.That(molecule.StereoRecords[0].StereoBondId, Is.EqualTo(0));
        Assert.That(molecule.StereoRecords[0].BeginLigandAtomId, Is.EqualTo(2));
        Assert.That(molecule.StereoRecords[0].EndLigandAtomId, Is.EqualTo(3));
        Assert.That(molecule.StereoRecords[0].Configuration, Is.EqualTo(DoubleBondConfiguration.Cis));
        Assert.That(molecule.StereoRecords[0].Source, Is.EqualTo("coordinates2d"));

        StereoValidator validator = new StereoValidator();
        Assert.That(validator.Validate(molecule).GetCount(), Is.EqualTo(0));
    }

    [Test]
    public void PerceiveFromCoordinates_CreatesTransDoubleBondRecordFrom2DCoordinates()
    {
        Molecule molecule = BuildCoordinateAlkene(false);
        StereoPerceiver perceiver = new StereoPerceiver();

        StereoPerceptionResult result = perceiver.PerceiveFromCoordinates(molecule);

        Assert.That(result.Success, Is.True);
        Assert.That(molecule.StereoRecords[0].Configuration, Is.EqualTo(DoubleBondConfiguration.Trans));
    }

    [Test]
    public void PerceiveFromCoordinates_SkipsPlanarTetrahedralAndMissingCoordinates()
    {
        Molecule tetrahedral = BuildTetrahedral3D(false);
        SetCoordinate(tetrahedral, 3, 0.0, -1.0, 0.0);
        SetCoordinate(tetrahedral, 4, -1.0, 0.0, 0.0);
        Molecule alkene = BuildCoordinateAlkene(true);
        alkene.Atoms[3].HasCoordinates = false;
        StereoPerceiver perceiver = new StereoPerceiver();

        StereoPerceptionResult tetrahedralResult = perceiver.PerceiveFromCoordinates(tetrahedral);
        StereoPerceptionResult alkeneResult = perceiver.PerceiveFromCoordinates(alkene);

        Assert.That(tetrahedralResult.Records, Is.Empty);
        Assert.That(alkeneResult.Records, Is.Empty);
    }

    [Test]
    public void PerceiveFromCoordinates_DoesNotDuplicateExistingStereoRecords()
    {
        Molecule molecule = BuildCoordinateAlkene(true);
        StereoRecord existing = new StereoRecord();
        existing.SetDoubleBond(0, 2, 3, DoubleBondConfiguration.Cis);
        molecule.StereoRecords.Add(existing);
        StereoPerceiver perceiver = new StereoPerceiver();

        StereoPerceptionResult result = perceiver.PerceiveFromCoordinates(molecule);

        Assert.That(result.Records, Is.Empty);
        Assert.That(molecule.StereoRecords.Count, Is.EqualTo(1));
    }

    [Test]
    public void PerceiveFromCoordinates_ReturnsErrorForNullMolecule()
    {
        StereoPerceiver perceiver = new StereoPerceiver();

        StereoPerceptionResult result = perceiver.PerceiveFromCoordinates(null);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("STEREO_NULL_MOLECULE"));
    }

    static Molecule BuildTetrahedral3D(bool mirrored)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int center = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int atom1 = builder.AddAtom(new AtomSpec(9, 0, 0, 0, 0, 0, 0, false, "", "F"));
        int atom2 = builder.AddAtom(new AtomSpec(17, 0, 0, 0, 0, 0, 0, false, "", "Cl"));
        int atom3 = builder.AddAtom(new AtomSpec(35, 0, 0, 0, 0, 0, 0, false, "", "Br"));
        int atom4 = builder.AddAtom(new AtomSpec(53, 0, 0, 0, 0, 0, 0, false, "", "I"));
        builder.AddBond(center, atom1, new BondSpec());
        builder.AddBond(center, atom2, new BondSpec());
        builder.AddBond(center, atom3, new BondSpec());
        builder.AddBond(center, atom4, new BondSpec());
        Molecule molecule = builder.Build();
        SetCoordinate(molecule, center, 0.0, 0.0, 0.0);
        SetCoordinate(molecule, atom1, 1.0, 0.0, 0.0);
        SetCoordinate(molecule, atom2, 0.0, 1.0, 0.0);
        SetCoordinate(molecule, atom3, 0.0, 0.0, 1.0);
        SetCoordinate(molecule, atom4, -1.0, -1.0, -1.0);
        if (mirrored)
        {
            SetCoordinate(molecule, atom2, 0.0, -1.0, 0.0);
        }

        return molecule;
    }

    static Molecule BuildCoordinateAlkene(bool cis)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int left = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int right = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int leftLigand = builder.AddAtom(new AtomSpec(17, 0, 0, 0, 0, 0, 0, false, "", "Cl"));
        int rightLigand = builder.AddAtom(new AtomSpec(35, 0, 0, 0, 0, 0, 0, false, "", "Br"));
        builder.AddBond(left, right, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(left, leftLigand, new BondSpec());
        builder.AddBond(right, rightLigand, new BondSpec());
        Molecule molecule = builder.Build();
        SetCoordinate(molecule, left, 0.0, 0.0, 0.0);
        SetCoordinate(molecule, right, 1.0, 0.0, 0.0);
        SetCoordinate(molecule, leftLigand, 0.0, 1.0, 0.0);
        if (cis)
        {
            SetCoordinate(molecule, rightLigand, 1.0, 1.0, 0.0);
        }
        else
        {
            SetCoordinate(molecule, rightLigand, 1.0, -1.0, 0.0);
        }

        return molecule;
    }

    static void SetCoordinate(Molecule molecule, int atomId, double x, double y, double z)
    {
        molecule.Atoms[atomId].HasCoordinates = true;
        molecule.Atoms[atomId].X = x;
        molecule.Atoms[atomId].Y = y;
        molecule.Atoms[atomId].Z = z;
    }
}
