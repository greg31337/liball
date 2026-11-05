using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class PublicToolingInterfaceTests
{
    [Test]
    public void CompatibilityAnalyzer_ExercisesEveryInterfaceMethod()
    {
        TemplateProfileRegistry profileRegistry = new TemplateProfileRegistry();
        ITemplateCompatibilityAnalyzer analyzer = new TemplateCompatibilityAnalyzer();
        analyzer.SetProfileRegistry(profileRegistry);

        TemplateCompatibilityAnalysis converted = analyzer.Analyze("{{ name | upper }}", "native", "mustache");
        TemplateDocumentSyntax document = profileRegistry.Parse("{% for item in items %}{{ item }}{% endfor %}", "native").Document;
        TemplateCompatibilityAnalysis native = analyzer.AnalyzeNative(document, "mustache");

        Assert.That(converted.NativeTemplateText, Is.EqualTo("{{ name | upper }}"));
        Assert.That(converted.Diagnostics.Count, Is.GreaterThan(0));
        Assert.That(native.Diagnostics.Exists((TemplateDiagnostic diagnostic) => diagnostic.Code == "COMPAT008"), Is.True);
    }

    [Test]
    public void DependencyGraphBuilder_ExercisesEveryInterfaceMethodAndUsesLiteralSpan()
    {
        TemplateProfileRegistry profileRegistry = new TemplateProfileRegistry();
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("child", "Child");
        TemplateOptions options = new TemplateOptions();
        ITemplateDependencyGraphBuilder builder = new TemplateDependencyGraphBuilder();
        builder.SetProfileRegistry(profileRegistry);
        builder.Configure(options, registry);

        string templateText = "{% include \"child\" %}";
        TemplateDependencyGraph fromText = builder.Build(templateText);
        TemplateDocumentSyntax document = profileRegistry.Parse(templateText, "native").Document;
        TemplateDependencyGraph fromDocument = builder.BuildDocument(document);

        Assert.That(fromText.Edges.Count, Is.EqualTo(1));
        Assert.That(fromText.Edges[0].Span, Is.Not.Null);
        Assert.That(fromText.Edges[0].Span!.Start, Is.EqualTo(11));
        Assert.That(fromText.Edges[0].Span.Length, Is.EqualTo(7));
        Assert.That(fromText.Edges[0].Span.Source.Text.Substring(fromText.Edges[0].Span.Start, fromText.Edges[0].Span.Length), Is.EqualTo("\"child\""));
        Assert.That(fromDocument.Edges.Count, Is.EqualTo(1));
        Assert.That(fromDocument.Edges[0].Resolved, Is.True);
    }

    [Test]
    public void SyntaxDumper_ExercisesEveryInterfaceMethod()
    {
        TemplateProfileRegistry profileRegistry = new TemplateProfileRegistry();
        TemplateDocumentSyntax document = profileRegistry.Parse("Hi {{ user.name }}", "native").Document;
        TemplateSyntaxNode textNode = document.Children[0];
        TemplateExpressionSyntax expression = document.Children[1].Output!.Expression;
        ITemplateSyntaxDumper dumper = new TemplateSyntaxDumper();

        string fromText = dumper.Dump("Hi {{ user.name }}");
        string fromDocument = dumper.DumpDocument(document);
        string fromNode = dumper.DumpNode(textNode);
        string fromExpression = dumper.DumpExpression(expression);

        Assert.That(fromText, Is.EqualTo(fromDocument));
        Assert.That(fromText, Does.Contain("Document"));
        Assert.That(fromNode, Does.StartWith("Text "));
        Assert.That(fromExpression, Does.Contain("path=Variable"));
        Assert.That(fromExpression, Does.Contain("PathSegment Name text=\"user\""));
    }

    [Test]
    public void DiagnosticFormatter_ExercisesEveryInterfaceMethodAndAlignsTabs()
    {
        TemplateSource source = new TemplateSource("diagnostic", null, null, null, "first\r\n\tvalue");
        TemplateSourceSpan span = new TemplateSourceSpan(source, 8, 5);
        TemplateDiagnostic diagnostic = new TemplateDiagnostic("TOOL001", TemplateDiagnosticSeverity.Warning, "Check value", span, null);
        ITemplateDiagnosticFormatter formatter = new TemplateDiagnosticFormatter();

        string formatted = formatter.Format(diagnostic);

        Assert.That(formatter.GetLineNumber(source.Text, 8), Is.EqualTo(2));
        Assert.That(formatter.GetColumnNumber(source.Text, 8), Is.EqualTo(2));
        Assert.That(formatter.GetLineText(source.Text, 8), Is.EqualTo("\tvalue"));
        Assert.That(formatted, Is.EqualTo("diagnostic:2:2: warning TOOL001: Check value\n\tvalue\n\t^^^^^"));
    }

    [Test]
    public void Formatter_ExercisesEveryInterfaceMethod()
    {
        TemplateFormatterOptions options = new TemplateFormatterOptions();
        options.IndentText = "    ";
        options.NewLine = "\n";
        options.WhitespaceMode = TemplateWhitespaceMode.Preserve;
        ITemplateFormatter formatter = new TemplateFormatter();
        formatter.Configure(options);

        string templateText = "{% if ready %}Yes{% endif %}";
        string fromText = formatter.Format(templateText);
        TemplateDocumentSyntax document = new TemplateProfileRegistry().Parse(templateText, "native").Document;
        string fromDocument = formatter.FormatDocument(document);

        Assert.That(fromText, Is.EqualTo("{% if ready %}\n    Yes\n{% endif %}\n"));
        Assert.That(fromDocument, Is.EqualTo(fromText));
    }

    [Test]
    public void WhitespaceNormalizer_ExercisesEveryInterfaceMethod()
    {
        ITemplateWhitespaceNormalizer normalizer = new TemplateWhitespaceNormalizer();
        TemplateOptions options = new TemplateOptions();
        options.WhitespaceMode = TemplateWhitespaceMode.Minimize;

        Assert.That(normalizer.Normalize("a\r\nb", TemplateWhitespaceMode.NormalizeLineFeeds), Is.EqualTo("a\nb"));
        Assert.That(normalizer.NormalizeWithOptions("a  \r\n\r\n\r\nb\t ", options), Is.EqualTo("a\n\nb"));
        Assert.That(normalizer.ConvertToLineFeeds("a\r\nb\rc"), Is.EqualTo("a\nb\nc"));
        Assert.That(normalizer.ConvertToCarriageReturnLineFeeds("a\nb\rc"), Is.EqualTo("a\r\nb\r\nc"));
        Assert.That(normalizer.TrimTrailingLineWhitespace("a \r\nb\t\nc  "), Is.EqualTo("a\r\nb\nc  "));
        Assert.That(normalizer.MinimizeWhitespace("a \n\n\n\nb\t "), Is.EqualTo("a\n\nb"));
    }

    [Test]
    public void SourceLineMap_ExercisesEveryInterfaceMethod()
    {
        TemplateSource source = new TemplateSource("lines", null, null, null, "a\r\nbc\n");
        ITemplateSourceLineMap lineMap = new TemplateSourceLineMap(source);

        TemplateSourcePosition position = lineMap.GetPosition(4);
        TemplateSourceLine line = lineMap.GetLine(2);

        Assert.That(lineMap.GetLineCount(), Is.EqualTo(3));
        Assert.That(position.LineNumber, Is.EqualTo(2));
        Assert.That(position.ColumnNumber, Is.EqualTo(2));
        Assert.That(line.Start, Is.EqualTo(3));
        Assert.That(line.End, Is.EqualTo(5));
        Assert.That(lineMap.GetLineText(2), Is.EqualTo("bc"));
        Assert.That(lineMap.GetOffset(2, 2), Is.EqualTo(4));
        Assert.That(lineMap.IsOffsetAtLineStart(3), Is.True);
        Assert.That(lineMap.IsOffsetAtLineEnd(5), Is.True);
    }

    [Test]
    public void ValueFactory_ExercisesEveryInterfaceMethod()
    {
        TemplateValueConversionPolicy policy = new TemplateValueConversionPolicy();
        policy.Converters.Add(new ToolingValueConverter());
        ITemplateValueFactory factory = new TemplateValueFactory();
        factory.Configure(policy);
        TemplateDocumentSyntax document = new TemplateProfileRegistry().Parse("text", "native").Document;
        List<TemplateValue> list = new List<TemplateValue>();
        list.Add(factory.CreateString("item"));
        Dictionary<string, TemplateValue> map = new Dictionary<string, TemplateValue>();
        map["key"] = factory.CreateNumber(2m);
        object objectValue = new object();
        Func<List<TemplateValue>, TemplateValue> function = (List<TemplateValue> arguments) => factory.CreateNumber(arguments.Count);
        ArrayList enumerable = new ArrayList();
        enumerable.Add("a");
        enumerable.Add(3);
        Hashtable dictionary = new Hashtable();
        dictionary["enabled"] = true;

        TemplateValue missing = factory.CreateMissing();
        TemplateValue nullValue = factory.CreateNull();
        TemplateValue boolean = factory.CreateBoolean(true);
        TemplateValue number = factory.CreateNumber(1.5m);
        TemplateValue text = factory.CreateString("text");
        TemplateValue safeText = factory.CreateSafeString("<b>");
        TemplateValue listValue = factory.CreateList(list);
        TemplateValue mapValue = factory.CreateMap(map);
        TemplateValue wrapped = factory.CreateObject(objectValue);
        TemplateValue callable = factory.CreateFunction(function);
        TemplateValue fragment = factory.CreateFragment(document.Children);
        TemplateValue custom = factory.Convert(new ToolingValue(7m));
        TemplateValue convertedList = factory.ConvertEnumerable(enumerable);
        TemplateValue convertedMap = factory.ConvertDictionary(dictionary);

        Assert.That(missing.Kind, Is.EqualTo(TemplateValueKind.Missing));
        Assert.That(nullValue.Kind, Is.EqualTo(TemplateValueKind.Null));
        Assert.That(boolean.BooleanValue, Is.True);
        Assert.That(number.NumberValue, Is.EqualTo(1.5m));
        Assert.That(text.StringValue, Is.EqualTo("text"));
        Assert.That(safeText.Kind, Is.EqualTo(TemplateValueKind.SafeString));
        Assert.That(listValue.ListValue[0].StringValue, Is.EqualTo("item"));
        Assert.That(mapValue.MapValue["key"].NumberValue, Is.EqualTo(2m));
        Assert.That(wrapped.ObjectValue, Is.SameAs(objectValue));
        Assert.That(callable.FunctionValue!(new List<TemplateValue>()).NumberValue, Is.EqualTo(0m));
        Assert.That(fragment.FragmentValue.Count, Is.EqualTo(1));
        Assert.That(custom.NumberValue, Is.EqualTo(7m));
        Assert.That(convertedList.ListValue[1].NumberValue, Is.EqualTo(3m));
        Assert.That(convertedMap.MapValue["enabled"].BooleanValue, Is.True);
    }

    [Test]
    public void Writer_ExercisesEveryInterfaceMethodAndNeverReportsNegativeCapacity()
    {
        StringWriter destination = new StringWriter(CultureInfo.InvariantCulture);
        destination.NewLine = "\n";
        ITemplateWriter writer = new TemplateWriter(destination);
        TemplateSecurityPolicy policy = new TemplateSecurityPolicy();
        policy.MaximumOutputLength = 8;
        writer.Configure(policy);

        Assert.That(writer.GetRemainingLength(), Is.EqualTo(8));
        Assert.That(writer.CanWrite(8), Is.True);
        writer.Write("a");
        writer.WriteCharacter('b');
        writer.WriteValue(new TemplateValueFactory().CreateString("c"));
        writer.WriteLine();
        writer.WriteTextLine("d");
        writer.Flush();

        Assert.That(destination.ToString(), Is.EqualTo("abc\nd\n"));
        Assert.That(writer.GetRemainingLength(), Is.EqualTo(2));

        TemplateSecurityPolicy smallerPolicy = new TemplateSecurityPolicy();
        smallerPolicy.MaximumOutputLength = 3;
        writer.Configure(smallerPolicy);

        Assert.That(writer.GetRemainingLength(), Is.EqualTo(0));
        Assert.That(writer.CanWrite(0), Is.False);
    }
}

public class ToolingValue
{
    public decimal Amount;

    public ToolingValue(decimal amount)
    {
        Amount = amount;
    }
}

public class ToolingValueConverter : ICustomTemplateValueConverter
{
    public TemplateValue? Convert(object value)
    {
        ToolingValue? toolingValue = value as ToolingValue;
        if (toolingValue == null)
        {
            return null;
        }

        return new TemplateValueFactory().CreateNumber(toolingValue.Amount);
    }
}
