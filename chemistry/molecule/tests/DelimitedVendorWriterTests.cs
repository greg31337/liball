using System.Collections.Generic;
using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class DelimitedVendorWriterTests
{
    [Test]
    public void Write_UsesDefaultSingleRecordCsvEntryPoint()
    {
        DelimitedVendorWriter writer = new DelimitedVendorWriter();
        Molecule molecule = BuildMolecule("alias");

        string alias = writer.Write(molecule);
        string direct = writer.WriteDelimitedVendor(molecule, null);

        Assert.That(alias, Is.EqualTo(direct));
        Assert.That(alias, Does.StartWith("smiles\n"));
        Assert.That(writer.Write(null), Is.Empty);
    }

    [Test]
    public void WriteDelimitedVendorRecords_QuotesAndLosslesslyRoundTripsVendorMetadata()
    {
        List<Molecule> molecules = new List<Molecule>();
        Molecule first = BuildMolecule("first");
        first.Metadata.AddVendorField("id", "cmpd-1");
        first.Metadata.AddVendorField("comment", "comma, quote \"and\" newline\nnext");
        first.Metadata.AddVendorField("empty", "");
        Molecule second = BuildMolecule("second");
        second.Metadata.AddVendorField("id", "cmpd-2");
        second.Metadata.AddVendorField("comment", "plain");
        second.Metadata.AddVendorField("empty", "kept");
        molecules.Add(first);
        molecules.Add(second);

        VendorWriteResult written = new DelimitedVendorWriter().WriteDelimitedVendorRecords(molecules, null);
        VendorReadResult read = new DelimitedVendorReader().ReadWithResult(written.Text);

        Assert.That(written.Success, Is.True);
        Assert.That(written.WrittenCount, Is.EqualTo(2));
        Assert.That(written.Columns, Is.EqualTo(new string[] { "smiles", "id", "comment", "empty" }));
        Assert.That(written.MoleculeColumnIndex, Is.Zero);
        Assert.That(written.Text, Does.Contain("\"comma, quote \"\"and\"\" newline\nnext\""));
        Assert.That(read.Success, Is.True);
        Assert.That(read.Molecules, Has.Count.EqualTo(2));
        Assert.That(read.Molecules[0].Metadata.GetVendorField("id"), Is.EqualTo("cmpd-1"));
        Assert.That(read.Molecules[0].Metadata.GetVendorField("comment"), Is.EqualTo("comma, quote \"and\" newline\nnext"));
        Assert.That(read.Molecules[0].Metadata.GetVendorField("empty"), Is.Empty);
        Assert.That(read.Molecules[1].Metadata.GetVendorField("empty"), Is.EqualTo("kept"));
    }

    [Test]
    public void WriteDelimitedVendorRecords_UsesCallerColumnsDelimiterAndQuotePolicy()
    {
        Molecule molecule = BuildMolecule("custom");
        molecule.Metadata.AddVendorField("id", "A");
        molecule.Metadata.AddVendorField("comment", "tab\tvalue");
        molecule.Metadata.AddVendorField("ignored", "not written");
        List<Molecule> molecules = new List<Molecule>();
        molecules.Add(molecule);
        VendorWriteOptions options = new VendorWriteOptions();
        options.Delimiter = '\t';
        options.MoleculeColumn = "structure";
        options.Columns.Add("id");
        options.Columns.Add("structure");
        options.Columns.Add("comment");
        options.IncludeUnlistedVendorFields = false;
        options.QuoteAllFields = true;

        VendorWriteResult written = new DelimitedVendorWriter().WriteDelimitedVendorRecords(molecules, options);
        VendorFormat readFormat = new VendorFormat();
        readFormat.Delimiter = '\t';
        readFormat.MoleculeColumn = "structure";
        VendorReadResult read = new DelimitedVendorReader().ReadWithFormat(written.Text, readFormat);

        Assert.That(written.Success, Is.True);
        Assert.That(written.Columns, Is.EqualTo(new string[] { "id", "structure", "comment" }));
        Assert.That(written.MoleculeColumnIndex, Is.EqualTo(1));
        Assert.That(written.Text, Does.StartWith("\"id\"\t\"structure\"\t\"comment\"\n"));
        Assert.That(read.Success, Is.True);
        Assert.That(read.Molecules[0].Metadata.GetVendorField("id"), Is.EqualTo("A"));
        Assert.That(read.Molecules[0].Metadata.GetVendorField("comment"), Is.EqualTo("tab\tvalue"));
        Assert.That(read.Molecules[0].Metadata.GetVendorField("ignored"), Is.Empty);
    }

    [TestCase(FormatKind.MolV2000)]
    [TestCase(FormatKind.MolV3000)]
    [TestCase(FormatKind.Sdf)]
    [TestCase(FormatKind.Mol2)]
    [TestCase(FormatKind.Pdb)]
    [TestCase(FormatKind.DeterministicText)]
    public void WriteDelimitedVendorRecords_RoundTripsSelectedEmbeddedFormats(FormatKind format)
    {
        Molecule molecule = BuildMolecule("embedded");
        molecule.Metadata.AddVendorField("id", "embedded-1");
        List<Molecule> molecules = new List<Molecule>();
        molecules.Add(molecule);
        VendorWriteOptions options = new VendorWriteOptions();
        options.MoleculeColumn = "structure";
        options.MoleculeFormat = format;
        options.Columns.Add("structure");
        options.Columns.Add("id");
        options.IncludeUnlistedVendorFields = false;

        VendorWriteResult written = new DelimitedVendorWriter().WriteDelimitedVendorRecords(molecules, options);
        VendorFormat readFormat = new VendorFormat();
        readFormat.MoleculeColumn = "structure";
        readFormat.MoleculeFormat = format;
        VendorReadResult read = new DelimitedVendorReader().ReadWithFormat(written.Text, readFormat);

        Assert.That(written.Success, Is.True);
        Assert.That(written.Text, Does.Contain("\""));
        Assert.That(read.Success, Is.True);
        Assert.That(read.Molecules, Has.Count.EqualTo(1));
        Assert.That(read.Molecules[0].Atoms, Has.Count.EqualTo(2));
        Assert.That(read.Molecules[0].Bonds, Has.Count.EqualTo(1));
        Assert.That(read.Molecules[0].Metadata.GetVendorField("id"), Is.EqualTo("embedded-1"));
    }

    [Test]
    public void WriteDelimitedVendorRecords_ReportsInvalidCollectionsColumnsAndFormats()
    {
        DelimitedVendorWriter writer = new DelimitedVendorWriter();
        VendorWriteResult nullResult = writer.WriteDelimitedVendorRecords(null, null);
        List<Molecule> molecules = new List<Molecule>();
        molecules.Add(BuildMolecule("invalid"));
        VendorWriteOptions missingColumnOptions = new VendorWriteOptions();
        missingColumnOptions.MoleculeColumn = "structure";
        missingColumnOptions.Columns.Add("id");
        missingColumnOptions.IncludeUnlistedVendorFields = false;
        VendorWriteResult missingColumn = writer.WriteDelimitedVendorRecords(molecules, missingColumnOptions);
        VendorWriteOptions unsupportedOptions = new VendorWriteOptions();
        unsupportedOptions.MoleculeFormat = FormatKind.InChI;
        VendorWriteResult unsupported = writer.WriteDelimitedVendorRecords(molecules, unsupportedOptions);

        Assert.That(writer.WriteDelimitedVendor(null, null), Is.Empty);
        Assert.That(nullResult.Success, Is.False);
        Assert.That(nullResult.Report.Errors[0].Code, Is.EqualTo("VENDOR_NULL_MOLECULES"));
        Assert.That(missingColumn.Success, Is.False);
        Assert.That(missingColumn.Report.Errors[0].Code, Is.EqualTo("VENDOR_MOLECULE_COLUMN_MISSING"));
        Assert.That(unsupported.Success, Is.False);
        Assert.That(unsupported.Report.Errors[0].Code, Is.EqualTo("VENDOR_MOLECULE_FORMAT_UNSUPPORTED"));
    }

    [Test]
    public void WriteDelimitedVendorRecords_SupportsHeaderlessOutput()
    {
        Molecule molecule = BuildMolecule("headerless");
        molecule.Metadata.AddVendorField("id", "no-header");
        List<Molecule> molecules = new List<Molecule>();
        molecules.Add(molecule);
        VendorWriteOptions options = new VendorWriteOptions();
        options.WriteHeader = false;
        options.Columns.Add("smiles");
        options.Columns.Add("id");
        options.IncludeUnlistedVendorFields = false;

        VendorWriteResult written = new DelimitedVendorWriter().WriteDelimitedVendorRecords(molecules, options);
        VendorFormat readFormat = new VendorFormat();
        readFormat.HasHeader = false;
        readFormat.MoleculeColumn = "field_1";
        VendorReadResult read = new DelimitedVendorReader().ReadWithFormat(written.Text, readFormat);

        Assert.That(written.Success, Is.True);
        Assert.That(written.Text, Does.Not.StartWith("smiles,id"));
        Assert.That(read.Success, Is.True);
        Assert.That(read.Molecules[0].Metadata.GetVendorField("field_2"), Is.EqualTo("no-header"));
    }

    [Test]
    public void MoleculeFormats_AppliesDelimitedVendorWriteOptions()
    {
        MoleculeFormats formats = new MoleculeFormats();
        FormatOptions options = new FormatOptions();
        options.KindHint = FormatKind.VendorDelimited;
        options.VendorWrite.Delimiter = '\t';
        options.VendorWrite.MoleculeColumn = "structure";
        options.VendorWrite.Columns.Add("id");
        options.VendorWrite.Columns.Add("structure");
        options.VendorWrite.IncludeUnlistedVendorFields = false;
        Molecule molecule = BuildMolecule("facade");
        molecule.Metadata.AddVendorField("id", "facade-1");

        string text = formats.WriteString(molecule, options);

        Assert.That(formats.CanWriteMolecule(FormatKind.VendorDelimited), Is.True);
        Assert.That(text, Does.StartWith("id\tstructure\nfacade-1\t"));
    }

    static Molecule BuildMolecule(string name)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        builder.AddBond(carbon, oxygen, new BondSpec());
        Molecule molecule = builder.Build();
        molecule.Metadata.AddName(name);
        molecule.Atoms[carbon].HasCoordinates = true;
        molecule.Atoms[oxygen].HasCoordinates = true;
        molecule.Atoms[oxygen].X = 1.2;
        return molecule;
    }
}
