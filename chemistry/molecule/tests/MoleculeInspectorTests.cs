using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class MoleculeInspectorTests
{
    [Test]
    public void CountsLookupsAndNeighbors_ReturnIndependentData()
    {
        Molecule molecule = BuildGraph();
        IMoleculeInspector inspector = new MoleculeInspector();

        Assert.That(inspector.GetAtomCount(molecule), Is.EqualTo(4));
        Assert.That(inspector.GetBondCount(molecule), Is.EqualTo(3));

        Atom atom = inspector.GetAtom(molecule, 0);
        Bond bond = inspector.GetBond(molecule, 0);
        Bond reverseLookup = inspector.GetBondBetween(molecule, 1, 0);
        List<int> neighborAtoms = inspector.GetNeighborAtomIds(molecule, 0);
        List<int> neighborBonds = inspector.GetNeighborBondIds(molecule, 0);

        Assert.That(atom.AtomicNumber, Is.EqualTo(6));
        Assert.That(bond.BeginAtomId, Is.EqualTo(0));
        Assert.That(reverseLookup.Id, Is.EqualTo(0));
        Assert.That(neighborAtoms, Is.EqualTo(new int[] { 1, 2 }));
        Assert.That(neighborBonds, Is.EqualTo(new int[] { 0, 2 }));

        atom.AtomicNumber = 99;
        bond.Order = BondOrder.Triple;
        neighborAtoms.Add(99);

        Assert.That(molecule.Atoms[0].AtomicNumber, Is.EqualTo(6));
        Assert.That(molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Single));
        Assert.That(inspector.GetNeighborAtomIds(molecule, 0), Is.EqualTo(new int[] { 1, 2 }));
    }

    [Test]
    public void TopologyOperations_ReportDegreeValenceRingsAndComponents()
    {
        Molecule molecule = BuildGraph();
        IMoleculeInspector inspector = new MoleculeInspector();

        ValenceResult valence = inspector.GetValence(molecule, 0);

        Assert.That(inspector.GetDegree(molecule, 0), Is.EqualTo(2));
        Assert.That(valence.BondValence, Is.EqualTo(2));
        Assert.That(valence.HydrogenValence, Is.EqualTo(2));
        Assert.That(valence.TotalValence, Is.EqualTo(4));
        Assert.That(valence.IsAllowed, Is.True);
        Assert.That(inspector.AtomIsInRing(molecule, 0), Is.True);
        Assert.That(inspector.BondIsInRing(molecule, 1), Is.True);
        Assert.That(inspector.GetAtomRingCount(molecule, 0), Is.EqualTo(1));
        Assert.That(inspector.GetBondRingCount(molecule, 1), Is.EqualTo(1));

        Assert.That(inspector.GetComponentCount(molecule), Is.EqualTo(2));
        Assert.That(inspector.GetComponentId(molecule, 0), Is.EqualTo(0));
        Assert.That(inspector.GetComponentId(molecule, 3), Is.EqualTo(1));
        Assert.That(inspector.GetBondComponentId(molecule, 2), Is.EqualTo(0));
        Assert.That(inspector.GetComponentAtomIds(molecule, 0), Is.EqualTo(new int[] { 0, 1, 2 }));
        Assert.That(inspector.GetComponentBondIds(molecule, 0), Is.EqualTo(new int[] { 0, 2, 1 }));
        Assert.That(inspector.GetComponentAtomIds(molecule, 1), Is.EqualTo(new int[] { 3 }));
    }

    [Test]
    public void AssociatedDataOperations_ReturnDeepCopies()
    {
        Molecule molecule = BuildGraph();
        Conformer conformer = new Conformer(4);
        conformer.Id = 0;
        conformer.Name = "lowest";
        conformer.Coordinates[0] = new Coordinate3D(1.0, 2.0, 3.0);
        molecule.Conformers.Add(conformer);

        StereoRecord stereo = new StereoRecord();
        stereo.Id = 0;
        stereo.Kind = "tetrahedral";
        stereo.CenterAtomId = 0;
        stereo.LigandAtomIds.Add(1);
        molecule.StereoRecords.Add(stereo);

        molecule.Metadata.Names.Add("cyclopropane");
        molecule.Metadata.Comments.Add("copy test");
        molecule.Metadata.SdfFields.Add(new MoleculeMetadataEntry("ID", "42"));

        IMoleculeInspector inspector = new MoleculeInspector();
        Conformer conformerCopy = inspector.GetConformer(molecule, 0);
        List<Conformer> conformerCopies = inspector.GetConformers(molecule);
        StereoRecord stereoCopy = inspector.GetStereoRecord(molecule, 0);
        List<StereoRecord> stereoCopies = inspector.GetStereoRecords(molecule);
        MoleculeMetadata metadataCopy = inspector.GetMetadata(molecule);

        Assert.That(inspector.GetConformerCount(molecule), Is.EqualTo(1));
        Assert.That(inspector.GetStereoRecordCount(molecule), Is.EqualTo(1));
        Assert.That(conformerCopy.Coordinates[0].X, Is.EqualTo(1.0));
        Assert.That(stereoCopy.LigandAtomIds, Is.EqualTo(new int[] { 1 }));
        Assert.That(metadataCopy.Names, Is.EqualTo(new string[] { "cyclopropane" }));

        conformerCopy.Coordinates[0].X = 99.0;
        conformerCopies[0].Name = "changed";
        stereoCopy.LigandAtomIds.Add(2);
        stereoCopies[0].Kind = "changed";
        metadataCopy.Names[0] = "changed";
        metadataCopy.SdfFields[0].Value = "changed";

        Assert.That(molecule.Conformers[0].Coordinates[0].X, Is.EqualTo(1.0));
        Assert.That(molecule.Conformers[0].Name, Is.EqualTo("lowest"));
        Assert.That(molecule.StereoRecords[0].LigandAtomIds, Is.EqualTo(new int[] { 1 }));
        Assert.That(molecule.StereoRecords[0].Kind, Is.EqualTo("tetrahedral"));
        Assert.That(molecule.Metadata.Names[0], Is.EqualTo("cyclopropane"));
        Assert.That(molecule.Metadata.SdfFields[0].Value, Is.EqualTo("42"));
    }

    [Test]
    public void NullAndInvalidIdentifiers_ReturnDocumentedEmptyValues()
    {
        IMoleculeInspector inspector = new MoleculeInspector();
        Molecule molecule = BuildGraph();

        Assert.That(inspector.GetAtomCount(null), Is.EqualTo(0));
        Assert.That(inspector.GetBondCount(null), Is.EqualTo(0));
        Assert.That(inspector.GetAtom(null, 0).Id, Is.EqualTo(-1));
        Assert.That(inspector.GetBond(null, 0).Id, Is.EqualTo(-1));
        Assert.That(inspector.GetBondBetween(null, 0, 1).Id, Is.EqualTo(-1));
        Assert.That(inspector.GetAtom(molecule, 99).Id, Is.EqualTo(-1));
        Assert.That(inspector.GetBond(molecule, 99).Id, Is.EqualTo(-1));
        Assert.That(inspector.GetBondBetween(molecule, 0, 99).Id, Is.EqualTo(-1));
        Assert.That(inspector.GetNeighborAtomIds(null, 0), Is.Empty);
        Assert.That(inspector.GetNeighborBondIds(null, 0), Is.Empty);
        Assert.That(inspector.GetNeighborBondIds(molecule, -1), Is.Empty);
        Assert.That(inspector.GetDegree(null, 0), Is.EqualTo(0));
        Assert.That(inspector.GetDegree(molecule, 99), Is.EqualTo(0));
        Assert.That(inspector.GetValence(null, 0).AtomId, Is.EqualTo(-1));
        Assert.That(inspector.AtomIsInRing(null, 0), Is.False);
        Assert.That(inspector.BondIsInRing(null, 0), Is.False);
        Assert.That(inspector.GetAtomRingCount(molecule, 99), Is.EqualTo(0));
        Assert.That(inspector.GetBondRingCount(molecule, 99), Is.EqualTo(0));
        Assert.That(inspector.GetComponentCount(null), Is.EqualTo(0));
        Assert.That(inspector.GetComponentId(molecule, 99), Is.EqualTo(-1));
        Assert.That(inspector.GetBondComponentId(molecule, 99), Is.EqualTo(-1));
        Assert.That(inspector.GetComponentAtomIds(molecule, 99), Is.Empty);
        Assert.That(inspector.GetComponentBondIds(null, 0), Is.Empty);
        Assert.That(inspector.GetConformerCount(null), Is.EqualTo(0));
        Assert.That(inspector.GetConformer(null, 0).Id, Is.EqualTo(-1));
        Assert.That(inspector.GetConformers(null), Is.Empty);
        Assert.That(inspector.GetStereoRecordCount(null), Is.EqualTo(0));
        Assert.That(inspector.GetStereoRecord(molecule, 99).Id, Is.EqualTo(-1));
        Assert.That(inspector.GetStereoRecords(null), Is.Empty);
        Assert.That(inspector.GetMetadata(null).Names, Is.Empty);
    }

    [Test]
    public void BondComponentLookup_RejectsMalformedBondEndpoints()
    {
        Molecule molecule = BuildGraph();
        molecule.Bonds[0].EndAtomId = 99;
        IMoleculeInspector inspector = new MoleculeInspector();

        Assert.That(inspector.GetBondComponentId(molecule, 0), Is.EqualTo(-1));
    }

    static Molecule BuildGraph()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon0 = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C0"));
        int carbon1 = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C1"));
        int carbon2 = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C2"));
        builder.AddAtom(new AtomSpec(8, 0, 0, 0, 2, 0, 0, false, "", "O"));
        BondSpec single = new BondSpec();
        builder.AddBond(carbon0, carbon1, single);
        builder.AddBond(carbon1, carbon2, single);
        builder.AddBond(carbon2, carbon0, single);
        return builder.Build();
    }
}
