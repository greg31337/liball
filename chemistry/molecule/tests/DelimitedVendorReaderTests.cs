using System.Collections.Generic;
using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class DelimitedVendorReaderTests
{
    [Test]
    public void ReadWithResult_ReadsCsvSmilesAndPreservesVendorFields()
    {
        DelimitedVendorReader reader = new DelimitedVendorReader();
        string text = "id,smiles,activity,comment\n"
            + "cmpd-1,CCO,7.2,\"quoted, note\"\n"
            + "cmpd-2,CC(=O)N,5.1,line two\n";

        VendorReadResult result = reader.ReadWithResult(text);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Report.GetErrorCount(), Is.EqualTo(0));
        Assert.That(result.Molecules.Count, Is.EqualTo(2));
        Assert.That(result.Molecules[0].Atoms.Count, Is.EqualTo(3));
        Assert.That(result.Molecules[1].Atoms.Count, Is.EqualTo(4));
        Assert.That(result.Molecules[0].Metadata.GetVendorField("id"), Is.EqualTo("cmpd-1"));
        Assert.That(result.Molecules[0].Metadata.GetVendorField("smiles"), Is.EqualTo("CCO"));
        Assert.That(result.Molecules[0].Metadata.GetVendorField("activity"), Is.EqualTo("7.2"));
        Assert.That(result.Molecules[0].Metadata.GetVendorField("comment"), Is.EqualTo("quoted, note"));
        Assert.That(result.Molecules[1].Metadata.GetVendorField("_vendor_row"), Is.EqualTo("2"));
        Assert.That(result.Metadata.Headers, Is.EqualTo(new string[] { "id", "smiles", "activity", "comment" }));
        Assert.That(result.Metadata.MoleculeColumnIndex, Is.EqualTo(1));
        Assert.That(result.Metadata.GetRowCount(), Is.EqualTo(2));
        Assert.That(result.Metadata.GetValue(0, 3), Is.EqualTo("quoted, note"));
    }

    [Test]
    public void ReadWithFormat_ReadsTsvAndCustomMoleculeColumn()
    {
        DelimitedVendorReader reader = new DelimitedVendorReader();
        VendorFormat format = new VendorFormat();
        format.UseTsv();
        format.MoleculeColumn = "structure";
        string text = "compound\tstructure\tsupplier\n"
            + "A\tCO\tvendor-a\n"
            + "B\tCCN\tvendor-b\n";

        VendorReadResult result = reader.ReadWithFormat(text, format);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Molecules.Count, Is.EqualTo(2));
        Assert.That(result.Molecules[0].Atoms.Count, Is.EqualTo(2));
        Assert.That(result.Molecules[1].Atoms.Count, Is.EqualTo(3));
        Assert.That(result.Metadata.MoleculeColumnIndex, Is.EqualTo(1));
        Assert.That(result.Molecules[1].Metadata.GetVendorField("compound"), Is.EqualTo("B"));
        Assert.That(result.Molecules[1].Metadata.GetVendorField("supplier"), Is.EqualTo("vendor-b"));
    }

    [Test]
    public void ReadWithFormat_GeneratesHeadersForHeaderlessData()
    {
        DelimitedVendorReader reader = new DelimitedVendorReader();
        VendorFormat format = new VendorFormat();
        format.HasHeader = false;
        format.MoleculeColumn = "field_2";
        string text = "cmpd-1,CCO,7.2\n";

        VendorReadResult result = reader.ReadWithFormat(text, format);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.Headers, Is.EqualTo(new string[] { "field_1", "field_2", "field_3" }));
        Assert.That(result.Molecules.Count, Is.EqualTo(1));
        Assert.That(result.Molecules[0].Metadata.GetVendorField("field_1"), Is.EqualTo("cmpd-1"));
        Assert.That(result.Molecules[0].Metadata.GetVendorField("field_2"), Is.EqualTo("CCO"));
    }

    [Test]
    public void ReadWithResult_ReportsInvalidSmilesPerRowButKeepsMetadata()
    {
        DelimitedVendorReader reader = new DelimitedVendorReader();
        string text = "id,smiles\n"
            + "bad,C1\n";

        VendorReadResult result = reader.ReadWithResult(text);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("SMILES_PARSE_ERROR"));
        Assert.That(result.Report.Get(0).Location.LineNumber, Is.EqualTo(2));
        Assert.That(result.Molecules.Count, Is.EqualTo(1));
        Assert.That(result.Molecules[0].Metadata.GetVendorField("id"), Is.EqualTo("bad"));
        Assert.That(result.Molecules[0].Metadata.GetVendorField("smiles"), Is.EqualTo("C1"));
    }

    [Test]
    public void MoleculeFormats_ReadString_ReadsDetectedVendorDelimitedFirstMolecule()
    {
        MoleculeFormats formats = new MoleculeFormats();
        FormatOptions options = new FormatOptions();
        options.FileName = "compounds.csv";
        string text = "id,smiles\n"
            + "cmpd-1,CCO\n"
            + "cmpd-2,CCN\n";

        Molecule molecule = formats.ReadString(text, options);

        Assert.That(formats.CanReadMolecule(FormatKind.VendorDelimited), Is.True);
        Assert.That(formats.CanWriteMolecule(FormatKind.VendorDelimited), Is.True);
        Assert.That(molecule.Atoms.Count, Is.EqualTo(3));
        Assert.That(molecule.Metadata.GetVendorField("id"), Is.EqualTo("cmpd-1"));
    }

    [Test]
    public void VendorFormat_DefaultsToCsvSmilesWithHeader()
    {
        VendorFormat format = new VendorFormat();

        Assert.That(format.Name, Is.EqualTo("Delimited"));
        Assert.That(format.GetSafeDelimiter(), Is.EqualTo(','));
        Assert.That(format.HasHeader, Is.True);
        Assert.That(format.GetSafeMoleculeColumn(), Is.EqualTo("smiles"));
        Assert.That(format.MoleculeFormat, Is.EqualTo(FormatKind.Smiles));
        Assert.That(format.PreserveMoleculeColumn, Is.True);
    }

    [Test]
    public void ParseLine_UnescapesQuotedDoubleQuotes()
    {
        DelimitedVendorReader reader = new DelimitedVendorReader();

        List<string> fields = reader.ParseLine("cmpd-1,\"has \"\"quote\"\"\",CCO", ',');

        Assert.That(fields, Is.EqualTo(new string[] { "cmpd-1", "has \"quote\"", "CCO" }));
    }

    [Test]
    public void ReadWithResult_ReportsEmptyInputAndHeaderOnlyInput()
    {
        DelimitedVendorReader reader = new DelimitedVendorReader();

        VendorReadResult empty = reader.ReadWithResult(null);
        VendorReadResult headerOnly = reader.ReadWithResult("id,smiles\n");

        Assert.That(empty.Success, Is.False);
        Assert.That(empty.Report.Get(0).Code, Is.EqualTo("VENDOR_EMPTY_INPUT"));
        Assert.That(headerOnly.Success, Is.False);
        Assert.That(headerOnly.Report.Get(0).Code, Is.EqualTo("VENDOR_NO_RECORDS"));
        Assert.That(headerOnly.Metadata.Headers, Is.EqualTo(new string[] { "id", "smiles" }));
    }

    [Test]
    public void ReadWithFormat_ReportsEmptyAndUnsupportedMoleculeValuesPerRow()
    {
        DelimitedVendorReader reader = new DelimitedVendorReader();
        VendorReadResult emptyValue = reader.ReadWithResult("id,smiles\nmissing,\n");
        VendorFormat unsupportedFormat = new VendorFormat();
        unsupportedFormat.MoleculeFormat = FormatKind.InChI;
        VendorReadResult unsupported = reader.ReadWithFormat("id,smiles\nwater,InChI=1S/H2O/h1H2\n", unsupportedFormat);

        Assert.That(emptyValue.Success, Is.False);
        Assert.That(emptyValue.Molecules, Has.Count.EqualTo(1));
        Assert.That(emptyValue.Report.Get(0).Code, Is.EqualTo("VENDOR_MOLECULE_EMPTY"));
        Assert.That(emptyValue.Report.Get(0).Location.LineNumber, Is.EqualTo(2));
        Assert.That(unsupported.Success, Is.False);
        Assert.That(unsupported.Molecules, Has.Count.EqualTo(1));
        Assert.That(unsupported.Report.Get(0).Code, Is.EqualTo("VENDOR_MOLECULE_FORMAT_UNSUPPORTED"));
    }
}
