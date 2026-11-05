using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using template;

namespace template.PublicContract.Tests;

[TestFixture]
public class PublicApiShapeContractTests
{
    [Test]
    public void EveryPublicClassAndConstructor_CanBeCreatedWithoutFriendAccess()
    {
        Assembly apiAssembly = typeof(TemplateRenderer).Assembly;
        Assembly consumerAssembly = typeof(PublicApiShapeContractTests).Assembly;
        List<string> friends = apiAssembly.GetCustomAttributes<InternalsVisibleToAttribute>().Select((InternalsVisibleToAttribute attribute) => attribute.AssemblyName).ToList();
        Type[] classes = apiAssembly.GetExportedTypes().Where((Type type) => type.IsClass).OrderBy((Type type) => type.FullName).ToArray();
        int constructorCount = 0;

        Assert.That(friends, Does.Not.Contain(consumerAssembly.GetName().Name));
        Assert.That(classes, Is.Not.Empty);

        foreach (Type type in classes)
        {
            ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
            Assert.That(constructors, Is.Not.Empty, type.FullName + " must be publicly constructible.");
            foreach (ConstructorInfo constructor in constructors)
            {
                object? instance = constructor.Invoke(CreateArguments(constructor.GetParameters()));
                Assert.That(instance, Is.InstanceOf(type), constructor.ToString());
                constructorCount = constructorCount + 1;
            }
        }

        Assert.That(constructorCount, Is.EqualTo(classes.Length));
    }

    [Test]
    public void EveryPublicEnumValue_RoundTripsByItsDeclaredName()
    {
        Type[] enumTypes = typeof(TemplateRenderer).Assembly.GetExportedTypes().Where((Type type) => type.IsEnum).OrderBy((Type type) => type.FullName).ToArray();
        int valueCount = 0;

        Assert.That(enumTypes, Is.Not.Empty);
        foreach (Type enumType in enumTypes)
        {
            Array values = Enum.GetValues(enumType);
            Assert.That(values.Length, Is.GreaterThan(0), enumType.FullName);
            foreach (object value in values)
            {
                string? name = Enum.GetName(enumType, value);
                Assert.That(name, Is.Not.Null.And.Not.Empty, enumType.FullName);
                Assert.That(Enum.Parse(enumType, name!), Is.EqualTo(value), enumType.FullName + "." + name);
                valueCount = valueCount + 1;
            }
        }

        Assert.That(valueCount, Is.GreaterThan(enumTypes.Length));
    }

    [Test]
    public void EveryPublicDataField_CanParticipateInAConsumerScenario()
    {
        Type[] dataTypes = typeof(TemplateRenderer).Assembly.GetExportedTypes()
            .Where((Type type) => type.IsClass && type.GetFields(BindingFlags.Instance | BindingFlags.Public).Length > 0)
            .OrderBy((Type type) => type.FullName)
            .ToArray();
        int fieldCount = 0;

        foreach (Type type in dataTypes)
        {
            ConstructorInfo constructor = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public).Single();
            object instance = constructor.Invoke(CreateArguments(constructor.GetParameters()));
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (FieldInfo field in fields)
            {
                object? original = field.GetValue(instance);
                object? sample = CreateFieldValue(field.FieldType, original);
                field.SetValue(instance, sample);
                Assert.That(field.GetValue(instance), Is.EqualTo(sample), type.FullName + "." + field.Name);
                fieldCount = fieldCount + 1;
            }
        }

        Assert.That(fieldCount, Is.GreaterThan(0));

        TemplateOptions options = new TemplateOptions();
        options.AutoEscape = false;
        TemplateProfile profile = new TemplateProfileCatalog().CreateNativeProfile();
        profile.AutoEscape = options.AutoEscape;
        TemplateValue model = new TemplateValueFactory().CreateMap(new Dictionary<string, TemplateValue>
        {
            ["name"] = new TemplateValueFactory().CreateString("Ada")
        });
        ITemplateRenderer renderer = new TemplateRenderer();
        renderer.Configure(options, new TemplateRegistry(), new TemplateMemberResolver());
        Assert.That(renderer.RenderToString("Hello {{ name }}", model), Is.EqualTo("Hello Ada"));
        Assert.That(profile.AutoEscape, Is.False);
    }

    [Test]
    public void DependencyGraphDataContracts_CanBeCreatedByARegularConsumer()
    {
        TemplateSource source = new TemplateSource("root", null, null, null, "{% include \"card\" %}");
        TemplateSourceSpan span = new TemplateSourceSpan(source, 11, 6);
        TemplateDependencyGraphNode root = new TemplateDependencyGraphNode("root:root", TemplateDependencyGraphNodeKind.Root, "root", source);
        TemplateDependencyGraphNode child = new TemplateDependencyGraphNode("template:card", TemplateDependencyGraphNodeKind.Template, "card", null);
        TemplateDependencyGraphEdge edge = new TemplateDependencyGraphEdge(root.Id, child.Id, TemplateDependencyKind.Template, child.Name, false, span);

        Assert.That(root.Source, Is.SameAs(source));
        Assert.That(child.Source, Is.Null);
        Assert.That(edge.FromId, Is.EqualTo(root.Id));
        Assert.That(edge.ToId, Is.EqualTo(child.Id));
        Assert.That(edge.Span, Is.SameAs(span));
        Assert.That(edge.Resolved, Is.False);
    }

    static object?[] CreateArguments(ParameterInfo[] parameters)
    {
        object?[] arguments = new object?[parameters.Length];
        int index = 0;
        while (index < parameters.Length)
        {
            arguments[index] = CreateValue(parameters[index].ParameterType, parameters[index].Name ?? "");
            index = index + 1;
        }

        return arguments;
    }

    static object? CreateFieldValue(Type type, object? original)
    {
        if (type == typeof(string))
        {
            return original as string == "contract" ? "updated" : "contract";
        }

        if (type == typeof(int))
        {
            int value = original is int number ? number : 0;
            return value == int.MaxValue ? 1 : value + 1;
        }

        if (type == typeof(bool))
        {
            return !(original is bool value && value);
        }

        if (type == typeof(decimal))
        {
            return 2m;
        }

        if (type.IsEnum)
        {
            Array values = Enum.GetValues(type);
            return values.GetValue(values.Length - 1);
        }

        if (original != null)
        {
            return original;
        }

        return CreateValue(type, "field");
    }

    static object? CreateValue(Type type, string parameterName)
    {
        TemplateSource source = new TemplateSource("contract", null, "v1", "hash", new string('x', 64));
        TemplateSourceSpan span = new TemplateSourceSpan(source, 1, 1);
        TemplateValueFactory factory = new TemplateValueFactory();

        if (type == typeof(string))
        {
            if (parameterName == "rootPath")
            {
                return Path.GetTempPath();
            }

            return "contract";
        }

        if (type == typeof(int))
        {
            return 1;
        }

        if (type == typeof(bool))
        {
            return false;
        }

        if (type == typeof(decimal))
        {
            return 1m;
        }

        if (type == typeof(char))
        {
            return 'x';
        }

        if (type.IsEnum)
        {
            return Enum.GetValues(type).GetValue(0);
        }

        if (type == typeof(TemplateSource))
        {
            return source;
        }

        if (type == typeof(TemplateSourceSpan))
        {
            return span;
        }

        if (type == typeof(TemplateDiagnostic))
        {
            return new TemplateDiagnostic("CONTRACT", TemplateDiagnosticSeverity.Information, "contract", span, null);
        }

        if (type == typeof(TemplateDocumentSyntax))
        {
            return new TemplateProfileRegistry().Parse("contract", "native").Document;
        }

        if (type == typeof(TemplateExpressionSyntax))
        {
            return new TemplateExpressionSyntax(span, TemplateExpressionSyntaxKind.Variable, "value");
        }

        if (type == typeof(TemplateStatementSyntax))
        {
            return new TemplateStatementSyntax(span, "set");
        }

        if (type == typeof(TemplateParseResult))
        {
            return new TemplateProfileRegistry().Parse("contract", "native");
        }

        if (type == typeof(TemplateLexerResult))
        {
            ITemplateLexer lexer = new TemplateLexer(source);
            return lexer.Lex();
        }

        if (type == typeof(TemplateValue))
        {
            return factory.CreateString("contract");
        }

        if (type == typeof(TemplateOptions))
        {
            return new TemplateOptions();
        }

        if (type == typeof(TemplateRegistry))
        {
            return new TemplateRegistry();
        }

        if (type == typeof(ITemplateMemberResolver))
        {
            return new TemplateMemberResolver();
        }

        if (type == typeof(TextWriter))
        {
            return new StringWriter();
        }

        if (type == typeof(Assembly))
        {
            return typeof(PublicApiShapeContractTests).Assembly;
        }

        if (type.IsArray)
        {
            return Array.CreateInstance(type.GetElementType()!, 0);
        }

        if (type.IsGenericType)
        {
            Type definition = type.GetGenericTypeDefinition();
            if (definition == typeof(List<>) || definition == typeof(Dictionary<,>))
            {
                return Activator.CreateInstance(type);
            }
        }

        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }

        return null;
    }
}
