using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class MoleculeEditorPreservationTests
{
    [Test]
    public void InitializeAndClone_DeepCopyAllAssociatedData()
    {
        Molecule source = BuildAssociatedMolecule();
        IMoleculeBuilder editor = new MoleculeBuilder();

        editor.Initialize(source);
        Molecule initialized = editor.Build();
        Molecule clone = editor.Clone();

        source.Atoms[0].AtomicNumber = 14;
        source.Conformers[0].Coordinates[0].X = 99.0;
        source.StereoRecords[0].Kind = "changed";
        source.Residues[0].AtomIds.Add(2);
        source.Chains[0].ResidueIds.Add(20);
        source.Metadata.Names[0] = "changed";

        Assert.That(initialized.Atoms[0].AtomicNumber, Is.EqualTo(6));
        Assert.That(initialized.Conformers[0].Coordinates[0].X, Is.EqualTo(1.0));
        Assert.That(initialized.StereoRecords[0].Kind, Is.EqualTo("double-bond"));
        Assert.That(initialized.Residues[0].AtomIds, Is.EqualTo(new int[] { 0, 1 }));
        Assert.That(initialized.Chains[0].ResidueIds, Is.EqualTo(new int[] { 10 }));
        Assert.That(initialized.Metadata.Names, Is.EqualTo(new string[] { "source" }));
        Assert.That(clone.Residues[0], Is.Not.SameAs(initialized.Residues[0]));
        Assert.That(clone.Chains[0], Is.Not.SameAs(initialized.Chains[0]));

        editor.Initialize(null);
        Assert.That(editor.Build().Atoms, Is.Empty);
        Assert.That(editor.Build().Residues, Is.Empty);
    }

    [Test]
    public void Merge_OffsetsAndPreservesAssociatedData()
    {
        MoleculeBuilder leftBuilder = new MoleculeBuilder();
        int left0 = leftBuilder.AddAtom(new AtomSpec(7, 0, 0, 0, 0, 0, 0, false, "", "N"));
        int left1 = leftBuilder.AddAtom(new AtomSpec(1, 0, 0, 0, 0, 0, 0, false, "", "H"));
        leftBuilder.AddBond(left0, left1, new BondSpec());
        Molecule left = leftBuilder.Build();
        Conformer leftConformer = new Conformer(2);
        leftConformer.Id = 0;
        leftConformer.Coordinates[0] = new Coordinate3D(-1.0, 0.0, 0.0);
        left.Conformers.Add(leftConformer);
        Residue leftResidue = new Residue();
        leftResidue.Id = 5;
        leftResidue.AtomIds.Add(0);
        left.Residues.Add(leftResidue);
        left.Metadata.Names.Add("left");

        Molecule source = BuildAssociatedMolecule();
        IMoleculeBuilder editor = leftBuilder;
        editor.Merge(source);
        Molecule merged = editor.Build();

        Assert.That(merged.Atoms.Count, Is.EqualTo(5));
        Assert.That(merged.Bonds.Count, Is.EqualTo(2));
        Assert.That(merged.Bonds[1].BeginAtomId, Is.EqualTo(2));
        Assert.That(merged.Bonds[1].EndAtomId, Is.EqualTo(3));
        Assert.That(merged.Conformers.Count, Is.EqualTo(2));
        Assert.That(merged.Conformers[0].Coordinates.Count, Is.EqualTo(5));
        Assert.That(merged.Conformers[1].Coordinates[0].IsSet, Is.False);
        Assert.That(merged.Conformers[1].Coordinates[2].X, Is.EqualTo(1.0));
        Assert.That(merged.StereoRecords[0].StereoBondId, Is.EqualTo(1));
        Assert.That(merged.StereoRecords[0].BeginLigandAtomId, Is.EqualTo(2));
        Assert.That(merged.StereoGroups.Groups[0].StereoRecordIds, Is.EqualTo(new int[] { 0 }));
        Assert.That(merged.Residues[1].Id, Is.EqualTo(1));
        Assert.That(merged.Residues[1].AtomIds, Is.EqualTo(new int[] { 2, 3 }));
        Assert.That(merged.Chains[0].ResidueIds, Is.EqualTo(new int[] { 1 }));
        Assert.That(merged.Metadata.Names, Is.EqualTo(new string[] { "left", "source" }));
    }

    [Test]
    public void RemoveOperations_RemapOrRemoveAssociatedReferencesAndCaches()
    {
        IMoleculeBuilder editor = new MoleculeBuilder();
        editor.Initialize(BuildAssociatedMolecule());
        Molecule molecule = editor.Build();
        molecule.GetDegree(0);
        new Components().GetComponentIds(molecule);
        new RingFinder().FindRings(molecule);

        editor.RemoveAtom(0);

        Assert.That(molecule.Atoms.Count, Is.EqualTo(2));
        Assert.That(molecule.Atoms[0].AtomicNumber, Is.EqualTo(8));
        Assert.That(molecule.Bonds, Is.Empty);
        Assert.That(molecule.Conformers[0].Coordinates.Count, Is.EqualTo(2));
        Assert.That(molecule.Conformers[0].Coordinates[0].X, Is.EqualTo(2.0));
        Assert.That(molecule.StereoRecords, Is.Empty);
        Assert.That(molecule.StereoGroups.Groups, Is.Empty);
        Assert.That(molecule.Residues[0].AtomIds, Is.EqualTo(new int[] { 0 }));
        Assert.That(molecule.Residues[0].RootAtomId, Is.EqualTo(0));
        Assert.That(molecule.Residues[1].AtomIds, Is.EqualTo(new int[] { 1 }));
        Assert.That(molecule.IsAdjacencyCacheValid, Is.False);
        Assert.That(molecule.IsComponentCacheValid, Is.False);
        Assert.That(molecule.IsRingCacheValid, Is.False);

        editor.Initialize(BuildAssociatedMolecule());
        editor.RemoveBond(0);
        Assert.That(editor.Build().StereoRecords, Is.Empty);
        Assert.That(editor.Build().StereoGroups.Groups, Is.Empty);
    }

    [Test]
    public void ReplaceAtom_PreservesAssociatedReferencesAndCoordinates()
    {
        IMoleculeBuilder editor = new MoleculeBuilder();
        editor.Initialize(BuildAssociatedMolecule());

        editor.ReplaceAtom(1, new AtomSpec(16, 0, 0, 0, 0, 0, 0, false, "", "S"));
        Molecule molecule = editor.Build();

        Assert.That(molecule.Atoms[1].AtomicNumber, Is.EqualTo(16));
        Assert.That(molecule.Conformers[0].Coordinates[1].X, Is.EqualTo(2.0));
        Assert.That(molecule.StereoRecords[0].EndLigandAtomId, Is.EqualTo(1));
        Assert.That(molecule.Residues[0].AtomIds, Is.EqualTo(new int[] { 0, 1 }));
        Assert.That(molecule.Chains[0].ResidueIds, Is.EqualTo(new int[] { 10 }));
        Assert.That(molecule.Metadata.Names, Is.EqualTo(new string[] { "source" }));
        Assert.That(molecule.IsAdjacencyCacheValid, Is.False);
    }

    [Test]
    public void Renumber_NormalizesGraphAndAssociatedIdentifiers()
    {
        IMoleculeBuilder editor = new MoleculeBuilder();
        editor.Initialize(BuildAssociatedMolecule());
        Molecule molecule = editor.Build();
        molecule.Atoms[0].Id = 10;
        molecule.Atoms[1].Id = 5;
        molecule.Atoms[2].Id = 20;
        molecule.Bonds[0].Id = 7;
        molecule.Bonds[0].BeginAtomId = 10;
        molecule.Bonds[0].EndAtomId = 5;
        molecule.StereoRecords[0].Id = 9;
        molecule.StereoRecords[0].StereoBondId = 7;
        molecule.StereoRecords[0].BeginLigandAtomId = 10;
        molecule.StereoRecords[0].EndLigandAtomId = 5;
        molecule.StereoGroups.Groups[0].StereoRecordIds[0] = 9;
        molecule.Residues[0].RootAtomId = 10;
        molecule.Residues[0].AtomIds[0] = 10;
        molecule.Residues[0].AtomIds[1] = 5;
        editor.Renumber();

        Assert.That(molecule.Atoms.Select(atom => atom.Id), Is.EqualTo(new int[] { 0, 1, 2 }));
        Assert.That(molecule.Bonds[0].Id, Is.EqualTo(0));
        Assert.That(molecule.Bonds[0].BeginAtomId, Is.EqualTo(0));
        Assert.That(molecule.Bonds[0].EndAtomId, Is.EqualTo(1));
        Assert.That(molecule.StereoRecords[0].Id, Is.EqualTo(0));
        Assert.That(molecule.StereoRecords[0].StereoBondId, Is.EqualTo(0));
        Assert.That(molecule.StereoRecords[0].BeginLigandAtomId, Is.EqualTo(0));
        Assert.That(molecule.StereoRecords[0].EndLigandAtomId, Is.EqualTo(1));
        Assert.That(molecule.StereoGroups.Groups[0].StereoRecordIds, Is.EqualTo(new int[] { 0 }));
        Assert.That(molecule.Residues[0].AtomIds, Is.EqualTo(new int[] { 0, 1 }));
        Assert.That(molecule.Residues[0].RootAtomId, Is.EqualTo(0));
        Assert.That(molecule.IsAdjacencyCacheValid, Is.False);
    }

    [Test]
    public void SplitComponents_SubsetsAssociatedDataAndPreservesMetadata()
    {
        IMoleculeBuilder editor = new MoleculeBuilder();
        editor.Initialize(BuildAssociatedMolecule());

        List<Molecule> fragments = editor.SplitComponents();

        Assert.That(fragments.Count, Is.EqualTo(2));
        Molecule connected = fragments[0];
        Molecule isolated = fragments[1];
        Assert.That(connected.Atoms.Count, Is.EqualTo(2));
        Assert.That(connected.Metadata.Names, Is.EqualTo(new string[] { "source" }));
        Assert.That(connected.Conformers[0].Coordinates.Count, Is.EqualTo(2));
        Assert.That(connected.Conformers[0].Coordinates[1].X, Is.EqualTo(2.0));
        Assert.That(connected.StereoRecords.Count, Is.EqualTo(1));
        Assert.That(connected.StereoRecords[0].StereoBondId, Is.EqualTo(0));
        Assert.That(connected.StereoGroups.Groups[0].StereoRecordIds, Is.EqualTo(new int[] { 0 }));
        Assert.That(connected.Residues.Count, Is.EqualTo(1));
        Assert.That(connected.Residues[0].AtomIds, Is.EqualTo(new int[] { 0, 1 }));
        Assert.That(connected.Chains[0].ResidueIds, Is.EqualTo(new int[] { 10 }));

        Assert.That(isolated.Atoms.Count, Is.EqualTo(1));
        Assert.That(isolated.Conformers[0].Coordinates[0].X, Is.EqualTo(3.0));
        Assert.That(isolated.StereoRecords, Is.Empty);
        Assert.That(isolated.Residues[0].AtomIds, Is.EqualTo(new int[] { 0 }));
        Assert.That(isolated.Chains[0].ResidueIds, Is.EqualTo(new int[] { 20 }));
    }

    static Molecule BuildAssociatedMolecule()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int nitrogen = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 0, 0, 0, false, "", "N"));
        BondSpec doubleBond = new BondSpec();
        doubleBond.Order = BondOrder.Double;
        builder.AddBond(carbon, oxygen, doubleBond);
        Molecule molecule = builder.Build();
        molecule.Atoms[0].HasCoordinates = true;
        molecule.Atoms[0].X = 1.0;

        Conformer conformer = new Conformer(3);
        conformer.Id = 0;
        conformer.Name = "model";
        conformer.Coordinates[0] = new Coordinate3D(1.0, 0.0, 0.0);
        conformer.Coordinates[1] = new Coordinate3D(2.0, 0.0, 0.0);
        conformer.Coordinates[2] = new Coordinate3D(3.0, 0.0, 0.0);
        molecule.Conformers.Add(conformer);

        StereoRecord stereo = new StereoRecord();
        stereo.Id = 0;
        stereo.Kind = "double-bond";
        stereo.StereoBondId = 0;
        stereo.BeginLigandAtomId = 0;
        stereo.EndLigandAtomId = 1;
        stereo.Configuration = DoubleBondConfiguration.E;
        stereo.GroupId = 0;
        molecule.StereoRecords.Add(stereo);

        EnhancedStereoGroup group = new EnhancedStereoGroup();
        group.Id = 0;
        group.Type = "abs";
        group.StereoRecordIds.Add(0);
        molecule.StereoGroups.Groups.Add(group);

        Residue connectedResidue = new Residue();
        connectedResidue.Id = 10;
        connectedResidue.Name = "CON";
        connectedResidue.ChainId = "A";
        connectedResidue.RootAtomId = 0;
        connectedResidue.AtomIds.Add(0);
        connectedResidue.AtomIds.Add(1);
        molecule.Residues.Add(connectedResidue);

        Residue isolatedResidue = new Residue();
        isolatedResidue.Id = 20;
        isolatedResidue.Name = "ISO";
        isolatedResidue.ChainId = "B";
        isolatedResidue.RootAtomId = 2;
        isolatedResidue.AtomIds.Add(2);
        molecule.Residues.Add(isolatedResidue);

        Chain chainA = new Chain();
        chainA.Id = "A";
        chainA.ResidueIds.Add(10);
        molecule.Chains.Add(chainA);
        Chain chainB = new Chain();
        chainB.Id = "B";
        chainB.ResidueIds.Add(20);
        molecule.Chains.Add(chainB);

        molecule.Metadata.Names.Add("source");
        molecule.Metadata.SdfFields.Add(new MoleculeMetadataEntry("ID", "42"));
        return molecule;
    }
}
