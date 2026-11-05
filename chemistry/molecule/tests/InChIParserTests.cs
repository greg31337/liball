using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class InChIParserTests
{
    [Test]
    public void Parse_ReadsVersionFormulaConnectivityHydrogenAndTautomerLayers()
    {
        InChIParser parser = new InChIParser();
        string text = "InChI=1S/C2H6O/c1-2-3/h3H,2H2,1H3";

        InChIRecord record = parser.Parse(text);

        Assert.That(record.Success, Is.True);
        Assert.That(record.Report.GetErrorCount(), Is.EqualTo(0));
        Assert.That(record.OriginalText, Is.EqualTo(text));
        Assert.That(record.Prefix, Is.EqualTo("InChI"));
        Assert.That(record.Version, Is.EqualTo("1"));
        Assert.That(record.IsStandard, Is.True);
        Assert.That(record.FormulaLayer, Is.EqualTo("C2H6O"));
        Assert.That(record.ConnectivityLayer, Is.EqualTo("1-2-3"));
        Assert.That(record.HydrogenLayer, Is.EqualTo("3H,2H2,1H3"));
        Assert.That(record.TautomerLayer, Is.EqualTo(""));
        Assert.That(record.HasFormula(), Is.True);
        Assert.That(record.HasConnectivity(), Is.True);
        Assert.That(record.HasHydrogen(), Is.True);
    }

    [Test]
    public void Parse_ExtractsMobileHydrogenTautomerSegment()
    {
        InChIParser parser = new InChIParser();

        InChIRecord record = parser.Parse("InChI=1S/CH4N2/c2-1-3/h(H4,2,3)");

        Assert.That(record.Success, Is.True);
        Assert.That(record.HydrogenLayer, Is.EqualTo("(H4,2,3)"));
        Assert.That(record.TautomerLayer, Is.EqualTo("(H4,2,3)"));
        Assert.That(record.HasTautomer(), Is.True);
        Assert.That(record.GetLayer("tautomer"), Is.EqualTo("(H4,2,3)"));
    }

    [Test]
    public void Parse_ReadsChargeProtonStereoIsotopeAndFixedHLayers()
    {
        InChIParser parser = new InChIParser();
        string text = "InChI=1S/C2H4/c1-2/h1-2H2/q+1/p-1/b2-1+/t1-/m0/s1/i1+1/fC2H4/h1H,2H3";

        InChIRecord record = parser.Parse(text);

        Assert.That(record.Success, Is.True);
        Assert.That(record.ChargeLayer, Is.EqualTo("+1"));
        Assert.That(record.ProtonLayer, Is.EqualTo("-1"));
        Assert.That(record.StereoLayer, Is.EqualTo("/b2-1+/t1-/m0/s1"));
        Assert.That(record.IsotopeLayer, Is.EqualTo("1+1"));
        Assert.That(record.FixedHLayer, Is.EqualTo("C2H4/h1H,2H3"));
        Assert.That(record.HasCharge(), Is.True);
        Assert.That(record.HasProton(), Is.True);
        Assert.That(record.HasStereo(), Is.True);
        Assert.That(record.HasIsotope(), Is.True);
        Assert.That(record.HasFixedH(), Is.True);
        Assert.That(record.GetLayer("q"), Is.EqualTo("+1"));
        Assert.That(record.GetLayer("f"), Is.EqualTo("C2H4/h1H,2H3"));
    }

    [Test]
    public void Parse_ReadsNonStandardVersion()
    {
        InChIParser parser = new InChIParser();

        InChIRecord record = parser.Parse("InChI=1/H2O/h1H2");

        Assert.That(record.Success, Is.True);
        Assert.That(record.Version, Is.EqualTo("1"));
        Assert.That(record.IsStandard, Is.False);
        Assert.That(record.FormulaLayer, Is.EqualTo("H2O"));
    }

    [Test]
    public void Parse_ReportsMissingPrefix()
    {
        InChIParser parser = new InChIParser();

        InChIRecord record = parser.Parse("1S/H2O/h1H2");

        Assert.That(record.Success, Is.False);
        Assert.That(record.Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(record.Report.Get(0).Code, Is.EqualTo("INCHI_PREFIX_MISSING"));
    }

    [Test]
    public void Parse_ReportsEmptyAndMissingVersionInputs()
    {
        InChIParser parser = new InChIParser();

        InChIRecord empty = parser.Parse(null);
        InChIRecord missingVersion = parser.Parse("InChI=");

        Assert.That(empty.Success, Is.False);
        Assert.That(empty.Report.Get(0).Code, Is.EqualTo("INCHI_EMPTY"));
        Assert.That(empty.Report.Get(0).Location.ColumnNumber, Is.EqualTo(1));
        Assert.That(missingVersion.Success, Is.False);
        Assert.That(missingVersion.Report.Get(0).Code, Is.EqualTo("INCHI_VERSION_MISSING"));
        Assert.That(missingVersion.Report.Get(0).Location.ColumnNumber, Is.EqualTo(7));
    }

    [Test]
    public void Parse_ReportsMissingFormula()
    {
        InChIParser parser = new InChIParser();

        InChIRecord record = parser.Parse("InChI=1S");

        Assert.That(record.Success, Is.False);
        Assert.That(record.Version, Is.EqualTo("1"));
        Assert.That(record.IsStandard, Is.True);
        Assert.That(record.Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(record.Report.Get(0).Code, Is.EqualTo("INCHI_FORMULA_MISSING"));
    }

    [Test]
    public void Parse_PreservesUnrecognizedLayersWithWarning()
    {
        InChIParser parser = new InChIParser();

        InChIRecord record = parser.Parse("InChI=1S/H2O/h1H2/zvendor");

        Assert.That(record.Success, Is.True);
        Assert.That(record.Report.GetWarningCount(), Is.EqualTo(1));
        Assert.That(record.OtherLayers.Count, Is.EqualTo(1));
        Assert.That(record.GetLayer("z"), Is.EqualTo("vendor"));
    }

    [Test]
    public void Parse_PreservesValidLayersAroundEmptyLayerAndWarns()
    {
        InChIRecord record = new InChIParser().Parse("InChI=1S/H2O//h1H2");

        Assert.That(record.Success, Is.True);
        Assert.That(record.FormulaLayer, Is.EqualTo("H2O"));
        Assert.That(record.HydrogenLayer, Is.EqualTo("1H2"));
        Assert.That(record.Report.GetWarningCount(), Is.EqualTo(1));
        Assert.That(record.Report.Get(0).Code, Is.EqualTo("INCHI_EMPTY_LAYER"));
    }
}
