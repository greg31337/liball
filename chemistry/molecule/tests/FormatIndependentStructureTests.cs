using System.Globalization;
using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class FormatIndependentStructureTests
{
    [Test]
    public void MoleculeBuilder_ClonesAtomPolymerAndModelDataIndependently()
    {
        Molecule source = new Molecule();
        Atom atom = new Atom(0, new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", ""));
        atom.AtomType = "C.ar";
        atom.PartialCharge = -0.25;
        atom.HasPartialCharge = true;
        atom.ResidueId = 7;
        atom.ResidueName = "PHE";
        atom.ChainId = "A";
        atom.ModelId = 3;
        atom.Occupancy = 0.75;
        atom.HasOccupancy = true;
        atom.TemperatureFactor = 12.5;
        atom.HasTemperatureFactor = true;
        atom.AlternateLocation = "B";
        atom.InsertionCode = "C";
        source.Atoms.Add(atom);

        MoleculeModel model = new MoleculeModel();
        model.Id = 3;
        model.Name = "third";
        model.AtomIds.Add(0);
        source.Models.Add(model);

        Polymer polymer = new Polymer();
        polymer.Id = 2;
        polymer.Name = "protein";
        polymer.Type = "polypeptide";
        polymer.ChainIds.Add("A");
        polymer.ResidueIds.Add(7);
        source.Polymers.Add(polymer);

        MoleculeBuilder builder = new MoleculeBuilder();
        builder.Initialize(source);
        Molecule clone = builder.Build();

        Assert.That(clone.Atoms[0].AtomType, Is.EqualTo("C.ar"));
        Assert.That(clone.Atoms[0].PartialCharge, Is.EqualTo(-0.25));
        Assert.That(clone.Atoms[0].ResidueId, Is.EqualTo(7));
        Assert.That(clone.Atoms[0].ModelId, Is.EqualTo(3));
        Assert.That(clone.Atoms[0].Occupancy, Is.EqualTo(0.75));
        Assert.That(clone.Atoms[0].TemperatureFactor, Is.EqualTo(12.5));
        Assert.That(clone.Atoms[0].AlternateLocation, Is.EqualTo("B"));
        Assert.That(clone.Atoms[0].InsertionCode, Is.EqualTo("C"));
        Assert.That(clone.Models[0].AtomIds, Is.EqualTo(new int[] { 0 }));
        Assert.That(clone.Polymers[0].ChainIds, Is.EqualTo(new string[] { "A" }));
        Assert.That(clone.Models[0], Is.Not.SameAs(source.Models[0]));
        Assert.That(clone.Polymers[0].ResidueIds, Is.Not.SameAs(source.Polymers[0].ResidueIds));

        builder.RemoveAtom(0);
        Assert.That(clone.Models[0].AtomIds, Is.Empty);
        Assert.That(source.Models[0].AtomIds, Is.EqualTo(new int[] { 0 }));
    }

    [Test]
    public void Mol2ReaderAndWriter_UseTypedAtomAndResidueDataWithoutMetadata()
    {
        string text = "@<TRIPOS>MOLECULE\nTyped\n1 0 1 0 0\nSMALL\nUSER_CHARGES\n"
            + "@<TRIPOS>ATOM\n1 C1 1.0 2.0 3.0 C.3 12 LIG -0.375\n"
            + "@<TRIPOS>SUBSTRUCTURE\n12 LIG 1 RESIDUE 0 A **** 0 ROOT\n";
        Mol2Reader reader = new Mol2Reader();

        Mol2ReadResult result = reader.ReadWithResult(text);
        Atom atom = result.Molecule.Atoms[0];

        Assert.That(result.Success, Is.True);
        Assert.That(atom.AtomType, Is.EqualTo("C.3"));
        Assert.That(atom.HasPartialCharge, Is.True);
        Assert.That(atom.PartialCharge, Is.EqualTo(-0.375).Within(0.000001));
        Assert.That(atom.ResidueId, Is.EqualTo(12));
        Assert.That(atom.ResidueName, Is.EqualTo("LIG"));
        Assert.That(result.Molecule.Residues, Has.Count.EqualTo(1));
        Assert.That(result.Molecule.Chains[0].ResidueIds, Is.EqualTo(new int[] { 12 }));

        result.Molecule.Metadata = new MoleculeMetadata();
        string written = new Mol2Writer().Write(result.Molecule);

        Assert.That(written, Does.Contain("C.3 12 LIG -0.3750"));
        Assert.That(written, Does.Contain("12 LIG 1 RESIDUE 0 A"));
    }

    [Test]
    public void PdbReaderAndWriter_UseTypedAtomResidueAndModelDataWithoutMetadata()
    {
        string text = "MODEL        4\n"
            + AtomLine(1, "CA", "B", "GLY", "Z", 25, "A", 1.0, 2.0, 3.0, "0.65", "18.25", "C")
            + "\nENDMDL\nEND\n";
        PdbReadResult result = new PdbReader().ReadWithResult(text);
        Atom atom = result.Molecule.Atoms[0];

        Assert.That(result.Success, Is.True);
        Assert.That(atom.ResidueId, Is.EqualTo(25));
        Assert.That(atom.ResidueName, Is.EqualTo("GLY"));
        Assert.That(atom.ChainId, Is.EqualTo("Z"));
        Assert.That(atom.ModelId, Is.EqualTo(4));
        Assert.That(atom.AlternateLocation, Is.EqualTo("B"));
        Assert.That(atom.InsertionCode, Is.EqualTo("A"));
        Assert.That(atom.HasOccupancy, Is.True);
        Assert.That(atom.Occupancy, Is.EqualTo(0.65));
        Assert.That(atom.HasTemperatureFactor, Is.True);
        Assert.That(atom.TemperatureFactor, Is.EqualTo(18.25));
        Assert.That(result.Molecule.Models[0].AtomIds, Is.EqualTo(new int[] { 0 }));
        Assert.That(result.Molecule.Residues[0].ModelId, Is.EqualTo(4));
        Assert.That(result.Molecule.Residues[0].InsertionCode, Is.EqualTo("A"));

        result.Molecule.Metadata = new MoleculeMetadata();
        string written = new PdbWriter().Write(result.Molecule);

        Assert.That(written, Does.StartWith("MODEL        4\n"));
        Assert.That(written, Does.Contain("BGLY Z  25A"));
        Assert.That(written, Does.Contain("  0.65 18.25"));
    }

    [Test]
    public void PdbReader_SeparatesRepeatedResiduesAcrossModels()
    {
        string atomLine = AtomLine(1, "N", "", "ALA", "A", 1, "", 0.0, 0.0, 0.0, "1.00", "10.00", "N");
        string text = "MODEL        1\n" + atomLine + "\nENDMDL\n"
            + "MODEL        2\n" + atomLine + "\nENDMDL\nEND\n";

        PdbReadResult result = new PdbReader().ReadWithResult(text);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Molecule.Models, Has.Count.EqualTo(2));
        Assert.That(result.Molecule.Models[0].AtomIds, Is.EqualTo(new int[] { 0 }));
        Assert.That(result.Molecule.Models[1].AtomIds, Is.EqualTo(new int[] { 1 }));
        Assert.That(result.Molecule.Atoms[0].ModelId, Is.EqualTo(1));
        Assert.That(result.Molecule.Atoms[1].ModelId, Is.EqualTo(2));
        Assert.That(result.Molecule.Residues, Has.Count.EqualTo(2));
        Assert.That(result.Molecule.Residues[0].ModelId, Is.EqualTo(1));
        Assert.That(result.Molecule.Residues[1].ModelId, Is.EqualTo(2));
        Assert.That(result.Molecule.Chains, Has.Count.EqualTo(2));
    }

    static string AtomLine(int serial, string atomName, string alternateLocation, string residueName, string chainId, int residueNumber, string insertionCode, double x, double y, double z, string occupancy, string temperatureFactor, string element)
    {
        return "ATOM  "
            + serial.ToString().PadLeft(5)
            + " "
            + atomName.PadLeft(4)
            + alternateLocation.PadRight(1)
            + residueName.PadRight(3)
            + " "
            + chainId
            + residueNumber.ToString().PadLeft(4)
            + insertionCode.PadRight(1)
            + "   "
            + x.ToString("0.000", CultureInfo.InvariantCulture).PadLeft(8)
            + y.ToString("0.000", CultureInfo.InvariantCulture).PadLeft(8)
            + z.ToString("0.000", CultureInfo.InvariantCulture).PadLeft(8)
            + occupancy.PadLeft(6)
            + temperatureFactor.PadLeft(6)
            + "          "
            + element.PadLeft(2)
            + "  ";
    }
}
