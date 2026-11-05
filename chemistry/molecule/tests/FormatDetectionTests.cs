using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class FormatDetectionTests
{
    [Test]
    public void Detect_UsesCallerHintBeforeTextOrExtension()
    {
        FormatDetection detector = new FormatDetection();

        FormatDetectionResult result = detector.DetectWithHint("C", "molecule.sdf", FormatKind.Smarts);

        Assert.That(result.Kind, Is.EqualTo(FormatKind.Smarts));
        Assert.That(result.Confidence, Is.EqualTo(100));
        Assert.That(result.Reason, Is.EqualTo("caller hint"));
    }

    [Test]
    public void Detect_UsesMagicTextBeforeExtension()
    {
        FormatDetection detector = new FormatDetection();

        FormatDetectionResult result = detector.Detect(BuildV3000Text(), "molecule.mol");

        Assert.That(result.Kind, Is.EqualTo(FormatKind.MolV3000));
        Assert.That(result.Reason, Is.EqualTo("MOL V3000 marker"));
    }

    [Test]
    public void Detect_RecognizesCommonExtensions()
    {
        FormatDetection detector = new FormatDetection();

        Assert.That(detector.Detect("", "hits.sdf").Kind, Is.EqualTo(FormatKind.Sdf));
        Assert.That(detector.Detect("", "query.smarts").Kind, Is.EqualTo(FormatKind.Smarts));
        Assert.That(detector.Detect("", "ligands.MOL2").Kind, Is.EqualTo(FormatKind.Mol2));
        Assert.That(detector.Detect("", "protein.pdb").Kind, Is.EqualTo(FormatKind.Pdb));
        Assert.That(detector.Detect("", "table.csv").Kind, Is.EqualTo(FormatKind.VendorDelimited));
    }

    [Test]
    public void Detect_RecognizesEveryExtensionAliasCaseInsensitively()
    {
        FormatDetection detector = new FormatDetection();

        Assert.That(detector.Detect(null, "records.sd").Kind, Is.EqualTo(FormatKind.Sdf));
        Assert.That(detector.Detect(null, "structure.MOL").Kind, Is.EqualTo(FormatKind.MolV2000));
        Assert.That(detector.Detect(null, "protein.ent").Kind, Is.EqualTo(FormatKind.Pdb));
        Assert.That(detector.Detect(null, "molecules.smi").Kind, Is.EqualTo(FormatKind.Smiles));
        Assert.That(detector.Detect(null, "molecules.smiles").Kind, Is.EqualTo(FormatKind.Smiles));
        Assert.That(detector.Detect(null, "query.sma").Kind, Is.EqualTo(FormatKind.Smarts));
        Assert.That(detector.Detect(null, "identifier.inchi").Kind, Is.EqualTo(FormatKind.InChI));
        Assert.That(detector.Detect(null, "identifier.inchikey").Kind, Is.EqualTo(FormatKind.InChIKey));
        Assert.That(detector.Detect(null, "vendor.tsv").Kind, Is.EqualTo(FormatKind.VendorDelimited));
    }

    [Test]
    public void Detect_RecognizesMagicTextFormats()
    {
        FormatDetection detector = new FormatDetection();

        Assert.That(detector.Detect("@<TRIPOS>MOLECULE\nname\n", "").Kind, Is.EqualTo(FormatKind.Mol2));
        Assert.That(detector.Detect("HEADER    TEST\nATOM      1", "").Kind, Is.EqualTo(FormatKind.Pdb));
        Assert.That(detector.Detect("InChI=1S/H2O/h1H2", "").Kind, Is.EqualTo(FormatKind.InChI));
        Assert.That(detector.Detect("BSYNRYMUTXBXSQ-UHFFFAOYSA-N", "").Kind, Is.EqualTo(FormatKind.InChIKey));
        Assert.That(detector.Detect("MOLDET1\nATOMS\t0\nBONDS\t0\nEND\n", "").Kind, Is.EqualTo(FormatKind.DeterministicText));
    }

    [Test]
    public void Detect_DistinguishesSdfFromMolByRecordSeparator()
    {
        FormatDetection detector = new FormatDetection();

        FormatDetectionResult sdf = detector.Detect("name\n\n\n  0  0  0  0  0  0            999 V2000\nM  END\n$$$$\n", "");
        FormatDetectionResult mol = detector.Detect("name\n\n\n  0  0  0  0  0  0            999 V2000\nM  END\n", "");

        Assert.That(sdf.Kind, Is.EqualTo(FormatKind.Sdf));
        Assert.That(mol.Kind, Is.EqualTo(FormatKind.MolV2000));
    }

    [Test]
    public void Detect_UsesContentHeuristicsForSmilesSmartsAndDelimitedText()
    {
        FormatDetection detector = new FormatDetection();

        Assert.That(detector.Detect("CC(=O)N", "").Kind, Is.EqualTo(FormatKind.Smiles));
        Assert.That(detector.Detect("[#6;D3]=O", "").Kind, Is.EqualTo(FormatKind.Smarts));
        Assert.That(detector.Detect("id,smiles\n1,CCO\n", "").Kind, Is.EqualTo(FormatKind.VendorDelimited));
    }

    [Test]
    public void Detect_ReturnsUnknownForEmptyInputWithoutExtension()
    {
        FormatDetection detector = new FormatDetection();

        FormatDetectionResult result = detector.Detect("", "");

        Assert.That(result.Kind, Is.EqualTo(FormatKind.Unknown));
        Assert.That(result.Confidence, Is.EqualTo(0));
    }

    [Test]
    public void Detect_NormalizesNullTextAndFileName()
    {
        FormatDetectionResult result = new FormatDetection().Detect(null, null);

        Assert.That(result.Kind, Is.EqualTo(FormatKind.Unknown));
        Assert.That(result.Confidence, Is.Zero);
        Assert.That(result.Reason, Is.Empty);
    }

    static string BuildV3000Text()
    {
        return string.Join(
            "\n",
            new string[]
            {
                "v3000",
                "",
                "",
                "  0  0  0  0  0  0            999 V3000",
                "M  V30 BEGIN CTAB",
                "M  V30 COUNTS 0 0 0 0 0",
                "M  V30 END CTAB",
                "M  END"
            });
    }
}
