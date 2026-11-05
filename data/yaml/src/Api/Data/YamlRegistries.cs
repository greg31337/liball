using System;
using System.Collections.Generic;

namespace yaml;

/// <summary>
/// Describes a custom YAML tag and the node, CLR type, or scalar matcher associated with it.
/// </summary>
public class YamlTagRegistration
{
    /// <summary>
    /// The YAML tag, using its expanded form when schema processing has resolved it.
    /// </summary>
    public string Tag;
    /// <summary>
    /// The node kind associated with the registered tag.
    /// </summary>
    public YamlNodeKind NodeKind;
    /// <summary>
    /// The CLR type associated with the registration.
    /// </summary>
    public Type? ClrType;
    /// <summary>
    /// A predicate that recognizes scalar text for the registered tag.
    /// </summary>
    public Func<string, YamlScalarStyle, bool>? ScalarMatcher;
    /// <summary>
    /// An interface-based scalar resolver for the registered tag; when present, it takes precedence over <see cref="ScalarMatcher"/>.
    /// </summary>
    public IYamlScalarResolver? ScalarResolver;

    /// <summary>
    /// Initializes a new YAML tag registration with empty or default state.
    /// </summary>
    public YamlTagRegistration()
    {
        Tag = "";
        NodeKind = YamlNodeKind.Unknown;
        ClrType = null;
        ScalarMatcher = null;
        ScalarResolver = null;
    }

    /// <summary>
    /// Creates a tag registration for the supplied tag and node kind without a CLR type or scalar matcher.
    /// </summary>
    /// <param name="tag">The YAML tag in shorthand or expanded form.</param>
    /// <param name="nodeKind">The YAML node kind associated with the tag.</param>
    /// <returns>The resulting YAML tag registration.</returns>
    internal static YamlTagRegistration Create(string tag, YamlNodeKind nodeKind)
    {
        YamlTagRegistration registration = new YamlTagRegistration();
        registration.Tag = tag == null ? "" : tag;
        registration.NodeKind = nodeKind;
        return registration;
    }

    /// <summary>
    /// Determines whether this registration matches the supplied value.
    /// </summary>
    /// <param name="tag">The YAML tag in shorthand or expanded form.</param>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool MatchesTag(string tag)
    {
        return Tag != "" && Tag == tag;
    }

    /// <summary>
    /// Determines whether this registration matches the supplied value.
    /// </summary>
    /// <param name="type">The CLR type associated with the registration.</param>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool MatchesType(Type? type)
    {
        return ClrType != null && type != null && ClrType == type;
    }

    /// <summary>
    /// Determines whether this registration matches the supplied value.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <param name="style">The presentation style assigned to the value.</param>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool MatchesScalar(string value, YamlScalarStyle style)
    {
        if (ScalarResolver != null)
        {
            return ScalarResolver.Matches(value, style);
        }

        if (ScalarMatcher == null)
        {
            return false;
        }

        return ScalarMatcher(value, style);
    }

    /// <summary>
    /// Creates an independent copy of this value and its mutable collections.
    /// </summary>
    /// <returns>The resulting YAML tag registration.</returns>
    internal YamlTagRegistration Copy()
    {
        YamlTagRegistration copy = new YamlTagRegistration();
        copy.Tag = Tag;
        copy.NodeKind = NodeKind;
        copy.ClrType = ClrType;
        copy.ScalarMatcher = ScalarMatcher;
        copy.ScalarResolver = ScalarResolver;
        return copy;
    }
}

/// <summary>
/// Stores custom YAML tag registrations in lookup order.
/// </summary>
public class YamlTagRegistry
{
    /// <summary>
    /// The registrations in lookup order; earlier entries are checked first.
    /// </summary>
    public List<YamlTagRegistration> Registrations;

    /// <summary>
    /// Initializes a new YAML tag registry with empty or default state.
    /// </summary>
    public YamlTagRegistry()
    {
        Registrations = new List<YamlTagRegistration>();
    }

    /// <summary>
    /// Returns the number of stored items.
    /// </summary>
    /// <returns>The number of matching, changed, or stored items.</returns>
    internal int Count()
    {
        return Registrations.Count;
    }

    /// <summary>
    /// Appends a tag registration; registrations are checked in insertion order.
    /// </summary>
    /// <param name="tag">The YAML tag in shorthand or expanded form.</param>
    /// <param name="nodeKind">The YAML node kind associated with the tag.</param>
    /// <returns>The resulting YAML tag registration.</returns>
    internal YamlTagRegistration Register(string tag, YamlNodeKind nodeKind)
    {
        YamlTagRegistration registration = YamlTagRegistration.Create(tag, nodeKind);
        Registrations.Add(registration);
        return registration;
    }

    /// <summary>
    /// Appends a tag registration associated with a CLR type.
    /// </summary>
    /// <param name="tag">The YAML tag in shorthand or expanded form.</param>
    /// <param name="type">The CLR type associated with the registration.</param>
    /// <param name="nodeKind">The YAML node kind associated with the tag.</param>
    /// <returns>The resulting YAML tag registration.</returns>
    internal YamlTagRegistration RegisterType(string tag, Type type, YamlNodeKind nodeKind)
    {
        YamlTagRegistration registration = Register(tag, nodeKind);
        registration.ClrType = type;
        return registration;
    }

    /// <summary>
    /// Appends a scalar tag matcher that is checked in insertion order.
    /// </summary>
    /// <param name="tag">The YAML tag in shorthand or expanded form.</param>
    /// <param name="matcher">The predicate used to recognize matching scalar text.</param>
    /// <returns>The resulting YAML tag registration.</returns>
    internal YamlTagRegistration RegisterScalarResolver(string tag, Func<string, YamlScalarStyle, bool> matcher)
    {
        YamlTagRegistration registration = Register(tag, YamlNodeKind.Scalar);
        registration.ScalarMatcher = matcher;
        return registration;
    }

    /// <summary>
    /// Appends an interface-based scalar tag resolver that is checked in insertion order.
    /// </summary>
    /// <param name="tag">The YAML tag in shorthand or expanded form.</param>
    /// <param name="resolver">The object used to recognize matching scalar text.</param>
    /// <returns>The resulting YAML tag registration.</returns>
    internal YamlTagRegistration RegisterScalarResolverInstance(string tag, IYamlScalarResolver resolver)
    {
        YamlTagRegistration registration = Register(tag, YamlNodeKind.Scalar);
        registration.ScalarResolver = resolver;
        return registration;
    }

    /// <summary>
    /// Returns the tag of the first custom scalar matcher that accepts the supplied text and style.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <param name="style">The presentation style assigned to the value.</param>
    /// <returns>The matching tag, or an empty string when no matcher accepts the scalar.</returns>
    internal string ResolveScalarTag(string value, YamlScalarStyle style)
    {
        int index = 0;
        while (index < Registrations.Count)
        {
            YamlTagRegistration registration = Registrations[index];
            if (registration.NodeKind == YamlNodeKind.Scalar && registration.MatchesScalar(value, style))
            {
                return registration.Tag;
            }

            index = index + 1;
        }

        return "";
    }

    /// <summary>
    /// Finds the first matching value, or returns null when no match exists.
    /// </summary>
    /// <param name="tag">The YAML tag in shorthand or expanded form.</param>
    /// <returns>The resulting YAML tag registration.</returns>
    internal YamlTagRegistration? FindByTag(string tag)
    {
        int index = 0;
        while (index < Registrations.Count)
        {
            if (Registrations[index].MatchesTag(tag))
            {
                return Registrations[index];
            }

            index = index + 1;
        }

        return null;
    }

    /// <summary>
    /// Finds the first matching value, or returns null when no match exists.
    /// </summary>
    /// <param name="type">The CLR type associated with the registration.</param>
    /// <returns>The resulting YAML tag registration.</returns>
    internal YamlTagRegistration? FindByType(Type? type)
    {
        int index = 0;
        while (index < Registrations.Count)
        {
            if (Registrations[index].MatchesType(type))
            {
                return Registrations[index];
            }

            index = index + 1;
        }

        return null;
    }

    /// <summary>
    /// Determines whether a matching value is present.
    /// </summary>
    /// <param name="tag">The YAML tag in shorthand or expanded form.</param>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool ContainsTag(string tag)
    {
        return FindByTag(tag) != null;
    }

    /// <summary>
    /// Removes the matching value and reports whether it was found.
    /// </summary>
    /// <param name="tag">The YAML tag in shorthand or expanded form.</param>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool RemoveTag(string tag)
    {
        int index = 0;
        while (index < Registrations.Count)
        {
            if (Registrations[index].MatchesTag(tag))
            {
                Registrations.RemoveAt(index);
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    /// <summary>
    /// Removes all stored entries and returns this instance to an empty state.
    /// </summary>
    internal void Clear()
    {
        Registrations.Clear();
    }

    /// <summary>
    /// Creates an independent copy of this value and its mutable collections.
    /// </summary>
    /// <returns>The resulting YAML tag registry.</returns>
    internal YamlTagRegistry Copy()
    {
        YamlTagRegistry copy = new YamlTagRegistry();
        int index = 0;
        while (index < Registrations.Count)
        {
            copy.Registrations.Add(Registrations[index].Copy());
            index = index + 1;
        }

        return copy;
    }
}

/// <summary>
/// Describes callbacks that convert a tagged YAML scalar to and from a CLR value.
/// </summary>
public class YamlScalarConverterRegistration
{
    /// <summary>
    /// The YAML tag, using its expanded form when schema processing has resolved it.
    /// </summary>
    public string Tag;
    /// <summary>
    /// The CLR type associated with the registration.
    /// </summary>
    public Type? ClrType;
    /// <summary>
    /// The scalar style requested when the converter writes a value.
    /// </summary>
    public YamlScalarStyle PreferredStyle;
    /// <summary>
    /// The delegate that converts scalar text into a CLR value.
    /// </summary>
    public Func<string, object?>? ReadScalar;
    /// <summary>
    /// The delegate that converts a CLR value into scalar text.
    /// </summary>
    public Func<object, string>? WriteScalar;
    /// <summary>
    /// The interface-based converter used to read and write scalar values; when present, it takes precedence over the delegate fields.
    /// </summary>
    public IYamlScalarConverter? Converter;

    /// <summary>
    /// Initializes a new YAML scalar converter registration with empty or default state.
    /// </summary>
    public YamlScalarConverterRegistration()
    {
        Tag = "";
        ClrType = null;
        PreferredStyle = YamlScalarStyle.Any;
        ReadScalar = null;
        WriteScalar = null;
        Converter = null;
    }

    /// <summary>
    /// Creates a scalar converter registration for the supplied tag and CLR type without conversion delegates.
    /// </summary>
    /// <param name="tag">The YAML tag in shorthand or expanded form.</param>
    /// <param name="type">The CLR type associated with the registration.</param>
    /// <returns>The resulting YAML scalar converter registration.</returns>
    internal static YamlScalarConverterRegistration Create(string tag, Type type)
    {
        YamlScalarConverterRegistration registration = new YamlScalarConverterRegistration();
        registration.Tag = tag == null ? "" : tag;
        registration.ClrType = type;
        return registration;
    }

    /// <summary>
    /// Determines whether this registration matches the supplied value.
    /// </summary>
    /// <param name="tag">The YAML tag in shorthand or expanded form.</param>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool MatchesTag(string tag)
    {
        if (Tag == "" || tag == null || tag == "")
        {
            return false;
        }

        if (Tag == tag)
        {
            return true;
        }

        return YamlScalarResolver.NormalizeStandardTag(Tag)
            == YamlScalarResolver.NormalizeStandardTag(tag);
    }

    /// <summary>
    /// Determines whether this registration matches the supplied value.
    /// </summary>
    /// <param name="type">The CLR type associated with the registration.</param>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool MatchesType(Type? type)
    {
        return ClrType != null && type != null && ClrType == type;
    }

    /// <summary>
    /// Determines whether the requested operation is supported.
    /// </summary>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool CanRead()
    {
        return Converter != null || ReadScalar != null;
    }

    /// <summary>
    /// Determines whether the requested operation is supported.
    /// </summary>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool CanWrite()
    {
        return Converter != null || WriteScalar != null;
    }

    /// <summary>
    /// Reads and advances over the requested input, returning the consumed value.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The loaded or converted value; null is a valid YAML result.</returns>
    internal object? Read(string value)
    {
        if (Converter != null)
        {
            return Converter.Read(value);
        }

        if (ReadScalar == null)
        {
            return null;
        }

        return ReadScalar(value);
    }

    /// <summary>
    /// Converts the supplied value using the registered writer delegate.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The emitted or formatted text.</returns>
    internal string Write(object value)
    {
        if (Converter != null)
        {
            return Converter.Write(value);
        }

        if (WriteScalar == null)
        {
            return "";
        }

        return WriteScalar(value);
    }

    /// <summary>
    /// Creates an independent copy of this value and its mutable collections.
    /// </summary>
    /// <returns>The resulting YAML scalar converter registration.</returns>
    internal YamlScalarConverterRegistration Copy()
    {
        YamlScalarConverterRegistration copy = new YamlScalarConverterRegistration();
        copy.Tag = Tag;
        copy.ClrType = ClrType;
        copy.PreferredStyle = PreferredStyle;
        copy.ReadScalar = ReadScalar;
        copy.WriteScalar = WriteScalar;
        copy.Converter = Converter;
        return copy;
    }
}

/// <summary>
/// Stores custom scalar converters used during object conversion.
/// </summary>
public class YamlScalarConverterRegistry
{
    /// <summary>
    /// The registrations in lookup order; earlier entries are checked first.
    /// </summary>
    public List<YamlScalarConverterRegistration> Registrations;

    /// <summary>
    /// Initializes a new YAML scalar converter registry with empty or default state.
    /// </summary>
    public YamlScalarConverterRegistry()
    {
        Registrations = new List<YamlScalarConverterRegistration>();
    }

    /// <summary>
    /// Returns the number of stored items.
    /// </summary>
    /// <returns>The number of matching, changed, or stored items.</returns>
    internal int Count()
    {
        return Registrations.Count;
    }

    /// <summary>
    /// Appends a scalar converter registration without assigning reader or writer delegates.
    /// </summary>
    /// <param name="tag">The YAML tag in shorthand or expanded form.</param>
    /// <param name="type">The CLR type associated with the registration.</param>
    /// <returns>The resulting YAML scalar converter registration.</returns>
    internal YamlScalarConverterRegistration Register(string tag, Type type)
    {
        YamlScalarConverterRegistration registration = YamlScalarConverterRegistration.Create(tag, type);
        Registrations.Add(registration);
        return registration;
    }

    /// <summary>
    /// Appends a scalar converter with a reader delegate.
    /// </summary>
    /// <param name="tag">The YAML tag in shorthand or expanded form.</param>
    /// <param name="type">The CLR type associated with the registration.</param>
    /// <param name="reader">The delegate that converts incoming data.</param>
    /// <returns>The resulting YAML scalar converter registration.</returns>
    internal YamlScalarConverterRegistration RegisterReader(string tag, Type type, Func<string, object?> reader)
    {
        YamlScalarConverterRegistration registration = Register(tag, type);
        registration.ReadScalar = reader;
        return registration;
    }

    /// <summary>
    /// Appends a scalar converter with a writer delegate.
    /// </summary>
    /// <param name="tag">The YAML tag in shorthand or expanded form.</param>
    /// <param name="type">The CLR type associated with the registration.</param>
    /// <param name="writer">The delegate that converts outgoing data.</param>
    /// <returns>The resulting YAML scalar converter registration.</returns>
    internal YamlScalarConverterRegistration RegisterWriter(string tag, Type type, Func<object, string> writer)
    {
        YamlScalarConverterRegistration registration = Register(tag, type);
        registration.WriteScalar = writer;
        return registration;
    }

    /// <summary>
    /// Appends a scalar converter with both reader and writer delegates.
    /// </summary>
    /// <param name="tag">The YAML tag in shorthand or expanded form.</param>
    /// <param name="type">The CLR type associated with the registration.</param>
    /// <param name="reader">The delegate that converts incoming data.</param>
    /// <param name="writer">The delegate that converts outgoing data.</param>
    /// <returns>The resulting YAML scalar converter registration.</returns>
    internal YamlScalarConverterRegistration RegisterConverter(string tag, Type type, Func<string, object?> reader, Func<object, string> writer)
    {
        YamlScalarConverterRegistration registration = Register(tag, type);
        registration.ReadScalar = reader;
        registration.WriteScalar = writer;
        return registration;
    }

    /// <summary>
    /// Appends an interface-based converter for reading and writing tagged scalar values.
    /// </summary>
    /// <param name="tag">The YAML tag in shorthand or expanded form.</param>
    /// <param name="type">The CLR type associated with the registration.</param>
    /// <param name="converter">The converter implementation.</param>
    /// <returns>The resulting YAML scalar converter registration.</returns>
    internal YamlScalarConverterRegistration RegisterConverterInstance(string tag, Type type, IYamlScalarConverter converter)
    {
        YamlScalarConverterRegistration registration = Register(tag, type);
        registration.Converter = converter;
        return registration;
    }

    /// <summary>
    /// Finds the first matching value, or returns null when no match exists.
    /// </summary>
    /// <param name="tag">The YAML tag in shorthand or expanded form.</param>
    /// <returns>The resulting YAML scalar converter registration.</returns>
    internal YamlScalarConverterRegistration? FindByTag(string tag)
    {
        int index = 0;
        while (index < Registrations.Count)
        {
            if (Registrations[index].MatchesTag(tag))
            {
                return Registrations[index];
            }

            index = index + 1;
        }

        return null;
    }

    /// <summary>
    /// Finds the first matching value, or returns null when no match exists.
    /// </summary>
    /// <param name="type">The CLR type associated with the registration.</param>
    /// <returns>The resulting YAML scalar converter registration.</returns>
    internal YamlScalarConverterRegistration? FindByType(Type? type)
    {
        int index = 0;
        while (index < Registrations.Count)
        {
            if (Registrations[index].MatchesType(type))
            {
                return Registrations[index];
            }

            index = index + 1;
        }

        return null;
    }

    /// <summary>
    /// Removes the matching value and reports whether it was found.
    /// </summary>
    /// <param name="tag">The YAML tag in shorthand or expanded form.</param>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool RemoveTag(string tag)
    {
        int index = 0;
        while (index < Registrations.Count)
        {
            if (Registrations[index].MatchesTag(tag))
            {
                Registrations.RemoveAt(index);
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    /// <summary>
    /// Removes all stored entries and returns this instance to an empty state.
    /// </summary>
    internal void Clear()
    {
        Registrations.Clear();
    }

    /// <summary>
    /// Creates an independent copy of this value and its mutable collections.
    /// </summary>
    /// <returns>The resulting YAML scalar converter registry.</returns>
    internal YamlScalarConverterRegistry Copy()
    {
        YamlScalarConverterRegistry copy = new YamlScalarConverterRegistry();
        int index = 0;
        while (index < Registrations.Count)
        {
            copy.Registrations.Add(Registrations[index].Copy());
            index = index + 1;
        }

        return copy;
    }
}

/// <summary>
/// Describes callbacks that convert an object value before reading or writing YAML.
/// </summary>
public class YamlTypeConverterRegistration
{
    /// <summary>
    /// The CLR type associated with the registration.
    /// </summary>
    public Type? ClrType;
    /// <summary>
    /// The delegate that converts a loaded value into the registered CLR type.
    /// </summary>
    public Func<object, object?>? ReadValue;
    /// <summary>
    /// The delegate that converts the registered CLR type into a YAML-compatible value.
    /// </summary>
    public Func<object, object?>? WriteValue;
    /// <summary>
    /// The interface-based converter used to read and write complete CLR values; when present, it takes precedence over the delegate fields.
    /// </summary>
    public IYamlTypeConverter? Converter;

    /// <summary>
    /// Initializes a new YAML type converter registration with empty or default state.
    /// </summary>
    public YamlTypeConverterRegistration()
    {
        ClrType = null;
        ReadValue = null;
        WriteValue = null;
        Converter = null;
    }

    /// <summary>
    /// Creates a type converter registration for the supplied CLR type without conversion delegates.
    /// </summary>
    /// <param name="type">The CLR type associated with the registration.</param>
    /// <returns>The resulting YAML type converter registration.</returns>
    internal static YamlTypeConverterRegistration Create(Type type)
    {
        YamlTypeConverterRegistration registration = new YamlTypeConverterRegistration();
        registration.ClrType = type;
        return registration;
    }

    /// <summary>
    /// Determines whether this registration matches the supplied value.
    /// </summary>
    /// <param name="type">The CLR type associated with the registration.</param>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool MatchesType(Type? type)
    {
        return ClrType != null && type != null && ClrType == type;
    }

    /// <summary>
    /// Determines whether the requested operation is supported.
    /// </summary>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool CanRead()
    {
        return Converter != null || ReadValue != null;
    }

    /// <summary>
    /// Determines whether the requested operation is supported.
    /// </summary>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool CanWrite()
    {
        return Converter != null || WriteValue != null;
    }

    /// <summary>
    /// Reads and advances over the requested input, returning the consumed value.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The loaded or converted value; null is a valid YAML result.</returns>
    internal object? Read(object value)
    {
        if (Converter != null)
        {
            return Converter.Read(value);
        }

        if (ReadValue == null)
        {
            return value;
        }

        return ReadValue(value);
    }

    /// <summary>
    /// Converts the supplied value using the registered writer delegate.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The loaded or converted value; null is a valid YAML result.</returns>
    internal object? Write(object value)
    {
        if (Converter != null)
        {
            return Converter.Write(value);
        }

        if (WriteValue == null)
        {
            return value;
        }

        return WriteValue(value);
    }

    /// <summary>
    /// Creates an independent copy of this value and its mutable collections.
    /// </summary>
    /// <returns>The resulting YAML type converter registration.</returns>
    internal YamlTypeConverterRegistration Copy()
    {
        YamlTypeConverterRegistration copy = new YamlTypeConverterRegistration();
        copy.ClrType = ClrType;
        copy.ReadValue = ReadValue;
        copy.WriteValue = WriteValue;
        copy.Converter = Converter;
        return copy;
    }
}

/// <summary>
/// Stores custom object converters used during serialization and deserialization.
/// </summary>
public class YamlTypeConverterRegistry
{
    /// <summary>
    /// The registrations in lookup order; earlier entries are checked first.
    /// </summary>
    public List<YamlTypeConverterRegistration> Registrations;

    /// <summary>
    /// Initializes a new YAML type converter registry with empty or default state.
    /// </summary>
    public YamlTypeConverterRegistry()
    {
        Registrations = new List<YamlTypeConverterRegistration>();
    }

    /// <summary>
    /// Returns the number of stored items.
    /// </summary>
    /// <returns>The number of matching, changed, or stored items.</returns>
    internal int Count()
    {
        return Registrations.Count;
    }

    /// <summary>
    /// Appends a type converter registration without assigning reader or writer delegates.
    /// </summary>
    /// <param name="type">The CLR type associated with the registration.</param>
    /// <returns>The resulting YAML type converter registration.</returns>
    internal YamlTypeConverterRegistration Register(Type type)
    {
        YamlTypeConverterRegistration registration = YamlTypeConverterRegistration.Create(type);
        Registrations.Add(registration);
        return registration;
    }

    /// <summary>
    /// Appends a type converter with a reader delegate.
    /// </summary>
    /// <param name="type">The CLR type associated with the registration.</param>
    /// <param name="reader">The delegate that converts incoming data.</param>
    /// <returns>The resulting YAML type converter registration.</returns>
    internal YamlTypeConverterRegistration RegisterReader(Type type, Func<object, object?> reader)
    {
        YamlTypeConverterRegistration registration = Register(type);
        registration.ReadValue = reader;
        return registration;
    }

    /// <summary>
    /// Appends a type converter with a writer delegate.
    /// </summary>
    /// <param name="type">The CLR type associated with the registration.</param>
    /// <param name="writer">The delegate that converts outgoing data.</param>
    /// <returns>The resulting YAML type converter registration.</returns>
    internal YamlTypeConverterRegistration RegisterWriter(Type type, Func<object, object?> writer)
    {
        YamlTypeConverterRegistration registration = Register(type);
        registration.WriteValue = writer;
        return registration;
    }

    /// <summary>
    /// Appends a type converter with both reader and writer delegates.
    /// </summary>
    /// <param name="type">The CLR type associated with the registration.</param>
    /// <param name="reader">The delegate that converts incoming data.</param>
    /// <param name="writer">The delegate that converts outgoing data.</param>
    /// <returns>The resulting YAML type converter registration.</returns>
    internal YamlTypeConverterRegistration RegisterConverter(Type type, Func<object, object?> reader, Func<object, object?> writer)
    {
        YamlTypeConverterRegistration registration = Register(type);
        registration.ReadValue = reader;
        registration.WriteValue = writer;
        return registration;
    }

    /// <summary>
    /// Appends an interface-based converter for reading and writing complete CLR values.
    /// </summary>
    /// <param name="type">The CLR type associated with the registration.</param>
    /// <param name="converter">The converter implementation.</param>
    /// <returns>The resulting YAML type converter registration.</returns>
    internal YamlTypeConverterRegistration RegisterConverterInstance(Type type, IYamlTypeConverter converter)
    {
        YamlTypeConverterRegistration registration = Register(type);
        registration.Converter = converter;
        return registration;
    }

    /// <summary>
    /// Finds the first matching value, or returns null when no match exists.
    /// </summary>
    /// <param name="type">The CLR type associated with the registration.</param>
    /// <returns>The resulting YAML type converter registration.</returns>
    internal YamlTypeConverterRegistration? FindByType(Type? type)
    {
        int index = 0;
        while (index < Registrations.Count)
        {
            if (Registrations[index].MatchesType(type))
            {
                return Registrations[index];
            }

            index = index + 1;
        }

        return null;
    }

    /// <summary>
    /// Removes the matching value and reports whether it was found.
    /// </summary>
    /// <param name="type">The CLR type associated with the registration.</param>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool RemoveType(Type type)
    {
        int index = 0;
        while (index < Registrations.Count)
        {
            if (Registrations[index].MatchesType(type))
            {
                Registrations.RemoveAt(index);
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    /// <summary>
    /// Removes all stored entries and returns this instance to an empty state.
    /// </summary>
    internal void Clear()
    {
        Registrations.Clear();
    }

    /// <summary>
    /// Creates an independent copy of this value and its mutable collections.
    /// </summary>
    /// <returns>The resulting YAML type converter registry.</returns>
    internal YamlTypeConverterRegistry Copy()
    {
        YamlTypeConverterRegistry copy = new YamlTypeConverterRegistry();
        int index = 0;
        while (index < Registrations.Count)
        {
            copy.Registrations.Add(Registrations[index].Copy());
            index = index + 1;
        }

        return copy;
    }
}
