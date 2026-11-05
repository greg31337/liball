using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class AtomBondModelTests
{
    [Test]
    public void AtomSpec_CreateEmpty_InitializesNeutralUnspecifiedAtom()
    {
        AtomSpec spec = AtomSpec.CreateEmpty();

        Assert.That(spec.AtomicNumber, Is.EqualTo(0));
        Assert.That(spec.IsotopeMassNumber, Is.EqualTo(0));
        Assert.That(spec.FormalCharge, Is.EqualTo(0));
        Assert.That(spec.ExplicitHydrogenCount, Is.EqualTo(0));
        Assert.That(spec.ImplicitHydrogenCount, Is.EqualTo(0));
        Assert.That(spec.RadicalElectronCount, Is.EqualTo(0));
        Assert.That(spec.AtomMapNumber, Is.EqualTo(0));
        Assert.That(spec.IsAromatic, Is.False);
        Assert.That(spec.QueryOperation, Is.EqualTo(QueryOperation.None));
        Assert.That(spec.QueryValue, Is.EqualTo(""));
        Assert.That(spec.Alias, Is.EqualTo(""));
        Assert.That(spec.Label, Is.EqualTo(""));
    }

    [Test]
    public void Atom_ConstructedFromSpec_CopiesSpecFieldsAndAssignsId()
    {
        AtomSpec spec = new AtomSpec(6, 13, -1, 1, 2, 1, 42, true, "Me", "C13");
        spec.QueryOperation = QueryOperation.Equals;
        spec.QueryValue = "organic";

        Atom atom = new Atom(7, spec);

        Assert.That(atom.Id, Is.EqualTo(7));
        Assert.That(atom.AtomicNumber, Is.EqualTo(6));
        Assert.That(atom.IsotopeMassNumber, Is.EqualTo(13));
        Assert.That(atom.FormalCharge, Is.EqualTo(-1));
        Assert.That(atom.ExplicitHydrogenCount, Is.EqualTo(1));
        Assert.That(atom.ImplicitHydrogenCount, Is.EqualTo(2));
        Assert.That(atom.RadicalElectronCount, Is.EqualTo(1));
        Assert.That(atom.AtomMapNumber, Is.EqualTo(42));
        Assert.That(atom.IsAromatic, Is.True);
        Assert.That(atom.QueryOperation, Is.EqualTo(QueryOperation.Equals));
        Assert.That(atom.QueryValue, Is.EqualTo("organic"));
        Assert.That(atom.Alias, Is.EqualTo("Me"));
        Assert.That(atom.Label, Is.EqualTo("C13"));
        Assert.That(atom.HasCoordinates, Is.False);
        Assert.That(atom.X, Is.EqualTo(0.0));
        Assert.That(atom.Y, Is.EqualTo(0.0));
        Assert.That(atom.Z, Is.EqualTo(0.0));
    }

    [Test]
    public void BondSpec_DefaultConstructor_InitializesSingleUnspecifiedBond()
    {
        BondSpec spec = new BondSpec();

        Assert.That(spec.Order, Is.EqualTo(BondOrder.Single));
        Assert.That(spec.IsAromatic, Is.False);
        Assert.That(spec.Stereo, Is.EqualTo(BondStereo.None));
        Assert.That(spec.Direction, Is.EqualTo(BondDirection.None));
        Assert.That(spec.IsDative, Is.False);
        Assert.That(spec.QueryOperation, Is.EqualTo(QueryOperation.None));
        Assert.That(spec.QueryValue, Is.EqualTo(""));
        Assert.That(spec.SourceFlags, Is.EqualTo(""));
    }

    [Test]
    public void Bond_ConstructedFromSpec_CopiesSpecFieldsAndAtomIds()
    {
        BondSpec spec = BondSpec.Create(BondOrder.Double, true, BondStereo.E, BondDirection.Up);
        spec.IsDative = true;
        spec.QueryOperation = QueryOperation.NotEquals;
        spec.QueryValue = "ring";
        spec.SourceFlags = "M  CFG";

        Bond bond = new Bond(3, 10, 11, spec);

        Assert.That(bond.Id, Is.EqualTo(3));
        Assert.That(bond.BeginAtomId, Is.EqualTo(10));
        Assert.That(bond.EndAtomId, Is.EqualTo(11));
        Assert.That(bond.Order, Is.EqualTo(BondOrder.Double));
        Assert.That(bond.IsAromatic, Is.True);
        Assert.That(bond.Stereo, Is.EqualTo(BondStereo.E));
        Assert.That(bond.Direction, Is.EqualTo(BondDirection.Up));
        Assert.That(bond.IsDative, Is.True);
        Assert.That(bond.QueryOperation, Is.EqualTo(QueryOperation.NotEquals));
        Assert.That(bond.QueryValue, Is.EqualTo("ring"));
        Assert.That(bond.SourceFlags, Is.EqualTo("M  CFG"));
    }

    [Test]
    public void SimpleAtomAndBondFactories_SetIdsAndCoreFields()
    {
        Atom atom = Atom.CreateWithAtomicNumber(5, 8);
        Bond bond = Bond.CreateWithOrder(9, 5, 6, BondOrder.Triple);

        Assert.That(atom.Id, Is.EqualTo(5));
        Assert.That(atom.AtomicNumber, Is.EqualTo(8));
        Assert.That(atom.FormalCharge, Is.EqualTo(0));
        Assert.That(atom.Label, Is.EqualTo(""));

        Assert.That(bond.Id, Is.EqualTo(9));
        Assert.That(bond.BeginAtomId, Is.EqualTo(5));
        Assert.That(bond.EndAtomId, Is.EqualTo(6));
        Assert.That(bond.Order, Is.EqualTo(BondOrder.Triple));
        Assert.That(bond.Stereo, Is.EqualTo(BondStereo.None));
    }
}
