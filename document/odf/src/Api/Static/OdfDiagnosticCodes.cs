namespace odf;

/// <summary>
/// Provides stable diagnostic codes emitted by SDK operations.
/// </summary>
public static class OdfDiagnosticCodes
{
    /// <summary>
    /// The package container is invalid.
    /// </summary>
    public const string InvalidPackage = "ODF-PKG-001";
    /// <summary>
    /// The package entry-count limit was exceeded.
    /// </summary>
    public const string EntryLimitExceeded = "ODF-PKG-002";
    /// <summary>
    /// An expanded-size limit was exceeded.
    /// </summary>
    public const string ExpandedSizeLimitExceeded = "ODF-PKG-003";
    /// <summary>
    /// The compression-ratio limit was exceeded.
    /// </summary>
    public const string CompressionRatioExceeded = "ODF-PKG-004";
    /// <summary>
    /// The package contains a duplicate entry name.
    /// </summary>
    public const string DuplicateEntry = "ODF-PKG-005";
    /// <summary>
    /// A package entry path is invalid.
    /// </summary>
    public const string InvalidEntryPath = "ODF-PKG-006";
    /// <summary>
    /// A package entry attempts path traversal.
    /// </summary>
    public const string PathTraversal = "ODF-PKG-007";
    /// <summary>
    /// Normalized package entry paths collide.
    /// </summary>
    public const string PathNormalizationCollision = "ODF-PKG-008";
    /// <summary>
    /// A package entry uses unsupported compression.
    /// </summary>
    public const string UnsupportedCompression = "ODF-PKG-009";
    /// <summary>
    /// A package entry CRC does not match.
    /// </summary>
    public const string CrcMismatch = "ODF-PKG-010";
    /// <summary>
    /// The package has no mimetype entry.
    /// </summary>
    public const string MissingMimetype = "ODF-PKG-011";
    /// <summary>
    /// The mimetype entry is not first.
    /// </summary>
    public const string MimetypeNotFirst = "ODF-PKG-012";
    /// <summary>
    /// The mimetype entry is compressed.
    /// </summary>
    public const string MimetypeCompressed = "ODF-PKG-013";
    /// <summary>
    /// The mimetype entry has a ZIP extra field.
    /// </summary>
    public const string MimetypeExtraField = "ODF-PKG-014";
    /// <summary>
    /// The mimetype entry value is invalid.
    /// </summary>
    public const string InvalidMimetype = "ODF-PKG-015";
    /// <summary>
    /// The package manifest is missing.
    /// </summary>
    public const string MissingManifest = "ODF-PKG-016";
    /// <summary>
    /// A package entry is absent from the manifest.
    /// </summary>
    public const string ManifestEntryMissing = "ODF-PKG-017";
    /// <summary>
    /// A manifest media type is inconsistent.
    /// </summary>
    public const string ManifestMediaTypeMismatch = "ODF-PKG-018";
    /// <summary>
    /// A required package part is missing.
    /// </summary>
    public const string MissingRequiredPackagePart = "ODF-PKG-019";
    /// <summary>
    /// A package manifest entry is invalid or duplicated.
    /// </summary>
    public const string InvalidManifestEntry = "ODF-PKG-020";
    /// <summary>
    /// A package or manifest media type is invalid.
    /// </summary>
    public const string InvalidMediaType = "ODF-PKG-021";
    /// <summary>
    /// An embedded OpenDocument package is invalid.
    /// </summary>
    public const string InvalidEmbeddedPackage = "ODF-PKG-022";
    /// <summary>
    /// Package encryption metadata or storage is structurally invalid.
    /// </summary>
    public const string InvalidEncryptionStructure = "ODF-PKG-023";
    /// <summary>
    /// An XML part is not well formed.
    /// </summary>
    public const string MalformedXml = "ODF-XML-001";
    /// <summary>
    /// An external XML entity was rejected.
    /// </summary>
    public const string ExternalEntityRejected = "ODF-XML-002";
    /// <summary>
    /// The XML depth limit was exceeded.
    /// </summary>
    public const string XmlDepthLimitExceeded = "ODF-XML-003";
    /// <summary>
    /// The XML attribute limit was exceeded.
    /// </summary>
    public const string XmlAttributeLimitExceeded = "ODF-XML-004";
    /// <summary>
    /// The XML text-size limit was exceeded.
    /// </summary>
    public const string XmlTextLimitExceeded = "ODF-XML-005";
    /// <summary>
    /// An XML namespace declaration or use is invalid.
    /// </summary>
    public const string InvalidNamespace = "ODF-XML-006";
    /// <summary>
    /// An XML name is invalid.
    /// </summary>
    public const string InvalidXmlName = "ODF-XML-007";
    /// <summary>
    /// An XML ID is duplicated.
    /// </summary>
    public const string DuplicateXmlId = "ODF-XML-008";
    /// <summary>
    /// An XML reference cannot be resolved.
    /// </summary>
    public const string BrokenReference = "ODF-XML-009";
    /// <summary>
    /// An element is not allowed in its context.
    /// </summary>
    public const string UnexpectedElement = "ODF-SCHEMA-001";
    /// <summary>
    /// An attribute is not allowed in its context.
    /// </summary>
    public const string UnexpectedAttribute = "ODF-SCHEMA-002";
    /// <summary>
    /// Required schema content is missing.
    /// </summary>
    public const string MissingRequiredContent = "ODF-SCHEMA-003";
    /// <summary>
    /// A lexical value does not match its datatype.
    /// </summary>
    public const string InvalidDatatype = "ODF-SCHEMA-004";
    /// <summary>
    /// A schema occurrence constraint is violated.
    /// </summary>
    public const string OccurrenceViolation = "ODF-SCHEMA-005";
    /// <summary>
    /// A style reference is invalid.
    /// </summary>
    public const string InvalidStyleReference = "ODF-SEM-001";
    /// <summary>
    /// A cell or range reference is invalid.
    /// </summary>
    public const string InvalidRangeReference = "ODF-SEM-002";
    /// <summary>
    /// A resource reference is invalid.
    /// </summary>
    public const string InvalidResourceReference = "ODF-SEM-003";
    /// <summary>
    /// A table merge is invalid.
    /// </summary>
    public const string InvalidMerge = "ODF-SEM-004";
    /// <summary>
    /// A value conflicts with its declared value type.
    /// </summary>
    public const string ValueTypeMismatch = "ODF-SEM-005";
    /// <summary>
    /// A formula is syntactically invalid.
    /// </summary>
    public const string InvalidFormula = "ODF-FORMULA-001";
    /// <summary>
    /// A formula function is unsupported.
    /// </summary>
    public const string UnsupportedFunction = "ODF-FORMULA-002";
    /// <summary>
    /// The formula nesting-depth limit was exceeded.
    /// </summary>
    public const string FormulaDepthLimitExceeded = "ODF-FORMULA-003";
    /// <summary>
    /// A circular formula dependency was detected.
    /// </summary>
    public const string CircularReference = "ODF-FORMULA-004";
    /// <summary>
    /// An encryption algorithm is unsupported.
    /// </summary>
    public const string UnsupportedEncryption = "ODF-SEC-001";
    /// <summary>
    /// An encrypted-entry checksum does not match.
    /// </summary>
    public const string ChecksumMismatch = "ODF-SEC-002";
    /// <summary>
    /// A digital signature is invalid.
    /// </summary>
    public const string SignatureInvalid = "ODF-SEC-003";
    /// <summary>
    /// A signature algorithm is unsupported.
    /// </summary>
    public const string SignatureAlgorithmUnsupported = "ODF-SEC-004";
    /// <summary>
    /// The declared ODF version is unsupported.
    /// </summary>
    public const string VersionUnsupported = "ODF-VERSION-001";
    /// <summary>
    /// A feature is unavailable in the selected ODF version.
    /// </summary>
    public const string FeatureNotAvailable = "ODF-VERSION-002";
    /// <summary>
    /// A version downgrade would lose information.
    /// </summary>
    public const string DowngradeLoss = "ODF-VERSION-003";
    /// <summary>
    /// An edit precondition failed.
    /// </summary>
    public const string EditConflict = "ODF-EDIT-001";
    /// <summary>
    /// A staged edit failed validation.
    /// </summary>
    public const string EditValidationFailed = "ODF-EDIT-002";
    /// <summary>
    /// An operation was cancelled.
    /// </summary>
    public const string OperationCancelled = "ODF-OP-001";
}
