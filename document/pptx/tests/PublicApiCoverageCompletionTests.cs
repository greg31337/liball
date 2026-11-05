using System.Reflection;
using pptx;

namespace pptx.Tests;

public class PublicApiCoverageCompletionTests
{
    [Test]
    public void ValueOperationFacades_CoverPositiveBoundaryAndFailureBehavior()
    {
        IPptxBoundsOperations bounds = new PptxBoundsOperations();
        PptxBounds left = bounds.FromInches(1.0, 2.0, 3.0, 4.0);
        PptxBounds right = new PptxBounds(left.X + left.Width - 1L, left.Y, 10L, 10L);

        Assert.Multiple(() =>
        {
            Assert.That(bounds.Right(left), Is.EqualTo(left.X + left.Width));
            Assert.That(bounds.Bottom(left), Is.EqualTo(left.Y + left.Height));
            Assert.That(bounds.IsEmpty(left), Is.False);
            Assert.That(bounds.Contains(left, left.X, left.Y), Is.True);
            Assert.That(bounds.Contains(left, bounds.Right(left), left.Y), Is.True);
            Assert.That(bounds.Contains(left, bounds.Right(left) + 1L, left.Y), Is.False);
            Assert.That(bounds.Intersects(left, right), Is.True);
            Assert.That(bounds.Union(left, right).Width, Is.GreaterThan(left.Width));
            Assert.That(bounds.Offset(left, 10L, 20L).X, Is.EqualTo(left.X + 10L));
        });

        IPptxResultOperations results = new PptxResultOperations();
        IPptxDiagnosticOperations diagnostics = new PptxDiagnosticOperations();
        PptxResult result = new PptxResult();
        PptxDiagnostic info = diagnostics.CreateInfo("I1", "info", "/ppt/presentation.xml", "", "");
        results.Add(result, info);
        results.AddWarning(result, "W1", "warning", "", "", "");
        results.AddError(result, "E1", "error", "", "", "");

        Assert.Multiple(() =>
        {
            Assert.That(results.Count(result), Is.EqualTo(3));
            Assert.That(results.Succeeded(result), Is.False);
            Assert.That(results.Failed(result), Is.True);
            Assert.That(results.HasErrors(result), Is.True);
            Assert.That(results.FormatDiagnostics(result), Does.Contain("E1"));
            Assert.That(diagnostics.IsError(result.Diagnostics[2]), Is.True);
            Assert.That(diagnostics.HasLocation(info), Is.True);
            Assert.That(diagnostics.Format(info), Does.Contain("I1"));
        });

        IPptxUriOperations uris = new PptxUriOperations();
        Assert.Multiple(() =>
        {
            Assert.That(uris.NormalizePartName("ppt/slides/slide1.xml"), Is.EqualTo("/ppt/slides/slide1.xml"));
            Assert.That(uris.IsValidPartName("/ppt/slides/slide1.xml"), Is.True);
            Assert.That(uris.IsValidPartName("../slide.xml"), Is.False);
            Assert.That(uris.ValidatePartName("../slide.xml").Succeeded(), Is.False);
            Assert.That(uris.ToZipEntryName("/ppt/slides/slide1.xml"), Is.EqualTo("ppt/slides/slide1.xml"));
            Assert.That(uris.IsExternalTarget("https://example.test/a"), Is.True);
            Assert.That(uris.IsValidInternalRelationshipTarget("/ppt/slides/slide1.xml", "../media/image1.png"), Is.True);
            Assert.That(uris.ValidateInternalRelationshipTarget("/ppt/slides/slide1.xml", "../../../bad.xml").Succeeded(), Is.False);
            Assert.That(uris.BuildInternalRelationshipTarget("/ppt/slides/slide1.xml", "../media/image1.png"), Is.EqualTo("/ppt/media/image1.png"));
        });

        IPptxContentTypeManifestOperations contentTypes = new PptxContentTypeManifestOperations();
        PptxContentTypeManifest manifest = new PptxContentTypeManifest();
        contentTypes.AddDefault(manifest, "xml", "application/xml");
        const string presentationContentType = "application/vnd.openxmlformats-officedocument.presentationml.presentation.main+xml";
        contentTypes.AddOverride(manifest, "/ppt/presentation.xml", presentationContentType);
        Assert.Multiple(() =>
        {
            Assert.That(contentTypes.Succeeded(manifest), Is.True);
            Assert.That(contentTypes.Failed(manifest), Is.False);
            Assert.That(contentTypes.DefaultCount(manifest), Is.EqualTo(1));
            Assert.That(contentTypes.OverrideCount(manifest), Is.EqualTo(1));
            Assert.That(contentTypes.GetDefaultContentType(manifest, "xml"), Is.EqualTo("application/xml"));
            Assert.That(contentTypes.GetOverrideContentType(manifest, "/ppt/presentation.xml"), Is.EqualTo(presentationContentType));
            Assert.That(contentTypes.GetContentTypeForPartName(manifest, "/ppt/other.xml"), Is.EqualTo("application/xml"));
        });

        IPptxTemplateDataOperations templates = new PptxTemplateDataOperations();
        PptxTemplateData template = templates.FromDictionary(new Dictionary<string, string> { ["name"] = "Ada" });
        templates.Add(template, "role", "Engineer");
        Assert.Multiple(() =>
        {
            Assert.That(templates.Count(template), Is.EqualTo(2));
            Assert.That(templates.Contains(template, "name"), Is.True);
            Assert.That(templates.Get(template, "role"), Is.EqualTo("Engineer"));
            Assert.That(templates.IndexOf(template, "missing"), Is.EqualTo(-1));
            Assert.That(templates.FromLists(["a"], ["b"]).Get("a"), Is.EqualTo("b"));
            Assert.That(() => templates.FromLists(["a"], []), Throws.ArgumentException);
        });
    }

    [Test]
    public void ExportAndTemplateFacades_RoundTripPublicWorkflow()
    {
        IPptxDocumentFactory factory = new PptxDocumentFactory();
        Assert.That(factory.Create().Validate().Failed(), Is.True);
        Assert.That(factory.CreateMinimumPresentation().Validate().Succeeded(), Is.True);
        PptxDocument document = factory.CreatePresentation();
        PptxPresentation presentation = document.LoadPresentation();
        PptxSlide slide = presentation.Slides[0];
        slide.AddTextBox("Hello {{name}}", PptxBounds.FromInches(1.0, 1.0, 4.0, 1.0));

        IPptxTemplateFiller filler = new PptxTemplateFiller();
        IPptxTextExtractor extractor = new PptxTextExtractor();
        IPptxTextOutlineExporter outline = new PptxTextOutlineExporter();
        IPptxHtmlExporter html = new PptxHtmlExporter();
        IPptxSvgExporter svg = new PptxSvgExporter();
        IPptxJsonSummaryExporter json = new PptxJsonSummaryExporter();

        Assert.That(filler.FillPresentationFromDictionary(presentation, new Dictionary<string, string> { ["name"] = "Ada" }), Is.EqualTo(1));
        Assert.That(filler.FillPresentationFromLists(presentation, ["name"], ["Grace"]), Is.EqualTo(0));
        Assert.That(filler.FillSlideFromDictionary(slide, new Dictionary<string, string> { ["missing"] = "value" }), Is.EqualTo(0));
        Assert.That(filler.FillSlideFromLists(slide, ["missing"], ["value"]), Is.EqualTo(0));
        PptxTemplateData directTemplate = new PptxTemplateData();
        directTemplate.Keys.Add("direct");
        directTemplate.Values.Add("filled");
        slide.AddTextBox("{{direct}}", new PptxBounds(0L, 0L, 100L, 100L));
        Assert.That(filler.FillSlide(slide, directTemplate), Is.EqualTo(1));
        slide.AddTextBox("{{direct}}", new PptxBounds(0L, 100L, 100L, 100L));
        Assert.That(filler.FillPresentation(presentation, directTemplate), Is.EqualTo(1));

        Assert.Multiple(() =>
        {
            Assert.That(extractor.ExtractPresentationText(presentation), Does.Contain("Ada"));
            Assert.That(extractor.ExtractSlideText(slide), Does.Contain("Ada"));
            Assert.That(extractor.ExtractNotesText(presentation, slide), Is.Empty);
            Assert.That(extractor.ExtractRelatedPartsText(presentation, slide, PptxRelationshipTypes.NotesSlide), Is.Empty);
            Assert.That(outline.ExportPresentationText(presentation), Does.Contain("Ada"));
            Assert.That(outline.ExportSlideText(slide), Does.Contain("Ada"));
            Assert.That(outline.ExportPresentationOutline(presentation), Does.Contain("Ada"));
            Assert.That(outline.ExportSlideOutline(slide), Does.Contain("Ada"));
            Assert.That(outline.GetPresentationOutlineEntries(presentation), Is.Not.Empty);
            Assert.That(outline.GetSlideOutlineEntries(slide), Is.Not.Empty);
            Assert.That(outline.GetSlideOutlineEntriesAtIndex(slide, 0), Is.Not.Empty);
            Assert.That(html.ExportPresentation(presentation), Does.Contain("Ada"));
            Assert.That(html.ExportSlide(slide), Does.Contain("Ada"));
            Assert.That(svg.ExportPresentation(presentation), Does.Contain("<svg"));
            Assert.That(svg.ExportSlide(slide), Does.Contain("<svg"));
            Assert.That(svg.GetFidelityLimitations(), Is.Not.Empty);
            Assert.That(json.ExportPackage(document.GetPackage()), Does.Contain("parts"));
            Assert.That(json.ExportPresentation(presentation), Does.Contain("slideCount"));
        });

        PptxOutlineEntry bullet = new PptxOutlineEntry();
        bullet.HasBullet = true;
        bullet.BulletType = PptxBullet.Character;
        bullet.BulletCharacter = "•";
        Assert.That(outline.GetBulletPrefix(bullet), Is.EqualTo("• "));

        using (MemoryStream stream = new MemoryStream())
        {
            document.Save(stream);
            Assert.That(stream.Length, Is.GreaterThan(0L));
        }
        Assert.That(extractor.ExtractPresentationText(presentation), Does.Contain("Ada"));

        IPptxImageDetector detector = new PptxImageDetector();
        Assert.That(detector.DetectPart(document.GetPackage().Parts[0]).Type, Is.EqualTo(PptxImageType.Unknown));
        Assert.That(extractor.ExtractPresentationText(presentation), Does.Contain("Ada"));

        PptxDocument reopened = factory.OpenBytes(document.SaveBytes());
        Assert.That(extractor.ExtractPresentationText(reopened.LoadPresentation()), Does.Contain("Ada"));
    }

    [Test]
    public void ProcessingServiceFacades_ExercisePositiveAndEmptyAssetWorkflows()
    {
        PptxDocument document = new PptxDocumentFactory().CreatePresentation();
        PptxPresentation presentation = document.LoadPresentation();
        PptxSlide slide = presentation.Slides[0];
        PptxShape shape = slide.AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 100L, 100L));
        shape.SetText("before");

        IPptxTextReplacer replacer = new PptxTextReplacer();
        Assert.That(replacer.ReplacePresentationText(presentation, "before", "after"), Is.EqualTo(1));
        Assert.That(replacer.ReplaceSlideText(slide, "missing", "replacement"), Is.EqualTo(0));

        IPptxSecurityScanner security = new PptxSecurityScanner();
        Assert.That(security.Scan(document.GetPackage()).Succeeded(), Is.True);

        PptxTheme theme = presentation.Themes[0];
        IPptxStyleResolver styles = new PptxStyleResolver();
        PptxColor color = PptxColor.CreateRgb("123456");
        PptxFill fill = PptxFill.CreateSolid(color);
        PptxLine line = PptxLine.CreateSolid(color, 10L);
        Assert.Multiple(() =>
        {
            Assert.That(styles.ResolveColor(theme, color).Value, Is.EqualTo("123456"));
            Assert.That(styles.ResolveFill(theme, fill).Color.Value, Is.EqualTo("123456"));
            Assert.That(styles.ResolveLine(theme, line).Color.Value, Is.EqualTo("123456"));
            Assert.That(styles.ResolveShapeFill(theme, shape), Is.Not.Null);
            Assert.That(styles.ResolveShapeLine(theme, shape), Is.Not.Null);
            Assert.That(styles.ResolveTextFont(theme, "major"), Is.Not.Empty);
            Assert.That(styles.ResolveTextColor(theme), Is.Not.Null);
        });

        string directory = Path.Combine(Path.GetTempPath(), "pptx-assets-" + Guid.NewGuid().ToString("N"));
        try
        {
            IPptxAssetExtractor assets = new PptxAssetExtractor();
            Assert.That(assets.ExtractPresentationImages(presentation, Path.Combine(directory, "presentation-images")).Assets, Is.Empty);
            Assert.That(assets.ExtractSlideImages(presentation, slide, Path.Combine(directory, "slide-images")).Assets, Is.Empty);
            Assert.That(assets.ExtractPresentationMedia(presentation, Path.Combine(directory, "presentation-media")).Assets, Is.Empty);
            Assert.That(assets.ExtractSlideMedia(presentation, slide, Path.Combine(directory, "slide-media")).Assets, Is.Empty);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Test]
    public void CollectionTransformMergeAndSplitFacades_CompletePositiveWorkflows()
    {
        PptxDocument document = new PptxDocumentFactory().CreatePresentation();
        PptxPresentation presentation = document.LoadPresentation();
        PptxSlide slide = presentation.Slides[0];

        IPptxTransformOperations transforms = new PptxTransformOperations();
        PptxTransform transform = transforms.CreateDefault();
        transforms.SetBounds(transform, new PptxBounds(10L, 20L, 300L, 400L));
        transforms.SetRotationDegrees(transform, 45.0);
        transforms.SetChildBounds(transform, new PptxBounds(0L, 0L, 100L, 100L));
        Assert.Multiple(() =>
        {
            Assert.That(transforms.Bounds(transform).Width, Is.EqualTo(300L));
            Assert.That(transforms.AnchorBounds(transform).Height, Is.GreaterThan(400L));
            Assert.That(transforms.RotationDegrees(transform), Is.EqualTo(45.0));
            Assert.That(transforms.HasChildBounds(transform), Is.True);
            Assert.That(transforms.ChildBounds(transform).Width, Is.EqualTo(100L));
            Assert.That(transforms.MapChildBounds(transform, new PptxBounds(0L, 0L, 50L, 50L)).Width, Is.EqualTo(150L));
            Assert.That(transforms.MapChildPoint(transform, new PptxPoint(50L, 50L)).X, Is.EqualTo(160L));
            Assert.That(transforms.MapChildAnchorBounds(transform, new PptxBounds(0L, 0L, 50L, 50L)).Width, Is.GreaterThan(0L));
            Assert.That(transforms.ScaleCoordinate(10L, 3L, 2L), Is.EqualTo(15L));
            Assert.That(transforms.ScaleCoordinate(10L, 1L, 0L), Is.EqualTo(10L));
        });

        PptxShape groupShape = slide.GetShapeTree().AddGroup(new PptxBounds(0L, 0L, 500L, 500L), new PptxBounds(0L, 0L, 100L, 100L));
        PptxGroupShape group = groupShape.AsGroup();
        IPptxGroupShapeOperations groups = new PptxGroupShapeOperations();
        PptxShape first = groups.AddAutoShape(group, PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 25L, 25L));
        first.SetName("first");
        PptxShape nested = groups.AddGroup(group, new PptxBounds(25L, 25L, 25L, 25L), new PptxBounds(0L, 0L, 10L, 10L));
        PptxShape clone = groups.CloneShape(group, first);
        groups.Move(group, 2, 0);
        groups.BringToFront(group, nested);
        groups.SendToBack(group, nested);
        Assert.Multiple(() =>
        {
            Assert.That(groups.Count(group), Is.EqualTo(3));
            Assert.That(groups.Get(group, 0), Is.SameAs(nested));
            Assert.That(groups.FindById(group, first.Id), Is.SameAs(first));
            Assert.That(groups.FindByName(group, "first"), Is.SameAs(first));
            Assert.That(groups.MapChildBounds(group, first).Width, Is.GreaterThan(0L));
            Assert.That(groups.MapChildPoint(group, new PptxPoint(10L, 10L)).X, Is.GreaterThanOrEqualTo(0L));
            Assert.That(groups.MapChildAnchorBounds(group, first).Width, Is.GreaterThan(0L));
            Assert.That(clone.Id, Is.Not.EqualTo(first.Id));
        });
        groups.Clone(group, 0);
        groups.RemoveAt(group, groups.Count(group) - 1);

        IPptxSlideCollectionOperations slides = new PptxSlideCollectionOperations();
        PptxSlideCollection collection = presentation.SlideCollection;
        PptxSlide added = slides.Add(collection);
        PptxSlide inserted = slides.Insert(collection, 1);
        slides.Clone(collection, 0);
        slides.CloneToIndex(collection, 0, 1);
        slides.Move(collection, 0, slides.Count(collection) - 1);
        Assert.That(slides.FindById(collection, added.SlideId), Is.SameAs(added));
        Assert.That(slides.Get(collection, 0), Is.Not.Null);
        Assert.That(slides.RemoveById(collection, inserted.SlideId), Is.True);
        slides.RemoveAt(collection, slides.Count(collection) - 1);

        PptxPresentation sourcePresentation = new PptxDocumentFactory().CreatePresentation().LoadPresentation();
        slides.Import(collection, sourcePresentation, 0);
        slides.ImportToIndex(collection, sourcePresentation, 0, 0);
        if (presentation.SlideLayouts.Count > 0)
        {
            slides.InsertWithLayout(collection, 0, presentation.SlideLayouts[0]);
        }

        IPptxMerger merger = new PptxMerger();
        PptxDocument firstDocument = new PptxDocumentFactory().CreatePresentation();
        PptxDocument secondDocument = new PptxDocumentFactory().CreatePresentation();
        Assert.That(merger.MergePresentations(firstDocument.LoadPresentation(), secondDocument.LoadPresentation()), Is.EqualTo(1));
        Assert.That(merger.MergeDocumentIntoPresentation(firstDocument.LoadPresentation(), secondDocument), Is.EqualTo(1));
        Assert.That(merger.MergeSourceIntoPresentation(firstDocument.LoadPresentation(), secondDocument), Is.EqualTo(1));
        Assert.That(merger.MergeDocuments(firstDocument, secondDocument).LoadPresentation().Slides.Count, Is.GreaterThan(1));
        Assert.That(merger.MergeDocumentWithSource(firstDocument, secondDocument).LoadPresentation().Slides.Count, Is.GreaterThan(1));

        IPptxSplitter splitter = new PptxSplitter();
        Assert.That(splitter.Split(firstDocument.LoadPresentation()), Is.Not.Empty);
        Assert.That(splitter.SplitDocument(firstDocument), Is.Not.Empty);
        Assert.That(splitter.SplitSource(firstDocument), Is.Not.Empty);
        Assert.That(splitter.SplitRange(firstDocument.LoadPresentation(), 0, 1).LoadPresentation().Slides.Count, Is.EqualTo(1));
        Assert.That(splitter.SplitDocumentRange(firstDocument, 0, 1).LoadPresentation().Slides.Count, Is.EqualTo(1));
        Assert.That(splitter.SplitSourceRange(firstDocument, 0, 1).LoadPresentation().Slides.Count, Is.EqualTo(1));

        IPptxComparisonOperations comparisons = new PptxComparisonOperations();
        Assert.That(comparisons.CompareDocuments(firstDocument, firstDocument.Clone()).Equal, Is.True);
        Assert.That(comparisons.CompareSources(secondDocument, secondDocument.Clone()).Equal, Is.True);
    }

    [Test]
    public void EveryPublicOperationMethod_IsReachableWithConsumerArguments()
    {
        Type[] operationTypes = typeof(PptxDocument).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == "pptx" && type.IsClass && !type.IsAbstract && type.Name.EndsWith("Operations", StringComparison.Ordinal))
            .ToArray();
        int methodCount = 0;
        NullabilityInfoContext nullability = new NullabilityInfoContext();

        foreach (Type operationType in operationTypes)
        {
            object operations = Activator.CreateInstance(operationType)!;
            MethodInfo[] methods = operationType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (MethodInfo method in methods)
            {
                if (method.GetParameters().Any(parameter => parameter.ParameterType.IsByRefLike))
                {
                    continue;
                }

                OperationFixture fixture = new OperationFixture();
                object?[] arguments = method.GetParameters().Select(parameter => fixture.CreateArgument(parameter.ParameterType, parameter.Name)).ToArray();
                try
                {
                    object? value = method.Invoke(operations, arguments);
                    if (method.ReturnType != typeof(void) && nullability.Create(method.ReturnParameter).ReadState == NullabilityState.NotNull)
                    {
                        Assert.That(value, Is.Not.Null, $"{operationType.Name}.{method.Name} returned null for a non-nullable public result.");
                    }
                }
                catch (TargetInvocationException exception)
                {
                    Exception failure = exception.InnerException!;
                    Assert.That(IsExpectedPublicFailure(failure), Is.True, $"{operationType.Name}.{method.Name} failed unexpectedly with {failure.GetType().Name}: {failure.Message}");
                }

                methodCount++;
            }
        }

        Assert.That(operationTypes.Length, Is.GreaterThan(60));
        Assert.That(methodCount, Is.GreaterThan(500));
    }

    private static bool IsExpectedPublicFailure(Exception failure)
    {
        return failure is ArgumentException
            || failure is InvalidOperationException
            || failure is IOException
            || failure is NotSupportedException
            || failure is UnauthorizedAccessException
            || failure is NullReferenceException
            || failure is System.Xml.XmlException
            || failure.GetType().Namespace == "pptx";
    }

    private sealed class OperationFixture
    {
        private readonly PptxDocument document;
        private readonly PptxPresentation presentation;
        private readonly PptxSlide slide;
        private readonly PptxShape shape;
        private readonly PptxTable table;
        private readonly PptxChart chart;

        public OperationFixture()
        {
            document = new PptxDocumentFactory().CreatePresentation();
            presentation = document.LoadPresentation();
            slide = presentation.Slides[0];
            shape = slide.AddTextBox("sample", new PptxBounds(10L, 20L, 300L, 200L));
            table = slide.AddTable(2, 2, new PptxBounds(10L, 250L, 300L, 200L));
            PptxChartData data = new PptxChartData();
            data.Categories.Add("A");
            PptxChartSeries series = new PptxChartSeries();
            series.Name = "Series";
            series.Values.Add("1");
            data.Series.Add(series);
            chart = slide.AddChart(PptxChartType.Column, data, new PptxBounds(350L, 250L, 300L, 200L));
        }

        public object? CreateArgument(Type type, string? name)
        {
            if (type == typeof(PptxDocument)) return document;
            if (type == typeof(IPptxPresentationSource)) return document;
            if (type == typeof(PptxPresentation)) return presentation;
            if (type == typeof(PptxSlide)) return slide;
            if (type == typeof(PptxShape)) return shape;
            if (type == typeof(PptxShapeTree)) return slide.GetShapeTree();
            if (type == typeof(PptxTable)) return table;
            if (type == typeof(PptxTableRow)) return table.Rows[0];
            if (type == typeof(PptxTableCell)) return table.Rows[0].Cells[0];
            if (type == typeof(PptxChart)) return chart;
            if (type == typeof(PptxChartData)) return PptxChartData.FromChart(chart);
            if (type == typeof(PptxChartSeries)) return PptxChartData.FromChart(chart).Series[0];
            if (type == typeof(PptxPackage)) return document.GetPackage();
            if (type == typeof(PptxPart)) return document.GetPackage().Parts[0];
            if (type == typeof(PptxBounds)) return new PptxBounds(10L, 20L, 100L, 100L);
            if (type == typeof(PptxPoint)) return new PptxPoint(10L, 20L);
            if (type == typeof(PptxMargins)) return new PptxMargins(1L, 1L, 1L, 1L);
            if (type == typeof(PptxRelationshipCollection)) return new PptxRelationshipCollection("/ppt/presentation.xml");
            if (type == typeof(PptxRelationship)) return new PptxRelationship("/ppt/presentation.xml", "rId1", "urn:pptx:test", "slides/slide1.xml", PptxRelationshipTargetMode.Internal, "/ppt/slides/slide1.xml", "");
            if (type == typeof(Stream)) return new MemoryStream(document.SaveBytes());
            if (type == typeof(byte[])) return document.SaveBytes();
            if (type == typeof(string)) return SampleString(name);
            if (type == typeof(int)) return SampleInteger(name);
            if (type == typeof(long)) return 1L;
            if (type == typeof(double)) return 1.0;
            if (type == typeof(bool)) return true;
            if (type.IsEnum) return Enum.GetValues(type).GetValue(0);
            if (type.IsInterface) return null;
            if (type.IsGenericType) return Activator.CreateInstance(type);

            ConstructorInfo? constructor = type.GetConstructor(Type.EmptyTypes);
            if (constructor != null) return constructor.Invoke(Array.Empty<object>());
            return null;
        }

        private static int SampleInteger(string? name)
        {
            if (name == "index" || name == "slideIndex" || name == "rowIndex" || name == "columnIndex" || name == "tableIndex" || name == "oldIndex" || name == "newIndex" || name == "fromIndex" || name == "toIndex") return 0;
            if (name == "rowCount" || name == "columnCount" || name == "count") return 1;
            return 1;
        }

        private static string SampleString(string? name)
        {
            return name switch
            {
                "partName" => "/ppt/presentation.xml",
                "sourcePartName" => "/ppt/presentation.xml",
                "target" => "slides/slide1.xml",
                "targetUri" => "slides/slide1.xml",
                "contentType" => "application/xml",
                "extension" => "xml",
                "chartType" => PptxChartType.Column,
                "shapeType" => PptxShapeType.Rectangle,
                "presetGeometry" => PptxShapeType.Rectangle,
                "connectorType" => PptxConnector.StraightPreset,
                "color" => "FF0000",
                "rgb" => "FF0000",
                "directoryPath" => "",
                "path" => "",
                _ => "value"
            };
        }
    }
}
