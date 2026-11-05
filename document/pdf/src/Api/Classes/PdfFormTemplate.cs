using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>Builds a labeled, interactive PDF form.</summary>
public class PdfFormTemplate : IPdfFormTemplate
{
    internal string Title = string.Empty;
    internal string Instructions = string.Empty;
    internal List<PdfFormTemplateField> Fields = new List<PdfFormTemplateField>();
    internal PdfDocument Document = new PdfDocument();
    internal bool Built;

    /// <summary>Initializes an interactive form template.</summary>
    /// <param name="title">The form title.</param>
    public PdfFormTemplate(string title)
    {
        ValidateText(title, "title");
        Title = title;
        Fields = new List<PdfFormTemplateField>();
        Document = new PdfDocument();
        Built = false;
    }

    /// <summary>Sets form instructions.</summary>
    /// <param name="instructions">The instructions text.</param>
    public void SetInstructions(string instructions)
    {
        EnsureNotBuilt();
        ValidateText(instructions, "instructions");
        Instructions = instructions;
    }

    /// <summary>Adds an interactive field.</summary>
    /// <param name="field">The field to add.</param>
    public void AddField(PdfFormTemplateField field)
    {
        EnsureNotBuilt();
        if (field == null) { throw new ArgumentNullException("field"); }
        ValidateKind(field.Kind);
        for (int index = 0; index < Fields.Count; index++)
        {
            if (Fields[index].Name == field.Name) { throw new ArgumentException("Form field name has already been added.", "field"); }
        }

        Fields.Add(field);
    }

    /// <summary>Builds the interactive form.</summary>
    /// <returns>The built document.</returns>
    public PdfDocument Build()
    {
        if (!Built)
        {
            PdfPage page = AddFormPage(false);
            double y = 650;
            PdfTextStyle labelStyle = PdfTextStyle.WithFont(10, PdfFont.Regular);
            for (int index = 0; index < Fields.Count; index++)
            {
                double requiredHeight = 48;
                if (Fields[index].Kind == PdfFormTemplateFieldKind.Signature) { requiredHeight = 64; }
                if (y - requiredHeight < 60)
                {
                    page = AddFormPage(true);
                    y = 680;
                }

                page.Canvas.DrawStyledText(Fields[index].Label, 72, y, labelStyle);
                if (Fields[index].Kind == PdfFormTemplateFieldKind.Text)
                {
                    Document.AddTextField(Fields[index].Name, page, 210, y - 7, 300, 22);
                }
                else if (Fields[index].Kind == PdfFormTemplateFieldKind.Checkbox)
                {
                    Document.AddCheckbox(Fields[index].Name, page, 210, y - 4, 14, 14);
                }
                else
                {
                    Document.AddSignaturePlaceholder(Fields[index].Name, page, 210, y - 17, 260, 34);
                }

                y -= requiredHeight;
            }

            Built = true;
        }

        return Document;
    }

    internal PdfPage AddFormPage(bool continuation)
    {
        PdfPage page = Document.AddPageWithDimensions(new PdfPageSizeFactory().Letter());
        PdfTextStyle titleStyle = PdfTextStyle.WithFont(20, PdfFont.Bold);
        PdfTextStyle bodyStyle = PdfTextStyle.WithFont(10, PdfFont.Regular);
        string title = Title;
        if (continuation) { title = Title + " - continued"; }
        page.Canvas.DrawStyledText(title, 72, 730, titleStyle);
        if (!continuation && Instructions.Length > 0) { page.Canvas.DrawStyledText(Instructions, 72, 700, bodyStyle); }
        return page;
    }

    internal void ValidateKind(PdfFormTemplateFieldKind kind)
    {
        if (kind == PdfFormTemplateFieldKind.Text || kind == PdfFormTemplateFieldKind.Checkbox || kind == PdfFormTemplateFieldKind.Signature) { return; }
        throw new ArgumentException("Form-template field kind is not supported.", "field");
    }

    internal void ValidateText(string text, string name)
    {
        if (text == null) { throw new ArgumentNullException(name); }
    }

    internal void EnsureNotBuilt()
    {
        if (Built) { throw new InvalidOperationException("Form template has already been built."); }
    }
}
