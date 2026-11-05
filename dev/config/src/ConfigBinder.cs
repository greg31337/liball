using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace config;

internal class ConfigBoundValue
{
    internal bool HasValue;
    internal object? Value;

    internal ConfigBoundValue(bool hasValue, object? value)
    {
        HasValue = hasValue;
        Value = value;
    }
}

internal class ConfigBinder
{
    internal static object Bind(Config config, Type targetType)
    {
        return Bind(config, "", targetType);
    }

    internal static object Bind(Config config, Type targetType, ConfigFieldMap fieldMap)
    {
        return Bind(config, "", targetType, fieldMap);
    }

    internal static object Bind(Config config, string path, Type targetType)
    {
        return Bind(config, path, targetType, new ConfigFieldMap());
    }

    internal static object Bind(Config config, string path, Type targetType, ConfigFieldMap fieldMap)
    {
        RequireFieldMap(fieldMap);
        RequireRootTargetType(targetType, false);
        object? target = Activator.CreateInstance(targetType);
        if (target == null)
        {
            throw ConfigExceptionFactory.ConversionFromValue("could not create config binding target: " + TypeName(targetType), path, config.Value(path));
        }

        Bind(config, path, target, fieldMap);
        return target;
    }

    internal static void Bind(Config config, object target)
    {
        Bind(config, "", target);
    }

    internal static void Bind(Config config, object target, ConfigFieldMap fieldMap)
    {
        Bind(config, "", target, fieldMap);
    }

    internal static void Bind(Config config, string path, object target)
    {
        Bind(config, path, target, new ConfigFieldMap());
    }

    internal static void Bind(Config config, string path, object target, ConfigFieldMap fieldMap)
    {
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        RequireFieldMap(fieldMap);
        RequireRootTargetType(target.GetType(), true);
        BindFields(config, path, path, "", target, fieldMap);
    }

    internal static void BindFields(Config config, string rootPath, string currentPath, string targetPath, object target, ConfigFieldMap fieldMap)
    {
        FieldInfo[] fields = target.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

        int index = 0;
        while (index < fields.Length)
        {
            BindField(config, rootPath, currentPath, targetPath, target, fields[index], fieldMap);
            index = index + 1;
        }
    }

    internal static void BindField(Config config, string path, object target, FieldInfo field)
    {
        BindField(config, path, path, "", target, field, new ConfigFieldMap());
    }

    internal static void BindField(Config config, string rootPath, string currentPath, string targetPath, object target, FieldInfo field, ConfigFieldMap fieldMap)
    {
        string targetFieldPath = TargetChildPath(targetPath, field.Name);
        string defaultPath = ChildPath(currentPath, field.Name, config.Delimiter);
        string mappedPath = fieldMap.Resolve(targetFieldPath, "");
        string configPath = defaultPath;
        if (mappedPath.Length > 0)
        {
            configPath = MappedPath(rootPath, mappedPath, config.Delimiter);
        }

        ConfigValue value = ConfigValueAt(config, configPath);
        if (value.Kind == ConfigValueKind.Missing || value.Kind == ConfigValueKind.Null)
        {
            return;
        }

        SetFieldValue(target, field, value, fieldMap, targetFieldPath, config.Delimiter, config.CaseMode);
    }

    internal static void SetFieldValue(object target, FieldInfo field, ConfigValue value)
    {
        SetFieldValue(target, field, value, new ConfigFieldMap(), "", ".", ConfigCaseMode.Sensitive);
    }

    internal static void SetFieldValue(object target, FieldInfo field, ConfigValue value, ConfigFieldMap fieldMap, string targetPath, string delimiter, ConfigCaseMode caseMode)
    {
        ConfigBoundValue bound = BoundValue(field.FieldType, value, field.GetValue(target), fieldMap, targetPath, delimiter, caseMode);
        if (!bound.HasValue)
        {
            return;
        }

        field.SetValue(target, bound.Value);
    }

    internal static ConfigBoundValue BoundValue(Type targetType, ConfigValue value, object? existingValue)
    {
        return BoundValue(targetType, value, existingValue, new ConfigFieldMap(), "", ".", ConfigCaseMode.Sensitive);
    }

    internal static ConfigBoundValue BoundValue(Type targetType, ConfigValue value, object? existingValue, ConfigFieldMap fieldMap, string targetPath, string delimiter, ConfigCaseMode caseMode)
    {
        if (value.Kind == ConfigValueKind.Missing || value.Kind == ConfigValueKind.Null)
        {
            return MissingBoundValue();
        }

        string typeName = TypeName(targetType);
        if (typeName == "System.String")
        {
            return FoundBoundValue(ConfigConversions.StringValue(value));
        }

        if (typeName == "System.Boolean")
        {
            RequireConversion(ConfigConversions.IsBoolValue(value), targetPath, targetType, value);
            return FoundBoundValue(ConfigConversions.BoolValue(value, false));
        }

        if (typeName == "System.Int32")
        {
            RequireConversion(ConfigConversions.IsIntValue(value), targetPath, targetType, value);
            return FoundBoundValue(ConfigConversions.IntValue(value, 0));
        }

        if (typeName == "System.Int64")
        {
            RequireConversion(ConfigConversions.IsLongValue(value), targetPath, targetType, value);
            return FoundBoundValue(ConfigConversions.LongValue(value, 0L));
        }

        if (typeName == "System.Decimal")
        {
            RequireConversion(ConfigConversions.IsDecimalValue(value), targetPath, targetType, value);
            return FoundBoundValue(ConfigConversions.DecimalValue(value, 0m));
        }

        if (typeName == "System.Double")
        {
            RequireConversion(ConfigConversions.IsDoubleValue(value), targetPath, targetType, value);
            return FoundBoundValue(ConfigConversions.DoubleValue(value, 0d));
        }

        if (typeName == "System.TimeSpan")
        {
            RequireConversion(ConfigConversions.IsTimeSpanValue(value), targetPath, targetType, value);
            return FoundBoundValue(ConfigConversions.TimeSpanValue(value, TimeSpan.Zero));
        }

        if (targetType.IsEnum)
        {
            RequireConversion(ConfigConversions.IsEnumValue(value, targetType), targetPath, targetType, value);
            return FoundBoundValue(ConfigConversions.EnumValue(value, targetType, ConfigConversions.EnumDefault(targetType)));
        }

        if (targetType.IsArray)
        {
            if (value.Kind != ConfigValueKind.Array || value.ArrayValue == null)
            {
                throw ConversionFailure(targetPath, targetType, value);
            }

            return BoundArrayValue(targetType, value.ArrayValue, fieldMap, targetPath, delimiter, caseMode);
        }

        if (IsListType(targetType))
        {
            if (value.Kind != ConfigValueKind.Array || value.ArrayValue == null)
            {
                throw ConversionFailure(targetPath, targetType, value);
            }

            return BoundListValue(targetType, value.ArrayValue, fieldMap, targetPath, delimiter, caseMode);
        }

        if (CanBindObjectType(targetType, existingValue))
        {
            if (value.Kind != ConfigValueKind.Object || value.ObjectValue == null)
            {
                throw ConversionFailure(targetPath, targetType, value);
            }

            return BoundObjectValue(targetType, value, existingValue, fieldMap, targetPath, delimiter, caseMode);
        }

        throw UnsupportedType(targetPath, targetType);
    }

    internal static ConfigBoundValue BoundArrayValue(Type arrayType, ConfigArray values)
    {
        return BoundArrayValue(arrayType, values, new ConfigFieldMap(), "", ".", ConfigCaseMode.Sensitive);
    }

    internal static ConfigBoundValue BoundArrayValue(Type arrayType, ConfigArray values, ConfigFieldMap fieldMap, string targetPath, string delimiter, ConfigCaseMode caseMode)
    {
        Type? elementType = arrayType.GetElementType();
        if (elementType == null)
        {
            throw UnsupportedType(targetPath, arrayType);
        }

        RequireSupportedValueType(elementType, targetPath);
        Array array = Array.CreateInstance(elementType, values.Count());

        int index = 0;
        while (index < values.Count())
        {
            ConfigBoundValue bound = BoundValue(elementType, values.Get(index), null, fieldMap, targetPath, delimiter, caseMode);
            if (!bound.HasValue)
            {
                throw ConversionFailure(targetPath, elementType, values.Get(index));
            }

            array.SetValue(bound.Value, index);
            index = index + 1;
        }

        return FoundBoundValue(array);
    }

    internal static ConfigBoundValue BoundListValue(Type listType, ConfigArray values)
    {
        return BoundListValue(listType, values, new ConfigFieldMap(), "", ".", ConfigCaseMode.Sensitive);
    }

    internal static ConfigBoundValue BoundListValue(Type listType, ConfigArray values, ConfigFieldMap fieldMap, string targetPath, string delimiter, ConfigCaseMode caseMode)
    {
        Type[] arguments = listType.GetGenericArguments();
        if (arguments.Length != 1)
        {
            throw UnsupportedType(targetPath, listType);
        }

        RequireSupportedValueType(arguments[0], targetPath);
        object? created = Activator.CreateInstance(listType);
        IList? list = created as IList;
        if (list == null)
        {
            throw UnsupportedType(targetPath, listType);
        }

        int index = 0;
        while (index < values.Count())
        {
            ConfigBoundValue bound = BoundValue(arguments[0], values.Get(index), null, fieldMap, targetPath, delimiter, caseMode);
            if (!bound.HasValue)
            {
                throw ConversionFailure(targetPath, arguments[0], values.Get(index));
            }

            list.Add(bound.Value);
            index = index + 1;
        }

        return FoundBoundValue(list);
    }

    internal static ConfigBoundValue BoundObjectValue(Type targetType, ConfigValue value, object? existingValue)
    {
        return BoundObjectValue(targetType, value, existingValue, new ConfigFieldMap(), "", ".", ConfigCaseMode.Sensitive);
    }

    internal static ConfigBoundValue BoundObjectValue(Type targetType, ConfigValue value, object? existingValue, ConfigFieldMap fieldMap, string targetPath, string delimiter, ConfigCaseMode caseMode)
    {
        if (!CanBindObjectType(targetType, existingValue))
        {
            throw UnsupportedType(targetPath, targetType);
        }

        object? target = existingValue;
        if (target == null)
        {
            target = Activator.CreateInstance(targetType);
        }

        if (target == null)
        {
            throw ConfigExceptionFactory.ConversionFromValue("could not create config binding target for field " + FieldLabel(targetPath) + ": " + TypeName(targetType), targetPath, value);
        }

        if (value.ObjectValue == null)
        {
            throw ConversionFailure(targetPath, targetType, value);
        }

        BindObjectValue(value.ObjectValue, target, fieldMap, targetPath, delimiter, caseMode);
        return FoundBoundValue(target);
    }

    internal static void BindObjectValue(ConfigMap map, object target)
    {
        BindObjectValue(map, target, new ConfigFieldMap(), "", ".", ConfigCaseMode.Sensitive);
    }

    internal static void BindObjectValue(ConfigMap map, object target, ConfigFieldMap fieldMap, string targetPath, string delimiter, ConfigCaseMode caseMode)
    {
        FieldInfo[] fields = target.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

        int index = 0;
        while (index < fields.Length)
        {
            string targetFieldPath = TargetChildPath(targetPath, fields[index].Name);
            string configPath = RelativeConfigPath(fieldMap, targetPath, targetFieldPath, fields[index].Name, delimiter, caseMode);
            ConfigValue value = MapValue(map, configPath, delimiter, caseMode);
            if (value.Kind != ConfigValueKind.Missing && value.Kind != ConfigValueKind.Null)
            {
                SetFieldValue(target, fields[index], value, fieldMap, targetFieldPath, delimiter, caseMode);
            }

            index = index + 1;
        }
    }

    internal static bool CanBindObjectType(Type targetType, object? existingValue)
    {
        if (!targetType.IsClass || targetType.IsAbstract)
        {
            return false;
        }

        if (TypeName(targetType) == "System.Object" || IsListType(targetType))
        {
            return false;
        }

        FieldInfo[] fields = targetType.GetFields(BindingFlags.Public | BindingFlags.Instance);
        if (fields.Length == 0)
        {
            return false;
        }

        if (existingValue != null)
        {
            return true;
        }

        ConstructorInfo? constructor = targetType.GetConstructor(new Type[0]);
        return constructor != null;
    }

    internal static bool IsListType(Type targetType)
    {
        if (!targetType.IsGenericType)
        {
            return false;
        }

        Type definition = targetType.GetGenericTypeDefinition();
        return TypeName(definition) == "System.Collections.Generic.List`1";
    }

    internal static bool IsSupportedValueType(Type targetType)
    {
        string typeName = TypeName(targetType);
        if (typeName == "System.String" || typeName == "System.Boolean" || typeName == "System.Int32" || typeName == "System.Int64" || typeName == "System.Decimal" || typeName == "System.Double" || typeName == "System.TimeSpan")
        {
            return true;
        }

        if (targetType.IsEnum)
        {
            return true;
        }

        if (targetType.IsArray)
        {
            Type? elementType = targetType.GetElementType();
            if (elementType == null)
            {
                return false;
            }

            return IsSupportedValueType(elementType);
        }

        if (IsListType(targetType))
        {
            Type[] arguments = targetType.GetGenericArguments();
            if (arguments.Length != 1)
            {
                return false;
            }

            return IsSupportedValueType(arguments[0]);
        }

        return CanBindObjectType(targetType, null);
    }

    internal static void RequireSupportedValueType(Type targetType, string targetPath)
    {
        if (!IsSupportedValueType(targetType))
        {
            throw UnsupportedType(targetPath, targetType);
        }
    }

    internal static void RequireRootTargetType(Type targetType, bool existingTarget)
    {
        object? existing = null;
        if (existingTarget)
        {
            existing = new object();
        }

        if (!CanBindObjectType(targetType, existing))
        {
            throw ConfigExceptionFactory.ConversionWithoutSource("unsupported config binding target type: " + TypeName(targetType), "");
        }
    }

    internal static void RequireFieldMap(ConfigFieldMap fieldMap)
    {
        if (fieldMap == null)
        {
            throw new ArgumentNullException("fieldMap");
        }
    }

    internal static void RequireConversion(bool convertible, string targetPath, Type targetType, ConfigValue value)
    {
        if (!convertible)
        {
            throw ConversionFailure(targetPath, targetType, value);
        }
    }

    internal static ConfigConversionException ConversionFailure(string targetPath, Type targetType, ConfigValue value)
    {
        return ConfigExceptionFactory.ConversionFromValue("could not convert config value for field " + FieldLabel(targetPath) + " to " + TypeName(targetType), targetPath, value);
    }

    internal static ConfigConversionException UnsupportedType(string targetPath, Type targetType)
    {
        return ConfigExceptionFactory.ConversionWithoutSource("unsupported config binding type for field " + FieldLabel(targetPath) + ": " + TypeName(targetType), targetPath);
    }

    internal static string FieldLabel(string targetPath)
    {
        if (targetPath.Length == 0)
        {
            return "(root)";
        }

        return targetPath;
    }

    internal static ConfigValue ConfigValueAt(Config config, string path)
    {
        if (config.CaseMode == ConfigCaseMode.Sensitive)
        {
            return config.Value(path);
        }

        string resolvedPath = ResolveAlias(config, path);
        ConfigValue value = ConfigValue.Missing();
        if (config.Root.Kind == ConfigValueKind.Object && config.Root.ObjectValue != null)
        {
            value = MapValue(config.Root.ObjectValue, resolvedPath, config.Delimiter, config.CaseMode);
        }
        else if (ConfigPath.Normalize(resolvedPath, config.Delimiter).Length == 0)
        {
            value = config.Root.Copy();
        }

        List<string> stack = new List<string>();
        return ConfigResolver.ResolveValueAt(config, value, 0, stack, ConfigPath.Normalize(resolvedPath, config.Delimiter), null);
    }

    internal static string ResolveAlias(Config config, string path)
    {
        string resolved = config.Aliases.Resolve(path, config.Delimiter);
        if (resolved != ConfigPath.Normalize(path, config.Delimiter))
        {
            return resolved;
        }

        string normalized = ConfigPath.Normalize(path, config.Delimiter);
        string[] keys = new string[config.Aliases.Paths.Keys.Count];
        config.Aliases.Paths.Keys.CopyTo(keys, 0);
        int index = 0;
        while (index < keys.Length)
        {
            if (string.Equals(keys[index], normalized, StringComparison.OrdinalIgnoreCase))
            {
                return config.Aliases.Resolve(keys[index], config.Delimiter);
            }

            index = index + 1;
        }

        return normalized;
    }

    internal static ConfigValue MapValue(ConfigMap map, string path, string delimiter, ConfigCaseMode caseMode)
    {
        ConfigPath parsed = ConfigPath.Parse(path, delimiter);
        if (parsed.Count() == 0)
        {
            return ConfigValue.Object(map);
        }

        ConfigMap current = map;
        int index = 0;
        while (index < parsed.Count())
        {
            ConfigValue value = DirectMapValue(current, parsed.Segment(index), caseMode);
            if (index == parsed.Count() - 1)
            {
                return value;
            }

            if (value.Kind != ConfigValueKind.Object || value.ObjectValue == null)
            {
                return ConfigValue.Missing();
            }

            current = value.ObjectValue;
            index = index + 1;
        }

        return ConfigValue.Missing();
    }

    internal static ConfigValue DirectMapValue(ConfigMap map, string key, ConfigCaseMode caseMode)
    {
        ConfigValue value = map.Get(key);
        if (value.Kind != ConfigValueKind.Missing || caseMode == ConfigCaseMode.Sensitive)
        {
            return value;
        }

        string[] keys = map.Keys();
        int index = 0;
        while (index < keys.Length)
        {
            if (string.Equals(keys[index], key, StringComparison.OrdinalIgnoreCase))
            {
                return map.Get(keys[index]);
            }

            index = index + 1;
        }

        return ConfigValue.Missing();
    }

    internal static string RelativeConfigPath(ConfigFieldMap fieldMap, string parentTargetPath, string targetFieldPath, string fieldName, string delimiter, ConfigCaseMode caseMode)
    {
        string mapped = fieldMap.Resolve(targetFieldPath, "");
        if (mapped.Length == 0)
        {
            return ConfigPath.EscapeSegment(fieldName, delimiter);
        }

        string normalized = ConfigPath.Normalize(mapped, delimiter);
        string parentMapped = fieldMap.Resolve(parentTargetPath, "");
        if (parentMapped.Length == 0)
        {
            parentMapped = TargetLeaf(parentTargetPath);
        }

        parentMapped = ConfigPath.Normalize(parentMapped, delimiter);
        if (parentMapped.Length == 0)
        {
            return normalized;
        }

        string prefix = parentMapped + ConfigPath.CleanDelimiter(delimiter);
        StringComparison comparison = StringComparison.Ordinal;
        if (caseMode == ConfigCaseMode.Insensitive)
        {
            comparison = StringComparison.OrdinalIgnoreCase;
        }

        if (normalized.StartsWith(prefix, comparison))
        {
            return normalized.Substring(prefix.Length);
        }

        return normalized;
    }

    internal static string TargetLeaf(string targetPath)
    {
        int separator = targetPath.LastIndexOf(".", StringComparison.Ordinal);
        if (separator < 0)
        {
            return targetPath;
        }

        return targetPath.Substring(separator + 1);
    }

    internal static string TypeName(Type type)
    {
        if (type.FullName == null)
        {
            return "";
        }

        return type.FullName;
    }

    internal static ConfigBoundValue FoundBoundValue(object value)
    {
        return new ConfigBoundValue(true, value);
    }

    internal static ConfigBoundValue MissingBoundValue()
    {
        return new ConfigBoundValue(false, null);
    }

    internal static string ChildPath(string parentPath, string childSegment, string delimiter)
    {
        string cleanDelimiter = ConfigPath.CleanDelimiter(delimiter);
        string escaped = ConfigPath.EscapeSegment(childSegment, cleanDelimiter);
        if (parentPath.Length == 0)
        {
            return escaped;
        }

        return parentPath + cleanDelimiter + escaped;
    }

    internal static string TargetChildPath(string parentPath, string childSegment)
    {
        if (parentPath.Length == 0)
        {
            return ConfigPath.Normalize(childSegment, ".");
        }

        return parentPath + "." + ConfigPath.Normalize(childSegment, ".");
    }

    internal static string MappedPath(string rootPath, string mappedPath, string delimiter)
    {
        string normalized = ConfigPath.Normalize(mappedPath, delimiter);
        if (rootPath.Length == 0)
        {
            return normalized;
        }

        if (normalized.Length == 0)
        {
            return rootPath;
        }

        return rootPath + ConfigPath.CleanDelimiter(delimiter) + normalized;
    }
}
