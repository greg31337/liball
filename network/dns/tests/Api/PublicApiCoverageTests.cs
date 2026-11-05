using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace dns.Tests.Api;

public class PublicApiCoverageTests
{
    const string ExpectedEnumHash =
        "3D324070D06A4D56F277FC4BD30C14F5947EA72747B07A0A839E44B5F287A556";
    const int ExpectedEnumMemberCount = 351;
    const string ExpectedStaticHash =
        "9081CEDFDA6B1DA0CD48C7419FAEE756D7EB0A49F4D8D7A726FB6E13CB72FEE7";
    const int ExpectedStaticFieldCount = 51;
    const string ExpectedContractHash =
        "4DB9921FF16B76F12855DF16A8413313C370022248587699E22596E71D714A5B";
    const int ExpectedContractCount = 55;
    const string ExpectedDataSurfaceHash =
        "9A4C8BE6DBB7B274B5079945BF3DAB03FE57311AE75BA12EEAD30EC85472334C";
    const int ExpectedDataFieldCount = 952;
    const string ExpectedDataDefaultsHash =
        "87A1D57BA00B8D5A8A6D2A6CF76A30551EA0EBCB6CD8F5B81EBCB14994FF205E";
    const int ExpectedNonDefaultCount = 129;

    [Test]
    public void EveryPublicEnumMemberHasItsStableValue()
    {
        string actual = EnumSnapshot();
        TestContext.Out.WriteLine($"hash={Hash(actual)}");
        TestContext.Out.WriteLine($"count={LineCount(actual)}");
        Assert.Multiple(() =>
        {
            Assert.That(Hash(actual), Is.EqualTo(ExpectedEnumHash));
            Assert.That(LineCount(actual),
                Is.EqualTo(ExpectedEnumMemberCount));
        });
    }

    [Test]
    public void EveryPublicStaticProtocolFieldHasItsStableValue()
    {
        string actual = StaticSnapshot();
        TestContext.Out.WriteLine($"hash={Hash(actual)}");
        TestContext.Out.WriteLine($"count={LineCount(actual)}");
        Assert.Multiple(() =>
        {
            Assert.That(Hash(actual), Is.EqualTo(ExpectedStaticHash));
            Assert.That(LineCount(actual),
                Is.EqualTo(ExpectedStaticFieldCount));
        });
    }

    [Test]
    public void EveryPublicImplementationHasItsConstructionContract()
    {
        string actual = ContractSnapshot();
        TestContext.Out.WriteLine($"hash={Hash(actual)}");
        TestContext.Out.WriteLine($"count={LineCount(actual)}");
        Assert.Multiple(() =>
        {
            Assert.That(Hash(actual), Is.EqualTo(ExpectedContractHash));
            Assert.That(LineCount(actual), Is.EqualTo(ExpectedContractCount));
        });
    }

    [Test]
    public void EveryPublicDataFieldHasItsStableTypeAndDefault()
    {
        string surface = DataSurfaceSnapshot();
        string defaults = DataDefaultsSnapshot();
        string surfaceHash = Hash(surface);
        string defaultsHash = Hash(defaults);
        TestContext.Out.WriteLine($"surface-hash={surfaceHash}");
        TestContext.Out.WriteLine($"field-count={LineCount(surface)}");
        TestContext.Out.WriteLine($"defaults-hash={defaultsHash}");
        TestContext.Out.WriteLine($"non-default-count={LineCount(defaults)}");

        Assert.Multiple(() =>
        {
            Assert.That(surfaceHash, Is.EqualTo(ExpectedDataSurfaceHash));
            Assert.That(LineCount(surface), Is.EqualTo(ExpectedDataFieldCount));
            Assert.That(defaultsHash, Is.EqualTo(ExpectedDataDefaultsHash));
            Assert.That(LineCount(defaults), Is.EqualTo(ExpectedNonDefaultCount));
        });
    }

    static string EnumSnapshot()
    {
        StringBuilder result = new StringBuilder();
        foreach (Type type in PublicDnsTypes().Where(type => type.IsEnum))
        {
            foreach (FieldInfo field in type.GetFields(
                BindingFlags.Public | BindingFlags.Static)
                .OrderBy(field => field.MetadataToken))
            {
                long value = Convert.ToInt64(
                    field.GetRawConstantValue(),
                    CultureInfo.InvariantCulture);
                result.Append(type.Name).Append('.').Append(field.Name)
                    .Append('=').Append(value).Append('\n');
            }
        }

        return result.ToString().TrimEnd();
    }

    static string StaticSnapshot()
    {
        StringBuilder result = new StringBuilder();
        foreach (Type type in PublicDnsTypes().Where(
            type => type.IsClass && type.IsAbstract && type.IsSealed))
        {
            foreach (FieldInfo field in type.GetFields(
                BindingFlags.Public | BindingFlags.Static)
                .OrderBy(field => field.MetadataToken))
            {
                result.Append(type.Name).Append('.').Append(field.Name)
                    .Append(':').Append(FriendlyName(field.FieldType))
                    .Append('=').Append(FormatValue(field.GetValue(null)))
                    .Append('\n');
            }
        }

        return result.ToString().TrimEnd();
    }

    static string ContractSnapshot()
    {
        StringBuilder result = new StringBuilder();
        Type[] publicTypes = PublicDnsTypes();
        foreach (Type contract in publicTypes.Where(type => type.IsInterface))
        {
            string implementations = string.Join(
                ",",
                publicTypes.Where(type => type.IsClass && !type.IsAbstract &&
                    contract.IsAssignableFrom(type)).Select(type => type.Name));
            result.Append(contract.Name).Append("->")
                .Append(implementations).Append('\n');
        }

        foreach (Type type in publicTypes.Where(type => type.IsClass &&
            !type.IsAbstract && type.GetInterfaces().Any(contract =>
                contract.Namespace == "dns")))
        {
            ConstructorInfo[] constructors = type.GetConstructors();
            string signatures = string.Join(
                ";",
                constructors.OrderBy(constructor => constructor.MetadataToken)
                    .Select(ConstructorSignature));
            string contracts = string.Join(
                ",",
                type.GetInterfaces().Where(contract =>
                    contract.Namespace == "dns").OrderBy(contract =>
                    contract.Name).Select(contract => contract.Name));
            result.Append(type.Name).Append('(').Append(signatures)
                .Append(")->").Append(contracts).Append('\n');
        }

        foreach (Type type in publicTypes.Where(type =>
            type.IsSubclassOf(typeof(Exception))))
        {
            string signatures = string.Join(
                ";",
                type.GetConstructors().OrderBy(constructor =>
                    constructor.MetadataToken).Select(ConstructorSignature));
            result.Append(type.Name).Append('(').Append(signatures)
                .Append(")->System.Exception").Append('\n');
        }

        return result.ToString().TrimEnd();
    }

    static string DataSurfaceSnapshot()
    {
        StringBuilder result = new StringBuilder();
        foreach (Type type in DataTypes())
        {
            foreach (FieldInfo field in DataFields(type))
            {
                NullabilityInfo nullability =
                    new NullabilityInfoContext().Create(field);
                result.Append(type.Name).Append('.').Append(field.Name)
                    .Append(':').Append(FriendlyName(field.FieldType))
                    .Append(':').Append(nullability.ReadState)
                    .Append('\n');
            }
        }

        return result.ToString().TrimEnd();
    }

    static string DataDefaultsSnapshot()
    {
        StringBuilder result = new StringBuilder();
        NullabilityInfoContext nullabilityContext =
            new NullabilityInfoContext();
        foreach (Type type in DataTypes())
        {
            object instance = Activator.CreateInstance(type)!;
            foreach (FieldInfo field in DataFields(type))
            {
                object? value = field.GetValue(instance);
                string actual = FormatValue(value);
                string baseline = BaselineValue(
                    field,
                    nullabilityContext.Create(field));
                if (actual != baseline)
                {
                    result.Append(type.Name).Append('.').Append(field.Name)
                        .Append('=').Append(actual).Append('\n');
                }
            }
        }

        return result.ToString().TrimEnd();
    }

    static Type[] PublicDnsTypes()
    {
        return typeof(DnsMessage).Assembly.GetTypes()
            .Where(type => type.IsPublic && type.Namespace == "dns")
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .ToArray();
    }

    static Type[] DataTypes()
    {
        return PublicDnsTypes().Where(type => type.IsClass &&
            !type.IsAbstract && !type.IsSubclassOf(typeof(Exception)) &&
            DataFields(type).Length > 0).ToArray();
    }

    static FieldInfo[] DataFields(Type type)
    {
        return type.GetFields(
            BindingFlags.Public | BindingFlags.Instance |
            BindingFlags.DeclaredOnly).OrderBy(field =>
            field.MetadataToken).ToArray();
    }

    static string ConstructorSignature(ConstructorInfo constructor)
    {
        return string.Join(
            ",",
            constructor.GetParameters().Select(parameter =>
                FriendlyName(parameter.ParameterType)));
    }

    static string FriendlyName(Type type)
    {
        if (type.IsArray)
        {
            return FriendlyName(type.GetElementType()!) + "[]";
        }

        if (!type.IsGenericType)
        {
            return type.Name;
        }

        string name = type.Name.Split('`')[0];
        return name + "<" + string.Join(
            ",",
            type.GetGenericArguments().Select(FriendlyName)) + ">";
    }

    static string FormatValue(object? value)
    {
        if (value == null)
        {
            return "null";
        }

        if (value is string text)
        {
            return $"\"{text}\"";
        }

        Type type = value.GetType();
        if (type.IsEnum)
        {
            return Convert.ToInt64(value, CultureInfo.InvariantCulture)
                .ToString(CultureInfo.InvariantCulture);
        }

        if (value is Array array)
        {
            return $"Length={array.Length}";
        }

        if (value is IList list)
        {
            return $"Count={list.Count}";
        }

        if (value is bool boolean)
        {
            return boolean ? "true" : "false";
        }

        if (value is IFormattable formattable && type.IsValueType)
        {
            return formattable.ToString(null, CultureInfo.InvariantCulture);
        }

        return "new " + FriendlyName(type);
    }

    static string BaselineValue(
        FieldInfo field,
        NullabilityInfo nullability)
    {
        Type type = field.FieldType;
        if (type.IsValueType)
        {
            return FormatValue(Activator.CreateInstance(type));
        }

        if (nullability.ReadState == NullabilityState.Nullable)
        {
            return "null";
        }

        if (type == typeof(string))
        {
            return "\"\"";
        }

        if (type.IsArray)
        {
            return "Length=0";
        }

        if (typeof(IList).IsAssignableFrom(type))
        {
            return "Count=0";
        }

        return "new " + FriendlyName(type);
    }

    static string Hash(string text)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes);
    }

    static int LineCount(string text)
    {
        return text.Length == 0 ? 0 : text.Count(character => character == '\n') + 1;
    }
}
