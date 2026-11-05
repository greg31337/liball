using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxOptionsTests
{
    [Test]
    public void DocxOptions_CreatesAllOptionGroups()
    {
        global::docx.DocxOptions options = new global::docx.DocxOptions();

        Assert.That(options.Load, Is.Not.Null);
        Assert.That(options.Save, Is.Not.Null);
        Assert.That(options.Validation, Is.Not.Null);
        Assert.That(options.Repair, Is.Not.Null);
        Assert.That(options.Text, Is.Not.Null);
    }

    [Test]
    public void LoadAndSaveOptions_DefaultToPreservingUnknownContent()
    {
        global::docx.DocxLoadOptions load = new global::docx.DocxLoadOptions();
        global::docx.DocxSaveOptions save = new global::docx.DocxSaveOptions();

        Assert.That(load.PreserveUnknownParts, Is.True);
        Assert.That(load.PreserveUnknownRelationships, Is.True);
        Assert.That(load.PreserveUnknownMarkup, Is.True);
        Assert.That(load.PreserveXmlWhitespace, Is.True);
        Assert.That(load.RepairRecoverablePackageIssues, Is.False);
        Assert.That(load.LazyLoadXmlParts, Is.True);
        Assert.That(load.LazyLoadBinaryParts, Is.True);
        Assert.That(load.ValidationMode, Is.EqualTo(global::docx.DocxValidationMode.None));
        Assert.That(load.RepairMode, Is.EqualTo(global::docx.DocxRepairMode.Disabled));
        Assert.That(load.ValidationReport, Is.Null);
        Assert.That(load.RepairReport, Is.Null);

        Assert.That(save.PreserveUnknownParts, Is.True);
        Assert.That(save.PreserveUnknownRelationships, Is.True);
        Assert.That(save.PreserveUnknownMarkup, Is.True);
        Assert.That(save.PreserveXmlWhitespace, Is.True);
        Assert.That(save.Deterministic, Is.False);
        Assert.That(save.Mode, Is.EqualTo(global::docx.DocxSaveMode.Standard));
        Assert.That(save.UsesDeterministicOutput(), Is.False);
        Assert.That(save.CompressionMode, Is.EqualTo(global::docx.DocxCompressionMode.Optimal));
        Assert.That(save.OutputType, Is.EqualTo(global::docx.DocxOutputType.Preserve));
    }

    [Test]
    public void ValidationAndRepairOptions_ExposeConservativeDefaults()
    {
        global::docx.DocxValidationOptions validation = new global::docx.DocxValidationOptions();
        global::docx.DocxRepairOptions repair = new global::docx.DocxRepairOptions();

        Assert.That(validation.Mode, Is.EqualTo(global::docx.DocxValidationMode.Full));
        Assert.That(validation.ValidateOpcPackage, Is.True);
        Assert.That(validation.ValidateWordprocessingMl, Is.True);
        Assert.That(validation.ValidateRelationships, Is.True);
        Assert.That(validation.ValidateContentTypes, Is.True);
        Assert.That(validation.ValidateXml, Is.True);

        Assert.That(repair.Mode, Is.EqualTo(global::docx.DocxRepairMode.Safe));
        Assert.That(repair.RepairMissingContentTypes, Is.True);
        Assert.That(repair.RepairMissingRelationships, Is.True);
        Assert.That(repair.RemoveOrphanedRelationships, Is.False);
        Assert.That(repair.RepairEmptyBody, Is.True);
    }

    [Test]
    public void TextOptions_ExposeExtractionModeSeparatorsAndRevisionControls()
    {
        global::docx.DocxTextOptions text = new global::docx.DocxTextOptions();

        Assert.That(text.Mode, Is.EqualTo(global::docx.DocxTextMode.PlainText));
        Assert.That(text.ParagraphSeparator, Is.EqualTo("\n"));
        Assert.That(text.TableCellSeparator, Is.EqualTo("\t"));
        Assert.That(text.TableRowSeparator, Is.EqualTo("\n"));
        Assert.That(text.TabSeparator, Is.EqualTo("\t"));
        Assert.That(text.LineBreakSeparator, Is.EqualTo("\n"));
        Assert.That(text.PageBreakSeparator, Is.EqualTo("\n"));
        Assert.That(text.ColumnBreakSeparator, Is.EqualTo("\n"));
        Assert.That(text.SoftHyphenText, Is.EqualTo(""));
        Assert.That(text.NoBreakHyphenText, Is.EqualTo("-"));
        Assert.That(text.SymbolText, Is.EqualTo(""));
        Assert.That(text.IncludeHeadersAndFooters, Is.True);
        Assert.That(text.IncludeFootnotesAndEndnotes, Is.True);
        Assert.That(text.IncludeComments, Is.False);
        Assert.That(text.IncludeFieldResults, Is.True);
        Assert.That(text.IncludeInsertedText, Is.True);
        Assert.That(text.IncludeDeletedText, Is.False);
    }

    [Test]
    public void OptionFields_AreMutableForCallers()
    {
        global::docx.DocxOptions options = new global::docx.DocxOptions();

        options.Save.Deterministic = true;
        options.Save.Mode = global::docx.DocxSaveMode.DeterministicRoundTrip;
        options.Save.CompressionMode = global::docx.DocxCompressionMode.NoCompression;
        options.Repair.Mode = global::docx.DocxRepairMode.Disabled;
        options.Text.IncludeComments = true;

        Assert.That(options.Save.Deterministic, Is.True);
        Assert.That(options.Save.Mode, Is.EqualTo(global::docx.DocxSaveMode.DeterministicRoundTrip));
        Assert.That(options.Save.UsesDeterministicOutput(), Is.True);
        Assert.That(options.Save.CompressionMode, Is.EqualTo(global::docx.DocxCompressionMode.NoCompression));
        Assert.That(options.Repair.Mode, Is.EqualTo(global::docx.DocxRepairMode.Disabled));
        Assert.That(options.Text.IncludeComments, Is.True);
    }

    [Test]
    public void SaveOptions_DeterministicRoundTripModeUsesDeterministicOutput()
    {
        global::docx.DocxSaveOptions options = new global::docx.DocxSaveOptions();

        options.Mode = global::docx.DocxSaveMode.DeterministicRoundTrip;

        Assert.That(options.Deterministic, Is.False);
        Assert.That(options.UsesDeterministicOutput(), Is.True);
    }
}
