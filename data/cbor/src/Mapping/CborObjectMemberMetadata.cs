using System;
using System.Reflection;

namespace cbor;

internal class CborObjectMemberMetadata
{
    internal string? Name;
    internal Type? MemberType;
    internal FieldInfo? Field;
    internal PropertyInfo? Property;
}
