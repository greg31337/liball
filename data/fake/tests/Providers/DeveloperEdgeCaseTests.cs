using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using fake;

namespace fake.Tests;

[TestFixture]
public class DeveloperEdgeCaseTests
{
    [Test]
    public void PathRootsUseExactRequestedPlatformSyntax()
    {
        const long seed = 12100;
        SystemProvider first = Fake.Create(seed).System();
        SystemProvider second = Fake.Create(seed).System();

        int index = 0;
        while (index < 64)
        {
            string unix = first.PathRoot(FilePathStyle.Unix);
            string windows = first.PathRoot(FilePathStyle.Windows);

            Assert.Multiple(() =>
            {
                Assert.That(unix, Is.EqualTo("/"));
                Assert.That(unix,
                    Is.EqualTo(second.PathRoot(FilePathStyle.Unix)));
                Assert.That(windows,
                    Is.EqualTo(second.PathRoot(FilePathStyle.Windows)));
                Assert.That(windows, Does.Match("^[C-E]:\\\\$"));
            });
            index++;
        }
    }

    [Test]
    public void ReservedNamesAreExplicitAndNeverUsedByOrdinaryPaths()
    {
        const long seed = 12101;
        SystemProvider system = Fake.Create(seed).System();

        Assert.Multiple(() =>
        {
            Assert.That(system.IsReservedPathName(
                FilePathStyle.Unix, "."), Is.True);
            Assert.That(system.IsReservedPathName(
                FilePathStyle.Unix, ".."), Is.True);
            Assert.That(system.IsReservedPathName(
                FilePathStyle.Unix, ".hidden"), Is.False);
            Assert.That(system.IsReservedPathName(
                FilePathStyle.Unix, "CON"), Is.False);
            Assert.That(system.IsReservedPathName(
                FilePathStyle.Windows, "CON"), Is.True);
            Assert.That(system.IsReservedPathName(
                FilePathStyle.Windows, "con.txt"), Is.True);
            Assert.That(system.IsReservedPathName(
                FilePathStyle.Windows, "NUL... "), Is.True);
            Assert.That(system.IsReservedPathName(
                FilePathStyle.Windows, "COM1"), Is.True);
            Assert.That(system.IsReservedPathName(
                FilePathStyle.Windows, "com9.log"), Is.True);
            Assert.That(system.IsReservedPathName(
                FilePathStyle.Windows, "LPT1"), Is.True);
            Assert.That(system.IsReservedPathName(
                FilePathStyle.Windows, "lpt9.txt"), Is.True);
            Assert.That(system.IsReservedPathName(
                FilePathStyle.Windows, "COM0"), Is.False);
            Assert.That(system.IsReservedPathName(
                FilePathStyle.Windows, "LPT10"), Is.False);
            Assert.That(system.IsReservedPathName(
                FilePathStyle.Windows, "console"), Is.False);
        });

        int index = 0;
        while (index < 128)
        {
            string unixReserved = system.ReservedPathName(FilePathStyle.Unix);
            string windowsReserved = system.ReservedPathName(
                FilePathStyle.Windows);
            string unixPath = system.DirectoryPathFromStyleAndRooted(FilePathStyle.Unix, false);
            string windowsPath = system.DirectoryPathFromStyleAndRooted(
                FilePathStyle.Windows,
                false);

            Assert.Multiple(() =>
            {
                Assert.That(system.IsReservedPathName(
                    FilePathStyle.Unix, unixReserved), Is.True);
                Assert.That(system.IsReservedPathName(
                    FilePathStyle.Windows, windowsReserved), Is.True);
            });
            string[] unixSegments = unixPath.Split('/');
            string[] windowsSegments = windowsPath.Split('\\');
            int segmentIndex = 0;
            while (segmentIndex < unixSegments.Length)
            {
                Assert.That(system.IsReservedPathName(
                    FilePathStyle.Unix,
                    unixSegments[segmentIndex]), Is.False);
                segmentIndex++;
            }
            segmentIndex = 0;
            while (segmentIndex < windowsSegments.Length)
            {
                Assert.That(system.IsReservedPathName(
                    FilePathStyle.Windows,
                    windowsSegments[segmentIndex]), Is.False);
                segmentIndex++;
            }
            index++;
        }
    }

    [Test]
    public void UnicodePathNamesRemainValidForBothPathStyles()
    {
        string[] reviewed = new string[]
        {
            "café", "naïve", "résumé", "資料", "東京"
        };
        const long seed = 12102;
        SystemProvider first = Fake.Create(seed).System();
        SystemProvider second = Fake.Create(seed).System();

        int index = 0;
        while (index < 128)
        {
            string unix = first.UnicodePathName(FilePathStyle.Unix);
            string windows = first.UnicodePathName(FilePathStyle.Windows);

            Assert.Multiple(() =>
            {
                Assert.That(unix,
                    Is.EqualTo(second.UnicodePathName(FilePathStyle.Unix)));
                Assert.That(windows,
                    Is.EqualTo(second.UnicodePathName(
                        FilePathStyle.Windows)));
                Assert.That(unix, Is.AnyOf(reviewed));
                Assert.That(windows, Is.AnyOf(reviewed));
                Assert.That(unix, Does.Not.Contain("/"));
                Assert.That(windows, Does.Not.Contain("\\"));
                Assert.That(systemReserved(first, FilePathStyle.Unix, unix),
                    Is.False);
                Assert.That(systemReserved(
                    first,
                    FilePathStyle.Windows,
                    windows), Is.False);
                Assert.That(unix.IsNormalized(
                    NormalizationForm.FormC), Is.True);
                Assert.That(windows.IsNormalized(
                    NormalizationForm.FormC), Is.True);
            });
            index++;
        }
    }

    [Test]
    public void CsvFieldEscapingGuardsSpreadsheetFormulaPrefixes()
    {
        ExportProvider export = Fake.Create(12103).Export();

        Assert.Multiple(() =>
        {
            Assert.That(export.CsvField("sample"), Is.EqualTo("sample"));
            Assert.That(export.CsvField("a,b"), Is.EqualTo("\"a,b\""));
            Assert.That(export.CsvField("a\"b"),
                Is.EqualTo("\"a\"\"b\""));
            Assert.That(export.CsvField("line one\r\nline two"),
                Is.EqualTo("\"line one\r\nline two\""));
            Assert.That(export.CsvField("=1+1"), Is.EqualTo("'=1+1"));
            Assert.That(export.CsvField("+1"), Is.EqualTo("'+1"));
            Assert.That(export.CsvField("-1"), Is.EqualTo("'-1"));
            Assert.That(export.CsvField("@SUM(A1:A2)"),
                Is.EqualTo("'@SUM(A1:A2)"));
            Assert.That(export.CsvField("  =1+1"),
                Is.EqualTo("'  =1+1"));
            Assert.That(export.CsvField("\t=1+1"),
                Is.EqualTo("'\t=1+1"));
            Assert.That(export.CsvField("=SUM(1,2)"),
                Is.EqualTo("\"'=SUM(1,2)\""));
            Assert.That(export.CsvFieldFromValueAndGuardSpreadsheetFormula("=1+1", false),
                Is.EqualTo("=1+1"));
        });
    }

    [Test]
    public void JsonStringsAndNumbersCoverRuntimeBoundaries()
    {
        ExportProvider export = Fake.Create(12104).Export();
        string[] strings = new string[]
        {
            string.Empty,
            "quote \" and slash \\",
            "line one\nline two\tend",
            "\u0000\u001f",
            "東京",
            "emoji \ud83d\ude80"
        };
        long[] integers = new long[]
        {
            long.MinValue, -1L, 0L, 1L, long.MaxValue
        };
        double[] floating = new double[]
        {
            double.MinValue,
            -0.0,
            double.Epsilon,
            1.0 / 3.0,
            double.MaxValue
        };

        int index = 0;
        while (index < strings.Length)
        {
            string json = export.JsonString(strings[index]);
            using JsonDocument document = JsonDocument.Parse(json);
            Assert.That(document.RootElement.GetString(),
                Is.EqualTo(strings[index]));
            index++;
        }
        index = 0;
        while (index < integers.Length)
        {
            string json = export.JsonNumber(integers[index]);
            using JsonDocument document = JsonDocument.Parse(json);
            Assert.Multiple(() =>
            {
                Assert.That(document.RootElement.GetInt64(),
                    Is.EqualTo(integers[index]));
                Assert.That(json, Is.EqualTo(integers[index].ToString(
                    CultureInfo.InvariantCulture)));
            });
            index++;
        }
        index = 0;
        while (index < floating.Length)
        {
            string json = export.JsonNumberFromValue(floating[index]);
            using JsonDocument document = JsonDocument.Parse(json);
            Assert.That(document.RootElement.GetDouble(),
                Is.EqualTo(floating[index]));
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(() => export.JsonNumberFromValue(double.NaN),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => export.JsonNumberFromValue(double.PositiveInfinity),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => export.JsonNumberFromValue(double.NegativeInfinity),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void XmlElementsEscapeTextAndRejectInvalidXmlInputs()
    {
        ExportProvider export = Fake.Create(12105).Export();
        string source = "&<>\"' 東京";
        string xml = export.XmlElement("sample-item", source);
        XElement element = XElement.Parse(xml);

        Assert.Multiple(() =>
        {
            Assert.That(xml, Is.EqualTo(
                "<sample-item>&amp;&lt;&gt;&quot;&apos; 東京</sample-item>"));
            Assert.That(element.Name.LocalName, Is.EqualTo("sample-item"));
            Assert.That(element.Value, Is.EqualTo(source));
            Assert.That(() => export.XmlElement("", "sample"),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => export.XmlElement("bad name", "sample"),
                Throws.TypeOf<XmlException>());
            Assert.That(() => export.XmlElement("p:item", "sample"),
                Throws.TypeOf<XmlException>());
            Assert.That(() => export.XmlElement("item", "bad\0value"),
                Throws.TypeOf<XmlException>());
        });
    }

    [Test]
    public void SqlQuoteCasesRemainEscapedInInertText()
    {
        DatabaseProvider database = Fake.Create(12106).Database();

        Assert.Multiple(() =>
        {
            Assert.That(database.SqlIdentifierFromValue("a\"\"b"),
                Is.EqualTo("\"a\"\"\"\"b\""));
            Assert.That(database.SqlStringLiteral("a''b"),
                Is.EqualTo("'a''''b'"));
            Assert.That(database.SqlStringLiteral("'"),
                Is.EqualTo("''''"));
            Assert.That(database.InsertFromTableAndColumnAndValue(
                "order\"archive",
                "display\"name",
                "'quoted'"), Is.EqualTo(
                "INSERT INTO \"order\"\"archive\" (\"display\"\"name\") VALUES ('''quoted''');"));
        });
    }

    [Test]
    public void EdgeFormattingHonorsLimitsAndDoesNotConsumeEntropy()
    {
        const long seed = 12107;
        FakeOptions tinyOptions = FakeOptions.Create(seed);
        tinyOptions.MaximumOutputSize = 2;
        Fake tiny = Fake.Create(tinyOptions);
        FakeOptions countOptions = FakeOptions.Create(seed);
        countOptions.MaximumCount = 1;
        Fake count = Fake.Create(countOptions);
        ExportProvider inspected = Fake.Create(seed).Export();
        ExportProvider untouched = Fake.Create(seed).Export();

        Assert.Multiple(() =>
        {
            Assert.That(() => tiny.System().PathRoot(
                FilePathStyle.Windows),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tiny.System().ReservedPathName(
                FilePathStyle.Windows),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tiny.System().UnicodePathName(
                FilePathStyle.Unix),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => count.System().ReservedPathName(
                FilePathStyle.Unix),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => count.System().UnicodePathName(
                FilePathStyle.Windows),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tiny.Export().JsonString("abc"),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tiny.Export().JsonNumber(long.MaxValue),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tiny.Export().CsvField("a,b"),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tiny.Export().XmlElement("x", "&"),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => count.Export().JsonString("ab"),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.JsonString("sample"), Throws.Nothing);
            Assert.That(() => inspected.JsonNumberFromValue(double.MaxValue),
                Throws.Nothing);
            Assert.That(() => inspected.CsvField("=1+1"), Throws.Nothing);
            Assert.That(() => inspected.XmlElement("item", "sample"),
                Throws.Nothing);
            Assert.That(inspected.RandomValue.Long(),
                Is.EqualTo(untouched.RandomValue.Long()));
        });
    }

    static bool systemReserved(
        SystemProvider system,
        FilePathStyle style,
        string value)
    {
        return system.IsReservedPathName(style, value);
    }
}
