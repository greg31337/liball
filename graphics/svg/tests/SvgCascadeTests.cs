namespace svg.Tests;

public class SvgCascadeTests
{
    [Test]
    public void OriginsImportanceSpecificityAndElementSourcesUseCssPrecedence()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect id='target' fill='red' style='fill:blue; stroke:blue !important'/></svg>");
        SvgNode rect = document.DocumentElement.Children[0];
        SvgStyleSheet userAgent = Sheet("rect { color:black !important; stroke:black !important; }", SvgStyleOrigin.UserAgent);
        SvgStyleSheet user = Sheet("rect { color:green !important; stroke:green !important; }", SvgStyleOrigin.User);
        SvgStyleSheet author = Sheet("rect { fill:yellow; color:blue !important; } #target { fill:green; }", SvgStyleOrigin.Author);

        SvgCascadeResult result = new SvgCascade().ResolveWithNodeAndStyleSheets(rect, new List<SvgStyleSheet> { userAgent, user, author });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Find("fill").ValueText, Is.EqualTo("blue"));
            Assert.That(result.Find("fill").Winner.SourceKind, Is.EqualTo(SvgCascadeSourceKind.StyleAttribute));
            Assert.That(result.Find("color").ValueText, Is.EqualTo("black"));
            Assert.That(result.Find("color").Winner.Origin, Is.EqualTo(SvgStyleOrigin.UserAgent));
            Assert.That(result.Find("stroke").ValueText, Is.EqualTo("black"));
            Assert.That(result.Candidates.Any(candidate => candidate.SourceKind == SvgCascadeSourceKind.PresentationAttribute), Is.True);
        });
    }

    [Test]
    public void PresentationAttributesHaveZeroSpecificityAndFollowAuthorSheets()
    {
        SvgNode rect = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect fill='red' stroke='red'/></svg>").DocumentElement.Children[0];
        SvgStyleSheet first = Sheet("* { fill:green; } rect { stroke:green; }", SvgStyleOrigin.Author);
        SvgStyleSheet second = Sheet("* { fill:blue; }", SvgStyleOrigin.Author);

        SvgCascadeResult result = new SvgCascade().ResolveWithNodeAndStyleSheets(rect, new List<SvgStyleSheet> { first, second });

        Assert.Multiple(() =>
        {
            Assert.That(result.Find("fill").ValueText, Is.EqualTo("red"));
            Assert.That(result.Find("fill").Winner.TypeSpecificity, Is.Zero);
            Assert.That(result.Find("fill").Winner.SourceKind, Is.EqualTo(SvgCascadeSourceKind.PresentationAttribute));
            Assert.That(result.Find("stroke").ValueText, Is.EqualTo("green"));
            Assert.That(result.Find("stroke").Winner.TypeSpecificity, Is.EqualTo(1));
        });
    }

    [Test]
    public void LayersReverseForImportantDeclarationsAndRevertLayerRollsBackOneLayer()
    {
        string css = "@layer base, theme;" +
            "@layer base { rect { fill:red; stroke:red !important; color:red; visibility:hidden; } }" +
            "@layer theme { rect { fill:blue; stroke:blue !important; color:revert-layer; visibility:collapse; } }" +
            "rect { fill:green; stroke:green !important; visibility:revert-layer; }";
        SvgNode rect = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect/></svg>").DocumentElement.Children[0];

        SvgCascadeResult result = new SvgCascade().ResolveWithNodeAndStyleSheets(rect, new List<SvgStyleSheet> { Sheet(css, SvgStyleOrigin.Author) });

        Assert.Multiple(() =>
        {
            Assert.That(result.Find("fill").ValueText, Is.EqualTo("green"));
            Assert.That(result.Find("stroke").ValueText, Is.EqualTo("red"));
            Assert.That(result.Find("stroke").Winner.LayerName, Is.EqualTo("base"));
            Assert.That(result.Find("color").ValueText, Is.EqualTo("red"));
            Assert.That(result.Find("visibility").ValueText, Is.EqualTo("collapse"));
        });
    }

    [Test]
    public void RevertFallsBackToTheNextOriginAndNamedLayersMergeAcrossSheets()
    {
        SvgNode rect = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect/></svg>").DocumentElement.Children[0];
        SvgStyleSheet userAgent = Sheet("rect { fill:red; }", SvgStyleOrigin.UserAgent);
        SvgStyleSheet user = Sheet("rect { fill:blue; }", SvgStyleOrigin.User);
        SvgStyleSheet authorBase = Sheet("@layer base { rect { color:red; } }", SvgStyleOrigin.Author);
        SvgStyleSheet authorTheme = Sheet("@layer theme { rect { color:blue; } } @layer base { rect { color:green; } } rect { fill:revert; }", SvgStyleOrigin.Author);

        SvgCascadeResult result = new SvgCascade().ResolveWithNodeAndStyleSheets(rect, new List<SvgStyleSheet> { userAgent, user, authorBase, authorTheme });

        Assert.Multiple(() =>
        {
            Assert.That(result.Find("fill").ValueText, Is.EqualTo("blue"));
            Assert.That(result.Find("fill").Winner.Origin, Is.EqualTo(SvgStyleOrigin.User));
            Assert.That(result.Find("color").ValueText, Is.EqualTo("blue"));
            Assert.That(result.Find("color").Winner.LayerName, Is.EqualTo("theme"));
        });
    }

    [Test]
    public void InitialInheritAndUnsetUsePropertyMetadataAndParentCascade()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><g><rect/></g></svg>");
        SvgNode group = document.DocumentElement.Children[0];
        SvgNode rect = group.Children[0];
        SvgStyleSheet sheet = Sheet("g { color:purple; fill:orange; opacity:.4; } rect { color:unset; fill:inherit; opacity:unset; stroke:initial; }", SvgStyleOrigin.Author);
        SvgCascadeResult parent = new SvgCascade().ResolveWithNodeAndStyleSheets(group, new List<SvgStyleSheet> { sheet });
        SvgCascadeOptions options = new SvgCascadeOptions { Parent = parent };

        SvgCascadeResult child = new SvgCascade().ResolveWithNodeAndStyleSheetsAndOptions(rect, new List<SvgStyleSheet> { sheet }, options);

        Assert.Multiple(() =>
        {
            Assert.That(child.Find("color").ValueText, Is.EqualTo("purple"));
            Assert.That(child.Find("color").Kind, Is.EqualTo(SvgCascadedValueKind.Inherited));
            Assert.That(child.Find("fill").ValueText, Is.EqualTo("orange"));
            Assert.That(child.Find("opacity").ValueText, Is.EqualTo("1"));
            Assert.That(child.Find("opacity").Kind, Is.EqualTo(SvgCascadedValueKind.Initial));
            Assert.That(child.Find("stroke").ValueText, Is.EqualTo("none"));
        });
    }

    [Test]
    public void InvalidDeclarationsAreIgnoredAndValidShorthandsCascadeAsLonghands()
    {
        SvgNode rect = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect style='fill:blue; fill:not-a-real-paint; marker:url(#tip)'/></svg>").DocumentElement.Children[0];
        SvgStyleSheet sheet = Sheet("rect { fill:red; overflow:hidden auto visible; }", SvgStyleOrigin.Author);

        SvgCascadeResult result = new SvgCascade().ResolveWithNodeAndStyleSheets(rect, new List<SvgStyleSheet> { sheet });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Find("fill").ValueText, Is.EqualTo("blue"));
            Assert.That(result.Find("marker-start").ValueText, Is.EqualTo("url(#tip)"));
            Assert.That(result.Find("marker-mid").ValueText, Is.EqualTo("url(#tip)"));
            Assert.That(result.Find("marker-end").ValueText, Is.EqualTo("url(#tip)"));
            Assert.That(result.Diagnostics.Any(diagnostic => diagnostic.Code == SvgDiagnosticCode.InvalidCss), Is.True);
        });
    }

    [Test]
    public void CollectedSheetMediaAndCallerOriginsParticipateInCascade()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><style media='print'>rect { fill:red; }</style><style media='screen'>rect { fill:blue; }</style><rect/></svg>");
        SvgStyleSheet caller = Sheet("rect { fill:green !important; }", SvgStyleOrigin.User);
        SvgStyleSheetCollection collection = new SvgStyleSheetCollector().CollectWithDocumentAndCallerStyleSheetsAndLimits(document, new List<SvgStyleSheet> { caller }, new SvgCssLimits());
        SvgNode rect = document.DocumentElement.Children[2];

        SvgCascadeResult result = new SvgCascade().ResolveWithNodeAndCollectionAndOptions(rect, collection, new SvgCascadeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Find("fill").ValueText, Is.EqualTo("green"));
            Assert.That(result.Find("fill").Winner.Origin, Is.EqualTo(SvgStyleOrigin.User));
            Assert.That(result.Candidates.Any(candidate => candidate.ValueText == "red"), Is.False);
            Assert.That(result.Candidates.Any(candidate => candidate.ValueText == "blue"), Is.True);
        });
    }

    static SvgStyleSheet Sheet(string css, SvgStyleOrigin origin)
    {
        return new SvgCssParser().ParseStyleSheetWithValueAndLimitsAndOriginAndSource(css, new SvgCssLimits(), origin, new SvgSourceUri(origin.ToString() + ".css")).StyleSheet;
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True);
        return result.Document;
    }
}
