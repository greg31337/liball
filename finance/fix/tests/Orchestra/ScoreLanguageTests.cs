using System;
using System.Linq;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class ScoreLanguageTests
{
    [Test]
    public void LexerRecognizesSymbolicAndWordOperatorsWhileSkippingComments()
    {
        ScoreLexResult result = ScoreLexer.Lex("/* before */ a eq 1 and b!=2 || c mod 3 <= 4 // after");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorCode);
            Assert.That(result.Tokens.ConvertAll(token => token.Kind), Is.EqualTo(new[]
            {
                ScoreTokenKind.Identifier,
                ScoreTokenKind.Equal,
                ScoreTokenKind.UnsignedInteger,
                ScoreTokenKind.And,
                ScoreTokenKind.Identifier,
                ScoreTokenKind.NotEqual,
                ScoreTokenKind.UnsignedInteger,
                ScoreTokenKind.Or,
                ScoreTokenKind.Identifier,
                ScoreTokenKind.Modulo,
                ScoreTokenKind.UnsignedInteger,
                ScoreTokenKind.LessEqual,
                ScoreTokenKind.UnsignedInteger,
                ScoreTokenKind.End
            }));
            Assert.That(result.Tokens[0].Text, Is.EqualTo("a"));
            Assert.That(result.Tokens[0].Start, Is.EqualTo(13));
            Assert.That(result.Tokens[^1].Start, Is.EqualTo(53));
        });
    }

    [Test]
    public void ParserBuildsTypedLiteralNodesAndDecodesQuotedEscapes()
    {
        ScoreParseResult number = ScoreParser.Parse("-123.45");
        ScoreParseResult text = ScoreParser.Parse("\"line\\nnext\"");
        ScoreParseResult character = ScoreParser.Parse("'\\''");
        ScoreParseResult date = ScoreParser.Parse("#2017-03-21#");
        ScoreParseResult time = ScoreParser.Parse("#09:58:24.123456789Z#");
        ScoreParseResult timestamp = ScoreParser.Parse("#2017-03-21T09:58-06:00#");
        ScoreParseResult duration = ScoreParser.Parse("#P7DT1H30M#");

        Assert.Multiple(() =>
        {
            AssertSuccess(number, ScoreSyntaxKind.Unary);
            Assert.That(number.Root!.Operator, Is.EqualTo(ScoreTokenKind.Minus));
            Assert.That(number.Root.Left!.Kind, Is.EqualTo(ScoreSyntaxKind.DecimalLiteral));
            Assert.That(number.Root.Left.Text, Is.EqualTo("123.45"));
            AssertSuccess(text, ScoreSyntaxKind.StringLiteral);
            Assert.That(text.Root!.Text, Is.EqualTo("line\nnext"));
            AssertSuccess(character, ScoreSyntaxKind.CharacterLiteral);
            Assert.That(character.Root!.Text, Is.EqualTo("'"));
            AssertSuccess(date, ScoreSyntaxKind.DateLiteral);
            Assert.That(date.Root!.Text, Is.EqualTo("2017-03-21"));
            AssertSuccess(time, ScoreSyntaxKind.TimeLiteral);
            AssertSuccess(timestamp, ScoreSyntaxKind.TimestampLiteral);
            AssertSuccess(duration, ScoreSyntaxKind.DurationLiteral);
        });
    }

    [Test]
    public void OperatorPrecedenceMatchesTheScoreGrammar()
    {
        ScoreParseResult result = ScoreParser.Parse("1 + 2 * 3 == 7 or !false and true");

        ScoreSyntaxNode root = result.Root!;
        ScoreSyntaxNode equality = root.Left!;
        ScoreSyntaxNode addition = equality.Left!;
        ScoreSyntaxNode conjunction = root.Right!;
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorCode);
            Assert.That(root.Operator, Is.EqualTo(ScoreTokenKind.Or));
            Assert.That(equality.Operator, Is.EqualTo(ScoreTokenKind.Equal));
            Assert.That(addition.Operator, Is.EqualTo(ScoreTokenKind.Plus));
            Assert.That(addition.Right!.Operator, Is.EqualTo(ScoreTokenKind.Multiply));
            Assert.That(conjunction.Operator, Is.EqualTo(ScoreTokenKind.And));
            Assert.That(conjunction.Left!.Kind, Is.EqualTo(ScoreSyntaxKind.Unary));
            Assert.That(conjunction.Left.Operator, Is.EqualTo(ScoreTokenKind.Not));
        });
    }

    [Test]
    public void ScopedIndexedAndPredicateSelectedReferencesRetainTheirStructure()
    {
        ScoreParseResult result = ScoreParser.Parse("in.Parties[PartyRole==^ExecutingFirm].PartyID == $market.roles[2].Name");

        ScoreSyntaxNode comparison = result.Root!;
        ScoreSyntaxNode incoming = comparison.Left!;
        ScoreSyntaxNode parties = incoming.Children[0];
        ScoreSyntaxNode predicate = parties.Left!;
        ScoreSyntaxNode state = comparison.Right!;
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(comparison.Operator, Is.EqualTo(ScoreTokenKind.Equal));
            Assert.That(incoming.Text, Is.EqualTo("in"));
            Assert.That(incoming.Children.ConvertAll(part => part.Text), Is.EqualTo(new[] { "Parties", "PartyID" }));
            Assert.That(predicate.Kind, Is.EqualTo(ScoreSyntaxKind.PredicateSelector));
            Assert.That(predicate.Text, Is.EqualTo("PartyRole"));
            Assert.That(predicate.Right!.Kind, Is.EqualTo(ScoreSyntaxKind.Reference));
            Assert.That(predicate.Right.Text, Is.EqualTo("^"));
            Assert.That(predicate.Right.Children[0].Text, Is.EqualTo("ExecutingFirm"));
            Assert.That(state.Text, Is.EqualTo("$"));
            Assert.That(state.Children.ConvertAll(part => part.Text), Is.EqualTo(new[] { "market", "roles", "Name" }));
            Assert.That(state.Children[1].Left!.Kind, Is.EqualTo(ScoreSyntaxKind.IntegerLiteral));
            Assert.That(state.Children[1].Left!.Text, Is.EqualTo("2"));
        });
    }

    [Test]
    public void SetRangeAndExistenceExpressionsHaveDedicatedSyntaxNodes()
    {
        ScoreParseResult condition = ScoreParser.Parse("OrdType in {^Stop, ^StopLimit} and Price between 10 + 1 and 20");
        ScoreParseResult standardExists = ScoreParser.Parse("exists ClOrdID");
        ScoreParseResult stateExists = ScoreParser.Parse("exists($market.phase)");
        ScoreParseResult numericExists = ScoreParser.Parse("exists(11)");

        ScoreSyntaxNode conjunction = condition.Root!;
        ScoreSyntaxNode set = conjunction.Left!;
        ScoreSyntaxNode range = conjunction.Right!;
        Assert.Multiple(() =>
        {
            Assert.That(condition.Success, Is.True, condition.ErrorCode);
            Assert.That(set.Kind, Is.EqualTo(ScoreSyntaxKind.SetMembership));
            Assert.That(set.Children, Has.Count.EqualTo(2));
            Assert.That(set.Children.All(member => member.Kind == ScoreSyntaxKind.Reference && member.Text == "^"), Is.True);
            Assert.That(range.Kind, Is.EqualTo(ScoreSyntaxKind.RangeMembership));
            Assert.That(range.Right!.Operator, Is.EqualTo(ScoreTokenKind.Plus));
            Assert.That(range.Third!.Text, Is.EqualTo("20"));
            AssertSuccess(standardExists, ScoreSyntaxKind.Exists);
            Assert.That(standardExists.Root!.Left!.Children[0].Text, Is.EqualTo("ClOrdID"));
            AssertSuccess(stateExists, ScoreSyntaxKind.Exists);
            Assert.That(stateExists.Root!.Left!.Text, Is.EqualTo("$"));
            AssertSuccess(numericExists, ScoreSyntaxKind.Exists);
            Assert.That(numericExists.Root!.Left!.Children[0].Text, Is.EqualTo("11"));
        });
    }

    [Test]
    public void AssignmentRequiresAReferenceTargetAndKeepsTheValueExpression()
    {
        ScoreParseResult field = ScoreParser.Parse("out.ClOrdID = in.ClOrdID");
        ScoreParseResult state = ScoreParser.Parse("$participant.totalQty = $participant.totalQty + 1");
        ScoreParseResult invalid = ScoreParser.Parse("1 = 2");
        ScoreParseResult parenthesizedTarget = ScoreParser.Parse("(out.ClOrdID) = in.ClOrdID");

        Assert.Multiple(() =>
        {
            AssertSuccess(field, ScoreSyntaxKind.Assignment);
            Assert.That(field.Root!.Left!.Text, Is.EqualTo("out"));
            Assert.That(field.Root.Right!.Text, Is.EqualTo("in"));
            AssertSuccess(state, ScoreSyntaxKind.Assignment);
            Assert.That(state.Root!.Left!.Text, Is.EqualTo("$"));
            Assert.That(state.Root.Right!.Operator, Is.EqualTo(ScoreTokenKind.Plus));
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.ErrorCode, Is.EqualTo("invalid-assignment-target"));
            Assert.That(invalid.ErrorOffset, Is.EqualTo(2));
            Assert.That(parenthesizedTarget.ErrorCode, Is.EqualTo("invalid-assignment-target"));
        });
    }

    [Test]
    public void ParsesPublishedOrchestraScoreExamples()
    {
        ScoreParseResult fieldValidation = ScoreParser.Parse("(Price * 100) % 5 != 0");
        ScoreParseResult responseCondition = ScoreParser.Parse("$Market.SecMassStatGrp[SecurityID==in.SecurityID].SecurityTradingStatus != ^TradingHalt and $Market.Phase == \"Open\"");
        ScoreParseResult stateAssignment = ScoreParser.Parse("$participant.RiskLimitAmount=15000");
        ScoreParseResult ordinalReference = ScoreParser.Parse("MDIncGrp[2].MDEntryType");
        ScoreParseResult numericPredicate = ScoreParser.Parse("Parties[PartyRole==4].PartyID");
        ScoreParseResult codePredicate = ScoreParser.Parse("Parties[PartyRole==^ClearingFirm].PartyID");
        ScoreParseResult dateDuration = ScoreParser.Parse("#P7D#");
        ScoreParseResult timeDuration = ScoreParser.Parse("#PT1H30M#");

        Assert.Multiple(() =>
        {
            AssertSuccess(fieldValidation, ScoreSyntaxKind.Binary);
            Assert.That(fieldValidation.Root!.Operator, Is.EqualTo(ScoreTokenKind.NotEqual));
            AssertSuccess(responseCondition, ScoreSyntaxKind.Binary);
            Assert.That(responseCondition.Root!.Operator, Is.EqualTo(ScoreTokenKind.And));
            AssertSuccess(stateAssignment, ScoreSyntaxKind.Assignment);
            AssertSuccess(ordinalReference, ScoreSyntaxKind.Reference);
            AssertSuccess(numericPredicate, ScoreSyntaxKind.Reference);
            AssertSuccess(codePredicate, ScoreSyntaxKind.Reference);
            AssertSuccess(dateDuration, ScoreSyntaxKind.DurationLiteral);
            AssertSuccess(timeDuration, ScoreSyntaxKind.DurationLiteral);
        });
    }

    [TestCase("", "empty-expression")]
    [TestCase("/* only comment */", "empty-expression")]
    [TestCase("/* unterminated", "unterminated-comment")]
    [TestCase("\"unterminated", "unterminated-quoted-literal")]
    [TestCase("\"bad\\xescape\"", "invalid-escape-sequence")]
    [TestCase("#not-a-date#", "invalid-temporal-literal")]
    [TestCase("#2019-02-29#", "invalid-temporal-literal")]
    [TestCase("#24:00:00Z#", "invalid-temporal-literal")]
    [TestCase("#12:34.123Z#", "invalid-temporal-literal")]
    [TestCase("#12:34+1:00#", "invalid-temporal-literal")]
    [TestCase("#P#", "invalid-temporal-literal")]
    [TestCase("#PT#", "invalid-temporal-literal")]
    [TestCase("(a == 1", "expected-right-parenthesis")]
    [TestCase("a in {}", "empty-set")]
    [TestCase("a in {1,}", "expected-set-member")]
    [TestCase("a between 1 2", "expected-range-and")]
    [TestCase("Parties[Role!=1]", "expected-predicate-equality")]
    [TestCase("a +", "expected-expression")]
    public void MalformedExpressionsReturnStableSyntaxFailures(string expression, string expectedCode)
    {
        ScoreParseResult result = ScoreParser.Parse(expression);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Root, Is.Null);
            Assert.That(result.ErrorCode, Is.EqualTo(expectedCode));
            Assert.That(result.ErrorOffset, Is.GreaterThanOrEqualTo(0));
            Assert.That(result.ErrorMessage, Is.Not.Empty);
        });
    }

    [Test]
    public void ExpressionLengthAndNestingAreBounded()
    {
        ScoreParseResult tooLong = ScoreParser.Parse(new string('a', ScoreLexer.MaximumExpressionLength + 1));
        string nestedText = new string('(', ScoreParser.MaximumParseDepth + 1) + "a" + new string(')', ScoreParser.MaximumParseDepth + 1);
        ScoreParseResult tooDeep = ScoreParser.Parse(nestedText);

        Assert.Multiple(() =>
        {
            Assert.That(tooLong.ErrorCode, Is.EqualTo("expression-too-long"));
            Assert.That(tooDeep.ErrorCode, Is.EqualTo("expression-depth-limit-exceeded"));
        });
    }

    private static void AssertSuccess(ScoreParseResult result, ScoreSyntaxKind kind)
    {
        Assert.That(result.Success, Is.True, result.ErrorCode + ":" + result.ErrorMessage);
        Assert.That(result.Root, Is.Not.Null);
        Assert.That(result.Root!.Kind, Is.EqualTo(kind));
    }
}
