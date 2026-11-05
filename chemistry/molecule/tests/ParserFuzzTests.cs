using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class ParserFuzzTests
{
    [Test]
    public void GeneratedSmilesStrings_ParseSuccessfully()
    {
        SmilesParser parser = new SmilesParser();
        int seed = 0;
        while (seed < 40)
        {
            string smiles = BuildGeneratedSmiles(seed);

            SmilesParseResult result = parser.ParseWithResult(smiles);

            Assert.That(result.Success, Is.True, smiles);
            Assert.That(result.Molecule.Atoms.Count, Is.GreaterThan(0), smiles);
            seed = seed + 1;
        }
    }

    [Test]
    public void GeneratedMalformedSmilesStrings_ReturnDiagnostics()
    {
        SmilesParser parser = new SmilesParser();
        int seed = 0;
        while (seed < 20)
        {
            string smiles = BuildMalformedSmiles(seed);

            SmilesParseResult result = parser.ParseWithResult(smiles);

            Assert.That(result.Success, Is.False, smiles);
            Assert.That(result.Report.GetErrorCount(), Is.GreaterThan(0), smiles);
            seed = seed + 1;
        }
    }

    [Test]
    public void GeneratedSmartsStrings_ParseSuccessfully()
    {
        SmartsParser parser = new SmartsParser();
        int seed = 0;
        while (seed < 40)
        {
            string smarts = BuildGeneratedSmarts(seed);

            SmartsParseResult result = parser.ParseWithResult(smarts);

            Assert.That(result.Success, Is.True, smarts);
            Assert.That(result.Query.GetAtomCount(), Is.GreaterThan(0), smarts);
            seed = seed + 1;
        }
    }

    [Test]
    public void GeneratedMalformedSmartsStrings_ReturnDiagnostics()
    {
        SmartsParser parser = new SmartsParser();
        int seed = 0;
        while (seed < 20)
        {
            string smarts = BuildMalformedSmarts(seed);

            SmartsParseResult result = parser.ParseWithResult(smarts);

            Assert.That(result.Success, Is.False, smarts);
            Assert.That(result.Report.GetErrorCount(), Is.GreaterThan(0), smarts);
            seed = seed + 1;
        }
    }

    [Test]
    public void OversizedNumericTokens_ReturnDiagnosticsWithoutOverflow()
    {
        SmilesParseResult smiles = new SmilesParser().ParseWithResult("[999999999999999999999C]");
        SmilesParseResult recoveredSmiles = new SmilesParser().ParseWithRecovery("C%999999999999999999999CC");
        SmartsParseResult smarts = new SmartsParser().ParseWithResult("[#999999999999999999999]");

        Assert.That(smiles.Success, Is.False);
        Assert.That(smiles.ErrorMessage, Does.Contain("out of range"));
        Assert.That(recoveredSmiles.Success, Is.True);
        Assert.That(recoveredSmiles.Recovered, Is.True);
        Assert.That(smarts.Success, Is.False);
        Assert.That(smarts.ErrorMessage, Does.Contain("out of range"));
    }

    [Test]
    public void GeneratedMolAndSdfStrings_ParseSuccessfully()
    {
        V2000Reader molReader = new V2000Reader();
        SdfReader sdfReader = new SdfReader();
        int seed = 1;
        while (seed <= 8)
        {
            string molfile = BuildGeneratedMolfile(seed);
            string sdf = molfile + "\n>  <FUZZ_SEED>\n" + seed.ToString() + "\n\n$$$$\n";

            MolfileReadResult molResult = molReader.ReadWithResult(molfile);
            List<SdfRecord> records = sdfReader.ReadRecords(sdf);

            Assert.That(molResult.Success, Is.True, molfile);
            Assert.That(molResult.Molecule.Atoms.Count, Is.EqualTo(seed), molfile);
            Assert.That(records.Count, Is.EqualTo(1), sdf);
            Assert.That(records[0].Success, Is.True, sdf);
            Assert.That(records[0].Molecule.Metadata.GetSdfField("FUZZ_SEED"), Is.EqualTo(seed.ToString()), sdf);
            seed = seed + 1;
        }
    }

    [Test]
    public void GeneratedMol2Strings_ParseSuccessfully()
    {
        Mol2Reader reader = new Mol2Reader();
        int seed = 1;
        while (seed <= 8)
        {
            string mol2 = BuildGeneratedMol2(seed);

            Mol2ReadResult result = reader.ReadWithResult(mol2);

            Assert.That(result.Success, Is.True, mol2);
            Assert.That(result.Molecule.Atoms.Count, Is.EqualTo(seed), mol2);
            Assert.That(result.Molecule.Bonds.Count, Is.EqualTo(seed - 1), mol2);
            seed = seed + 1;
        }
    }

    [Test]
    public void GeneratedPdbStrings_ParseSuccessfully()
    {
        PdbReader reader = new PdbReader();
        int seed = 1;
        while (seed <= 8)
        {
            string pdb = BuildGeneratedPdb(seed);

            PdbReadResult result = reader.ReadWithResult(pdb);

            Assert.That(result.Success, Is.True, pdb);
            Assert.That(result.Molecule.Atoms.Count, Is.EqualTo(seed), pdb);
            Assert.That(result.Molecule.Atoms[0].HasCoordinates, Is.True, pdb);
            seed = seed + 1;
        }
    }

    static string BuildGeneratedSmiles(int seed)
    {
        int mode = seed % 5;
        if (mode == 0)
        {
            return "C" + RepeatAtom("C", seed % 4);
        }

        if (mode == 1)
        {
            return "C(=O)" + GetSmilesAtom(seed);
        }

        if (mode == 2)
        {
            return "C(" + GetSmilesAtom(seed) + ")" + GetSmilesAtom(seed + 1);
        }

        if (mode == 3)
        {
            return "C1" + RepeatAtom("C", 2 + seed % 3) + "1";
        }

        return "[13CH3+]" + GetSmilesAtom(seed);
    }

    static string BuildMalformedSmiles(int seed)
    {
        int mode = seed % 4;
        if (mode == 0)
        {
            return "C1" + RepeatAtom("C", seed % 3);
        }

        if (mode == 1)
        {
            return "C(" + GetSmilesAtom(seed);
        }

        if (mode == 2)
        {
            return "X" + RepeatAtom("C", seed % 3);
        }

        return "[13C" + RepeatAtom("H", seed % 2);
    }

    static string BuildGeneratedSmarts(int seed)
    {
        int mode = seed % 5;
        if (mode == 0)
        {
            return "[#" + GetAtomicNumberText(seed) + "]";
        }

        if (mode == 1)
        {
            return "[#6]-[#" + GetAtomicNumberText(seed) + "]";
        }

        if (mode == 2)
        {
            return "[#6](~[#8])~[#7]";
        }

        if (mode == 3)
        {
            return "[#6]1-[#6]-[#6]-1";
        }

        return "[#8-]~*";
    }

    static string BuildMalformedSmarts(int seed)
    {
        int mode = seed % 4;
        if (mode == 0)
        {
            return "X" + RepeatAtom("C", seed % 2);
        }

        if (mode == 1)
        {
            return "[#6";
        }

        if (mode == 2)
        {
            return "C1CC";
        }

        return "[#]";
    }

    static string BuildGeneratedMolfile(int atomCount)
    {
        int bondCount = atomCount - 1;
        List<string> lines = new List<string>();
        lines.Add("Generated " + atomCount.ToString());
        lines.Add("  fuzz tests");
        lines.Add("");
        lines.Add(atomCount.ToString().PadLeft(3) + bondCount.ToString().PadLeft(3) + "  0  0  0  0            999 V2000");

        int atomIndex = 0;
        while (atomIndex < atomCount)
        {
            lines.Add(FormatMolAtomLine((double)atomIndex * 1.5, 0.0, 0.0, GetMolSymbol(atomIndex)));
            atomIndex = atomIndex + 1;
        }

        int bondIndex = 1;
        while (bondIndex < atomCount)
        {
            lines.Add(bondIndex.ToString().PadLeft(3) + (bondIndex + 1).ToString().PadLeft(3) + "  1  0  0  0  0");
            bondIndex = bondIndex + 1;
        }

        lines.Add("M  END");
        return string.Join("\n", lines);
    }

    static string BuildGeneratedMol2(int atomCount)
    {
        int bondCount = atomCount - 1;
        List<string> lines = new List<string>();
        lines.Add("@<TRIPOS>MOLECULE");
        lines.Add("GeneratedMol2" + atomCount.ToString());
        lines.Add(atomCount.ToString() + " " + bondCount.ToString() + " 0 0 0");
        lines.Add("SMALL");
        lines.Add("NO_CHARGES");
        lines.Add("@<TRIPOS>ATOM");

        int atomIndex = 1;
        while (atomIndex <= atomCount)
        {
            string symbol = GetMolSymbol(atomIndex - 1);
            lines.Add(atomIndex.ToString() + " " + symbol + atomIndex.ToString() + " " + FormatNumber((double)(atomIndex - 1) * 1.5) + " 0.0 0.0 " + GetMol2Type(symbol));
            atomIndex = atomIndex + 1;
        }

        lines.Add("@<TRIPOS>BOND");
        int bondIndex = 1;
        while (bondIndex <= bondCount)
        {
            lines.Add(bondIndex.ToString() + " " + bondIndex.ToString() + " " + (bondIndex + 1).ToString() + " 1");
            bondIndex = bondIndex + 1;
        }

        lines.Add("");
        return string.Join("\n", lines);
    }

    static string BuildGeneratedPdb(int atomCount)
    {
        List<string> lines = new List<string>();
        lines.Add("HEADER    GENERATED PDB");
        int atomIndex = 1;
        while (atomIndex <= atomCount)
        {
            string symbol = GetMolSymbol(atomIndex - 1);
            lines.Add(PdbAtomLine(atomIndex, symbol, (double)(atomIndex - 1) * 1.5, 0.0, 0.0));
            atomIndex = atomIndex + 1;
        }

        lines.Add("END");
        lines.Add("");
        return string.Join("\n", lines);
    }

    static string RepeatAtom(string atom, int count)
    {
        string text = "";
        int index = 0;
        while (index < count)
        {
            text = text + atom;
            index = index + 1;
        }

        return text;
    }

    static string GetSmilesAtom(int seed)
    {
        int index = seed % 4;
        if (index == 0)
        {
            return "C";
        }

        if (index == 1)
        {
            return "N";
        }

        if (index == 2)
        {
            return "O";
        }

        return "Cl";
    }

    static string GetAtomicNumberText(int seed)
    {
        int index = seed % 4;
        if (index == 0)
        {
            return "6";
        }

        if (index == 1)
        {
            return "7";
        }

        if (index == 2)
        {
            return "8";
        }

        return "17";
    }

    static string GetMolSymbol(int index)
    {
        int mode = index % 4;
        if (mode == 0)
        {
            return "C";
        }

        if (mode == 1)
        {
            return "N";
        }

        if (mode == 2)
        {
            return "O";
        }

        return "Cl";
    }

    static string GetMol2Type(string symbol)
    {
        if (symbol == "C")
        {
            return "C.3";
        }

        if (symbol == "N")
        {
            return "N.3";
        }

        if (symbol == "O")
        {
            return "O.3";
        }

        return "Cl";
    }

    static string FormatMolAtomLine(double x, double y, double z, string symbol)
    {
        return FormatNumber(x).PadLeft(10)
            + FormatNumber(y).PadLeft(10)
            + FormatNumber(z).PadLeft(10)
            + " "
            + symbol.PadRight(3)
            + " 0  0  0  0  0  0  0  0  0  0  0  0";
    }

    static string FormatNumber(double value)
    {
        return value.ToString("0.0000", CultureInfo.InvariantCulture);
    }

    static string PdbAtomLine(int serial, string symbol, double x, double y, double z)
    {
        string atomName = symbol + serial.ToString();
        string line = "ATOM".PadRight(6)
            + serial.ToString().PadLeft(5)
            + " "
            + atomName.PadLeft(4)
            + " "
            + "GLY"
            + " "
            + "A"
            + "   1"
            + "    "
            + x.ToString("0.000", CultureInfo.InvariantCulture).PadLeft(8)
            + y.ToString("0.000", CultureInfo.InvariantCulture).PadLeft(8)
            + z.ToString("0.000", CultureInfo.InvariantCulture).PadLeft(8)
            + "  1.00 10.00"
            + "          "
            + symbol.PadLeft(2)
            + "  ";
        return line;
    }
}
