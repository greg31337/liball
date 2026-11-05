using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>
/// Provides locale inspection and selection operations.
/// </summary>
public class LocaleProvider : ILocaleProvider
{
    internal GenerationContext Context;
    internal FakeRandom RandomValue;
    internal LocaleProvider(GenerationContext context)
    {
        Context = context;
        RandomValue = context.ProviderRandom("locale");
    }

    /// <summary>
    /// Parses and canonicalizes a valid BCP 47 language tag using the bundled IANA registry.
    /// </summary>
    /// <param name="tag">The language tag to parse.</param>
    /// <returns>The parsed canonical language-tag components.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>tag</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>tag</c> is empty, malformed, or contains an unregistered subtag.</exception>
    public LanguageTag ParseTag(string tag)
    {
        return LanguageTagParser.Parse(tag, "tag");
    }

    /// <summary>
    /// Returns the canonical form of a valid BCP 47 language tag.
    /// </summary>
    /// <remarks>
    /// Canonicalization applies registry preferred values, removes extended-language
    /// prefixes, uses conventional casing, orders extensions by singleton, and lowercases
    /// extension and private-use values.
    /// </remarks>
    /// <param name="tag">The language tag to normalize.</param>
    /// <returns>The canonical language tag.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>tag</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>tag</c> is empty, malformed, or contains an unregistered subtag.</exception>
    public string NormalizeTag(string tag)
    {
        return ParseTag(tag).Normalized;
    }

    /// <summary>
    /// Resolves a field from active locale resources for the configured locale.
    /// </summary>
    /// <param name="field">The nonempty field name to resolve.</param>
    /// <returns>The resolved JSON field value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>field</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>field</c> is empty or whitespace.</exception>
    /// <exception cref="FakeException">Thrown when resources are invalid or no permitted source contains the field.</exception>
    public object Resolve(string field)
    {
        return LocaleResolver.Resolve(Context, Context.CapturedOptions.Locale, field, Context.LocaleValues());
    }

    /// <summary>
    /// Resolves a field for the configured locale using the configured fallback policy.
    /// </summary>
    /// <remarks>
    /// Allowed fallback proceeds through RFC 4647 lookup parents, the configured fallback
    /// locale and its parents, then <c>invariant</c>. Strict mode accepts only an exact
    /// normalized-locale field.
    /// </remarks>
    /// <param name="field">The nonempty field name to resolve.</param>
    /// <param name="values">The available locale field values.</param>
    /// <returns>The resolved field value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when an argument is malformed or contains duplicate normalized locale fields.</exception>
    /// <exception cref="FakeException">Thrown when no permitted source contains the field.</exception>
    public object ResolveFromFieldAndValues(string field, LocaleFieldValue[] values)
    {
        return LocaleResolver.Resolve(Context, Context.CapturedOptions.Locale, field, values);
    }

    /// <summary>
    /// Resolves a field for an explicit locale using the configured fallback policy.
    /// </summary>
    /// <remarks>
    /// Allowed fallback proceeds through RFC 4647 lookup parents, the configured fallback
    /// locale and its parents, then <c>invariant</c>. Strict mode accepts only an exact
    /// normalized-locale field.
    /// </remarks>
    /// <param name="locale">The requested BCP 47 locale.</param>
    /// <param name="field">The nonempty field name to resolve.</param>
    /// <param name="values">The available locale field values.</param>
    /// <returns>The resolved field value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when an argument is malformed or contains duplicate normalized locale fields.</exception>
    /// <exception cref="FakeException">Thrown when no permitted source contains the field.</exception>
    public object ResolveFromLocaleAndFieldAndValues(string locale, string field, LocaleFieldValue[] values)
    {
        return LocaleResolver.Resolve(Context, locale, field, values);
    }

    /// <summary>
    /// Returns the selected bundled-data version.
    /// </summary>
    /// <returns>The immutable version captured by this <see cref="Fake"/> instance.</returns>
    public string DataVersion()
    {
        return Context.CapturedOptions.DataVersion;
    }

    /// <summary>
    /// Reports whether a locale has native fields in the active catalog.
    /// </summary>
    /// <param name="locale">The BCP 47 locale tag to inspect.</param>
    /// <returns>True when the locale has native embedded or registered fields; otherwise false.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>locale</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>locale</c> is malformed.</exception>
    public bool IsSupported(string locale)
    {
        return ActiveCatalog().IsSupported(locale);
    }

    /// <summary>
    /// Reports whether a locale has native fields in a supplied catalog.
    /// </summary>
    /// <param name="locale">The BCP 47 locale tag to inspect.</param>
    /// <param name="values">The available locale field values.</param>
    /// <returns>True when the locale has native fields; otherwise false.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when an argument is malformed.</exception>
    public bool IsSupportedFromLocaleAndValues(string locale, LocaleFieldValue[] values)
    {
        return Catalog(values).IsSupported(locale);
    }

    /// <summary>
    /// Enumerates canonical locale tags with native fields in the active catalog.
    /// </summary>
    /// <returns>Canonical supported tags in ordinal order.</returns>
    public string[] Supported()
    {
        return ActiveCatalog().Supported();
    }

    /// <summary>
    /// Enumerates canonical locale tags with native fields in a supplied catalog.
    /// </summary>
    /// <param name="values">The available locale field values.</param>
    /// <returns>Canonical supported tags in ordinal order.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>values</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>values</c> is malformed.</exception>
    public string[] SupportedFromValues(LocaleFieldValue[] values)
    {
        return Catalog(values).Supported();
    }

    /// <summary>
    /// Returns native metadata for a locale in the active catalog.
    /// </summary>
    /// <param name="locale">The BCP 47 locale tag to inspect.</param>
    /// <returns>The locale metadata.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>locale</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>locale</c> is malformed.</exception>
    /// <exception cref="FakeException">Thrown when the locale has no native fields.</exception>
    public LocaleInfo Metadata(string locale)
    {
        return ActiveCatalog().Metadata(locale);
    }

    /// <summary>
    /// Returns native metadata for a locale in a supplied catalog.
    /// </summary>
    /// <param name="locale">The BCP 47 locale tag to inspect.</param>
    /// <param name="values">The available locale field values.</param>
    /// <returns>The locale metadata.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when an argument is malformed.</exception>
    /// <exception cref="FakeException">Thrown when the locale has no native fields.</exception>
    public LocaleInfo MetadataFromLocaleAndValues(string locale, LocaleFieldValue[] values)
    {
        return Catalog(values).Metadata(locale);
    }

    /// <summary>
    /// Returns the script codes and display names represented by active locale profiles.
    /// </summary>
    /// <returns>Independent records in canonical script-code order.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured count or output limit is insufficient.</exception>
    /// <exception cref="FakeException">Thrown when a locale profile is malformed.</exception>
    public LocaleNameRecord[] Scripts()
    {
        return ActiveCatalog().Scripts();
    }

    /// <summary>
    /// Returns the script codes and display names represented by supplied locale profiles.
    /// </summary>
    /// <param name="values">The available locale field values.</param>
    /// <returns>Independent records in canonical script-code order.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>values</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>values</c> is malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured count or output limit is insufficient.</exception>
    /// <exception cref="FakeException">Thrown when a locale profile is malformed.</exception>
    public LocaleNameRecord[] ScriptsFromValues(LocaleFieldValue[] values)
    {
        return Catalog(values).Scripts();
    }

    /// <summary>
    /// Returns the territory codes and native display names represented by active locale profiles.
    /// </summary>
    /// <returns>Independent records in canonical territory-code order.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured count or output limit is insufficient.</exception>
    /// <exception cref="FakeException">Thrown when a locale profile is malformed.</exception>
    public LocaleNameRecord[] Territories()
    {
        return ActiveCatalog().Territories();
    }

    /// <summary>
    /// Returns the territory codes and native display names represented by supplied locale profiles.
    /// </summary>
    /// <param name="values">The available locale field values.</param>
    /// <returns>Independent records in canonical territory-code order.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>values</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>values</c> is malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured count or output limit is insufficient.</exception>
    /// <exception cref="FakeException">Thrown when a locale profile is malformed.</exception>
    public LocaleNameRecord[] TerritoriesFromValues(LocaleFieldValue[] values)
    {
        return Catalog(values).Territories();
    }

    /// <summary>
    /// Reports native and fallback field and provider coverage from the active catalog.
    /// </summary>
    /// <param name="locale">The BCP 47 locale tag to inspect.</param>
    /// <returns>The locale coverage under the active fallback policy.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>locale</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>locale</c> is malformed.</exception>
    public LocaleCoverage Coverage(string locale)
    {
        return ActiveCatalog().Coverage(locale);
    }

    /// <summary>
    /// Reports native and fallback field and provider coverage from a supplied catalog.
    /// </summary>
    /// <param name="locale">The BCP 47 locale tag to inspect.</param>
    /// <param name="values">The available locale field values.</param>
    /// <returns>The locale coverage under the active fallback policy.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when an argument is malformed.</exception>
    public LocaleCoverage CoverageFromLocaleAndValues(string locale, LocaleFieldValue[] values)
    {
        return Catalog(values).Coverage(locale);
    }

    /// <summary>
    /// Resolves a configured-locale field from active resources and reports its source.
    /// </summary>
    /// <param name="field">The nonempty field name to trace.</param>
    /// <returns>The deterministic resolution trace.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>field</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>field</c> is empty or whitespace.</exception>
    /// <exception cref="FakeException">Thrown when resources are invalid or no permitted source contains the field.</exception>
    public FallbackTrace FallbackTrace(string field)
    {
        return LocaleResolver.Trace(Context, Context.CapturedOptions.Locale, field, Context.LocaleValues());
    }

    /// <summary>
    /// Resolves a configured-locale field and reports the winning source.
    /// </summary>
    /// <param name="field">The nonempty field name to trace.</param>
    /// <param name="values">The available locale field values.</param>
    /// <returns>The deterministic resolution trace.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when an argument is malformed.</exception>
    /// <exception cref="FakeException">Thrown when no permitted source contains the field.</exception>
    public FallbackTrace FallbackTraceFromFieldAndValues(string field, LocaleFieldValue[] values)
    {
        return LocaleResolver.Trace(Context, Context.CapturedOptions.Locale, field, values);
    }

    /// <summary>
    /// Resolves an explicit-locale field and reports the winning source.
    /// </summary>
    /// <param name="locale">The requested BCP 47 locale tag.</param>
    /// <param name="field">The nonempty field name to trace.</param>
    /// <param name="values">The available locale field values.</param>
    /// <returns>The deterministic resolution trace.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when an argument is malformed.</exception>
    /// <exception cref="FakeException">Thrown when no permitted source contains the field.</exception>
    public FallbackTrace FallbackTraceFromLocaleAndFieldAndValues(string locale, string field, LocaleFieldValue[] values)
    {
        return LocaleResolver.Trace(Context, locale, field, values);
    }

    /// <summary>
    /// Registers a caller-supplied JSON locale-pack stream for this <see cref="Fake"/> instance.
    /// </summary>
    /// <remarks>
    /// The stream remains open. Runtime fields override embedded fields for the same canonical
    /// locale and field. A later successful registration replaces an earlier runtime field.
    /// </remarks>
    /// <param name="stream">The readable UTF-8 locale-pack stream.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>stream</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>stream</c> is unreadable.</exception>
    /// <exception cref="FakeException">Thrown when the pack is malformed or uses another data version.</exception>
    public void Register(Stream stream)
    {
        RegisterFromStreamAndResourceName(stream, "caller-locale-pack.json");
    }

    /// <summary>
    /// Registers a named caller-supplied JSON locale-pack stream for this <see cref="Fake"/> instance.
    /// </summary>
    /// <remarks>
    /// The stream remains open. The resource name is used in diagnostics. Runtime fields override
    /// embedded fields for the same canonical locale and field. Later successful registration wins.
    /// </remarks>
    /// <param name="stream">The readable UTF-8 locale-pack stream.</param>
    /// <param name="resourceName">The nonempty diagnostic resource name.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when an argument is empty, whitespace, or unreadable.</exception>
    /// <exception cref="FakeException">Thrown when the pack is malformed or uses another data version.</exception>
    public void RegisterFromStreamAndResourceName(Stream stream, string resourceName)
    {
        Context.LocaleRegistrations.Register(resourceName, stream);
    }

    /// <summary>
    /// Registers caller-supplied fields for one locale using captured version metadata.
    /// </summary>
    /// <remarks>
    /// The dictionary is copied, while field values are captured by reference. Runtime fields
    /// override embedded fields for the same canonical locale and field. Later registration wins.
    /// </remarks>
    /// <param name="locale">The BCP 47 locale tag or <c>invariant</c>.</param>
    /// <param name="fields">The nonempty field dictionary.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument or field value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when an argument is malformed.</exception>
    public void RegisterFromLocaleAndFields(string locale, Dictionary<string, object> fields)
    {
        RegisterFromLocaleAndDataVersionAndProvenanceAndFields(locale, Context.CapturedOptions.DataVersion, LocaleFieldValue.CallerProvenance, fields);
    }

    /// <summary>
    /// Registers caller-supplied fields with explicit version and provenance metadata.
    /// </summary>
    /// <remarks>
    /// The supplied version must equal the version captured by this instance. The dictionary is
    /// copied, while values are captured by reference. Later registration wins for an equal field.
    /// </remarks>
    /// <param name="locale">The BCP 47 locale tag or <c>invariant</c>.</param>
    /// <param name="dataVersion">The captured locale data version.</param>
    /// <param name="provenance">The nonempty source or authorship description.</param>
    /// <param name="fields">The nonempty field dictionary.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument or field value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when an argument is malformed or uses another version.</exception>
    public void RegisterFromLocaleAndDataVersionAndProvenanceAndFields(string locale, string dataVersion, string provenance, Dictionary<string, object> fields)
    {
        Context.LocaleRegistrations.RegisterFromLocaleAndDataVersionAndProvenanceAndFields(locale, dataVersion, provenance, fields);
    }

    LocaleCatalog Catalog(LocaleFieldValue[] values)
    {
        return new LocaleCatalog(Context, values);
    }

    LocaleCatalog ActiveCatalog()
    {
        return Catalog(Context.LocaleValues());
    }
}
