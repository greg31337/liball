using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class MoleculeFormatsTests
{
    [Test]
    public void Facade_ReportsRegisteredMoleculeCapabilities()
    {
        MoleculeFormats formats = new MoleculeFormats();

        Assert.That(formats.CanReadMolecule(FormatKind.Smiles), Is.True);
        Assert.That(formats.CanWriteMolecule(FormatKind.Smiles), Is.True);
        Assert.That(formats.CanReadMolecule(FormatKind.MolV2000), Is.True);
        Assert.That(formats.CanWriteMolecule(FormatKind.MolV2000), Is.True);
        Assert.That(formats.CanReadMolecule(FormatKind.MolV3000), Is.True);
        Assert.That(formats.CanWriteMolecule(FormatKind.MolV3000), Is.True);
        Assert.That(formats.CanReadMolecule(FormatKind.Sdf), Is.True);
        Assert.That(formats.CanWriteMolecule(FormatKind.Sdf), Is.True);
        Assert.That(formats.CanReadMolecule(FormatKind.Mol2), Is.True);
        Assert.That(formats.CanWriteMolecule(FormatKind.Mol2), Is.True);
        Assert.That(formats.CanReadMolecule(FormatKind.Pdb), Is.True);
        Assert.That(formats.CanWriteMolecule(FormatKind.Pdb), Is.True);
        Assert.That(formats.CanReadMolecule(FormatKind.InChI), Is.True);
        Assert.That(formats.CanWriteMolecule(FormatKind.InChI), Is.True);
        Assert.That(formats.CanReadMolecule(FormatKind.InChIKey), Is.True);
        Assert.That(formats.CanWriteMolecule(FormatKind.InChIKey), Is.True);
        Assert.That(formats.CanReadMolecule(FormatKind.VendorDelimited), Is.True);
        Assert.That(formats.CanReadMolecule(FormatKind.Smarts), Is.False);
        Assert.That(formats.CanWriteMolecule(FormatKind.VendorDelimited), Is.True);
        Assert.That(formats.CanReadMolecule(FormatKind.DeterministicText), Is.True);
        Assert.That(formats.CanWriteMolecule(FormatKind.DeterministicText), Is.True);
        Assert.That(formats.CanReadMolecule(FormatKind.Unknown), Is.False);
        Assert.That(formats.CanWriteMolecule(FormatKind.Unknown), Is.False);
    }

    [Test]
    public void CommonReaderAndWriterInterfacesWorkWithConcreteFormats()
    {
        IMoleculeReader reader = new SmilesParser();
        IMoleculeWriter writer = new SmilesWriter();

        Molecule molecule = reader.Read("CO");

        Assert.That(writer.Write(molecule), Is.EqualTo("CO"));
    }

    [Test]
    public void RegisterReaderAndWriterReplaceFacadeBehavior()
    {
        MoleculeFormats formats = new MoleculeFormats();
        StubMoleculeReader reader = new StubMoleculeReader();
        StubMoleculeWriter writer = new StubMoleculeWriter();
        formats.RegisterReader(FormatKind.Smiles, reader);
        formats.RegisterWriter(FormatKind.Smiles, writer);

        Molecule molecule = formats.ReadStringAs("ignored", FormatKind.Smiles);
        string text = formats.WriteStringAs(molecule, FormatKind.Smiles);

        Assert.That(reader.ReadCount, Is.EqualTo(1));
        Assert.That(writer.WriteCount, Is.EqualTo(1));
        Assert.That(molecule.Atoms.Count, Is.EqualTo(1));
        Assert.That(molecule.Atoms[0].AtomicNumber, Is.EqualTo(7));
        Assert.That(text, Is.EqualTo("stub-format"));
    }

    [Test]
    public void Detect_DelegatesToFormatDetection()
    {
        MoleculeFormats formats = new MoleculeFormats();

        FormatDetectionResult result = formats.Detect("@<TRIPOS>MOLECULE\nname\n", "");

        Assert.That(result.Kind, Is.EqualTo(FormatKind.Mol2));
    }

    [Test]
    public void ReadString_ReadsDetectedSmilesMol2PdbAndSdf()
    {
        MoleculeFormats formats = new MoleculeFormats();

        Molecule smiles = formats.ReadString("CO", new FormatOptions());
        Molecule mol2 = formats.ReadString(BuildMol2(), new FormatOptions());
        Molecule pdb = formats.ReadString(BuildPdb(), new FormatOptions());
        Molecule sdf = formats.ReadString(BuildSdf(), new FormatOptions());

        Assert.That(smiles.Atoms.Count, Is.EqualTo(2));
        Assert.That(mol2.Metadata.GetPrimaryName(), Is.EqualTo("Methane"));
        Assert.That(pdb.Atoms.Count, Is.EqualTo(1));
        Assert.That(sdf.Metadata.GetPrimaryName(), Is.EqualTo("SDF molecule"));
    }

    [Test]
    public void WriteString_UsesHintAndFileName()
    {
        MoleculeFormats formats = new MoleculeFormats();
        Molecule molecule = BuildMolecule("Facade");
        FormatOptions mol2Options = new FormatOptions();
        mol2Options.KindHint = FormatKind.Mol2;
        FormatOptions pdbOptions = new FormatOptions();
        pdbOptions.FileName = "facade.pdb";

        string mol2 = formats.WriteString(molecule, mol2Options);
        string pdb = formats.WriteString(molecule, pdbOptions);

        Assert.That(mol2, Does.StartWith("@<TRIPOS>MOLECULE\nFacade\n"));
        Assert.That(pdb, Does.Contain("ATOM"));
        Assert.That(pdb, Does.EndWith("END\n"));
    }

    [Test]
    public void WriteString_DefaultsToSmilesForUnknownOutputFormat()
    {
        MoleculeFormats formats = new MoleculeFormats();

        string text = formats.WriteString(BuildMolecule("Default"), new FormatOptions());

        Assert.That(text, Is.EqualTo("C"));
    }

    [Test]
    public void WriteString_ReadString_RoundTripDeterministicText()
    {
        MoleculeFormats formats = new MoleculeFormats();
        FormatOptions options = new FormatOptions();
        options.KindHint = FormatKind.DeterministicText;

        string text = formats.WriteString(BuildMolecule("Deterministic"), options);
        Molecule molecule = formats.ReadString(text, new FormatOptions());

        Assert.That(text, Does.StartWith("MOLDET1\n"));
        Assert.That(molecule.Atoms.Count, Is.EqualTo(1));
    }

    [Test]
    public void ExplicitIdentifierCodecs_RoundTripStoredInChIAndInChIKey()
    {
        MoleculeFormats formats = new MoleculeFormats();
        string inchiText = "InChI=1S/H2O/h1H2";
        string keyText = "XLYOFNOQVPJJNP-UHFFFAOYSA-N";

        Molecule inchi = formats.ReadStringAs(inchiText, FormatKind.InChI);
        Molecule key = formats.ReadStringAs(keyText, FormatKind.InChIKey);

        Assert.That(formats.WriteStringAs(inchi, FormatKind.InChI), Is.EqualTo(inchiText));
        Assert.That(formats.WriteStringAs(key, FormatKind.InChIKey), Is.EqualTo(keyText));
    }

    [Test]
    public void ExplicitUnsupportedCodec_ReturnsEmptyValuesWithoutGuessing()
    {
        MoleculeFormats formats = new MoleculeFormats();

        Molecule molecule = formats.ReadStringAs("[#6]", FormatKind.Smarts);
        string text = formats.WriteStringAs(BuildMolecule("unsupported"), FormatKind.Smarts);

        Assert.That(molecule.GetAtomCount(), Is.Zero);
        Assert.That(text, Is.Empty);
    }

    static Molecule BuildMolecule(string name)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", "C"));
        Molecule molecule = builder.Build();
        molecule.Metadata.AddName(name);
        molecule.Atoms[carbon].HasCoordinates = true;
        return molecule;
    }

    static string BuildMol2()
    {
        return string.Join(
            "\n",
            new string[]
            {
                "@<TRIPOS>MOLECULE",
                "Methane",
                "1 0 0 0 0",
                "SMALL",
                "NO_CHARGES",
                "@<TRIPOS>ATOM",
                "1 C1 0.0 0.0 0.0 C.3",
                ""
            });
    }

    static string BuildPdb()
    {
        return "ATOM      1    C UNK A   1       0.000   0.000   0.000  1.00  0.00           C  \nEND\n";
    }

    static string BuildSdf()
    {
        V2000Writer writer = new V2000Writer();
        Molecule molecule = BuildMolecule("SDF molecule");
        return writer.Write(molecule) + "$$$$\n";
    }

    class StubMoleculeReader : IMoleculeReader
    {
        public int ReadCount;

        public Molecule Read(string? text)
        {
            ReadCount = ReadCount + 1;
            MoleculeBuilder builder = new MoleculeBuilder();
            builder.AddAtom(new AtomSpec(7, 0, 0, 0, 3, 0, 0, false, "", "N"));
            return builder.Build();
        }
    }

    class StubMoleculeWriter : IMoleculeWriter
    {
        public int WriteCount;

        public string Write(Molecule? molecule)
        {
            WriteCount = WriteCount + 1;
            return "stub-format";
        }
    }
}
