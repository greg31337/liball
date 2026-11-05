using System.Reflection;
using System.Runtime.CompilerServices;
using markdown;

namespace markdown.ConsumerContractTests;

[TestFixture]
public class PublicApiContractTests
{
    [Test]
    public void ExportedTypes_ConstructorsFieldsEnumsAndInterfaceMethods_AreUsableByAConsumer()
    {
        Type[] exportedTypes = typeof(Markdown).Assembly.GetExportedTypes();
        int constructorCount = 0;
        int fieldCount = 0;
        int enumValueCount = 0;
        int interfaceMethodCount = 0;

        foreach (Type type in exportedTypes)
        {
            Assert.That(type.Namespace, Is.EqualTo("markdown"), type.FullName);
            if (type.IsEnum)
            {
                Array values = Enum.GetValues(type);
                string[] names = Enum.GetNames(type);
                Assert.That(values.Length, Is.EqualTo(names.Length), type.FullName);
                foreach (object value in values)
                {
                    Assert.That(Enum.IsDefined(type, value), Is.True, type.FullName + "." + value.ToString());
                    enumValueCount = enumValueCount + 1;
                }

                continue;
            }

            if (type.IsInterface)
            {
                object proxy = DispatchProxy.Create(type, typeof(ContractDispatchProxy));
                ContractDispatchProxy recorder = (ContractDispatchProxy)proxy;
                foreach (MethodInfo method in type.GetMethods())
                {
                    int before = recorder.InvocationCount;
                    object? result = method.Invoke(proxy, ContractValues.CreateArguments(method.GetParameters()));
                    Assert.That(recorder.InvocationCount, Is.EqualTo(before + 1), type.FullName + "." + method.Name);
                    if (method.ReturnType != typeof(void) && method.ReturnType.IsValueType)
                    {
                        Assert.That(result, Is.Not.Null, type.FullName + "." + method.Name);
                    }

                    interfaceMethodCount = interfaceMethodCount + 1;
                }

                continue;
            }

            object? instance = null;
            ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            foreach (ConstructorInfo constructor in constructors)
            {
                object created = constructor.Invoke(ContractValues.CreateArguments(constructor.GetParameters()));
                Assert.That(created.GetType(), Is.EqualTo(type), constructor.ToString());
                instance = created;
                constructorCount = constructorCount + 1;
            }

            if (!type.IsAbstract && instance == null)
            {
                instance = RuntimeHelpers.GetUninitializedObject(type);
            }

            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                object? owner = field.IsStatic ? null : instance;
                object? original = field.GetValue(owner);
                if (!field.IsInitOnly && !field.IsLiteral)
                {
                    object? replacement = ContractValues.Create(field.FieldType, field.Name);
                    field.SetValue(owner, replacement);
                    Assert.That(field.GetValue(owner), Is.EqualTo(replacement), type.FullName + "." + field.Name);
                    field.SetValue(owner, original);
                }
                else
                {
                    Assert.That(() => field.GetValue(owner), Throws.Nothing, type.FullName + "." + field.Name);
                }

                fieldCount = fieldCount + 1;
            }
        }

        Assert.That(exportedTypes.Length, Is.GreaterThan(60));
        Assert.That(constructorCount, Is.GreaterThan(50));
        Assert.That(fieldCount, Is.GreaterThan(250));
        Assert.That(enumValueCount, Is.GreaterThan(70));
        Assert.That(interfaceMethodCount, Is.GreaterThan(200));
    }

    [Test]
    public void FacadeInterfaces_InvokeEveryOperationWithPublicConsumerValues()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices services = markdown.GetDataServices();
        MarkdownPipeline pipeline = markdown.CreateGfmPipeline();
        MarkdownDocument document = markdown.ParseSourceWithPipeline("# Title\n\ntext ~~old~~", pipeline);
        MarkdownEvent[] events = services.ReadDocumentEvents(document);
        MarkdownNode firstBlock = services.CreateNodeOperations(document.Root).GetChildAt(0);
        MarkdownNode block = services.CreateTreeOperations().CloneNode(firstBlock);

        InvokeFacade(typeof(IMarkdown), markdown, pipeline, document, events, block);
        InvokeFacade(typeof(IMarkdownDataServices), services, pipeline, document, events, block);
    }

    [Test]
    public void ParserRendererAndEditorProducedTypes_AreAvailableWithoutInternalAccess()
    {
        IMarkdown markdown = new Markdown();
        CaptureBlockRule blockRule = new CaptureBlockRule();
        CaptureInlineRule inlineRule = new CaptureInlineRule();
        CaptureHtmlRule htmlRule = new CaptureHtmlRule(markdown);
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder()
            .AddBlockRule(blockRule)
            .AddInlineRule(inlineRule)
            .AddHtmlRenderRule(htmlRule)
            .Build();

        MarkdownDocument capturedDocument = markdown.ParseSourceWithPipeline("capture @title", pipeline);
        MarkdownDocument document = markdown.ParseSourceWithPipeline("# @title", pipeline);
        string html = markdown.RenderHtmlDocumentWithPipeline(document, pipeline);
        MarkdownIncrementalParseResult incremental = markdown.GetDataServices()
            .CreateEditorOperations()
            .ReparseDocument(document, new MarkdownTextChange(2, 1, "updated"));

        Assert.That(blockRule.Context, Is.TypeOf<MarkdownBlockParseContext>());
        Assert.That(inlineRule.Context, Is.TypeOf<MarkdownInlineParseContext>());
        Assert.That(htmlRule.Context, Is.TypeOf<HtmlRenderContext>());
        Assert.That(incremental, Is.TypeOf<MarkdownIncrementalParseResult>());
        Assert.That(capturedDocument.Root, Is.Not.Null);
        Assert.That(html, Does.Contain("@title"));
        Assert.That(incremental.Document, Is.SameAs(document));
    }

    [Test]
    public void PublicOperations_ReportNullAndInvalidStateFailures()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices services = markdown.GetDataServices();
        IMarkdownEventWriter eventWriter = markdown.CreateEventWriter();
        MarkdownPipeline pipeline = markdown.CreateCommonMarkPipeline();
        MarkdownEvent openDocument = new MarkdownEvent();
        openDocument.EventKind = MarkdownEventKind.EnterNode;
        openDocument.NodeKind = MarkdownNodeKind.Document;
        openDocument.Depth = 0;
        openDocument.ChildIndex = -1;
        eventWriter.Write(openDocument);

        Assert.That(() => markdown.ParseSource(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.RenderHtmlSourceWithPipeline("", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => services.CreateDocumentOperations(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => services.CreateNodeOperations(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => services.CreateEditorOperations().ParsePartialSource("text", 5, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => services.CreateSafetyOperations().EvaluateRawHtml("<b>x</b>", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => eventWriter.FinishDocument(), Throws.TypeOf<InvalidOperationException>());
        Assert.That(() => markdown.CreatePipelineBuilder().SetMaximumInputSize(0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => markdown.CreateDocumentFromEvents(new[] { openDocument }), Throws.TypeOf<InvalidOperationException>());
        Assert.That(pipeline, Is.Not.Null);
    }

    [Test]
    public void ReturnedSnapshotsAndClones_AreDetachedFromTheirOwners()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices services = markdown.GetDataServices();
        IMarkdownPipelineBuilder builder = markdown.CreatePipelineBuilder();
        MarkdownOptions firstOptions = builder.GetOptions();
        firstOptions.LineEnding = "\r\n";
        MarkdownOptions secondOptions = builder.GetOptions();
        MarkdownDocument document = markdown.ParseSource("# Title");
        IMarkdownDocumentOperations documentOperations = services.CreateDocumentOperations(document);
        MarkdownEvent[] firstEvents = services.ReadDocumentEvents(document);
        MarkdownSourceMapEntry[] firstMap = documentOperations.GetSourceMapEntries();
        MarkdownDocument clone = services.CreateTreeOperations().CloneDocument(document);

        firstEvents[0].NodeKind = MarkdownNodeKind.Unknown;
        firstMap[0].Span.Start.Offset = 999;
        services.CreateNodeOperations(clone.Root).GetChildAt(0).Literal = "Changed";

        Assert.That(secondOptions.LineEnding, Is.EqualTo("\n"));
        Assert.That(services.ReadDocumentEvents(document)[0].NodeKind, Is.EqualTo(MarkdownNodeKind.Document));
        Assert.That(documentOperations.GetSourceMapEntries()[0].Span.Start.Offset, Is.Not.EqualTo(999));
        Assert.That(services.CreateNodeOperations(document.Root).GetChildAt(0).Literal, Is.EqualTo("Title"));
    }

    static void InvokeFacade(
        Type contract,
        object target,
        MarkdownPipeline pipeline,
        MarkdownDocument document,
        MarkdownEvent[] events,
        MarkdownNode block)
    {
        foreach (MethodInfo method in contract.GetMethods())
        {
            if (method.Name == "RenderHtmlChildren" || method.Name == "WriteHtmlLineEnding")
            {
                continue;
            }

            object?[] arguments = CreateFacadeArguments(method, pipeline, document, events, block);
            Assert.That(
                () => method.Invoke(target, arguments),
                Throws.Nothing,
                contract.Name + "." + method.Name);
        }
    }

    static object?[] CreateFacadeArguments(
        MethodInfo method,
        MarkdownPipeline pipeline,
        MarkdownDocument document,
        MarkdownEvent[] events,
        MarkdownNode block)
    {
        ParameterInfo[] parameters = method.GetParameters();
        object?[] arguments = new object?[parameters.Length];
        int index = 0;
        while (index < parameters.Length)
        {
            Type type = parameters[index].ParameterType;
            if (type == typeof(string))
            {
                if (method.Name == "ClassifyUri")
                {
                    arguments[index] = "https://example.test/path";
                }
                else if (parameters[index].Name == "extensionName")
                {
                    arguments[index] = "contract-extension";
                }
                else if (parameters[index].Name == "name")
                {
                    arguments[index] = "contract.rule";
                }
                else
                {
                    arguments[index] = "# Title\n\ntext";
                }
            }
            else if (type == typeof(MarkdownPipeline))
            {
                arguments[index] = pipeline;
            }
            else if (type == typeof(MarkdownOptions))
            {
                arguments[index] = new MarkdownOptions();
            }
            else if (type == typeof(MarkdownAnalysisOptions))
            {
                arguments[index] = new MarkdownAnalysisOptions();
            }
            else if (type == typeof(MarkdownDocument))
            {
                arguments[index] = document;
            }
            else if (type == typeof(MarkdownNode))
            {
                MarkdownNode node = block;
                if (method.Name == "CreateInlineNode")
                {
                    node = new MarkdownNode();
                    node.Kind = MarkdownNodeKind.Text;
                }
                else if (method.Name == "CreateExtensionNode")
                {
                    node = new MarkdownNode();
                    node.Kind = MarkdownNodeKind.ExtensionBlock;
                }

                arguments[index] = node;
            }
            else if (type == typeof(MarkdownEvent[]))
            {
                arguments[index] = events;
            }
            else if (type == typeof(MarkdownNode[]))
            {
                arguments[index] = new[] { block };
            }
            else if (type == typeof(TextReader))
            {
                arguments[index] = new StringReader("# Title\n\ntext");
            }
            else if (type == typeof(TextWriter))
            {
                arguments[index] = new StringWriter();
            }
            else if (type == typeof(ReadOnlyMemory<char>))
            {
                arguments[index] = "# Title\n\ntext".AsMemory();
            }
            else if (type == typeof(object))
            {
                arguments[index] = new object();
            }
            else
            {
                arguments[index] = ContractValues.Create(type, parameters[index].Name ?? "value");
            }

            index = index + 1;
        }

        return arguments;
    }
}

public class ContractDispatchProxy : DispatchProxy
{
    public int InvocationCount;

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        InvocationCount = InvocationCount + 1;
        if (targetMethod == null || targetMethod.ReturnType == typeof(void))
        {
            return null;
        }

        return ContractValues.Create(targetMethod.ReturnType, targetMethod.Name);
    }
}

public static class ContractValues
{
    public static object?[] CreateArguments(ParameterInfo[] parameters)
    {
        object?[] arguments = new object?[parameters.Length];
        int index = 0;
        while (index < parameters.Length)
        {
            arguments[index] = Create(parameters[index].ParameterType, parameters[index].Name ?? "value");
            index = index + 1;
        }

        return arguments;
    }

    public static object? Create(Type type, string name)
    {
        if (type == typeof(void))
        {
            return null;
        }

        if (type == typeof(string))
        {
            return "contract-" + name;
        }

        if (type == typeof(bool))
        {
            return true;
        }

        if (type == typeof(int))
        {
            return 1;
        }

        if (type == typeof(char))
        {
            return 'x';
        }

        if (type == typeof(object))
        {
            return new object();
        }

        if (type == typeof(TextWriter))
        {
            return new StringWriter();
        }

        if (type == typeof(TextReader))
        {
            return new StringReader("contract");
        }

        if (type == typeof(ReadOnlyMemory<char>))
        {
            return "contract".AsMemory();
        }

        if (type.IsEnum)
        {
            return Enum.GetValues(type).GetValue(0);
        }

        if (type.IsArray)
        {
            return Array.CreateInstance(type.GetElementType()!, 0);
        }

        if (typeof(Delegate).IsAssignableFrom(type))
        {
            return null;
        }

        if (type.IsInterface)
        {
            return DispatchProxy.Create(type, typeof(ContractDispatchProxy));
        }

        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }

        ConstructorInfo? constructor = type
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(item => item.GetParameters().Length)
            .FirstOrDefault();
        if (constructor != null)
        {
            return constructor.Invoke(CreateArguments(constructor.GetParameters()));
        }

        return RuntimeHelpers.GetUninitializedObject(type);
    }
}

public class CaptureBlockRule : IMarkdownBlockRule
{
    public MarkdownBlockParseContext? Context;

    public string GetName()
    {
        return "contract.capture-block";
    }

    public bool TryParse(MarkdownBlockParseContext context)
    {
        Context = context;
        return false;
    }
}

public class CaptureInlineRule : IMarkdownInlineRule
{
    public MarkdownInlineParseContext? Context;

    public string GetName()
    {
        return "contract.capture-inline";
    }

    public string GetTriggerCharacters()
    {
        return "@";
    }

    public bool TryParse(MarkdownInlineParseContext context)
    {
        Context = context;
        return false;
    }
}

public class CaptureHtmlRule : IHtmlRenderRule
{
    public IMarkdown Markdown;
    public HtmlRenderContext? Context;

    public CaptureHtmlRule(IMarkdown markdown)
    {
        Markdown = markdown;
    }

    public string GetName()
    {
        return "contract.capture-html";
    }

    public bool TryRender(HtmlRenderContext context, MarkdownNode node)
    {
        Context = context;
        if (node.Kind != MarkdownNodeKind.Heading)
        {
            return false;
        }

        Markdown.RenderHtmlChildren(context, node);
        Markdown.WriteHtmlLineEnding(context);
        return true;
    }
}
