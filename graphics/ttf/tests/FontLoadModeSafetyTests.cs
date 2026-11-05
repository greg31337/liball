using System.Buffers.Binary;
using ttf;

namespace ttf.Tests;

internal class FontLoadModeSafetyTests
{
    [Test]
    public void StrictRejectsAndCompatibleReportsCanonicalSearchFieldRepair()
    {
        byte[] font = System.Convert.FromBase64String(
            HarfBuzzLayoutDifferentialTests.OracleFontBase64);
        BinaryPrimitives.WriteUInt16BigEndian(font.AsSpan(6, 2), 0);

        FontPackage compatible = FontLoader.Load(
            font,
            CompatibleOptions(FontRepairPolicy.Safe));
        FontDiagnostic[] diagnostics = compatible.GetDiagnostics();

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() =>
                FontLoader.Load(font, FontLoadOptions.CreateStrict()));
            Assert.That(FindDiagnostic(
                diagnostics,
                FontDiagnosticCode.InvalidSearchFields), Is.Not.Null);
            Assert.That(FindDiagnostic(
                diagnostics,
                FontDiagnosticCode.RepairApplied), Is.Not.Null);
            Assert.That(FindDiagnostic(
                diagnostics,
                FontDiagnosticCode.RepairApplied)!.GetLogicalPath(),
                Is.EqualTo("sfnt.tableDirectory.searchFields"));
        });
    }

    [Test]
    public void CompatibleWithoutRepairPolicyRejectsRecoverableViolation()
    {
        byte[] font = System.Convert.FromBase64String(
            HarfBuzzLayoutDifferentialTests.OracleFontBase64);
        BinaryPrimitives.WriteUInt16BigEndian(font.AsSpan(6, 2), 0);

        Assert.Throws<FontFormatException>(() => FontLoader.Load(
            font,
            CompatibleOptions(FontRepairPolicy.None)));
    }

    [Test]
    public void InvalidInspectOnlyFaceExposesRawButNoSemanticData()
    {
        byte[] font = new TinySfntBuilder()
            .AddTable("name", new byte[6])
            .Build();
        FontPackage package = FontLoader.Load(
            font,
            FontLoadOptions.CreateInspectOnly());
        FontFace face = package.GetFace(0);

        Assert.Multiple(() =>
        {
            Assert.That(face.HasTable(FontTags.Name), Is.True);
            Assert.That(face.GetOutlineKind(), Is.EqualTo(FontOutlineKind.Unknown));
            Assert.That(face.CopyTable(FontTags.Name), Is.EqualTo(new byte[6]));
            Assert.That(face.GetTableInfo(0).GetTag(), Is.EqualTo(FontTags.Name));
            Assert.That(face.GetNameTable(), Is.Null);
            Assert.Throws<InvalidOperationException>(() => face.GetNames());
            Assert.Throws<InvalidOperationException>(() =>
                face.GetDescription());
            Assert.Throws<InvalidOperationException>(() =>
                face.CreateDefaultInstance());
        });
    }

    static FontLoadOptions CompatibleOptions(FontRepairPolicy repairPolicy)
    {
        return FontLoadOptions.Create(
            FontLoadMode.Compatible,
            FontInputOwnership.Copy,
            true,
            repairPolicy,
            FontChecksumPolicy.Ignore,
            FontLimits.CreateCompatible(),
            CancellationToken.None);
    }

    static FontDiagnostic? FindDiagnostic(
        FontDiagnostic[] diagnostics,
        FontDiagnosticCode code)
    {
        int index = 0;

        while (index < diagnostics.Length)
        {
            if (diagnostics[index].GetCode() == code)
            {
                return diagnostics[index];
            }

            index += 1;
        }

        return null;
    }
}
