namespace docx;

internal enum DocxTextMode
{
    PlainText = 0,
    StoryText = 1
}

internal class DocxOptions
{
    internal DocxLoadOptions Load;
    internal DocxSaveOptions Save;
    internal DocxValidationOptions Validation;
    internal DocxRepairOptions Repair;
    internal DocxTextOptions Text;

    internal DocxOptions()
    {
        this.Load = new DocxLoadOptions();
        this.Save = new DocxSaveOptions();
        this.Validation = new DocxValidationOptions();
        this.Repair = new DocxRepairOptions();
        this.Text = new DocxTextOptions();
    }
}

internal class DocxValidationOptions
{
    internal DocxValidationMode Mode;
    internal bool ValidateOpcPackage;
    internal bool ValidateWordprocessingMl;
    internal bool ValidateRelationships;
    internal bool ValidateContentTypes;
    internal bool ValidateXml;

    internal DocxValidationOptions()
    {
        this.Mode = DocxValidationMode.Full;
        this.ValidateOpcPackage = true;
        this.ValidateWordprocessingMl = true;
        this.ValidateRelationships = true;
        this.ValidateContentTypes = true;
        this.ValidateXml = true;
    }
}

internal class DocxRepairOptions
{
    internal DocxRepairMode Mode;
    internal bool RepairMissingContentTypes;
    internal bool RepairMissingRelationships;
    internal bool RemoveOrphanedRelationships;
    internal bool RepairEmptyBody;

    internal DocxRepairOptions()
    {
        this.Mode = DocxRepairMode.Safe;
        this.RepairMissingContentTypes = true;
        this.RepairMissingRelationships = true;
        this.RemoveOrphanedRelationships = false;
        this.RepairEmptyBody = true;
    }
}
