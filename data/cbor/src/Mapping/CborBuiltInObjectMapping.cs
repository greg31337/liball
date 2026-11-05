using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Reflection;

namespace cbor;

internal class CborBuiltInObjectMapping
{
    internal CborMappingOptions? Options;
    internal CborMappingContext Context = new CborMappingContext();
    internal int ItemCount;
    internal string CurrentPath = "$";
    internal List<object> NativeStack = new List<object>();
    internal List<CborValue> ValueStack = new List<CborValue>();
    internal static readonly object MetadataLock = new object();
    internal static readonly Dictionary<Type, CborObjectMetadata> MetadataCache =
        new Dictionary<Type, CborObjectMetadata>();
    internal static readonly BigInteger MaximumBasicInteger = new BigInteger(ulong.MaxValue);
    internal static readonly BigInteger MinimumBasicInteger = -BigInteger.One - MaximumBasicInteger;
    internal static readonly BigInteger MaximumDecimalMagnitude = BigInteger.Parse(
        "79228162514264337593543950335",
        CultureInfo.InvariantCulture
    );

    internal CborBuiltInObjectMapping(CborMappingOptions? options)
    {
        Options = options;
        Context.Options = options ?? new CborMappingOptions();
    }

    internal CborValue MapNative(object? value, Type? runtimeType)
    {
        ValidateOptions();
        if (runtimeType == null)
        {
            Fail("CBOR_MAPPING_RUNTIME_TYPE_MISSING", "The declared runtime type is required.", "$" );
        }
        ItemCount = 0;
        NativeStack.Clear();
        Context.Depth = 0;
        Context.ItemCount = 0;
        Context.NativeReferences.Clear();
        Context.ValueReferences.Clear();
        Type declaredType = runtimeType ?? new object().GetType();
        return MapNativeValue(value, declaredType, 0, "$" );
    }

    internal CborValue MapNativeValue(
        object? value,
        Type declaredType,
        int depth,
        string path)
    {
        EnterMapping(depth, path);
        Type effectiveType = GetEffectiveType(declaredType);
        if (value == null)
        {
            if (!IsNullAllowed(declaredType))
            {
                Fail(
                    "CBOR_MAPPING_NULL_NOT_ALLOWED",
                    "Null cannot be mapped for the declared non-nullable value type.",
                    path
                );
            }
            return CreateNull();
        }
        if (declaredType.FullName == "System.Object")
        {
            effectiveType = value.GetType();
        }
        RequireRuntimeValueType(value, effectiveType);
        CborMappingOptions activeOptions = Options ?? new CborMappingOptions();
        if (activeOptions.PreserveObjectReferences && !effectiveType.IsValueType)
        {
            return EncodeTrackedValue(value, effectiveType, depth, path);
        }
        return EncodeKnownValue(value, effectiveType, depth, path);
    }

    internal CborValue EncodeKnownValue(
        object value,
        Type effectiveType,
        int depth,
        string path)
    {
        ICborTypeAdapter? adapter = FindTypeAdapter(effectiveType);
        if (adapter != null)
        {
            return EncodeCustomObject(value, effectiveType, adapter, depth, path);
        }
        string typeName = TypeName(effectiveType);
        if (typeName == "System.Boolean")
        {
            return EncodeBoolean(value);
        }
        if (IsIntegerType(typeName))
        {
            return EncodeInteger(value, typeName);
        }
        if (typeName == "System.Numerics.BigInteger")
        {
            return CreateIntegerOrBignum((BigInteger)value);
        }
        if (typeName == "System.Half"
            || typeName == "System.Single"
            || typeName == "System.Double")
        {
            return EncodeFloating(value, typeName);
        }
        if (typeName == "System.Decimal")
        {
            return EncodeDecimal(value);
        }
        if (typeName == "System.String")
        {
            string nativeText = (string)value;
            if (!CborTokenWritePlan.IsWellFormedUtf16(nativeText))
            {
                Fail(
                    "CBOR_MAPPING_TEXT_INVALID_UTF16",
                    "The runtime string contains an unpaired UTF-16 surrogate.",
                    path
                );
            }
            CborValue text = new CborValue();
            text.Kind = CborValueKind.TextString;
            text.TextStringValue = nativeText;
            text.LengthKind = CborLengthKind.Definite;
            return text;
        }
        if (typeName == "System.Byte[]" || IsByteMemoryType(effectiveType))
        {
            return EncodeBytes(value, typeName);
        }
        if (typeName == "System.DateTime" || typeName == "System.DateTimeOffset")
        {
            return EncodeDateTime(value, typeName);
        }
        if (typeName == "System.Uri")
        {
            return EncodeUri(value);
        }
        if (typeName == "System.Guid")
        {
            return EncodeGuid(value);
        }
        if (effectiveType.IsEnum)
        {
            return EncodeEnum(value, effectiveType);
        }
        return EncodeCompound(value, effectiveType, depth, path);
    }

    internal object? MapCbor(CborValue? value, Type? targetType)
    {
        ValidateOptions();
        if (value == null)
        {
            Fail("CBOR_MAPPING_VALUE_MISSING", "The CBOR value is required.", "$" );
        }
        if (targetType == null)
        {
            Fail("CBOR_MAPPING_TARGET_TYPE_MISSING", "The target runtime type is required.", "$" );
        }
        CborValue source = value ?? new CborValue();
        Type declaredType = targetType ?? new object().GetType();
        ItemCount = 0;
        ValueStack.Clear();
        Context.Depth = 0;
        Context.ItemCount = 0;
        Context.NativeReferences.Clear();
        Context.ValueReferences.Clear();
        return MapCborValue(source, declaredType, 0, "$" );
    }

    internal object? MapCborValue(
        CborValue source,
        Type declaredType,
        int depth,
        string path)
    {
        EnterMapping(depth, path);
        CborValue activeSource = PrepareDecodedSource(source, path);
        CborMappingOptions activeOptions = Options ?? new CborMappingOptions();
        if (activeOptions.PreserveObjectReferences)
        {
            int existingIndex = FindValueReference(activeSource);
            if (existingIndex >= 0)
            {
                return GetDecodedReference(existingIndex, declaredType, path);
            }
        }
        object? result = DecodeKnownValue(activeSource, declaredType, depth, path);
        if (activeOptions.PreserveObjectReferences
            && result != null
            && FindValueReference(activeSource) < 0)
        {
            RegisterDecodedReference(activeSource, result);
        }
        return result;
    }

    internal object? DecodeKnownValue(
        CborValue source,
        Type declaredType,
        int depth,
        string path)
    {
        if (source.Kind == CborValueKind.SimpleValue
            && source.SimpleValue == CborSimpleValues.Null)
        {
            if (!IsNullAllowed(declaredType))
            {
                Fail(
                    "CBOR_MAPPING_NULL_NOT_ALLOWED",
                    "CBOR null cannot be mapped to the requested non-nullable value type.",
                    path
                );
            }
            return null;
        }
        Type effectiveType = GetEffectiveType(declaredType);
        ICborTypeAdapter? adapter = FindTypeAdapter(effectiveType);
        if (adapter != null)
        {
            return DecodeCustomObject(source, effectiveType, adapter, depth, path);
        }
        string typeName = TypeName(effectiveType);
        if (typeName == "System.Boolean")
        {
            return DecodeBoolean(source);
        }
        if (IsIntegerType(typeName))
        {
            return DecodeIntegerTarget(source, effectiveType);
        }
        if (typeName == "System.Numerics.BigInteger")
        {
            return ReadInteger(source, "$" );
        }
        if (typeName == "System.Half"
            || typeName == "System.Single"
            || typeName == "System.Double")
        {
            return DecodeFloatingTarget(source, typeName);
        }
        if (typeName == "System.Decimal")
        {
            return DecodeDecimal(source);
        }
        if (typeName == "System.String")
        {
            if (source.Kind != CborValueKind.TextString || source.TextStringValue == null)
            {
                Fail("CBOR_MAPPING_TEXT_REQUIRED", "A CBOR text string is required.", "$" );
            }
            return source.TextStringValue;
        }
        if (typeName == "System.Byte[]" || IsByteMemoryType(effectiveType))
        {
            return DecodeBytes(source, effectiveType, typeName);
        }
        if (typeName == "System.DateTime" || typeName == "System.DateTimeOffset")
        {
            return DecodeDateTime(source, typeName);
        }
        if (typeName == "System.Uri")
        {
            return DecodeUri(source);
        }
        if (typeName == "System.Guid")
        {
            return DecodeGuid(source);
        }
        if (effectiveType.IsEnum)
        {
            return DecodeEnum(source, effectiveType);
        }
        return DecodeCompound(source, effectiveType, depth, path);
    }

    internal void ValidateOptions()
    {
        if (Options == null)
        {
            Fail("CBOR_MAPPING_OPTIONS_MISSING", "Mapping options are required.", "$" );
        }
        CborMappingOptions activeOptions = Options ?? new CborMappingOptions();
        if ((int)activeOptions.MemberNamingPolicy < (int)CborMappingNamingPolicy.Exact
            || (int)activeOptions.MemberNamingPolicy > (int)CborMappingNamingPolicy.CamelCase)
        {
            Fail(
                "CBOR_MAPPING_NAMING_POLICY_INVALID",
                "The reflected member naming policy is invalid.",
                "$"
            );
        }
        if ((int)activeOptions.DictionaryKeyPolicy < (int)CborMappingKeyPolicy.Any
            || (int)activeOptions.DictionaryKeyPolicy > (int)CborMappingKeyPolicy.TextOnly)
        {
            Fail(
                "CBOR_MAPPING_KEY_POLICY_INVALID",
                "The dictionary key policy is invalid.",
                "$"
            );
        }
        if (activeOptions.MaxNestingDepth < 0)
        {
            Fail(
                "CBOR_MAPPING_MAX_NESTING_DEPTH_INVALID",
                "The mapping nesting limit cannot be negative.",
                "$"
            );
        }
        if (activeOptions.MaxCollectionItems < 0)
        {
            Fail(
                "CBOR_MAPPING_MAX_COLLECTION_ITEMS_INVALID",
                "The mapping collection limit cannot be negative.",
                "$"
            );
        }
        if (activeOptions.MaxTotalItems < 0)
        {
            Fail(
                "CBOR_MAPPING_MAX_TOTAL_ITEMS_INVALID",
                "The mapping item limit cannot be negative.",
                "$"
            );
        }
        if (activeOptions.TypeRegistrations == null)
        {
            Fail(
                "CBOR_MAPPING_TYPE_REGISTRATIONS_MISSING",
                "The custom type registration collection is required.",
                "$"
            );
        }
        ValidateTypeRegistrations(activeOptions);
    }

    internal void ValidateTypeRegistrations(CborMappingOptions options)
    {
        int index = 0;
        while (index < options.TypeRegistrations.Count)
        {
            CborTypeRegistration? registration = options.TypeRegistrations[index];
            if (registration == null)
            {
                Fail(
                    "CBOR_MAPPING_TYPE_REGISTRATION_MISSING",
                    "A custom type registration is required.",
                    "$"
                );
            }
            CborTypeRegistration activeRegistration =
                registration ?? new CborTypeRegistration();
            if (activeRegistration.RuntimeType == null)
            {
                Fail(
                    "CBOR_MAPPING_REGISTERED_TYPE_MISSING",
                    "A custom type registration requires a runtime type.",
                    "$"
                );
            }
            if (activeRegistration.Adapter == null)
            {
                Fail(
                    "CBOR_MAPPING_TYPE_ADAPTER_MISSING",
                    "A custom type registration requires an adapter.",
                    "$"
                );
            }
            int priorIndex = 0;
            while (priorIndex < index)
            {
                CborTypeRegistration? prior = options.TypeRegistrations[priorIndex];
                if (prior != null
                    && prior.RuntimeType == activeRegistration.RuntimeType)
                {
                    Fail(
                        "CBOR_MAPPING_TYPE_REGISTRATION_DUPLICATE",
                        $"The runtime type {TypeName(activeRegistration.RuntimeType ?? new object().GetType())} is registered more than once.",
                        "$"
                    );
                }
                priorIndex++;
            }
            index++;
        }
    }

    internal ICborTypeAdapter? FindTypeAdapter(Type runtimeType)
    {
        CborMappingOptions activeOptions = Options ?? new CborMappingOptions();
        int index = 0;
        while (index < activeOptions.TypeRegistrations.Count)
        {
            CborTypeRegistration registration =
                activeOptions.TypeRegistrations[index] ?? new CborTypeRegistration();
            if (registration.RuntimeType == runtimeType)
            {
                ICborTypeAdapter? exactAdapter = registration.Adapter;
                if (exactAdapter == null || !exactAdapter.SupportsType(runtimeType))
                {
                    Fail(
                        "CBOR_MAPPING_ADAPTER_TYPE_UNSUPPORTED",
                        $"The registered adapter does not support {TypeName(runtimeType)}.",
                        CurrentPath
                    );
                }
                return exactAdapter;
            }
            index++;
        }
        index = 0;
        while (index < activeOptions.TypeRegistrations.Count)
        {
            CborTypeRegistration registration =
                activeOptions.TypeRegistrations[index] ?? new CborTypeRegistration();
            Type? registeredType = registration.RuntimeType;
            ICborTypeAdapter? adapter = registration.Adapter;
            if (registeredType != null
                && adapter != null
                && registeredType.IsAssignableFrom(runtimeType)
                && adapter.SupportsType(runtimeType))
            {
                return adapter;
            }
            index++;
        }
        return null;
    }

    internal CborValue EncodeCustomObject(
        object value,
        Type runtimeType,
        ICborTypeAdapter adapter,
        int depth,
        string path)
    {
        Context.Depth = depth;
        Context.ItemCount = ItemCount;
        CborValue? result = adapter.EncodeObject(value, Context);
        if (result == null)
        {
            Fail(
                "CBOR_MAPPING_ADAPTER_VALUE_MISSING",
                $"The adapter for {TypeName(runtimeType)} returned no CBOR value.",
                path
            );
        }
        return result ?? new CborValue();
    }

    internal object? DecodeCustomObject(
        CborValue value,
        Type targetType,
        ICborTypeAdapter adapter,
        int depth,
        string path)
    {
        Context.Depth = depth;
        Context.ItemCount = ItemCount;
        object? result = adapter.DecodeObject(value, targetType, Context);
        if (result == null)
        {
            if (!IsNullAllowed(targetType))
            {
                Fail(
                    "CBOR_MAPPING_ADAPTER_RESULT_NULL",
                    $"The adapter for {TypeName(targetType)} returned null for a non-nullable target.",
                    path
                );
            }
            return null;
        }
        RequireRuntimeValueType(result, targetType);
        return result;
    }

    internal CborValue EncodeTrackedValue(
        object value,
        Type runtimeType,
        int depth,
        string path)
    {
        int existingIndex = FindNativeReference(value);
        if (existingIndex >= 0)
        {
            CborValue existing = Context.ValueReferences[existingIndex];
            existing.IsSharedValue = true;
            return existing;
        }
        CborValue placeholder = new CborValue();
        Context.NativeReferences.Add(value);
        Context.ValueReferences.Add(placeholder);
        CborValue encoded = EncodeKnownValue(value, runtimeType, depth, path);
        CopyCborValue(encoded, placeholder);
        return placeholder;
    }

    internal int FindNativeReference(object value)
    {
        int index = 0;
        while (index < Context.NativeReferences.Count
            && index < Context.ValueReferences.Count)
        {
            if (object.ReferenceEquals(Context.NativeReferences[index], value))
            {
                return index;
            }
            index++;
        }
        return -1;
    }

    internal int FindValueReference(CborValue value)
    {
        int index = 0;
        while (index < Context.ValueReferences.Count
            && index < Context.NativeReferences.Count)
        {
            if (object.ReferenceEquals(Context.ValueReferences[index], value))
            {
                return index;
            }
            index++;
        }
        return -1;
    }

    internal void RegisterDecodedReference(CborValue value, object nativeValue)
    {
        if (FindValueReference(value) < 0)
        {
            Context.ValueReferences.Add(value);
            Context.NativeReferences.Add(nativeValue);
        }
    }

    internal object GetDecodedReference(int index, Type targetType, string path)
    {
        object result = Context.NativeReferences[index];
        Type effectiveType = GetEffectiveType(targetType);
        if (TypeName(effectiveType) != "System.Object"
            && !effectiveType.IsAssignableFrom(result.GetType()))
        {
            Fail(
                "CBOR_MAPPING_REFERENCE_TYPE_MISMATCH",
                $"The referenced native object cannot be assigned to {TypeName(effectiveType)}.",
                path
            );
        }
        return result;
    }

    internal CborValue PrepareDecodedSource(CborValue source, string path)
    {
        CborMappingOptions activeOptions = Options ?? new CborMappingOptions();
        if (!activeOptions.PreserveObjectReferences)
        {
            return source;
        }
        if (source.ReferencedValue != null)
        {
            if (FindValueReference(source.ReferencedValue) < 0)
            {
                Fail(
                    "CBOR_MAPPING_REFERENCE_NOT_FOUND",
                    "The CBOR reference target has not been mapped yet.",
                    path
                );
            }
            return source.ReferencedValue;
        }
        if (source.Kind == CborValueKind.Tag
            && source.TagNumber == CborTagNumbers.SharedValueReference)
        {
            Fail(
                "CBOR_MAPPING_REFERENCE_NOT_RESOLVED",
                "A shared-value reference must be resolved before object mapping.",
                path
            );
        }
        if (source.Kind == CborValueKind.Tag
            && source.TagNumber == CborTagNumbers.ShareableValue)
        {
            if (source.TaggedValue == null)
            {
                Fail(
                    "CBOR_MAPPING_SHARED_VALUE_MISSING",
                    "A shareable-value tag requires a content value.",
                    path
                );
            }
            return source.TaggedValue ?? new CborValue();
        }
        return source;
    }

    internal void CopyCborValue(CborValue source, CborValue target)
    {
        if (object.ReferenceEquals(source, target))
        {
            return;
        }
        bool shared = target.IsSharedValue || source.IsSharedValue;
        target.Kind = source.Kind;
        target.IntegerValue = source.IntegerValue;
        target.ByteStringValue = source.ByteStringValue;
        target.TextStringValue = source.TextStringValue;
        target.ArrayItems = source.ArrayItems;
        target.MapEntries = source.MapEntries;
        target.TagNumber = source.TagNumber;
        target.TaggedValue = source.TaggedValue;
        target.FloatValue = source.FloatValue;
        target.FloatWidth = source.FloatWidth;
        target.SimpleValue = source.SimpleValue;
        target.LengthKind = source.LengthKind;
        target.Representation = source.Representation;
        target.IsSharedValue = shared;
        target.SharedValueIndex = source.SharedValueIndex;
        target.ReferencedValue = source.ReferencedValue;
    }

    internal Type GetEffectiveType(Type declaredType)
    {
        Type? underlying = System.Nullable.GetUnderlyingType(declaredType);
        return underlying ?? declaredType;
    }

    internal bool IsNullAllowed(Type declaredType)
    {
        return !declaredType.IsValueType
            || System.Nullable.GetUnderlyingType(declaredType) != null;
    }

    internal void RequireRuntimeValueType(object value, Type effectiveType)
    {
        Type actualType = value.GetType();
        if (actualType != effectiveType && !effectiveType.IsAssignableFrom(actualType))
        {
            Fail(
                "CBOR_MAPPING_RUNTIME_TYPE_MISMATCH",
                $"The runtime value type {TypeName(actualType)} does not match the declared type {TypeName(effectiveType)}.",
                "$"
            );
        }
    }

    internal bool IsIntegerType(string typeName)
    {
        return typeName == "System.SByte"
            || typeName == "System.Byte"
            || typeName == "System.Int16"
            || typeName == "System.UInt16"
            || typeName == "System.Int32"
            || typeName == "System.UInt32"
            || typeName == "System.Int64"
            || typeName == "System.UInt64"
            || typeName == "System.Char";
    }

    internal bool IsByteMemoryType(Type runtimeType)
    {
        if (!runtimeType.IsGenericType)
        {
            return false;
        }
        string definition = runtimeType.GetGenericTypeDefinition().FullName ?? "";
        if (definition != "System.Memory`1" && definition != "System.ReadOnlyMemory`1")
        {
            return false;
        }
        Type[] arguments = runtimeType.GetGenericArguments();
        return arguments.Length == 1 && TypeName(arguments[0]) == "System.Byte";
    }

    internal string TypeName(Type runtimeType)
    {
        return runtimeType.FullName ?? runtimeType.Name;
    }

    internal void EnterMapping(int depth, string path)
    {
        CurrentPath = path;
        CborMappingOptions activeOptions = Options ?? new CborMappingOptions();
        if (depth > activeOptions.MaxNestingDepth)
        {
            Fail(
                "CBOR_MAPPING_MAX_NESTING_DEPTH",
                "The mapped value exceeds the configured nesting limit.",
                path
            );
        }
        if (ItemCount >= activeOptions.MaxTotalItems)
        {
            Fail(
                "CBOR_MAPPING_MAX_TOTAL_ITEMS",
                "The mapping operation exceeds the configured item limit.",
                path
            );
        }
        ItemCount++;
        Context.Depth = depth;
        Context.ItemCount = ItemCount;
    }

    internal CborCollectionShape GetCollectionShape(Type runtimeType)
    {
        CborCollectionShape shape = new CborCollectionShape();
        if (runtimeType.IsArray)
        {
            if (runtimeType.GetArrayRank() != 1)
            {
                Fail(
                    "CBOR_MAPPING_ARRAY_RANK_UNSUPPORTED",
                    "Only one-dimensional arrays are supported.",
                    CurrentPath
                );
            }
            shape.IsArray = true;
            shape.ElementType = runtimeType.GetElementType();
            shape.ConcreteType = runtimeType;
            return shape;
        }
        Type? dictionaryInterface = FindGenericInterface(
            runtimeType,
            "System.Collections.Generic.IDictionary`2"
        );
        if (dictionaryInterface == null)
        {
            dictionaryInterface = FindGenericInterface(
                runtimeType,
                "System.Collections.Generic.IReadOnlyDictionary`2"
            );
        }
        if (dictionaryInterface != null)
        {
            Type[] arguments = dictionaryInterface.GetGenericArguments();
            shape.IsDictionary = true;
            shape.KeyType = arguments[0];
            shape.ValueType = arguments[1];
            if (runtimeType.IsInterface || runtimeType.IsAbstract)
            {
                Type definition = new Dictionary<object, object>()
                    .GetType()
                    .GetGenericTypeDefinition();
                shape.ConcreteType = definition.MakeGenericType(arguments);
            }
            else
            {
                shape.ConcreteType = runtimeType;
            }
            return shape;
        }
        Type? listInterface = FindGenericInterface(
            runtimeType,
            "System.Collections.Generic.IList`1"
        );
        if (listInterface == null)
        {
            listInterface = FindGenericInterface(
                runtimeType,
                "System.Collections.Generic.IReadOnlyList`1"
            );
        }
        if (listInterface != null)
        {
            Type[] arguments = listInterface.GetGenericArguments();
            shape.IsList = true;
            shape.ElementType = arguments[0];
            if (runtimeType.IsInterface || runtimeType.IsAbstract)
            {
                Type definition = new List<object>()
                    .GetType()
                    .GetGenericTypeDefinition();
                shape.ConcreteType = definition.MakeGenericType(arguments);
            }
            else
            {
                shape.ConcreteType = runtimeType;
            }
        }
        return shape;
    }

    internal Type? FindGenericInterface(Type runtimeType, string definitionName)
    {
        if (runtimeType.IsGenericType
            && TypeName(runtimeType.GetGenericTypeDefinition()) == definitionName)
        {
            return runtimeType;
        }
        Type[] interfaces = runtimeType.GetInterfaces();
        int index = 0;
        while (index < interfaces.Length)
        {
            Type candidate = interfaces[index];
            if (candidate.IsGenericType
                && TypeName(candidate.GetGenericTypeDefinition()) == definitionName)
            {
                return candidate;
            }
            index++;
        }
        return null;
    }

    internal CborValue EncodeCompound(
        object value,
        Type runtimeType,
        int depth,
        string path)
    {
        CborCollectionShape shape = GetCollectionShape(runtimeType);
        if (shape.IsDictionary)
        {
            return EncodeDictionary(value, shape, depth, path);
        }
        if (shape.IsArray || shape.IsList)
        {
            return EncodeList(value, shape, depth, path);
        }
        if (runtimeType.IsClass)
        {
            return EncodeReflectedObject(value, runtimeType, depth, path);
        }
        Fail(
            "CBOR_MAPPING_TYPE_UNSUPPORTED",
            $"The runtime type {TypeName(runtimeType)} does not have a built-in mapping.",
            path
        );
        return new CborValue();
    }

    internal object? DecodeCompound(
        CborValue value,
        Type targetType,
        int depth,
        string path)
    {
        CborCollectionShape shape = GetCollectionShape(targetType);
        if (shape.IsDictionary)
        {
            return DecodeDictionary(value, shape, depth, path);
        }
        if (shape.IsArray || shape.IsList)
        {
            return DecodeList(value, shape, depth, path);
        }
        if (targetType.IsClass)
        {
            return DecodeReflectedObject(value, targetType, depth, path);
        }
        Fail(
            "CBOR_MAPPING_TYPE_UNSUPPORTED",
            $"The target type {TypeName(targetType)} does not have a built-in mapping.",
            path
        );
        return null;
    }

    internal void EnterNativeCompound(object value, string path)
    {
        int index = 0;
        while (index < NativeStack.Count)
        {
            if (object.ReferenceEquals(NativeStack[index], value))
            {
                Fail(
                    "CBOR_MAPPING_CYCLE",
                    "A cyclic native object requires reference preservation.",
                    path
                );
            }
            index++;
        }
        NativeStack.Add(value);
    }

    internal void LeaveNativeCompound()
    {
        NativeStack.RemoveAt(NativeStack.Count - 1);
    }

    internal void EnterValueCompound(CborValue value, string path)
    {
        int index = 0;
        while (index < ValueStack.Count)
        {
            if (object.ReferenceEquals(ValueStack[index], value))
            {
                Fail(
                    "CBOR_MAPPING_CYCLE",
                    "A cyclic CBOR value requires reference preservation.",
                    path
                );
            }
            index++;
        }
        ValueStack.Add(value);
    }

    internal void LeaveValueCompound()
    {
        ValueStack.RemoveAt(ValueStack.Count - 1);
    }

    internal void ValidateCollectionCount(int count, string path)
    {
        CborMappingOptions activeOptions = Options ?? new CborMappingOptions();
        if (count > activeOptions.MaxCollectionItems)
        {
            Fail(
                "CBOR_MAPPING_MAX_COLLECTION_ITEMS",
                "The collection exceeds the configured member limit.",
                path
            );
        }
    }

    internal CborValue EncodeList(
        object value,
        CborCollectionShape shape,
        int depth,
        string path)
    {
        IList? list = value as IList;
        Type elementType = shape.ElementType ?? new object().GetType();
        if (list == null)
        {
            Fail(
                "CBOR_MAPPING_LIST_RUNTIME_TYPE_INVALID",
                "The native list must implement the non-generic IList interface.",
                path
            );
        }
        IList activeList = list ?? new ArrayList();
        ValidateCollectionCount(activeList.Count, path);
        EnterNativeCompound(value, path);
        CborValue array = new CborValue();
        array.Kind = CborValueKind.Array;
        array.LengthKind = CborLengthKind.Definite;
        array.ArrayItems = new List<CborValue>();
        int index = 0;
        while (index < activeList.Count)
        {
            array.ArrayItems.Add(MapNativeValue(
                activeList[index],
                elementType,
                depth + 1,
                $"{path}[{index}]"
            ));
            index++;
        }
        LeaveNativeCompound();
        return array;
    }

    internal CborValue EncodeDictionary(
        object value,
        CborCollectionShape shape,
        int depth,
        string path)
    {
        IDictionary? dictionary = value as IDictionary;
        if (dictionary == null)
        {
            Fail(
                "CBOR_MAPPING_DICTIONARY_RUNTIME_TYPE_INVALID",
                "The native dictionary must implement the non-generic IDictionary interface.",
                path
            );
        }
        IDictionary activeDictionary = dictionary ?? new Hashtable();
        ValidateCollectionCount(activeDictionary.Count, path);
        EnterNativeCompound(value, path);
        Type keyType = shape.KeyType ?? new object().GetType();
        Type valueType = shape.ValueType ?? new object().GetType();
        CborMappingOptions activeOptions = Options ?? new CborMappingOptions();
        CborValue map = new CborValue();
        map.Kind = CborValueKind.Map;
        map.LengthKind = CborLengthKind.Definite;
        map.MapEntries = new List<CborMapEntry>();
        int index = 0;
        foreach (DictionaryEntry nativeEntry in activeDictionary)
        {
            CborValue key = MapNativeValue(
                nativeEntry.Key,
                keyType,
                depth + 1,
                $"{path}[{index}].key"
            );
            if (activeOptions.DictionaryKeyPolicy == CborMappingKeyPolicy.TextOnly
                && key.Kind != CborValueKind.TextString)
            {
                Fail(
                    "CBOR_MAPPING_DICTIONARY_KEY_NOT_TEXT",
                    "The configured dictionary policy requires text keys.",
                    $"{path}[{index}].key"
                );
            }
            CborMapEntry entry = new CborMapEntry();
            entry.Key = key;
            entry.Value = MapNativeValue(
                nativeEntry.Value,
                valueType,
                depth + 1,
                $"{path}[{index}].value"
            );
            map.MapEntries.Add(entry);
            index++;
        }
        LeaveNativeCompound();
        return map;
    }

    internal object DecodeList(
        CborValue value,
        CborCollectionShape shape,
        int depth,
        string path)
    {
        if (value.Kind != CborValueKind.Array || value.ArrayItems == null)
        {
            Fail("CBOR_MAPPING_ARRAY_REQUIRED", "A CBOR array is required.", path);
        }
        List<CborValue> items = value.ArrayItems ?? new List<CborValue>();
        ValidateCollectionCount(items.Count, path);
        EnterValueCompound(value, path);
        Type elementType = shape.ElementType ?? new object().GetType();
        if (shape.IsArray)
        {
            Array array = Array.CreateInstance(elementType, items.Count);
            CborMappingOptions activeOptions = Options ?? new CborMappingOptions();
            if (activeOptions.PreserveObjectReferences)
            {
                RegisterDecodedReference(value, array);
            }
            int arrayIndex = 0;
            while (arrayIndex < items.Count)
            {
                object? item = MapCborValue(
                    items[arrayIndex],
                    elementType,
                    depth + 1,
                    $"{path}[{arrayIndex}]"
                );
                array.SetValue(item, arrayIndex);
                arrayIndex++;
            }
            LeaveValueCompound();
            return array;
        }
        Type concreteType = shape.ConcreteType ?? new List<object>().GetType();
        object? instance = Activator.CreateInstance(concreteType);
        IList? list = instance as IList;
        if (list == null)
        {
            Fail(
                "CBOR_MAPPING_LIST_TARGET_INVALID",
                "The target list requires a public parameterless constructor and IList support.",
                path
            );
        }
        IList activeList = list ?? new ArrayList();
        CborMappingOptions listOptions = Options ?? new CborMappingOptions();
        if (listOptions.PreserveObjectReferences)
        {
            RegisterDecodedReference(value, activeList);
        }
        int index = 0;
        while (index < items.Count)
        {
            activeList.Add(MapCborValue(
                items[index],
                elementType,
                depth + 1,
                $"{path}[{index}]"
            ));
            index++;
        }
        LeaveValueCompound();
        return activeList;
    }

    internal object DecodeDictionary(
        CborValue value,
        CborCollectionShape shape,
        int depth,
        string path)
    {
        if (value.Kind != CborValueKind.Map || value.MapEntries == null)
        {
            Fail("CBOR_MAPPING_MAP_REQUIRED", "A CBOR map is required.", path);
        }
        List<CborMapEntry> entries = value.MapEntries ?? new List<CborMapEntry>();
        ValidateCollectionCount(entries.Count, path);
        EnterValueCompound(value, path);
        Type concreteType = shape.ConcreteType ?? new Dictionary<object, object>().GetType();
        object? instance = Activator.CreateInstance(concreteType);
        IDictionary? dictionary = instance as IDictionary;
        if (dictionary == null)
        {
            Fail(
                "CBOR_MAPPING_DICTIONARY_TARGET_INVALID",
                "The target dictionary requires a public parameterless constructor and IDictionary support.",
                path
            );
        }
        IDictionary activeDictionary = dictionary ?? new Hashtable();
        CborMappingOptions referenceOptions = Options ?? new CborMappingOptions();
        if (referenceOptions.PreserveObjectReferences)
        {
            RegisterDecodedReference(value, activeDictionary);
        }
        Type keyType = shape.KeyType ?? new object().GetType();
        Type valueType = shape.ValueType ?? new object().GetType();
        CborMappingOptions activeOptions = Options ?? new CborMappingOptions();
        int index = 0;
        while (index < entries.Count)
        {
            CborMapEntry entry = entries[index];
            if (entry == null || entry.Key == null || entry.Value == null)
            {
                Fail(
                    "CBOR_MAPPING_MAP_ENTRY_MISSING",
                    "A complete CBOR map entry is required.",
                    $"{path}[{index}]"
                );
            }
            CborMapEntry activeEntry = entry ?? new CborMapEntry();
            CborValue keyValue = activeEntry.Key ?? new CborValue();
            if (activeOptions.DictionaryKeyPolicy == CborMappingKeyPolicy.TextOnly
                && keyValue.Kind != CborValueKind.TextString)
            {
                Fail(
                    "CBOR_MAPPING_DICTIONARY_KEY_NOT_TEXT",
                    "The configured dictionary policy requires text keys.",
                    $"{path}[{index}].key"
                );
            }
            object? key = MapCborValue(
                keyValue,
                keyType,
                depth + 1,
                $"{path}[{index}].key"
            );
            if (key == null)
            {
                Fail(
                    "CBOR_MAPPING_DICTIONARY_KEY_NULL",
                    "A native dictionary key cannot be null.",
                    $"{path}[{index}].key"
                );
            }
            object activeKey = key ?? new object();
            if (activeDictionary.Contains(activeKey))
            {
                Fail(
                    "CBOR_MAPPING_DUPLICATE_DICTIONARY_KEY",
                    "Two CBOR map keys convert to the same native dictionary key.",
                    $"{path}[{index}].key"
                );
            }
            activeDictionary.Add(
                activeKey,
                MapCborValue(
                    activeEntry.Value ?? new CborValue(),
                    valueType,
                    depth + 1,
                    $"{path}[{index}].value"
                )
            );
            index++;
        }
        LeaveValueCompound();
        return activeDictionary;
    }

    internal CborValue EncodeReflectedObject(
        object value,
        Type runtimeType,
        int depth,
        string path)
    {
        CborObjectMetadata metadata = GetObjectMetadata(runtimeType);
        ValidateMemberNames(metadata, path);
        ValidateCollectionCount(metadata.Members.Count, path);
        EnterNativeCompound(value, path);
        CborMappingOptions activeOptions = Options ?? new CborMappingOptions();
        if (activeOptions.MapObjectsAsArrays)
        {
            CborValue array = new CborValue();
            array.Kind = CborValueKind.Array;
            array.LengthKind = CborLengthKind.Definite;
            array.ArrayItems = new List<CborValue>();
            int arrayIndex = 0;
            while (arrayIndex < metadata.Members.Count)
            {
                CborObjectMemberMetadata member = metadata.Members[arrayIndex];
                Type memberType = member.MemberType ?? new object().GetType();
                array.ArrayItems.Add(MapNativeValue(
                    ReadObjectMember(member, value),
                    memberType,
                    depth + 1,
                    $"{path}[{arrayIndex}]"
                ));
                arrayIndex++;
            }
            LeaveNativeCompound();
            return array;
        }
        CborValue map = new CborValue();
        map.Kind = CborValueKind.Map;
        map.LengthKind = CborLengthKind.Definite;
        map.MapEntries = new List<CborMapEntry>();
        int index = 0;
        while (index < metadata.Members.Count)
        {
            CborObjectMemberMetadata member = metadata.Members[index];
            string name = EncodeMemberName(member.Name ?? "");
            CborValue key = new CborValue();
            key.Kind = CborValueKind.TextString;
            key.TextStringValue = name;
            key.LengthKind = CborLengthKind.Definite;
            CborMapEntry entry = new CborMapEntry();
            entry.Key = key;
            entry.Value = MapNativeValue(
                ReadObjectMember(member, value),
                member.MemberType ?? new object().GetType(),
                depth + 1,
                $"{path}.{name}"
            );
            map.MapEntries.Add(entry);
            index++;
        }
        LeaveNativeCompound();
        return map;
    }

    internal object DecodeReflectedObject(
        CborValue value,
        Type targetType,
        int depth,
        string path)
    {
        CborObjectMetadata metadata = GetObjectMetadata(targetType);
        ValidateMemberNames(metadata, path);
        ValidateCollectionCount(metadata.Members.Count, path);
        if (!metadata.CanCreate)
        {
            Fail(
                "CBOR_MAPPING_CONSTRUCTOR_REQUIRED",
                "The reflected target type requires a public parameterless constructor.",
                path
            );
        }
        object? instance = Activator.CreateInstance(targetType);
        if (instance == null)
        {
            Fail(
                "CBOR_MAPPING_OBJECT_CREATION_FAILED",
                "The reflected target object could not be created.",
                path
            );
        }
        object target = instance ?? new object();
        CborMappingOptions referenceOptions = Options ?? new CborMappingOptions();
        if (referenceOptions.PreserveObjectReferences)
        {
            RegisterDecodedReference(value, target);
        }
        List<bool> seen = new List<bool>();
        int seenIndex = 0;
        while (seenIndex < metadata.Members.Count)
        {
            seen.Add(false);
            seenIndex++;
        }
        EnterValueCompound(value, path);
        CborMappingOptions activeOptions = Options ?? new CborMappingOptions();
        if (activeOptions.MapObjectsAsArrays)
        {
            DecodeObjectArray(value, target, metadata, seen, depth, path);
        }
        else
        {
            DecodeObjectMap(value, target, metadata, seen, depth, path);
        }
        if (activeOptions.RequireAllMembers)
        {
            int requiredIndex = 0;
            while (requiredIndex < seen.Count)
            {
                if (!seen[requiredIndex])
                {
                    string name = EncodeMemberName(
                        metadata.Members[requiredIndex].Name ?? ""
                    );
                    Fail(
                        "CBOR_MAPPING_REQUIRED_MEMBER_MISSING",
                        $"The required member {name} is missing.",
                        $"{path}.{name}"
                    );
                }
                requiredIndex++;
            }
        }
        LeaveValueCompound();
        return target;
    }

    internal void DecodeObjectArray(
        CborValue value,
        object target,
        CborObjectMetadata metadata,
        List<bool> seen,
        int depth,
        string path)
    {
        if (value.Kind != CborValueKind.Array || value.ArrayItems == null)
        {
            Fail(
                "CBOR_MAPPING_OBJECT_ARRAY_REQUIRED",
                "A positional object mapping requires a CBOR array.",
                path
            );
        }
        List<CborValue> items = value.ArrayItems ?? new List<CborValue>();
        ValidateCollectionCount(items.Count, path);
        CborMappingOptions activeOptions = Options ?? new CborMappingOptions();
        if (items.Count > metadata.Members.Count && !activeOptions.IgnoreUnknownMembers)
        {
            Fail(
                "CBOR_MAPPING_UNKNOWN_MEMBER",
                "The positional CBOR object contains an unknown member.",
                $"{path}[{metadata.Members.Count}]"
            );
        }
        int count = items.Count < metadata.Members.Count
            ? items.Count
            : metadata.Members.Count;
        int index = 0;
        while (index < count)
        {
            CborObjectMemberMetadata member = metadata.Members[index];
            object? mapped = MapCborValue(
                items[index],
                member.MemberType ?? new object().GetType(),
                depth + 1,
                $"{path}[{index}]"
            );
            WriteObjectMember(member, target, mapped);
            seen[index] = true;
            index++;
        }
    }

    internal void DecodeObjectMap(
        CborValue value,
        object target,
        CborObjectMetadata metadata,
        List<bool> seen,
        int depth,
        string path)
    {
        if (value.Kind != CborValueKind.Map || value.MapEntries == null)
        {
            Fail(
                "CBOR_MAPPING_OBJECT_MAP_REQUIRED",
                "A named object mapping requires a CBOR map.",
                path
            );
        }
        List<CborMapEntry> entries = value.MapEntries ?? new List<CborMapEntry>();
        ValidateCollectionCount(entries.Count, path);
        CborMappingOptions activeOptions = Options ?? new CborMappingOptions();
        int index = 0;
        while (index < entries.Count)
        {
            CborMapEntry entry = entries[index];
            if (entry == null || entry.Key == null || entry.Value == null)
            {
                Fail(
                    "CBOR_MAPPING_MAP_ENTRY_MISSING",
                    "A complete CBOR map entry is required.",
                    $"{path}[{index}]"
                );
            }
            CborMapEntry activeEntry = entry ?? new CborMapEntry();
            CborValue key = activeEntry.Key ?? new CborValue();
            if (key.Kind != CborValueKind.TextString || key.TextStringValue == null)
            {
                if (activeOptions.IgnoreUnknownMembers)
                {
                    index++;
                    continue;
                }
                Fail(
                    "CBOR_MAPPING_OBJECT_KEY_NOT_TEXT",
                    "A reflected object member key must be a CBOR text string.",
                    $"{path}[{index}].key"
                );
            }
            string name = key.TextStringValue ?? "";
            int memberIndex = FindObjectMember(metadata, name);
            if (memberIndex < 0)
            {
                if (activeOptions.IgnoreUnknownMembers)
                {
                    index++;
                    continue;
                }
                Fail(
                    "CBOR_MAPPING_UNKNOWN_MEMBER",
                    $"The CBOR member {name} is not recognized.",
                    $"{path}.{name}"
                );
            }
            if (seen[memberIndex])
            {
                Fail(
                    "CBOR_MAPPING_DUPLICATE_MEMBER",
                    $"The CBOR member {name} occurs more than once.",
                    $"{path}.{name}"
                );
            }
            CborObjectMemberMetadata member = metadata.Members[memberIndex];
            object? mapped = MapCborValue(
                activeEntry.Value ?? new CborValue(),
                member.MemberType ?? new object().GetType(),
                depth + 1,
                $"{path}.{name}"
            );
            WriteObjectMember(member, target, mapped);
            seen[memberIndex] = true;
            index++;
        }
    }

    internal CborObjectMetadata GetObjectMetadata(Type runtimeType)
    {
        lock (MetadataLock)
        {
            if (MetadataCache.ContainsKey(runtimeType))
            {
                return MetadataCache[runtimeType];
            }
            CborObjectMetadata metadata = BuildObjectMetadata(runtimeType);
            MetadataCache.Add(runtimeType, metadata);
            return metadata;
        }
    }

    internal CborObjectMetadata BuildObjectMetadata(Type runtimeType)
    {
        CborObjectMetadata metadata = new CborObjectMetadata();
        metadata.CanCreate = runtimeType.IsValueType
            || runtimeType.GetConstructor(Type.EmptyTypes) != null;
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
        FieldInfo[] fields = runtimeType.GetFields(flags);
        int fieldIndex = 0;
        while (fieldIndex < fields.Length)
        {
            FieldInfo field = fields[fieldIndex];
            if (!field.IsInitOnly)
            {
                CborObjectMemberMetadata member = new CborObjectMemberMetadata();
                member.Name = field.Name;
                member.MemberType = field.FieldType;
                member.Field = field;
                metadata.Members.Add(member);
            }
            fieldIndex++;
        }
        PropertyInfo[] properties = runtimeType.GetProperties(flags);
        int propertyIndex = 0;
        while (propertyIndex < properties.Length)
        {
            PropertyInfo property = properties[propertyIndex];
            MethodInfo? getter = property.GetMethod;
            MethodInfo? setter = property.SetMethod;
            if (property.CanRead
                && property.CanWrite
                && getter != null
                && setter != null
                && getter.IsPublic
                && setter.IsPublic
                && property.GetIndexParameters().Length == 0)
            {
                CborObjectMemberMetadata member = new CborObjectMemberMetadata();
                member.Name = property.Name;
                member.MemberType = property.PropertyType;
                member.Property = property;
                metadata.Members.Add(member);
            }
            propertyIndex++;
        }
        SortObjectMembers(metadata);
        return metadata;
    }

    internal void SortObjectMembers(CborObjectMetadata metadata)
    {
        int index = 1;
        while (index < metadata.Members.Count)
        {
            CborObjectMemberMetadata current = metadata.Members[index];
            int insertion = index;
            while (insertion > 0
                && string.CompareOrdinal(
                    metadata.Members[insertion - 1].Name,
                    current.Name) > 0)
            {
                metadata.Members[insertion] = metadata.Members[insertion - 1];
                insertion--;
            }
            metadata.Members[insertion] = current;
            index++;
        }
    }

    internal string EncodeMemberName(string name)
    {
        CborMappingOptions activeOptions = Options ?? new CborMappingOptions();
        if (activeOptions.MemberNamingPolicy != CborMappingNamingPolicy.CamelCase
            || name.Length == 0)
        {
            return name;
        }
        char first = char.ToLowerInvariant(name[0]);
        if (first == name[0])
        {
            return name;
        }
        return first + name.Substring(1);
    }

    internal void ValidateMemberNames(CborObjectMetadata metadata, string path)
    {
        int index = 0;
        while (index < metadata.Members.Count)
        {
            string name = EncodeMemberName(metadata.Members[index].Name ?? "");
            int comparison = 0;
            while (comparison < index)
            {
                string prior = EncodeMemberName(
                    metadata.Members[comparison].Name ?? ""
                );
                if (name == prior)
                {
                    Fail(
                        "CBOR_MAPPING_MEMBER_NAME_COLLISION",
                        $"Two reflected members map to the name {name}.",
                        path
                    );
                }
                comparison++;
            }
            index++;
        }
    }

    internal int FindObjectMember(CborObjectMetadata metadata, string encodedName)
    {
        int index = 0;
        while (index < metadata.Members.Count)
        {
            if (EncodeMemberName(metadata.Members[index].Name ?? "") == encodedName)
            {
                return index;
            }
            index++;
        }
        return -1;
    }

    internal object? ReadObjectMember(CborObjectMemberMetadata member, object target)
    {
        if (member.Field != null)
        {
            return member.Field.GetValue(target);
        }
        if (member.Property != null)
        {
            return member.Property.GetValue(target);
        }
        Fail(
            "CBOR_MAPPING_MEMBER_METADATA_INVALID",
            "The reflected member metadata is incomplete.",
            CurrentPath
        );
        return null;
    }

    internal void WriteObjectMember(
        CborObjectMemberMetadata member,
        object target,
        object? value)
    {
        if (member.Field != null)
        {
            member.Field.SetValue(target, value);
            return;
        }
        if (member.Property != null)
        {
            member.Property.SetValue(target, value);
            return;
        }
        Fail(
            "CBOR_MAPPING_MEMBER_METADATA_INVALID",
            "The reflected member metadata is incomplete.",
            CurrentPath
        );
    }

    internal CborValue EncodeBoolean(object value)
    {
        CborValue result = new CborValue();
        result.Kind = CborValueKind.SimpleValue;
        result.SimpleValue = (bool)value
            ? CborSimpleValues.True
            : CborSimpleValues.False;
        return result;
    }

    internal CborValue EncodeInteger(object value, string typeName)
    {
        BigInteger integer;
        if (typeName == "System.SByte")
        {
            integer = new BigInteger((sbyte)value);
        }
        else if (typeName == "System.Byte")
        {
            integer = new BigInteger((byte)value);
        }
        else if (typeName == "System.Int16")
        {
            integer = new BigInteger((short)value);
        }
        else if (typeName == "System.UInt16")
        {
            integer = new BigInteger((ushort)value);
        }
        else if (typeName == "System.Int32")
        {
            integer = new BigInteger((int)value);
        }
        else if (typeName == "System.UInt32")
        {
            integer = new BigInteger((uint)value);
        }
        else if (typeName == "System.Int64")
        {
            integer = new BigInteger((long)value);
        }
        else if (typeName == "System.UInt64")
        {
            integer = new BigInteger((ulong)value);
        }
        else
        {
            integer = new BigInteger((char)value);
        }
        return CreateBasicInteger(integer);
    }

    internal CborValue EncodeFloating(object value, string typeName)
    {
        if (typeName == "System.Half")
        {
            ushort bits = BitConverter.HalfToUInt16Bits((Half)value);
            return CreateFloat(CborHalfPrecision.CreateHalfFloat(bits));
        }
        if (typeName == "System.Single")
        {
            uint bits = BitConverter.SingleToUInt32Bits((float)value);
            return CreateFloat(CborHalfPrecision.CreateSingleFloat(bits));
        }
        ulong doubleBits = BitConverter.DoubleToUInt64Bits((double)value);
        return CreateFloat(CborHalfPrecision.CreateDoubleFloat(doubleBits));
    }

    internal CborValue EncodeDecimal(object value)
    {
        decimal number = (decimal)value;
        int[] bits = decimal.GetBits(number);
        int scale = (bits[3] >> 16) & 0x7f;
        bool negative = (bits[3] & int.MinValue) != 0;
        BigInteger low = new BigInteger((long)bits[0] & 0xffffffffL);
        BigInteger middle = new BigInteger((long)bits[1] & 0xffffffffL);
        BigInteger high = new BigInteger((long)bits[2] & 0xffffffffL);
        BigInteger mantissa = low + (middle << 32) + (high << 64);
        if (negative)
        {
            mantissa = -mantissa;
        }
        BigInteger[] pair = [new BigInteger(-scale), mantissa];
        CborTagResult encoded = CborBuiltInTags.BigNumberHandler.EncodeTag(
            CborTagNumbers.DecimalFraction,
            pair
        );
        if (!encoded.IsValid || encoded.Value == null)
        {
            Fail(
                encoded.ErrorCode ?? "CBOR_MAPPING_DECIMAL_ENCODING_FAILED",
                encoded.ErrorMessage ?? "The decimal value could not be represented as a CBOR decimal fraction.",
                "$"
            );
        }
        return CreateTag(
            CborTagNumbers.DecimalFraction,
            encoded.Value ?? new CborValue()
        );
    }

    internal CborValue EncodeBytes(object value, string typeName)
    {
        byte[] bytes;
        if (typeName == "System.Byte[]")
        {
            bytes = CopyBytes((byte[])value);
        }
        else if (typeName.StartsWith("System.ReadOnlyMemory`1", StringComparison.Ordinal))
        {
            bytes = ((ReadOnlyMemory<byte>)value).ToArray();
        }
        else
        {
            bytes = ((Memory<byte>)value).ToArray();
        }
        CborValue result = new CborValue();
        result.Kind = CborValueKind.ByteString;
        result.ByteStringValue = bytes;
        result.LengthKind = CborLengthKind.Definite;
        return result;
    }

    internal CborValue EncodeDateTime(object value, string typeName)
    {
        DateTimeOffset instant = typeName == "System.DateTimeOffset"
            ? (DateTimeOffset)value
            : new DateTimeOffset((DateTime)value);
        CborTagResult encoded = CborBuiltInTags.DateTimeHandler.EncodeTag(
            CborTagNumbers.DateTimeString,
            instant
        );
        if (!encoded.IsValid || encoded.Value == null)
        {
            Fail(
                encoded.ErrorCode ?? "CBOR_MAPPING_DATE_TIME_ENCODING_FAILED",
                encoded.ErrorMessage ?? "The date-time value could not be encoded.",
                "$"
            );
        }
        return CreateTag(
            CborTagNumbers.DateTimeString,
            encoded.Value ?? new CborValue()
        );
    }

    internal CborValue EncodeUri(object value)
    {
        CborTagResult encoded = CborBuiltInTags.HintHandler.EncodeTag(
            CborTagNumbers.Uri,
            value
        );
        if (!encoded.IsValid || encoded.Value == null)
        {
            Fail(
                encoded.ErrorCode ?? "CBOR_MAPPING_URI_ENCODING_FAILED",
                encoded.ErrorMessage ?? "The URI value could not be encoded.",
                "$"
            );
        }
        return CreateTag(CborTagNumbers.Uri, encoded.Value ?? new CborValue());
    }

    internal CborValue EncodeGuid(object value)
    {
        Guid identifier = (Guid)value;
        CborValue bytes = new CborValue();
        bytes.Kind = CborValueKind.ByteString;
        bytes.ByteStringValue = identifier.ToByteArray(true);
        bytes.LengthKind = CborLengthKind.Definite;
        return CreateTag(CborTagNumbers.Uuid, bytes);
    }

    internal CborValue EncodeEnum(object value, Type enumType)
    {
        Type underlying = Enum.GetUnderlyingType(enumType);
        string underlyingName = TypeName(underlying);
        BigInteger integer;
        if (underlyingName == "System.Byte"
            || underlyingName == "System.UInt16"
            || underlyingName == "System.UInt32"
            || underlyingName == "System.UInt64")
        {
            integer = new BigInteger(System.Convert.ToUInt64(value, CultureInfo.InvariantCulture));
        }
        else
        {
            integer = new BigInteger(System.Convert.ToInt64(value, CultureInfo.InvariantCulture));
        }
        return CreateBasicInteger(integer);
    }

    internal object DecodeBoolean(CborValue value)
    {
        if (value.Kind != CborValueKind.SimpleValue
            || (value.SimpleValue != CborSimpleValues.False
                && value.SimpleValue != CborSimpleValues.True))
        {
            Fail("CBOR_MAPPING_BOOLEAN_REQUIRED", "A CBOR boolean is required.", "$" );
        }
        return value.SimpleValue == CborSimpleValues.True;
    }

    internal object DecodeIntegerTarget(CborValue value, Type targetType)
    {
        BigInteger integer = ReadInteger(value, "$" );
        return ConvertIntegerToType(integer, targetType, "$" );
    }

    internal object DecodeFloatingTarget(CborValue value, string typeName)
    {
        double number = ReadFloating(value, "$" );
        if (typeName == "System.Double")
        {
            return number;
        }
        if (typeName == "System.Single")
        {
            float converted = (float)number;
            if (!double.IsInfinity(number) && float.IsInfinity(converted))
            {
                Fail(
                    "CBOR_MAPPING_FLOAT_OVERFLOW",
                    "The CBOR number is outside the Single range.",
                    "$"
                );
            }
            return converted;
        }
        Half half = (Half)number;
        if (!double.IsInfinity(number) && Half.IsInfinity(half))
        {
            Fail(
                "CBOR_MAPPING_FLOAT_OVERFLOW",
                "The CBOR number is outside the Half range.",
                "$"
            );
        }
        return half;
    }

    internal object DecodeDecimal(CborValue value)
    {
        if (value.Kind == CborValueKind.UnsignedInteger
            || value.Kind == CborValueKind.NegativeInteger
            || value.TagNumber == CborTagNumbers.PositiveBignum
            || value.TagNumber == CborTagNumbers.NegativeBignum)
        {
            return DecimalFromPair(BigInteger.Zero, ReadInteger(value, "$" ));
        }
        if (value.Kind != CborValueKind.Tag
            || value.TagNumber != CborTagNumbers.DecimalFraction
            || value.TaggedValue == null)
        {
            Fail(
                "CBOR_MAPPING_DECIMAL_REQUIRED",
                "A CBOR integer or decimal-fraction tag is required.",
                "$"
            );
        }
        CborTagResult decoded = CborBuiltInTags.BigNumberHandler.DecodeTag(
            CborTagNumbers.DecimalFraction,
            value.TaggedValue ?? new CborValue()
        );
        BigInteger[]? pair = decoded.NativeValue as BigInteger[];
        if (!decoded.IsValid || pair == null || pair.Length != 2)
        {
            Fail(
                decoded.ErrorCode ?? "CBOR_MAPPING_DECIMAL_INVALID",
                decoded.ErrorMessage ?? "The CBOR decimal fraction is invalid.",
                "$"
            );
        }
        BigInteger[] activePair = pair ?? [];
        return DecimalFromPair(activePair[0], activePair[1]);
    }

    internal object DecodeBytes(CborValue value, Type targetType, string typeName)
    {
        if (value.Kind != CborValueKind.ByteString || value.ByteStringValue == null)
        {
            Fail("CBOR_MAPPING_BYTE_STRING_REQUIRED", "A CBOR byte string is required.", "$" );
        }
        byte[] bytes = CopyBytes(value.ByteStringValue ?? []);
        if (typeName == "System.Byte[]")
        {
            return bytes;
        }
        string definition = targetType.GetGenericTypeDefinition().FullName ?? "";
        if (definition == "System.ReadOnlyMemory`1")
        {
            return new ReadOnlyMemory<byte>(bytes);
        }
        return new Memory<byte>(bytes);
    }

    internal object DecodeDateTime(CborValue value, string typeName)
    {
        if (value.Kind != CborValueKind.Tag
            || (value.TagNumber != CborTagNumbers.DateTimeString
                && value.TagNumber != CborTagNumbers.EpochDateTime)
            || value.TaggedValue == null)
        {
            Fail(
                "CBOR_MAPPING_DATE_TIME_REQUIRED",
                "A standard CBOR date-time tag is required.",
                "$"
            );
        }
        CborTagResult decoded = CborBuiltInTags.DateTimeHandler.DecodeTag(
            value.TagNumber,
            value.TaggedValue ?? new CborValue()
        );
        object? native = decoded.NativeValue;
        if (!decoded.IsValid || native == null
            || TypeName(native.GetType()) != "System.DateTimeOffset")
        {
            Fail(
                decoded.ErrorCode ?? "CBOR_MAPPING_DATE_TIME_INVALID",
                decoded.ErrorMessage ?? "The CBOR date-time tag is invalid.",
                "$"
            );
        }
        DateTimeOffset instant = (DateTimeOffset)(native ?? DateTimeOffset.UnixEpoch);
        if (typeName == "System.DateTimeOffset")
        {
            return instant;
        }
        return instant.UtcDateTime;
    }

    internal object DecodeUri(CborValue value)
    {
        if (value.Kind != CborValueKind.Tag
            || value.TagNumber != CborTagNumbers.Uri
            || value.TaggedValue == null)
        {
            Fail("CBOR_MAPPING_URI_REQUIRED", "A CBOR URI tag is required.", "$" );
        }
        CborTagResult decoded = CborBuiltInTags.HintHandler.DecodeTag(
            CborTagNumbers.Uri,
            value.TaggedValue ?? new CborValue()
        );
        if (!decoded.IsValid || decoded.NativeValue == null)
        {
            Fail(
                decoded.ErrorCode ?? "CBOR_MAPPING_URI_INVALID",
                decoded.ErrorMessage ?? "The CBOR URI tag is invalid.",
                "$"
            );
        }
        return decoded.NativeValue ?? new Uri("", UriKind.Relative);
    }

    internal object DecodeGuid(CborValue value)
    {
        CborValue content = value.TaggedValue ?? new CborValue();
        if (value.Kind != CborValueKind.Tag
            || value.TagNumber != CborTagNumbers.Uuid
            || content.Kind != CborValueKind.ByteString
            || content.ByteStringValue == null
            || content.ByteStringValue.Length != 16)
        {
            Fail(
                "CBOR_MAPPING_UUID_REQUIRED",
                "A UUID tag containing exactly sixteen bytes is required.",
                "$"
            );
        }
        byte[] bytes = content.ByteStringValue ?? [];
        return new Guid(bytes, true);
    }

    internal object DecodeEnum(CborValue value, Type enumType)
    {
        BigInteger integer = ReadInteger(value, "$" );
        Type underlying = Enum.GetUnderlyingType(enumType);
        object converted = ConvertIntegerToType(integer, underlying, "$" );
        return Enum.ToObject(enumType, converted);
    }

    internal BigInteger ReadInteger(CborValue value, string path)
    {
        if (value.Kind == CborValueKind.UnsignedInteger)
        {
            if (value.IntegerValue < BigInteger.Zero)
            {
                Fail("CBOR_MAPPING_INTEGER_SIGN_INVALID", "An unsigned integer is negative.", path);
            }
            return value.IntegerValue;
        }
        if (value.Kind == CborValueKind.NegativeInteger)
        {
            if (value.IntegerValue >= BigInteger.Zero)
            {
                Fail("CBOR_MAPPING_INTEGER_SIGN_INVALID", "A negative integer is not negative.", path);
            }
            return value.IntegerValue;
        }
        if (value.Kind == CborValueKind.Tag
            && (value.TagNumber == CborTagNumbers.PositiveBignum
                || value.TagNumber == CborTagNumbers.NegativeBignum)
            && value.TaggedValue != null)
        {
            CborTagResult decoded = CborBuiltInTags.BigNumberHandler.DecodeTag(
                value.TagNumber,
                value.TaggedValue
            );
            if (decoded.IsValid && decoded.NativeValue != null
                && TypeName(decoded.NativeValue.GetType()) == "System.Numerics.BigInteger")
            {
                return (BigInteger)decoded.NativeValue;
            }
            Fail(
                decoded.ErrorCode ?? "CBOR_MAPPING_BIGNUM_INVALID",
                decoded.ErrorMessage ?? "The CBOR bignum is invalid.",
                path
            );
        }
        CborMappingOptions activeOptions = Options ?? new CborMappingOptions();
        if (activeOptions.AllowIntegerNarrowing
            && value.Kind == CborValueKind.Float
            && value.FloatValue != null)
        {
            double number = value.FloatValue.Value;
            if (!double.IsNaN(number)
                && !double.IsInfinity(number)
                && number == Math.Truncate(number))
            {
                return new BigInteger(number);
            }
        }
        Fail("CBOR_MAPPING_INTEGER_REQUIRED", "A CBOR integer is required.", path);
        return BigInteger.Zero;
    }

    internal double ReadFloating(CborValue value, string path)
    {
        if (value.Kind == CborValueKind.Float && value.FloatValue != null)
        {
            return value.FloatValue.Value;
        }
        BigInteger integer = ReadInteger(value, path);
        double number = (double)integer;
        if (double.IsInfinity(number))
        {
            Fail(
                "CBOR_MAPPING_FLOAT_OVERFLOW",
                "The CBOR integer is outside the binary64 range.",
                path
            );
        }
        CborMappingOptions activeOptions = Options ?? new CborMappingOptions();
        if (!activeOptions.AllowIntegerNarrowing && new BigInteger(number) != integer)
        {
            Fail(
                "CBOR_MAPPING_INTEGER_PRECISION_LOSS",
                "The CBOR integer cannot be represented exactly as a floating-point value.",
                path
            );
        }
        return number;
    }

    internal CborValue CreateIntegerOrBignum(BigInteger value)
    {
        if (value >= MinimumBasicInteger && value <= MaximumBasicInteger)
        {
            return CreateBasicInteger(value);
        }
        ulong tagNumber = value < BigInteger.Zero
            ? CborTagNumbers.NegativeBignum
            : CborTagNumbers.PositiveBignum;
        CborTagResult encoded = CborBuiltInTags.BigNumberHandler.EncodeTag(tagNumber, value);
        if (!encoded.IsValid || encoded.Value == null)
        {
            Fail(
                encoded.ErrorCode ?? "CBOR_MAPPING_BIGNUM_ENCODING_FAILED",
                encoded.ErrorMessage ?? "The BigInteger could not be encoded.",
                "$"
            );
        }
        return CreateTag(tagNumber, encoded.Value ?? new CborValue());
    }

    internal CborValue CreateBasicInteger(BigInteger value)
    {
        CborValue result = new CborValue();
        result.Kind = value < BigInteger.Zero
            ? CborValueKind.NegativeInteger
            : CborValueKind.UnsignedInteger;
        result.IntegerValue = value;
        return result;
    }

    internal CborValue CreateFloat(CborFloatValue value)
    {
        CborValue result = new CborValue();
        result.Kind = CborValueKind.Float;
        result.FloatValue = value;
        result.FloatWidth = value.Width;
        return result;
    }

    internal CborValue CreateTag(ulong tagNumber, CborValue content)
    {
        CborValue result = new CborValue();
        result.Kind = CborValueKind.Tag;
        result.TagNumber = tagNumber;
        result.TaggedValue = content;
        return result;
    }

    internal CborValue CreateNull()
    {
        CborValue result = new CborValue();
        result.Kind = CborValueKind.SimpleValue;
        result.SimpleValue = CborSimpleValues.Null;
        return result;
    }

    internal byte[] CopyBytes(byte[] source)
    {
        byte[] copy = new byte[source.Length];
        Buffer.BlockCopy(source, 0, copy, 0, source.Length);
        return copy;
    }

    internal object ConvertIntegerToType(BigInteger value, Type targetType, string path)
    {
        string typeName = TypeName(targetType);
        BigInteger minimum;
        BigInteger maximum;
        if (typeName == "System.SByte")
        {
            minimum = new BigInteger(sbyte.MinValue);
            maximum = new BigInteger(sbyte.MaxValue);
        }
        else if (typeName == "System.Byte")
        {
            minimum = BigInteger.Zero;
            maximum = new BigInteger(byte.MaxValue);
        }
        else if (typeName == "System.Int16")
        {
            minimum = new BigInteger(short.MinValue);
            maximum = new BigInteger(short.MaxValue);
        }
        else if (typeName == "System.UInt16" || typeName == "System.Char")
        {
            minimum = BigInteger.Zero;
            maximum = new BigInteger(ushort.MaxValue);
        }
        else if (typeName == "System.Int32")
        {
            minimum = new BigInteger(int.MinValue);
            maximum = new BigInteger(int.MaxValue);
        }
        else if (typeName == "System.UInt32")
        {
            minimum = BigInteger.Zero;
            maximum = new BigInteger(uint.MaxValue);
        }
        else if (typeName == "System.Int64")
        {
            minimum = new BigInteger(long.MinValue);
            maximum = new BigInteger(long.MaxValue);
        }
        else if (typeName == "System.UInt64")
        {
            minimum = BigInteger.Zero;
            maximum = new BigInteger(ulong.MaxValue);
        }
        else
        {
            Fail(
                "CBOR_MAPPING_INTEGER_TARGET_UNSUPPORTED",
                $"The integer target type {typeName} is unsupported.",
                path
            );
            return 0;
        }
        if (value < minimum || value > maximum)
        {
            Fail(
                "CBOR_MAPPING_INTEGER_OVERFLOW",
                $"The CBOR integer is outside the range of {typeName}.",
                path
            );
        }
        if (typeName == "System.SByte")
        {
            return (sbyte)value;
        }
        if (typeName == "System.Byte")
        {
            return (byte)value;
        }
        if (typeName == "System.Int16")
        {
            return (short)value;
        }
        if (typeName == "System.UInt16")
        {
            return (ushort)value;
        }
        if (typeName == "System.Char")
        {
            return (char)(ushort)value;
        }
        if (typeName == "System.Int32")
        {
            return (int)value;
        }
        if (typeName == "System.UInt32")
        {
            return (uint)value;
        }
        if (typeName == "System.Int64")
        {
            return (long)value;
        }
        return (ulong)value;
    }

    internal object DecimalFromPair(BigInteger exponent, BigInteger mantissa)
    {
        if (mantissa == BigInteger.Zero)
        {
            return decimal.Zero;
        }
        if (exponent > new BigInteger(28) || exponent < new BigInteger(-int.MaxValue))
        {
            Fail(
                "CBOR_MAPPING_DECIMAL_OVERFLOW",
                "The decimal-fraction exponent is outside the Decimal range.",
                "$"
            );
        }
        if (exponent >= BigInteger.Zero)
        {
            int power = (int)exponent;
            while (power > 0)
            {
                mantissa = mantissa * 10;
                if (BigInteger.Abs(mantissa) > MaximumDecimalMagnitude)
                {
                    Fail(
                        "CBOR_MAPPING_DECIMAL_OVERFLOW",
                        "The decimal fraction is outside the Decimal range.",
                        "$"
                    );
                }
                power--;
            }
            return ToDecimalBits(mantissa, 0);
        }
        int scale = (int)(-exponent);
        if (scale > 28)
        {
            int requiredZeros = scale - 28;
            string digits = BigInteger.Abs(mantissa).ToString(CultureInfo.InvariantCulture);
            if (requiredZeros > digits.Length)
            {
                Fail(
                    "CBOR_MAPPING_DECIMAL_OVERFLOW",
                    "The decimal fraction has more scale than Decimal can represent.",
                    "$"
                );
            }
            while (requiredZeros > 0)
            {
                if (mantissa % 10 != BigInteger.Zero)
                {
                    Fail(
                        "CBOR_MAPPING_DECIMAL_PRECISION_LOSS",
                        "The decimal fraction cannot be reduced to Decimal scale without losing precision.",
                        "$"
                    );
                }
                mantissa = mantissa / 10;
                scale--;
                requiredZeros--;
            }
        }
        return ToDecimalBits(mantissa, scale);
    }

    internal object ToDecimalBits(BigInteger mantissa, int scale)
    {
        bool negative = mantissa < BigInteger.Zero;
        BigInteger magnitude = BigInteger.Abs(mantissa);
        if (magnitude > MaximumDecimalMagnitude || scale < 0 || scale > 28)
        {
            Fail(
                "CBOR_MAPPING_DECIMAL_OVERFLOW",
                "The decimal fraction is outside the Decimal range.",
                "$"
            );
        }
        BigInteger mask = new BigInteger(uint.MaxValue);
        int low = ToSignedBits(magnitude & mask);
        int middle = ToSignedBits((magnitude >> 32) & mask);
        int high = ToSignedBits((magnitude >> 64) & mask);
        return new decimal(low, middle, high, negative, (byte)scale);
    }

    internal int ToSignedBits(BigInteger value)
    {
        if (value <= new BigInteger(int.MaxValue))
        {
            return (int)value;
        }
        return (int)(value - (BigInteger.One << 32));
    }

    internal void Fail(string code, string message, string path)
    {
        string effectivePath = path == "$" ? CurrentPath : path;
        throw new CborMappingException(code, message, -1, effectivePath);
    }
}
