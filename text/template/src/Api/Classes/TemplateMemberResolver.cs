using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace template;

/// <summary>
/// Resolves member and index access across template values, dictionaries, collections, strings, and permitted CLR members.
/// </summary>
public class TemplateMemberResolver : ITemplateMemberResolver, IConfigurableTemplateMemberResolver
{
    /// <summary>
    /// Specifies the options for this member resolver.
    /// </summary>
    internal TemplateMemberResolverOptions Options;

    /// <summary>
    /// Initializes a new template member resolver and validates any supplied values.
    /// </summary>
    public TemplateMemberResolver()
    {
        Options = new TemplateMemberResolverOptions();
    }

    /// <summary>
    /// Configures member resolution behavior.
    /// </summary>
    /// <param name="options">The options that control member resolution.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>options</c> is <c>null</c>.</exception>
    public void Configure(TemplateMemberResolverOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.Validate();
        Options = options;
    }

    /// <summary>
    /// Creates a member resolver that uses the supplied options.
    /// </summary>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The configured member resolver.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal static TemplateMemberResolver Create(TemplateMemberResolverOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.Validate();
        TemplateMemberResolver resolver = new TemplateMemberResolver();
        resolver.Options = options;
        return resolver;
    }

    /// <summary>
    /// Resolves the requested value using configured template semantics and access policy.
    /// </summary>
    /// <param name="target">The value on which lookup is performed.</param>
    /// <param name="memberName">The member name written in the template.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public TemplateValue ResolveMember(TemplateValue target, string memberName)
    {
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        if (memberName == null)
        {
            throw new ArgumentNullException("memberName");
        }

        if (target.Kind == TemplateValueKind.Missing || target.Kind == TemplateValueKind.Null)
        {
            return TemplateValue.Missing();
        }

        if (!CanAccessMemberName(memberName))
        {
            return TemplateValue.Missing();
        }

        if (target.Kind == TemplateValueKind.Map)
        {
            TemplateValue mapResult = ResolveTemplateMap(target, memberName);
            if (!mapResult.IsMissing())
            {
                return mapResult;
            }

            return ResolveTemplateIntrinsicMember(target, memberName);
        }

        if (target.Kind == TemplateValueKind.List)
        {
            TemplateValue listResult = ResolveTemplateListMember(target, memberName);
            if (!listResult.IsMissing())
            {
                return listResult;
            }

            return ResolveTemplateIntrinsicMember(target, memberName);
        }

        if (target.Kind == TemplateValueKind.String || target.Kind == TemplateValueKind.SafeString)
        {
            return ResolveStringMember(target.StringValue, memberName);
        }

        if (target.Kind == TemplateValueKind.Fragment)
        {
            return ResolveTemplateIntrinsicMember(target, memberName);
        }

        if (target.Kind == TemplateValueKind.Object && target.ObjectValue != null)
        {
            return ResolveObjectMember(target.ObjectValue, memberName);
        }

        return TemplateValue.Missing();
    }

    /// <summary>
    /// Resolves the requested value using configured template semantics and access policy.
    /// </summary>
    /// <param name="target">The value on which lookup is performed.</param>
    /// <param name="index">The index or key used for lookup.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public TemplateValue ResolveIndex(TemplateValue target, TemplateValue index)
    {
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        if (index == null)
        {
            throw new ArgumentNullException("index");
        }

        if (target.Kind == TemplateValueKind.Missing || target.Kind == TemplateValueKind.Null)
        {
            return TemplateValue.Missing();
        }

        if (index.Kind == TemplateValueKind.Number)
        {
            int numericIndex = ToIntegerIndex(index.NumberValue);
            if (numericIndex < 0)
            {
                return TemplateValue.Missing();
            }

            return ResolveNumericIndex(target, numericIndex);
        }

        if (index.Kind == TemplateValueKind.String || index.Kind == TemplateValueKind.SafeString)
        {
            return ResolveStringIndex(target, index.StringValue);
        }

        return TemplateValue.Missing();
    }

    internal TemplateValue ResolveStringIndex(TemplateValue target, string index)
    {
        if (!CanAccessMemberName(index))
        {
            return TemplateValue.Missing();
        }

        if (target.Kind == TemplateValueKind.Map)
        {
            TemplateValue mapResult = ResolveTemplateMap(target, index);
            if (!mapResult.IsMissing())
            {
                return mapResult;
            }

            return ResolveTemplateIntrinsicMember(target, index);
        }

        int numericIndex = ParseIndexText(index);
        if (numericIndex >= 0)
        {
            TemplateValue numericResult = ResolveNumericIndex(target, numericIndex);
            if (!numericResult.IsMissing())
            {
                return numericResult;
            }
        }

        return ResolveMember(target, index);
    }

    internal TemplateValue ResolveNumericIndex(TemplateValue target, int index)
    {
        if (target.Kind == TemplateValueKind.List)
        {
            return target.GetListItem(index);
        }

        if (target.Kind == TemplateValueKind.String || target.Kind == TemplateValueKind.SafeString)
        {
            return ResolveStringCharacterIndex(target.StringValue, index);
        }

        if (target.Kind == TemplateValueKind.Object && target.ObjectValue != null)
        {
            return ResolveObjectIndex(target.ObjectValue, index);
        }

        return TemplateValue.Missing();
    }

    internal TemplateValue ResolveTemplateMap(TemplateValue target, string memberName)
    {
        if (Options.CaseSensitive && Options.AllowsMemberNamingPolicy(TemplateMemberNamingPolicy.Exact) && target.MapValue.ContainsKey(memberName))
        {
            return target.MapValue[memberName];
        }

        foreach (KeyValuePair<string, TemplateValue> pair in target.MapValue)
        {
            if (Options.MemberNamesEqual(pair.Key, memberName) && CanAccessMemberName(pair.Key))
            {
                return pair.Value;
            }
        }

        return TemplateValue.Missing();
    }

    internal TemplateValue ResolveTemplateListMember(TemplateValue target, string memberName)
    {
        int index = ParseIndexText(memberName);
        if (index >= 0)
        {
            return target.GetListItem(index);
        }

        return TemplateValue.Missing();
    }

    internal TemplateValue ResolveTemplateIntrinsicMember(TemplateValue target, string memberName)
    {
        if (!IsCountMember(memberName))
        {
            return TemplateValue.Missing();
        }

        if (target.Kind == TemplateValueKind.List)
        {
            return TemplateValue.FromNumber(target.ListValue.Count);
        }

        if (target.Kind == TemplateValueKind.Map)
        {
            return TemplateValue.FromNumber(target.MapValue.Count);
        }

        if (target.Kind == TemplateValueKind.Fragment)
        {
            return TemplateValue.FromNumber(target.FragmentValue.Count);
        }

        return TemplateValue.Missing();
    }

    internal TemplateValue ResolveStringMember(string value, string memberName)
    {
        if (IsCountMember(memberName))
        {
            return TemplateValue.FromNumber(value.Length);
        }

        int index = ParseIndexText(memberName);
        if (index >= 0)
        {
            return ResolveStringCharacterIndex(value, index);
        }

        return TemplateValue.Missing();
    }

    internal TemplateValue ResolveStringCharacterIndex(string value, int index)
    {
        if (index < 0 || index >= value.Length)
        {
            return TemplateValue.Missing();
        }

        return TemplateValue.FromString(value[index].ToString());
    }

    internal TemplateValue ResolveObjectMember(object instance, string memberName)
    {
        string? stringValue = instance as string;
        if (stringValue != null)
        {
            return ResolveStringMember(stringValue, memberName);
        }

        IDictionary? mutableDictionary = instance as IDictionary;
        if (mutableDictionary == null)
        {
            TemplateValue? readOnlyDictionary = TemplateValueConverter.ConvertReadOnlyDictionary(instance, GetValueConversionPolicy());
            if (readOnlyDictionary != null)
            {
                TemplateValue readOnlyResult = ResolveTemplateMap(readOnlyDictionary, memberName);
                if (!readOnlyResult.IsMissing())
                {
                    return readOnlyResult;
                }

                return ResolveTemplateIntrinsicMember(readOnlyDictionary, memberName);
            }
        }

        if (Options.DictionaryFirst)
        {
            TemplateValue dictionaryResult = ResolveObjectDictionaryMember(instance, memberName);
            if (!dictionaryResult.IsMissing())
            {
                return dictionaryResult;
            }
        }

        TemplateValue collectionResult = ResolveObjectCollectionMember(instance, memberName);
        if (!collectionResult.IsMissing())
        {
            return collectionResult;
        }

        TemplateValue reflectionResult = ResolveReflectedMember(instance, memberName);
        if (!reflectionResult.IsMissing())
        {
            return reflectionResult;
        }

        if (!Options.DictionaryFirst)
        {
            return ResolveObjectDictionaryMember(instance, memberName);
        }

        return TemplateValue.Missing();
    }

    internal TemplateValue ResolveObjectDictionaryMember(object instance, string memberName)
    {
        IDictionary? dictionary = instance as IDictionary;
        if (dictionary == null)
        {
            return TemplateValue.Missing();
        }

        if (Options.CaseSensitive && Options.AllowsMemberNamingPolicy(TemplateMemberNamingPolicy.Exact) && dictionary.Contains(memberName))
        {
            return ConvertResolvedValue(dictionary[memberName]);
        }

        foreach (DictionaryEntry entry in dictionary)
        {
            string key = ConvertKeyToString(entry.Key);
            if (Options.MemberNamesEqual(key, memberName) && CanAccessMemberName(key))
            {
                return ConvertResolvedValue(entry.Value);
            }
        }

        return TemplateValue.Missing();
    }

    internal TemplateValue ResolveObjectCollectionMember(object instance, string memberName)
    {
        if (!IsCountMember(memberName))
        {
            int index = ParseIndexText(memberName);
            if (index >= 0)
            {
                return ResolveObjectIndex(instance, index);
            }

            return TemplateValue.Missing();
        }

        IDictionary? dictionary = instance as IDictionary;
        if (dictionary != null)
        {
            return TemplateValue.FromNumber(dictionary.Count);
        }

        ICollection? collection = instance as ICollection;
        if (collection != null)
        {
            return TemplateValue.FromNumber(collection.Count);
        }

        IEnumerable? enumerable = instance as IEnumerable;
        if (enumerable != null)
        {
            int count = 0;
            foreach (object? item in enumerable)
            {
                count = count + 1;
            }

            return TemplateValue.FromNumber(count);
        }

        return TemplateValue.Missing();
    }

    internal TemplateValue ResolveObjectIndex(object instance, int index)
    {
        if (index < 0)
        {
            return TemplateValue.Missing();
        }

        string? stringValue = instance as string;
        if (stringValue != null)
        {
            return ResolveStringCharacterIndex(stringValue, index);
        }

        IList? list = instance as IList;
        if (list != null)
        {
            if (index >= list.Count)
            {
                return TemplateValue.Missing();
            }

            return ConvertResolvedValue(list[index]);
        }

        IEnumerable? enumerable = instance as IEnumerable;
        if (enumerable != null)
        {
            int currentIndex = 0;
            foreach (object? item in enumerable)
            {
                if (currentIndex == index)
                {
                    return ConvertResolvedValue(item);
                }

                currentIndex = currentIndex + 1;
            }
        }

        return TemplateValue.Missing();
    }

    internal TemplateValue ResolveReflectedMember(object instance, string memberName)
    {
        TemplateValue fieldResult = ResolvePublicField(instance, memberName);
        if (!fieldResult.IsMissing())
        {
            return fieldResult;
        }

        TemplateValue propertyResult = ResolvePublicProperty(instance, memberName);
        if (!propertyResult.IsMissing())
        {
            return propertyResult;
        }

        return ResolvePublicMethod(instance, memberName);
    }

    internal TemplateValue ResolvePublicField(object instance, string memberName)
    {
        if (!Options.AllowPublicFields)
        {
            return TemplateValue.Missing();
        }

        Type type = instance.GetType();
        FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
        int index = 0;
        while (index < fields.Length)
        {
            FieldInfo field = fields[index];
            if (Options.MemberNamesEqual(field.Name, memberName) && CanAccessMemberName(field.Name))
            {
                return ConvertResolvedValue(field.GetValue(instance));
            }

            index = index + 1;
        }

        return TemplateValue.Missing();
    }

    internal TemplateValue ResolvePublicProperty(object instance, string memberName)
    {
        if (!Options.AllowPublicProperties)
        {
            return TemplateValue.Missing();
        }

        Type type = instance.GetType();
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        int index = 0;
        while (index < properties.Length)
        {
            PropertyInfo property = properties[index];
            if (Options.MemberNamesEqual(property.Name, memberName) && CanAccessMemberName(property.Name) && property.CanRead && property.GetIndexParameters().Length == 0)
            {
                return ConvertResolvedValue(property.GetValue(instance, null));
            }

            index = index + 1;
        }

        return TemplateValue.Missing();
    }

    internal TemplateValue ResolvePublicMethod(object instance, string memberName)
    {
        if (!Options.AllowPublicMethods)
        {
            return TemplateValue.Missing();
        }

        Type type = instance.GetType();
        MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
        int index = 0;
        while (index < methods.Length)
        {
            MethodInfo method = methods[index];
            if (Options.MemberNamesEqual(method.Name, memberName) && CanAccessMemberName(method.Name) && Options.IsSafeMethod(method.Name) && !method.IsSpecialName && method.GetParameters().Length == 0)
            {
                return CreateMethodFunction(instance, method);
            }

            index = index + 1;
        }

        return TemplateValue.Missing();
    }

    internal TemplateValue CreateMethodFunction(object instance, MethodInfo method)
    {
        Func<List<TemplateValue>, TemplateValue> function = (List<TemplateValue> arguments) =>
        {
            if (arguments == null)
            {
                throw new ArgumentNullException("arguments");
            }

            if (arguments.Count != 0)
            {
                throw new InvalidOperationException("Template method expects no arguments.");
            }

            object? result = method.Invoke(instance, new object[0]);
            return ConvertResolvedValue(result);
        };

        return TemplateValue.FromFunction(function);
    }

    internal bool IsCountMember(string memberName)
    {
        return Options.NamesEqual(memberName, "count") || Options.NamesEqual(memberName, "length") || Options.NamesEqual(memberName, "size");
    }

    internal bool CanAccessMemberName(string memberName)
    {
        if (Options.SecurityPolicy == null)
        {
            return true;
        }

        return Options.SecurityPolicy.CanAccessMember(memberName);
    }

    internal int ToIntegerIndex(decimal value)
    {
        if (value < 0m || value > 2147483647m)
        {
            return -1;
        }

        int result = (int)value;
        if ((decimal)result != value)
        {
            return -1;
        }

        return result;
    }

    internal int ParseIndexText(string text)
    {
        if (text.Length == 0)
        {
            return -1;
        }

        int index = 0;
        int position = 0;
        while (position < text.Length)
        {
            char current = text[position];
            if (current < '0' || current > '9')
            {
                return -1;
            }

            int digit = current - '0';
            if (index > 214748364)
            {
                return -1;
            }

            if (index == 214748364 && digit > 7)
            {
                return -1;
            }

            index = index * 10 + digit;
            position = position + 1;
        }

        return index;
    }

    internal string ConvertKeyToString(object? key)
    {
        if (key == null)
        {
            return "";
        }

        string? stringKey = key as string;
        if (stringKey != null)
        {
            return stringKey;
        }

        return TemplateValueConverter.ConvertKeyWithCulture(key, GetValueConversionPolicy().Culture);
    }

    internal TemplateValueConversionPolicy GetValueConversionPolicy()
    {
        if (Options.ValueConversionPolicy == null)
        {
            throw new InvalidOperationException("A value conversion policy is required.");
        }

        Options.ValueConversionPolicy.Validate();
        return Options.ValueConversionPolicy;
    }

    internal TemplateValue ConvertResolvedValue(object? value)
    {
        return TemplateValueConverter.ConvertConfigured(value, GetValueConversionPolicy());
    }
}
