using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace yaml;

/// <summary>
/// Provides the high-level entry points for parsing, loading, emitting, and converting YAML.
/// </summary>
/// <remarks>
/// Throwing methods surface YAML diagnostics as <see cref="InvalidOperationException"/>. Their <c>Try</c> counterparts populate a caller-provided <see cref="YamlParseResult"/>; argument and CLR conversion errors can still throw. Use <see cref="YamlProcessor"/> when an instance implementing <see cref="IYamlProcessor"/> is preferable.
/// </remarks>
internal static class Yaml
{
    /// <summary>
    /// Parses the first YAML document into the mutable document model and throws when processing reports an error.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <returns>The parsed YAML document.</returns>
    /// <exception cref="InvalidOperationException">The YAML contains an error diagnostic.</exception>
    internal static YamlDocument Parse(string text)
    {
        return Parse(text, null);
    }

    /// <summary>
    /// Parses the first YAML document into the mutable document model and throws when processing reports an error.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <param name="options">The settings that control the operation; pass null to use the applicable defaults.</param>
    /// <returns>The parsed YAML document.</returns>
    /// <exception cref="InvalidOperationException">The YAML contains an error diagnostic.</exception>
    internal static YamlDocument Parse(string text, YamlOptions? options)
    {
        YamlProcessor processor = YamlProcessor.Create(options);
        YamlParseResult result = new YamlParseResult();
        processor.TryParse(text, result);
        ThrowIfFailed(result);
        return ResultDocument(result);
    }

    /// <summary>
    /// Parses every document in a YAML stream into the mutable document model and throws when processing reports an error.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <returns>The parsed YAML stream.</returns>
    /// <exception cref="InvalidOperationException">The YAML contains an error diagnostic.</exception>
    internal static YamlStream ParseAll(string text)
    {
        return ParseAll(text, null);
    }

    /// <summary>
    /// Parses every document in a YAML stream into the mutable document model and throws when processing reports an error.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <param name="options">The settings that control the operation; pass null to use the applicable defaults.</param>
    /// <returns>The parsed YAML stream.</returns>
    /// <exception cref="InvalidOperationException">The YAML contains an error diagnostic.</exception>
    internal static YamlStream ParseAll(string text, YamlOptions? options)
    {
        YamlProcessor processor = YamlProcessor.Create(options);
        YamlParseResult result = new YamlParseResult();
        processor.TryParseAll(text, result);
        ThrowIfFailed(result);
        return ResultStream(result);
    }

    /// <summary>
    /// Parses the first YAML document and reports errors through the supplied result instead of throwing for YAML diagnostics.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <param name="result">The result object to reset and populate.</param>
    /// <returns>True when the operation succeeds; otherwise false.</returns>
    internal static bool TryParse(string text, YamlParseResult? result)
    {
        return TryParse(text, result, null);
    }

    /// <summary>
    /// Parses the first YAML document and reports errors through the supplied result instead of throwing for YAML diagnostics.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <param name="result">The result object to reset and populate.</param>
    /// <param name="options">The settings that control the operation; pass null to use the applicable defaults.</param>
    /// <returns>True when the operation succeeds; otherwise false.</returns>
    internal static bool TryParse(string text, YamlParseResult? result, YamlOptions? options)
    {
        YamlProcessor processor = YamlProcessor.Create(options);
        return processor.TryParse(text, result);
    }

    /// <summary>
    /// Parses every document and reports errors through the supplied result instead of throwing for YAML diagnostics.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <param name="result">The result object to reset and populate.</param>
    /// <returns>True when the operation succeeds; otherwise false.</returns>
    internal static bool TryParseAll(string text, YamlParseResult? result)
    {
        return TryParseAll(text, result, null);
    }

    /// <summary>
    /// Parses every document and reports errors through the supplied result instead of throwing for YAML diagnostics.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <param name="result">The result object to reset and populate.</param>
    /// <param name="options">The settings that control the operation; pass null to use the applicable defaults.</param>
    /// <returns>True when the operation succeeds; otherwise false.</returns>
    internal static bool TryParseAll(string text, YamlParseResult? result, YamlOptions? options)
    {
        YamlProcessor processor = YamlProcessor.Create(options);
        return processor.TryParseAll(text, result);
    }

    /// <summary>
    /// Loads the first YAML document as dictionaries, lists, scalar CLR values, or null and throws when processing reports an error.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <returns>The loaded or converted value; null is a valid YAML result.</returns>
    internal static object? Load(string text)
    {
        return Load(text, null);
    }

    /// <summary>
    /// Loads the first YAML document as dictionaries, lists, scalar CLR values, or null and throws when processing reports an error.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <param name="options">The settings that control the operation; pass null to use the applicable defaults.</param>
    /// <returns>The loaded or converted value; null is a valid YAML result.</returns>
    internal static object? Load(string text, YamlOptions? options)
    {
        YamlProcessor processor = YamlProcessor.Create(options);
        return processor.Load(text);
    }

    /// <summary>
    /// Loads every YAML document as JSON-compatible CLR values and throws when processing reports an error.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <returns>The resulting values in document or sequence order.</returns>
    internal static List<object?> LoadAll(string text)
    {
        return LoadAll(text, null);
    }

    /// <summary>
    /// Loads every YAML document as JSON-compatible CLR values and throws when processing reports an error.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <param name="options">The settings that control the operation; pass null to use the applicable defaults.</param>
    /// <returns>The resulting values in document or sequence order.</returns>
    internal static List<object?> LoadAll(string text, YamlOptions? options)
    {
        YamlProcessor processor = YamlProcessor.Create(options);
        return processor.LoadAll(text);
    }

    /// <summary>
    /// Loads the first document and reports YAML errors through the supplied result.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <param name="result">The result object to reset and populate.</param>
    /// <returns>True when the operation succeeds; otherwise false.</returns>
    internal static bool TryLoad(string text, YamlParseResult? result)
    {
        return TryLoad(text, result, null);
    }

    /// <summary>
    /// Loads the first document and reports YAML errors through the supplied result.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <param name="result">The result object to reset and populate.</param>
    /// <param name="options">The settings that control the operation; pass null to use the applicable defaults.</param>
    /// <returns>True when the operation succeeds; otherwise false.</returns>
    internal static bool TryLoad(string text, YamlParseResult? result, YamlOptions? options)
    {
        YamlProcessor processor = YamlProcessor.Create(options);
        return processor.TryLoad(text, result);
    }

    /// <summary>
    /// Converts a supported CLR value to YAML text.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The emitted or formatted text.</returns>
    internal static string Dump(object? value)
    {
        return Dump(value, null);
    }

    /// <summary>
    /// Converts a supported CLR value to YAML text.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <param name="options">The settings that control the operation; pass null to use the applicable defaults.</param>
    /// <returns>The emitted or formatted text.</returns>
    internal static string Dump(object? value, YamlOptions? options)
    {
        YamlProcessor processor = YamlProcessor.Create(options);
        return processor.Dump(value);
    }

    /// <summary>
    /// Converts a supported CLR value to YAML text and reports conversion errors through the supplied result.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <param name="result">The result object to reset and populate.</param>
    /// <returns>True when the operation succeeds; otherwise false.</returns>
    internal static bool TryDump(object? value, YamlParseResult? result)
    {
        return TryDump(value, result, null);
    }

    /// <summary>
    /// Converts a supported CLR value to YAML text and reports conversion errors through the supplied result.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <param name="result">The result object to reset and populate.</param>
    /// <param name="options">The settings that control the operation; pass null to use the applicable defaults.</param>
    /// <returns>True when the operation succeeds; otherwise false.</returns>
    internal static bool TryDump(object? value, YamlParseResult? result, YamlOptions? options)
    {
        YamlProcessor processor = YamlProcessor.Create(options);
        return processor.TryDump(value, result);
    }

    /// <summary>
    /// Serializes supported values, including ordinary objects through their public instance fields, to YAML text.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The emitted or formatted text.</returns>
    internal static string Serialize(object? value)
    {
        return Serialize(value, null);
    }

    /// <summary>
    /// Serializes supported values, including ordinary objects through their public instance fields, to YAML text.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <param name="options">The settings that control the operation; pass null to use the applicable defaults.</param>
    /// <returns>The emitted or formatted text.</returns>
    internal static string Serialize(object? value, YamlOptions? options)
    {
        YamlProcessor processor = YamlProcessor.Create(options);
        return processor.Serialize(value);
    }

    /// <summary>
    /// Loads YAML and converts the result to the requested CLR type, using public instance fields for ordinary objects.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <param name="targetType">The CLR type to create.</param>
    /// <returns>The loaded or converted value; null is a valid YAML result.</returns>
    internal static object? Deserialize(string text, Type? targetType)
    {
        return Deserialize(text, targetType, null);
    }

    /// <summary>
    /// Loads YAML and converts the result to the requested CLR type, using public instance fields for ordinary objects.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <param name="targetType">The CLR type to create.</param>
    /// <param name="options">The settings that control the operation; pass null to use the applicable defaults.</param>
    /// <returns>The loaded or converted value; null is a valid YAML result.</returns>
    internal static object? Deserialize(string text, Type? targetType, YamlOptions? options)
    {
        YamlProcessor processor = YamlProcessor.Create(options);
        return processor.Deserialize(text, targetType);
    }

    internal static YamlComposerResult ComposeText(string text, YamlOptions? options)
    {
        YamlOptions effective = EffectiveOptions(options);
        ApplyScalarConverterTags(effective);
        YamlComposerResult security = YamlSecurityValidator.ValidateInput(text, effective.Security);
        if (!security.Success)
        {
            return security;
        }

        YamlScannerResult scanner = YamlScanner.Scan(text, effective.Parser);
        YamlParserResult parser = YamlParser.ParseScannerResultWithSecurity(scanner, effective.Parser, effective.Security);
        YamlComposerResult composer = YamlComposer.ComposeParserResult(parser, effective.Composer);
        YamlSchemaResolver.Resolve(composer.Stream, effective.Schema);
        YamlSchemaResolver.ValidateUnknownTags(composer.Stream, effective.Schema, composer);
        if (!parser.SecurityLimitExceeded)
        {
            YamlSecurityValidator.ValidateStream(composer.Stream, effective.Security, composer);
        }

        return composer;
    }

    internal static YamlComposerResult ComposeBytes(ReadOnlySpan<byte> bytes, YamlOptions? options)
    {
        YamlOptions effective = EffectiveOptions(options);
        YamlByteDecodingResult decoded = YamlByteDecoder.Decode(bytes, effective.Security);
        if (decoded.Diagnostic != null)
        {
            YamlComposerResult failed = new YamlComposerResult();
            failed.AddDiagnostic(decoded.Diagnostic);
            return failed;
        }

        return ComposeText(decoded.Text, effective);
    }

    internal static YamlOptions EffectiveOptions(YamlOptions? options)
    {
        if (options == null)
        {
            return new YamlOptions();
        }

        return options.Copy();
    }

    internal static void ApplyScalarConverterTags(YamlOptions options)
    {
        if (options == null || options.Object == null || options.Schema == null
            || options.Object.ScalarConverters == null
            || options.Object.ScalarConverters.Registrations == null
            || options.Schema.CustomTags == null)
        {
            return;
        }

        int index = 0;
        while (index < options.Object.ScalarConverters.Registrations.Count)
        {
            YamlScalarConverterRegistration? registration = options.Object.ScalarConverters.Registrations[index];
            if (registration != null && registration.Tag != null && registration.Tag != ""
                && !options.Schema.CustomTags.ContainsTag(registration.Tag))
            {
                options.Schema.CustomTags.Register(registration.Tag, YamlNodeKind.Scalar);
            }

            index = index + 1;
        }
    }

    internal static YamlDocument DocumentFromValue(object? value)
    {
        return DocumentFromValue(value, null);
    }

    internal static YamlDocument DocumentFromValue(object? value, YamlObjectOptions? options)
    {
        YamlDocument document = new YamlDocument();
        document.SetRoot(NodeFromObjectValue(value, options));
        return document;
    }

    internal static YamlDocument DocumentFromConfiguredValue(object? value, YamlOptions? options)
    {
        YamlOptions effective = EffectiveOptions(options);
        YamlDocument document = new YamlDocument();
        document.SetRoot(NodeFromConfiguredObjectValue(value, effective.Object, effective.Schema.CustomTags));
        return document;
    }

    internal static object? NodeToJsonValue(YamlNode? node)
    {
        if (node == null)
        {
            return null;
        }

        return YamlJsonConverter.ToJsonValue(node);
    }

    internal static object? NodeToObjectValue(YamlNode? node, YamlObjectOptions? options)
    {
        if (node == null)
        {
            return null;
        }

        YamlObjectOptions effective = EffectiveObjectOptions(options);
        return YamlJsonConverter.ToObjectValue(node, effective.ScalarConverters);
    }

    internal static object? NodeToConfiguredValue(YamlNode? node, YamlOptions? options)
    {
        if (node == null)
        {
            return null;
        }

        YamlOptions effective = EffectiveOptions(options);
        return YamlJsonConverter.ToConfiguredObjectValue(node, effective.Object, effective.Schema.CustomTags);
    }

    internal static void CopyDiagnostics(List<YamlDiagnostic>? diagnostics, YamlParseResult? result)
    {
        if (diagnostics == null || result == null)
        {
            return;
        }

        int index = 0;
        while (index < diagnostics.Count)
        {
            result.AddDiagnostic(diagnostics[index]);
            index = index + 1;
        }
    }

    internal static void ThrowIfFailed(YamlParseResult? result)
    {
        if (result == null || result.Success)
        {
            return;
        }

        string message = result.FormatDiagnostics();
        if (message == "")
        {
            message = "YAML operation failed.";
        }

        throw new InvalidOperationException(message);
    }

    internal static YamlDocument ResultDocument(YamlParseResult? result)
    {
        if (result != null)
        {
            YamlDocument? document = result.Value as YamlDocument;
            if (document != null)
            {
                return document;
            }
        }

        return new YamlDocument();
    }

    internal static YamlStream ResultStream(YamlParseResult? result)
    {
        if (result != null)
        {
            YamlStream? stream = result.Value as YamlStream;
            if (stream != null)
            {
                return stream;
            }
        }

        return new YamlStream();
    }

    internal static YamlDocument FirstDocument(YamlStream? stream)
    {
        if (stream != null && stream.Documents.Count > 0)
        {
            return stream.Documents[0];
        }

        return new YamlDocument();
    }

    internal static object? ConvertLoadedValue(object? value, Type? targetType)
    {
        return ConvertLoadedValue(value, targetType, null);
    }

    internal static object? ConvertLoadedValue(object? value, Type? targetType, YamlObjectOptions? options)
    {
        if (targetType == null || targetType.FullName == "System.Object")
        {
            return value;
        }

        Type? nullableType = Nullable.GetUnderlyingType(targetType);
        if (nullableType != null)
        {
            if (value == null)
            {
                return null;
            }

            return ConvertLoadedValue(value, nullableType, options);
        }

        if (value == null)
        {
            return null;
        }

        YamlTypeConverterRegistration? reader = FindReadableTypeConverter(targetType, options);
        if (reader != null)
        {
            return reader.Read(value);
        }

        if (targetType.IsAssignableFrom(value.GetType()))
        {
            return value;
        }

        YamlScalarConverterRegistration? scalarReader = FindReadableScalarConverter(targetType, options);
        if (scalarReader != null)
        {
            return scalarReader.Read(TextValue(value));
        }

        if (targetType.FullName == "System.Byte[]")
        {
            return Convert.FromBase64String(TextValue(value));
        }

        if (targetType.IsArray)
        {
            return ConvertArrayValue(value, targetType, options);
        }

        if (IsDictionaryTarget(targetType))
        {
            return ConvertDictionaryValue(value, targetType, options);
        }

        if (IsListTarget(targetType))
        {
            return ConvertListValue(value, targetType, options);
        }

        if (targetType.IsEnum)
        {
            return Enum.Parse(targetType, TextValue(value), true);
        }

        if (targetType.FullName == "System.Guid")
        {
            return new Guid(TextValue(value));
        }

        if (targetType.FullName == "System.DateTimeOffset")
        {
            return DateTimeOffset.Parse(TextValue(value), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        TypeCode code = Type.GetTypeCode(targetType);
        if (code == TypeCode.String)
        {
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        if (code == TypeCode.Boolean)
        {
            return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
        }

        if (code == TypeCode.Byte)
        {
            return Convert.ToByte(value, CultureInfo.InvariantCulture);
        }

        if (code == TypeCode.SByte)
        {
            return Convert.ToSByte(value, CultureInfo.InvariantCulture);
        }

        if (code == TypeCode.Int16)
        {
            return Convert.ToInt16(value, CultureInfo.InvariantCulture);
        }

        if (code == TypeCode.UInt16)
        {
            return Convert.ToUInt16(value, CultureInfo.InvariantCulture);
        }

        if (code == TypeCode.Int32)
        {
            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        if (code == TypeCode.UInt32)
        {
            return Convert.ToUInt32(value, CultureInfo.InvariantCulture);
        }

        if (code == TypeCode.Int64)
        {
            return Convert.ToInt64(value, CultureInfo.InvariantCulture);
        }

        if (code == TypeCode.UInt64)
        {
            return Convert.ToUInt64(value, CultureInfo.InvariantCulture);
        }

        if (code == TypeCode.Single)
        {
            return Convert.ToSingle(value, CultureInfo.InvariantCulture);
        }

        if (code == TypeCode.Double)
        {
            return Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }

        if (code == TypeCode.Decimal)
        {
            return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }

        if (code == TypeCode.DateTime)
        {
            return DateTime.Parse(TextValue(value), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        if (CanConvertPublicFields(value, targetType, options))
        {
            return ConvertPublicFieldObject(value, targetType, options);
        }

        return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }

    internal static object ConvertArrayValue(object value, Type targetType)
    {
        return ConvertArrayValue(value, targetType, null);
    }

    internal static object ConvertArrayValue(object value, Type targetType, YamlObjectOptions? options)
    {
        Type? elementType = targetType.GetElementType();
        if (elementType == null)
        {
            return value;
        }

        IList? source = value as IList;
        if (source == null)
        {
            Array singleton = Array.CreateInstance(elementType, 1);
            singleton.SetValue(ConvertLoadedValue(value, elementType, options), 0);
            return singleton;
        }

        Array array = Array.CreateInstance(elementType, source.Count);
        int index = 0;
        while (index < source.Count)
        {
            array.SetValue(ConvertLoadedValue(source[index], elementType, options), index);
            index = index + 1;
        }

        return array;
    }

    internal static object ConvertListValue(object value, Type targetType)
    {
        return ConvertListValue(value, targetType, null);
    }

    internal static object ConvertListValue(object value, Type targetType, YamlObjectOptions? options)
    {
        IList? source = value as IList;
        if (source == null)
        {
            return value;
        }

        IList? target = Activator.CreateInstance(targetType) as IList;
        if (target == null)
        {
            return value;
        }

        Type itemType = ListItemType(targetType);
        int index = 0;
        while (index < source.Count)
        {
            target.Add(ConvertLoadedValue(source[index], itemType, options));
            index = index + 1;
        }

        return target;
    }

    internal static object ConvertDictionaryValue(object value, Type targetType)
    {
        return ConvertDictionaryValue(value, targetType, null);
    }

    internal static object ConvertDictionaryValue(object value, Type targetType, YamlObjectOptions? options)
    {
        IDictionary? source = value as IDictionary;
        if (source == null)
        {
            return value;
        }

        IDictionary? target = Activator.CreateInstance(targetType) as IDictionary;
        if (target == null)
        {
            return value;
        }

        Type keyType = DictionaryKeyType(targetType);
        Type valueType = DictionaryValueType(targetType);
        foreach (DictionaryEntry entry in source)
        {
            object? key = ConvertLoadedValue(entry.Key, keyType, options);
            object? item = ConvertLoadedValue(entry.Value, valueType, options);
            if (key == null)
            {
                key = "";
            }

            target.Add(key, item);
        }

        return target;
    }

    internal static YamlNode NodeFromObjectValue(object? value, YamlObjectOptions? options)
    {
        if (value == null)
        {
            return YamlJsonConverter.NullNode();
        }

        Type valueType = value.GetType();
        YamlTypeConverterRegistration? writer = FindWritableTypeConverter(valueType, options);
        if (writer != null)
        {
            object? converted = writer.Write(value);
            if (!object.ReferenceEquals(converted, value))
            {
                return NodeFromObjectValue(converted, options);
            }
        }

        YamlScalarConverterRegistration? scalarWriter = FindWritableScalarConverter(valueType, options);
        if (scalarWriter != null)
        {
            YamlNode scalar = YamlNode.Scalar(scalarWriter.Write(value), scalarWriter.PreferredStyle, YamlSourceSpan.Empty());
            scalar.Tag = scalarWriter.Tag == null ? "" : scalarWriter.Tag;
            return scalar;
        }

        if (IsScalarObjectValue(value, valueType))
        {
            return YamlJsonConverter.FromJsonValue(value);
        }

        IDictionary? dictionary = value as IDictionary;
        if (dictionary != null)
        {
            return MappingFromObjectDictionary(dictionary, options);
        }

        IList? list = value as IList;
        if (list != null)
        {
            return SequenceFromObjectList(list, options);
        }

        YamlObjectOptions effective = EffectiveObjectOptions(options);
        if (effective.IncludePublicFields)
        {
            return MappingFromPublicFields(value, effective);
        }

        return YamlJsonConverter.FromJsonValue(value);
    }

    internal static YamlNode NodeFromConfiguredObjectValue(object? value, YamlObjectOptions options, YamlTagRegistry? typeTags)
    {
        if (value == null)
        {
            return YamlJsonConverter.NullNode();
        }

        Dictionary<object, object> path = new Dictionary<object, object>(ReferenceEqualityComparer.Instance);
        return NodeFromConfiguredObjectValueWithPath(value, options, typeTags, path);
    }

    internal static YamlNode NodeFromConfiguredObjectValueWithPath(object? value, YamlObjectOptions options, YamlTagRegistry? typeTags, Dictionary<object, object> path)
    {
        if (value == null)
        {
            return YamlJsonConverter.NullNode();
        }

        if (path.ContainsKey(value))
        {
            throw YamlException.Create("YAML object conversion cannot process a direct object cycle.");
        }

        path[value] = value;
        YamlNode node = BuildConfiguredObjectValue(value, options, typeTags, path);
        path.Remove(value);
        return node;
    }

    internal static YamlNode BuildConfiguredObjectValue(object value, YamlObjectOptions options, YamlTagRegistry? typeTags, Dictionary<object, object> path)
    {

        Type valueType = value.GetType();
        YamlTagRegistration? typeTag = FindWritableTypeTag(valueType, options, typeTags);
        YamlTypeConverterRegistration? writer = FindWritableTypeConverter(valueType, options);
        if (writer != null)
        {
            object? converted = writer.Write(value);
            if (!object.ReferenceEquals(converted, value))
            {
                YamlNode convertedNode = NodeFromConfiguredObjectValueWithPath(converted, options, typeTags, path);
                return ApplyTypeTag(convertedNode, typeTag);
            }
        }

        YamlScalarConverterRegistration? scalarWriter = FindWritableScalarConverter(valueType, options);
        if (scalarWriter != null)
        {
            YamlNode scalar = YamlNode.Scalar(scalarWriter.Write(value), scalarWriter.PreferredStyle, YamlSourceSpan.Empty());
            scalar.Tag = scalarWriter.Tag == null ? "" : scalarWriter.Tag;
            return ApplyTypeTag(scalar, typeTag);
        }

        YamlNode node;
        if (IsScalarObjectValue(value, valueType))
        {
            node = YamlJsonConverter.FromJsonValue(value);
            return ApplyTypeTag(node, typeTag);
        }

        IDictionary? dictionary = value as IDictionary;
        if (dictionary != null)
        {
            node = MappingFromConfiguredDictionary(dictionary, options, typeTags, path);
            return ApplyTypeTag(node, typeTag);
        }

        IList? list = value as IList;
        if (list != null)
        {
            node = SequenceFromConfiguredList(list, options, typeTags, path);
            return ApplyTypeTag(node, typeTag);
        }

        if (options.IncludePublicFields)
        {
            node = MappingFromConfiguredFields(value, options, typeTags, path);
            return ApplyTypeTag(node, typeTag);
        }

        node = YamlJsonConverter.FromJsonValue(value);
        return ApplyTypeTag(node, typeTag);
    }

    internal static YamlNode SequenceFromConfiguredList(IList list, YamlObjectOptions options, YamlTagRegistry? typeTags, Dictionary<object, object> path)
    {
        YamlNode node = YamlNodeFactory.Sequence();
        foreach (object? item in list)
        {
            node.AddSequenceItem(NodeFromConfiguredObjectValueWithPath(item, options, typeTags, path));
        }

        return node;
    }

    internal static YamlNode MappingFromConfiguredDictionary(IDictionary dictionary, YamlObjectOptions options, YamlTagRegistry? typeTags, Dictionary<object, object> path)
    {
        YamlNode node = YamlNodeFactory.Mapping();
        foreach (DictionaryEntry entry in dictionary)
        {
            string key = YamlJsonConverter.JsonKeyToString(entry.Key);
            node.AddMappingValue(key, NodeFromConfiguredObjectValueWithPath(entry.Value, options, typeTags, path));
        }

        return node;
    }

    internal static YamlNode MappingFromConfiguredFields(object value, YamlObjectOptions options, YamlTagRegistry? typeTags, Dictionary<object, object> path)
    {
        YamlNode node = YamlNodeFactory.Mapping();
        FieldInfo[] fields = PublicInstanceFields(value.GetType());
        int index = 0;
        while (index < fields.Length)
        {
            FieldInfo field = fields[index];
            object? fieldValue = field.GetValue(value);
            if (!ShouldSkipObjectFieldValue(fieldValue, field.FieldType, options))
            {
                string key = YamlFieldNaming.Apply(field.Name, options.NamingConvention);
                node.AddMappingValue(key, NodeFromConfiguredObjectValueWithPath(fieldValue, options, typeTags, path));
            }

            index = index + 1;
        }

        return node;
    }

    internal static YamlTagRegistration? FindWritableTypeTag(Type valueType, YamlObjectOptions options, YamlTagRegistry? typeTags)
    {
        if (!options.UseTypeTags || typeTags == null || typeTags.Registrations == null)
        {
            return null;
        }

        int index = 0;
        while (index < typeTags.Registrations.Count)
        {
            YamlTagRegistration? registration = typeTags.Registrations[index];
            if (registration != null && registration.MatchesType(valueType) && registration.Tag != null && registration.Tag != "")
            {
                return registration;
            }

            index = index + 1;
        }

        return null;
    }

    internal static YamlNode ApplyTypeTag(YamlNode node, YamlTagRegistration? registration)
    {
        if (registration == null)
        {
            return node;
        }

        if (registration.NodeKind != YamlNodeKind.Unknown && registration.NodeKind != node.Kind)
        {
            return node;
        }

        node.Tag = registration.Tag == null ? "" : registration.Tag;
        return node;
    }

    internal static bool IsScalarObjectValue(object value, Type valueType)
    {
        if (valueType.FullName == "System.Byte[]")
        {
            return true;
        }

        if (valueType.FullName == "System.DateTime")
        {
            return true;
        }

        if (valueType.FullName == "System.DateTimeOffset")
        {
            return true;
        }

        if (valueType.FullName == "System.Guid")
        {
            return true;
        }

        if (valueType.IsEnum)
        {
            return true;
        }

        TypeCode code = Convert.GetTypeCode(value);
        if (code != TypeCode.Object && code != TypeCode.Empty && code != TypeCode.DBNull)
        {
            return true;
        }

        return false;
    }

    internal static YamlObjectOptions EffectiveObjectOptions(YamlObjectOptions? options)
    {
        if (options == null)
        {
            return new YamlObjectOptions();
        }

        return options.Copy();
    }

    internal static YamlNode SequenceFromObjectList(IList list, YamlObjectOptions? options)
    {
        YamlNode node = YamlNodeFactory.Sequence();
        foreach (object? item in list)
        {
            node.AddSequenceItem(NodeFromObjectValue(item, options));
        }

        return node;
    }

    internal static YamlNode MappingFromObjectDictionary(IDictionary dictionary, YamlObjectOptions? options)
    {
        YamlNode node = YamlNodeFactory.Mapping();
        foreach (DictionaryEntry entry in dictionary)
        {
            string key = YamlJsonConverter.JsonKeyToString(entry.Key);
            node.AddMappingValue(key, NodeFromObjectValue(entry.Value, options));
        }

        return node;
    }

    internal static YamlNode MappingFromPublicFields(object value, YamlObjectOptions options)
    {
        YamlNode node = YamlNodeFactory.Mapping();
        FieldInfo[] fields = PublicInstanceFields(value.GetType());
        int index = 0;
        while (index < fields.Length)
        {
            FieldInfo field = fields[index];
            object? fieldValue = field.GetValue(value);
            if (!ShouldSkipObjectFieldValue(fieldValue, field.FieldType, options))
            {
                string key = YamlFieldNaming.Apply(field.Name, options.NamingConvention);
                node.AddMappingValue(key, NodeFromObjectValue(fieldValue, options));
            }

            index = index + 1;
        }

        return node;
    }

    internal static bool CanConvertPublicFields(object? value, Type? targetType, YamlObjectOptions? options)
    {
        if (value == null || targetType == null)
        {
            return false;
        }

        YamlObjectOptions effective = EffectiveObjectOptions(options);
        if (!effective.IncludePublicFields)
        {
            return false;
        }

        IDictionary? source = value as IDictionary;
        if (source == null)
        {
            return false;
        }

        if (targetType.FullName == null || targetType.FullName.StartsWith("System."))
        {
            return false;
        }

        return true;
    }

    internal static object ConvertPublicFieldObject(object value, Type targetType, YamlObjectOptions? options)
    {
        IDictionary? source = value as IDictionary;
        if (source == null)
        {
            return value;
        }

        object? target = Activator.CreateInstance(targetType);
        if (target == null)
        {
            return value;
        }

        YamlObjectOptions effective = EffectiveObjectOptions(options);
        FieldInfo[] fields = PublicInstanceFields(targetType);
        ValidateUnknownObjectMembers(source, fields, effective);
        ValidateRequiredObjectMembers(source, effective);
        int index = 0;
        while (index < fields.Length)
        {
            FieldInfo field = fields[index];
            string key = YamlFieldNaming.Apply(field.Name, effective.NamingConvention);
            if (!field.IsInitOnly && source.Contains(key))
            {
                object? fieldValue = ConvertLoadedValue(source[key], field.FieldType, effective);
                if (!ShouldSkipLoadedFieldValue(fieldValue, field.FieldType, effective))
                {
                    field.SetValue(target, fieldValue);
                }
            }

            index = index + 1;
        }

        return target;
    }

    internal static bool ShouldSkipObjectFieldValue(object? value, Type fieldType, YamlObjectOptions? options)
    {
        if (options == null)
        {
            return false;
        }

        if (options.IgnoreNulls && value == null)
        {
            return true;
        }

        if (options.IgnoreDefaults && IsDefaultFieldValue(value, fieldType))
        {
            return true;
        }

        return false;
    }

    internal static bool ShouldSkipLoadedFieldValue(object? value, Type fieldType, YamlObjectOptions? options)
    {
        return ShouldSkipObjectFieldValue(value, fieldType, options);
    }

    internal static bool IsDefaultFieldValue(object? value, Type? fieldType)
    {
        if (fieldType == null)
        {
            return value == null;
        }

        if (value == null)
        {
            if (!fieldType.IsValueType)
            {
                return true;
            }

            if (Nullable.GetUnderlyingType(fieldType) != null)
            {
                return true;
            }

            return false;
        }

        if (!fieldType.IsValueType)
        {
            return false;
        }

        object? defaultValue = Activator.CreateInstance(fieldType);
        if (defaultValue == null)
        {
            return false;
        }

        return value.Equals(defaultValue);
    }

    internal static YamlTypeConverterRegistration? FindReadableTypeConverter(Type targetType, YamlObjectOptions? options)
    {
        YamlObjectOptions effective = EffectiveObjectOptions(options);
        YamlTypeConverterRegistration? registration = effective.TypeConverters.FindByType(targetType);
        if (registration != null && registration.CanRead())
        {
            return registration;
        }

        return null;
    }

    internal static YamlTypeConverterRegistration? FindWritableTypeConverter(Type valueType, YamlObjectOptions? options)
    {
        YamlObjectOptions effective = EffectiveObjectOptions(options);
        YamlTypeConverterRegistration? registration = effective.TypeConverters.FindByType(valueType);
        if (registration != null && registration.CanWrite())
        {
            return registration;
        }

        return null;
    }

    internal static YamlScalarConverterRegistration? FindReadableScalarConverter(Type targetType, YamlObjectOptions? options)
    {
        YamlObjectOptions effective = EffectiveObjectOptions(options);
        YamlScalarConverterRegistration? registration = effective.ScalarConverters.FindByType(targetType);
        if (registration != null && registration.CanRead())
        {
            return registration;
        }

        return null;
    }

    internal static YamlScalarConverterRegistration? FindWritableScalarConverter(Type valueType, YamlObjectOptions? options)
    {
        YamlObjectOptions effective = EffectiveObjectOptions(options);
        YamlScalarConverterRegistration? registration = effective.ScalarConverters.FindByType(valueType);
        if (registration != null && registration.CanWrite())
        {
            return registration;
        }

        return null;
    }

    internal static void ValidateUnknownObjectMembers(IDictionary source, FieldInfo[] fields, YamlObjectOptions options)
    {
        if (source == null || fields == null || options == null || !options.ErrorOnUnknownMembers)
        {
            return;
        }

        foreach (DictionaryEntry entry in source)
        {
            string key = YamlJsonConverter.JsonKeyToString(entry.Key);
            if (!IsKnownObjectMember(key, fields, options))
            {
                throw new InvalidOperationException("Unknown YAML member '" + key + "'.");
            }
        }
    }

    internal static bool IsKnownObjectMember(string key, FieldInfo[] fields, YamlObjectOptions options)
    {
        int index = 0;
        while (index < fields.Length)
        {
            string fieldKey = YamlFieldNaming.Apply(fields[index].Name, options.NamingConvention);
            if (fieldKey == key)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static void ValidateRequiredObjectMembers(IDictionary source, YamlObjectOptions options)
    {
        if (source == null || options == null || !options.RequireConfiguredMembers)
        {
            return;
        }

        int index = 0;
        while (index < options.RequiredMembers.Count)
        {
            string memberName = options.RequiredMembers[index];
            string key = YamlFieldNaming.Apply(memberName, options.NamingConvention);
            if (!source.Contains(memberName) && !source.Contains(key))
            {
                throw new InvalidOperationException("Missing required YAML member '" + key + "'.");
            }

            index = index + 1;
        }
    }

    internal static FieldInfo[] PublicInstanceFields(Type targetType)
    {
        FieldInfo[] fields = targetType.GetFields(BindingFlags.Instance | BindingFlags.Public);
        Array.Sort(fields, CompareFieldInfo);
        return fields;
    }

    internal static int CompareFieldInfo(FieldInfo left, FieldInfo right)
    {
        if (left == null && right == null)
        {
            return 0;
        }

        if (left == null)
        {
            return -1;
        }

        if (right == null)
        {
            return 1;
        }

        if (left.MetadataToken < right.MetadataToken)
        {
            return -1;
        }

        if (left.MetadataToken > right.MetadataToken)
        {
            return 1;
        }

        return string.CompareOrdinal(left.Name, right.Name);
    }

    internal static bool IsListTarget(Type targetType)
    {
        string name = targetType.FullName == null ? "" : targetType.FullName;
        return name.StartsWith("System.Collections.Generic.List`1");
    }

    internal static bool IsDictionaryTarget(Type targetType)
    {
        string name = targetType.FullName == null ? "" : targetType.FullName;
        return name.StartsWith("System.Collections.Generic.Dictionary`2");
    }

    internal static Type ListItemType(Type targetType)
    {
        Type[] arguments = targetType.GetGenericArguments();
        if (arguments.Length > 0)
        {
            return arguments[0];
        }

        return targetType;
    }

    internal static Type DictionaryKeyType(Type targetType)
    {
        Type[] arguments = targetType.GetGenericArguments();
        if (arguments.Length > 0)
        {
            return arguments[0];
        }

        return targetType;
    }

    internal static Type DictionaryValueType(Type targetType)
    {
        Type[] arguments = targetType.GetGenericArguments();
        if (arguments.Length > 1)
        {
            return arguments[1];
        }

        return targetType;
    }

    internal static string TextValue(object value)
    {
        if (value == null)
        {
            return "";
        }

        string? text = Convert.ToString(value, CultureInfo.InvariantCulture);
        return text == null ? "" : text;
    }
}
