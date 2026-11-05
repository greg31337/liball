using System;
using System.Collections.Generic;
using System.Reflection;
using fix;

namespace fix.Tests;

[TestFixture]
public class PublicApiConstructionTests
{
    [Test]
    public void EveryPublicDataClassCanBeConstructedAndItsFieldsAccessed()
    {
        Type[] exportedTypes = typeof(FixField).Assembly.GetExportedTypes();
        List<Type> dataTypes = [];
        for (int index = 0; index < exportedTypes.Length; index++)
        {
            Type type = exportedTypes[index];
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (type.IsClass && !type.IsAbstract && type.BaseType == typeof(object) && fields.Length > 0)
            {
                dataTypes.Add(type);
            }
        }

        Assert.That(dataTypes, Has.Count.EqualTo(110));
        Assert.Multiple(() =>
        {
            for (int typeIndex = 0; typeIndex < dataTypes.Count; typeIndex++)
            {
                Type type = dataTypes[typeIndex];
                ConstructorInfo? constructor = type.GetConstructor(Type.EmptyTypes);
                Assert.That(constructor, Is.Not.Null, type.FullName);
                object? instance = Activator.CreateInstance(type);
                Assert.That(instance, Is.Not.Null, type.FullName);
                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                for (int fieldIndex = 0; fieldIndex < fields.Length; fieldIndex++)
                {
                    FieldInfo field = fields[fieldIndex];
                    Assert.DoesNotThrow(() => field.GetValue(instance), type.FullName + "." + field.Name);
                }
            }
        });
    }

    [Test]
    public void EveryPublicEnumHasPublishedValues()
    {
        Type[] exportedTypes = typeof(FixField).Assembly.GetExportedTypes();
        List<Type> enumTypes = [];
        for (int index = 0; index < exportedTypes.Length; index++)
        {
            if (exportedTypes[index].IsEnum)
            {
                enumTypes.Add(exportedTypes[index]);
            }
        }

        Assert.That(enumTypes, Has.Count.EqualTo(19));
        Assert.Multiple(() =>
        {
            for (int index = 0; index < enumTypes.Count; index++)
            {
                Array values = Enum.GetValues(enumTypes[index]);
                Assert.That(values.Length, Is.GreaterThan(0), enumTypes[index].FullName);
            }
        });
    }

    [Test]
    public void EveryPublicStaticFieldCanBeRead()
    {
        Type[] exportedTypes = typeof(FixField).Assembly.GetExportedTypes();
        List<Type> staticTypes = [];
        for (int index = 0; index < exportedTypes.Length; index++)
        {
            Type type = exportedTypes[index];
            if (type.IsClass && type.IsAbstract && type.IsSealed)
            {
                staticTypes.Add(type);
            }
        }

        Assert.That(staticTypes, Has.Count.EqualTo(6));
        Assert.Multiple(() =>
        {
            for (int typeIndex = 0; typeIndex < staticTypes.Count; typeIndex++)
            {
                Type type = staticTypes[typeIndex];
                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
                Assert.That(fields.Length, Is.GreaterThan(0), type.FullName);
                for (int fieldIndex = 0; fieldIndex < fields.Length; fieldIndex++)
                {
                    FieldInfo field = fields[fieldIndex];
                    object? value = field.GetValue(null);
                    Assert.That(value, Is.Not.Null, type.FullName + "." + field.Name);
                }
            }
        });
    }

    [Test]
    public void EveryPublicExceptionHasOneUsableMessageConstructor()
    {
        Type[] exportedTypes = typeof(FixField).Assembly.GetExportedTypes();
        List<Type> exceptionTypes = [];
        for (int index = 0; index < exportedTypes.Length; index++)
        {
            if (exportedTypes[index].BaseType == typeof(Exception))
            {
                exceptionTypes.Add(exportedTypes[index]);
            }
        }

        Assert.That(exceptionTypes, Has.Count.EqualTo(8));
        Assert.Multiple(() =>
        {
            for (int index = 0; index < exceptionTypes.Count; index++)
            {
                Type type = exceptionTypes[index];
                ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
                Assert.That(constructors, Has.Length.EqualTo(1), type.FullName);
                object? instance = constructors[0].Invoke(["failure"]);
                Assert.That(instance, Is.InstanceOf<Exception>(), type.FullName);
                if (instance == null)
                {
                    continue;
                }
                Exception exception = (Exception)instance;
                Assert.That(exception.Message, Is.EqualTo("failure"), type.FullName);
            }
        });
    }
}
