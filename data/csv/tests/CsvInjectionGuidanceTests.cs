using System;
using System.IO;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvInjectionGuidanceTests
{
    [TestCase("=")]
    [TestCase("+")]
    [TestCase("-")]
    [TestCase("@")]
    [TestCase("\t")]
    [TestCase("\r")]
    [TestCase("\n")]
    [TestCase("＝")]
    [TestCase("＋")]
    [TestCase("－")]
    [TestCase("＠")]
    public void IsDangerous_RecognizesEveryOwaspFormulaInitiator(string prefix)
    {
        ICsvInjectionGuard guard = new CsvInjectionGuardService();

        Assert.That(guard.IsDangerous(prefix + "payload"), Is.True);
        Assert.That(guard.IsDangerous("  " + prefix + "payload"), Is.True);
    }

    [Test]
    public void Apply_AllPoliciesHandleControlAndFullWidthInitiators()
    {
        ICsvInjectionGuard guard = new CsvInjectionGuardService();
        string dangerous = "  \t\r\n＝＋－＠=+-@payload";

        Assert.That(guard.Apply(dangerous, CsvInjectionMode.None), Is.EqualTo(dangerous));
        Assert.That(guard.Apply(dangerous, CsvInjectionMode.Strip), Is.EqualTo("payload"));
        Assert.That(guard.Apply(dangerous, CsvInjectionMode.Escape), Is.EqualTo("'" + dangerous));
        Assert.Throws<InvalidOperationException>(() => guard.Apply(dangerous, CsvInjectionMode.Error));
    }

    [Test]
    public void WriteField_AllInjectionPoliciesUseExpandedDetection()
    {
        StringWriter noneOutput = new StringWriter();
        CsvWriterOptions noneOptions = new CsvWriterOptions();
        noneOptions.InjectionMode = CsvInjectionMode.None;
        CsvWriter noneWriter = new CsvWriter(noneOutput, noneOptions);
        noneWriter.WriteField("＝SUM(A1:A2)");
        noneWriter.NextRecord();

        StringWriter stripOutput = new StringWriter();
        CsvWriterOptions stripOptions = new CsvWriterOptions();
        stripOptions.InjectionMode = CsvInjectionMode.Strip;
        CsvWriter stripWriter = new CsvWriter(stripOutput, stripOptions);
        stripWriter.WriteField("\r\n＠payload");
        stripWriter.NextRecord();

        StringWriter escapeOutput = new StringWriter();
        CsvWriterOptions escapeOptions = new CsvWriterOptions();
        escapeOptions.InjectionMode = CsvInjectionMode.Escape;
        CsvWriter escapeWriter = new CsvWriter(escapeOutput, escapeOptions);
        escapeWriter.WriteField("\n=payload");
        escapeWriter.NextRecord();

        StringWriter errorOutput = new StringWriter();
        CsvWriterOptions errorOptions = new CsvWriterOptions();
        errorOptions.InjectionMode = CsvInjectionMode.Error;
        CsvWriter errorWriter = new CsvWriter(errorOutput, errorOptions);

        Assert.Throws<InvalidOperationException>(() => errorWriter.WriteField("－10"));
        Assert.That(noneOutput.ToString(), Is.EqualTo("＝SUM(A1:A2)\r\n"));
        Assert.That(stripOutput.ToString(), Is.EqualTo("payload\r\n"));
        Assert.That(escapeOutput.ToString(), Is.EqualTo("\"'\n=payload\"\r\n"));
        Assert.That(errorOutput.ToString(), Is.Empty);
    }

    [Test]
    public void IsDangerous_DoesNotFlagSimilarSafeUnicodeText()
    {
        ICsvInjectionGuard guard = new CsvInjectionGuardService();

        Assert.That(guard.IsDangerous("≠formula"), Is.False);
        Assert.That(guard.IsDangerous("﹢text"), Is.False);
        Assert.That(guard.IsDangerous("ordinary text"), Is.False);
    }
}
