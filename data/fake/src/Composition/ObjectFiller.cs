using System;
using System.Collections;
using System.Reflection;
using System.Threading;

namespace fake;

internal class ObjectMember
{
    internal string Name = string.Empty;
    internal Type ValueType = new object().GetType();
    internal FieldInfo? Field;
    internal PropertyInfo? Property;

    internal static ObjectMember FromField(string name, FieldInfo field)
    {
        ObjectMember value = new ObjectMember();
        value.Name = name;
        value.ValueType = field.FieldType;
        value.Field = field;
        return value;
    }

    internal static ObjectMember FromProperty(string name, PropertyInfo property)
    {
        ObjectMember value = new ObjectMember();
        value.Name = name;
        value.ValueType = property.PropertyType;
        value.Property = property;
        return value;
    }

    internal bool CanRead()
    {
        if (Field != null)
            return true;
        return Property != null && Property.GetGetMethod() != null;
    }

    internal bool CanWrite()
    {
        if (Field != null)
            return !Field.IsInitOnly && !Field.IsLiteral;
        return Property != null && Property.GetSetMethod() != null;
    }

    internal object? Read(object target)
    {
        if (Field != null)
            return Field.GetValue(target);
        if (Property == null)
            throw FakeException.Create("An object member has no reflection source.");
        return Property.GetValue(target, null);
    }

    internal void Write(object target, object? value)
    {
        if (Field != null)
            Field.SetValue(target, value);
        else if (Property != null)
            Property.SetValue(target, value, null);
        else
            throw FakeException.Create("An object member has no reflection source.");
    }
}

internal class ObjectMemberPath
{
    internal bool Found;
    internal object? Target;
    internal ObjectMember? Member;
}
