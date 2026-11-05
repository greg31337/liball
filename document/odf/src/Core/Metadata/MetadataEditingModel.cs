using System;
using System.Collections.Generic;

namespace odf;

internal static class MetadataEditingModel
{
    internal static OdfDocumentMetadata GetMetadata(OdfDocument document)
    {
        RequireDocument(document);
        if (document.Metadata == null)
        {
            document.Metadata = new OdfDocumentMetadata();
        }
        return document.Metadata;
    }

    internal static OdfDocumentMetadata SetSimpleMetadata(
        OdfDocument document,
        OdfDocumentMetadata source)
    {
        RequireDocument(document);
        ValidateSimpleMetadata(source);
        OdfDocumentMetadata target = GetMetadata(document);
        target.Title = source.Title;
        target.Description = source.Description;
        target.Subject = source.Subject;
        target.Creator = source.Creator;
        target.Date = source.Date;
        target.IsDateValid = IsDate(source.Date);
        target.Language = source.Language;
        target.Generator = source.Generator;
        target.InitialCreator = source.InitialCreator;
        target.CreationDate = source.CreationDate;
        target.IsCreationDateValid = IsDate(source.CreationDate);
        target.PrintedBy = source.PrintedBy;
        target.PrintDate = source.PrintDate;
        target.IsPrintDateValid = IsDate(source.PrintDate);
        target.EditingDuration = source.EditingDuration;
        target.IsEditingDurationValid = IsDuration(source.EditingDuration);
        target.EditingCycles = source.EditingCycles;
        target.IsEditingCyclesValid = source.EditingCycles >= 0;
        target.Keywords = CopyStrings(source.Keywords);
        CopyTemplate(target.Template, source.Template);
        CopyAutoReload(target.AutoReload, source.AutoReload);
        CopyHyperlinkBehavior(
            target.HyperlinkBehavior, source.HyperlinkBehavior);
        return target;
    }

    internal static OdfUserDefinedMetadata? FindUserMetadata(
        OdfDocument document,
        string name)
    {
        RequireDocument(document);
        RequireName(name);
        int index = 0;
        while (index < document.Metadata.UserDefined.Length)
        {
            OdfUserDefinedMetadata value =
                document.Metadata.UserDefined[index];
            if (value.Name == name)
            {
                return value;
            }
            index = index + 1;
        }
        return null;
    }

    internal static OdfUserDefinedMetadata SetUserMetadata(
        OdfDocument document,
        OdfUserDefinedMetadata source)
    {
        RequireDocument(document);
        ValidateUserMetadata(source);
        OdfUserDefinedMetadata? existing =
            FindUserMetadata(document, source.Name);
        if (existing != null)
        {
            CopyUserMetadata(existing, source);
            return existing;
        }
        OdfUserDefinedMetadata created = new OdfUserDefinedMetadata();
        CopyUserMetadata(created, source);
        OdfUserDefinedMetadata[] values = new OdfUserDefinedMetadata[
            document.Metadata.UserDefined.Length + 1];
        Array.Copy(document.Metadata.UserDefined, values,
            document.Metadata.UserDefined.Length);
        values[values.Length - 1] = created;
        document.Metadata.UserDefined = values;
        return created;
    }

    internal static bool RemoveUserMetadata(
        OdfDocument document,
        string name)
    {
        RequireDocument(document);
        RequireName(name);
        int match = -1;
        int index = 0;
        while (index < document.Metadata.UserDefined.Length && match < 0)
        {
            if (document.Metadata.UserDefined[index].Name == name)
            {
                match = index;
            }
            index = index + 1;
        }
        if (match < 0)
        {
            return false;
        }
        OdfUserDefinedMetadata[] values = new OdfUserDefinedMetadata[
            document.Metadata.UserDefined.Length - 1];
        int sourceIndex = 0;
        int destinationIndex = 0;
        while (sourceIndex < document.Metadata.UserDefined.Length)
        {
            if (sourceIndex != match)
            {
                values[destinationIndex] =
                    document.Metadata.UserDefined[sourceIndex];
                destinationIndex = destinationIndex + 1;
            }
            sourceIndex = sourceIndex + 1;
        }
        document.Metadata.UserDefined = values;
        return true;
    }

    internal static OdfDocumentStatistics SetStatistics(
        OdfDocument document,
        OdfDocumentStatistics source)
    {
        RequireDocument(document);
        ValidateStatistics(source);
        OdfDocumentStatistics target = document.Metadata.Statistics;
        target.IsPresent = true;
        target.PageCount = source.PageCount;
        target.TableCount = source.TableCount;
        target.DrawCount = source.DrawCount;
        target.ImageCount = source.ImageCount;
        target.ObjectCount = source.ObjectCount;
        target.OleObjectCount = source.OleObjectCount;
        target.ParagraphCount = source.ParagraphCount;
        target.WordCount = source.WordCount;
        target.CharacterCount = source.CharacterCount;
        target.NonWhitespaceCharacterCount =
            source.NonWhitespaceCharacterCount;
        target.RowCount = source.RowCount;
        target.CellCount = source.CellCount;
        target.FrameCount = source.FrameCount;
        target.SentenceCount = source.SentenceCount;
        target.SyllableCount = source.SyllableCount;
        return target;
    }

    internal static void ClearStatistics(OdfDocument document)
    {
        RequireDocument(document);
        OdfDocumentStatistics target = document.Metadata.Statistics;
        target.IsPresent = false;
        target.PageCount = -1;
        target.TableCount = -1;
        target.DrawCount = -1;
        target.ImageCount = -1;
        target.ObjectCount = -1;
        target.OleObjectCount = -1;
        target.ParagraphCount = -1;
        target.WordCount = -1;
        target.CharacterCount = -1;
        target.NonWhitespaceCharacterCount = -1;
        target.RowCount = -1;
        target.CellCount = -1;
        target.FrameCount = -1;
        target.SentenceCount = -1;
        target.SyllableCount = -1;
    }

    internal static OdfRdfTriple AddRdfTriple(
        OdfDocument document,
        OdfRdfTriple source)
    {
        RequireDocument(document);
        OdfRdfTriple created = CloneTriple(source);
        ValidateRdfTriple(document, created);
        AppendRdfTriple(document, created);
        return created;
    }

    internal static OdfRdfTriple ModifyRdfTriple(
        OdfDocument document,
        OdfRdfTriple triple,
        OdfRdfTriple replacement)
    {
        RequireDocument(document);
        if (!ContainsRdfTriple(document, triple))
        {
            throw new OdfDocumentException(
                "The RDF triple does not belong to the document.");
        }
        if (replacement == null)
        {
            throw new OdfDocumentException(
                "A replacement RDF triple is required.");
        }
        OdfRdfTriple candidate = CloneTriple(replacement);
        candidate.SourceKind = triple.SourceKind;
        candidate.PartPath = triple.PartPath;
        if (candidate.SubjectLexical.Length == 0)
        {
            candidate.SubjectLexical = triple.SubjectLexical;
        }
        if (candidate.PredicateLexical.Length == 0)
        {
            candidate.PredicateLexical = triple.PredicateLexical;
        }
        if (candidate.ObjectLexical.Length == 0)
        {
            candidate.ObjectLexical = triple.ObjectLexical;
        }
        if (candidate.DatatypeLexical.Length == 0)
        {
            candidate.DatatypeLexical = triple.DatatypeLexical;
        }
        if (triple.SourceKind == OdfRdfSourceKind.Rdfa)
        {
            candidate.Node = replacement.Node == null
                ? triple.Node
                : replacement.Node;
        }
        ValidateRdfTriple(document, candidate);
        CopyTripleTerms(triple, candidate);
        return triple;
    }

    internal static bool RemoveRdfTriple(
        OdfDocument document,
        OdfRdfTriple triple)
    {
        RequireDocument(document);
        if (triple == null)
        {
            throw new OdfDocumentException("An RDF triple is required.");
        }
        int match = -1;
        int index = 0;
        while (index < document.RdfTriples.Length && match < 0)
        {
            if (document.RdfTriples[index] == triple)
            {
                match = index;
            }
            index = index + 1;
        }
        if (match < 0)
        {
            return false;
        }
        OdfRdfTriple[] triples = new OdfRdfTriple[
            document.RdfTriples.Length - 1];
        int sourceIndex = 0;
        int destinationIndex = 0;
        while (sourceIndex < document.RdfTriples.Length)
        {
            if (sourceIndex != match)
            {
                triples[destinationIndex] = document.RdfTriples[sourceIndex];
                destinationIndex = destinationIndex + 1;
            }
            sourceIndex = sourceIndex + 1;
        }
        document.RdfTriples = triples;
        return true;
    }

    internal static OdfRdfTriple AddPackageMetadata(
        OdfDocument document,
        OdfRdfTriple source)
    {
        RequireDocument(document);
        if (document.PackageKind != OdfPackageKind.Package)
        {
            throw new OdfDocumentException(
                "Package metadata requires a package document.");
        }
        OdfRdfTriple created = CloneTriple(source);
        created.SourceKind = OdfRdfSourceKind.PackageMetadata;
        created.PartPath = OdfPartNames.MetadataManifest;
        created.Node = null;
        ValidateRdfTriple(document, created);
        AppendRdfTriple(document, created);
        return created;
    }

    internal static OdfRdfTriple[] GetPackageMetadata(OdfDocument document)
    {
        RequireDocument(document);
        List<OdfRdfTriple> result = new List<OdfRdfTriple>();
        int index = 0;
        while (index < document.RdfTriples.Length)
        {
            OdfRdfTriple triple = document.RdfTriples[index];
            if (triple.SourceKind == OdfRdfSourceKind.PackageMetadata)
            {
                result.Add(triple);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static void ValidateSimpleMetadata(OdfDocumentMetadata value)
    {
        if (value == null
            || value.Title == null
            || value.Description == null
            || value.Subject == null
            || value.Creator == null
            || value.Date == null
            || value.Language == null
            || value.Generator == null
            || value.InitialCreator == null
            || value.CreationDate == null
            || value.PrintedBy == null
            || value.PrintDate == null
            || value.EditingDuration == null
            || value.Keywords == null
            || value.Template == null
            || value.AutoReload == null
            || value.HyperlinkBehavior == null)
        {
            throw new OdfDocumentException(
                "Simple metadata fields cannot be null.");
        }
        if (value.EditingCycles < -1)
        {
            throw new OdfDocumentException(
                "Editing cycles cannot be less than -1.");
        }
        ValidateOptionalDate(value.Date, "modification date");
        ValidateOptionalDate(value.CreationDate, "creation date");
        ValidateOptionalDate(value.PrintDate, "print date");
        ValidateOptionalDuration(
            value.EditingDuration, "editing duration");
        ValidateKeywords(value.Keywords);
        ValidateTemplate(value.Template);
        ValidateAutoReload(value.AutoReload);
        ValidateHyperlinkBehavior(value.HyperlinkBehavior);
    }

    static void ValidateUserMetadata(OdfUserDefinedMetadata value)
    {
        if (value == null
            || value.Name == null
            || value.ValueType == null
            || value.LexicalValue == null
            || value.StringValue == null
            || value.Currency == null
            || value.DateValue == null
            || value.DurationValue == null)
        {
            throw new OdfDocumentException(
                "User-defined metadata fields cannot be null.");
        }
        RequireName(value.Name);
        if (value.Kind == OdfValueKind.String)
        {
            return;
        }
        if (value.Kind == OdfValueKind.Float
            || value.Kind == OdfValueKind.Decimal
            || value.Kind == OdfValueKind.Percentage
            || value.Kind == OdfValueKind.Currency)
        {
            if (double.IsNaN(value.NumericValue)
                || double.IsInfinity(value.NumericValue))
            {
                throw new OdfDocumentException(
                    "User-defined numeric metadata must be finite.");
            }
            if (value.Kind == OdfValueKind.Currency
                && value.Currency.Length == 0)
            {
                throw new OdfDocumentException(
                    "Currency metadata requires a currency code.");
            }
            return;
        }
        if (value.Kind == OdfValueKind.Date
            || value.Kind == OdfValueKind.DateTime)
        {
            if (!IsDate(value.DateValue))
            {
                throw new OdfDocumentException(
                    "User-defined date metadata is invalid.");
            }
            return;
        }
        if (value.Kind == OdfValueKind.Time
            || value.Kind == OdfValueKind.Duration)
        {
            if (!IsDuration(value.DurationValue))
            {
                throw new OdfDocumentException(
                    "User-defined time metadata is invalid.");
            }
            return;
        }
        if (value.Kind != OdfValueKind.Boolean)
        {
            throw new OdfDocumentException(
                "The user-defined metadata type is not supported.");
        }
    }

    static void ValidateStatistics(OdfDocumentStatistics value)
    {
        if (value == null
            || value.PageCount < -1
            || value.TableCount < -1
            || value.DrawCount < -1
            || value.ImageCount < -1
            || value.ObjectCount < -1
            || value.OleObjectCount < -1
            || value.ParagraphCount < -1
            || value.WordCount < -1
            || value.CharacterCount < -1
            || value.NonWhitespaceCharacterCount < -1
            || value.RowCount < -1
            || value.CellCount < -1
            || value.FrameCount < -1
            || value.SentenceCount < -1
            || value.SyllableCount < -1)
        {
            throw new OdfDocumentException(
                "Document statistics cannot be less than -1.");
        }
    }

    static void ValidateRdfTriple(
        OdfDocument document,
        OdfRdfTriple triple)
    {
        if (triple == null
            || triple.Subject == null
            || triple.SubjectLexical == null
            || triple.Predicate == null
            || triple.PredicateLexical == null
            || triple.Object == null
            || triple.ObjectLexical == null
            || triple.Datatype == null
            || triple.DatatypeLexical == null
            || triple.Language == null
            || triple.PartPath == null)
        {
            throw new OdfDocumentException("RDF triple fields cannot be null.");
        }
        if (triple.Predicate.Length == 0
            && triple.PredicateLexical.Length == 0)
        {
            throw new OdfDocumentException(
                "An RDF triple requires a predicate.");
        }
        if (!triple.IsLiteral && triple.Object.Length == 0
            && triple.ObjectLexical.Length == 0)
        {
            throw new OdfDocumentException(
                "An RDF resource triple requires an object.");
        }
        if (!triple.IsLiteral
            && (triple.Datatype.Length > 0
                || triple.DatatypeLexical.Length > 0
                || triple.Language.Length > 0))
        {
            throw new OdfDocumentException(
                "RDF resource objects cannot have a datatype or language.");
        }
        if (triple.SourceKind == OdfRdfSourceKind.Rdfa)
        {
            if (triple.Node == null
                || triple.Node.Kind != OdfNodeKind.Element)
            {
                throw new OdfDocumentException(
                    "An RDFa triple requires a content element.");
            }
            triple.PartPath = triple.Node.Location.PartPath;
            return;
        }
        if (triple.SourceKind != OdfRdfSourceKind.RdfXml
            && triple.SourceKind != OdfRdfSourceKind.PackageMetadata)
        {
            throw new OdfDocumentException(
                "The RDF triple source kind is not editable.");
        }
        if (document.PackageKind != OdfPackageKind.Package)
        {
            throw new OdfDocumentException(
                "RDF/XML triples require a package document.");
        }
        if (triple.Predicate.Length == 0
            || !OdfIriResolver.IsAbsoluteIri(triple.Predicate))
        {
            throw new OdfDocumentException(
                "An RDF/XML predicate must be an absolute IRI.");
        }
    }

    static void CopyTemplate(
        OdfMetadataTemplate target,
        OdfMetadataTemplate source)
    {
        if (source.Href == null
            || source.XLinkType == null
            || source.Actuate == null
            || source.Title == null
            || source.Date == null)
        {
            throw new OdfDocumentException(
                "Template metadata fields cannot be null.");
        }
        target.IsPresent = source.IsPresent;
        target.Href = source.Href;
        target.XLinkType = source.XLinkType;
        target.Actuate = source.Actuate;
        target.Title = source.Title;
        target.Date = source.Date;
        target.IsDateValid = IsDate(source.Date);
    }

    static void CopyAutoReload(
        OdfMetadataAutoReload target,
        OdfMetadataAutoReload source)
    {
        if (source.Href == null
            || source.XLinkType == null
            || source.Show == null
            || source.Actuate == null
            || source.Delay == null)
        {
            throw new OdfDocumentException(
                "Auto-reload metadata fields cannot be null.");
        }
        target.IsPresent = source.IsPresent;
        target.Href = source.Href;
        target.XLinkType = source.XLinkType;
        target.Show = source.Show;
        target.Actuate = source.Actuate;
        target.Delay = source.Delay;
        target.IsDelayValid = IsDuration(source.Delay);
    }

    static void CopyHyperlinkBehavior(
        OdfMetadataHyperlinkBehavior target,
        OdfMetadataHyperlinkBehavior source)
    {
        if (source.TargetFrameName == null || source.Show == null)
        {
            throw new OdfDocumentException(
                "Hyperlink metadata fields cannot be null.");
        }
        target.IsPresent = source.IsPresent;
        target.TargetFrameName = source.TargetFrameName;
        target.Show = source.Show;
    }

    static void ValidateTemplate(OdfMetadataTemplate value)
    {
        if (value.Href == null
            || value.XLinkType == null
            || value.Actuate == null
            || value.Title == null
            || value.Date == null)
        {
            throw new OdfDocumentException(
                "Template metadata fields cannot be null.");
        }
        if (value.IsPresent)
        {
            ValidateOptionalDate(value.Date, "template date");
        }
    }

    static void ValidateAutoReload(OdfMetadataAutoReload value)
    {
        if (value.Href == null
            || value.XLinkType == null
            || value.Show == null
            || value.Actuate == null
            || value.Delay == null)
        {
            throw new OdfDocumentException(
                "Auto-reload metadata fields cannot be null.");
        }
        if (value.IsPresent)
        {
            ValidateOptionalDuration(value.Delay, "auto-reload delay");
        }
    }

    static void ValidateHyperlinkBehavior(
        OdfMetadataHyperlinkBehavior value)
    {
        if (value.TargetFrameName == null || value.Show == null)
        {
            throw new OdfDocumentException(
                "Hyperlink metadata fields cannot be null.");
        }
    }

    static void ValidateOptionalDate(string value, string fieldName)
    {
        if (value.Length > 0 && !IsDate(value))
        {
            throw new OdfDocumentException(
                "The metadata " + fieldName + " is invalid.");
        }
    }

    static void ValidateOptionalDuration(string value, string fieldName)
    {
        if (value.Length > 0 && !IsDuration(value))
        {
            throw new OdfDocumentException(
                "The metadata " + fieldName + " is invalid.");
        }
    }

    static void ValidateKeywords(string[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new OdfDocumentException(
                    "A metadata keyword cannot be null.");
            }
            index = index + 1;
        }
    }

    static void CopyUserMetadata(
        OdfUserDefinedMetadata target,
        OdfUserDefinedMetadata source)
    {
        target.Name = source.Name;
        target.ValueType = source.ValueType;
        target.Kind = source.Kind;
        target.LexicalValue = source.LexicalValue;
        target.StringValue = source.StringValue;
        target.NumericValue = source.NumericValue;
        target.Currency = source.Currency;
        target.DateValue = source.DateValue;
        target.DurationValue = source.DurationValue;
        target.BooleanValue = source.BooleanValue;
        target.IsValid = true;
    }

    static OdfRdfTriple CloneTriple(OdfRdfTriple source)
    {
        if (source == null)
        {
            throw new OdfDocumentException("An RDF triple is required.");
        }
        OdfRdfTriple result = new OdfRdfTriple();
        result.SourceKind = source.SourceKind;
        result.Subject = source.Subject;
        result.SubjectLexical = source.SubjectLexical;
        result.Predicate = source.Predicate;
        result.PredicateLexical = source.PredicateLexical;
        result.Object = source.Object;
        result.ObjectLexical = source.ObjectLexical;
        result.Datatype = source.Datatype;
        result.DatatypeLexical = source.DatatypeLexical;
        result.Language = source.Language;
        result.IsLiteral = source.IsLiteral;
        result.IsValid = source.IsValid;
        result.PartPath = source.PartPath;
        if (source.SourceKind == OdfRdfSourceKind.Rdfa)
        {
            result.Node = source.Node;
        }
        return result;
    }

    static void CopyTripleTerms(
        OdfRdfTriple target,
        OdfRdfTriple source)
    {
        target.Subject = source.Subject;
        target.SubjectLexical = source.SubjectLexical;
        target.Predicate = source.Predicate;
        target.PredicateLexical = source.PredicateLexical;
        target.Object = source.Object;
        target.ObjectLexical = source.ObjectLexical;
        target.Datatype = source.Datatype;
        target.DatatypeLexical = source.DatatypeLexical;
        target.Language = source.Language;
        target.IsLiteral = source.IsLiteral;
        target.IsValid = source.IsValid;
        if (target.SourceKind == OdfRdfSourceKind.Rdfa)
        {
            target.Node = source.Node;
            target.PartPath = source.PartPath;
        }
    }

    static void AppendRdfTriple(
        OdfDocument document,
        OdfRdfTriple triple)
    {
        OdfRdfTriple[] triples = new OdfRdfTriple[
            document.RdfTriples.Length + 1];
        Array.Copy(document.RdfTriples, triples,
            document.RdfTriples.Length);
        triples[triples.Length - 1] = triple;
        document.RdfTriples = triples;
    }

    static bool ContainsRdfTriple(
        OdfDocument document,
        OdfRdfTriple triple)
    {
        if (triple == null)
        {
            return false;
        }
        int index = 0;
        while (index < document.RdfTriples.Length)
        {
            if (document.RdfTriples[index] == triple)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static string[] CopyStrings(string[] values)
    {
        string[] result = new string[values.Length];
        Array.Copy(values, result, values.Length);
        return result;
    }

    static bool IsDate(string value)
    {
        return value.Length > 0
            && (OdfLexicalParser.ParseDate(value).IsValid
                || OdfLexicalParser.ParseDateTime(value).IsValid);
    }

    static bool IsDuration(string value)
    {
        return value.Length > 0
            && OdfLexicalParser.ParseDuration(value).IsValid;
    }

    static void RequireName(string name)
    {
        if (name == null || name.Length == 0)
        {
            throw new OdfDocumentException(
                "A non-empty user metadata name is required.");
        }
    }

    static void RequireDocument(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for metadata editing.");
        }
        if (document.Metadata == null)
        {
            document.Metadata = new OdfDocumentMetadata();
        }
        if (document.Metadata.UserDefined == null
            || document.Metadata.Statistics == null
            || document.RdfTriples == null)
        {
            throw new OdfDocumentException(
                "The document metadata collections are required.");
        }
    }
}
