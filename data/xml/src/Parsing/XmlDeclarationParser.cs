using System;

namespace xml;

internal enum XmlDeclarationOpeningKind
{
    None = 0,
    Declaration = 1,
    ReservedTarget = 2
}

internal class XmlDeclarationInfo
{
    internal bool HasDeclaration;
    internal bool IsTextDeclaration;
    internal bool HasVersion;
    internal XmlVersion Version;
    internal string? VersionNumber;
    internal string? EncodingName;
    internal XmlStandalone Standalone;
    internal int StartUtf16Offset;
    internal int CharacterCount;

    internal XmlDeclarationInfo(bool hasDeclaration, bool isTextDeclaration, bool hasVersion, XmlVersion version, string? versionNumber, string? encodingName, XmlStandalone standalone, int startUtf16Offset, int characterCount)
    {
        HasDeclaration = hasDeclaration;
        IsTextDeclaration = isTextDeclaration;
        HasVersion = hasVersion;
        Version = version;
        VersionNumber = versionNumber;
        EncodingName = encodingName;
        Standalone = standalone;
        StartUtf16Offset = startUtf16Offset;
        CharacterCount = characterCount;
    }
}

internal class XmlDeclarationParser
{
    internal ReadOnlyMemory<char> Characters;
    internal XmlEncodingEntityKind EntityKind;
    internal int StartOffset;
    internal int Position;
    internal bool HasVersion;
    internal XmlVersion Version;
    internal string? VersionNumber;
    internal string? EncodingName;
    internal XmlStandalone Standalone;
    internal bool MapWellFormednessErrors;
    internal string? ErrorSourceUri;
    internal IXmlErrorReporter? ErrorReporter;

    internal XmlDeclarationParser(ReadOnlyMemory<char> characters, XmlEncodingEntityKind entityKind, int startOffset)
    {
        if (entityKind != XmlEncodingEntityKind.Document && entityKind != XmlEncodingEntityKind.ExternalParsedEntity)
        {
            throw new ArgumentOutOfRangeException("entityKind");
        }

        Guard.NonNegativeInt(startOffset, "startOffset");
        if (startOffset > characters.Length)
        {
            throw new ArgumentOutOfRangeException("startOffset");
        }

        Characters = characters;
        EntityKind = entityKind;
        StartOffset = startOffset;
        Position = startOffset;
        HasVersion = false;
        Version = XmlVersion.Xml10;
        VersionNumber = null;
        EncodingName = null;
        Standalone = XmlStandalone.Unspecified;
        MapWellFormednessErrors = false;
        ErrorSourceUri = null;
        ErrorReporter = null;
    }

    internal void ConfigureXmlDeclarationErrors(string? sourceUri, IXmlErrorReporter? reporter)
    {
        MapWellFormednessErrors = true;
        ErrorSourceUri = sourceUri;
        ErrorReporter = reporter;
    }

    internal XmlDeclarationInfo ParseXmlOrTextDeclaration()
    {
        ResetXmlDeclarationParser();
        XmlDeclarationOpeningKind opening = ClassifyXmlDeclarationOpening();
        if (opening == XmlDeclarationOpeningKind.None)
        {
            return new XmlDeclarationInfo(false, EntityKind == XmlEncodingEntityKind.ExternalParsedEntity, false, XmlVersion.Xml10, null, null, XmlStandalone.Unspecified, StartOffset, 0);
        }

        if (opening == XmlDeclarationOpeningKind.ReservedTarget)
        {
            throw CreateXmlDeclarationError("A processing-instruction target matching 'xml' is reserved", StartOffset + 2);
        }

        if (StartOffset != 0)
        {
            throw CreateXmlDeclarationError("An XML or text declaration may appear only at the beginning of its entity", StartOffset);
        }

        Position = StartOffset + 5;
        if (!ConsumeDeclarationSpaces())
        {
            throw CreateXmlDeclarationError("Whitespace is required after '<?xml'", Position);
        }

        while (!IsDeclarationClosing())
        {
            string name = ReadPseudoAttributeName();
            string value = ReadPseudoAttributeValue(name);
            ApplyPseudoAttribute(name, value);

            bool separated = ConsumeDeclarationSpaces();
            if (IsDeclarationClosing())
            {
                break;
            }

            if (!separated)
            {
                throw CreateXmlDeclarationError("Whitespace is required between declaration pseudo-attributes", Position);
            }

            if (Position >= Characters.Length)
            {
                throw CreateXmlDeclarationError("The XML or text declaration is not terminated", StartOffset);
            }
        }

        ValidateRequiredPseudoAttributes();
        ConsumeDeclarationClosing();
        return new XmlDeclarationInfo(true, EntityKind == XmlEncodingEntityKind.ExternalParsedEntity, HasVersion, Version, VersionNumber, EncodingName, Standalone, StartOffset, Position - StartOffset);
    }

    internal void ResetXmlDeclarationParser()
    {
        Position = StartOffset;
        HasVersion = false;
        Version = XmlVersion.Xml10;
        VersionNumber = null;
        EncodingName = null;
        Standalone = XmlStandalone.Unspecified;
    }

    internal XmlDeclarationOpeningKind ClassifyXmlDeclarationOpening()
    {
        if (Characters.Length - StartOffset < 3 || Characters.Span[StartOffset] != '<' || Characters.Span[StartOffset + 1] != '?')
        {
            return XmlDeclarationOpeningKind.None;
        }

        string? target = ReadDeclarationTargetName();
        if (target == null)
        {
            return XmlDeclarationOpeningKind.None;
        }

        if (string.Equals(target, "xml", StringComparison.Ordinal))
        {
            return XmlDeclarationOpeningKind.Declaration;
        }

        if (string.Equals(target, "xml", StringComparison.OrdinalIgnoreCase))
        {
            return XmlDeclarationOpeningKind.ReservedTarget;
        }

        return XmlDeclarationOpeningKind.None;
    }

    internal string? ReadDeclarationTargetName()
    {
        int targetStart = StartOffset + 2;
        if (targetStart >= Characters.Length)
        {
            return null;
        }

        UnicodeScalarScanner scalarScanner = new UnicodeScalarScanner(Characters);
        scalarScanner.Offset = targetStart;
        UnicodeScalar first = scalarScanner.ReadUnicodeScalar();
        if (!Xml10CharacterRules.IsXml10NameStartCharacter(first.Value))
        {
            return null;
        }

        int targetEnd = scalarScanner.CurrentUtf16Offset();
        while (!scalarScanner.IsUnicodeScalarEnd())
        {
            UnicodeScalar scalar = scalarScanner.ReadUnicodeScalar();
            if (!Xml10CharacterRules.IsXml10NameCharacter(scalar.Value))
            {
                break;
            }

            targetEnd = scalarScanner.CurrentUtf16Offset();
        }

        return Characters.Span.Slice(targetStart, targetEnd - targetStart).ToString();
    }

    internal bool ConsumeDeclarationSpaces()
    {
        int start = Position;
        while (Position < Characters.Length && XmlLexicalCharacterRules.IsXmlSpaceCharacter(Characters.Span[Position]))
        {
            Position = Position + 1;
        }

        return Position > start;
    }

    internal string ReadPseudoAttributeName()
    {
        if (Position >= Characters.Length)
        {
            throw CreateXmlDeclarationError("A declaration pseudo-attribute was expected", Position);
        }

        UnicodeScalarScanner scalarScanner = new UnicodeScalarScanner(Characters);
        scalarScanner.Offset = Position;
        UnicodeScalar first = scalarScanner.ReadUnicodeScalar();
        if (!Xml10CharacterRules.IsXml10NameStartCharacter(first.Value))
        {
            throw CreateXmlDeclarationError("A declaration pseudo-attribute name was expected", Position);
        }

        int nameStart = Position;
        Position = scalarScanner.CurrentUtf16Offset();
        while (!scalarScanner.IsUnicodeScalarEnd())
        {
            UnicodeScalar scalar = scalarScanner.ReadUnicodeScalar();
            if (!Xml10CharacterRules.IsXml10NameCharacter(scalar.Value))
            {
                break;
            }

            Position = scalarScanner.CurrentUtf16Offset();
        }

        return Characters.Span.Slice(nameStart, Position - nameStart).ToString();
    }

    internal string ReadPseudoAttributeValue(string name)
    {
        ConsumeDeclarationSpaces();
        if (Position >= Characters.Length || Characters.Span[Position] != '=')
        {
            throw CreateXmlDeclarationError("Pseudo-attribute '" + name + "' requires '='", Position);
        }

        Position = Position + 1;
        ConsumeDeclarationSpaces();
        if (Position >= Characters.Length || Characters.Span[Position] != '\'' && Characters.Span[Position] != '"')
        {
            throw CreateXmlDeclarationError("Pseudo-attribute '" + name + "' requires a quoted value", Position);
        }

        char quote = Characters.Span[Position];
        Position = Position + 1;
        int valueStart = Position;
        while (Position < Characters.Length && Characters.Span[Position] != quote)
        {
            Position = Position + 1;
        }

        if (Position >= Characters.Length)
        {
            throw CreateXmlDeclarationError("Pseudo-attribute '" + name + "' has an unterminated value", valueStart);
        }

        string value = Characters.Span.Slice(valueStart, Position - valueStart).ToString();
        Position = Position + 1;
        return value;
    }

    internal void ApplyPseudoAttribute(string name, string value)
    {
        if (EntityKind == XmlEncodingEntityKind.Document)
        {
            ApplyDocumentPseudoAttribute(name, value);
            return;
        }

        ApplyTextPseudoAttribute(name, value);
    }

    internal void ApplyDocumentPseudoAttribute(string name, string value)
    {
        if (string.Equals(name, "version", StringComparison.Ordinal))
        {
            if (HasVersion || EncodingName != null || Standalone != XmlStandalone.Unspecified)
            {
                throw CreateXmlDeclarationError("The version pseudo-attribute must appear first and only once", Position);
            }

            Version = ParseDeclarationVersion(value);
            VersionNumber = value;
            HasVersion = true;
            return;
        }

        if (string.Equals(name, "encoding", StringComparison.Ordinal))
        {
            if (!HasVersion || EncodingName != null || Standalone != XmlStandalone.Unspecified)
            {
                throw CreateXmlDeclarationError("The encoding pseudo-attribute is duplicated or out of order", Position);
            }

            ValidateDeclarationEncodingName(value);
            EncodingName = value;
            return;
        }

        if (string.Equals(name, "standalone", StringComparison.Ordinal))
        {
            if (!HasVersion || Standalone != XmlStandalone.Unspecified)
            {
                throw CreateXmlDeclarationError("The standalone pseudo-attribute is duplicated or out of order", Position);
            }

            Standalone = ParseDeclarationStandalone(value);
            return;
        }

        throw CreateXmlDeclarationError("Unknown XML declaration pseudo-attribute '" + name + "'", Position);
    }

    internal void ApplyTextPseudoAttribute(string name, string value)
    {
        if (string.Equals(name, "version", StringComparison.Ordinal))
        {
            if (HasVersion || EncodingName != null)
            {
                throw CreateXmlDeclarationError("The text declaration version pseudo-attribute must appear first and only once", Position);
            }

            Version = ParseDeclarationVersion(value);
            VersionNumber = value;
            HasVersion = true;
            return;
        }

        if (string.Equals(name, "encoding", StringComparison.Ordinal))
        {
            if (EncodingName != null)
            {
                throw CreateXmlDeclarationError("The text declaration encoding pseudo-attribute is duplicated", Position);
            }

            ValidateDeclarationEncodingName(value);
            EncodingName = value;
            return;
        }

        if (string.Equals(name, "standalone", StringComparison.Ordinal))
        {
            throw CreateXmlDeclarationError("A text declaration cannot contain a standalone pseudo-attribute", Position);
        }

        throw CreateXmlDeclarationError("Unknown text declaration pseudo-attribute '" + name + "'", Position);
    }

    internal XmlVersion ParseDeclarationVersion(string value)
    {
        if (value.Length < 3 || value[0] != '1' || value[1] != '.')
        {
            throw CreateXmlDeclarationError("An XML version must match '1.' followed by one or more digits", Position);
        }

        int index = 2;
        while (index < value.Length)
        {
            if (!IsAsciiDigit(value[index]))
            {
                throw CreateXmlDeclarationError("An XML version must match '1.' followed by one or more digits", Position);
            }

            index = index + 1;
        }

        if (string.Equals(value, "1.1", StringComparison.Ordinal))
        {
            return XmlVersion.Xml11;
        }

        return XmlVersion.Xml10;
    }

    internal void ValidateDeclarationEncodingName(string value)
    {
        if (value.Length == 0 || !IsAsciiLetter(value[0]))
        {
            throw CreateXmlDeclarationError("An encoding name must begin with an ASCII letter", Position);
        }

        int index = 1;
        while (index < value.Length)
        {
            char character = value[index];
            if (!IsAsciiLetter(character) && !IsAsciiDigit(character) && character != '.' && character != '_' && character != '-')
            {
                throw CreateXmlDeclarationError("An encoding name contains a character outside EncName", Position);
            }

            index = index + 1;
        }
    }

    internal XmlStandalone ParseDeclarationStandalone(string value)
    {
        if (string.Equals(value, "yes", StringComparison.Ordinal))
        {
            return XmlStandalone.Yes;
        }

        if (string.Equals(value, "no", StringComparison.Ordinal))
        {
            return XmlStandalone.No;
        }

        throw CreateXmlDeclarationError("The standalone value must be 'yes' or 'no'", Position);
    }

    internal void ValidateRequiredPseudoAttributes()
    {
        if (EntityKind == XmlEncodingEntityKind.Document && !HasVersion)
        {
            throw CreateXmlDeclarationError("An XML declaration requires the version pseudo-attribute", StartOffset);
        }

        if (EntityKind == XmlEncodingEntityKind.ExternalParsedEntity && EncodingName == null)
        {
            throw CreateXmlDeclarationError("A text declaration requires the encoding pseudo-attribute", StartOffset);
        }
    }

    internal bool IsDeclarationClosing()
    {
        return Position + 1 < Characters.Length && Characters.Span[Position] == '?' && Characters.Span[Position + 1] == '>';
    }

    internal void ConsumeDeclarationClosing()
    {
        if (!IsDeclarationClosing())
        {
            throw CreateXmlDeclarationError("The XML or text declaration is not terminated with '?>'", Position);
        }

        Position = Position + 2;
    }

    internal bool IsAsciiLetter(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z';
    }

    internal bool IsAsciiDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    internal Exception CreateXmlDeclarationError(string message, int offset)
    {
        if (MapWellFormednessErrors)
        {
            return XmlParserDiagnosticFactory.CreateXmlWellFormednessError(message, offset, ErrorSourceUri, null, ErrorReporter);
        }

        return new FormatException(message + " at UTF-16 offset " + offset + ".");
    }
}
