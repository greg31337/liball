using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class MoleculeMetadataTests
{
    [Test]
    public void MetadataEntry_CreateEmpty_InitializesEmptyText()
    {
        MoleculeMetadataEntry entry = MoleculeMetadataEntry.CreateEmpty();

        Assert.That(entry.Key, Is.EqualTo(""));
        Assert.That(entry.Value, Is.EqualTo(""));
    }

    [Test]
    public void Constructor_InitializesEmptyMetadataLists()
    {
        MoleculeMetadata metadata = new MoleculeMetadata();

        Assert.That(metadata.Names, Is.Empty);
        Assert.That(metadata.Comments, Is.Empty);
        Assert.That(metadata.SdfFields, Is.Empty);
        Assert.That(metadata.PdbFields, Is.Empty);
        Assert.That(metadata.VendorFields, Is.Empty);
        Assert.That(metadata.StringValues, Is.Empty);
        Assert.That(metadata.GetPrimaryName(), Is.EqualTo(""));
        Assert.That(metadata.GetStringValue("missing"), Is.EqualTo(""));
        Assert.That(metadata.HasStringValue("missing"), Is.False);
    }

    [Test]
    public void NamesAndComments_PreserveInsertionOrder()
    {
        MoleculeMetadata metadata = new MoleculeMetadata();

        metadata.AddName("water");
        metadata.AddName("oxidane");
        metadata.AddComment("first");
        metadata.AddComment("second");

        Assert.That(metadata.GetPrimaryName(), Is.EqualTo("water"));
        Assert.That(metadata.Names, Is.EqualTo(new string[] { "water", "oxidane" }));
        Assert.That(metadata.Comments, Is.EqualTo(new string[] { "first", "second" }));
    }

    [Test]
    public void FormatFields_StoreDuplicateKeysAndReturnAllValues()
    {
        MoleculeMetadata metadata = new MoleculeMetadata();

        metadata.AddSdfField("PUBCHEM_ID", "1");
        metadata.AddSdfField("PUBCHEM_ID", "2");
        metadata.AddPdbField("HEADER", "LIGAND");
        metadata.AddVendorField("vendor:score", "0.42");

        Assert.That(metadata.GetSdfField("PUBCHEM_ID"), Is.EqualTo("1"));
        Assert.That(metadata.GetSdfFieldValues("PUBCHEM_ID"), Is.EqualTo(new string[] { "1", "2" }));
        Assert.That(metadata.GetPdbField("HEADER"), Is.EqualTo("LIGAND"));
        Assert.That(metadata.GetVendorField("vendor:score"), Is.EqualTo("0.42"));
        Assert.That(metadata.SdfFields[0].Key, Is.EqualTo("PUBCHEM_ID"));
        Assert.That(metadata.SdfFields[1].Value, Is.EqualTo("2"));
    }

    [Test]
    public void StringValues_UpdateExistingKeys()
    {
        MoleculeMetadata metadata = new MoleculeMetadata();

        metadata.SetStringValue("source", "smiles");
        metadata.SetStringValue("source", "sdf");
        metadata.SetStringValue("owner", "lab");

        Assert.That(metadata.HasStringValue("source"), Is.True);
        Assert.That(metadata.GetStringValue("source"), Is.EqualTo("sdf"));
        Assert.That(metadata.GetStringValue("owner"), Is.EqualTo("lab"));
        Assert.That(metadata.StringValues.Count, Is.EqualTo(2));
    }

    [Test]
    public void MetadataEntry_NormalizesNullTextToEmptyStrings()
    {
        MoleculeMetadataEntry entry = new MoleculeMetadataEntry(null, null);
        MoleculeMetadata metadata = new MoleculeMetadata();

        metadata.AddName(null);
        metadata.AddSdfField(null, null);
        metadata.SetStringValue(null, null);

        Assert.That(entry.Key, Is.EqualTo(""));
        Assert.That(entry.Value, Is.EqualTo(""));
        Assert.That(metadata.Names[0], Is.EqualTo(""));
        Assert.That(metadata.SdfFields[0].Key, Is.EqualTo(""));
        Assert.That(metadata.SdfFields[0].Value, Is.EqualTo(""));
        Assert.That(metadata.GetStringValue(""), Is.EqualTo(""));
        Assert.That(metadata.HasStringValue(""), Is.True);
    }

    [Test]
    public void Clone_CopiesMetadataWithoutSharingEntryObjects()
    {
        MoleculeMetadata metadata = new MoleculeMetadata();
        metadata.AddName("ethanol");
        metadata.AddComment("liquid");
        metadata.AddSdfField("ID", "10");
        metadata.AddPdbField("CHAIN", "A");
        metadata.AddVendorField("vendor", "x");
        metadata.SetStringValue("source", "manual");

        MoleculeMetadata clone = metadata.Clone();
        metadata.Names[0] = "changed";
        metadata.SdfFields[0].Value = "11";
        metadata.SetStringValue("source", "changed");

        Assert.That(clone.Names[0], Is.EqualTo("ethanol"));
        Assert.That(clone.Comments[0], Is.EqualTo("liquid"));
        Assert.That(clone.SdfFields[0].Value, Is.EqualTo("10"));
        Assert.That(clone.PdbFields[0].Value, Is.EqualTo("A"));
        Assert.That(clone.VendorFields[0].Value, Is.EqualTo("x"));
        Assert.That(clone.GetStringValue("source"), Is.EqualTo("manual"));
        Assert.That(clone.SdfFields[0], Is.Not.SameAs(metadata.SdfFields[0]));
    }

    [Test]
    public void MoleculeBuilderClone_CopiesMetadata()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.Molecule.Metadata.AddName("benzene");
        builder.Molecule.Metadata.AddSdfField("ID", "benzene-1");

        Molecule clone = builder.Clone();
        builder.Molecule.Metadata.AddName("changed");
        builder.Molecule.Metadata.SdfFields[0].Value = "changed";

        Assert.That(clone.Metadata.GetPrimaryName(), Is.EqualTo("benzene"));
        Assert.That(clone.Metadata.GetSdfField("ID"), Is.EqualTo("benzene-1"));
        Assert.That(clone.Metadata.SdfFields[0], Is.Not.SameAs(builder.Molecule.Metadata.SdfFields[0]));
    }
}
