using System.Reflection;

namespace gltf.Tests.Api.Static;

[TestFixture]
public class PublicStaticDataInventoryTests
{
    [Test]
    public void StaticDataInventory_ContainsEveryExpectedClassAndConstant()
    {
        Type[] types = typeof(GltfDocument).Assembly.GetExportedTypes()
            .Where(type => type.IsClass && type.IsAbstract && type.IsSealed)
            .Where(type => type.GetFields(
                BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Any(field => field.IsLiteral))
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .ToArray();
        string[] expectedNames =
        [
            nameof(GltfConstants),
            nameof(GltfExtensionNames),
            nameof(GltfIssueCodes),
            nameof(GltfMimeTypes),
            nameof(GltfSemantics)
        ];
        int[] expectedCounts = [28, 69, 201, 10, 13];

        Assert.Multiple(() =>
        {
            Assert.That(types.Select(type => type.Name), Is.EqualTo(expectedNames));
            Assert.That(types.Select(CountConstants), Is.EqualTo(expectedCounts));
            Assert.That(types.Sum(CountConstants), Is.EqualTo(321));
        });
    }

    private static int CountConstants(Type type)
    {
        return type.GetFields(
                BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Count(field => field.IsLiteral);
    }
}
