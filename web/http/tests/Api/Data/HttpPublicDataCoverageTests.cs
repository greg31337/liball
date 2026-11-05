using System.Reflection;

namespace http.Tests;

public class HttpPublicDataCoverageTests
{
    [Test]
    public void EveryPublicDataFieldRetainsItsAssignedRoleValue()
    {
        Type[] dataTypes = ReadDataTypes();

        Assert.That(dataTypes, Has.Length.EqualTo(57));
        Assert.Multiple(() =>
        {
            foreach (Type dataType in dataTypes)
            {
                object instance = Activator.CreateInstance(dataType)!;
                FieldInfo[] fields = dataType.GetFields(
                    BindingFlags.Public | BindingFlags.Instance |
                    BindingFlags.DeclaredOnly);
                Assert.That(fields, Is.Not.Empty,
                    $"{dataType.Name} has no data fields.");
                foreach (FieldInfo field in fields)
                {
                    object assigned = CreateAssignedValue(field.FieldType);
                    field.SetValue(instance, assigned);
                    object? actual = field.GetValue(instance);
                    if (field.FieldType.IsValueType ||
                        field.FieldType == typeof(string))
                    {
                        Assert.That(actual, Is.EqualTo(assigned),
                            $"{dataType.Name}.{field.Name} did not retain its value.");
                    }
                    else
                    {
                        Assert.That(actual, Is.SameAs(assigned),
                            $"{dataType.Name}.{field.Name} did not retain its object.");
                    }
                }
            }
        });
    }

    [Test]
    public void DataReferenceDefaultsExpressRequiredAndOptionalRoles()
    {
        NullabilityInfoContext nullability = new();

        Assert.Multiple(() =>
        {
            foreach (Type dataType in ReadDataTypes())
            {
                object first = Activator.CreateInstance(dataType)!;
                object second = Activator.CreateInstance(dataType)!;
                foreach (FieldInfo field in dataType.GetFields(
                    BindingFlags.Public | BindingFlags.Instance |
                    BindingFlags.DeclaredOnly))
                {
                    if (field.FieldType.IsValueType)
                    {
                        continue;
                    }

                    object? firstValue = field.GetValue(first);
                    NullabilityState state =
                        nullability.Create(field).ReadState;
                    if (state == NullabilityState.Nullable)
                    {
                        Assert.That(firstValue, Is.Null,
                            $"{dataType.Name}.{field.Name} should default to absent.");
                        continue;
                    }

                    Assert.That(firstValue, Is.Not.Null,
                        $"{dataType.Name}.{field.Name} should have a usable default.");
                    if (IsOwnedContainerOrData(field.FieldType))
                    {
                        Assert.That(firstValue,
                            Is.Not.SameAs(field.GetValue(second)),
                            $"{dataType.Name}.{field.Name} should be independently owned.");
                    }
                }
            }
        });
    }

    [Test]
    public void PresenceFlagsDefaultToAbsentAcrossPublicData()
    {
        Assert.Multiple(() =>
        {
            foreach (Type dataType in ReadDataTypes())
            {
                object instance = Activator.CreateInstance(dataType)!;
                FieldInfo[] presenceFields = dataType.GetFields(
                    BindingFlags.Public | BindingFlags.Instance |
                    BindingFlags.DeclaredOnly)
                    .Where(field => field.FieldType == typeof(bool))
                    .Where(field => field.Name.StartsWith(
                        "Has", StringComparison.Ordinal))
                    .ToArray();
                foreach (FieldInfo field in presenceFields)
                {
                    Assert.That(field.GetValue(instance), Is.EqualTo(false),
                        $"{dataType.Name}.{field.Name} should default to absent.");
                }
            }
        });
    }

    [Test]
    public void ResultPayloadValuesDoNotImplicitlySetPresenceFlags()
    {
        object[][] cases =
        [
            [typeof(HpackResult), "HasErrorPosition", "ErrorPosition"],
            [typeof(Http1ParseResult), "HasErrorPosition", "ErrorPosition"],
            [typeof(Http2ConnectionResult), "HasUnexpectedHttpVersion", "UnexpectedHttpVersion"],
            [typeof(Http2ConnectionResult), "HasErrorCode", "ErrorCode"],
            [typeof(Http2FrameResult), "HasErrorPosition", "ErrorPosition"],
            [typeof(Http2MessageResult), "HasAssociatedStreamId", "AssociatedStreamId"],
            [typeof(Http2MessageResult), "HasErrorCode", "ErrorCode"],
            [typeof(HttpNegotiationResult), "HasMediaMatch", "MediaQuality"],
            [typeof(HttpNegotiationResult), "HasCharsetMatch", "CharsetQuality"],
            [typeof(HttpNegotiationResult), "HasEncodingMatch", "EncodingQuality"],
            [typeof(HttpNegotiationResult), "HasLanguageMatch", "LanguageQuality"],
            [typeof(HttpNegotiationResult), "HasOverallQuality", "OverallQuality"],
            [typeof(Http1FramingDecision), "HasDeclaredLength", "DeclaredLength"],
            [typeof(HttpCacheDecision), "HasCurrentAge", "CurrentAge"],
            [typeof(HttpCacheDecision), "HasFreshnessLifetime", "FreshnessLifetime"],
            [typeof(HttpPreconditionDecision), "HasStatusCode", "StatusCode"]
        ];

        Assert.Multiple(() =>
        {
            foreach (object[] item in cases)
            {
                Type dataType = (Type)item[0];
                string presenceName = (string)item[1];
                string payloadName = (string)item[2];
                object instance = Activator.CreateInstance(dataType)!;
                FieldInfo presence = dataType.GetField(presenceName)!;
                FieldInfo payload = dataType.GetField(payloadName)!;
                object assigned = CreateAssignedValue(payload.FieldType);

                payload.SetValue(instance, assigned);
                Assert.That(presence.GetValue(instance), Is.EqualTo(false),
                    $"{dataType.Name}.{presenceName} changed implicitly.");
                presence.SetValue(instance, true);
                Assert.That(presence.GetValue(instance), Is.EqualTo(true));
                Assert.That(payload.GetValue(instance), Is.EqualTo(assigned));
            }
        });
    }

    static Type[] ReadDataTypes()
    {
        return typeof(HttpRequest).Assembly.GetTypes()
            .Where(type => type.IsPublic && type.IsClass && !type.IsAbstract)
            .Where(type => !typeof(Exception).IsAssignableFrom(type))
            .Where(type => type.GetConstructor(Type.EmptyTypes) != null)
            .Where(type => type.GetFields(
                BindingFlags.Public | BindingFlags.Instance |
                BindingFlags.DeclaredOnly).Length > 0)
            .OrderBy(type => type.Name)
            .ToArray();
    }

    static object CreateAssignedValue(Type type)
    {
        if (type == typeof(string))
        {
            return "assigned";
        }

        if (type == typeof(bool))
        {
            return true;
        }

        if (type == typeof(byte))
        {
            return (byte)7;
        }

        if (type == typeof(int))
        {
            return 7;
        }

        if (type == typeof(uint))
        {
            return 7u;
        }

        if (type == typeof(long))
        {
            return 7L;
        }

        if (type == typeof(DateTimeOffset))
        {
            return new DateTimeOffset(7, TimeSpan.Zero);
        }

        if (type.IsArray)
        {
            return Array.CreateInstance(type.GetElementType()!, 1);
        }

        if (type.IsEnum)
        {
            Array values = Enum.GetValues(type);
            return values.GetValue(values.Length - 1)!;
        }

        return Activator.CreateInstance(type)!;
    }

    static bool IsOwnedContainerOrData(Type type)
    {
        if (type.IsGenericType &&
            type.GetGenericTypeDefinition() == typeof(List<>))
        {
            return true;
        }

        return type.IsClass && type != typeof(string) && !type.IsArray;
    }
}
