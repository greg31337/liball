using xml;

namespace xml.Tests;

[TestFixture]
public class XsltStreamabilityAnalyzerTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void ClassifiesEveryStreamableComponentAndExpressionFamily()
    {
        string source =
            "<xsl:package xmlns:xsl='" + Xsl + "' xmlns:f='urn:functions' name='urn:streamable' package-version='1' version='3.0'>" +
            "<xsl:mode name='walk' streamable='yes' use-accumulators='count'/>" +
            "<xsl:accumulator name='count' initial-value='0' streamable='yes'><xsl:accumulator-rule match='item' select='$value + 1'/></xsl:accumulator>" +
            "<xsl:attribute-set name='labels' streamable='yes'><xsl:attribute name='class' select=\"'item'\"/></xsl:attribute-set>" +
            "<xsl:function name='f:keep' streamability='filter'><xsl:param name='node'/><xsl:sequence select='$node'/></xsl:function>" +
            "<xsl:template match='item' mode='walk'><out xsl:use-attribute-sets='labels'><xsl:value-of select='@name'/><xsl:apply-templates select='child::item' mode='#current'/></out></xsl:template>" +
            "</xsl:package>";

        XsltExecutable executable = Compile(source, XsltConformanceLevel.Basic);
        XsltStylesheetPlan stylesheet = (XsltStylesheetPlan)executable.Plan!;
        XsltStreamabilityAnalysis analysis = stylesheet.StreamabilityAnalysis!;
        XsltStreamabilityEntry childAxis = analysis.Entries.Single(item => item.Kind == "expression" && ((XPathPlanInstruction)item.Subject).Opcode == XPathPlanOpcode.AxisStep && ((XPathPlanInstruction)item.Subject).Axis == XPathAxis.Child);

        Assert.Multiple(() =>
        {
            Assert.That(executable.StreamabilityReport!.Streamability, Is.EqualTo(XsltStreamability.Streamable));
            Assert.That(executable.StreamabilityReport.Reasons, Is.Empty);
            Assert.That(executable.StreamabilityReport.Modes!.Single().LocalName, Is.EqualTo("walk"));
            Assert.That(childAxis.Classification.Posture, Is.EqualTo(XsltPosture.Striding));
            Assert.That(childAxis.Classification.Sweep, Is.EqualTo(XsltSweep.Consuming));
            Assert.That(analysis.Entries.Select(item => item.Kind), Does.Contain("pattern"));
            Assert.That(analysis.Entries.Select(item => item.Kind), Does.Contain("sequence-constructor"));
            Assert.That(analysis.Entries.Select(item => item.Kind), Does.Contain("instruction"));
            Assert.That(analysis.Entries.Select(item => item.Kind), Does.Contain("template"));
            Assert.That(analysis.Entries.Select(item => item.Kind), Does.Contain("function"));
            Assert.That(analysis.Entries.Select(item => item.Kind), Does.Contain("mode"));
            Assert.That(analysis.Entries.Select(item => item.Kind), Does.Contain("accumulator"));
            Assert.That(analysis.Entries.Select(item => item.Kind), Does.Contain("attribute-set"));
            Assert.That(analysis.Entries.Select(item => item.Kind), Does.Contain("package"));
        });
    }

    [Test]
    public void RejectsMultipleConsumingOperandsInAStreamedConstructor()
    {
        string source = Stylesheet(
            "<xsl:mode name='walk' streamable='yes'/>" +
            "<xsl:template match='item' mode='walk'><xsl:sequence select='child::a, child::b'/></xsl:template>");

        XsltExecutable executable = Compile(source, XsltConformanceLevel.Basic);
        XsltStreamabilityReport report = executable.StreamabilityReport!;

        Assert.Multiple(() =>
        {
            Assert.That(report.Streamability, Is.EqualTo(XsltStreamability.NotStreamable));
            Assert.That(report.Reasons, Has.Some.Contains("more than one consuming operand"));
            Assert.That(report.Locations, Has.Length.EqualTo(report.Reasons!.Length));
        });
    }

    [Test]
    public void RejectsNonMotionlessPatternsInStreamableModes()
    {
        string source = Stylesheet(
            "<xsl:mode name='walk' streamable='yes'/>" +
            "<xsl:template match=\"item[child]\" mode='walk'><xsl:text>value</xsl:text></xsl:template>");

        XsltExecutable executable = Compile(source, XsltConformanceLevel.Basic);
        XsltStreamabilityEntry pattern = ((XsltStylesheetPlan)executable.Plan!).StreamabilityAnalysis!.Entries.Single(item => item.Kind == "pattern" && item.Name == "item[child]");

        Assert.Multiple(() =>
        {
            Assert.That(pattern.Classification.Posture, Is.EqualTo(XsltPosture.Roaming));
            Assert.That(pattern.Classification.Sweep, Is.EqualTo(XsltSweep.FreeRanging));
            Assert.That(executable.StreamabilityReport!.Streamability, Is.EqualTo(XsltStreamability.NotStreamable));
            Assert.That(executable.StreamabilityReport.Reasons, Has.Some.Contains("pattern is not motionless"));
        });
    }

    [Test]
    public void PropagatesNamedTemplateStreamabilityIntoCallingRules()
    {
        string source = Stylesheet(
            "<xsl:mode name='walk' streamable='yes'/>" +
            "<xsl:template match='item' mode='walk'><xsl:call-template name='helper'/></xsl:template>" +
            "<xsl:template name='helper'><xsl:sequence select='child::a, child::b'/></xsl:template>");

        XsltExecutable executable = Compile(source, XsltConformanceLevel.Basic);
        XsltStreamabilityAnalysis analysis = ((XsltStylesheetPlan)executable.Plan!).StreamabilityAnalysis!;
        XsltStreamabilityEntry rule = analysis.Entries.Single(item => item.Kind == "template" && item.Name == "item");

        Assert.Multiple(() =>
        {
            Assert.That(rule.Classification.Guaranteed, Is.False);
            Assert.That(rule.Classification.Reason, Does.Contain("more than one consuming operand"));
            Assert.That(executable.StreamabilityReport!.Streamability, Is.EqualTo(XsltStreamability.NotStreamable));
        });
    }

    [Test]
    public void AppliesDeclaredFunctionAccumulatorAndAttributeSetRules()
    {
        string source =
            "<xsl:package xmlns:xsl='" + Xsl + "' xmlns:f='urn:functions' name='urn:invalid' package-version='1' version='3.0'>" +
            "<xsl:function name='f:bad' streamability='filter'><xsl:param name='node' as='node()'/><xsl:sequence select='$node/descendant::item'/></xsl:function>" +
            "<xsl:accumulator name='nodes' initial-value='/' streamable='yes'><xsl:accumulator-rule match='item' select='descendant::item'/></xsl:accumulator>" +
            "<xsl:attribute-set name='labels' streamable='yes'><xsl:attribute name='class' select='/'/></xsl:attribute-set>" +
            "</xsl:package>";

        XsltExecutable executable = Compile(source, XsltConformanceLevel.Basic);
        XsltStreamabilityAnalysis analysis = ((XsltStylesheetPlan)executable.Plan!).StreamabilityAnalysis!;

        Assert.Multiple(() =>
        {
            Assert.That(analysis.Entries.Single(item => item.Kind == "function").Classification.Guaranteed, Is.False);
            Assert.That(analysis.Entries.Single(item => item.Kind == "accumulator").Classification.Guaranteed, Is.False);
            Assert.That(analysis.Entries.Single(item => item.Kind == "attribute-set").Classification.Guaranteed, Is.False);
            Assert.That(executable.StreamabilityReport!.Reasons, Has.Length.EqualTo(3));
        });
    }

    [Test]
    public void StreamingConformanceRunsAnalysisWithoutTheOptionalFlag()
    {
        XsltExecutable executable = Compile(Stylesheet("<xsl:template match='item'><xsl:value-of select='@name'/></xsl:template>"), XsltConformanceLevel.Streaming, false);

        Assert.Multiple(() =>
        {
            Assert.That(executable.IsStreaming, Is.True);
            Assert.That(executable.StreamabilityReport, Is.Not.Null);
            Assert.That(executable.StreamabilityReport!.Streamability, Is.EqualTo(XsltStreamability.Streamable));
        });
    }

    [Test]
    public void PublicReportExposesPerConstructClassificationAndFallbackRequirements()
    {
        string source = Stylesheet(
            "<xsl:mode name='walk' streamable='yes'/>" +
            "<xsl:template match='item' mode='walk'><xsl:sequence select='child::a, child::b'/></xsl:template>");

        XsltExecutable executable = Compile(source, XsltConformanceLevel.Basic);
        XsltStreamabilityReport report = new XsltStreamAnalyzer().AnalyzeXsltStreamability(executable);
        XsltStreamabilityConstruct mode = report.Constructs!.Single(item => item.Kind == "mode" && item.Name == "Q{}walk");
        XsltStreamabilityConstruct expression = report.Constructs!.First(item => item.Kind == "expression" && item.Reason != null && item.Reason.Contains("more than one consuming operand"));

        Assert.Multiple(() =>
        {
            Assert.That(report, Is.SameAs(executable.StreamabilityReport));
            Assert.That(report.ConformanceLevel, Is.EqualTo(XsltConformanceLevel.Basic));
            Assert.That(report.RequiresFallback, Is.True);
            Assert.That(report.Constructs, Has.Some.Matches<XsltStreamabilityConstruct>(item => item.Kind == "instruction"));
            Assert.That(mode.Required, Is.True);
            Assert.That(mode.Streamability, Is.EqualTo(XsltStreamability.NotStreamable));
            Assert.That(mode.RequiresFallback, Is.True);
            Assert.That(expression.Posture, Is.EqualTo(XsltPosture.Roaming));
            Assert.That(expression.Sweep, Is.EqualTo(XsltSweep.FreeRanging));
            Assert.That(expression.ConsumingOperandCount, Is.EqualTo(2));
            Assert.That(expression.Location, Is.Not.Null);
            Assert.That(expression.Location!.Start, Is.Not.Null);
            Assert.That(expression.Location.Start!.Line, Is.GreaterThan(0));
        });
    }

    [Test]
    public void PublicAnalyzerPerformsOnDemandAnalysisWithoutMutatingTheExecutable()
    {
        string source = Stylesheet("<xsl:template match='item'><xsl:sequence select='following::item'/></xsl:template>");
        XsltExecutable executable = Compile(source, XsltConformanceLevel.Basic, false);
        XsltStylesheetPlan stylesheet = (XsltStylesheetPlan)executable.Plan!;

        XsltStreamabilityReport report = new XsltStreamAnalyzer().AnalyzeXsltStreamability(executable);
        XsltStreamabilityConstruct roaming = report.Constructs!.First(item => item.Kind == "expression" && item.Posture == XsltPosture.Roaming);

        Assert.Multiple(() =>
        {
            Assert.That(report.Streamability, Is.EqualTo(XsltStreamability.Streamable));
            Assert.That(report.ConformanceLevel, Is.EqualTo(XsltConformanceLevel.Basic));
            Assert.That(report.RequiresFallback, Is.True);
            Assert.That(roaming.Sweep, Is.EqualTo(XsltSweep.FreeRanging));
            Assert.That(roaming.Reason, Does.Contain("lateral axis"));
            Assert.That(roaming.Required, Is.False);
            Assert.That(roaming.RequiresFallback, Is.True);
            Assert.That(executable.StreamabilityReport, Is.Null);
            Assert.That(stylesheet.StreamabilityAnalysis, Is.Null);
            Assert.That(stylesheet.IsImmutable, Is.True);
        });
    }

    [Test]
    public void PublicAnalyzerRejectsAnUncompiledExecutableWithoutAStoredReport()
    {
        XsltExecutable executable = new XsltExecutable();

        ArgumentException error = Assert.Throws<ArgumentException>(() => new XsltStreamAnalyzer().AnalyzeXsltStreamability(executable))!;

        Assert.That(error.ParamName, Is.EqualTo("executable"));
    }

    private static string Stylesheet(string content)
    {
        return "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'>" + content + "</xsl:stylesheet>";
    }

    private static XsltExecutable Compile(string source, XsltConformanceLevel level, bool analyze = true)
    {
        XsltCompileOptions options = new XsltCompileOptions
        {
            AnalyzeStreamability = analyze,
            ConformanceLevel = level,
            ResolverPolicy = new XmlResolverPolicy { Access = XmlResourceAccess.Denied }
        };
        return new XsltCompiler().CompileXslt(new XmlInput { Text = source }, options, new DeniedResolver());
    }

    private sealed class DeniedResolver : IXmlResourceResolver
    {
        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            return new XmlResolvedResource { Kind = kind, Access = XmlResourceAccess.Denied };
        }
    }
}
