using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class InChIWorkflowMetadataTests
{
    [Test]
    public void AttachInChI_StoresOriginalTextAndParsedLayers()
    {
        InChIWorkflows workflows = new InChIWorkflows();
        Molecule molecule = new Molecule();
        string text = "InChI=1S/C2H6O/c1-2-3/h3H,2H2,1H3";

        bool attached = workflows.AttachInChI(molecule, text);

        Assert.That(attached, Is.True);
        Assert.That(molecule.Metadata.GetStringValue("INCHI"), Is.EqualTo(text));
        Assert.That(molecule.Metadata.GetStringValue("INCHI_VERSION"), Is.EqualTo("1"));
        Assert.That(molecule.Metadata.GetStringValue("INCHI_STANDARD"), Is.EqualTo("true"));
        Assert.That(molecule.Metadata.GetStringValue("INCHI_FORMULA"), Is.EqualTo("C2H6O"));
        Assert.That(molecule.Metadata.GetStringValue("INCHI_CONNECTIVITY"), Is.EqualTo("1-2-3"));
        Assert.That(molecule.Metadata.GetStringValue("INCHI_HYDROGEN"), Is.EqualTo("3H,2H2,1H3"));
        Assert.That(workflows.ExportInChI(molecule), Is.EqualTo(text));
    }

    [Test]
    public void AttachInChIKey_StoresOriginalTextAndParsedBlocks()
    {
        InChIWorkflows workflows = new InChIWorkflows();
        Molecule molecule = new Molecule();

        bool attached = workflows.AttachInChIKey(molecule, "  XLYOFNOQVPJJNP-UHFFFAOYSA-N\n");

        Assert.That(attached, Is.True);
        Assert.That(molecule.Metadata.GetStringValue("INCHI_KEY"), Is.EqualTo("XLYOFNOQVPJJNP-UHFFFAOYSA-N"));
        Assert.That(molecule.Metadata.GetStringValue("INCHI_KEY_FIRST_BLOCK"), Is.EqualTo("XLYOFNOQVPJJNP"));
        Assert.That(molecule.Metadata.GetStringValue("INCHI_KEY_SECOND_BLOCK"), Is.EqualTo("UHFFFAOYSA"));
        Assert.That(molecule.Metadata.GetStringValue("INCHI_KEY_REMAINING_HASH"), Is.EqualTo("UHFFFAOY"));
        Assert.That(molecule.Metadata.GetStringValue("INCHI_KEY_VERSION"), Is.EqualTo("SA"));
        Assert.That(molecule.Metadata.GetStringValue("INCHI_KEY_PROTONATION"), Is.EqualTo("N"));
        Assert.That(workflows.ExportInChIKey(molecule), Is.EqualTo("XLYOFNOQVPJJNP-UHFFFAOYSA-N"));
    }

    [Test]
    public void AttachIdentifiers_ValidatesBothIdentifiersBeforeMutatingMetadata()
    {
        InChIWorkflows workflows = new InChIWorkflows();
        Molecule molecule = new Molecule();

        bool attached = workflows.AttachIdentifiers(molecule, "InChI=1S/H2O/h1H2", "bad-key");

        Assert.That(attached, Is.False);
        Assert.That(molecule.Metadata.HasStringValue("INCHI"), Is.False);
        Assert.That(molecule.Metadata.HasStringValue("INCHI_KEY"), Is.False);
    }

    [Test]
    public void AttachIdentifiers_StoresBothIdentifiersWhenValid()
    {
        InChIWorkflows workflows = new InChIWorkflows();
        Molecule molecule = new Molecule();

        bool attached = workflows.AttachIdentifiers(molecule, "InChI=1S/H2O/h1H2", "XLYOFNOQVPJJNP-UHFFFAOYSA-N");

        Assert.That(attached, Is.True);
        Assert.That(workflows.ExportInChI(molecule), Is.EqualTo("InChI=1S/H2O/h1H2"));
        Assert.That(workflows.ExportInChIKey(molecule), Is.EqualTo("XLYOFNOQVPJJNP-UHFFFAOYSA-N"));
        Assert.That(workflows.GetInChIRecord(molecule).FormulaLayer, Is.EqualTo("H2O"));
        Assert.That(workflows.GetInChIKeyRecord(molecule).FirstBlock, Is.EqualTo("XLYOFNOQVPJJNP"));
    }

    [Test]
    public void Export_ReadsSdfAndVendorIdentifierFields()
    {
        InChIWorkflows workflows = new InChIWorkflows();
        Molecule sdfMolecule = new Molecule();
        Molecule vendorMolecule = new Molecule();

        sdfMolecule.Metadata.AddSdfField("InChI", "InChI=1S/H2O/h1H2");
        vendorMolecule.Metadata.AddVendorField("InChIKey", "XLYOFNOQVPJJNP-UHFFFAOYSA-N");

        Assert.That(workflows.ExportInChI(sdfMolecule), Is.EqualTo("InChI=1S/H2O/h1H2"));
        Assert.That(workflows.ExportInChIKey(vendorMolecule), Is.EqualTo("XLYOFNOQVPJJNP-UHFFFAOYSA-N"));
    }

    [Test]
    public void MoleculeFormats_ReadWriteInChIAndInChIKeyAsMetadata()
    {
        MoleculeFormats formats = new MoleculeFormats();
        FormatOptions inchiOptions = new FormatOptions();
        FormatOptions keyOptions = new FormatOptions();
        inchiOptions.KindHint = FormatKind.InChI;
        keyOptions.KindHint = FormatKind.InChIKey;

        Molecule inchiMolecule = formats.ReadString("InChI=1S/H2O/h1H2", inchiOptions);
        Molecule keyMolecule = formats.ReadString("XLYOFNOQVPJJNP-UHFFFAOYSA-N", keyOptions);

        Assert.That(formats.CanReadMolecule(FormatKind.InChI), Is.True);
        Assert.That(formats.CanWriteMolecule(FormatKind.InChI), Is.True);
        Assert.That(formats.CanReadMolecule(FormatKind.InChIKey), Is.True);
        Assert.That(formats.CanWriteMolecule(FormatKind.InChIKey), Is.True);
        Assert.That(inchiMolecule.Metadata.GetStringValue("INCHI_FORMULA"), Is.EqualTo("H2O"));
        Assert.That(keyMolecule.Metadata.GetStringValue("INCHI_KEY_PROTONATION"), Is.EqualTo("N"));
        Assert.That(formats.WriteString(inchiMolecule, inchiOptions), Is.EqualTo("InChI=1S/H2O/h1H2"));
        Assert.That(formats.WriteString(keyMolecule, keyOptions), Is.EqualTo("XLYOFNOQVPJJNP-UHFFFAOYSA-N"));
    }
}
