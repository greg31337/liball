using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using uspto;

namespace uspto.Tests;

public class PublicDataModelRoundTripTests
{
    [Test]
    public void EveryPublicDataModelPreservesAllFieldsAcrossJsonRoundTrip()
    {
        JsonSerializerOptions options = new JsonSerializerOptions { IncludeFields = true };
        options.Converters.Add(new CancellationTokenJsonConverter());
        Type[] modelTypes = typeof(UsptoClients).Assembly.GetExportedTypes()
            .Where(IsPublicDataModel)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
        List<string> failures = new List<string>();

        foreach (Type modelType in modelTypes)
        {
            try
            {
                object source = CreateSample(modelType, 0)!;
                string serialized = JsonSerializer.Serialize(source, modelType, options);
                object? restored = JsonSerializer.Deserialize(serialized, modelType, options);
                string reserialized = JsonSerializer.Serialize(restored, modelType, options);
                if (restored == null)
                {
                    failures.Add(modelType.FullName + ": deserialization returned null");
                    continue;
                }

                if (!string.Equals(serialized, reserialized, StringComparison.Ordinal))
                {
                    failures.Add(modelType.FullName + ": serialized state changed after round trip");
                }

                foreach (FieldInfo field in modelType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (!serialized.Contains("\"" + field.Name + "\"", StringComparison.Ordinal))
                    {
                        failures.Add(modelType.FullName + "." + field.Name + ": public field was not serialized");
                    }
                }
            }
            catch (Exception exception)
            {
                failures.Add(modelType.FullName + ": " + exception.GetType().Name + " - " + exception.Message);
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(modelTypes, Has.Length.GreaterThan(250));
            Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures));
        });
    }

    private static bool IsPublicDataModel(Type type)
    {
        return type.IsClass
            && !type.IsAbstract
            && type.GetConstructor(Type.EmptyTypes) != null
            && type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Length > 0;
    }

    private static object? CreateSample(Type type, int depth)
    {
        if (type == typeof(string))
        {
            return "sample";
        }

        if (type == typeof(object))
        {
            return "sample-object";
        }

        if (type == typeof(Uri))
        {
            return new Uri("https://example.test/sample");
        }

        if (type == typeof(DateTimeOffset))
        {
            return new DateTimeOffset(2026, 9, 11, 12, 34, 56, TimeSpan.Zero);
        }

        if (type == typeof(DateTime))
        {
            return new DateTime(2026, 9, 11, 12, 34, 56, DateTimeKind.Utc);
        }

        if (type == typeof(bool))
        {
            return true;
        }

        if (type == typeof(byte))
        {
            return (byte)7;
        }

        if (type == typeof(short))
        {
            return (short)17;
        }

        if (type == typeof(int))
        {
            return 23;
        }

        if (type == typeof(long))
        {
            return 29L;
        }

        if (type == typeof(float))
        {
            return 3.5F;
        }

        if (type == typeof(double))
        {
            return 7.25D;
        }

        if (type == typeof(decimal))
        {
            return 11.75M;
        }

        if (type.IsEnum)
        {
            Array values = Enum.GetValues(type);
            return values.Length == 0 ? Activator.CreateInstance(type) : values.GetValue(values.Length - 1);
        }

        if (type.IsArray)
        {
            Type elementType = type.GetElementType()!;
            Array result = Array.CreateInstance(elementType, 1);
            object? element = CreateSample(elementType, depth + 1);
            if (element != null || !elementType.IsValueType)
            {
                result.SetValue(element, 0);
            }

            return result;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            IDictionary dictionary = (IDictionary)Activator.CreateInstance(type)!;
            Type[] arguments = type.GetGenericArguments();
            object? key = CreateSample(arguments[0], depth + 1);
            object? value = CreateSample(arguments[1], depth + 1);
            if (key != null)
            {
                dictionary.Add(key, value);
            }

            return dictionary;
        }

        if (type.IsInterface || type.IsAbstract || type == typeof(HttpClient) || depth >= 5)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        ConstructorInfo? constructor = type.GetConstructor(Type.EmptyTypes);
        if (constructor == null)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        object instance = constructor.Invoke(Array.Empty<object>());
        foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            if (!field.IsInitOnly)
            {
                object? value = CreateSample(field.FieldType, depth + 1);
                if (value != null || !field.FieldType.IsValueType)
                {
                    field.SetValue(instance, value);
                }
            }
        }

        return instance;
    }
}

internal class CancellationTokenJsonConverter : JsonConverter<CancellationToken>
{
    public override CancellationToken Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new CancellationToken(reader.GetBoolean());
    }

    public override void Write(Utf8JsonWriter writer, CancellationToken value, JsonSerializerOptions options)
    {
        writer.WriteBooleanValue(value.IsCancellationRequested);
    }
}
