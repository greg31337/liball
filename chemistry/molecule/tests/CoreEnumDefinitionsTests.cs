using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class CoreEnumDefinitionsTests
{
    [Test]
    public void BondOrder_DefinesStableChemicalOrders()
    {
        AssertEnumValues<BondOrder>(
            new string[] { "Unknown", "None", "Single", "Double", "Triple", "Quadruple", "Aromatic", "Dative" },
            new int[] { 0, 1, 2, 3, 4, 5, 6, 7 });
    }

    [Test]
    public void StereoEnums_DefineStableStereoStates()
    {
        AssertEnumValues<BondDirection>(
            new string[] { "None", "Up", "Down", "Either", "Crossed" },
            new int[] { 0, 1, 2, 3, 4 });

        AssertEnumValues<BondStereo>(
            new string[] { "None", "Wedge", "Hash", "UpOrDown", "Cis", "Trans", "E", "Z", "Unknown" },
            new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 });

        AssertEnumValues<Chirality>(
            new string[] { "None", "Clockwise", "CounterClockwise", "Unspecified", "Unknown" },
            new int[] { 0, 1, 2, 3, 4 });

        AssertEnumValues<DoubleBondConfiguration>(
            new string[] { "None", "E", "Z", "Cis", "Trans", "Unspecified", "Unknown" },
            new int[] { 0, 1, 2, 3, 4, 5, 6 });
    }

    [Test]
    public void PerceptionEnums_DefineStablePerceptionStates()
    {
        AssertEnumValues<Aromaticity>(
            new string[] { "Unknown", "NonAromatic", "Aromatic", "Candidate" },
            new int[] { 0, 1, 2, 3 });

        AssertEnumValues<RadicalState>(
            new string[] { "None", "Singlet", "Doublet", "Triplet", "Quartet" },
            new int[] { 0, 1, 2, 3, 4 });

        AssertEnumValues<Hybridization>(
            new string[] { "Unknown", "S", "Sp", "Sp2", "Sp3", "Sp3d", "Sp3d2" },
            new int[] { 0, 1, 2, 3, 4, 5, 6 });
    }

    [Test]
    public void WorkflowEnums_DefineStablePolicyAndDiagnosticValues()
    {
        AssertEnumValues<FormatKind>(
            new string[] { "Unknown", "Smiles", "Smarts", "MolV2000", "MolV3000", "Sdf", "Mol2", "Pdb", "InChI", "InChIKey", "VendorDelimited", "DeterministicText" },
            new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 });

        AssertEnumValues<QueryOperation>(
            new string[] { "None", "Equals", "NotEquals", "InList", "NotInList", "LessThan", "LessThanOrEqual", "GreaterThan", "GreaterThanOrEqual", "And", "Or", "Not", "Recursive" },
            new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 });

        AssertEnumValues<IdentityPolicy>(
            new string[] { "Exact", "Connectivity", "StereoInsensitive", "ChargeInsensitive", "IsotopeInsensitive", "TautomerInsensitive", "FragmentParent", "ChargeParent", "StereoParent", "TautomerParent", "SuperParent", "IsotopeParent" },
            new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 });

        AssertEnumValues<StandardizationPolicy>(
            new string[] { "None", "Preserve", "Normalize", "Strict", "Relaxed" },
            new int[] { 0, 1, 2, 3, 4 });

        AssertEnumValues<ErrorSeverity>(
            new string[] { "Info", "Warning", "Error", "Fatal" },
            new int[] { 0, 1, 2, 3 });

        AssertEnumValues<ReportSeverity>(
            new string[] { "Info", "Warning", "Error", "Fatal" },
            new int[] { 0, 1, 2, 3 });
    }

    static void AssertEnumValues<TEnum>(string[] expectedNames, int[] expectedValues)
        where TEnum : struct, Enum
    {
        string[] actualNames = Enum.GetNames<TEnum>();
        TEnum[] actualValues = Enum.GetValues<TEnum>();

        Assert.That(actualNames, Is.EqualTo(expectedNames));
        Assert.That(actualValues.Length, Is.EqualTo(expectedValues.Length));

        for (int i = 0; i < expectedValues.Length; i++)
        {
            int actualValue = Convert.ToInt32(actualValues[i]);
            Assert.That(actualValue, Is.EqualTo(expectedValues[i]), expectedNames[i]);
        }
    }
}
