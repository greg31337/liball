using System.Text;
using iso20022;

namespace iso20022.Tests;

public class DataDictionaryImporterTests
{
    [Test]
    public void ImporterBuildsBusinessConceptsElementsRegistrationAndSemantics()
    {
        RepositorySnapshot snapshot = ImportFixture();
        BusinessComponentDefinition account = snapshot.BusinessComponents[0];
        BusinessComponentDefinition baseAccount = snapshot.BusinessComponents[1];
        BusinessElementDefinition balance = snapshot.BusinessElements.Single(
            element => element.Identifier == "_balance");
        BusinessElementDefinition association = snapshot.BusinessElements.Single(
            element => element.Identifier == "_association");
        ConstraintDefinition constraint = account.Registration.Constraints.Single();

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Identifier, Is.EqualTo("_repository"));
            Assert.That(snapshot.DataDictionary.TargetIdentifier, Is.EqualTo("_dictionary"));
            Assert.That(snapshot.DataDictionary.IsResolved, Is.True);
            Assert.That(snapshot.BusinessComponents.Select(component => component.Identifier),
                Is.EqualTo(new[] { "_account", "_base" }));
            Assert.That(snapshot.BusinessElements, Has.Count.EqualTo(3));
            Assert.That(account.Name, Is.EqualTo("Account"));
            Assert.That(account.Definition, Is.EqualTo("An account concept."));
            Assert.That(account.Registration.Status, Is.EqualTo(RegistrationStatus.Registered));
            Assert.That(account.Registration.StatusValue, Is.EqualTo("Registered"));
            Assert.That(account.Registration.PreviousVersion?.TargetIdentifier, Is.EqualTo("_base"));
            Assert.That(account.Registration.Examples, Is.EqualTo(new[] { "Account example" }));
            Assert.That(account.Registration.SemanticMarkup.Single().Type, Is.EqualTo("synonym"));
            Assert.That(account.Registration.SemanticMarkup.Single().Elements.Single().Name,
                Is.EqualTo("name"));
            Assert.That(account.Registration.SemanticMarkup.Single().Elements.Single().Value,
                Is.EqualTo("Ledger Account"));
            Assert.That(account.Registration.Doclets.Single().Type, Is.EqualTo("definition"));
            Assert.That(account.Registration.Doclets.Single().Value, Is.EqualTo("Detailed account doclet."));
            Assert.That(constraint.Name, Is.EqualTo("PositiveBalance"));
            Assert.That(constraint.Expression, Is.EqualTo("balance >= 0"));
            Assert.That(constraint.ExpressionLanguage, Is.EqualTo("OCL"));
            Assert.That(constraint.Owner.TargetIdentifier, Is.EqualTo("_account"));
            Assert.That(constraint.Registration.Status,
                Is.EqualTo(RegistrationStatus.ProvisionallyRegistered));
            Assert.That(account.SuperType?.TargetIdentifier, Is.EqualTo("_base"));
            Assert.That(account.SuperType?.IsResolved, Is.True);
            Assert.That(baseAccount.Registration.Status, Is.EqualTo(RegistrationStatus.Obsolete));
            Assert.That(baseAccount.Registration.RemovalDate?.LexicalValue, Is.EqualTo("2026-06-30"));
            Assert.That(baseAccount.Registration.NextVersions.Single().TargetIdentifier,
                Is.EqualTo("_account"));
            Assert.That(account.Elements.Select(reference => reference.TargetIdentifier),
                Is.EqualTo(new[] { "_balance", "_association" }));
            Assert.That(balance.Kind, Is.EqualTo(MessageElementKind.Attribute));
            Assert.That(balance.Multiplicity.MinimumOccurs, Is.EqualTo(0));
            Assert.That(balance.Multiplicity.HasMaximumOccurs, Is.True);
            Assert.That(balance.Multiplicity.MaximumOccurs, Is.EqualTo(1));
            Assert.That(balance.IsDerived, Is.True);
            Assert.That(balance.ElementContext.TargetIdentifier, Is.EqualTo("_account"));
            Assert.That(balance.SimpleType?.TargetIdentifier, Is.EqualTo("_text"));
            Assert.That(balance.MemberType.TargetIdentifier, Is.EqualTo("_text"));
            Assert.That(balance.DerivedMessageElements.Single().TargetIdentifier,
                Is.EqualTo("_messageElement"));
            Assert.That(association.Kind, Is.EqualTo(MessageElementKind.AssociationEnd));
            Assert.That(association.Aggregation, Is.EqualTo("Shared"));
            Assert.That(association.ComplexType?.TargetIdentifier, Is.EqualTo("_base"));
            Assert.That(association.Opposite?.TargetIdentifier, Is.EqualTo("_associationOther"));
            Assert.That(account.Location.SourceName, Is.EqualTo("dictionary.iso20022"));
            Assert.That(account.Location.HasLineInformation, Is.True);
        });
    }

    [Test]
    public void ImporterBuildsDataTypeFacetsCodeSetsAndExternalSchemas()
    {
        RepositorySnapshot snapshot = ImportFixture();
        DataTypeDefinition text = snapshot.DataTypes.Single(type => type.Identifier == "_text");
        DataTypeDefinition identifier = snapshot.DataTypes.Single(type => type.Identifier == "_currency");
        DataTypeDefinition indicator = snapshot.DataTypes.Single(type => type.Identifier == "_indicator");
        DataTypeDefinition rate = snapshot.DataTypes.Single(type => type.Identifier == "_rate");
        DataTypeDefinition amount = snapshot.DataTypes.Single(type => type.Identifier == "_amount");
        DataTypeDefinition schema = snapshot.DataTypes.Single(type => type.Identifier == "_schema");
        CodeSetDefinition baseCodes = snapshot.CodeSets.Single(codeSet => codeSet.Identifier == "_baseCodes");
        CodeSetDefinition derivedCodes = snapshot.CodeSets.Single(
            codeSet => codeSet.Identifier == "_derivedCodes");
        CodeSetDefinition externalCodes = snapshot.CodeSets.Single(
            codeSet => codeSet.Identifier == "_externalCodes");
        ExternalSchemaDefinition external = snapshot.ExternalSchemas.Single(
            type => type.Identifier == "_external");
        ExternalSchemaDefinition userDefined = snapshot.ExternalSchemas.Single(
            type => type.Identifier == "_userDefined");

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.DataTypes, Has.Count.EqualTo(7));
            Assert.That(text.Kind, Is.EqualTo(DataTypeKind.Text));
            Assert.That(text.SourceTypeName, Is.EqualTo("Text"));
            Assert.That(text.Facets.Select(facet => facet.Name),
                Is.EqualTo(new[] { "minLength", "maxLength", "pattern" }));
            Assert.That(text.Facets[0].HasIntegerValue, Is.True);
            Assert.That(text.Facets[0].IntegerValue, Is.EqualTo(1));
            Assert.That(text.Facets[1].IntegerValue, Is.EqualTo(35));
            Assert.That(text.Facets[2].Value, Is.EqualTo("[A-Z]+"));
            Assert.That(text.BusinessElements.Single().TargetIdentifier, Is.EqualTo("_balance"));
            Assert.That(text.MessageMembers.Single().TargetIdentifier, Is.EqualTo("_messageElement"));
            Assert.That(identifier.Kind, Is.EqualTo(DataTypeKind.Identifier));
            Assert.That(identifier.IdentificationScheme, Is.EqualTo("urn:iso:currency"));
            Assert.That(indicator.Kind, Is.EqualTo(DataTypeKind.Indicator));
            Assert.That(indicator.MeaningWhenTrue, Is.EqualTo("Enabled"));
            Assert.That(indicator.MeaningWhenFalse, Is.EqualTo("Disabled"));
            Assert.That(rate.BaseValue?.LexicalValue, Is.EqualTo("100.0"));
            Assert.That(rate.BaseValue?.Value, Is.EqualTo(100m));
            Assert.That(rate.UnitCode, Is.EqualTo("PCT"));
            Assert.That(amount.Kind, Is.EqualTo(DataTypeKind.Amount));
            Assert.That(amount.CurrencyIdentifierSet?.TargetIdentifier, Is.EqualTo("_currency"));
            Assert.That(schema.Kind, Is.EqualTo(DataTypeKind.Schema));
            Assert.That(schema.SchemaTypeName, Is.EqualTo("anyURI"));
            Assert.That(snapshot.CodeSets, Has.Count.EqualTo(3));
            Assert.That(baseCodes.Kind, Is.EqualTo(CodeSetKind.Internal));
            Assert.That(baseCodes.Codes.Single().Value, Is.EqualTo("ACTV"));
            Assert.That(baseCodes.Codes.Single().Name, Is.EqualTo("Active"));
            Assert.That(baseCodes.Codes.Single().Owner.TargetIdentifier, Is.EqualTo("_baseCodes"));
            Assert.That(baseCodes.DerivedCodeSets.Single().TargetIdentifier,
                Is.EqualTo("_derivedCodes"));
            Assert.That(derivedCodes.Kind, Is.EqualTo(CodeSetKind.Derived));
            Assert.That(derivedCodes.BaseCodeSet?.TargetIdentifier, Is.EqualTo("_baseCodes"));
            Assert.That(externalCodes.Kind, Is.EqualTo(CodeSetKind.External));
            Assert.That(externalCodes.IdentificationScheme, Is.EqualTo("urn:test:external-codes"));
            Assert.That(external.Kind, Is.EqualTo(DataTypeKind.ExternalSchema));
            Assert.That(external.NamespaceUris, Is.EqualTo(new[] { "urn:test:one", "urn:test:two" }));
            Assert.That(external.ProcessContents, Is.EqualTo("lax"));
            Assert.That(external.IsTechnical, Is.True);
            Assert.That(external.BusinessComponentTrace?.TargetIdentifier, Is.EqualTo("_account"));
            Assert.That(userDefined.Kind, Is.EqualTo(DataTypeKind.UserDefined));
            Assert.That(userDefined.NamespaceMode, Is.EqualTo("any"));
            Assert.That(userDefined.ProcessContents, Is.EqualTo("skip"));
        });
    }

    [TestCase("Text", DataTypeKind.Text)]
    [TestCase("String", DataTypeKind.String)]
    [TestCase("IdentifierSet", DataTypeKind.Identifier)]
    [TestCase("Boolean", DataTypeKind.Boolean)]
    [TestCase("Indicator", DataTypeKind.Indicator)]
    [TestCase("Decimal", DataTypeKind.Decimal)]
    [TestCase("Amount", DataTypeKind.Amount)]
    [TestCase("Quantity", DataTypeKind.Quantity)]
    [TestCase("Rate", DataTypeKind.Rate)]
    [TestCase("Binary", DataTypeKind.Binary)]
    [TestCase("Date", DataTypeKind.Date)]
    [TestCase("DateTime", DataTypeKind.DateTime)]
    [TestCase("Day", DataTypeKind.Day)]
    [TestCase("Duration", DataTypeKind.Duration)]
    [TestCase("Month", DataTypeKind.Month)]
    [TestCase("MonthDay", DataTypeKind.MonthDay)]
    [TestCase("Time", DataTypeKind.Time)]
    [TestCase("Year", DataTypeKind.Year)]
    [TestCase("YearMonth", DataTypeKind.YearMonth)]
    [TestCase("SchemaType", DataTypeKind.Schema)]
    public void ImporterMapsEveryPrimitiveAndSchemaDataTypeFamily(
        string sourceTypeName,
        DataTypeKind expectedKind)
    {
        DataDictionaryImportContext context = new DataDictionaryImportContext(new XmiPassTwoResult());
        DataDictionaryEntityMapper mapper = new DataDictionaryEntityMapper(context);

        Assert.That(mapper.MapDataTypeKind(sourceTypeName), Is.EqualTo(expectedKind));
    }

    [Test]
    public void ImporterPreservesResolvedDerivationGraphAndDeterministicOrder()
    {
        RepositorySnapshot first = ImportFixture();
        RepositorySnapshot second = ImportFixture();
        BusinessComponentDefinition account = first.BusinessComponents[0];

        Assert.Multiple(() =>
        {
            Assert.That(account.DerivedMessageComponents.Single().TargetIdentifier,
                Is.EqualTo("_messageComponent"));
            Assert.That(account.DerivedMessageComponents.Single().IsResolved, Is.True);
            Assert.That(account.DerivedMessageElements.Single().TargetIdentifier,
                Is.EqualTo("_messageElement"));
            Assert.That(first.BusinessComponents.Select(component => component.Identifier),
                Is.EqualTo(second.BusinessComponents.Select(component => component.Identifier)));
            Assert.That(first.DataTypes.Select(type => type.Identifier),
                Is.EqualTo(second.DataTypes.Select(type => type.Identifier)));
            Assert.That(first.CodeSets.Select(codeSet => codeSet.Identifier),
                Is.EqualTo(second.CodeSets.Select(codeSet => codeSet.Identifier)));
            Assert.That(first.References.Any(reference => !reference.IsResolved), Is.False);
            Assert.That(first.References.Select(reference =>
                    reference.SourceIdentifier + "|" + reference.PropertyName + "|" +
                    reference.Position + "|" + reference.TargetIdentifier),
                Is.EqualTo(second.References.Select(reference =>
                    reference.SourceIdentifier + "|" + reference.PropertyName + "|" +
                    reference.Position + "|" + reference.TargetIdentifier)));
        });
    }

    private static RepositorySnapshot ImportFixture()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(CreateDictionaryFixture());
        using MemoryStream source = new MemoryStream(bytes);
        Iso20022Ecore2013Adapter adapter = new Iso20022Ecore2013Adapter();
        XmiPassOneResult passOne = XmiPassOneReader.BuildIndex(
            source,
            "dictionary.iso20022",
            adapter,
            new XmiPassOneOptions());
        XmiPassTwoResult passTwo = XmiPassTwoReader.BuildGraph(
            source,
            "dictionary.iso20022",
            adapter,
            passOne,
            new XmiPassTwoOptions());
        return DataDictionaryImporter.ImportDataDictionary(passTwo);
    }

    private static string CreateDictionaryFixture()
    {
        return "<?xml version='1.0'?>\n" +
            "<iso:Repository xmlns:iso='urn:iso:std:iso:20022:2013:ecore' " +
            "xmlns:xmi='http://www.omg.org/XMI' " +
            "xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmi:id='_repository'>\n" +
            "  <dataDictionary xmi:id='_dictionary'>\n" +
            "    <topLevelDictionaryEntry xsi:type='iso:BusinessComponent' xmi:id='_account' " +
            "name='Account' definition='An account concept.' registrationStatus='Registered' " +
            "previousVersion='_base' superType='_base' derivationComponent='_messageComponent' " +
            "derivationElement='_messageElement' associationDomain='_association'>\n" +
            "      <semanticMarkup xmi:id='_semantic' type='synonym'>\n" +
            "        <elements xmi:id='_semanticElement' name='name' value='Ledger Account'/>\n" +
            "      </semanticMarkup>\n" +
            "      <doclet xmi:id='_doclet' type='definition' content='Detailed account doclet.'/>\n" +
            "      <example>Account example</example>\n" +
            "      <constraint xmi:id='_constraint' name='PositiveBalance' " +
            "definition='Balance cannot be negative.' expression='balance &gt;= 0' " +
            "expressionLanguage='OCL' registrationStatus='Provisionally Registered'/>\n" +
            "      <element xsi:type='iso:BusinessAttribute' xmi:id='_balance' name='Balance' " +
            "definition='Current balance.' registrationStatus='Provisionally Registered' " +
            "minOccurs='0' maxOccurs='1' isDerived='true' simpleType='_text' " +
            "derivation='_messageElement'/>\n" +
            "      <element xsi:type='iso:BusinessAssociationEnd' xmi:id='_association' " +
            "name='RelatedAccount' minOccurs='0' aggregation='Shared' type='_base' " +
            "opposite='_associationOther'/>\n" +
            "    </topLevelDictionaryEntry>\n" +
            "    <topLevelDictionaryEntry xsi:type='iso:BusinessComponent' xmi:id='_base' " +
            "name='BaseAccount' registrationStatus='Obsolete' removalDate='2026-06-30' " +
            "nextVersions='_account'>\n" +
            "      <element xsi:type='iso:BusinessAssociationEnd' xmi:id='_associationOther' " +
            "name='OriginalAccount' type='_account' opposite='_association'/>\n" +
            "    </topLevelDictionaryEntry>\n" +
            "    <topLevelDictionaryEntry xsi:type='iso:MessageComponent' " +
            "xmi:id='_messageComponent' name='AccountMessageComponent'>\n" +
            "      <messageElement xsi:type='iso:MessageAttribute' xmi:id='_messageElement' " +
            "name='Balance' simpleType='_text'/>\n" +
            "    </topLevelDictionaryEntry>\n" +
            "    <topLevelDictionaryEntry xsi:type='iso:Text' xmi:id='_text' name='AccountText' " +
            "registrationStatus='Registered' minLength='1' maxLength='35' pattern='[A-Z]+'/>\n" +
            "    <topLevelDictionaryEntry xsi:type='iso:IdentifierSet' xmi:id='_currency' " +
            "name='CurrencyIdentifier' identificationScheme='urn:iso:currency'/>\n" +
            "    <topLevelDictionaryEntry xsi:type='iso:Indicator' xmi:id='_indicator' " +
            "name='EnabledIndicator' meaningWhenTrue='Enabled' meaningWhenFalse='Disabled'/>\n" +
            "    <topLevelDictionaryEntry xsi:type='iso:Rate' xmi:id='_rate' name='PercentageRate' " +
            "baseValue='100.0' baseUnitCode='PCT' totalDigits='8' fractionDigits='4'/>\n" +
            "    <topLevelDictionaryEntry xsi:type='iso:Amount' xmi:id='_amount' " +
            "name='CurrencyAmount' currencyIdentifierSet='_currency' minInclusive='0.00'/>\n" +
            "    <topLevelDictionaryEntry xsi:type='iso:Date' xmi:id='_date' " +
            "name='SettlementDate' minInclusive='2000-01-01'/>\n" +
            "    <topLevelDictionaryEntry xsi:type='iso:SchemaType' xmi:id='_schema' " +
            "name='AnyUri' kind='anyURI'/>\n" +
            "    <topLevelDictionaryEntry xsi:type='iso:CodeSet' xmi:id='_baseCodes' " +
            "name='StatusCode' derivation='_derivedCodes' minLength='4' maxLength='4'>\n" +
            "      <code xmi:id='_activeCode' name='Active' codeName='ACTV' " +
            "definition='The active state.' registrationStatus='Registered'/>\n" +
            "    </topLevelDictionaryEntry>\n" +
            "    <topLevelDictionaryEntry xsi:type='iso:CodeSet' xmi:id='_derivedCodes' " +
            "name='DerivedStatusCode' trace='_baseCodes'/>\n" +
            "    <topLevelDictionaryEntry xsi:type='iso:CodeSet' xmi:id='_externalCodes' " +
            "name='ExternalStatusCode' identificationScheme='urn:test:external-codes'/>\n" +
            "    <topLevelDictionaryEntry xsi:type='iso:ExternalSchema' xmi:id='_external' " +
            "name='ExternalContent' processContent='lax' isTechnical='true' trace='_account'>\n" +
            "      <namespaceList>urn:test:one</namespaceList>\n" +
            "      <namespaceList>urn:test:two</namespaceList>\n" +
            "    </topLevelDictionaryEntry>\n" +
            "    <topLevelDictionaryEntry xsi:type='iso:UserDefined' xmi:id='_userDefined' " +
            "name='AnyContent' namespace='any' processContents='skip'/>\n" +
            "  </dataDictionary>\n" +
            "  <businessProcessCatalogue xmi:id='_catalogue'/>\n" +
            "</iso:Repository>";
    }
}
