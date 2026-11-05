using System;
using System.Globalization;
using System.IO;
using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class TemplateWriterTests
{
    [Test]
    public void WriteAndWriteLine_ForwardToTextWriterAndTrackLength()
    {
        StringWriter inner = new StringWriter(CultureInfo.InvariantCulture);
        inner.NewLine = "\n";
        TemplateWriter writer = new TemplateWriter(inner);

        writer.Write("Hello");
        writer.WriteCharacter(' ');
        writer.WriteTextLine("Ada");
        writer.WriteLine();
        writer.Flush();

        Assert.That(inner.ToString(), Is.EqualTo("Hello Ada\n\n"));
        Assert.That(writer.WrittenLength, Is.EqualTo(11));
        Assert.That(writer.GetRemainingLength(), Is.EqualTo(writer.SecurityPolicy.MaximumOutputLength - 11));
    }

    [Test]
    public void Write_AllowsExactLimitAndRejectsOverflowWithoutPartialWrite()
    {
        StringWriter inner = new StringWriter(CultureInfo.InvariantCulture);
        TemplateSecurityPolicy policy = new TemplateSecurityPolicy();
        policy.MaximumOutputLength = 5;
        TemplateWriter writer = TemplateWriter.Create(inner, policy);

        writer.Write("hello");

        Assert.That(writer.WrittenLength, Is.EqualTo(5));
        Assert.That(writer.CanWrite(0), Is.True);
        Assert.That(writer.CanWrite(1), Is.False);
        Assert.Throws<InvalidOperationException>(() => writer.Write("!"));
        Assert.That(inner.ToString(), Is.EqualTo("hello"));
        Assert.That(writer.WrittenLength, Is.EqualTo(5));
    }

    [Test]
    public void WriteLine_ChecksCombinedTextAndNewLineBeforeWriting()
    {
        StringWriter inner = new StringWriter(CultureInfo.InvariantCulture);
        inner.NewLine = "\n";
        TemplateSecurityPolicy policy = new TemplateSecurityPolicy();
        policy.MaximumOutputLength = 3;
        TemplateWriter writer = TemplateWriter.Create(inner, policy);

        Assert.Throws<InvalidOperationException>(() => writer.WriteTextLine("abc"));
        Assert.That(inner.ToString(), Is.EqualTo(""));
        Assert.That(writer.WrittenLength, Is.EqualTo(0));
    }

    [Test]
    public void Factory_FromOptions_UsesConfiguredMaximumOutputLength()
    {
        StringWriter inner = new StringWriter(CultureInfo.InvariantCulture);
        TemplateOptions options = new TemplateOptions();
        options.MaximumOutputLength = 3;
        TemplateWriter writer = TemplateWriter.Create(inner, options);

        writer.Write("abc");

        Assert.That(writer.SecurityPolicy.MaximumOutputLength, Is.EqualTo(3));
        Assert.Throws<InvalidOperationException>(() => writer.WriteCharacter('d'));
        Assert.That(inner.ToString(), Is.EqualTo("abc"));
    }

    [Test]
    public void WriteValue_ConvertsScalarTemplateValues()
    {
        StringWriter inner = new StringWriter(CultureInfo.InvariantCulture);
        TemplateWriter writer = new TemplateWriter(inner);

        writer.WriteValue(TemplateValue.FromString("x"));
        writer.WriteValue(TemplateValue.FromSafeString("<b>"));
        writer.WriteValue(TemplateValue.FromNumber(1.5m));
        writer.WriteValue(TemplateValue.FromBoolean(true));
        writer.WriteValue(TemplateValue.Null());
        writer.WriteValue(TemplateValue.Missing());

        Assert.That(inner.ToString(), Is.EqualTo("x<b>1.5true"));
        Assert.That(writer.WrittenLength, Is.EqualTo(11));
    }

    [Test]
    public void CanWriteConstructorAndFactories_RejectInvalidInputs()
    {
        StringWriter inner = new StringWriter(CultureInfo.InvariantCulture);
        TemplateWriter writer = new TemplateWriter(inner);

        Assert.Throws<ArgumentNullException>(() => new TemplateWriter(null!));
        Assert.Throws<ArgumentNullException>(() => TemplateWriter.Create(null!, new TemplateOptions()));
        Assert.Throws<ArgumentNullException>(() => TemplateWriter.Create(inner, (TemplateOptions)null!));
        Assert.Throws<ArgumentNullException>(() => TemplateWriter.Create(null!, new TemplateSecurityPolicy()));
        Assert.Throws<ArgumentNullException>(() => TemplateWriter.Create(inner, (TemplateSecurityPolicy)null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.CanWrite(-1));
        Assert.Throws<ArgumentNullException>(() => writer.Write(null!));
        Assert.Throws<ArgumentNullException>(() => writer.WriteTextLine(null!));
        Assert.Throws<ArgumentNullException>(() => writer.WriteValue(null!));
    }
}
