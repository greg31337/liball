using System.Reflection;

namespace odf.Tests;

public class PublicApiConstructionTests
{
    [Test]
    public void EveryDataFieldAndOptionDefaultIsSafeAndIndependent()
    {
        Type[] dataTypes = PublicTypes()
            .Where(IsDataClass)
            .OrderBy(type => type.Name)
            .ToArray();
        int fieldCount = 0;

        Assert.Multiple(() =>
        {
            Assert.That(dataTypes, Has.Length.GreaterThan(200));
            foreach (Type type in dataTypes)
            {
                object first = Activator.CreateInstance(type)!;
                object second = Activator.CreateInstance(type)!;
                FieldInfo[] fields = type.GetFields(
                    BindingFlags.Public
                        | BindingFlags.Instance
                        | BindingFlags.DeclaredOnly);

                Assert.That(first, Is.TypeOf(type), type.Name);
                Assert.That(second, Is.TypeOf(type), type.Name);
                Assert.That(fields, Is.Not.Empty, type.Name);
                foreach (FieldInfo field in fields)
                {
                    fieldCount = fieldCount + 1;
                    AssertFieldDefault(type, field, first, second);
                }
            }
            Assert.That(fieldCount, Is.GreaterThan(3000));
        });
    }

    [Test]
    public void EveryPublicEnumMemberHasAnInt32RoundTrip()
    {
        Type[] enumTypes = PublicTypes()
            .Where(type => type.IsEnum)
            .OrderBy(type => type.Name)
            .ToArray();
        int memberCount = 0;

        Assert.Multiple(() =>
        {
            Assert.That(enumTypes, Has.Length.GreaterThan(70));
            foreach (Type type in enumTypes)
            {
                Assert.That(
                    Enum.GetUnderlyingType(type),
                    Is.EqualTo(typeof(int)),
                    type.Name);
                FieldInfo[] members = type.GetFields(
                    BindingFlags.Public | BindingFlags.Static);
                Assert.That(members, Is.Not.Empty, type.Name);
                foreach (FieldInfo member in members)
                {
                    memberCount = memberCount + 1;
                    object rawValue = member.GetRawConstantValue()!;
                    object enumValue = Enum.ToObject(type, rawValue);
                    Assert.That(rawValue, Is.TypeOf<int>(), MemberName(type, member));
                    Assert.That(
                        Enum.GetName(type, enumValue),
                        Is.EqualTo(member.Name),
                        MemberName(type, member));
                }
            }
            Assert.That(memberCount, Is.GreaterThan(500));
        });
    }

    [Test]
    public void EveryStaticDataFieldHasANonEmptyStringValue()
    {
        Type[] staticTypes = PublicTypes()
            .Where(type => type.IsAbstract && type.IsSealed)
            .OrderBy(type => type.Name)
            .ToArray();
        int fieldCount = 0;

        Assert.Multiple(() =>
        {
            Assert.That(staticTypes, Has.Length.EqualTo(7));
            foreach (Type type in staticTypes)
            {
                FieldInfo[] fields = type.GetFields(
                    BindingFlags.Public
                        | BindingFlags.Static
                        | BindingFlags.DeclaredOnly);
                Assert.That(fields, Is.Not.Empty, type.Name);
                foreach (FieldInfo field in fields)
                {
                    fieldCount = fieldCount + 1;
                    Assert.That(
                        field.GetValue(null),
                        Is.TypeOf<string>().And.Not.Empty,
                        MemberName(type, field));
                }
            }
            Assert.That(fieldCount, Is.GreaterThan(600));
        });
    }

    [Test]
    public void EveryDirectOdfExceptionPreservesItsMessage()
    {
        Type[] exceptionTypes = PublicTypes()
            .Where(type => type.BaseType == typeof(Exception))
            .OrderBy(type => type.Name)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(exceptionTypes, Has.Length.EqualTo(7));
            foreach (Type type in exceptionTypes)
            {
                string message = "message for " + type.Name;
                Exception exception = (Exception)Activator.CreateInstance(
                    type,
                    message)!;
                Assert.That(exception, Is.TypeOf(type), type.Name);
                Assert.That(exception.Message, Is.EqualTo(message), type.Name);
            }
        });
    }

    static Type[] PublicTypes()
    {
        return typeof(OdfDocument).Assembly.GetTypes()
            .Where(type => type.IsPublic && type.Namespace == "odf")
            .ToArray();
    }

    static bool IsDataClass(Type type)
    {
        if (!type.IsClass || type.IsAbstract || type.BaseType != typeof(object))
        {
            return false;
        }
        return type.GetFields(
            BindingFlags.Public
                | BindingFlags.Instance
                | BindingFlags.DeclaredOnly).Length > 0;
    }

    static void AssertFieldDefault(
        Type type,
        FieldInfo field,
        object first,
        object second)
    {
        object? firstValue = field.GetValue(first);
        object? secondValue = field.GetValue(second);
        string name = MemberName(type, field);

        if (field.FieldType == typeof(string))
        {
            Assert.That(firstValue, Is.Not.Null, name);
            Assert.That(secondValue, Is.EqualTo(firstValue), name);
            return;
        }
        if (field.FieldType.IsArray)
        {
            Assert.That(firstValue, Is.Not.Null, name);
            Assert.That(secondValue, Is.Not.Null, name);
            Assert.That((Array)firstValue!, Is.Empty, name);
            Assert.That((Array)secondValue!, Is.Empty, name);
            Array replacement = Array.CreateInstance(
                field.FieldType.GetElementType()!,
                1);
            field.SetValue(first, replacement);
            Assert.That((Array)field.GetValue(second)!, Is.Empty, name);
            return;
        }
        if (field.FieldType.IsEnum)
        {
            Assert.That(firstValue, Is.Not.Null, name);
            Assert.That(Enum.IsDefined(field.FieldType, firstValue!), Is.True, name);
            Assert.That(secondValue, Is.EqualTo(firstValue), name);
            return;
        }
        if (field.FieldType.IsValueType)
        {
            Assert.That(secondValue, Is.EqualTo(firstValue), name);
            return;
        }
        if (firstValue == null)
        {
            Assert.That(secondValue, Is.Null, name);
            return;
        }

        Assert.That(secondValue, Is.Not.Null, name);
        Assert.That(secondValue, Is.TypeOf(firstValue.GetType()), name);
        Assert.That(secondValue, Is.Not.SameAs(firstValue), name);
    }

    static string MemberName(Type type, FieldInfo field)
    {
        return type.Name + "." + field.Name;
    }
}
