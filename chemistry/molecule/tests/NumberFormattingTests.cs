using NUnit.Framework;
using System.Globalization;

namespace molecule.Tests;

[TestFixture]
public class NumberFormattingTests
{
    [Test]
    public void NumberFormatting_UsesInvariantCultureForFormattingAndParsing()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");

            Assert.That(NumberFormatting.FormatInt(1234), Is.EqualTo("1234"));
            Assert.That(NumberFormatting.FormatLong(1234567890123), Is.EqualTo("1234567890123"));
            Assert.That(NumberFormatting.FormatDouble(1234.5), Is.EqualTo("1234.5"));
            Assert.That(NumberFormatting.FormatDoubleWithPattern(1.23456, "0.00"), Is.EqualTo("1.23"));
            Assert.That(NumberFormatting.ParseInt("42"), Is.EqualTo(42));
            Assert.That(NumberFormatting.ParseLong("1234567890123"), Is.EqualTo(1234567890123));
            Assert.That(NumberFormatting.ParseDouble("12.75"), Is.EqualTo(12.75));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Test]
    public void StableTextWriter_AppendsStableNewlineDelimitedText()
    {
        StableTextWriter writer = new StableTextWriter();

        writer.Append("header");
        writer.AppendLine("");
        writer.AppendKeyValue("name", "water");
        writer.AppendInt("atoms", 3);
        writer.AppendLong("large", 1234567890123);
        writer.AppendDouble("mass", 18.01528);
        writer.AppendDoubleWithPattern("rounded", 1.23456, "0.000");

        Assert.That(
            writer.ToText(),
            Is.EqualTo("header\nname=water\natoms=3\nlarge=1234567890123\nmass=18.01528\nrounded=1.235\n"));

        writer.Clear();

        Assert.That(writer.ToText(), Is.EqualTo(""));
    }

    [Test]
    public void StableTextWriter_AppendsMetadataEntriesInSortedOrderWithoutMutatingInput()
    {
        List<MoleculeMetadataEntry> fields = new List<MoleculeMetadataEntry>();
        fields.Add(new MoleculeMetadataEntry("b", "2"));
        fields.Add(new MoleculeMetadataEntry("a", "2"));
        fields.Add(new MoleculeMetadataEntry("a", "1"));
        fields.Add(new MoleculeMetadataEntry("a", "1"));

        StableTextWriter writer = new StableTextWriter();
        writer.AppendSortedFields(fields);

        Assert.That(writer.ToText(), Is.EqualTo("a=1\na=1\na=2\nb=2\n"));
        Assert.That(fields[0].Key, Is.EqualTo("b"));
        Assert.That(fields[1].Value, Is.EqualTo("2"));
    }

    [Test]
    public void StableTextWriter_NormalizesNullTextToEmptyText()
    {
        StableTextWriter writer = new StableTextWriter();

        writer.Append(null);
        writer.AppendLine(null);
        writer.AppendKeyValue(null, null);

        Assert.That(writer.ToText(), Is.EqualTo("\n=\n"));
    }
}
