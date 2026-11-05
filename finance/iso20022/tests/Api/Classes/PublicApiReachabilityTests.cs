using System.Reflection;
using iso20022;

namespace iso20022.Tests;

public class PublicApiReachabilityTests
{
    [Test]
    public void EveryPublicDataClassCanBeInstantiated()
    {
        Assembly assembly = typeof(MessageDocument).Assembly;
        Type[] implementations = PublicImplementations(assembly);
        Type[] dataClasses = assembly.GetExportedTypes()
            .Where(type => type.IsClass && !type.IsAbstract &&
                !typeof(Exception).IsAssignableFrom(type) &&
                !implementations.Contains(type))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        Assert.That(dataClasses, Is.Not.Empty);
        foreach (Type dataClass in dataClasses)
        {
            object? instance = Activator.CreateInstance(dataClass);
            Assert.That(instance, Is.Not.Null, dataClass.FullName);
            Assert.That(instance?.GetType(), Is.EqualTo(dataClass));
        }
    }

    [Test]
    public void EveryPublicEnumValueStaticFieldAndExceptionIsReachable()
    {
        Assembly assembly = typeof(MessageDocument).Assembly;
        Type[] enums = assembly.GetExportedTypes()
            .Where(type => type.IsEnum)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
        Assert.That(enums, Is.Not.Empty);
        foreach (Type enumType in enums)
        {
            Array values = Enum.GetValues(enumType);
            Assert.That(values.Length, Is.GreaterThan(0), enumType.FullName);
            foreach (object value in values)
            {
                object boxed = Enum.ToObject(enumType, Convert.ToInt32(value));
                Assert.That(boxed.GetType(), Is.EqualTo(enumType));
                Assert.That(Enum.IsDefined(enumType, boxed), Is.True);
            }
        }

        Type[] staticClasses = assembly.GetExportedTypes()
            .Where(type => type.IsClass && type.IsAbstract && type.IsSealed)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
        Assert.That(staticClasses, Is.Not.Empty);
        foreach (Type staticClass in staticClasses)
        {
            FieldInfo[] fields = staticClass.GetFields(
                BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            Assert.That(fields, Is.Not.Empty, staticClass.FullName);
            foreach (FieldInfo field in fields)
            {
                object? value = field.GetValue(null);
                Assert.That(value, Is.Not.Null,
                    staticClass.FullName + "." + field.Name);
                Assert.That(field.FieldType.IsInstanceOfType(value), Is.True,
                    staticClass.FullName + "." + field.Name);
            }
        }

        Type[] exceptions = assembly.GetExportedTypes()
            .Where(type => type.IsClass && !type.IsAbstract &&
                typeof(Exception).IsAssignableFrom(type))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
        foreach (Type exceptionType in exceptions)
        {
            ConstructorInfo constructor = exceptionType.GetConstructors()
                .OrderBy(value => value.GetParameters().Length)
                .First();
            object? exception = constructor.Invoke(DefaultArguments(
                constructor.GetParameters()));
            Assert.That(exception, Is.InstanceOf<Exception>(),
                exceptionType.FullName);
        }
    }

    [Test]
    public void EveryPublicImplementationIsExercisedThroughEveryApiInterfaceMethod()
    {
        Assembly assembly = typeof(MessageDocument).Assembly;
        Type[] implementations = PublicImplementations(assembly);
        Assert.That(implementations, Is.Not.Empty);

        foreach (Type implementation in implementations)
        {
            object instance = CreateImplementation(implementation);
            Type[] contracts = implementation.GetInterfaces()
                .Where(type => type.Assembly == assembly && type.IsPublic)
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToArray();
            Assert.That(contracts, Is.Not.Empty, implementation.FullName);
            foreach (Type contract in contracts)
            {
                foreach (MethodInfo method in contract.GetMethods()
                    .OrderBy(value => value.Name, StringComparer.Ordinal))
                {
                    object? result = InvokeBoundaryCase(
                        instance,
                        implementation,
                        contract,
                        method);
                    if (method.ReturnType != typeof(void) &&
                        !CanReturnNull(method.ReturnType))
                    {
                        Assert.That(result, Is.Not.Null,
                            implementation.Name + "." + method.Name);
                    }
                }
            }
            IDisposable? disposable = instance as IDisposable;
            disposable?.Dispose();
        }
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

    private static object? InvokeBoundaryCase(
        object instance,
        Type implementation,
        Type contract,
        MethodInfo method)
    {
        object?[] arguments = DefaultArguments(method.GetParameters());
        try
        {
            return method.Invoke(instance, arguments);
        }
        catch (TargetInvocationException exception)
        {
            if (implementation == typeof(MetadataExtractor) &&
                contract == typeof(IMetadataExtractor) &&
                method.Name == "ExtractMetadata" &&
                exception.InnerException is ArgumentNullException)
            {
                return null;
            }
            throw new AssertionException(
                implementation.Name + "." + method.Name +
                " did not handle its boundary invocation.",
                exception.InnerException ?? exception);
        }
    }

    private static object?[] DefaultArguments(ParameterInfo[] parameters)
    {
        object?[] arguments = new object?[parameters.Length];
        int index = 0;
        while (index < parameters.Length)
        {
            Type type = parameters[index].ParameterType;
            if (type.IsValueType)
            {
                arguments[index] = Activator.CreateInstance(type);
            }
            index++;
        }
        return arguments;
    }

    private static bool CanReturnNull(Type returnType)
    {
        return !returnType.IsValueType;
    }
}
