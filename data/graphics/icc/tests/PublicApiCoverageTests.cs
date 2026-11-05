using icc;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace icc.Tests;

public class PublicApiCoverageTests
{
    [Test]
    public void EveryStaticApiFieldHasAStableNameTypeAndValue()
    {
        Type[] types = ApiTypes().Where(type => type.IsAbstract
            && type.IsSealed
            && PublicStaticFields(type).Length > 0).ToArray();
        string snapshot = StaticFieldSnapshot(types, out int fieldCount);
        string hash = SnapshotHash(snapshot);

        Assert.Multiple(() =>
        {
            Assert.That(types, Has.Length.EqualTo(13));
            Assert.That(fieldCount, Is.EqualTo(435));
            Assert.That(hash,
                Is.EqualTo("D3E758D1DABD76569C54158FB08C8A64752885ECFDF26E687F9A9F1E6929C344"),
                snapshot);
        });
    }

    [Test]
    public void EveryDataFieldHasADeterministicIndependentDefault()
    {
        Type[] types = DataTypes();
        NullabilityInfoContext nullability = new NullabilityInfoContext();
        int fieldCount = 0;
        StringBuilder snapshot = new StringBuilder();

        foreach (Type type in types)
        {
            object first = Activator.CreateInstance(type)!;
            object second = Activator.CreateInstance(type)!;
            snapshot.Append(type.FullName).AppendLine();
            foreach (FieldInfo field in PublicInstanceFields(type))
            {
                fieldCount++;
                object? firstValue = field.GetValue(first);
                object? secondValue = field.GetValue(second);
                snapshot.Append("  ").Append(field.Name).Append(':')
                    .Append(field.FieldType.FullName).Append('=')
                    .Append(FormatValue(firstValue, new HashSet<object>(
                        ReferenceEqualityComparer.Instance))).AppendLine();

                Assert.That(FormatValue(secondValue, new HashSet<object>(
                    ReferenceEqualityComparer.Instance)),
                    Is.EqualTo(FormatValue(firstValue, new HashSet<object>(
                        ReferenceEqualityComparer.Instance))),
                    type.Name + "." + field.Name + " default");
                if (!field.FieldType.IsValueType
                    && nullability.Create(field).ReadState
                        == NullabilityState.NotNull)
                {
                    Assert.That(firstValue, Is.Not.Null,
                        type.Name + "." + field.Name);
                }

                if (firstValue != null && secondValue != null
                    && firstValue is not string
                    && !field.FieldType.IsValueType)
                {
                    Assert.That(firstValue, Is.Not.SameAs(secondValue),
                        type.Name + "." + field.Name);
                }
            }
        }

        string text = snapshot.ToString();
        Assert.Multiple(() =>
        {
            Assert.That(types, Has.Length.EqualTo(110));
            Assert.That(fieldCount, Is.EqualTo(703));
            Assert.That(SnapshotHash(text),
                Is.EqualTo("2B3E855E0EDA6B3A60FCA2E2B5DD406C8DEA3657B25018C107DA6A7BE26D119D"),
                text);
        });
    }

    [Test]
    public void EveryRegisteredSignatureHasAStableDescription()
    {
        IIccSignatureDescriber describer = new IccSignatureDescriber();

        Assert.Multiple(() =>
        {
            AssertSignatureDescriptions(typeof(IccProfileClassSignatures),
                describer.DescribeProfileClassSignature);
            AssertSignatureDescriptions(typeof(IccColorSpaceSignatures),
                describer.DescribeColorSpaceSignature, "Prefix");
            AssertSignatureDescriptions(typeof(IccTagSignatures),
                describer.DescribeTagSignature);
            AssertSignatureDescriptions(typeof(IccTypeSignatures),
                describer.DescribeTypeSignature);
            AssertSignatureDescriptions(
                typeof(IccProcessingElementSignatures),
                describer.DescribeProcessingElementSignature);
            AssertSignatureDescriptions(typeof(IccArraySignatures),
                describer.DescribeArraySignature);
            AssertSignatureDescriptions(typeof(IccStructureSignatures),
                describer.DescribeStructureSignature);
            AssertSignatureDescriptions(typeof(IccTechnologySignatures),
                describer.DescribeTechnologySignature);
            AssertSignatureDescriptions(typeof(IccImageStateSignatures),
                describer.DescribeImageStateSignature);
        });
    }

    [Test]
    public void EveryDescribedEnumValueHasAStableResult()
    {
        IIccSignatureDescriber describer = new IccSignatureDescriber();

        Assert.Multiple(() =>
        {
            AssertDescriptions(Enum.GetValues<IccVersionKind>(),
                describer.DescribeVersionKind, new string[]
                {
                    "Unknown ICC version", "ICC.1 v2", "ICC.1 v4",
                    "ICC.2 v5"
                });
            AssertDescriptions(Enum.GetValues<IccProfileClass>(),
                describer.DescribeProfileClass, new string[]
                {
                    "Unknown profile class", "Input device profile",
                    "Display device profile", "Output device profile",
                    "DeviceLink profile", "Colour-space conversion profile",
                    "Abstract profile", "Named-colour profile",
                    "ICC.2 colour-encoding-space profile",
                    "ICC.2 multiplex-identification profile",
                    "ICC.2 multiplex-link profile",
                    "ICC.2 multiplex-visualization profile"
                });
            AssertDescriptions(Enum.GetValues<IccColorSpaceKind>(),
                describer.DescribeColorSpaceKind, new string[]
                {
                    "Unknown colour space", "CIE XYZ", "CIELAB", "CIELUV",
                    "YCbCr", "CIE Yxy", "RGB", "Grayscale", "HSV", "HLS",
                    "CMYK", "CMY", "Named colour", "Device channels",
                    "ICC.2 extended device channels", "Spectral",
                    "Bi-spectral", "Multiplex channels"
                });
            AssertDescriptions(Enum.GetValues<IccPcsKind>(),
                describer.DescribePcsKind, new string[]
                {
                    "No PCS", "CIE XYZ PCS", "CIELAB PCS", "Spectral PCS",
                    "Bi-spectral PCS", "Material Connection Space"
                });
            AssertDescriptions(Enum.GetValues<IccRenderingIntent>(),
                describer.DescribeRenderingIntent, new string[]
                {
                    "Perceptual", "Media-relative colorimetric", "Saturation",
                    "ICC-absolute colorimetric"
                });
            AssertEnumNames(Enum.GetValues<IccTagDataKind>(),
                describer.DescribeTagDataKind);
            AssertEnumNames(Enum.GetValues<IccCurveKind>(),
                describer.DescribeCurveKind);
            AssertEnumNames(Enum.GetValues<IccCurveSegmentKind>(),
                describer.DescribeCurveSegmentKind);
            AssertEnumNames(Enum.GetValues<IccProcessingElementKind>(),
                describer.DescribeProcessingElementKind);
            AssertEnumNames(Enum.GetValues<IccInterpolationKind>(),
                describer.DescribeInterpolationKind);
            AssertEnumNames(Enum.GetValues<IccValidationSeverity>(),
                describer.DescribeValidationSeverity);
            AssertEnumNames(Enum.GetValues<IccValidationMode>(),
                describer.DescribeValidationMode);
            AssertEnumNames(Enum.GetValues<IccWriteMode>(),
                describer.DescribeWriteMode);
            AssertEnumNames(Enum.GetValues<IccReadMode>(),
                describer.DescribeReadMode);
            AssertEnumNames(Enum.GetValues<IccTransformDirection>(),
                describer.DescribeTransformDirection);
            AssertEnumNames(Enum.GetValues<IccIntentFallback>(),
                describer.DescribeIntentFallback);
            AssertEnumNames(Enum.GetValues<IccPcsEncodingKind>(),
                describer.DescribePcsEncodingKind);
            AssertEnumNames(Enum.GetValues<IccColorDifferenceFormula>(),
                describer.DescribeColorDifferenceFormula);
            AssertEnumNames(Enum.GetValues<IccInspectionDetail>(),
                describer.DescribeInspectionDetail);
            AssertEnumNames(Enum.GetValues<IccGamutStatus>(),
                describer.DescribeGamutStatus);
            AssertEnumNames(Enum.GetValues<IccEmbeddedFormat>(),
                describer.DescribeEmbeddedFormat);
            AssertEnumNames(Enum.GetValues<IccEmbeddedOperation>(),
                describer.DescribeEmbeddedOperation);
            AssertEnumNames(Enum.GetValues<IccMeasurementObserver>(),
                describer.DescribeMeasurementObserver);
            AssertEnumNames(Enum.GetValues<IccMeasurementGeometry>(),
                describer.DescribeMeasurementGeometry);
            AssertEnumNames(Enum.GetValues<IccStandardIlluminant>(),
                describer.DescribeStandardIlluminant);
            AssertEnumNames(Enum.GetValues<IccOperationStatus>(),
                describer.DescribeOperationStatus);
        });
    }

    private static Type[] ApiTypes()
    {
        return typeof(IccProfile).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == "icc")
            .OrderBy(type => type.FullName, StringComparer.Ordinal).ToArray();
    }

    private static Type[] DataTypes()
    {
        return ApiTypes().Where(type => type.IsClass && !type.IsAbstract
            && type.BaseType == typeof(object)
            && type.GetInterfaces().Length == 0
            && PublicInstanceFields(type).Length > 0).ToArray();
    }

    private static FieldInfo[] PublicInstanceFields(Type type)
    {
        return type.GetFields(BindingFlags.Public | BindingFlags.Instance
                | BindingFlags.DeclaredOnly)
            .OrderBy(field => field.Name, StringComparer.Ordinal).ToArray();
    }

    private static FieldInfo[] PublicStaticFields(Type type)
    {
        return type.GetFields(BindingFlags.Public | BindingFlags.Static
                | BindingFlags.DeclaredOnly)
            .OrderBy(field => field.Name, StringComparer.Ordinal).ToArray();
    }

    private static string StaticFieldSnapshot(Type[] types,
        out int fieldCount)
    {
        StringBuilder snapshot = new StringBuilder();
        fieldCount = 0;
        foreach (Type type in types)
        {
            snapshot.Append(type.FullName).AppendLine();
            foreach (FieldInfo field in PublicStaticFields(type))
            {
                fieldCount++;
                snapshot.Append("  ").Append(field.Name).Append(':')
                    .Append(field.FieldType.FullName).Append('=')
                    .Append(FormatValue(field.GetValue(null),
                        new HashSet<object>(
                            ReferenceEqualityComparer.Instance)))
                    .AppendLine();
            }
        }

        return snapshot.ToString();
    }

    private static string FormatValue(object? value, HashSet<object> active)
    {
        if (value == null)
        {
            return "<null>";
        }

        Type type = value.GetType();
        if (value is string text)
        {
            return text.Length.ToString(CultureInfo.InvariantCulture)
                + ":" + text;
        }

        if (type.IsEnum)
        {
            return type.FullName + "." + value + "("
                + Convert.ToInt64(value, CultureInfo.InvariantCulture)
                    .ToString(CultureInfo.InvariantCulture) + ")";
        }

        if (value is Array array)
        {
            StringBuilder formatted = new StringBuilder();
            formatted.Append(type.FullName).Append('[').Append(array.Length)
                .Append("]{");
            int index = 0;
            while (index < array.Length)
            {
                if (index > 0)
                {
                    formatted.Append(',');
                }

                formatted.Append(FormatValue(array.GetValue(index), active));
                index++;
            }

            return formatted.Append('}').ToString();
        }

        if (type.Namespace == "icc")
        {
            if (!active.Add(value))
            {
                return "<cycle:" + type.FullName + ">";
            }

            StringBuilder formatted = new StringBuilder();
            formatted.Append(type.FullName).Append('{');
            FieldInfo[] fields = PublicInstanceFields(type);
            int index = 0;
            while (index < fields.Length)
            {
                if (index > 0)
                {
                    formatted.Append(',');
                }

                formatted.Append(fields[index].Name).Append('=')
                    .Append(FormatValue(fields[index].GetValue(value), active));
                index++;
            }

            active.Remove(value);
            return formatted.Append('}').ToString();
        }

        if (value is double doubleValue)
        {
            return doubleValue.ToString("R", CultureInfo.InvariantCulture);
        }

        if (value is float floatValue)
        {
            return floatValue.ToString("R", CultureInfo.InvariantCulture);
        }

        return Convert.ToString(value, CultureInfo.InvariantCulture)
            ?? string.Empty;
    }

    private static string SnapshotHash(string value)
    {
        return Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes(value)));
    }

    private static void AssertSignatureDescriptions(Type type,
        Func<uint, string> describe, string excludedSuffix = "")
    {
        FieldInfo[] fields = PublicStaticFields(type);
        foreach (FieldInfo field in fields)
        {
            if (excludedSuffix.Length > 0
                && field.Name.EndsWith(excludedSuffix,
                    StringComparison.Ordinal))
            {
                continue;
            }

            uint value = (uint)field.GetValue(null)!;
            string description = describe(value);
            Assert.That(description, Is.Not.Empty, type.Name + "." + field.Name);
            Assert.That(description, Does.Not.StartWith("Unknown/private"),
                type.Name + "." + field.Name);
            Assert.That(description, Does.Contain(
                "0x" + value.ToString("X8", CultureInfo.InvariantCulture)),
                type.Name + "." + field.Name);
        }
    }

    private static void AssertDescriptions<T>(T[] values,
        Func<T, string> describe, string[] expected) where T : struct, Enum
    {
        Assert.That(values, Has.Length.EqualTo(expected.Length));
        int index = 0;
        while (index < values.Length)
        {
            Assert.That(describe(values[index]), Is.EqualTo(expected[index]),
                typeof(T).Name + "." + values[index]);
            index++;
        }
    }

    private static void AssertEnumNames<T>(T[] values,
        Func<T, string> describe) where T : struct, Enum
    {
        string[] expected = values.Select(value => value.ToString()).ToArray();
        AssertDescriptions(values, describe, expected);
    }
}
