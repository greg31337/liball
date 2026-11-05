using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class SvgDepictionWriterTests
{
    [Test]
    public void WriteString_LayoutsMoleculeAndWritesSvgElementsWithoutMutatingSource()
    {
        Molecule molecule = BuildEthanol();
        SvgDepictionWriter writer = new SvgDepictionWriter();

        string svg = writer.WriteString(molecule);

        Assert.That(svg, Does.StartWith("<svg xmlns=\"http://www.w3.org/2000/svg\""));
        Assert.That(svg, Does.Contain("<line class=\"bond single\""));
        Assert.That(svg, Does.Contain(">C</text>"));
        Assert.That(svg, Does.Contain(">O</text>"));
        Assert.That(svg, Does.EndWith("</svg>"));
        Assert.That(molecule.Atoms[0].HasCoordinates, Is.False);
    }

    [Test]
    public void WriteString_WritesWedgeBondFromClockwiseStereoRecord()
    {
        Molecule molecule = BuildTetrahedralMolecule(Chirality.Clockwise);
        SvgDepictionWriter writer = new SvgDepictionWriter();

        string svg = writer.WriteString(molecule);

        Assert.That(svg, Does.Contain("<polygon class=\"bond wedge\""));
    }

    [Test]
    public void WriteString_WritesHashBondFromCounterClockwiseStereoRecord()
    {
        Molecule molecule = BuildTetrahedralMolecule(Chirality.CounterClockwise);
        SvgDepictionWriter writer = new SvgDepictionWriter();

        string svg = writer.WriteString(molecule);

        Assert.That(svg, Does.Contain("class=\"bond hash\""));
    }

    [Test]
    public void WriteString_WritesDoubleBondAsParallelLines()
    {
        Molecule molecule = BuildCarbonyl();
        SvgDepictionWriter writer = new SvgDepictionWriter();

        string svg = writer.WriteString(molecule);

        Assert.That(CountOccurrences(svg, "class=\"bond double\""), Is.EqualTo(2));
    }

    [Test]
    public void WriteString_EscapesAtomLabels()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(7, 0, 0, 0, 3, 0, 0, false, "", "N<>&\"'"));
        SvgDepictionWriter writer = new SvgDepictionWriter();

        string svg = writer.WriteString(builder.Build());

        Assert.That(svg, Does.Contain(">N&lt;&gt;&amp;&quot;&apos;</text>"));
    }

    [Test]
    public void WriteStringWithOptions_HonorsDimensionsFontsAndStrokeWidthsWithoutMutatingOptions()
    {
        Molecule molecule = BuildCoordinateBond(BondOrder.Single);
        SvgDepictionOptions options = new SvgDepictionOptions();
        options.Scale = 10.0;
        options.Padding = 3.0;
        options.FontSize = 9.0;
        options.StrokeWidth = 4.0;
        options.StereoWidth = 6.0;

        string svg = new SvgDepictionWriter().WriteStringWithOptions(molecule, options);

        Assert.That(svg, Does.Contain("width=\"26\" height=\"16\" viewBox=\"0 0 26 16\""));
        Assert.That(svg, Does.Contain("font-size=\"9\""));
        Assert.That(svg, Does.Contain("stroke-width=\"4\""));
        Assert.That(options.Scale, Is.EqualTo(10.0));
        Assert.That(options.Padding, Is.EqualTo(3.0));
        Assert.That(options.FontSize, Is.EqualTo(9.0));
        Assert.That(options.StrokeWidth, Is.EqualTo(4.0));
        Assert.That(options.StereoWidth, Is.EqualTo(6.0));
    }

    [Test]
    public void WriteStringWithOptions_UsesFallbacksForInvalidValuesWithoutChangingCallerOptions()
    {
        Molecule molecule = BuildCoordinateBond(BondOrder.Single);
        SvgDepictionOptions options = new SvgDepictionOptions();
        options.Scale = 0.0;
        options.Padding = -1.0;
        options.FontSize = 0.0;
        options.StrokeWidth = -2.0;
        options.StereoWidth = 0.0;

        string svg = new SvgDepictionWriter().WriteStringWithOptions(molecule, options);

        Assert.That(svg, Does.Contain("width=\"80\" height=\"40\" viewBox=\"0 0 80 40\""));
        Assert.That(svg, Does.Contain("font-size=\"14\""));
        Assert.That(svg, Does.Contain("stroke-width=\"2\""));
        Assert.That(options.Scale, Is.EqualTo(0.0));
        Assert.That(options.Padding, Is.EqualTo(-1.0));
        Assert.That(options.FontSize, Is.EqualTo(0.0));
        Assert.That(options.StrokeWidth, Is.EqualTo(-2.0));
        Assert.That(options.StereoWidth, Is.EqualTo(0.0));
    }

    [Test]
    public void WriteString_LayoutsAllAtomsWhenCoordinatesAreOnlyPartiallyAssigned()
    {
        Molecule molecule = BuildEthanol();
        molecule.Atoms[0].HasCoordinates = true;
        molecule.Atoms[0].X = 50.0;
        molecule.Atoms[0].Y = 50.0;

        string svg = new SvgDepictionWriter().WriteString(molecule);

        Assert.That(CountOccurrences(svg, "<text class=\"atom\""), Is.EqualTo(3));
        Assert.That(CountOccurrences(svg, "class=\"bond single\""), Is.EqualTo(2));
        Assert.That(molecule.Atoms[0].X, Is.EqualTo(50.0));
        Assert.That(molecule.Atoms[1].HasCoordinates, Is.False);
    }

    [Test]
    public void WriteString_CoversEmptyTripleUnknownAndFallbackLabelOutput()
    {
        SvgDepictionWriter writer = new SvgDepictionWriter();
        MoleculeBuilder builder = new MoleculeBuilder();
        int first = builder.AddAtom(new AtomSpec(999, 0, 0, 0, 0, 0, 0, false, "R&", ""));
        int second = builder.AddAtom(new AtomSpec(998, 0, 0, 0, 0, 0, 0, false, "", ""));
        int third = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", ""));
        builder.AddBond(first, second, BondSpec.Create(BondOrder.Triple, false, BondStereo.None, BondDirection.None));
        builder.AddBond(second, third, BondSpec.Create(BondOrder.Single, false, BondStereo.UpOrDown, BondDirection.None));
        Molecule molecule = builder.Build();
        molecule.Bonds.Add(new Bond(2, 0, 99, new BondSpec()));
        SetCoordinate(molecule.Atoms[0], 0.0, 0.0);
        SetCoordinate(molecule.Atoms[1], 1.0, 0.0);
        SetCoordinate(molecule.Atoms[2], 2.0, 0.0);

        string svg = writer.WriteString(molecule);
        string empty = writer.WriteStringWithOptions(null, null);

        Assert.That(CountOccurrences(svg, "class=\"bond triple\""), Is.EqualTo(3));
        Assert.That(svg, Does.Contain("class=\"bond unknown\""));
        Assert.That(svg, Does.Contain("stroke-dasharray=\"4 3\""));
        Assert.That(svg, Does.Contain(">R&amp;</text>"));
        Assert.That(svg, Does.Contain(">?</text>"));
        Assert.That(svg, Does.Contain(">C</text>"));
        Assert.That(empty, Does.Contain("width=\"40\" height=\"40\""));
    }

    static Molecule BuildEthanol()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbonA = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int carbonB = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        builder.AddBond(carbonA, carbonB, new BondSpec());
        builder.AddBond(carbonB, oxygen, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildTetrahedralMolecule(Chirality chirality)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int fluorine = builder.AddAtom(new AtomSpec(9, 0, 0, 0, 0, 0, 0, false, "", "F"));
        int chlorine = builder.AddAtom(new AtomSpec(17, 0, 0, 0, 0, 0, 0, false, "", "Cl"));
        int bromine = builder.AddAtom(new AtomSpec(35, 0, 0, 0, 0, 0, 0, false, "", "Br"));
        int iodine = builder.AddAtom(new AtomSpec(53, 0, 0, 0, 0, 0, 0, false, "", "I"));
        builder.AddBond(carbon, fluorine, new BondSpec());
        builder.AddBond(carbon, chlorine, new BondSpec());
        builder.AddBond(carbon, bromine, new BondSpec());
        builder.AddBond(carbon, iodine, new BondSpec());
        Molecule molecule = builder.Build();
        StereoRecord record = new StereoRecord();
        record.SetTetrahedral(carbon, fluorine, chlorine, bromine, iodine, chirality);
        molecule.StereoRecords.Add(record);
        return molecule;
    }

    static Molecule BuildCarbonyl()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        builder.AddBond(carbon, oxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }

    static Molecule BuildCoordinateBond(BondOrder order)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int first = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int second = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        builder.AddBond(first, second, BondSpec.Create(order, false, BondStereo.None, BondDirection.None));
        Molecule molecule = builder.Build();
        SetCoordinate(molecule.Atoms[0], 0.0, 0.0);
        SetCoordinate(molecule.Atoms[1], 2.0, 1.0);
        return molecule;
    }

    static void SetCoordinate(Atom atom, double x, double y)
    {
        atom.HasCoordinates = true;
        atom.X = x;
        atom.Y = y;
        atom.Z = 0.0;
    }

    static int CountOccurrences(string text, string pattern)
    {
        int count = 0;
        int index = 0;
        while (index <= text.Length - pattern.Length)
        {
            if (text.Substring(index, pattern.Length) == pattern)
            {
                count = count + 1;
                index = index + pattern.Length;
            }
            else
            {
                index = index + 1;
            }
        }

        return count;
    }
}
