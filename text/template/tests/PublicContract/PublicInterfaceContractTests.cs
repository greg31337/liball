using System.Collections;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using template;

namespace template.PublicContract.Tests;

[TestFixture]
public class PublicInterfaceContractTests
{
    string RootPath = "";

    [SetUp]
    public void SetUp()
    {
        RootPath = Path.Combine(Path.GetTempPath(), "template-public-contract-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(RootPath);
        File.WriteAllText(Path.Combine(RootPath, "card.tpl"), "Card {{ name }}");
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(RootPath))
        {
            Directory.Delete(RootPath, true);
        }
    }

    [Test]
    public void EveryPublicInterfaceMethod_CanBeCalledThroughItsDeclaredInterface()
    {
        Type[] interfaces = typeof(TemplateRenderer).Assembly.GetExportedTypes().Where((Type type) => type.IsInterface).OrderBy((Type type) => type.FullName).ToArray();
        int methodCount = 0;

        Assert.That(interfaces, Is.Not.Empty);
        foreach (Type interfaceType in interfaces)
        {
            MethodInfo[] methods = interfaceType.GetMethods().OrderBy((MethodInfo method) => method.Name).ToArray();
            Assert.That(methods, Is.Not.Empty, interfaceType.FullName);
            foreach (MethodInfo method in methods)
            {
                object target = CreateTarget(interfaceType, method.Name);
                Assert.That(interfaceType.IsInstanceOfType(target), Is.True, interfaceType.FullName);
                object? result = method.Invoke(target, CreateArguments(interfaceType, method));
                CompleteResult(result);
                ValidateResult(method, result);
                methodCount = methodCount + 1;
            }
        }

        Assert.That(methodCount, Is.GreaterThan(0));
    }

    object CreateTarget(Type interfaceType, string methodName)
    {
        if (interfaceType == typeof(ITemplateOutputEncoder))
        {
            return new ContractOutputEncoder();
        }

        if (interfaceType == typeof(ITemplateDiagnosticSink))
        {
            TemplateDiagnosticCollectionSink sink = new TemplateDiagnosticCollectionSink();
            if (methodName != "Report")
            {
                sink.Report(CreateDiagnostic());
            }

            return sink;
        }

        if (interfaceType == typeof(ITemplateSourceMapCollector))
        {
            TemplateSourceMapCollector collector = new TemplateSourceMapCollector();
            if (methodName != "Report")
            {
                collector.Report(CreateMapEntry());
            }

            return collector;
        }

        if (interfaceType == typeof(ITemplateLoader))
        {
            MemoryTemplateLoader loader = new MemoryTemplateLoader();
            loader.AddTemplate("card", "Card");
            return loader;
        }

        if (interfaceType == typeof(ITemplateMemberResolver))
        {
            return new TemplateMemberResolver();
        }

        if (interfaceType == typeof(IConfigurableTemplateMemberResolver))
        {
            return new TemplateMemberResolver();
        }

        if (interfaceType == typeof(ICustomTemplateValueConverter))
        {
            return new ContractValueConverter();
        }

        if (interfaceType == typeof(ITemplateMemberNameMapper))
        {
            return new ContractNameMapper();
        }

        if (interfaceType == typeof(ITemplateValueFactory))
        {
            return new TemplateValueFactory();
        }

        if (interfaceType == typeof(ITemplateProfileCatalog))
        {
            return new TemplateProfileCatalog();
        }

        if (interfaceType == typeof(ITemplateProfileRegistry))
        {
            TemplateProfileRegistry registry = new TemplateProfileRegistry();
            TemplateProfile profile = new TemplateProfileCatalog().CreateNativeProfile();
            profile.Name = "contract";
            registry.Register(profile);
            return registry;
        }

        if (interfaceType == typeof(ITemplateDiagnosticFormatter))
        {
            return new TemplateDiagnosticFormatter();
        }

        if (interfaceType == typeof(ITemplateCompatibilityAnalyzer))
        {
            return new TemplateCompatibilityAnalyzer();
        }

        if (interfaceType == typeof(ITemplateCompilationCache))
        {
            TemplateCompilationCache cache = new TemplateCompilationCache();
            cache.GetOrCompile(CreateSource(), new TemplateOptions());
            return cache;
        }

        if (interfaceType == typeof(ITemplateDependencyGraphBuilder))
        {
            TemplateRegistry registry = new TemplateRegistry();
            registry.RegisterTemplate("card", "Card");
            TemplateDependencyGraphBuilder builder = new TemplateDependencyGraphBuilder();
            builder.Configure(new TemplateOptions(), registry);
            return builder;
        }

        if (interfaceType == typeof(ITemplateSyntaxDumper))
        {
            return new TemplateSyntaxDumper();
        }

        if (interfaceType == typeof(ITemplateWhitespaceNormalizer))
        {
            return new TemplateWhitespaceNormalizer();
        }

        if (interfaceType == typeof(ITemplateFormatter))
        {
            return new TemplateFormatter();
        }

        if (interfaceType == typeof(IFileTemplateLoader))
        {
            return new FileTemplateLoader(RootPath);
        }

        if (interfaceType == typeof(IMemoryTemplateLoader))
        {
            MemoryTemplateLoader loader = new MemoryTemplateLoader();
            if (methodName != "AddTemplate" && methodName != "AddTemplateSource")
            {
                loader.AddTemplate("card", "Card");
            }

            return loader;
        }

        if (interfaceType == typeof(IEmbeddedResourceTemplateLoader))
        {
            EmbeddedResourceTemplateLoader loader = new EmbeddedResourceTemplateLoader(typeof(PublicInterfaceContractTests).Assembly);
            TemplateLoaderOptions options = new TemplateLoaderOptions();
            options.ResourcePrefix = "PublicContract.Resources.";
            loader.Configure(options);
            return loader;
        }

        if (interfaceType == typeof(ITemplateLexer))
        {
            return new TemplateLexer(CreateSource());
        }

        if (interfaceType == typeof(ITemplateParser))
        {
            ITemplateLexer lexer = new TemplateLexer(CreateSource());
            return new TemplateParser(lexer.Lex());
        }

        if (interfaceType == typeof(ITemplateRegistry))
        {
            TemplateRegistry registry = new TemplateRegistry();
            registry.RegisterPartial("card", "Card");
            registry.RegisterTemplate("card", "Card");
            return registry;
        }

        if (interfaceType == typeof(ITemplateRenderer))
        {
            TemplateRenderer renderer = new TemplateRenderer();
            renderer.Configure(new TemplateOptions(), new TemplateRegistry(), new TemplateMemberResolver());
            return renderer;
        }

        if (interfaceType == typeof(ITemplateLinter))
        {
            TemplateLinter linter = new TemplateLinter();
            linter.Configure(new TemplateOptions(), new TemplateRegistry());
            return linter;
        }

        if (interfaceType == typeof(ITemplateSecurityAuditor))
        {
            return new TemplateSecurityAuditor();
        }

        if (interfaceType == typeof(ITemplateSourceLineMap))
        {
            return new TemplateSourceLineMap(CreateSource());
        }

        if (interfaceType == typeof(ITemplateWriter))
        {
            return new TemplateWriter(new StringWriter());
        }

        throw new AssertionException("No public contract target exists for " + interfaceType.FullName + ".");
    }

    object?[] CreateArguments(Type interfaceType, MethodInfo method)
    {
        ParameterInfo[] parameters = method.GetParameters();
        object?[] arguments = new object?[parameters.Length];
        int index = 0;
        while (index < parameters.Length)
        {
            arguments[index] = CreateArgument(interfaceType, method.Name, parameters[index]);
            index = index + 1;
        }

        return arguments;
    }

    object? CreateArgument(Type interfaceType, string methodName, ParameterInfo parameter)
    {
        Type type = parameter.ParameterType;
        string name = parameter.Name ?? "";
        TemplateValueFactory factory = new TemplateValueFactory();

        if (type == typeof(string))
        {
            if (name == "sourceProfileName" || name == "profileName")
            {
                return "native";
            }

            if (name == "targetProfileName")
            {
                return "mustache";
            }

            if (name == "templateText")
            {
                return "Hello {{ name }}";
            }

            if (name == "version")
            {
                return "v1";
            }

            if (name == "contentHash")
            {
                return "hash";
            }

            if (name == "timestamp")
            {
                return "time";
            }

            if (name == "memberName")
            {
                return "name";
            }

            if (interfaceType == typeof(ITemplateProfileRegistry) && name == "name")
            {
                return methodName == "Register" ? "contract" : "contract";
            }

            if ((interfaceType == typeof(IFileTemplateLoader) || interfaceType == typeof(IEmbeddedResourceTemplateLoader)) && name == "name")
            {
                return "card.tpl";
            }

            return "card";
        }

        if (type == typeof(int))
        {
            if (name == "capacity")
            {
                return 2;
            }

            if (name == "lineNumber" || name == "columnNumber")
            {
                return 1;
            }

            return 0;
        }

        if (type == typeof(bool))
        {
            return true;
        }

        if (type == typeof(decimal))
        {
            return 1m;
        }

        if (type == typeof(char))
        {
            return 'x';
        }

        if (type == typeof(CancellationToken))
        {
            return CancellationToken.None;
        }

        if (type == typeof(TemplateValue))
        {
            if (name == "index")
            {
                return factory.CreateString("name");
            }

            return CreateModel();
        }

        if (type == typeof(TemplateValueConversionPolicy))
        {
            return new TemplateValueConversionPolicy();
        }

        if (type == typeof(TemplateProfile))
        {
            TemplateProfile profile = new TemplateProfileCatalog().CreateNativeProfile();
            profile.Name = "contract";
            return profile;
        }

        if (type == typeof(TemplateProfileRegistry))
        {
            return new TemplateProfileRegistry();
        }

        if (type == typeof(TemplateOptions))
        {
            return new TemplateOptions();
        }

        if (type == typeof(TemplateFormatterOptions))
        {
            return new TemplateFormatterOptions();
        }

        if (type == typeof(TemplateLoaderOptions))
        {
            TemplateLoaderOptions options = new TemplateLoaderOptions();
            if (interfaceType == typeof(IEmbeddedResourceTemplateLoader))
            {
                options.ResourcePrefix = "PublicContract.Resources.";
            }

            return options;
        }

        if (type == typeof(TemplateSecurityPolicy))
        {
            TemplateSecurityPolicy policy = new TemplateSecurityPolicy();
            if (interfaceType == typeof(IFileTemplateLoader))
            {
                policy.AllowedLoaderRoots.Add(RootPath);
            }

            return policy;
        }

        if (type == typeof(TemplateMemberResolverOptions))
        {
            return new TemplateMemberResolverOptions();
        }

        if (type == typeof(TemplateRegistry))
        {
            return new TemplateRegistry();
        }

        if (type == typeof(ITemplateMemberResolver))
        {
            return new TemplateMemberResolver();
        }

        if (type == typeof(ITemplateCompilationCache))
        {
            return new TemplateCompilationCache();
        }

        if (type == typeof(ITemplateDiagnosticSink))
        {
            return new TemplateDiagnosticCollectionSink();
        }

        if (type == typeof(ITemplateSourceMapCollector))
        {
            return new TemplateSourceMapCollector();
        }

        if (type == typeof(ITemplateLoader))
        {
            MemoryTemplateLoader loader = new MemoryTemplateLoader();
            loader.AddTemplate("card", "Card");
            return loader;
        }

        if (type == typeof(MemoryTemplateLoader))
        {
            return new MemoryTemplateLoader();
        }

        if (type == typeof(FileTemplateLoader))
        {
            return new FileTemplateLoader(RootPath);
        }

        if (type == typeof(TemplateSource))
        {
            return CreateSource();
        }

        if (type == typeof(TemplateDocumentSyntax))
        {
            return CreateDocument();
        }

        if (type == typeof(TemplateRenderContext))
        {
            return new TemplateRenderContext(CreateModel(), new StringWriter(), new TemplateOptions(), new TemplateRegistry(), new TemplateMemberResolver());
        }

        if (type == typeof(TemplateSyntaxNode))
        {
            return CreateDocument().Children[0];
        }

        if (type == typeof(TemplateExpressionSyntax))
        {
            return new TemplateProfileRegistry().Parse("{{ name }}", "native").Document.Children[0].Output!.Expression;
        }

        if (type == typeof(TemplateDiagnostic))
        {
            return CreateDiagnostic();
        }

        if (type == typeof(TemplateSourceMapEntry))
        {
            return CreateMapEntry();
        }

        if (type == typeof(TextWriter))
        {
            return new StringWriter();
        }

        if (type == typeof(IEnumerable))
        {
            return new ArrayList { "a", 1 };
        }

        if (type == typeof(IDictionary))
        {
            return new Hashtable { ["name"] = "Ada" };
        }

        if (type == typeof(List<TemplateValue>))
        {
            return new List<TemplateValue> { factory.CreateString("value") };
        }

        if (type == typeof(Dictionary<string, TemplateValue>))
        {
            return new Dictionary<string, TemplateValue> { ["name"] = factory.CreateString("Ada") };
        }

        if (type == typeof(List<TemplateSyntaxNode>))
        {
            return CreateDocument().Children;
        }

        if (type == typeof(Func<List<TemplateValue>, TemplateValue>))
        {
            return (List<TemplateValue> values) => values.Count == 0 ? factory.CreateNull() : values[0];
        }

        if (type == typeof(Func<List<TemplateValue>, CancellationToken, Task<TemplateValue>>))
        {
            return (List<TemplateValue> values, CancellationToken cancellationToken) => Task.FromResult(values.Count == 0 ? factory.CreateNull() : values[0]);
        }

        if (type == typeof(Func<TemplateBlockHelperContext, TemplateValue>))
        {
            return (TemplateBlockHelperContext context) => factory.CreateString("block");
        }

        if (type == typeof(object))
        {
            return new ContractValue("converted");
        }

        if (type.IsEnum)
        {
            return Enum.GetValues(type).GetValue(0);
        }

        throw new AssertionException("No argument exists for " + interfaceType.Name + "." + methodName + "(" + type.Name + " " + name + ").");
    }

    static void CompleteResult(object? result)
    {
        Task? task = result as Task;
        if (task != null)
        {
            task.GetAwaiter().GetResult();
        }
    }

    static void ValidateResult(MethodInfo method, object? result)
    {
        if (method.ReturnType == typeof(void))
        {
            Assert.That(result, Is.Null, method.Name);
            return;
        }

        if (typeof(Task).IsAssignableFrom(method.ReturnType))
        {
            Assert.That(result, Is.InstanceOf<Task>(), method.Name);
            return;
        }

        if (method.ReturnType.IsValueType)
        {
            Assert.That(result, Is.Not.Null, method.Name);
            return;
        }

        if (method.Name == "GetMappingAt" || method.Name == "Convert" || method.Name == "MapName")
        {
            return;
        }

        Assert.That(result, Is.Not.Null, method.Name);
    }

    static TemplateSource CreateSource()
    {
        TemplateSource source = new TemplateSource("contract", null, "v1", "hash", "Hello {{ name }}");
        source.Timestamp = "time";
        return source;
    }

    static TemplateDocumentSyntax CreateDocument()
    {
        return new TemplateProfileRegistry().Parse("Hello {{ name }}", "native").Document;
    }

    static TemplateValue CreateModel()
    {
        TemplateValueFactory factory = new TemplateValueFactory();
        return factory.CreateMap(new Dictionary<string, TemplateValue> { ["name"] = factory.CreateString("Ada") });
    }

    static TemplateDiagnostic CreateDiagnostic()
    {
        TemplateSource source = CreateSource();
        return new TemplateDiagnostic("CONTRACT", TemplateDiagnosticSeverity.Information, "contract", new TemplateSourceSpan(source, 0, 5), null);
    }

    static TemplateSourceMapEntry CreateMapEntry()
    {
        TemplateSource source = CreateSource();
        return new TemplateSourceMapEntry(0, 1, new TemplateSourceSpan(source, 0, 1));
    }
}

public class ContractOutputEncoder : ITemplateOutputEncoder
{
    public string Encode(string text, TemplateEscapingMode mode)
    {
        return "[" + mode.ToString() + "]" + text;
    }
}

public class ContractValue
{
    public string Text;

    public ContractValue(string text)
    {
        Text = text;
    }
}

public class ContractValueConverter : ICustomTemplateValueConverter
{
    public TemplateValue? Convert(object value)
    {
        ContractValue? contract = value as ContractValue;
        if (contract == null)
        {
            return null;
        }

        return new TemplateValueFactory().CreateString(contract.Text);
    }
}

public class ContractNameMapper : ITemplateMemberNameMapper
{
    public string? MapName(string memberName)
    {
        if (memberName == "name")
        {
            return "display_name";
        }

        return null;
    }
}
