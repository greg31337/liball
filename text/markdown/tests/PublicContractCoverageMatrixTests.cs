using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class PublicContractCoverageMatrixTests
{
    [Test]
    public void EveryNodeKindParticipatesInPublicTreeSearchTraversalAndRendering()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices services = markdown.GetDataServices();
        MarkdownPipeline pipeline = PublicRendererMatrixTests.CreateMatrixPipeline(markdown);
        MarkdownDocument document = markdown.ParseSourceWithPipeline(
            PublicRendererMatrixTests.CreateMatrixSource(),
            pipeline);
        MarkdownNode paragraph = services.CreateTreeOperations()
            .FindFirstInDocument(document, MarkdownNodeKind.Paragraph)!;
        MarkdownNode unknown = new MarkdownNode();
        MarkdownNode attribute = new MarkdownNode();
        MarkdownNode extensionBlock = new MarkdownExtensionNode().Node;
        MarkdownNode extensionInline = new MarkdownExtensionNode().Node;
        attribute.Kind = MarkdownNodeKind.Attribute;
        attribute.Data.Kind = MarkdownNodeKind.Attribute;
        attribute.Data.Attribute = new MarkdownAttributeNodeData();
        attribute.Data.Attribute.Name = "data-contract";
        attribute.Data.Attribute.Value = "covered";
        extensionBlock.Literal = "block";
        extensionInline.Kind = MarkdownNodeKind.ExtensionInline;
        extensionInline.Data.Kind = MarkdownNodeKind.ExtensionInline;
        extensionInline.Data.Extension!.IsBlock = false;
        extensionInline.Literal = "inline";
        services.CreateExtensionNode("contract-block", extensionBlock);
        services.CreateExtensionNode("contract-inline", extensionInline);
        services.CreateNodeOperations(paragraph).AppendChild(attribute);
        services.CreateNodeOperations(paragraph).AppendChild(extensionInline);
        services.CreateNodeOperations(document.Root).AppendChild(extensionBlock);
        services.CreateNodeOperations(document.Root).AppendChild(unknown);
        HashSet<MarkdownNodeKind> visited = new HashSet<MarkdownNodeKind>();

        MarkdownTreeWalkResult result = services.CreateTreeOperations().WalkDocument(
            document,
            new MarkdownNodeVisitorAdapter(
                context =>
                {
                    visited.Add(context.Node.Kind);
                    return MarkdownTreeWalkResult.Continue;
                },
                null));

        MarkdownNodeKind[] expected = EveryNodeKind();
        Assert.That(result, Is.EqualTo(MarkdownTreeWalkResult.Continue));
        Assert.That(visited.Count, Is.EqualTo(expected.Length));
        foreach (MarkdownNodeKind kind in expected)
        {
            Assert.That(visited, Does.Contain(kind), kind.ToString());
            Assert.That(
                services.CreateTreeOperations().FindFirstInDocument(document, kind),
                Is.Not.Null,
                kind.ToString());
        }

        Assert.That(markdown.RenderHtmlDocumentWithPipeline(document, pipeline), Is.Not.Null);
        Assert.That(markdown.RenderMarkdownDocumentWithPipeline(document, pipeline), Is.Not.Null);
        Assert.That(markdown.RenderPlainTextDocumentWithPipeline(document, pipeline), Is.Not.Null);
        Assert.That(markdown.RenderDebugTreeDocumentWithPipeline(document, pipeline), Is.Not.Null);
        Assert.That(markdown.RenderXmlAstDocumentWithPipeline(document, pipeline), Is.Not.Null);
    }

    [Test]
    public void EveryFeatureFlagAndPresetControlsObservableParsingOrRenderingBehavior()
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipeline commonMark = markdown.CreateCommonMarkPipeline();
        MarkdownPipeline gfm = markdown.CreateGfmPipeline();
        MarkdownPipeline advanced = markdown.CreateAdvancedPipeline();
        MarkdownPipeline zero = markdown.CreatePipelineBuilder().UseZero().Build();
        int featureCount = 0;

        foreach (ExtensionFeatureCase feature in ExtensionFeatureMatrixTests.ExtensionCases())
        {
            MarkdownPipeline enabled = ExtensionFeatureMatrixTests.CreatePipeline(
                markdown,
                feature.FeatureName,
                true);
            MarkdownPipeline disabled = ExtensionFeatureMatrixTests.CreatePipeline(
                markdown,
                feature.FeatureName,
                false);
            MarkdownDocument enabledDocument = markdown.ParseSourceWithPipeline(feature.Source, enabled);
            MarkdownDocument disabledDocument = markdown.ParseSourceWithPipeline(feature.Source, disabled);

            Assert.That(
                ExtensionFeatureMatrixTests.FindFirstKind(enabledDocument.Root, feature.ExpectedKind),
                Is.Not.Null,
                feature.FeatureName);
            Assert.That(
                markdown.RenderHtmlDocumentWithPipeline(enabledDocument, enabled),
                Is.Not.EqualTo(markdown.RenderHtmlDocumentWithPipeline(disabledDocument, disabled)),
                feature.FeatureName);
            featureCount = featureCount + 1;
        }

        Assert.That(featureCount, Is.EqualTo(25));
        Assert.That(commonMark.UsesCommonMark(), Is.True);
        Assert.That(commonMark.UsesGfm(), Is.False);
        Assert.That(gfm.UsesGfm(), Is.True);
        Assert.That(gfm.UsesAdvancedExtensions(), Is.False);
        Assert.That(advanced.UsesAdvancedExtensions(), Is.True);
        AssertEveryAdvancedFeature(advanced.GetFeatures());
        AssertEveryFeatureDisabled(zero.GetFeatures());
        Assert.That(markdown.ParseSourceWithPipeline("text", zero).Root.Children, Is.Empty);

        Assert.That(
            markdown.RenderHtmlSourceWithPipeline("a\nb", commonMark),
            Is.EqualTo("<p>a\nb</p>\n"));
        Assert.That(
            markdown.RenderHtmlSourceWithPipeline(
                "a\nb",
                markdown.CreatePipelineBuilder().SetSoftBreakMode(MarkdownSoftBreakMode.Space).Build()),
            Is.EqualTo("<p>a b</p>\n"));
        Assert.That(
            markdown.RenderHtmlSourceWithPipeline(
                "a\nb",
                markdown.CreatePipelineBuilder().SetSoftBreakMode(MarkdownSoftBreakMode.HardBreak).Build()),
            Is.EqualTo("<p>a<br>\nb</p>\n"));
        Assert.That(
            markdown.RenderHtmlSourceWithPipeline(
                "漢字\n仮名",
                markdown.CreatePipelineBuilder().UseCjkSoftBreaks().Build()),
            Is.EqualTo("<p>漢字仮名</p>\n"));
    }

    [Test]
    public void EveryRenderModeHasEquivalentSourceDocumentAndStreamingOperations()
    {
        IMarkdown markdown = new Markdown();
        const string source = "# Title\n\ntext *value*";
        MarkdownDocument document = markdown.ParseSource(source);
        StringWriter html = new StringWriter();
        StringWriter normalized = new StringWriter();
        StringWriter plainText = new StringWriter();
        StringWriter debugTree = new StringWriter();
        StringWriter xml = new StringWriter();

        markdown.WriteHtmlDocument(document, html);
        markdown.WriteMarkdownDocument(document, normalized);
        markdown.WritePlainTextDocument(document, plainText);
        markdown.WriteDebugTreeDocument(document, debugTree);
        markdown.WriteXmlAstSource(source, xml);

        Assert.That(html.ToString(), Is.EqualTo(markdown.RenderHtmlSource(source)));
        Assert.That(html.ToString(), Is.EqualTo(markdown.RenderHtmlDocument(document)));
        Assert.That(normalized.ToString(), Is.EqualTo(markdown.RenderMarkdownSource(source)));
        Assert.That(normalized.ToString(), Is.EqualTo(markdown.RenderMarkdownDocument(document)));
        Assert.That(plainText.ToString(), Is.EqualTo(markdown.RenderPlainTextSource(source)));
        Assert.That(plainText.ToString(), Is.EqualTo(markdown.RenderPlainTextDocument(document)));
        Assert.That(debugTree.ToString(), Is.EqualTo(markdown.RenderDebugTreeSource(source)));
        Assert.That(debugTree.ToString(), Is.EqualTo(markdown.RenderDebugTreeDocument(document)));
        Assert.That(xml.ToString(), Is.EqualTo(markdown.RenderXmlAstSource(source)));
        Assert.That(xml.ToString(), Is.EqualTo(markdown.RenderXmlAstDocument(document)));
    }

    [Test]
    public void EveryEventKindAndDiagnosticSeverityHasPublicOperationalBehavior()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownEventReader reader = markdown.CreateEventReaderFromSource("text");
        bool sawEnter = false;
        bool sawLeave = false;

        Assert.That(reader.GetCurrent().EventKind, Is.EqualTo(MarkdownEventKind.None));
        while (reader.Read())
        {
            if (reader.GetCurrent().EventKind == MarkdownEventKind.EnterNode)
            {
                sawEnter = true;
            }
            else if (reader.GetCurrent().EventKind == MarkdownEventKind.LeaveNode)
            {
                sawLeave = true;
            }
        }

        Assert.That(reader.GetCurrent().EventKind, Is.EqualTo(MarkdownEventKind.None));
        Assert.That(sawEnter, Is.True);
        Assert.That(sawLeave, Is.True);
        IMarkdownEventWriter eventWriter = markdown.CreateEventWriter();
        eventWriter.Write(new MarkdownEvent());
        Assert.That(eventWriter.GetOpenNodeCount(), Is.EqualTo(0));

        MarkdownDocument document = markdown.ParseSource("text");
        IMarkdownDocumentOperations operations = markdown.GetDataServices().CreateDocumentOperations(document);
        MarkdownSourceSpan span = document.Root.Span;
        operations.AddDiagnosticValues(MarkdownDiagnosticSeverity.Info, "INFO", "info", span);
        operations.AddDiagnosticValues(MarkdownDiagnosticSeverity.Warning, "WARN", "warning", span);
        operations.AddDiagnosticValues(MarkdownDiagnosticSeverity.Error, "ERROR", "error", span);

        Assert.That(operations.GetDiagnosticCount(), Is.EqualTo(3));
        Assert.That(operations.GetDiagnostics()[0].Severity, Is.EqualTo(MarkdownDiagnosticSeverity.Info));
        Assert.That(operations.GetDiagnostics()[1].Severity, Is.EqualTo(MarkdownDiagnosticSeverity.Warning));
        Assert.That(operations.GetDiagnostics()[2].Severity, Is.EqualTo(MarkdownDiagnosticSeverity.Error));
        Assert.That(operations.HasErrors(), Is.True);
        Assert.That(
            () => operations.AddDiagnosticValues((MarkdownDiagnosticSeverity)99, "BAD", "bad", span),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        MarkdownDiagnostic invalid = new MarkdownDiagnostic();
        invalid.Severity = (MarkdownDiagnosticSeverity)99;
        Assert.That(() => operations.AddDiagnostic(invalid), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void EveryUriKindAndRawHtmlDecisionIsReachableThroughPublicSafetyOperations()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices services = markdown.GetDataServices();
        IMarkdownSafetyOperations safety = services.CreateSafetyOperations();
        string[] destinations = new string[]
        {
            "", "#fragment", "docs/page", "https://example.test", "mailto:user@example.test",
            "data:image/png;base64,AA==", "javascript:alert(1)"
        };
        MarkdownUriKind[] kinds = new MarkdownUriKind[]
        {
            MarkdownUriKind.Empty, MarkdownUriKind.Fragment, MarkdownUriKind.Relative,
            MarkdownUriKind.Absolute, MarkdownUriKind.Mailto, MarkdownUriKind.Data,
            MarkdownUriKind.Unsafe
        };
        int index = 0;
        while (index < destinations.Length)
        {
            Assert.That(services.ClassifyUri(destinations[index]).Kind, Is.EqualTo(kinds[index]));
            index = index + 1;
        }

        MarkdownPipeline escape = markdown.CreateCommonMarkPipeline();
        MarkdownPipeline render = markdown.CreatePipelineBuilder().AllowUnsafeHtml().Build();
        MarkdownPipeline filter = markdown.CreatePipelineBuilder().UseGfm().AllowRawHtml().Build();
        MarkdownPipeline suppress = markdown.CreatePipelineBuilder()
            .SetRawHtmlPolicy(new ContractRawHtmlPolicy(MarkdownRawHtmlDecision.Suppress))
            .Build();
        Assert.That(safety.EvaluateRawHtml("<script>", escape).Decision, Is.EqualTo(MarkdownRawHtmlDecision.Escape));
        Assert.That(safety.EvaluateRawHtml("<script>", render).Decision, Is.EqualTo(MarkdownRawHtmlDecision.Render));
        Assert.That(safety.EvaluateRawHtml("<script>", filter).Decision, Is.EqualTo(MarkdownRawHtmlDecision.Filter));
        Assert.That(safety.EvaluateRawHtml("<script>", suppress).Decision, Is.EqualTo(MarkdownRawHtmlDecision.Suppress));
    }

    [Test]
    public void EveryTreeWalkResultControlsTraversalAndInvalidResultsAreRejected()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownTreeOperations trees = markdown.GetDataServices().CreateTreeOperations();
        MarkdownDocument document = markdown.ParseSource("first *child*\n\nsecond");
        int continued = 0;
        int skipped = 0;
        int stopped = 0;

        MarkdownTreeWalkResult continueResult = trees.WalkDocument(
            document,
            new MarkdownNodeVisitorAdapter(
                context =>
                {
                    continued = continued + 1;
                    return MarkdownTreeWalkResult.Continue;
                },
                null));
        MarkdownTreeWalkResult skipResult = trees.WalkDocument(
            document,
            new MarkdownNodeVisitorAdapter(
                context =>
                {
                    skipped = skipped + 1;
                    if (context.Node.Kind == MarkdownNodeKind.Paragraph)
                    {
                        return MarkdownTreeWalkResult.SkipChildren;
                    }

                    return MarkdownTreeWalkResult.Continue;
                },
                null));
        MarkdownTreeWalkResult stopResult = trees.WalkDocument(
            document,
            new MarkdownNodeVisitorAdapter(
                context =>
                {
                    stopped = stopped + 1;
                    return MarkdownTreeWalkResult.Stop;
                },
                null));

        Assert.That(continueResult, Is.EqualTo(MarkdownTreeWalkResult.Continue));
        Assert.That(skipResult, Is.EqualTo(MarkdownTreeWalkResult.Continue));
        Assert.That(stopResult, Is.EqualTo(MarkdownTreeWalkResult.Stop));
        Assert.That(continued, Is.GreaterThan(skipped));
        Assert.That(stopped, Is.EqualTo(1));
        Assert.That(
            () => trees.WalkDocument(
                document,
                new MarkdownNodeVisitorAdapter(
                    context => (MarkdownTreeWalkResult)99,
                    null)),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void PreviouslyUncalledPublicMembersHaveDirectBehavioralCoverage()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices services = markdown.GetDataServices();
        List<int> lineIndexes = new List<int>();
        MarkdownPipeline? blockContextPipeline = null;
        IMarkdownBlockRule blockRule = new TestBlockRule(
            "coverage.block",
            context =>
            {
                lineIndexes.Add(context.GetLineIndex());
                blockContextPipeline = context.GetPipeline();
                context.AdvanceLine();
                return true;
            });
        MarkdownPipeline blockPipeline = markdown.CreatePipelineBuilder()
            .ClearBlockRules()
            .AddBlockRule(blockRule)
            .Build();
        markdown.ParseSourceWithPipeline("one\ntwo", blockPipeline);

        int inlinePosition = -1;
        MarkdownPipeline? inlineContextPipeline = null;
        IMarkdownInlineRule inlineRule = new TestInlineRule(
            "coverage.inline",
            "@",
            context =>
            {
                if (context.GetCurrentCharacter() != '@')
                {
                    return false;
                }

                inlinePosition = context.GetPosition();
                inlineContextPipeline = context.GetPipeline();
                MarkdownNode replacement = new MarkdownNode();
                replacement.Kind = MarkdownNodeKind.Text;
                replacement.Literal = "AT";
                context.AppendInline(replacement);
                context.Advance();
                return true;
            });
        MarkdownPipeline inlinePipeline = markdown.CreatePipelineBuilder()
            .AddInlineRule(inlineRule)
            .Build();
        MarkdownDocument inlineDocument = markdown.ParseSourceWithPipeline("a@b", inlinePipeline);

        MarkdownRuleRegistration registration = services.CreateRuleRegistration("coverage.rule", blockRule);
        Assert.That(registration.Name, Is.EqualTo("coverage.rule"));
        Assert.That(registration.Rule, Is.SameAs(blockRule));
        Assert.That(lineIndexes, Is.EqualTo(new int[] { 0, 1 }));
        Assert.That(blockContextPipeline!.UsesCommonMark(), Is.True);
        Assert.That(inlinePosition, Is.EqualTo(1));
        Assert.That(inlineContextPipeline!.UsesCommonMark(), Is.True);
        MarkdownNode firstText = services.CreateTreeOperations()
            .FindFirstInDocument(inlineDocument, MarkdownNodeKind.Text)!;
        Assert.That(services.CreateNodeOperations(firstText).GetLiteral(), Is.EqualTo("a"));
        Assert.That(markdown.RenderPlainTextDocumentWithPipeline(inlineDocument, inlinePipeline), Is.EqualTo("aATb\n"));

        MarkdownDocument mapped = markdown.ParseSource("# Title");
        IMarkdownDocumentOperations mappedOperations = services.CreateDocumentOperations(mapped);
        MarkdownSourcePosition position = new MarkdownSourcePosition();
        position.Offset = 2;
        position.Line = 1;
        position.Column = 3;
        Assert.That(mappedOperations.FindSourceMapEntriesContainingOffset(2), Is.Not.Empty);
        Assert.That(mappedOperations.FindDeepestSourceMapEntryAtPosition(position), Is.Not.Null);

        MarkdownPipeline clearedSafety = markdown.CreatePipelineBuilder()
            .AllowUnsafeHtml()
            .RewriteImageUrisWith(new ContractDestinationRewriter("javascript:changed"))
            .SetImageUriPolicy(new ContractUriPolicy(false))
            .SetRawHtmlPolicy(new ContractRawHtmlPolicy(MarkdownRawHtmlDecision.Suppress))
            .ClearImageUriRewriter()
            .ClearImageUriPolicy()
            .ClearRawHtmlPolicy()
            .Build();
        Assert.That(
            markdown.RenderHtmlSourceWithPipeline("![alt](/image.png) <b>raw</b>", clearedSafety),
            Is.EqualTo("<p><img src=\"/image.png\" alt=\"alt\"> <b>raw</b></p>\n"));
    }

    private static MarkdownNodeKind[] EveryNodeKind()
    {
        return new MarkdownNodeKind[]
        {
            MarkdownNodeKind.Unknown, MarkdownNodeKind.Document, MarkdownNodeKind.BlockQuote,
            MarkdownNodeKind.List, MarkdownNodeKind.ListItem, MarkdownNodeKind.Paragraph,
            MarkdownNodeKind.Heading, MarkdownNodeKind.ThematicBreak, MarkdownNodeKind.IndentedCodeBlock,
            MarkdownNodeKind.FencedCodeBlock, MarkdownNodeKind.HtmlBlock,
            MarkdownNodeKind.LinkReferenceDefinition, MarkdownNodeKind.Text,
            MarkdownNodeKind.SoftLineBreak, MarkdownNodeKind.HardLineBreak, MarkdownNodeKind.CodeSpan,
            MarkdownNodeKind.Emphasis, MarkdownNodeKind.StrongEmphasis, MarkdownNodeKind.Link,
            MarkdownNodeKind.Image, MarkdownNodeKind.HtmlInline, MarkdownNodeKind.Table,
            MarkdownNodeKind.TableHead, MarkdownNodeKind.TableBody, MarkdownNodeKind.TableRow,
            MarkdownNodeKind.TableCell, MarkdownNodeKind.Strikethrough, MarkdownNodeKind.TaskListItem,
            MarkdownNodeKind.FootnoteDefinition, MarkdownNodeKind.FootnoteReference,
            MarkdownNodeKind.DefinitionList, MarkdownNodeKind.DefinitionTerm,
            MarkdownNodeKind.DefinitionDescription, MarkdownNodeKind.FrontMatter,
            MarkdownNodeKind.Attribute, MarkdownNodeKind.MathSpan, MarkdownNodeKind.MathBlock,
            MarkdownNodeKind.ExtensionBlock, MarkdownNodeKind.ExtensionInline,
            MarkdownNodeKind.AbbreviationDefinition, MarkdownNodeKind.Abbreviation,
            MarkdownNodeKind.Superscript, MarkdownNodeKind.Subscript, MarkdownNodeKind.InsertedText,
            MarkdownNodeKind.MarkedText, MarkdownNodeKind.DiagramBlock, MarkdownNodeKind.Emoji,
            MarkdownNodeKind.AdmonitionBlock, MarkdownNodeKind.CustomContainerBlock,
            MarkdownNodeKind.CustomFencedBlock, MarkdownNodeKind.CustomInline,
            MarkdownNodeKind.DetailsBlock, MarkdownNodeKind.DetailsSummary
        };
    }

    private static void AssertEveryAdvancedFeature(MarkdownFeatureSet features)
    {
        Assert.That(features.CommonMark, Is.True);
        Assert.That(features.GfmTables, Is.True);
        Assert.That(features.GfmTaskLists, Is.True);
        Assert.That(features.GfmStrikethrough, Is.True);
        Assert.That(features.GfmBareAutolinks, Is.True);
        Assert.That(features.GfmDisallowedRawHtml, Is.True);
        Assert.That(features.Footnotes, Is.True);
        Assert.That(features.DefinitionLists, Is.True);
        Assert.That(features.HeadingIds, Is.True);
        Assert.That(features.Attributes, Is.True);
        Assert.That(features.FrontMatter, Is.True);
        Assert.That(features.Abbreviations, Is.True);
        Assert.That(features.Superscript, Is.True);
        Assert.That(features.Subscript, Is.True);
        Assert.That(features.InsertedText, Is.True);
        Assert.That(features.MarkedText, Is.True);
        Assert.That(features.Typography, Is.True);
        Assert.That(features.Math, Is.True);
        Assert.That(features.Diagrams, Is.True);
        Assert.That(features.Emoji, Is.True);
        Assert.That(features.CjkSoftBreaks, Is.True);
        Assert.That(features.Admonitions, Is.True);
        Assert.That(features.CustomContainers, Is.True);
        Assert.That(features.CustomFencedBlocks, Is.True);
        Assert.That(features.CustomInlineDelimiters, Is.True);
        Assert.That(features.Details, Is.True);
    }

    private static void AssertEveryFeatureDisabled(MarkdownFeatureSet features)
    {
        Assert.That(features.CommonMark, Is.False);
        Assert.That(features.GfmTables, Is.False);
        Assert.That(features.GfmTaskLists, Is.False);
        Assert.That(features.GfmStrikethrough, Is.False);
        Assert.That(features.GfmBareAutolinks, Is.False);
        Assert.That(features.GfmDisallowedRawHtml, Is.False);
        Assert.That(features.Footnotes, Is.False);
        Assert.That(features.DefinitionLists, Is.False);
        Assert.That(features.HeadingIds, Is.False);
        Assert.That(features.Attributes, Is.False);
        Assert.That(features.FrontMatter, Is.False);
        Assert.That(features.Abbreviations, Is.False);
        Assert.That(features.Superscript, Is.False);
        Assert.That(features.Subscript, Is.False);
        Assert.That(features.InsertedText, Is.False);
        Assert.That(features.MarkedText, Is.False);
        Assert.That(features.Typography, Is.False);
        Assert.That(features.Math, Is.False);
        Assert.That(features.Diagrams, Is.False);
        Assert.That(features.Emoji, Is.False);
        Assert.That(features.CjkSoftBreaks, Is.False);
        Assert.That(features.Admonitions, Is.False);
        Assert.That(features.CustomContainers, Is.False);
        Assert.That(features.CustomFencedBlocks, Is.False);
        Assert.That(features.CustomInlineDelimiters, Is.False);
        Assert.That(features.Details, Is.False);
    }

    private sealed class ContractDestinationRewriter : IMarkdownDestinationRewriter
    {
        private readonly string destination;

        public ContractDestinationRewriter(string destination)
        {
            this.destination = destination;
        }

        public string RewriteDestination(MarkdownNode node, string currentDestination)
        {
            return destination;
        }
    }

    private sealed class ContractUriPolicy : IMarkdownUriPolicy
    {
        private readonly bool allowed;

        public ContractUriPolicy(bool allowed)
        {
            this.allowed = allowed;
        }

        public bool AllowUri(MarkdownUriPolicyContext context)
        {
            return allowed;
        }
    }

    private sealed class ContractRawHtmlPolicy : IMarkdownRawHtmlPolicy
    {
        private readonly MarkdownRawHtmlDecision decision;

        public ContractRawHtmlPolicy(MarkdownRawHtmlDecision decision)
        {
            this.decision = decision;
        }

        public MarkdownRawHtmlDecision DecideRawHtml(MarkdownRawHtmlPolicyContext context)
        {
            return decision;
        }
    }
}
