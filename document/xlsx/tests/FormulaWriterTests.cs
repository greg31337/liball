using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class FormulaWriterTests
{
    [Test]
    public void WriteString_PreservesParsedFormulaText()
    {
        string formula = "SUM(A1:B2,Table1[Column],TaxRate)";
        XlsxFormulaParseResult parseResult = new XlsxFormulaParser().Parse(formula);

        XlsxFormulaWriteResult writeResult = new XlsxFormulaWriter().WriteParseResult(parseResult);

        Assert.That(parseResult.IsValid, Is.True);
        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(writeResult.Text, Is.EqualTo(formula));
    }

    [Test]
    public void WriteNode_WritesConstructedFormulaTree()
    {
        XlsxFormulaNode multiply = CreateBinary("*", CreateLiteral("2"), CreateReference("B1"));
        XlsxFormulaNode root = CreateBinary("+", CreateReference("A1"), multiply);

        XlsxFormulaWriteResult result = new XlsxFormulaWriter().WriteNode(root);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Text, Is.EqualTo("A1+2*B1"));
    }

    [Test]
    public void WriteNode_WritesConstructedFunctionsArraysAndPostfixOperators()
    {
        XlsxFormulaNode percent = new XlsxFormulaNode();
        percent.Kind = XlsxFormulaNodeKind.UnaryOperation;
        percent.Operator = "%";
        percent.Children.Add(CreateLiteral("50"));
        XlsxFormulaNode array = new XlsxFormulaNode();
        array.Kind = XlsxFormulaNodeKind.ArrayConstant;
        array.Children.Add(CreateLiteral("1"));
        array.Children.Add(CreateLiteral("2"));
        XlsxFormulaNode function = new XlsxFormulaNode();
        function.Kind = XlsxFormulaNodeKind.FunctionCall;
        function.Operator = "SUM";
        function.Children.Add(CreateRange("A1:B2"));
        function.Children.Add(percent);
        function.Children.Add(array);

        XlsxFormulaWriteResult result = new XlsxFormulaWriter().WriteNode(function);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Text, Is.EqualTo("SUM(A1:B2,50%,{1,2})"));
    }

    [Test]
    public void WriteString_PreservesIncompleteAndUnknownFormulaText()
    {
        XlsxFormulaParseResult missingClose = new XlsxFormulaParser().Parse("SUM(A1");
        XlsxFormulaParseResult unknownSyntax = new XlsxFormulaParser().Parse("A1~B1");

        XlsxFormulaWriteResult missingCloseResult = new XlsxFormulaWriter().WriteParseResult(missingClose);
        XlsxFormulaWriteResult unknownSyntaxResult = new XlsxFormulaWriter().WriteParseResult(unknownSyntax);

        Assert.That(missingClose.IsValid, Is.False);
        Assert.That(missingCloseResult.IsValid, Is.False);
        Assert.That(missingCloseResult.Text, Is.EqualTo("SUM(A1"));
        Assert.That(HasDiagnostic(missingCloseResult, "formulaParser.closeParenMissing"), Is.True);
        Assert.That(unknownSyntax.IsValid, Is.False);
        Assert.That(unknownSyntaxResult.IsValid, Is.False);
        Assert.That(unknownSyntaxResult.Text, Is.EqualTo("A1~B1"));
        Assert.That(HasDiagnostic(unknownSyntaxResult, "formulaTokenizer.characterUnsupported"), Is.True);
    }

    [Test]
    public void WriteString_ReportsInvalidInputs()
    {
        XlsxFormulaParseResult emptyParse = new XlsxFormulaParseResult();

        XlsxFormulaWriteResult? nullParseResult = new XlsxFormulaWriter().WriteParseResult((XlsxFormulaParseResult)null!);
        XlsxFormulaWriteResult emptyParseResult = new XlsxFormulaWriter().WriteParseResult(emptyParse);
        XlsxFormulaWriteResult? nullNodeResult = new XlsxFormulaWriter().WriteNode(null!);

        Assert.That(nullParseResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullParseResult, "formulaWriter.parseResultNull"), Is.True);
        Assert.That(emptyParseResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(emptyParseResult, "formulaWriter.rootMissing"), Is.True);
        Assert.That(nullNodeResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullNodeResult, "formulaWriter.nodeNull"), Is.True);
    }

    static XlsxFormulaNode CreateReference(string text)
    {
        XlsxFormulaNode node = new XlsxFormulaNode();
        node.Kind = XlsxFormulaNodeKind.Reference;
        node.Text = text;
        node.Length = text.Length;
        return node;
    }

    static XlsxFormulaNode CreateLiteral(string text)
    {
        XlsxFormulaNode node = new XlsxFormulaNode();
        node.Kind = XlsxFormulaNodeKind.Literal;
        node.Text = text;
        node.Length = text.Length;
        return node;
    }

    static XlsxFormulaNode CreateRange(string text)
    {
        XlsxFormulaNode node = new XlsxFormulaNode();
        node.Kind = XlsxFormulaNodeKind.Range;
        node.Text = text;
        node.Length = text.Length;
        return node;
    }

    static XlsxFormulaNode CreateBinary(string op, XlsxFormulaNode left, XlsxFormulaNode right)
    {
        XlsxFormulaNode node = new XlsxFormulaNode();
        node.Kind = XlsxFormulaNodeKind.BinaryOperation;
        node.Operator = op;
        node.Children.Add(left);
        node.Children.Add(right);
        return node;
    }

    static bool HasDiagnostic(XlsxFormulaWriteResult result, string code)
    {
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
