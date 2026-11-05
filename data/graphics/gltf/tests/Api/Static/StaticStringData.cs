using System.Reflection;

namespace gltf.Tests.Api.Static;

internal static class StaticStringData
{
    internal static string[] ReadConstants(Type type)
    {
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
        string[] values = new string[fields.Length];

        for (int index = 0; index < fields.Length; index++)
        {
            object? value = fields[index].GetRawConstantValue();
            Assert.That(value, Is.TypeOf<string>());
            values[index] = (string)value!;
        }

        Array.Sort(values, StringComparer.Ordinal);
        return values;
    }
}
