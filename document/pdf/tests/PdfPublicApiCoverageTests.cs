using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfPublicApiCoverageTests
{
    [Test]
    public void PublicApiSurfaceMatchesBehavioralCoverageInventory()
    {
        string[] inventory = CreatePublicApiInventory();
        string inventoryText = string.Join("\n", inventory);
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(inventoryText)));

        Assert.Multiple(() =>
        {
            Assert.That(inventory.Count(item => item.StartsWith("type:", StringComparison.Ordinal)), Is.EqualTo(273));
            Assert.That(inventory.Count(item => item.StartsWith("constructor:", StringComparison.Ordinal)), Is.EqualTo(136));
            Assert.That(inventory.Count(item => item.StartsWith("field:", StringComparison.Ordinal)), Is.EqualTo(282));
            Assert.That(inventory.Count(item => item.StartsWith("enum:", StringComparison.Ordinal)), Is.EqualTo(175));
            Assert.That(inventory.Count(item => item.StartsWith("interface-method:", StringComparison.Ordinal)), Is.EqualTo(851));
            Assert.That(hash, Is.EqualTo("5C70B11E93BFA2D74D1ADD008AB39DB414F74E2F1E5A2FEEF58E09CCA419D380"), "Public API inventory changed. Add behavioral coverage for every new member, then review and update the approved fingerprint. Actual: " + hash);
        });
    }

    [Test]
    public void OutlineChildrenRejectDuplicatesReparentingAndCycles()
    {
        PdfPage page = new PdfPage(100, 100);
        PdfOutlineItem root = new PdfOutlineItem("Root", page, 0, 100);
        PdfOutlineItem child = new PdfOutlineItem("Child", page, 0, 90);
        PdfOutlineItem grandchild = new PdfOutlineItem("Grandchild", page, 0, 80);
        PdfOutlineItem other = new PdfOutlineItem("Other", page, 0, 70);

        root.AddChild(child);
        child.AddChild(grandchild);

        Assert.That(root.Children[0], Is.SameAs(child));
        Assert.That(child.Parent, Is.SameAs(root));
        Assert.Throws<ArgumentException>(() => root.AddChild(root));
        Assert.Throws<ArgumentException>(() => root.AddChild(child));
        Assert.Throws<ArgumentException>(() => grandchild.AddChild(root));
        Assert.Throws<ArgumentException>(() => other.AddChild(child));
        Assert.Throws<ArgumentNullException>(() => root.AddChild(null!));
    }

    [Test]
    public void NavigationAndFormConvenienceMembersPreserveBehavior()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);
        PdfNamedDestination destination = document.AddNamedDestinationAtPageTop("top", page);
        PdfOutlineItem root = document.AddOutlineAtPageTop("Root", page);
        PdfOutlineItem child = document.AddOutlineChildAtPageTop(root, "Child", page);
        PdfAcroFormField text = document.AddTextFieldInRectangle("name", page, new PdfRectangle(10, 50, 80, 20));
        PdfAcroFormField checkbox = document.AddCheckboxInRectangle("accept", page, new PdfRectangle(10, 20, 12, 12));

        text.SetAccessibilityName("Full name");
        Assert.That(destination.Y, Is.EqualTo(100));
        Assert.That(child.Parent, Is.SameAs(root));
        Assert.That(text.GetAccessibilityName(), Is.EqualTo("Full name"));
        Assert.That(checkbox.GetFieldType(), Is.EqualTo("Btn"));
        Assert.That(document.AcroForm.GetFieldCount(), Is.EqualTo(2));

        document.AcroForm.ClearFields();
        Assert.That(document.AcroForm.GetFieldCount(), Is.EqualTo(0));
        Assert.That(document.AcroForm.GetRadioButtonGroupCount(), Is.EqualTo(0));
    }

    [Test]
    public void PreviouslyUncoveredFactoryStateAndHighLevelConvenienceMembersBehave()
    {
        PdfStyleFactory styles = new PdfStyleFactory();
        Assert.That(styles.CreateElementStyle(), Is.Not.Null);
        Assert.That(styles.CreateTableStyle(), Is.Not.Null);

        PdfPath path = new PdfPath();
        Assert.That(path.HasCommands(), Is.False);
        path.MoveTo(0, 0).LineTo(1, 1);
        Assert.That(path.HasCommands(), Is.True);

        PdfDocument document = new PdfDocument();
        PdfPageCursor cursor = new PdfPageCursorFactory().CreateDefault(document);
        Assert.That(document.GetPageCount(), Is.EqualTo(1));
        cursor.AddPage();
        Assert.That(document.GetPageCount(), Is.EqualTo(2));
        Assert.That(document.GetFontRegistry().HasFont(PdfFont.Regular), Is.True);

        PdfReport report = new PdfReport("Report");
        report.AddReportSection(new PdfReportSection("Section", "Body"));
        report.AddReportKpi(new PdfKpi("Metric", "Value"));
        Assert.That(AllContent(report.Build()), Does.Contain("(Section) Tj"));

        PdfPaper paper = new PdfPaper("Paper");
        paper.AddPaperSection(new PdfPaperSection("Section"));
        Assert.That(AllContent(paper.Build()), Does.Contain("(Section) Tj"));

        PdfPage radioPage = new PdfPage(100, 100);
        PdfRadioButtonGroup group = new PdfRadioButtonGroup("choice");
        PdfAcroFormField option = group.AddOption("one", radioPage, 1, 1, 10, 10);
        group.SetAccessibilityName("Choice");
        group.SelectOption("one");
        group.ClearSelection();
        Assert.That(group.GetSelectedValue(), Is.Empty);
        Assert.That(group.GetAccessibilityName(), Is.EqualTo("Choice"));
        Assert.That(option.IsChecked(), Is.False);
    }

    [Test]
    public void RemainingCanvasShapeHelpersWriteExpectedPathsAndPaintOperators()
    {
        PdfCanvas canvas = new PdfCanvas();

        canvas.FillCircle(10, 10, 2);
        canvas.FillStrokeCircle(20, 20, 3);
        canvas.StrokeEllipse(30, 40, 4, 5);
        canvas.FillStrokeEllipse(50, 60, 6, 7);
        canvas.StrokeRoundedRectangle(5, 6, 20, 10, 2);

        string content = canvas.ToPdfString();
        string[] lines = content.Split('\n');
        Assert.That(content, Does.Contain("12 10 m"));
        Assert.That(content, Does.Contain("23 20 m"));
        Assert.That(content, Does.Contain("34 40 m"));
        Assert.That(content, Does.Contain("56 60 m"));
        Assert.That(content, Does.Contain("7 6 m"));
        Assert.That(lines.Count(line => line == "f"), Is.EqualTo(1));
        Assert.That(lines.Count(line => line == "B"), Is.EqualTo(2));
        Assert.That(lines.Count(line => line == "S"), Is.EqualTo(2));
    }

    [Test]
    public void LayoutConvenienceMembersExposeMeasurementsStylesAndCalculatedValues()
    {
        PdfParagraphStyle paragraphStyle = new PdfParagraphStyle();
        paragraphStyle.LineHeight = 20;
        PdfTextBlock text = new PdfTextBlock("text", new PdfParagraphStyle());
        text.SetParagraphStyle(paragraphStyle);
        Assert.That(text.Measure(new PdfLayoutContext(100, 100)).Size.Height, Is.EqualTo(20));

        PdfListBlock list = new PdfListBlock();
        list.SetMarkerStyle(PdfListMarkerStyle.Numbered);
        list.AddBlock(PdfBlock.WithSize(10, 10));
        PdfPage listPage = new PdfPage(100, 100);
        list.Draw(new PdfLayoutDrawContext(listPage, 0, 0, 50, 50));
        Assert.That(listPage.GetCanvas().ToPdfString(), Does.Contain("(1.) Tj"));
        Assert.Throws<ArgumentException>(() => list.SetMarkerStyle((PdfListMarkerStyle)99));

        PdfCaption caption = new PdfCaption(PdfBlock.WithSize(10, 10));
        caption.SetAccessibilityDescription("A concise caption");
        Assert.That(caption.GetAccessibilityDescription(), Is.EqualTo("A concise caption"));

        PdfInvoiceLine line = new PdfInvoiceLine("Work", 2, 10);
        line.SetDiscount(5);
        line.SetTaxRate(0.1);
        Assert.That(line.GetSubtotal(), Is.EqualTo(20));
        Assert.That(line.GetTaxAmount(), Is.EqualTo(1.5));
        Assert.That(line.GetTotal(), Is.EqualTo(16.5));

        PdfReceipt receipt = new PdfReceipt("Store");
        receipt.AddLine("One", 2, 3);
        receipt.AddLine("Two", 1, 4);
        Assert.That(receipt.GetSubtotal(), Is.EqualTo(10));

        PdfTableBlock table = new PdfTableBlock(new double[] { 40 });
        table.AddHeaderRow().AddBlock(PdfBlock.WithSize(20, 8));
        table.AddFooterRow().AddBlock(PdfBlock.WithSize(20, 6));
        Assert.That(table.MeasureHeaderRowsHeight(100), Is.EqualTo(8));
        Assert.That(table.MeasureFooterRowsHeight(100), Is.EqualTo(6));
    }

    [Test]
    public void CursorConvenienceMembersMeasureDrawAndFlowThroughPublicEntryPoints()
    {
        PdfDocument imageDocument = new PdfDocument();
        PdfPageCursor imageCursor = new PdfPageCursorFactory().Create(
            imageDocument,
            new PdfPageDimensions(100, 80),
            new PdfPageMargins(10, 5, 10, 5));
        PdfLayoutContext measureContext = imageCursor.GetMeasureContext();
        PdfImage image = new PdfImage("InventoryImage", 1, 1, new byte[] { 1, 2, 3 });
        PdfLayoutResult imageResult = imageCursor.DrawImageBlock(new PdfImageBlock(image, 20, 10));

        Assert.That(measureContext.AvailableWidth, Is.EqualTo(80));
        Assert.That(measureContext.AvailableHeight, Is.EqualTo(70));
        Assert.That(imageResult.Size.Width, Is.EqualTo(20));
        Assert.That(imageDocument.GetPage(0).GetCanvas().ToPdfString(), Does.Contain("/InventoryImage Do"));
        Assert.Throws<ArgumentNullException>(() => imageCursor.DrawImageBlock(null!));

        PdfLayoutItem ordinary = new PdfLayoutItem(PdfBlock.WithSize(12, 7));
        PdfDocument ordinaryDocument = new PdfDocument();
        PdfPageCursor ordinaryCursor = new PdfPageCursorFactory().CreateDefault(ordinaryDocument);
        PdfLayoutResult ordinaryResult = ordinary.DrawWithoutKeepTogether(ordinaryCursor);
        Assert.That(ordinaryResult.Size.Height, Is.EqualTo(7));

        PdfLayoutItem kept = new PdfLayoutItem(PdfBlock.WithSize(12, 7));
        kept.SetKeepTogether();
        PdfDocument keptDocument = new PdfDocument();
        PdfPageCursor keptCursor = new PdfPageCursorFactory().CreateDefault(keptDocument);
        PdfLayoutResult keptResult = kept.DrawWithCursor(keptCursor);
        Assert.That(keptResult.Size.Height, Is.EqualTo(7));
        Assert.Throws<ArgumentNullException>(() => ordinary.DrawWithoutKeepTogether(null!));
        Assert.Throws<ArgumentNullException>(() => kept.DrawWithCursor(null!));
    }

    [Test]
    public void TemplateClearMembersFallBackAndFinalPassResolvesPageText()
    {
        PdfParagraphStyle style = new PdfParagraphStyle();
        PdfDocument layoutDocument = new PdfDocument();
        PdfLayoutDocument layout = new PdfLayoutDocument(
            layoutDocument,
            new PdfPageDimensions(100, 60),
            new PdfPageMargins(10, 5, 10, 5));
        layout.SetHeaderTemplate(PdfPageTemplate.FromPageText("default-head {page}/{total}", style, 10));
        layout.SetFooterTemplate(PdfPageTemplate.FromPageText("default-foot {page}/{total}", style, 10));
        layout.SetFirstPageHeaderTemplate(PdfPageTemplate.FromPageText("first-head", style, 10));
        layout.SetFirstPageFooterTemplate(PdfPageTemplate.FromPageText("first-foot", style, 10));
        layout.SetEvenPageHeaderTemplate(PdfPageTemplate.FromPageText("even-head", style, 10));
        layout.SetEvenPageFooterTemplate(PdfPageTemplate.FromPageText("even-foot", style, 10));
        layout.ClearFirstPageHeaderTemplate();
        layout.ClearFirstPageFooterTemplate();
        layout.ClearEvenPageHeaderTemplate();
        layout.ClearEvenPageFooterTemplate();
        layout.AddBlock(PdfBlock.WithSize(10, 10));
        layout.AddPageBreak();
        layout.AddBlock(PdfBlock.WithSize(10, 10));
        layout.Render();

        Assert.That(layoutDocument.GetPage(0).GetCanvas().ToPdfString(), Does.Contain("(default-head) Tj").And.Contain("(1/2) Tj"));
        Assert.That(layoutDocument.GetPage(1).GetCanvas().ToPdfString(), Does.Contain("(default-foot) Tj").And.Contain("(2/2) Tj"));
        Assert.That(AllContent(layoutDocument), Does.Not.Contain("first-head").And.Not.Contain("even-head"));

        PdfDocument cursorDocument = new PdfDocument();
        PdfPageCursor cursor = new PdfPageCursorFactory().Create(
            cursorDocument,
            new PdfPageDimensions(100, 60),
            new PdfPageMargins(10, 5, 10, 5));
        cursor.SetHeaderTemplate(PdfPageTemplate.FromPageText("cursor-head {page}/{total}", style, 10));
        cursor.SetFooterTemplate(PdfPageTemplate.FromPageText("cursor-foot {page}/{total}", style, 10));
        cursor.SetFirstPageHeaderTemplate(PdfPageTemplate.FromPageText("cursor-first-head", style, 10));
        cursor.SetFirstPageFooterTemplate(PdfPageTemplate.FromPageText("cursor-first-foot", style, 10));
        cursor.SetEvenPageHeaderTemplate(PdfPageTemplate.FromPageText("cursor-even-head", style, 10));
        cursor.SetEvenPageFooterTemplate(PdfPageTemplate.FromPageText("cursor-even-foot", style, 10));
        cursor.ClearFirstPageHeaderTemplate();
        cursor.ClearFirstPageFooterTemplate();
        cursor.ClearEvenPageHeaderTemplate();
        cursor.ClearEvenPageFooterTemplate();
        cursor.AddPage();
        cursor.DrawFinalPageTemplates();

        Assert.That(cursorDocument.GetPage(0).GetCanvas().ToPdfString(), Does.Contain("(cursor-head 1/2) Tj"));
        Assert.That(cursorDocument.GetPage(1).GetCanvas().ToPdfString(), Does.Contain("(cursor-foot 2/2) Tj"));
        Assert.That(AllContent(cursorDocument), Does.Not.Contain("cursor-first").And.Not.Contain("cursor-even"));
    }

    [Test]
    public void ClippingCanBeReenabledForAbsoluteElementsAndContentLayers()
    {
        PdfLayoutFactory factory = new PdfLayoutFactory();
        PdfAbsoluteLayoutElement absolute = factory.CreateAbsoluteElement(factory.CreateFixedBlock(10, 10), 2, 3, 10, 10);
        absolute.DisableClipping();
        absolute.EnableClipping();
        PdfPage absolutePage = new PdfPage(50, 50);
        absolute.Draw(new PdfLayoutDrawContext(absolutePage, 0, 0, 20, 20));
        Assert.That(absolutePage.GetCanvas().ToPdfString(), Does.Contain("2 3 10 10 re\nW\nn"));

        PdfContentLayer layer = factory.CreateContentLayer(
            factory.CreateBackgroundTemplate(PdfColor.Gray(0.5), 20),
            PdfContentLayerPlacement.Background,
            0);
        layer.SetBounds(new PdfRectangle(4, 5, 20, 15));
        layer.DisableClipping();
        layer.EnableClipping();
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = new PdfLayoutDocument(document, new PdfPageDimensions(50, 50), new PdfPageMargins(0, 0, 0, 0));
        layout.AddContentLayer(layer);
        layout.Render();
        Assert.That(document.GetPage(0).GetCanvas().ToPdfString(), Does.Contain("4 5 20 15 re\nW\nn"));
    }

    [Test]
    public void TrueTypeFontFileRegistrationStoresTheLoadedFont()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "public-api-font-" + Guid.NewGuid().ToString("N") + ".ttf");
        File.WriteAllBytes(path, TestSfntFont.Create(0x00010000U, 0, false));

        try
        {
            PdfFontRegistry registry = new PdfFontRegistry();
            PdfTrueTypeFont registryFont = registry.RegisterTrueTypeFontFromFile(path);
            PdfDocument document = new PdfDocument();
            PdfTrueTypeFont documentFont = document.RegisterTrueTypeFontFromFile(path);

            Assert.That(registry.HasTrueTypeFont(registryFont.Identity), Is.True);
            Assert.That(document.GetFontRegistry().HasTrueTypeFont(documentFont.Identity), Is.True);
            Assert.That(documentFont.Identity, Is.EqualTo(registryFont.Identity));
        }
        finally
        {
            File.Delete(path);
        }
    }

    static string[] CreatePublicApiInventory()
    {
        List<string> inventory = new List<string>();
        Type[] types = typeof(PdfDocument).Assembly.GetExportedTypes();
        foreach (Type type in types)
        {
            if (type.Namespace != "pdf")
            {
                continue;
            }

            inventory.Add("type:" + TypeName(type));
            foreach (ConstructorInfo constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                inventory.Add("constructor:" + TypeName(type) + "(" + Parameters(constructor.GetParameters()) + ")");
            }

            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
            foreach (FieldInfo field in fields)
            {
                if (!type.IsEnum && !field.IsSpecialName)
                {
                    inventory.Add("field:" + TypeName(type) + "." + field.Name + ":" + TypeName(field.FieldType));
                }
            }

            if (type.IsEnum)
            {
                foreach (string name in Enum.GetNames(type))
                {
                    object value = Enum.Parse(type, name);
                    inventory.Add("enum:" + TypeName(type) + "." + name + "=" + Convert.ToInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture));
                }
            }

            if (type.IsInterface)
            {
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    inventory.Add("interface-method:" + TypeName(type) + "." + method.Name + "(" + Parameters(method.GetParameters()) + "):" + TypeName(method.ReturnType));
                }
            }
        }

        inventory.Sort(StringComparer.Ordinal);
        return inventory.ToArray();
    }

    static string Parameters(ParameterInfo[] parameters)
    {
        string[] names = new string[parameters.Length];
        for (int index = 0; index < parameters.Length; index++)
        {
            names[index] = TypeName(parameters[index].ParameterType);
        }

        return string.Join(",", names);
    }

    static string TypeName(Type type)
    {
        if (type.IsArray)
        {
            return TypeName(type.GetElementType()!) + "[]";
        }

        if (type.IsGenericType)
        {
            string name = type.GetGenericTypeDefinition().FullName!;
            int marker = name.IndexOf('`');
            if (marker >= 0)
            {
                name = name.Substring(0, marker);
            }

            return name + "<" + string.Join(",", type.GetGenericArguments().Select(TypeName)) + ">";
        }

        return type.FullName ?? type.Name;
    }

    static string AllContent(PdfDocument document)
    {
        StringBuilder content = new StringBuilder();
        foreach (PdfPage page in document.Pages)
        {
            content.Append(page.GetCanvas().ToPdfString());
        }

        return content.ToString();
    }
}
