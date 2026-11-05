using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvLibraryTests
{
    [Test]
    public void Create_ReturnsLibraryFixture()
    {
        CsvLibrary library = CsvLibrary.Create();

        Assert.That(library, Is.Not.Null);
        Assert.That(library.GetNamespaceName(), Is.EqualTo("csv"));
    }

    [Test]
    public void DisplayName_DescribesCsvLibrary()
    {
        CsvLibrary library = CsvLibrary.Create();

        Assert.That(library.GetDisplayName(), Is.EqualTo("Universal CSV Library"));
    }
}
