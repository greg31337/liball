using System.Text;
using asn1;

namespace asn1.Tests;

public class IncrementalParseSessionTests
{
    [Test]
    public void SessionResumesAcrossEverySuppliedTokenBoundary()
    {
        string[] chunks =
        [
            "Example ",
            "{ ",
            "iso ",
            "( ",
            "1 ",
            ") ",
            "2 ",
            "} ",
            "DEFINITIONS ",
            "AUTOMATIC ",
            "TAGS ",
            "::= ",
            "BEGIN ",
            "Value ",
            "::= ",
            "SEQUENCE ",
            "{ ",
            "item ",
            "INTEGER ",
            "( ",
            "1 ",
            ".. ",
            "5 ",
            ") ",
            "} ",
            "END"
        ];
        IncrementalParseSession session = CreateSession("boundaries.asn");

        int index = 0;
        while (index < chunks.Length - 1)
        {
            session.SupplyTextChunk(chunks[index], false);
            Assert.That(session.ParseNext().Status, Is.EqualTo(ModuleParseStatus.NeedMoreData));
            index = index + 1;
        }

        session.SupplyTextChunk(chunks[index], true);
        ModuleParseResult result = session.ParseNext();

        Assert.That(result.Status, Is.EqualTo(ModuleParseStatus.Parsed));
        Assert.That(result.Module?.Name, Is.EqualTo("Example"));
        Assert.That(result.Module?.DefinitiveOidComponents, Has.Length.EqualTo(2));
        Assert.That(result.Module?.AssignmentHeads, Has.Length.EqualTo(1));
        Assert.That(session.GetDiagnostics(), Is.Empty);
        Assert.That(session.ParseNext().Status, Is.EqualTo(ModuleParseStatus.EndOfInput));
    }

    [Test]
    public void SessionRetainsExplicitNestedProductionFramesWhileWaiting()
    {
        IncrementalParseSession session = CreateSession("stack.asn");
        session.SupplyTextChunk(
            "Nested DEFINITIONS ::= BEGIN Value ::= SEQUENCE { item INTEGER (",
            false);

        ModuleParseResult waiting = session.ParseNext();
        ParseProductionFrame[] stack = session.GetProductionStack();

        Assert.That(waiting.Status, Is.EqualTo(ModuleParseStatus.NeedMoreData));
        Assert.That(
            stack.Select(frame => frame.Kind),
            Is.EqualTo(new[]
            {
                ParseProductionKind.CompilationUnit,
                ParseProductionKind.Module,
                ParseProductionKind.ModuleBody,
                ParseProductionKind.BracedGroup,
                ParseProductionKind.ParenthesizedGroup
            }));
        Assert.That(stack[1].State, Is.EqualTo(2));
        Assert.That(stack[3].OpeningToken?.Text, Is.EqualTo("{"));
        Assert.That(stack[4].OpeningToken?.Text, Is.EqualTo("("));

        session.SupplyTextChunk("1 .. 5) } END", true);
        Assert.That(session.ParseNext().Status, Is.EqualTo(ModuleParseStatus.Parsed));
        Assert.That(
            session.GetProductionStack().Select(frame => frame.Kind),
            Is.EqualTo(new[] { ParseProductionKind.CompilationUnit }));
    }

    [Test]
    public void SessionReturnsConsecutiveModulesIndependently()
    {
        IncrementalParseSession session = CreateSession("modules.asn");
        session.SupplyTextChunk(
            "One DEFINITIONS ::= BEGIN First ::= INTEGER END "
                + "Two DEFINITIONS ::= BEGIN Second ::= BOOLEAN END",
            true);

        ModuleParseResult first = session.ParseNext();
        ModuleParseResult second = session.ParseNext();
        ModuleParseResult end = session.ParseNext();

        Assert.That(first.Status, Is.EqualTo(ModuleParseStatus.Parsed));
        Assert.That(first.Module?.Name, Is.EqualTo("One"));
        Assert.That(first.Module?.AssignmentHeads[0].Alternatives[0].Name, Is.EqualTo("First"));
        Assert.That(second.Status, Is.EqualTo(ModuleParseStatus.Parsed));
        Assert.That(second.Module?.Name, Is.EqualTo("Two"));
        Assert.That(second.Module?.AssignmentHeads[0].Alternatives[0].Name, Is.EqualTo("Second"));
        Assert.That(end.Status, Is.EqualTo(ModuleParseStatus.EndOfInput));
        Assert.That(session.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void SessionFinalizesAnIncompleteModuleAtFinalInput()
    {
        IncrementalParseSession session = CreateSession("incomplete.asn");
        session.SupplyTextChunk(
            "Broken DEFINITIONS ::= BEGIN Value ::= INTEGER",
            true);

        ModuleParseResult result = session.ParseNext();

        Assert.That(result.Status, Is.EqualTo(ModuleParseStatus.Invalid));
        Assert.That(result.Module?.Name, Is.EqualTo("Broken"));
        Assert.That(result.Module?.AssignmentHeads, Has.Length.EqualTo(1));
        Assert.That(
            session.GetDiagnostics().Select(diagnostic => diagnostic.Code),
            Does.Contain("ASNP0001"));
        Assert.That(session.ParseNext().Status, Is.EqualTo(ModuleParseStatus.EndOfInput));
    }

    [Test]
    public void SessionResumesUtf8InputInsideAMultibyteTokenBoundary()
    {
        byte[] source = Encoding.UTF8.GetBytes(
            "Utf DEFINITIONS ::= BEGIN -- π --\nValue ::= INTEGER END");
        IncrementalParseSession session = CreateSession("utf8.asn");

        int index = 0;
        while (index < source.Length)
        {
            bool isFinal = index == source.Length - 1;
            session.SupplyUtf8Chunk([source[index]], isFinal);
            ModuleParseResult current = session.ParseNext();
            if (isFinal)
            {
                Assert.That(current.Status, Is.EqualTo(ModuleParseStatus.Parsed));
                Assert.That(current.Module?.Name, Is.EqualTo("Utf"));
            }
            else
            {
                Assert.That(current.Status, Is.EqualTo(ModuleParseStatus.NeedMoreData));
            }

            index = index + 1;
        }

        Assert.That(session.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void EveryTextAndUtf8SplitParsesTheConformanceCorpusIdentically()
    {
        string source = "\uFEFFConformance { iso(1) 3 6 } DEFINITIONS AUTOMATIC TAGS "
            + "EXTENSIBILITY IMPLIED ::= BEGIN\n"
            + "EXPORTS First, first, XmlValue;\n"
            + "First ::= SEQUENCE { flag BOOLEAN OPTIONAL, number INTEGER DEFAULT 1 }\n"
            + "first First ::= { flag TRUE, number 2 }\n"
            + "XmlValue UTF8String ::= <UTF8String>é\ud83d\ude00</UTF8String>\n"
            + "END";

        int textSplit = 0;
        while (textSplit <= source.Length)
        {
            IncrementalParseSession session = CreateSession("conformance-text.asn");
            session.SupplyTextChunk(source.Substring(0, textSplit), false);
            Assert.That(
                session.ParseNext().Status,
                Is.EqualTo(ModuleParseStatus.NeedMoreData),
                "UTF-16 split " + textSplit);
            session.SupplyTextChunk(source.Substring(textSplit), true);

            this.AssertConformanceModule(
                source,
                session.ParseNext(),
                session,
                "UTF-16 split " + textSplit);
            textSplit = textSplit + 1;
        }

        byte[] utf8 = Encoding.UTF8.GetBytes(source);
        int byteSplit = 0;
        while (byteSplit <= utf8.Length)
        {
            IncrementalParseSession session = CreateSession("conformance-utf8.asn");
            session.SupplyUtf8Chunk(utf8.Take(byteSplit).ToArray(), false);
            Assert.That(
                session.ParseNext().Status,
                Is.EqualTo(ModuleParseStatus.NeedMoreData),
                "UTF-8 split " + byteSplit);
            session.SupplyUtf8Chunk(utf8.Skip(byteSplit).ToArray(), true);

            this.AssertConformanceModule(
                source,
                session.ParseNext(),
                session,
                "UTF-8 split " + byteSplit);
            byteSplit = byteSplit + 1;
        }
    }

    [Test]
    public void EveryMalformedModuleSplitRecoversAtTheNextModuleBoundary()
    {
        string source = "Broken DEFINITIONS AUTOMATIC junk END "
            + "Healthy DEFINITIONS ::= BEGIN Good ::= INTEGER END";

        int split = 0;
        while (split <= source.Length)
        {
            IncrementalParseSession session = CreateSession("recovery-splits.asn");
            System.Collections.Generic.List<ModuleParseResult> results =
                new System.Collections.Generic.List<ModuleParseResult>();
            session.SupplyTextChunk(source.Substring(0, split), false);
            results.AddRange(this.DrainModulesUntilWait(session));
            session.SupplyTextChunk(source.Substring(split), true);
            results.AddRange(this.DrainModulesUntilWait(session));

            Assert.Multiple(() =>
            {
                Assert.That(
                    results.Select(result => result.Status),
                    Is.EqualTo(new[]
                    {
                        ModuleParseStatus.Invalid,
                        ModuleParseStatus.Parsed
                    }),
                    "split " + split);
                Assert.That(
                    results.Select(result => result.Module?.Name),
                    Is.EqualTo(new[] { "Broken", "Healthy" }),
                    "split " + split);
                Assert.That(
                    results[0].Module?.Recoveries.Single().SkippedTokens
                        .Select(token => token.Text),
                    Is.EqualTo(new[] { "junk" }),
                    "split " + split);
                Assert.That(
                    results[1].Module?.AssignmentHeads.Single()
                        .DeclaredReference?.Name,
                    Is.EqualTo("Good"),
                    "split " + split);
                Assert.That(
                    session.GetDiagnostics().Select(diagnostic => diagnostic.Code),
                    Does.Contain("ASNP0001"),
                    "split " + split);
                Assert.That(
                    session.ParseNext().Status,
                    Is.EqualTo(ModuleParseStatus.EndOfInput),
                    "split " + split);
            });
            split = split + 1;
        }
    }

    private void AssertConformanceModule(
        string source,
        ModuleParseResult result,
        IncrementalParseSession session,
        string context)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(ModuleParseStatus.Parsed), context);
            Assert.That(result.Module?.Name, Is.EqualTo("Conformance"), context);
            Assert.That(result.Module?.DefinitiveOidComponents, Has.Length.EqualTo(3), context);
            Assert.That(result.Module?.TagDefault, Is.EqualTo(AsnTaggingMode.Automatic), context);
            Assert.That(result.Module?.ExtensibilityImplied, Is.True, context);
            Assert.That(
                result.Module?.AssignmentHeads.Select(
                    head => head.DeclaredReference?.Name),
                Is.EqualTo(new[] { "First", "first", "XmlValue" }),
                context);
            Assert.That(
                result.Module?.BodyTokens.Select(token => token.Kind),
                Does.Contain(AsnTokenKind.XmlValue),
                context);
            Assert.That(result.Module?.Span?.Start.Offset, Is.EqualTo(1), context);
            Assert.That(result.Module?.Span?.End.Offset, Is.EqualTo(source.Length), context);
            Assert.That(session.GetDiagnostics(), Is.Empty, context);
            Assert.That(
                session.ParseNext().Status,
                Is.EqualTo(ModuleParseStatus.EndOfInput),
                context);
        });
    }

    private System.Collections.Generic.List<ModuleParseResult> DrainModulesUntilWait(
        IncrementalParseSession session)
    {
        System.Collections.Generic.List<ModuleParseResult> results =
            new System.Collections.Generic.List<ModuleParseResult>();
        while (true)
        {
            ModuleParseResult result = session.ParseNext();
            if (result.Status == ModuleParseStatus.NeedMoreData
                || result.Status == ModuleParseStatus.EndOfInput)
            {
                return results;
            }

            results.Add(result);
        }
    }

    private static IncrementalParseSession CreateSession(string sourceName)
    {
        return new IncrementalParseSession(
            sourceName,
            false,
            new AsnResourceLimits(),
            4);
    }
}
