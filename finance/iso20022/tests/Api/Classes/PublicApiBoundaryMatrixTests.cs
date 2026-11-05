using System.Buffers;
using System.Reflection;
using iso20022;

namespace iso20022.Tests;

public class PublicApiBoundaryMatrixTests
{
    [Test]
    public void EveryPublicImplementationHandlesIndependentBoundaryArguments()
    {
        Assembly assembly = typeof(MessageDocument).Assembly;
        int invocationCount = 0;
        foreach (Type implementation in PublicImplementations(assembly))
        {
            foreach (Type contract in ApiContracts(implementation, assembly))
            {
                foreach (MethodInfo method in contract.GetMethods())
                {
                    invocationCount += ExerciseMethod(implementation, contract, method);
                }
            }
        }

        Assert.That(invocationCount, Is.GreaterThan(100));
    }

    private static int ExerciseMethod(
        Type implementation,
        Type contract,
        MethodInfo method)
    {
        int count = 0;
        ParameterInfo[] parameters = method.GetParameters();
        int parameterIndex = 0;
        while (parameterIndex < parameters.Length)
        {
            if (!parameters[parameterIndex].ParameterType.IsValueType)
            {
                InvokeBoundary(
                    implementation,
                    contract,
                    method,
                    parameterIndex);
                count++;
            }
            parameterIndex++;
        }
        return count;
    }

    private static void InvokeBoundary(
        Type implementation,
        Type contract,
        MethodInfo method,
        int changedParameter)
    {
        object instance = CreateImplementation(implementation);
        object?[] arguments = CreateArguments(method.GetParameters());
        arguments[changedParameter] = null;

        try
        {
            object? result = method.Invoke(instance, arguments);
            if (method.ReturnType != typeof(void) &&
                method.ReturnType != typeof(MessageStreamItem) &&
                method.ReturnType != typeof(ProfileDefinition))
            {
                Assert.That(result, Is.Not.Null,
                    implementation.Name + "." + method.Name);
            }
        }
        catch (TargetInvocationException exception)
        {
            if (!IsDocumentedBoundaryException(
                implementation,
                method,
                exception.InnerException))
            {
                throw new AssertionException(
                    implementation.Name + "." + method.Name +
                    " did not handle an independent boundary value: " +
                    (exception.InnerException?.GetType().Name ?? "unknown") +
                    " - " +
                    (exception.InnerException?.Message ?? exception.Message),
                    exception.InnerException ?? exception);
            }
        }
        finally
        {
            IDisposable? disposable = instance as IDisposable;
            disposable?.Dispose();
        }
    }

    private static bool IsDocumentedBoundaryException(
        Type implementation,
        MethodInfo method,
        Exception? exception)
    {
        if (implementation == typeof(MetadataExtractor) &&
            method.Name == nameof(IMetadataExtractor.ExtractMetadata) &&
            exception is ArgumentNullException)
        {
            return true;
        }
        return exception is ArgumentException ||
            exception is FormatException ||
            exception is System.Text.Json.JsonException ||
            exception is System.Xml.XmlException;
    }

    private static object?[] CreateArguments(ParameterInfo[] parameters)
    {
        object?[] arguments = new object?[parameters.Length];
        int index = 0;
        while (index < parameters.Length)
        {
            arguments[index] = CreateValue(parameters[index].ParameterType, 0);
            index++;
        }
        return arguments;
    }

    private static object? CreateValue(Type type, int depth)
    {
        if (type == typeof(string))
        {
            return string.Empty;
        }
        if (type == typeof(byte[]))
        {
            return Array.Empty<byte>();
        }
        if (type == typeof(Stream))
        {
            return new MemoryStream();
        }
        if (type == typeof(TextWriter))
        {
            return new StringWriter();
        }
        if (type == typeof(IBufferWriter<byte>))
        {
            return new ArrayBufferWriter<byte>();
        }
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }
        if (depth > 3 || type.IsInterface || type.IsAbstract)
        {
            return null;
        }

        object? value = Activator.CreateInstance(type);
        if (value == null)
        {
            return null;
        }
        foreach (FieldInfo field in type.GetFields(
            BindingFlags.Public | BindingFlags.Instance))
        {
            object? fieldValue = CreateFieldValue(field.FieldType, depth + 1);
            if (fieldValue != null || !field.FieldType.IsValueType)
            {
                field.SetValue(value, fieldValue);
            }
        }
        return value;
    }

    private static object? CreateFieldValue(Type type, int depth)
    {
        if (type == typeof(int))
        {
            return 1;
        }
        if (type == typeof(long))
        {
            return 1L;
        }
        if (type == typeof(string))
        {
            return string.Empty;
        }
        if (type == typeof(byte[]))
        {
            return Array.Empty<byte>();
        }
        if (type.IsGenericType &&
            type.GetGenericTypeDefinition() == typeof(List<>))
        {
            return Activator.CreateInstance(type);
        }
        return CreateValue(type, depth);
    }

    private static Type[] PublicImplementations(Assembly assembly)
    {
        return assembly.GetExportedTypes()
            .Where(type => type.IsClass && !type.IsAbstract &&
                type.GetInterfaces().Any(contract =>
                    contract.Assembly == assembly && contract.IsPublic))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
    }

    private static Type[] ApiContracts(Type implementation, Assembly assembly)
    {
        return implementation.GetInterfaces()
            .Where(type => type.Assembly == assembly && type.IsPublic)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
    }

    private static object CreateImplementation(Type implementation)
    {
        if (implementation == typeof(CatalogueClient))
        {
            return new CatalogueClient(new System.Net.Http.HttpClient());
        }
        if (implementation == typeof(CodeSetProvider))
        {
            return new CodeSetProvider(
                new List<RepositorySnapshot>(),
                new List<ExternalCodeCatalog>());
        }
        if (implementation == typeof(MessageStreamReader))
        {
            return new MessageStreamReader(null, null);
        }
        if (implementation == typeof(MessageStreamWriter))
        {
            return new MessageStreamWriter(null, null, null);
        }
        if (implementation == typeof(ProfileProvider))
        {
            return new ProfileProvider(new List<ProfileDefinition>());
        }
        if (implementation == typeof(RepositoryQuery))
        {
            return new RepositoryQuery(new RepositorySnapshot());
        }
        if (implementation == typeof(XsdValidator))
        {
            return new XsdValidator(new SchemaRegistry());
        }

        return Activator.CreateInstance(implementation) ??
            throw new AssertionException(
                "Could not instantiate " + implementation.FullName + ".");
    }
}
