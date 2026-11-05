using System.Reflection;

namespace uuid.Tests;

[TestFixture]
public class ConstructorPolicyTests
{
    [Test]
    public void SourceClasses_DeclareAtMostOneInstanceConstructor()
    {
        Type[] types = typeof(Uuid).Assembly.GetTypes();
        int index = 0;
        while (index < types.Length)
        {
            Type type = types[index];
            if (type.IsClass && type.DeclaringType == null)
            {
                ConstructorInfo[] constructors = type.GetConstructors(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly);

                Assert.That(constructors.Length, Is.LessThanOrEqualTo(1), type.FullName);
            }

            index = index + 1;
        }
    }
}
