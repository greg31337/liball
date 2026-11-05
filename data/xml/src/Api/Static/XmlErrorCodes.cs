namespace xml;

/// <summary>
/// Provides stable XML library and Serialization 3.1 error codes.
/// </summary>
public static class XmlErrorCodes
{
    /// <summary>
    /// Identifies an unclassified XML library failure.
    /// </summary>
    public const string GeneralFailure = "XML0000";
    /// <summary>
    /// Identifies an invalid XML character.
    /// </summary>
    public const string InvalidCharacter = "XML0001";
    /// <summary>
    /// Identifies malformed XML markup.
    /// </summary>
    public const string MalformedMarkup = "XML0002";
    /// <summary>
    /// Identifies unexpected end of input.
    /// </summary>
    public const string UnexpectedEndOfInput = "XML0003";
    /// <summary>
    /// Identifies an invalid XML name.
    /// </summary>
    public const string InvalidName = "XML0004";
    /// <summary>
    /// Identifies an invalid XML or text declaration.
    /// </summary>
    public const string InvalidDeclaration = "XML0005";
    /// <summary>
    /// Identifies multiple document elements.
    /// </summary>
    public const string MultipleDocumentElements = "XML0006";
    /// <summary>
    /// Identifies a missing document element.
    /// </summary>
    public const string MissingDocumentElement = "XML0007";
    /// <summary>
    /// Identifies trailing document content.
    /// </summary>
    public const string TrailingContent = "XML0008";
    /// <summary>
    /// Identifies a duplicate attribute.
    /// </summary>
    public const string DuplicateAttribute = "XML0009";
    /// <summary>
    /// Identifies a mismatched end tag.
    /// </summary>
    public const string MismatchedEndTag = "XML0010";
    /// <summary>
    /// Identifies an invalid character reference.
    /// </summary>
    public const string InvalidCharacterReference = "XML0011";
    /// <summary>
    /// Identifies an undeclared entity.
    /// </summary>
    public const string UndeclaredEntity = "XML0012";
    /// <summary>
    /// Identifies a recursive entity.
    /// </summary>
    public const string RecursiveEntity = "XML0013";
    /// <summary>
    /// Identifies an unbound namespace prefix.
    /// </summary>
    public const string NamespaceUnboundPrefix = "NS0001";
    /// <summary>
    /// Identifies an invalid reserved namespace binding.
    /// </summary>
    public const string NamespaceReservedBinding = "NS0002";
    /// <summary>
    /// Identifies duplicate attributes by expanded name.
    /// </summary>
    public const string NamespaceDuplicateAttribute = "NS0003";
    /// <summary>
    /// Identifies a malformed document type definition.
    /// </summary>
    public const string DtdMalformed = "DTD0001";
    /// <summary>
    /// Identifies a DTD validity error.
    /// </summary>
    public const string DtdValidity = "DTD0002";
    /// <summary>
    /// Identifies an undeclared DTD entity.
    /// </summary>
    public const string DtdUndeclaredEntity = "DTD0003";
    /// <summary>
    /// Identifies a recursive DTD entity.
    /// </summary>
    public const string DtdRecursiveEntity = "DTD0004";
    /// <summary>
    /// Identifies denied external resource access.
    /// </summary>
    public const string ResourceDenied = "RES0001";
    /// <summary>
    /// Identifies an unavailable external resource.
    /// </summary>
    public const string ResourceNotFound = "RES0002";
    /// <summary>
    /// Identifies a resource resolution cycle.
    /// </summary>
    public const string ResourceCycle = "RES0003";
    /// <summary>
    /// Identifies an exceeded processing limit.
    /// </summary>
    public const string LimitExceeded = "LIM0001";
    /// <summary>
    /// Identifies an XInclude processing failure.
    /// </summary>
    public const string IncludeFailure = "XI0001";
    /// <summary>
    /// Identifies an XInclude cycle.
    /// </summary>
    public const string IncludeCycle = "XI0002";
    /// <summary>
    /// Identifies a missing required XInclude fallback.
    /// </summary>
    public const string IncludeFallbackMissing = "XI0003";
    /// <summary>
    /// Identifies a malformed XML Catalog.
    /// </summary>
    public const string CatalogMalformed = "CAT0001";
    /// <summary>
    /// Identifies an XML Catalog cycle.
    /// </summary>
    public const string CatalogCycle = "CAT0002";
    /// <summary>
    /// Identifies an XML canonicalization failure.
    /// </summary>
    public const string CanonicalizationFailure = "C14N0001";
    /// <summary>
    /// Identifies an XML Schema processing failure.
    /// </summary>
    public const string SchemaFailure = "XSD0001";
    /// <summary>
    /// Identifies the standard Serialization 3.1 error <c>SENR0001</c>.
    /// </summary>
    public const string SENR0001 = "SENR0001";
    /// <summary>
    /// Identifies the standard Serialization 3.1 error <c>SEPM0004</c>.
    /// </summary>
    public const string SEPM0004 = "SEPM0004";
    /// <summary>
    /// Identifies the standard Serialization 3.1 error <c>SEPM0009</c>.
    /// </summary>
    public const string SEPM0009 = "SEPM0009";
    /// <summary>
    /// Identifies the standard Serialization 3.1 error <c>SEPM0010</c>.
    /// </summary>
    public const string SEPM0010 = "SEPM0010";
    /// <summary>
    /// Identifies the standard Serialization 3.1 error <c>SEPM0016</c>.
    /// </summary>
    public const string SEPM0016 = "SEPM0016";
    /// <summary>
    /// Identifies the standard Serialization 3.1 error <c>SEPM0017</c>.
    /// </summary>
    public const string SEPM0017 = "SEPM0017";
    /// <summary>
    /// Identifies the standard Serialization 3.1 error <c>SEPM0018</c>.
    /// </summary>
    public const string SEPM0018 = "SEPM0018";
    /// <summary>
    /// Identifies the standard Serialization 3.1 error <c>SEPM0019</c>.
    /// </summary>
    public const string SEPM0019 = "SEPM0019";
    /// <summary>
    /// Identifies the standard Serialization 3.1 error <c>SERE0003</c>.
    /// </summary>
    public const string SERE0003 = "SERE0003";
    /// <summary>
    /// Identifies the standard Serialization 3.1 error <c>SERE0005</c>.
    /// </summary>
    public const string SERE0005 = "SERE0005";
    /// <summary>
    /// Identifies the standard Serialization 3.1 error <c>SERE0006</c>.
    /// </summary>
    public const string SERE0006 = "SERE0006";
    /// <summary>
    /// Identifies the standard Serialization 3.1 error <c>SERE0008</c>.
    /// </summary>
    public const string SERE0008 = "SERE0008";
    /// <summary>
    /// Identifies the standard Serialization 3.1 error <c>SERE0012</c>.
    /// </summary>
    public const string SERE0012 = "SERE0012";
    /// <summary>
    /// Identifies the standard Serialization 3.1 error <c>SERE0014</c>.
    /// </summary>
    public const string SERE0014 = "SERE0014";
    /// <summary>
    /// Identifies the standard Serialization 3.1 error <c>SERE0015</c>.
    /// </summary>
    public const string SERE0015 = "SERE0015";
    /// <summary>
    /// Identifies the standard Serialization 3.1 error <c>SERE0020</c>.
    /// </summary>
    public const string SERE0020 = "SERE0020";
    /// <summary>
    /// Identifies the standard Serialization 3.1 error <c>SERE0021</c>.
    /// </summary>
    public const string SERE0021 = "SERE0021";
    /// <summary>
    /// Identifies the standard Serialization 3.1 error <c>SERE0022</c>.
    /// </summary>
    public const string SERE0022 = "SERE0022";
    /// <summary>
    /// Identifies the standard Serialization 3.1 error <c>SERE0023</c>.
    /// </summary>
    public const string SERE0023 = "SERE0023";
    /// <summary>
    /// Identifies the standard Serialization 3.1 error <c>SESU0007</c>.
    /// </summary>
    public const string SESU0007 = "SESU0007";
    /// <summary>
    /// Identifies the standard Serialization 3.1 error <c>SESU0011</c>.
    /// </summary>
    public const string SESU0011 = "SESU0011";
    /// <summary>
    /// Identifies the standard Serialization 3.1 error <c>SESU0013</c>.
    /// </summary>
    public const string SESU0013 = "SESU0013";
}

