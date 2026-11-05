using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxIdAllocatorTests
{
    [Test]
    public void AllocateNext_ReturnsFirstAvailablePrefixedInteger()
    {
        global::docx.DocxIdAllocator allocator = new global::docx.DocxIdAllocator("rId");
        List<string> existingIds = new List<string>
        {
            "rId1",
            "rId3",
            "other1",
            "rIdText",
            "rId0"
        };

        Assert.That(allocator.AllocateNext(existingIds), Is.EqualTo("rId2"));

        existingIds.Add("rId2");

        Assert.That(allocator.AllocateNext(existingIds), Is.EqualTo("rId4"));
    }

    [Test]
    public void GetNumericSuffix_ReturnsPositiveNumericSuffixOnly()
    {
        global::docx.DocxIdAllocator allocator = new global::docx.DocxIdAllocator("rId");

        Assert.That(allocator.GetNumericSuffix("rId12"), Is.EqualTo(12));
        Assert.That(allocator.GetNumericSuffix("rId001"), Is.EqualTo(1));
        Assert.That(allocator.GetNumericSuffix("rId"), Is.EqualTo(-1));
        Assert.That(allocator.GetNumericSuffix("rId0"), Is.EqualTo(-1));
        Assert.That(allocator.GetNumericSuffix("rIdABC"), Is.EqualTo(-1));
        Assert.That(allocator.GetNumericSuffix("id1"), Is.EqualTo(-1));
        Assert.That(allocator.GetNumericSuffix("rId2147483647"), Is.EqualTo(int.MaxValue));
        Assert.That(allocator.GetNumericSuffix("rId2147483648"), Is.EqualTo(-1));
        Assert.That(allocator.GetNumericSuffix("rId99999999999999999999"), Is.EqualTo(-1));
    }

    [Test]
    public void RelationshipSet_AllocatesNextRelationshipId()
    {
        global::docx.DocxRelationshipSet relationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        relationships.AddRelationship(new global::docx.DocxRelationship("rId1", global::docx.DocxConstants.RelationshipTypeImage, "media/image1.png", ""));
        relationships.AddRelationship(new global::docx.DocxRelationship("rId3", global::docx.DocxConstants.RelationshipTypeImage, "media/image3.png", ""));
        relationships.AddRelationship(new global::docx.DocxRelationship("custom", global::docx.DocxConstants.RelationshipTypeImage, "media/custom.png", ""));

        Assert.That(relationships.ContainsRelationshipId("rId1"), Is.True);
        Assert.That(relationships.ContainsRelationshipId("rId2"), Is.False);
        Assert.That(relationships.AllocateRelationshipId(), Is.EqualTo("rId2"));
    }

    [Test]
    public void IdAllocator_RejectsNullAndEmptyInputs()
    {
        global::docx.DocxIdAllocator allocator = new global::docx.DocxIdAllocator("rId");

        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxIdAllocator(null!));
        Assert.Throws<ArgumentException>(() => new global::docx.DocxIdAllocator(""));
        Assert.Throws<ArgumentNullException>(() => allocator.AllocateNext(null!));
        Assert.Throws<ArgumentNullException>(() => allocator.ContainsId(null!, "rId1"));
        Assert.Throws<ArgumentNullException>(() => allocator.ContainsId(new List<string>(), null!));
        Assert.Throws<ArgumentNullException>(() => allocator.GetNumericSuffix(null!));
    }
}
