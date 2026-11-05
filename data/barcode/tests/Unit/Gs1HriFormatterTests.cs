namespace barcode.Tests;

public class Gs1HriFormatterTests
{
    [Test]
    public void GenerateHriCreatesOneLinePerElementAndPreservesLiteralParentheses()
    {
        Gs1Element[] elements = new Gs1Element[]
        {
            CreateElement("01", "09506000134352", false),
            CreateElement("10", "LOT(A)42", true)
        };

        Gs1HriResult result = Gs1HriFormatter.GenerateHri(
            elements,
            false,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
            Assert.That(result.Lines, Is.EqualTo(new string[]
            {
                "(01) 09506000134352",
                "(10) LOT(A)42"
            }));
            Assert.That(result.CompactText,
                Is.EqualTo("(01)09506000134352(10)LOT(A)42"));
            Assert.That(result.Elements, Is.EqualTo(elements));
        });
    }

    [Test]
    public void DictionaryTitlesAreOptionalAndBlankTitlesDoNotCreatePadding()
    {
        Gs1Element[] elements = new Gs1Element[]
        {
            CreateElement("01", "09506000134352", false),
            CreateElement("8110", "COUPON", true)
        };

        Gs1HriResult titled = Gs1HriFormatter.GenerateHri(
            elements,
            true,
            false);
        Gs1HriResult untitled = Gs1HriFormatter.GenerateHri(
            elements,
            false,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(titled.Lines, Is.EqualTo(new string[]
            {
                "GTIN (01) 09506000134352",
                "(8110) COUPON"
            }));
            Assert.That(untitled.Lines, Is.EqualTo(new string[]
            {
                "(01) 09506000134352",
                "(8110) COUPON"
            }));
            Assert.That(titled.CompactText,
                Is.EqualTo(untitled.CompactText));
        });
    }

    [Test]
    public void OptimizedOrderPlacesPredefinedKeyAndFieldsBeforeVariableFieldsStably()
    {
        Gs1Element[] elements = new Gs1Element[]
        {
            CreateElement("10", "LOT", true),
            CreateElement("17", "260101", false),
            CreateElement("01", "09506000134352", false),
            CreateElement("21", "SERIAL", true)
        };

        Gs1HriResult result = Gs1HriFormatter.GenerateHri(
            elements,
            false,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(
                result.Elements.Select(element => element.ApplicationIdentifier),
                Is.EqualTo(new string[] { "01", "17", "10", "21" }));
            Assert.That(
                elements.Select(element => element.ApplicationIdentifier),
                Is.EqualTo(new string[] { "10", "17", "01", "21" }));
            Assert.That(
                Gs1HriFormatter.IsVariableLengthElement(
                    CreateElement("17", "260101", true)),
                Is.False);
            Assert.That(
                Gs1HriFormatter.IsVariableLengthElement(
                    CreateElement("10", "LOT", false)),
                Is.True);
            Assert.That(result.Lines, Is.EqualTo(new string[]
            {
                "(01) 09506000134352",
                "(17) 260101",
                "(10) LOT",
                "(21) SERIAL"
            }));
        });
    }

    [Test]
    public void InvalidElementsProduceIssuesAndAreNotRendered()
    {
        Gs1Element invalidIdentifier =
            CreateElement("A1", "VALUE", true);
        Gs1Element emptyValue = CreateElement("10", "", true);
        Gs1Element controlValue = CreateElement("21", "A\u001dB", true);

        Gs1HriResult result = Gs1HriFormatter.GenerateHri(
            new Gs1Element[]
            {
                invalidIdentifier,
                emptyValue,
                controlValue,
                null!
            },
            false,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.CompactText, Is.Empty);
            Assert.That(result.Lines, Is.Empty);
            Assert.That(result.Issues.Select(issue => issue.Code),
                Is.EquivalentTo(new string[]
                {
                    "GS1_HRI_AI_INVALID",
                    "GS1_HRI_VALUE_EMPTY",
                    "GS1_HRI_VALUE_NOT_DISPLAYABLE",
                    "GS1_HRI_ELEMENT_NULL"
                }));
            Assert.That(result.Issues.Select(issue => issue.Field),
                Has.All.EqualTo("HumanReadableInterpretation"));
        });
    }

    static Gs1Element CreateElement(
        string applicationIdentifier,
        string value,
        bool variableLength)
    {
        Gs1Element element = new Gs1Element();
        element.ApplicationIdentifier = applicationIdentifier;
        element.Value = value;
        element.VariableLength = variableLength;
        return element;
    }
}
