namespace svg;

/// <summary>Provides the reflection-free library capability catalog.</summary>
public class SvgCapabilities : ISvgCapabilities
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgCapabilities()
    {
    }

    /// <summary>Analyzes features dynamically used by one document with bounded defaults.</summary>
    /// <param name="document">The document to inspect without mutation.</param>
    /// <returns>The policy-aware dynamic feature analysis.</returns>
    public SvgFeatureAnalysisResult AnalyzeDocumentWithDocument(SvgDocument document)
    {
        return new SvgDocumentFeatureAnalyzer().AnalyzeWithDocument(document);
    }

    /// <summary>Analyzes features dynamically used by one document with explicit options.</summary>
    /// <param name="document">The document to inspect without mutation.</param>
    /// <param name="options">The bounds, resource policy, and host capabilities.</param>
    /// <returns>The policy-aware dynamic feature analysis.</returns>
    public SvgFeatureAnalysisResult AnalyzeDocumentWithDocumentAndOptions(SvgDocument document, SvgFeatureAnalysisOptions options)
    {
        return new SvgDocumentFeatureAnalyzer().AnalyzeWithDocumentAndOptions(document, options);
    }

    /// <summary>Returns the published profile identifiers in contract order.</summary>
    /// <returns>A new profile identifier list.</returns>
    public System.Collections.Generic.List<string> GetProfileIdentifiers()
    {
        return new System.Collections.Generic.List<string>(
        [
            SvgFeatureIdentifiers.ProfileSvg2,
            SvgFeatureIdentifiers.ProfileSvg11Compatibility,
            SvgFeatureIdentifiers.ProfileLegacyPreservation,
            SvgFeatureIdentifiers.ProfileHostCallback
        ]);
    }

    /// <summary>Creates an honest report of the currently implemented library.</summary>
    /// <returns>A new deterministic library feature report.</returns>
    public SvgFeatureReport GetLibraryReport()
    {
        SvgFeatureReport report = new SvgFeatureReport();
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.SourceMapping, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Source provenance and nested mapping are implemented.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.Diagnostics, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Bounded deterministic diagnostics are implemented.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.SecurityLimits, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Secure default limit groups are implemented.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.CapabilityReporting, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Capability and status reporting are implemented.");

        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.Loading, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Secure SVG, SVGZ, fragment, path, and callback-mediated URI loading are implemented.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.EditableDocument, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "The source-preserving namespace-aware editable document is implemented.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.DocumentMutation, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Invariant-preserving node and attribute mutation is implemented.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.CssParsing, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Bounded CSS tokenization, parsing, validation, and writing are implemented.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.CssSelectors, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Bounded namespace-aware selector parsing, matching, and queries are implemented.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.CssCascade, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "The SVG and CSS cascade, computed values, variables, media, and invalidation are implemented.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.Shapes, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Current SVG basic shapes and their geometry are implemented.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.Paths, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "SVG path parsing, formatting, construction, and geometry are implemented.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.PathProcessing, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Bounded path measurement, transformation, flattening, outlining, simplification, and Boolean operations are implemented.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.PaintServers, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Solid paint, gradients, patterns, inheritance, and sampling are implemented.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.Markers, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Marker resolution, orientation, placement, bounds, and rendering are implemented.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.Clipping, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Clip-path resolution, units, geometry, hit testing, and rendering are implemented.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.Masking, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Mask resolution, units, luminance and alpha modes, and rendering are implemented.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.Filters, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Bounded filter graphs, standard primitives, regions, color spaces, and rendering are implemented.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.Images, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "SVG image resolution, intrinsic sizing, placement, sampling, and nested SVG content are implemented.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.PngCodec, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Portable bounded PNG probing, decoding, and encoding are implemented.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.HostImageCodec, SvgFeatureIdentifiers.ProfileHostCallback, SvgFeatureStatus.CallbackRequired, "Image formats without a portable decoder require explicit caller-provided bytes, metadata, and decoded-content handling.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.TextLayout, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Portable Unicode text, bidi, line breaking, baseline layout, text paths, and glyph geometry are implemented.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.OpenType, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Bounded managed OpenType, TrueType, CFF, SVG-font, and web-font reading is implemented for the declared profile.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.HostTextShaping, SvgFeatureIdentifiers.ProfileHostCallback, SvgFeatureStatus.CallbackRequired, "Advanced scripts and unsupported OpenType layout require an explicit caller shaping callback.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.SmilAnimation, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Caller-time SMIL timing, interpolation, motion, composition, and discard sampling are implemented.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.CssAnimation, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Caller-time CSS keyframe parsing, timing, interpolation, and composition are implemented.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.Interaction, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Geometry hit testing, selector interaction state, focus metadata, and navigation request creation are implemented.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.Accessibility, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Accessible names, descriptions, roles, validation, reading order, and accessibility-tree construction are implemented.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.Scene, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Deterministic backend-neutral resolved scenes, layers, resources, and command streams are implemented.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.Rasterization, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Portable deterministic bounded RGBA rasterization and compositing are implemented.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.ForeignContent, SvgFeatureIdentifiers.ProfileHostCallback, SvgFeatureStatus.CallbackRequired, "Foreign content is preserved and requires explicit host layout and rendering.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.ExternalResources, SvgFeatureIdentifiers.ProfileHostCallback, SvgFeatureStatus.CallbackRequired, "External files and network resources require policy approval and an explicit caller callback.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.Validation, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Bounded structural, value, CSS, reference, security, and accessibility validation is implemented.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.Sanitization, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Secure-static sanitization with an independent document and exact change report is implemented.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.Processing, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Ordered bounded safe and explicitly lossy document processing with exact change reporting is implemented.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.Generation, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Document, path, paint, filter, canvas, and sprite generation are implemented.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.Writing, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Implemented, "Source-preserving, canonical, pretty, compact, SVGZ, UTF-8, and data-URL writing are implemented.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.ScriptPreservation, SvgFeatureIdentifiers.ProfileLegacyPreservation, SvgFeatureStatus.PreserveOnly, "Scripts and event handlers can be retained as inert authored content but are never executed.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.ScriptExecution, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Unsupported, "Script execution is intentionally unavailable in every processing mode.");
        new SvgCapabilities().AddLibraryEntry(report, SvgFeatureIdentifiers.LegacyPreservation, SvgFeatureIdentifiers.ProfileLegacyPreservation, SvgFeatureStatus.PreserveOnly, "Recognized legacy, vendor, and unknown content is retained when preservation is enabled but is not silently rendered.");
        new SvgFeatureInventory().AddLibraryEntries(report);
        return report;
    }

    /// <summary>Returns all published feature identifiers in ordinal order.</summary>
    /// <returns>A new deterministic feature identifier list.</returns>
    public System.Collections.Generic.List<string> GetFeatureIdentifiers()
    {
        SvgFeatureReport report = new SvgCapabilities().GetLibraryReport();
        System.Collections.Generic.List<SvgFeatureEntry> entries = report.Snapshot();
        System.Collections.Generic.List<string> result = new System.Collections.Generic.List<string>();
        int index = 0;
        while (index < entries.Count)
        {
            result.Add(entries[index].Identifier);
            index = index + 1;
        }
        return result;
    }

    /// <summary>Creates a stable granular identifier from a family and inventory name.</summary>
    /// <param name="family">The feature family.</param>
    /// <param name="name">The nonempty inventory name.</param>
    /// <returns>The normalized public capability identifier.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown for an invalid family or name.</exception>
    public string GetFeatureIdentifierWithFamilyAndName(SvgFeatureFamily family, string name)
    {
        return new SvgFeatureInventory().Identifier(family, name);
    }

    /// <summary>Finds current library status for a feature identifier.</summary>
    /// <param name="identifier">The feature identifier.</param>
    /// <returns>The current library entry, or <c>null</c>.</returns>
    public SvgFeatureEntry? FindLibraryFeature(string identifier)
    {
        return new SvgCapabilities().GetLibraryReport().Find(identifier);
    }

    internal void AddLibraryEntry(SvgFeatureReport report, string identifier, string profile, SvgFeatureStatus status, string detail)
    {
        report.Add(new SvgFeatureEntry(identifier, profile, status, detail, null));
    }

}
