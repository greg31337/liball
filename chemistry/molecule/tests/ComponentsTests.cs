using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class ComponentsTests
{
    [Test]
    public void GetComponentIds_AssignsDiscoveryOrderIdsAndCachesThem()
    {
        Molecule molecule = BuildDisconnectedMolecule();
        Components components = new Components();

        List<int> ids = components.GetComponentIds(molecule);

        Assert.That(ids, Is.EqualTo(new int[] { 0, 0, 1, 1, 1 }));
        Assert.That(molecule.ComponentIdsByAtomId, Is.EqualTo(new int[] { 0, 0, 1, 1, 1 }));
        Assert.That(molecule.IsComponentCacheValid, Is.True);

        List<int> idsFromCache = components.GetComponentIds(molecule);

        Assert.That(idsFromCache, Is.EqualTo(new int[] { 0, 0, 1, 1, 1 }));
        Assert.That(idsFromCache, Is.Not.SameAs(molecule.ComponentIdsByAtomId));
    }

    [Test]
    public void FindComponents_RecordsAtomBondAndSummaryData()
    {
        Molecule molecule = BuildDisconnectedMolecule();
        Components components = new Components();

        List<ComponentInfo> found = components.FindComponents(molecule);

        Assert.That(found.Count, Is.EqualTo(2));
        Assert.That(found[0].ComponentId, Is.EqualTo(0));
        Assert.That(found[0].AtomIds, Is.EqualTo(new int[] { 0, 1 }));
        Assert.That(found[0].BondIds, Is.EqualTo(new int[] { 0 }));
        Assert.That(found[0].HeavyAtomCount, Is.EqualTo(2));
        Assert.That(found[0].AtomCount, Is.EqualTo(2));
        Assert.That(found[0].BondCount, Is.EqualTo(1));
        Assert.That(found[0].FirstAtomId, Is.EqualTo(0));
        Assert.That(found[0].AtomKey, Is.EqualTo("11.17"));

        Assert.That(found[1].ComponentId, Is.EqualTo(1));
        Assert.That(found[1].AtomIds, Is.EqualTo(new int[] { 2, 3, 4 }));
        Assert.That(found[1].BondIds, Is.EqualTo(new int[] { 1, 2 }));
        Assert.That(found[1].HeavyAtomCount, Is.EqualTo(2));
        Assert.That(found[1].AtomCount, Is.EqualTo(3));
        Assert.That(found[1].BondCount, Is.EqualTo(2));
        Assert.That(found[1].AtomKey, Is.EqualTo("1.6.8"));
    }

    [Test]
    public void OrderComponents_SortsFragmentsByDeterministicSummary()
    {
        Molecule molecule = BuildDisconnectedMolecule();
        Components components = new Components();

        List<ComponentInfo> ordered = components.OrderComponents(components.FindComponents(molecule));

        Assert.That(ordered.Count, Is.EqualTo(2));
        Assert.That(ordered[0].ComponentId, Is.EqualTo(1));
        Assert.That(ordered[1].ComponentId, Is.EqualTo(0));
    }

    [Test]
    public void OrderComponents_UsesAtomKeyAndFirstAtomTieBreakers()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        Molecule molecule = builder.Build();
        Components components = new Components();

        List<ComponentInfo> ordered = components.OrderComponents(components.FindComponents(molecule));

        Assert.That(ordered[0].FirstAtomId, Is.EqualTo(1));
        Assert.That(ordered[1].FirstAtomId, Is.EqualTo(2));
        Assert.That(ordered[2].FirstAtomId, Is.EqualTo(0));
    }

    [Test]
    public void Split_ReturnsOrderedFragmentsWithRenumberedAtomsAndBonds()
    {
        Molecule molecule = BuildDisconnectedMolecule();
        Components components = new Components();

        List<Molecule> fragments = components.Split(molecule);

        Assert.That(fragments.Count, Is.EqualTo(2));
        Assert.That(fragments[0].GetAtomCount(), Is.EqualTo(3));
        Assert.That(fragments[0].GetBondCount(), Is.EqualTo(2));
        Assert.That(fragments[0].Atoms[0].AtomicNumber, Is.EqualTo(6));
        Assert.That(fragments[0].Atoms[1].AtomicNumber, Is.EqualTo(1));
        Assert.That(fragments[0].Atoms[2].AtomicNumber, Is.EqualTo(8));
        Assert.That(fragments[0].Bonds[0].BeginAtomId, Is.EqualTo(0));
        Assert.That(fragments[0].Bonds[0].EndAtomId, Is.EqualTo(1));
        Assert.That(fragments[0].Bonds[1].BeginAtomId, Is.EqualTo(0));
        Assert.That(fragments[0].Bonds[1].EndAtomId, Is.EqualTo(2));

        Assert.That(fragments[1].GetAtomCount(), Is.EqualTo(2));
        Assert.That(fragments[1].GetBondCount(), Is.EqualTo(1));
        Assert.That(fragments[1].Atoms[0].AtomicNumber, Is.EqualTo(11));
        Assert.That(fragments[1].Atoms[1].AtomicNumber, Is.EqualTo(17));
        Assert.That(fragments[1].Bonds[0].BeginAtomId, Is.EqualTo(0));
        Assert.That(fragments[1].Bonds[0].EndAtomId, Is.EqualTo(1));
    }

    [Test]
    public void NullComponentOperations_ReturnIndependentEmptyCollections()
    {
        Components components = new Components();

        List<ComponentInfo> found = components.FindComponents(null);
        List<int> ids = components.GetComponentIds(null);
        List<Molecule> split = components.Split(null);
        found.Add(new ComponentInfo());
        ids.Add(4);
        split.Add(new Molecule());

        Assert.That(components.FindComponents(null), Is.Empty);
        Assert.That(components.GetComponentIds(null), Is.Empty);
        Assert.That(components.Split(null), Is.Empty);
    }

    [Test]
    public void Split_PreservesConformersResiduesChainsAndMetadataWithoutAliasing()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 2, 0, 0, false, "", "O"));
        Molecule source = builder.Build();
        source.Metadata.Names.Add("mixture");

        Conformer conformer = new Conformer(2);
        conformer.Id = 0;
        conformer.Name = "model";
        conformer.Coordinates[carbon] = new Coordinate3D(1.0, 0.0, 0.0);
        conformer.Coordinates[oxygen] = new Coordinate3D(2.0, 0.0, 0.0);
        source.Conformers.Add(conformer);

        Residue carbonResidue = new Residue();
        carbonResidue.Id = 10;
        carbonResidue.Name = "CAR";
        carbonResidue.ChainId = "A";
        carbonResidue.RootAtomId = 99;
        carbonResidue.AtomIds.Add(carbon);
        source.Residues.Add(carbonResidue);
        Residue oxygenResidue = new Residue();
        oxygenResidue.Id = 20;
        oxygenResidue.Name = "OXY";
        oxygenResidue.ChainId = "B";
        oxygenResidue.RootAtomId = oxygen;
        oxygenResidue.AtomIds.Add(oxygen);
        source.Residues.Add(oxygenResidue);

        Chain carbonChain = new Chain();
        carbonChain.Id = "A";
        carbonChain.ResidueIds.Add(10);
        source.Chains.Add(carbonChain);
        Chain oxygenChain = new Chain();
        oxygenChain.Id = "B";
        oxygenChain.ResidueIds.Add(20);
        source.Chains.Add(oxygenChain);

        Components components = new Components();
        List<Molecule> fragments = components.Split(source);
        Molecule carbonFragment = fragments[0];
        Molecule oxygenFragment = fragments[1];

        Assert.That(carbonFragment.Atoms[0].AtomicNumber, Is.EqualTo(6));
        Assert.That(carbonFragment.Conformers[0].Coordinates[0].X, Is.EqualTo(1.0));
        Assert.That(carbonFragment.Residues, Has.Count.EqualTo(1));
        Assert.That(carbonFragment.Residues[0].Id, Is.EqualTo(10));
        Assert.That(carbonFragment.Residues[0].RootAtomId, Is.EqualTo(0));
        Assert.That(carbonFragment.Chains[0].ResidueIds, Is.EqualTo(new int[] { 10 }));
        Assert.That(oxygenFragment.Atoms[0].AtomicNumber, Is.EqualTo(8));
        Assert.That(oxygenFragment.Conformers[0].Coordinates[0].X, Is.EqualTo(2.0));
        Assert.That(oxygenFragment.Residues[0].Id, Is.EqualTo(20));
        Assert.That(oxygenFragment.Chains[0].Id, Is.EqualTo("B"));

        carbonFragment.Metadata.Names[0] = "changed";
        carbonFragment.Conformers[0].Coordinates[0].X = 99.0;
        carbonFragment.Residues[0].Name = "changed";
        carbonFragment.Chains[0].ResidueIds.Add(11);

        Assert.That(source.Metadata.Names[0], Is.EqualTo("mixture"));
        Assert.That(source.Conformers[0].Coordinates[0].X, Is.EqualTo(1.0));
        Assert.That(source.Residues[0].Name, Is.EqualTo("CAR"));
        Assert.That(source.Chains[0].ResidueIds, Is.EqualTo(new int[] { 10 }));
    }

    static Molecule BuildDisconnectedMolecule()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int sodium = builder.AddAtom(new AtomSpec(11, 0, 1, 0, 0, 0, 0, false, "", "Na"));
        int chlorine = builder.AddAtom(new AtomSpec(17, 0, -1, 0, 0, 0, 0, false, "", "Cl"));
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int hydrogen = builder.AddAtom(new AtomSpec(1, 0, 0, 0, 0, 0, 0, false, "", "H"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        builder.AddBond(sodium, chlorine, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        builder.AddBond(carbon, hydrogen, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        builder.AddBond(carbon, oxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }
}
