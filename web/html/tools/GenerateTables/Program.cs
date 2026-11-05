using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace UniversalHtml.Tools.GenerateTables;

internal static class Program
{
    private const string GeneratorVersion = "1.3.0";

    public static int Main(string[] args)
    {
        try
        {
            GeneratorArguments arguments = GeneratorArguments.Parse(args);
            if (arguments.ShowHelp)
            {
                WriteHelp();
                return 0;
            }

            string root = arguments.Root ?? FindRepositoryRoot();
            return Run(root, arguments.Mode);
        }
        catch (GeneratorUsageException exception)
        {
            Console.Error.WriteLine(exception.Message);
            Console.Error.WriteLine("Run with --help for usage.");
            return 2;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"GenerateTables: {exception.Message}");
            return 1;
        }
    }

    private static int Run(string root, string mode)
    {
        root = Path.GetFullPath(root);
        string manifestPath = ResolveWithinRoot(root, "tools/GenerateTables/inputs/manifest.json");
        GeneratorManifest manifest = ReadJson<GeneratorManifest>(manifestPath);
        ValidateManifest(manifest);

        List<VerifiedInput> inputs = [];
        foreach (GeneratorInput input in manifest.Inputs)
        {
            string path = ResolveWithinRoot(root, input.Path);
            byte[] bytes = File.ReadAllBytes(path);
            string checksum = Convert.ToHexStringLower(SHA256.HashData(bytes));
            if (!string.Equals(checksum, input.Sha256, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Checksum mismatch for input '{input.Id}'. Expected {input.Sha256}, found {checksum}.");
            }

            inputs.Add(new VerifiedInput(input, path, checksum));
        }

        inputs.Sort((left, right) => string.CompareOrdinal(left.Input.Id, right.Input.Id));
        VerifiedInput standardsInput = inputs.Find(input => input.Input.Id == "standards")
            ?? throw new InvalidDataException("The pinned 'standards' input is required.");
        VerifiedInput htmlNamesInput = inputs.Find(input => input.Input.Id == "html-names")
            ?? throw new InvalidDataException("The pinned 'html-names' input is required.");
        VerifiedInput doctypeForeignInput = inputs.Find(input => input.Input.Id == "doctype-foreign-names")
            ?? throw new InvalidDataException("The pinned 'doctype-foreign-names' input is required.");
        VerifiedInput encodingLabelsInput = inputs.Find(input => input.Input.Id == "encoding-labels")
            ?? throw new InvalidDataException("The pinned 'encoding-labels' input is required.");
        VerifiedInput encodingIndexesInput = inputs.Find(input => input.Input.Id == "encoding-indexes")
            ?? throw new InvalidDataException("The pinned 'encoding-indexes' input is required.");
        VerifiedInput htmlEntitiesInput = inputs.Find(input => input.Input.Id == "html-entities")
            ?? throw new InvalidDataException("The pinned 'html-entities' input is required.");
        VerifiedInput cssSafeDataInput = inputs.Find(input => input.Input.Id == "css-safe-data")
            ?? throw new InvalidDataException("The pinned 'css-safe-data' input is required.");
        StandardsDocument standards = ReadJson<StandardsDocument>(standardsInput.AbsolutePath);
        if (standards.SchemaVersion != 1)
        {
            throw new InvalidDataException($"Unsupported standards schema version {standards.SchemaVersion}.");
        }

        standards.Standards.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
        ValidateStandards(standards.Standards);
        HtmlNamesDocument htmlNames = ReadJson<HtmlNamesDocument>(htmlNamesInput.AbsolutePath);
        ValidateHtmlNames(htmlNames);
        DoctypeForeignDocument doctypeForeign = ReadJson<DoctypeForeignDocument>(doctypeForeignInput.AbsolutePath);
        ValidateDoctypeForeign(doctypeForeign);
        EncodingLabelsDocument encodingLabels = ReadJson<EncodingLabelsDocument>(encodingLabelsInput.AbsolutePath);
        ValidateEncodingLabels(encodingLabels);
        JsonDocument encodingIndexes = ReadCompressedJsonDocument(encodingIndexesInput.AbsolutePath);
        ValidateEncodingIndexes(encodingIndexes.RootElement);
        JsonDocument htmlEntities = ReadCompressedJsonDocument(htmlEntitiesInput.AbsolutePath);
        List<NamedCharacterReferencePin> namedCharacterReferences = ReadNamedCharacterReferences(htmlEntities.RootElement);
        CssSafeDataDocument cssSafeData = ReadJson<CssSafeDataDocument>(cssSafeDataInput.AbsolutePath);
        ValidateCssSafeData(cssSafeData);

        List<GeneratedOutput> outputs =
        [
            new GeneratedOutput(ResolveWithinRoot(root, manifest.ManifestOutput), ApplyProductionVisibility(GenerateManifestSource(inputs, standards.Standards))),
            new GeneratedOutput(ResolveWithinRoot(root, manifest.HtmlNamesOutput), ApplyProductionVisibility(GenerateHtmlNamesSource(inputs, htmlNames.Sets))),
            new GeneratedOutput(ResolveWithinRoot(root, manifest.DoctypeOutput), ApplyProductionVisibility(GenerateDoctypeSource(inputs, doctypeForeign.Doctype))),
            new GeneratedOutput(ResolveWithinRoot(root, manifest.ForeignNamesOutput), ApplyProductionVisibility(GenerateForeignNamesSource(inputs, doctypeForeign))),
            new GeneratedOutput(ResolveWithinRoot(root, manifest.EncodingLabelsOutput), ApplyProductionVisibility(GenerateEncodingLabelsSource(inputs, encodingLabels))),
            new GeneratedOutput(ResolveWithinRoot(root, manifest.EncodingIndexesOutput), ApplyProductionVisibility(GenerateEncodingIndexesSource(inputs, encodingIndexes.RootElement))),
            new GeneratedOutput(ResolveWithinRoot(root, manifest.HtmlEntitiesOutput), ApplyProductionVisibility(GenerateNamedCharacterReferencesSource(inputs, namedCharacterReferences))),
            new GeneratedOutput(ResolveWithinRoot(root, manifest.CssSafeDataOutput), ApplyProductionVisibility(GenerateCssSafeDataSource(inputs, cssSafeData)))
        ];

        if (mode == "verify")
        {
            Console.WriteLine($"Verified {inputs.Count} pinned input(s).");
            return 0;
        }

        if (mode == "check")
        {
            bool current = true;
            foreach (GeneratedOutput output in outputs)
            {
                if (!File.Exists(output.AbsolutePath))
                {
                    Console.Error.WriteLine($"Generated output is missing: {ToRepositoryPath(root, output.AbsolutePath)}");
                    current = false;
                    continue;
                }

                string committed = File.ReadAllText(output.AbsolutePath, Encoding.UTF8);
                if (!string.Equals(committed, output.Content, StringComparison.Ordinal))
                {
                    Console.Error.WriteLine($"Generated output is stale: {ToRepositoryPath(root, output.AbsolutePath)}");
                    current = false;
                    continue;
                }

                Console.WriteLine($"Generated output is current: {ToRepositoryPath(root, output.AbsolutePath)}");
            }

            if (!current)
            {
                Console.Error.WriteLine("Run GenerateTables with --write and commit the result.");
                return 1;
            }

            return 0;
        }

        foreach (GeneratedOutput output in outputs)
        {
            WriteAtomically(output.AbsolutePath, output.Content);
            Console.WriteLine($"Wrote {ToRepositoryPath(root, output.AbsolutePath)}");
        }

        return 0;
    }

    private static string ApplyProductionVisibility(string source)
    {
        string[] lines = source.Split('\n');
        int braceDepth = 0;
        bool topLevelEnum = false;
        for (int index = 0; index < lines.Length; index++)
        {
            string line = lines[index];
            string trimmed = line.TrimStart();
            if (braceDepth == 0)
            {
                if (trimmed.StartsWith("enum ", StringComparison.Ordinal))
                {
                    topLevelEnum = true;
                    lines[index] = "internal " + line;
                    line = lines[index];
                }
                else if (trimmed.StartsWith("class ", StringComparison.Ordinal)
                    || trimmed.StartsWith("static class ",
                        StringComparison.Ordinal))
                {
                    topLevelEnum = false;
                    lines[index] = "internal " + line;
                    line = lines[index];
                }
            }
            else if (braceDepth == 1 && !topLevelEnum
                && line.StartsWith("    ", StringComparison.Ordinal)
                && !line.StartsWith("        ", StringComparison.Ordinal)
                && trimmed.Length > 0 && trimmed != "{" && trimmed != "}"
                && !trimmed.StartsWith("[", StringComparison.Ordinal)
                && !trimmed.StartsWith("]", StringComparison.Ordinal))
            {
                if (trimmed.StartsWith("public ", StringComparison.Ordinal))
                {
                    lines[index] = "    internal "
                        + trimmed.Substring("public ".Length);
                    line = lines[index];
                }
                else if (!trimmed.StartsWith("internal ",
                             StringComparison.Ordinal))
                {
                    lines[index] = "    internal " + trimmed;
                    line = lines[index];
                }
            }

            foreach (char value in line)
            {
                if (value == '{')
                {
                    braceDepth++;
                }
                else if (value == '}')
                {
                    braceDepth--;
                }
            }
        }

        return string.Join('\n', lines);
    }

    private static void ValidateManifest(GeneratorManifest manifest)
    {
        if (manifest.SchemaVersion != 1)
        {
            throw new InvalidDataException($"Unsupported generator manifest schema version {manifest.SchemaVersion}.");
        }

        if (!string.Equals(manifest.GeneratorVersion, GeneratorVersion, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Manifest generator version '{manifest.GeneratorVersion}' does not match tool version '{GeneratorVersion}'.");
        }

        if (manifest.Inputs.Count == 0)
        {
            throw new InvalidDataException("The generator manifest contains no pinned inputs.");
        }

        HashSet<string> ids = new(StringComparer.Ordinal);
        foreach (GeneratorInput input in manifest.Inputs)
        {
            if (string.IsNullOrWhiteSpace(input.Id) || string.IsNullOrWhiteSpace(input.Path)
                || string.IsNullOrWhiteSpace(input.Revision) || input.Sha256.Length != 64)
            {
                throw new InvalidDataException("Every generator input requires an id, path, revision, and lowercase SHA-256 checksum.");
            }

            if (!ids.Add(input.Id))
            {
                throw new InvalidDataException($"Duplicate generator input id '{input.Id}'.");
            }

            foreach (char value in input.Sha256)
            {
                if (!(value >= '0' && value <= '9') && !(value >= 'a' && value <= 'f'))
                {
                    throw new InvalidDataException($"Input '{input.Id}' has a non-canonical SHA-256 checksum.");
                }
            }
        }

        if (string.IsNullOrWhiteSpace(manifest.ManifestOutput) || string.IsNullOrWhiteSpace(manifest.HtmlNamesOutput)
            || string.IsNullOrWhiteSpace(manifest.DoctypeOutput) || string.IsNullOrWhiteSpace(manifest.ForeignNamesOutput)
            || string.IsNullOrWhiteSpace(manifest.EncodingLabelsOutput) || string.IsNullOrWhiteSpace(manifest.EncodingIndexesOutput)
            || string.IsNullOrWhiteSpace(manifest.HtmlEntitiesOutput) || string.IsNullOrWhiteSpace(manifest.CssSafeDataOutput))
        {
            throw new InvalidDataException("Every generated output path is required.");
        }
    }

    private static void ValidateCssSafeData(CssSafeDataDocument document)
    {
        if (document.SchemaVersion != 1 || document.SecurityRationales.Count == 0
            || document.Properties.Count == 0 || document.Functions.Count == 0
            || document.Keywords.Count == 0 || document.Units.Count == 0
            || document.Colors.Count == 0 || document.Sources.Count == 0)
        {
            throw new InvalidDataException("The CSS safety input has an unsupported schema or an empty required table.");
        }

        foreach (CssSourcePin source in document.Sources)
        {
            if (!Uri.TryCreate(source.Url, UriKind.Absolute, out Uri? uri)
                || uri.Scheme != Uri.UriSchemeHttps || string.IsNullOrWhiteSpace(source.Revision))
            {
                throw new InvalidDataException("Every CSS safety source requires an HTTPS URL and revision.");
            }
        }

        foreach (string rationale in document.SecurityRationales)
        {
            if (string.IsNullOrWhiteSpace(rationale))
            {
                throw new InvalidDataException("A CSS security rationale cannot be empty.");
            }
        }

        ValidateCssProfileNames(document.PropertyProfiles, "property profile");
        ValidateCssProfileNames(document.FunctionProfiles, "function profile");
        ValidateCssProfileNames(document.UnitKinds, "unit kind");
        ValidateCssProfilePins(document.Properties, document.PropertyProfiles, "property");
        ValidateCssProfilePins(document.Functions, document.FunctionProfiles, "function");
        ValidateCssProfilePins(document.Units, document.UnitKinds, "unit");
        ValidateCssNames(document.Keywords, "keyword");
        ValidateCssNames(document.Colors, "color");
        ValidateCssNames(document.ExcludedProperties, "excluded property");
        ValidateCssNames(document.ExcludedFunctions, "excluded function");
        ValidateCssNames(document.ExcludedUnits, "excluded unit");

        EnsureCssTablesAreDisjoint(document.Properties.Select(value => value.Name),
            document.ExcludedProperties, "property");
        EnsureCssTablesAreDisjoint(document.Functions.Select(value => value.Name),
            document.ExcludedFunctions, "function");
        EnsureCssTablesAreDisjoint(document.Units.Select(value => value.Name),
            document.ExcludedUnits, "unit");

        CssNamedProfilePin? urlFunction = document.Functions.Find(value => value.Name == "url");
        if (urlFunction == null || urlFunction.Profile != "Url")
        {
            throw new InvalidDataException("The CSS url() function must remain explicitly classified as Url.");
        }

        foreach (CssNamedProfilePin property in document.Properties)
        {
            if ((property.Name == "background-image" || property.Name == "list-style-image")
                && property.Profile != "UrlImage")
            {
                throw new InvalidDataException($"URL-bearing property '{property.Name}' must use the UrlImage profile.");
            }
        }

        string[] requiredExcludedProperties = ["-moz-binding", "behavior", "content", "cursor", "filter", "position", "z-index"];
        string[] requiredExcludedFunctions = ["attr", "env", "expression", "image-set", "paint"];
        foreach (string name in requiredExcludedProperties)
        {
            if (!document.ExcludedProperties.Contains(name, StringComparer.Ordinal))
            {
                throw new InvalidDataException($"Security-sensitive CSS property '{name}' must remain explicitly excluded.");
            }
        }

        foreach (string name in requiredExcludedFunctions)
        {
            if (!document.ExcludedFunctions.Contains(name, StringComparer.Ordinal))
            {
                throw new InvalidDataException($"Security-sensitive CSS function '{name}' must remain explicitly excluded.");
            }
        }

        document.Properties.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));
        document.Functions.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));
        document.Units.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));
        document.Keywords.Sort(StringComparer.Ordinal);
        document.Colors.Sort(StringComparer.Ordinal);
        document.ExcludedProperties.Sort(StringComparer.Ordinal);
        document.ExcludedFunctions.Sort(StringComparer.Ordinal);
        document.ExcludedUnits.Sort(StringComparer.Ordinal);
    }

    private static void ValidateCssProfileNames(List<string> profiles, string description)
    {
        if (profiles.Count == 0)
        {
            throw new InvalidDataException($"The CSS {description} list cannot be empty.");
        }

        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (string profile in profiles)
        {
            if (profile.Length == 0 || !char.IsAsciiLetterUpper(profile[0])
                || !profile.All(value => char.IsAsciiLetterOrDigit(value)) || !seen.Add(profile))
            {
                throw new InvalidDataException($"Invalid or duplicate CSS {description} '{profile}'.");
            }
        }
    }

    private static void ValidateCssProfilePins(List<CssNamedProfilePin> values,
        List<string> profiles, string description)
    {
        HashSet<string> names = new(StringComparer.Ordinal);
        HashSet<string> knownProfiles = new(profiles, StringComparer.Ordinal);
        foreach (CssNamedProfilePin value in values)
        {
            ValidateCssName(value.Name, description);
            if (value.Profile.Length > 0 && value.Kind.Length > 0
                || !names.Add(value.Name) || !knownProfiles.Contains(value.GetProfile()))
            {
                throw new InvalidDataException($"Invalid duplicate or profile for CSS {description} '{value.Name}'.");
            }
        }
    }

    private static void ValidateCssNames(List<string> values, string description)
    {
        HashSet<string> names = new(StringComparer.Ordinal);
        foreach (string value in values)
        {
            ValidateCssName(value, description);
            if (!names.Add(value))
            {
                throw new InvalidDataException($"Duplicate CSS {description} '{value}'.");
            }
        }
    }

    private static void ValidateCssName(string value, string description)
    {
        if (value.Length == 0 || value != value.ToLowerInvariant()
            || value.Any(character => character < 33 || character > 126))
        {
            throw new InvalidDataException($"CSS {description} '{value}' must be lowercase printable ASCII.");
        }
    }

    private static void EnsureCssTablesAreDisjoint(IEnumerable<string> allowed,
        List<string> excluded, string description)
    {
        HashSet<string> allowedNames = new(allowed, StringComparer.Ordinal);
        foreach (string name in excluded)
        {
            if (allowedNames.Contains(name))
            {
                throw new InvalidDataException($"CSS {description} '{name}' cannot be both allowed and explicitly excluded.");
            }
        }
    }

    private static void ValidateEncodingLabels(EncodingLabelsDocument document)
    {
        if (document.SchemaVersion != 1 || document.Encodings.Count == 0)
        {
            throw new InvalidDataException("The encoding-label input has an unsupported schema or no encodings.");
        }

        HashSet<string> names = new(StringComparer.Ordinal);
        HashSet<string> labels = new(StringComparer.Ordinal);
        foreach (EncodingPin encoding in document.Encodings)
        {
            if (encoding.Name.Length == 0 || encoding.Labels.Count == 0 || !names.Add(encoding.Name))
            {
                throw new InvalidDataException($"Invalid or duplicate encoding name '{encoding.Name}'.");
            }

            foreach (string label in encoding.Labels)
            {
                if (label.Length == 0 || !labels.Add(label) || !string.Equals(label, label.ToLowerInvariant(), StringComparison.Ordinal))
                {
                    throw new InvalidDataException($"Invalid or duplicate encoding label '{label}'.");
                }

                foreach (char value in label)
                {
                    if (value < 33 || value > 126)
                    {
                        throw new InvalidDataException($"Encoding label '{label}' contains a non-ASCII graphic character.");
                    }
                }
            }

            encoding.Labels.Sort(StringComparer.Ordinal);
        }

        document.Encodings.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));
    }

    private static void ValidateEncodingIndexes(JsonElement root)
    {
        Dictionary<string, int> expected = new(StringComparer.Ordinal)
        {
            ["big5"] = 19782,
            ["euc-kr"] = 23940,
            ["gb18030"] = 23940,
            ["gb18030-ranges"] = 207,
            ["jis0208"] = 11280,
            ["jis0212"] = 8836,
            ["ibm866"] = 128,
            ["iso-2022-jp-katakana"] = 63,
            ["iso-8859-2"] = 128,
            ["iso-8859-3"] = 128,
            ["iso-8859-4"] = 128,
            ["iso-8859-5"] = 128,
            ["iso-8859-6"] = 128,
            ["iso-8859-7"] = 128,
            ["iso-8859-8"] = 128,
            ["iso-8859-10"] = 128,
            ["iso-8859-13"] = 128,
            ["iso-8859-14"] = 128,
            ["iso-8859-15"] = 128,
            ["iso-8859-16"] = 128,
            ["koi8-r"] = 128,
            ["koi8-u"] = 128,
            ["macintosh"] = 128,
            ["windows-874"] = 128,
            ["windows-1250"] = 128,
            ["windows-1251"] = 128,
            ["windows-1252"] = 128,
            ["windows-1253"] = 128,
            ["windows-1254"] = 128,
            ["windows-1255"] = 128,
            ["windows-1256"] = 128,
            ["windows-1257"] = 128,
            ["windows-1258"] = 128,
            ["x-mac-cyrillic"] = 128
        };

        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != expected.Count)
        {
            throw new InvalidDataException("The encoding-index input has an unexpected root shape.");
        }

        foreach (KeyValuePair<string, int> item in expected)
        {
            if (!root.TryGetProperty(item.Key, out JsonElement index) || index.ValueKind != JsonValueKind.Array
                || index.GetArrayLength() != item.Value)
            {
                throw new InvalidDataException($"Encoding index '{item.Key}' is missing or has an unexpected length.");
            }

            int previousPointer = -1;
            foreach (JsonElement value in index.EnumerateArray())
            {
                if (item.Key == "gb18030-ranges")
                {
                    if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() != 2)
                    {
                        throw new InvalidDataException("A gb18030 range entry is malformed.");
                    }

                    int pointer = value[0].GetInt32();
                    int codePoint = value[1].GetInt32();
                    if (pointer <= previousPointer || codePoint < 0 || codePoint > 0x10FFFF)
                    {
                        throw new InvalidDataException("The gb18030 ranges are not canonical.");
                    }

                    previousPointer = pointer;
                }
                else if (value.ValueKind != JsonValueKind.Null
                    && (value.ValueKind != JsonValueKind.Number || value.GetInt32() < 0 || value.GetInt32() > 0x10FFFF))
                {
                    throw new InvalidDataException($"Encoding index '{item.Key}' contains an invalid code point.");
                }
            }
        }
    }

    private static List<NamedCharacterReferencePin> ReadNamedCharacterReferences(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException("The named-character-reference input must be a JSON object.");
        }

        List<NamedCharacterReferencePin> references = [];
        HashSet<string> names = new(StringComparer.Ordinal);
        int legacyCount = 0;
        int twoCodePointCount = 0;
        foreach (JsonProperty property in root.EnumerateObject())
        {
            if (property.Name.Length < 2 || property.Name[0] != '&')
            {
                throw new InvalidDataException($"Named character reference '{property.Name}' does not begin with '&'.");
            }

            string name = property.Name[1..];
            if (!names.Add(name))
            {
                throw new InvalidDataException($"Duplicate named character reference '{property.Name}'.");
            }

            foreach (char value in name)
            {
                if (!(value >= '0' && value <= '9') && !(value >= 'A' && value <= 'Z')
                    && !(value >= 'a' && value <= 'z') && value != ';')
                {
                    throw new InvalidDataException($"Named character reference '{property.Name}' contains an unexpected character.");
                }
            }

            JsonElement valueObject = property.Value;
            if (valueObject.ValueKind != JsonValueKind.Object
                || !valueObject.TryGetProperty("codepoints", out JsonElement codePoints)
                || codePoints.ValueKind != JsonValueKind.Array
                || codePoints.GetArrayLength() < 1 || codePoints.GetArrayLength() > 2
                || !valueObject.TryGetProperty("characters", out JsonElement characters)
                || characters.ValueKind != JsonValueKind.String)
            {
                throw new InvalidDataException($"Named character reference '{property.Name}' has an unexpected value shape.");
            }

            int firstCodePoint = codePoints[0].GetInt32();
            int secondCodePoint = codePoints.GetArrayLength() == 2 ? codePoints[1].GetInt32() : -1;
            ValidateScalar(property.Name, firstCodePoint);
            if (secondCodePoint >= 0)
            {
                ValidateScalar(property.Name, secondCodePoint);
                twoCodePointCount++;
            }

            string expectedCharacters = char.ConvertFromUtf32(firstCodePoint);
            if (secondCodePoint >= 0)
            {
                expectedCharacters += char.ConvertFromUtf32(secondCodePoint);
            }

            if (!string.Equals(expectedCharacters, characters.GetString(), StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Named character reference '{property.Name}' has inconsistent characters and code points.");
            }

            if (!name.EndsWith(';'))
            {
                legacyCount++;
            }

            references.Add(new NamedCharacterReferencePin(name, firstCodePoint, secondCodePoint));
        }

        if (references.Count != 2231 || legacyCount != 106 || twoCodePointCount != 93)
        {
            throw new InvalidDataException(
                $"Named character reference counts are unexpected: {references.Count} entries, {legacyCount} legacy entries, {twoCodePointCount} two-code-point entries.");
        }

        references.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));
        return references;
    }

    private static void ValidateScalar(string name, int codePoint)
    {
        if (codePoint < 0 || codePoint > 0x10FFFF || codePoint >= 0xD800 && codePoint <= 0xDFFF)
        {
            throw new InvalidDataException($"Named character reference '{name}' contains an invalid Unicode scalar.");
        }
    }

    private static void ValidateDoctypeForeign(DoctypeForeignDocument document)
    {
        if (document.SchemaVersion != 1)
        {
            throw new InvalidDataException($"Unsupported doctype/foreign-names schema version {document.SchemaVersion}.");
        }

        ValidateUniqueStrings("quirks public identifiers", document.Doctype.QuirksPublicExact);
        ValidateUniqueStrings("quirks public prefixes", document.Doctype.QuirksPublicPrefixes);
        ValidateUniqueStrings("HTML 4.01 public prefixes", document.Doctype.Html401PublicPrefixes);
        ValidateUniqueStrings("limited-quirks public prefixes", document.Doctype.LimitedQuirksPublicPrefixes);
        ValidateUniqueStrings("quirks system identifiers", document.Doctype.QuirksSystemExact);
        ValidateMappings("MathML attributes", document.MathMlAttributes);
        ValidateMappings("SVG tag names", document.SvgTagNames);
        ValidateMappings("SVG attributes", document.SvgAttributes);

        HashSet<string> foreignInputs = new(StringComparer.Ordinal);
        foreach (ForeignAttributePin adjustment in document.ForeignAttributes)
        {
            ValidateInputName("foreign attribute", adjustment.Input);
            if (!foreignInputs.Add(adjustment.Input) || adjustment.LocalName.Length == 0
                || adjustment.NamespaceUri.Length == 0)
            {
                throw new InvalidDataException($"Invalid or duplicate foreign attribute adjustment '{adjustment.Input}'.");
            }

            if (!Uri.TryCreate(adjustment.NamespaceUri, UriKind.Absolute, out Uri? uri)
                || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            {
                throw new InvalidDataException($"Foreign attribute '{adjustment.Input}' has an invalid namespace URI.");
            }
        }

        document.Doctype.QuirksPublicExact.Sort(StringComparer.Ordinal);
        document.Doctype.QuirksPublicPrefixes.Sort(StringComparer.Ordinal);
        document.Doctype.Html401PublicPrefixes.Sort(StringComparer.Ordinal);
        document.Doctype.LimitedQuirksPublicPrefixes.Sort(StringComparer.Ordinal);
        document.Doctype.QuirksSystemExact.Sort(StringComparer.Ordinal);
        document.MathMlAttributes.Sort((left, right) => string.CompareOrdinal(left.Input, right.Input));
        document.SvgTagNames.Sort((left, right) => string.CompareOrdinal(left.Input, right.Input));
        document.SvgAttributes.Sort((left, right) => string.CompareOrdinal(left.Input, right.Input));
        document.ForeignAttributes.Sort((left, right) => string.CompareOrdinal(left.Input, right.Input));
    }

    private static void ValidateUniqueStrings(string description, List<string> values)
    {
        if (values.Count == 0)
        {
            throw new InvalidDataException($"The {description} table is empty.");
        }

        HashSet<string> unique = new(StringComparer.OrdinalIgnoreCase);
        foreach (string value in values)
        {
            if (value.Length == 0 || !unique.Add(value))
            {
                throw new InvalidDataException($"The {description} table contains an empty or duplicate value.");
            }
        }
    }

    private static void ValidateMappings(string description, List<NameMappingPin> mappings)
    {
        if (mappings.Count == 0)
        {
            throw new InvalidDataException($"The {description} table is empty.");
        }

        HashSet<string> inputs = new(StringComparer.Ordinal);
        foreach (NameMappingPin mapping in mappings)
        {
            ValidateInputName(description, mapping.Input);
            if (mapping.Output.Length == 0 || !inputs.Add(mapping.Input))
            {
                throw new InvalidDataException($"The {description} table contains an empty output or duplicate input '{mapping.Input}'.");
            }
        }
    }

    private static void ValidateInputName(string description, string name)
    {
        if (name.Length == 0)
        {
            throw new InvalidDataException($"The {description} table contains an empty input name.");
        }

        foreach (char value in name)
        {
            if (!(value >= 'a' && value <= 'z') && !(value >= '0' && value <= '9') && value != ':' && value != '-')
            {
                throw new InvalidDataException($"The {description} table contains non-canonical input '{name}'.");
            }
        }
    }

    private static void ValidateHtmlNames(HtmlNamesDocument document)
    {
        if (document.SchemaVersion != 1)
        {
            throw new InvalidDataException($"Unsupported HTML names schema version {document.SchemaVersion}.");
        }

        if (document.Sets.Count == 0)
        {
            throw new InvalidDataException("The HTML names input contains no sets.");
        }

        HashSet<string> methods = new(StringComparer.Ordinal);
        foreach (HtmlNameSet set in document.Sets)
        {
            if (set.Method.Length < 3 || !set.Method.StartsWith("Is", StringComparison.Ordinal)
                || !char.IsAsciiLetterUpper(set.Method[2]))
            {
                throw new InvalidDataException($"HTML name-set method '{set.Method}' must start with 'Is' and an uppercase ASCII letter.");
            }

            foreach (char value in set.Method)
            {
                if (!char.IsAsciiLetterOrDigit(value))
                {
                    throw new InvalidDataException($"HTML name-set method '{set.Method}' is not a simple identifier.");
                }
            }

            if (!methods.Add(set.Method))
            {
                throw new InvalidDataException($"Duplicate HTML name-set method '{set.Method}'.");
            }

            HashSet<string> names = new(StringComparer.Ordinal);
            foreach (string name in set.Names)
            {
                if (name.Length == 0)
                {
                    throw new InvalidDataException($"HTML name set '{set.Method}' contains an empty name.");
                }

                foreach (char value in name)
                {
                    if (!(value >= 'a' && value <= 'z') && !(value >= '0' && value <= '9') && value != '-')
                    {
                        throw new InvalidDataException($"HTML name set '{set.Method}' contains non-canonical name '{name}'.");
                    }
                }

                if (!names.Add(name))
                {
                    throw new InvalidDataException($"HTML name set '{set.Method}' contains duplicate name '{name}'.");
                }
            }

            set.Names.Sort(StringComparer.Ordinal);
        }

        document.Sets.Sort((left, right) => string.CompareOrdinal(left.Method, right.Method));
    }

    private static void ValidateStandards(List<StandardPin> standards)
    {
        HashSet<string> ids = new(StringComparer.Ordinal);
        foreach (StandardPin standard in standards)
        {
            if (string.IsNullOrWhiteSpace(standard.Id) || string.IsNullOrWhiteSpace(standard.Url)
                || string.IsNullOrWhiteSpace(standard.Revision))
            {
                throw new InvalidDataException("Every standard pin requires an id, URL, and revision.");
            }

            if (!Uri.TryCreate(standard.Url, UriKind.Absolute, out Uri? uri)
                || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            {
                throw new InvalidDataException($"Standard pin '{standard.Id}' has an invalid source URL.");
            }

            if (!ids.Add(standard.Id))
            {
                throw new InvalidDataException($"Duplicate standard pin id '{standard.Id}'.");
            }
        }
    }

    private static string GenerateManifestSource(List<VerifiedInput> inputs, List<StandardPin> standards)
    {
        StringBuilder builder = new();
        builder.Append("// <auto-generated />\n");
        builder.Append("// Do not edit directly. Update pinned inputs and rerun the generator.\n");
        builder.Append("// Generator: tools/GenerateTables ").Append(GeneratorVersion).Append('\n');
        builder.Append("// Recreate: dotnet run --project tools/GenerateTables -- --write\n");
        foreach (VerifiedInput input in inputs)
        {
            builder.Append("// Input: ").Append(input.Input.Id).Append(" | ")
                .Append(NormalizePath(input.Input.Path)).Append(" | ")
                .Append(input.Input.Revision).Append(" | SHA-256 ").Append(input.Checksum).Append('\n');
        }

        foreach (StandardPin standard in standards)
        {
            builder.Append("// Standard: ").Append(standard.Id).Append(" | ")
                .Append(standard.Url).Append(" | ").Append(standard.Revision).Append('\n');
        }

        builder.Append("\nnamespace html;\n\n");
        builder.Append("static class GeneratedTableManifest\n{\n");
        builder.Append("    public static string GetGeneratorVersion()\n    {\n        return ")
            .Append(ToCSharpString(GeneratorVersion)).Append(";\n    }\n\n");

        AppendStringArrayMethod(builder, "GetInputIds", inputs.Select(input => input.Input.Id));
        AppendStringArrayMethod(builder, "GetInputChecksums", inputs.Select(input => input.Checksum));
        AppendStringArrayMethod(builder, "GetStandardIds", standards.Select(standard => standard.Id));
        AppendStringArrayMethod(builder, "GetStandardUrls", standards.Select(standard => standard.Url));
        AppendStringArrayMethod(builder, "GetStandardRevisions", standards.Select(standard => standard.Revision));
        builder.Append("}\n");
        return builder.ToString();
    }

    private static string GenerateHtmlNamesSource(List<VerifiedInput> inputs, List<HtmlNameSet> sets)
    {
        StringBuilder builder = new();
        builder.Append("// <auto-generated />\n");
        builder.Append("// Do not edit directly. Update pinned inputs and rerun the generator.\n");
        builder.Append("// Generator: tools/GenerateTables ").Append(GeneratorVersion).Append('\n');
        builder.Append("// Recreate: dotnet run --project tools/GenerateTables -- --write\n");
        foreach (VerifiedInput input in inputs)
        {
            builder.Append("// Input: ").Append(input.Input.Id).Append(" | ")
                .Append(NormalizePath(input.Input.Path)).Append(" | ")
                .Append(input.Input.Revision).Append(" | SHA-256 ").Append(input.Checksum).Append('\n');
        }

        builder.Append("\nusing System;\n\nnamespace html;\n\n");
        builder.Append("static class HtmlNames\n{\n");
        foreach (HtmlNameSet set in sets)
        {
            string field = char.ToLowerInvariant(set.Method[2]) + set.Method[3..] + "Names";
            builder.Append("    static readonly string[] ").Append(field).Append(" =\n    [\n");
            foreach (string name in set.Names)
            {
                builder.Append("        ").Append(ToCSharpString(name)).Append(",\n");
            }

            builder.Append("    ];\n\n");
        }

        foreach (HtmlNameSet set in sets)
        {
            string field = char.ToLowerInvariant(set.Method[2]) + set.Method[3..] + "Names";
            builder.Append("    public static bool ").Append(set.Method).Append("(ReadOnlySpan<char> name)\n")
                .Append("    {\n        return Contains(").Append(field).Append(", name);\n    }\n\n");
        }

        builder.Append("    static bool Contains(string[] names, ReadOnlySpan<char> name)\n")
            .Append("    {\n        int minimum = 0;\n        int maximum = names.Length - 1;\n")
            .Append("        while (minimum <= maximum)\n        {\n            int middle = minimum + (maximum - minimum) / 2;\n")
            .Append("            int comparison = Compare(name, names[middle]);\n            if (comparison == 0)\n            {\n                return true;\n            }\n\n")
            .Append("            if (comparison < 0)\n            {\n                maximum = middle - 1;\n            }\n            else\n            {\n                minimum = middle + 1;\n            }\n        }\n\n        return false;\n    }\n\n")
            .Append("    static int Compare(ReadOnlySpan<char> value, string name)\n    {\n        int count = value.Length < name.Length ? value.Length : name.Length;\n        int index = 0;\n")
            .Append("        while (index < count)\n        {\n            char left = Ascii.ToLower(value[index]);\n            char right = name[index];\n            if (left < right)\n            {\n                return -1;\n            }\n\n")
            .Append("            if (left > right)\n            {\n                return 1;\n            }\n\n            index = index + 1;\n        }\n\n")
            .Append("        if (value.Length < name.Length)\n        {\n            return -1;\n        }\n\n        if (value.Length > name.Length)\n        {\n            return 1;\n        }\n\n        return 0;\n    }\n");
        builder.Append("}\n");
        return builder.ToString();
    }

    private static string GenerateDoctypeSource(List<VerifiedInput> inputs, DoctypePins doctype)
    {
        StringBuilder builder = new();
        AppendGeneratedSourceHeader(builder, inputs);
        builder.Append("\nusing System;\n\nnamespace html;\n\n");
        builder.Append("static class HtmlDoctypeTables\n{\n");
        AppendSourceStringArray(builder, "quirksPublicExact", doctype.QuirksPublicExact);
        AppendSourceStringArray(builder, "quirksPublicPrefixes", doctype.QuirksPublicPrefixes);
        AppendSourceStringArray(builder, "html401PublicPrefixes", doctype.Html401PublicPrefixes);
        AppendSourceStringArray(builder, "limitedQuirksPublicPrefixes", doctype.LimitedQuirksPublicPrefixes);
        AppendSourceStringArray(builder, "quirksSystemExact", doctype.QuirksSystemExact);
        builder.Append("    public static bool IsQuirksPublicIdentifier(ReadOnlySpan<char> publicIdentifier, bool systemIdentifierMissingOrEmpty)\n")
            .Append("    {\n        if (EqualsAnyIgnoreCase(publicIdentifier, quirksPublicExact)\n")
            .Append("            || StartsWithAnyIgnoreCase(publicIdentifier, quirksPublicPrefixes))\n        {\n            return true;\n        }\n\n")
            .Append("        return systemIdentifierMissingOrEmpty\n            && StartsWithAnyIgnoreCase(publicIdentifier, html401PublicPrefixes);\n    }\n\n")
            .Append("    public static bool IsQuirksSystemIdentifier(ReadOnlySpan<char> systemIdentifier)\n")
            .Append("    {\n        return EqualsAnyIgnoreCase(systemIdentifier, quirksSystemExact);\n    }\n\n")
            .Append("    public static bool IsLimitedQuirksPublicIdentifier(ReadOnlySpan<char> publicIdentifier, bool hasNonEmptySystemIdentifier)\n")
            .Append("    {\n        if (StartsWithAnyIgnoreCase(publicIdentifier, limitedQuirksPublicPrefixes))\n        {\n            return true;\n        }\n\n")
            .Append("        return hasNonEmptySystemIdentifier\n            && StartsWithAnyIgnoreCase(publicIdentifier, html401PublicPrefixes);\n    }\n\n")
            .Append("    static bool EqualsAnyIgnoreCase(ReadOnlySpan<char> value, string[] candidates)\n")
            .Append("    {\n        int index = 0;\n        while (index < candidates.Length)\n        {\n            if (Ascii.EqualsIgnoreCase(value, candidates[index].AsSpan()))\n            {\n                return true;\n            }\n\n            index = index + 1;\n        }\n\n        return false;\n    }\n\n")
            .Append("    static bool StartsWithAnyIgnoreCase(ReadOnlySpan<char> value, string[] prefixes)\n")
            .Append("    {\n        int index = 0;\n        while (index < prefixes.Length)\n        {\n            string prefix = prefixes[index];\n            if (value.Length >= prefix.Length\n")
            .Append("                && Ascii.EqualsIgnoreCase(value.Slice(0, prefix.Length), prefix.AsSpan()))\n            {\n                return true;\n            }\n\n")
            .Append("            index = index + 1;\n        }\n\n        return false;\n    }\n");
        builder.Append("}\n");
        return builder.ToString();
    }

    private static string GenerateForeignNamesSource(List<VerifiedInput> inputs, DoctypeForeignDocument document)
    {
        StringBuilder builder = new();
        AppendGeneratedSourceHeader(builder, inputs);
        builder.Append("\nusing System;\n\nnamespace html;\n\n");
        builder.Append("class HtmlForeignAttributeAdjustment\n{\n")
            .Append("    bool found;\n    string prefix;\n    string localName;\n    string namespaceUri;\n\n")
            .Append("    public HtmlForeignAttributeAdjustment(bool foundValue, string prefixValue, string localNameValue, string namespaceUriValue)\n")
            .Append("    {\n        found = foundValue;\n        prefix = prefixValue;\n        localName = localNameValue;\n        namespaceUri = namespaceUriValue;\n    }\n\n")
            .Append("    public bool GetFound()\n    {\n        return found;\n    }\n\n")
            .Append("    public string GetPrefix()\n    {\n        return prefix;\n    }\n\n")
            .Append("    public string GetLocalName()\n    {\n        return localName;\n    }\n\n")
            .Append("    public string GetNamespaceUri()\n    {\n        return namespaceUri;\n    }\n")
            .Append("}\n\nstatic class HtmlForeignNames\n{\n");

        AppendMappingArrays(builder, "mathMlAttribute", document.MathMlAttributes);
        AppendMappingArrays(builder, "svgTagName", document.SvgTagNames);
        AppendMappingArrays(builder, "svgAttribute", document.SvgAttributes);
        AppendSourceStringArray(builder, "foreignAttributeInputs", document.ForeignAttributes.Select(value => value.Input));
        builder.Append("    static readonly HtmlForeignAttributeAdjustment[] foreignAttributeAdjustments =\n    [\n");
        foreach (ForeignAttributePin adjustment in document.ForeignAttributes)
        {
            builder.Append("        new HtmlForeignAttributeAdjustment(true, ").Append(ToCSharpString(adjustment.Prefix)).Append(", ")
                .Append(ToCSharpString(adjustment.LocalName)).Append(", ").Append(ToCSharpString(adjustment.NamespaceUri)).Append("),\n");
        }

        builder.Append("    ];\n\n")
            .Append("    static readonly HtmlForeignAttributeAdjustment missingForeignAttribute = new HtmlForeignAttributeAdjustment(false, \"\", \"\", \"\");\n\n")
            .Append("    public static string AdjustMathMlAttributeName(ReadOnlySpan<char> name)\n")
            .Append("    {\n        return AdjustName(mathMlAttributeInputs, mathMlAttributeOutputs, name);\n    }\n\n")
            .Append("    public static string AdjustSvgTagName(ReadOnlySpan<char> name)\n")
            .Append("    {\n        return AdjustName(svgTagNameInputs, svgTagNameOutputs, name);\n    }\n\n")
            .Append("    public static string AdjustSvgAttributeName(ReadOnlySpan<char> name)\n")
            .Append("    {\n        return AdjustName(svgAttributeInputs, svgAttributeOutputs, name);\n    }\n\n")
            .Append("    public static HtmlForeignAttributeAdjustment GetForeignAttributeAdjustment(ReadOnlySpan<char> name)\n")
            .Append("    {\n        int index = FindName(foreignAttributeInputs, name);\n        if (index < 0)\n        {\n            return missingForeignAttribute;\n        }\n\n")
            .Append("        return foreignAttributeAdjustments[index];\n    }\n\n")
            .Append("    static string AdjustName(string[] inputs, string[] outputs, ReadOnlySpan<char> name)\n")
            .Append("    {\n        int index = FindName(inputs, name);\n        if (index < 0)\n        {\n            return name.ToString();\n        }\n\n        return outputs[index];\n    }\n\n")
            .Append("    static int FindName(string[] inputs, ReadOnlySpan<char> name)\n")
            .Append("    {\n        int minimum = 0;\n        int maximum = inputs.Length - 1;\n        while (minimum <= maximum)\n        {\n")
            .Append("            int middle = minimum + (maximum - minimum) / 2;\n            int comparison = Compare(name, inputs[middle]);\n            if (comparison == 0)\n            {\n                return middle;\n            }\n\n")
            .Append("            if (comparison < 0)\n            {\n                maximum = middle - 1;\n            }\n            else\n            {\n                minimum = middle + 1;\n            }\n        }\n\n        return -1;\n    }\n\n")
            .Append("    static int Compare(ReadOnlySpan<char> value, string name)\n")
            .Append("    {\n        int count = value.Length < name.Length ? value.Length : name.Length;\n        int index = 0;\n        while (index < count)\n        {\n")
            .Append("            char left = Ascii.ToLower(value[index]);\n            char right = name[index];\n            if (left < right)\n            {\n                return -1;\n            }\n\n")
            .Append("            if (left > right)\n            {\n                return 1;\n            }\n\n            index = index + 1;\n        }\n\n")
            .Append("        if (value.Length < name.Length)\n        {\n            return -1;\n        }\n\n        if (value.Length > name.Length)\n        {\n            return 1;\n        }\n\n        return 0;\n    }\n");
        builder.Append("}\n");
        return builder.ToString();
    }

    private static string GenerateEncodingLabelsSource(List<VerifiedInput> inputs, EncodingLabelsDocument document)
    {
        List<EncodingLabelMapping> mappings = [];
        foreach (EncodingPin encoding in document.Encodings)
        {
            foreach (string label in encoding.Labels)
            {
                mappings.Add(new EncodingLabelMapping(label, encoding.Name));
            }
        }

        mappings.Sort((left, right) => string.CompareOrdinal(left.Label, right.Label));
        StringBuilder builder = new();
        AppendGeneratedSourceHeader(builder, inputs);
        builder.Append("\nusing System;\n\nnamespace html;\n\n");
        builder.Append("static class WebEncodingLabels\n{\n");
        AppendSourceStringArray(builder, "labels", mappings.Select(value => value.Label));
        AppendSourceStringArray(builder, "names", mappings.Select(value => value.Name));
        builder.Append("    public static string GetEncodingName(string label)\n")
            .Append("    {\n        if (label == null)\n        {\n            throw new ArgumentNullException(\"label\");\n        }\n\n")
            .Append("        return GetEncodingName(label.AsSpan());\n    }\n\n")
            .Append("    public static string GetEncodingName(ReadOnlySpan<char> label)\n")
            .Append("    {\n        int start = 0;\n        int end = label.Length;\n")
            .Append("        while (start < end && Ascii.IsHtmlWhitespace(label[start]))\n        {\n            start = start + 1;\n        }\n\n")
            .Append("        while (end > start && Ascii.IsHtmlWhitespace(label[end - 1]))\n        {\n            end = end - 1;\n        }\n\n")
            .Append("        ReadOnlySpan<char> normalized = label.Slice(start, end - start);\n        int minimum = 0;\n        int maximum = labels.Length - 1;\n")
            .Append("        while (minimum <= maximum)\n        {\n            int middle = minimum + (maximum - minimum) / 2;\n            int comparison = Compare(normalized, labels[middle]);\n")
            .Append("            if (comparison == 0)\n            {\n                return names[middle];\n            }\n\n")
            .Append("            if (comparison < 0)\n            {\n                maximum = middle - 1;\n            }\n            else\n            {\n                minimum = middle + 1;\n            }\n        }\n\n        return \"\";\n    }\n\n")
            .Append("    static int Compare(ReadOnlySpan<char> value, string label)\n    {\n        int count = value.Length < label.Length ? value.Length : label.Length;\n        int index = 0;\n")
            .Append("        while (index < count)\n        {\n            char left = Ascii.ToLower(value[index]);\n            char right = label[index];\n            if (left < right)\n            {\n                return -1;\n            }\n\n")
            .Append("            if (left > right)\n            {\n                return 1;\n            }\n\n            index = index + 1;\n        }\n\n")
            .Append("        if (value.Length < label.Length)\n        {\n            return -1;\n        }\n\n        if (value.Length > label.Length)\n        {\n            return 1;\n        }\n\n        return 0;\n    }\n");
        builder.Append("}\n");
        return builder.ToString();
    }

    private static string GenerateEncodingIndexesSource(List<VerifiedInput> inputs, JsonElement root)
    {
        StringBuilder builder = new();
        AppendGeneratedSourceHeader(builder, inputs);
        builder.Append("\nnamespace html;\n\nstatic class WebEncodingIndexes\n{\n");
        AppendIndexArray(builder, root, "big5", "big5");
        AppendIndexArray(builder, root, "euc-kr", "eucKr");
        AppendIndexArray(builder, root, "gb18030", "gb18030");
        AppendIndexArray(builder, root, "jis0208", "jis0208");
        AppendIndexArray(builder, root, "jis0212", "jis0212");
        AppendIndexArray(builder, root, "iso-2022-jp-katakana", "iso2022JpKatakana");
        string[] singleByteIndexes =
        [
            "ibm866", "iso-8859-2", "iso-8859-3", "iso-8859-4", "iso-8859-5", "iso-8859-6",
            "iso-8859-7", "iso-8859-8", "iso-8859-10", "iso-8859-13", "iso-8859-14",
            "iso-8859-15", "iso-8859-16", "koi8-r", "koi8-u", "macintosh", "windows-874",
            "windows-1250", "windows-1251", "windows-1252", "windows-1253", "windows-1254",
            "windows-1255", "windows-1256", "windows-1257", "windows-1258", "x-mac-cyrillic"
        ];
        foreach (string indexName in singleByteIndexes)
        {
            AppendIndexArray(builder, root, indexName, ToFieldName(indexName));
        }

        JsonElement ranges = root.GetProperty("gb18030-ranges");
        AppendRangeArray(builder, ranges, 0, "gb18030RangePointers");
        AppendRangeArray(builder, ranges, 1, "gb18030RangeCodePoints");
        builder.Append("    public static int GetBig5CodePoint(int pointer)\n    {\n        return GetCodePoint(big5, pointer);\n    }\n\n")
            .Append("    public static int GetEucKrCodePoint(int pointer)\n    {\n        return GetCodePoint(eucKr, pointer);\n    }\n\n")
            .Append("    public static int GetGb18030CodePoint(int pointer)\n    {\n        return GetCodePoint(gb18030, pointer);\n    }\n\n")
            .Append("    public static int GetJis0208CodePoint(int pointer)\n    {\n        return GetCodePoint(jis0208, pointer);\n    }\n\n")
            .Append("    public static int GetJis0212CodePoint(int pointer)\n    {\n        return GetCodePoint(jis0212, pointer);\n    }\n\n")
            .Append("    public static int GetIso2022JpKatakanaCodePoint(int pointer)\n    {\n        return GetCodePoint(iso2022JpKatakana, pointer);\n    }\n\n")
            .Append("    public static int GetSingleByteCodePoint(string encodingName, int pointer)\n    {\n");
        Dictionary<string, string> singleNames = new(StringComparer.Ordinal)
        {
            ["IBM866"] = "ibm866", ["ISO-8859-2"] = "iso-8859-2", ["ISO-8859-3"] = "iso-8859-3",
            ["ISO-8859-4"] = "iso-8859-4", ["ISO-8859-5"] = "iso-8859-5", ["ISO-8859-6"] = "iso-8859-6",
            ["ISO-8859-7"] = "iso-8859-7", ["ISO-8859-8"] = "iso-8859-8", ["ISO-8859-8-I"] = "iso-8859-8",
            ["ISO-8859-10"] = "iso-8859-10", ["ISO-8859-13"] = "iso-8859-13", ["ISO-8859-14"] = "iso-8859-14",
            ["ISO-8859-15"] = "iso-8859-15", ["ISO-8859-16"] = "iso-8859-16", ["KOI8-R"] = "koi8-r",
            ["KOI8-U"] = "koi8-u", ["macintosh"] = "macintosh", ["windows-874"] = "windows-874",
            ["windows-1250"] = "windows-1250", ["windows-1251"] = "windows-1251", ["windows-1252"] = "windows-1252",
            ["windows-1253"] = "windows-1253", ["windows-1254"] = "windows-1254", ["windows-1255"] = "windows-1255",
            ["windows-1256"] = "windows-1256", ["windows-1257"] = "windows-1257", ["windows-1258"] = "windows-1258",
            ["x-mac-cyrillic"] = "x-mac-cyrillic"
        };
        foreach (KeyValuePair<string, string> item in singleNames.OrderBy(value => value.Key, StringComparer.Ordinal))
        {
            builder.Append("        if (encodingName == ").Append(ToCSharpString(item.Key)).Append(")\n        {\n            return GetCodePoint(")
                .Append(ToFieldName(item.Value)).Append(", pointer);\n        }\n\n");
        }

        builder.Append("        return -2;\n    }\n\n")
            .Append("    public static int GetGb18030RangeCodePoint(int pointer)\n    {\n")
            .Append("        if (pointer < 0 || pointer > 1237575 || pointer > 39419 && pointer < 189000)\n        {\n            return -1;\n        }\n\n")
            .Append("        if (pointer == 7457)\n        {\n            return 0xE7C7;\n        }\n\n")
            .Append("        int minimum = 0;\n        int maximum = gb18030RangePointers.Length - 1;\n        while (minimum <= maximum)\n        {\n            int middle = minimum + (maximum - minimum) / 2;\n")
            .Append("            if (gb18030RangePointers[middle] <= pointer)\n            {\n                minimum = middle + 1;\n            }\n            else\n            {\n                maximum = middle - 1;\n            }\n        }\n\n")
            .Append("        if (maximum < 0)\n        {\n            return -1;\n        }\n\n")
            .Append("        return gb18030RangeCodePoints[maximum] + pointer - gb18030RangePointers[maximum];\n    }\n\n")
            .Append("    static int GetCodePoint(int[] index, int pointer)\n    {\n        if (pointer < 0 || pointer >= index.Length)\n        {\n            return -1;\n        }\n\n        return index[pointer];\n    }\n");
        builder.Append("}\n");
        return builder.ToString();
    }

    private static string GenerateNamedCharacterReferencesSource(List<VerifiedInput> inputs,
        List<NamedCharacterReferencePin> references)
    {
        List<NamedCharacterTrieNode> nodes = [new NamedCharacterTrieNode()];
        for (int referenceIndex = 0; referenceIndex < references.Count; referenceIndex++)
        {
            int nodeIndex = 0;
            foreach (char value in references[referenceIndex].Name)
            {
                if (!nodes[nodeIndex].Children.TryGetValue(value, out int childIndex))
                {
                    childIndex = nodes.Count;
                    nodes[nodeIndex].Children.Add(value, childIndex);
                    nodes.Add(new NamedCharacterTrieNode());
                }

                nodeIndex = childIndex;
            }

            if (nodes[nodeIndex].ValueIndex >= 0)
            {
                throw new InvalidDataException($"Duplicate trie terminal for '{references[referenceIndex].Name}'.");
            }

            nodes[nodeIndex].ValueIndex = referenceIndex;
        }

        List<int> nodeFirstEdges = [];
        List<int> nodeEdgeCounts = [];
        List<int> nodeValues = [];
        List<char> edgeCharacters = [];
        List<int> edgeTargets = [];
        foreach (NamedCharacterTrieNode node in nodes)
        {
            nodeFirstEdges.Add(edgeCharacters.Count);
            nodeEdgeCounts.Add(node.Children.Count);
            nodeValues.Add(node.ValueIndex);
            foreach (KeyValuePair<char, int> edge in node.Children)
            {
                edgeCharacters.Add(edge.Key);
                edgeTargets.Add(edge.Value);
            }
        }

        Dictionary<(int First, int Second), NamedCharacterReferencePin> preferredByValue = [];
        foreach (NamedCharacterReferencePin reference in references)
        {
            if (!reference.Name.EndsWith(';'))
            {
                continue;
            }

            (int First, int Second) key = (reference.FirstCodePoint, reference.SecondCodePoint);
            if (!preferredByValue.TryGetValue(key, out NamedCharacterReferencePin? current)
                || IsPreferredReferenceName(reference.Name, current.Name))
            {
                preferredByValue[key] = reference;
            }
        }

        List<NamedCharacterReferencePin> preferred = preferredByValue.Values.ToList();
        preferred.Sort((left, right) =>
        {
            int first = left.FirstCodePoint.CompareTo(right.FirstCodePoint);
            return first != 0 ? first : left.SecondCodePoint.CompareTo(right.SecondCodePoint);
        });

        StringBuilder builder = new();
        AppendGeneratedSourceHeader(builder, inputs);
        builder.Append("\nusing System;\n\nnamespace html;\n\n")
            .Append("class HtmlNamedCharacterReferenceMatch\n{\n")
            .Append("    bool matched;\n    int consumedCharacters;\n    int firstCodePoint;\n    int secondCodePoint;\n    bool hasSemicolon;\n\n")
            .Append("    public HtmlNamedCharacterReferenceMatch(bool matched, int consumedCharacters, int firstCodePoint, int secondCodePoint, bool hasSemicolon)\n")
            .Append("    {\n        this.matched = matched;\n        this.consumedCharacters = consumedCharacters;\n")
            .Append("        this.firstCodePoint = firstCodePoint;\n        this.secondCodePoint = secondCodePoint;\n        this.hasSemicolon = hasSemicolon;\n    }\n\n")
            .Append("    public bool IsMatch()\n    {\n        return this.matched;\n    }\n\n")
            .Append("    public int GetConsumedCharacters()\n    {\n        return this.consumedCharacters;\n    }\n\n")
            .Append("    public int GetFirstCodePoint()\n    {\n        return this.firstCodePoint;\n    }\n\n")
            .Append("    public bool HasSecondCodePoint()\n    {\n        return this.secondCodePoint >= 0;\n    }\n\n")
            .Append("    public int GetSecondCodePoint()\n    {\n        return this.secondCodePoint;\n    }\n\n")
            .Append("    public bool HasSemicolon()\n    {\n        return this.hasSemicolon;\n    }\n")
            .Append("}\n\nstatic class HtmlNamedCharacterReferences\n{\n");
        AppendSourceIntArray(builder, "nodeFirstEdges", nodeFirstEdges);
        AppendSourceIntArray(builder, "nodeEdgeCounts", nodeEdgeCounts);
        AppendSourceIntArray(builder, "nodeValues", nodeValues);
        AppendSourceCharArray(builder, "edgeCharacters", edgeCharacters);
        AppendSourceIntArray(builder, "edgeTargets", edgeTargets);
        AppendSourceStringArray(builder, "names", references.Select(value => value.Name));
        AppendSourceIntArray(builder, "firstCodePoints", references.Select(value => value.FirstCodePoint));
        AppendSourceIntArray(builder, "secondCodePoints", references.Select(value => value.SecondCodePoint));
        AppendSourceIntArrayClean(builder, "preferredFirstCodePoints", preferred.Select(value => value.FirstCodePoint));
        AppendSourceIntArrayClean(builder, "preferredSecondCodePoints", preferred.Select(value => value.SecondCodePoint));
        AppendSourceStringArray(builder, "preferredNames", preferred.Select(value => value.Name));
        builder.Append("    public static int GetCount()\n    {\n        return names.Length;\n    }\n\n")
            .Append("    public static int GetLegacySemicolonlessCount()\n    {\n        return 106;\n    }\n\n")
            .Append("    public static int GetTwoCodePointCount()\n    {\n        return 93;\n    }\n\n")
            .Append("    public static string GetName(int index)\n    {\n        ValidateIndex(index);\n        return names[index];\n    }\n\n")
            .Append("    public static int GetFirstCodePoint(int index)\n    {\n        ValidateIndex(index);\n        return firstCodePoints[index];\n    }\n\n")
            .Append("    public static int GetSecondCodePoint(int index)\n    {\n        ValidateIndex(index);\n        return secondCodePoints[index];\n    }\n\n")
            .Append("    public static string GetPreferredName(int firstCodePoint, int secondCodePoint)\n")
            .Append("    {\n        int minimum = 0;\n        int maximum = preferredNames.Length - 1;\n        while (minimum <= maximum)\n        {\n")
            .Append("            int middle = minimum + (maximum - minimum) / 2;\n            int storedFirst = preferredFirstCodePoints[middle];\n")
            .Append("            int storedSecond = preferredSecondCodePoints[middle];\n            if (firstCodePoint == storedFirst && secondCodePoint == storedSecond)\n")
            .Append("            {\n                return preferredNames[middle];\n            }\n\n")
            .Append("            if (firstCodePoint < storedFirst || firstCodePoint == storedFirst && secondCodePoint < storedSecond)\n")
            .Append("            {\n                maximum = middle - 1;\n            }\n            else\n            {\n                minimum = middle + 1;\n            }\n        }\n\n")
            .Append("        return \"\";\n    }\n\n")
            .Append("    public static HtmlNamedCharacterReferenceMatch FindLongest(string input)\n")
            .Append("    {\n        if (input == null)\n        {\n            throw new ArgumentNullException(\"input\");\n        }\n\n        return FindLongest(input.AsSpan());\n    }\n\n")
            .Append("    public static HtmlNamedCharacterReferenceMatch FindLongest(ReadOnlySpan<char> input)\n")
            .Append("    {\n        int nodeIndex = 0;\n        int position = 0;\n        int matchedValue = -1;\n        int matchedLength = 0;\n")
            .Append("        while (position < input.Length)\n        {\n            int edgeIndex = FindEdge(nodeIndex, input[position]);\n")
            .Append("            if (edgeIndex < 0)\n            {\n                break;\n            }\n\n            nodeIndex = edgeTargets[edgeIndex];\n")
            .Append("            position = position + 1;\n            int valueIndex = nodeValues[nodeIndex];\n            if (valueIndex >= 0)\n")
            .Append("            {\n                matchedValue = valueIndex;\n                matchedLength = position;\n            }\n        }\n\n")
            .Append("        if (matchedValue < 0)\n        {\n            return new HtmlNamedCharacterReferenceMatch(false, 0, -1, -1, false);\n        }\n\n")
            .Append("        bool hasSemicolon = input[matchedLength - 1] == ';';\n        return new HtmlNamedCharacterReferenceMatch(true, matchedLength,\n")
            .Append("            firstCodePoints[matchedValue], secondCodePoints[matchedValue], hasSemicolon);\n    }\n\n")
            .Append("    static int FindEdge(int nodeIndex, char value)\n    {\n        int minimum = nodeFirstEdges[nodeIndex];\n")
            .Append("        int maximum = minimum + nodeEdgeCounts[nodeIndex] - 1;\n        while (minimum <= maximum)\n        {\n")
            .Append("            int middle = minimum + (maximum - minimum) / 2;\n            char edge = edgeCharacters[middle];\n")
            .Append("            if (value == edge)\n            {\n                return middle;\n            }\n\n")
            .Append("            if (value < edge)\n            {\n                maximum = middle - 1;\n            }\n            else\n            {\n                minimum = middle + 1;\n            }\n        }\n\n")
            .Append("        return -1;\n    }\n\n")
            .Append("    static void ValidateIndex(int index)\n    {\n        if (index < 0 || index >= names.Length)\n")
            .Append("        {\n            throw new ArgumentOutOfRangeException(\"index\", index, \"The named character reference index is outside the table.\");\n        }\n    }\n")
            .Append("}\n");
        return builder.ToString();
    }

    private static string GenerateCssSafeDataSource(List<VerifiedInput> inputs,
        CssSafeDataDocument document)
    {
        StringBuilder builder = new();
        AppendGeneratedSourceHeader(builder, inputs);
        foreach (CssSourcePin source in document.Sources)
        {
            builder.Append("// Source: ").Append(source.Url).Append(" | ")
                .Append(source.Revision).Append('\n');
        }

        foreach (string rationale in document.SecurityRationales)
        {
            builder.Append("// Security: ").Append(rationale).Append('\n');
        }

        builder.Append("\nusing System;\n\nnamespace html;\n\n");
        AppendCssEnum(builder, "CssSafePropertyProfile", document.PropertyProfiles);
        AppendCssEnum(builder, "CssSafeFunctionProfile", document.FunctionProfiles);
        AppendCssEnum(builder, "CssSafeUnitKind", document.UnitKinds);
        builder.Append("static class CssSafeData\n{\n");
        AppendSourceStringArray(builder, "propertyNames", document.Properties.Select(value => value.Name));
        AppendCssProfileArray(builder, "CssSafePropertyProfile", "propertyProfiles",
            document.Properties.Select(value => value.Profile));
        AppendSourceStringArray(builder, "functionNames", document.Functions.Select(value => value.Name));
        AppendCssProfileArray(builder, "CssSafeFunctionProfile", "functionProfiles",
            document.Functions.Select(value => value.Profile));
        AppendSourceStringArray(builder, "keywords", document.Keywords);
        AppendSourceStringArray(builder, "unitNames", document.Units.Select(value => value.Name));
        AppendCssProfileArray(builder, "CssSafeUnitKind", "unitKinds",
            document.Units.Select(value => value.GetProfile()));
        AppendSourceStringArray(builder, "colorNames", document.Colors);
        AppendSourceStringArray(builder, "excludedPropertyNames", document.ExcludedProperties);
        AppendSourceStringArray(builder, "excludedFunctionNames", document.ExcludedFunctions);
        AppendSourceStringArray(builder, "excludedUnitNames", document.ExcludedUnits);
        AppendSourceStringArray(builder, "securityRationales", document.SecurityRationales);
        builder.Append("    public static CssSafePropertyProfile GetPropertyProfile(ReadOnlySpan<char> name)\n")
            .Append("    {\n        int index = Find(propertyNames, name);\n        return index < 0\n")
            .Append("            ? CssSafePropertyProfile.Unknown : propertyProfiles[index];\n    }\n\n")
            .Append("    public static CssSafeFunctionProfile GetFunctionProfile(ReadOnlySpan<char> name)\n")
            .Append("    {\n        int index = Find(functionNames, name);\n        return index < 0\n")
            .Append("            ? CssSafeFunctionProfile.Unknown : functionProfiles[index];\n    }\n\n")
            .Append("    public static bool IsKeyword(ReadOnlySpan<char> name)\n")
            .Append("    {\n        return Find(keywords, name) >= 0;\n    }\n\n")
            .Append("    public static CssSafeUnitKind GetUnitKind(ReadOnlySpan<char> name)\n")
            .Append("    {\n        int index = Find(unitNames, name);\n        return index < 0\n")
            .Append("            ? CssSafeUnitKind.Unknown : unitKinds[index];\n    }\n\n")
            .Append("    public static bool IsColorKeyword(ReadOnlySpan<char> name)\n")
            .Append("    {\n        return Find(colorNames, name) >= 0;\n    }\n\n")
            .Append("    public static bool IsExplicitlyExcludedProperty(ReadOnlySpan<char> name)\n")
            .Append("    {\n        return Find(excludedPropertyNames, name) >= 0;\n    }\n\n")
            .Append("    public static bool IsExplicitlyExcludedFunction(ReadOnlySpan<char> name)\n")
            .Append("    {\n        return Find(excludedFunctionNames, name) >= 0;\n    }\n\n")
            .Append("    public static bool IsExplicitlyExcludedUnit(ReadOnlySpan<char> name)\n")
            .Append("    {\n        return Find(excludedUnitNames, name) >= 0;\n    }\n\n")
            .Append("    public static string[] GetPropertyNames()\n    {\n        return Copy(propertyNames);\n    }\n\n")
            .Append("    public static string[] GetFunctionNames()\n    {\n        return Copy(functionNames);\n    }\n\n")
            .Append("    public static string[] GetKeywords()\n    {\n        return Copy(keywords);\n    }\n\n")
            .Append("    public static string[] GetUnitNames()\n    {\n        return Copy(unitNames);\n    }\n\n")
            .Append("    public static string[] GetColorNames()\n    {\n        return Copy(colorNames);\n    }\n\n")
            .Append("    public static string[] GetSecurityRationales()\n    {\n        return Copy(securityRationales);\n    }\n\n")
            .Append("    static int Find(string[] values, ReadOnlySpan<char> value)\n")
            .Append("    {\n        int minimum = 0;\n        int maximum = values.Length - 1;\n")
            .Append("        while (minimum <= maximum)\n        {\n            int middle = minimum + (maximum - minimum) / 2;\n")
            .Append("            int comparison = Compare(value, values[middle]);\n            if (comparison == 0)\n")
            .Append("            {\n                return middle;\n            }\n\n            if (comparison < 0)\n")
            .Append("            {\n                maximum = middle - 1;\n            }\n            else\n")
            .Append("            {\n                minimum = middle + 1;\n            }\n        }\n\n        return -1;\n    }\n\n")
            .Append("    static int Compare(ReadOnlySpan<char> value, string candidate)\n")
            .Append("    {\n        int count = value.Length < candidate.Length ? value.Length : candidate.Length;\n        int index = 0;\n")
            .Append("        while (index < count)\n        {\n            char left = Ascii.ToLower(value[index]);\n")
            .Append("            char right = candidate[index];\n            if (left < right)\n            {\n                return -1;\n            }\n\n")
            .Append("            if (left > right)\n            {\n                return 1;\n            }\n\n            index = index + 1;\n        }\n\n")
            .Append("        if (value.Length < candidate.Length)\n        {\n            return -1;\n        }\n\n")
            .Append("        if (value.Length > candidate.Length)\n        {\n            return 1;\n        }\n\n        return 0;\n    }\n\n")
            .Append("    static string[] Copy(string[] values)\n    {\n        string[] result = new string[values.Length];\n")
            .Append("        Array.Copy(values, result, values.Length);\n        return result;\n    }\n")
            .Append("}\n");
        return builder.ToString();
    }

    private static void AppendCssEnum(StringBuilder builder, string name,
        List<string> values)
    {
        builder.Append("enum ").Append(name).Append("\n{\n    Unknown = 0,\n");
        for (int index = 0; index < values.Count; index++)
        {
            builder.Append("    ").Append(values[index]).Append(" = ")
                .Append((index + 1).ToString(CultureInfo.InvariantCulture)).Append(",\n");
        }

        builder.Append("}\n\n");
    }

    private static void AppendCssProfileArray(StringBuilder builder,
        string type, string name, IEnumerable<string> values)
    {
        builder.Append("    static readonly ").Append(type).Append("[] ").Append(name)
            .Append(" =\n    [\n");
        foreach (string value in values)
        {
            builder.Append("        ").Append(type).Append('.').Append(value).Append(",\n");
        }

        builder.Append("    ];\n\n");
    }

    private static bool IsPreferredReferenceName(string candidate, string current)
    {
        if (candidate.Length != current.Length)
        {
            return candidate.Length < current.Length;
        }

        int candidateUppercase = candidate.Count(value => value >= 'A' && value <= 'Z');
        int currentUppercase = current.Count(value => value >= 'A' && value <= 'Z');
        if (candidateUppercase != currentUppercase)
        {
            return candidateUppercase < currentUppercase;
        }

        return string.CompareOrdinal(candidate, current) < 0;
    }

    private static void AppendIndexArray(StringBuilder builder, JsonElement root, string propertyName, string fieldName)
    {
        JsonElement values = root.GetProperty(propertyName);
        builder.Append("    static readonly int[] ").Append(fieldName).Append(" =\n    [\n");
        int column = 0;
        foreach (JsonElement value in values.EnumerateArray())
        {
            if (column == 0)
            {
                builder.Append("        ");
            }

            builder.Append(value.ValueKind == JsonValueKind.Null ? "-1" : value.GetInt32().ToString(CultureInfo.InvariantCulture)).Append(", ");
            column++;
            if (column == 16)
            {
                builder.Append('\n');
                column = 0;
            }
        }

        if (column != 0)
        {
            builder.Append('\n');
        }

        builder.Append("    ];\n\n");
    }

    private static void AppendSourceIntArray(StringBuilder builder, string name, IEnumerable<int> values)
    {
        builder.Append("    static readonly int[] ").Append(name).Append(" =\n    [\n        ");
        int column = 0;
        foreach (int value in values)
        {
            builder.Append(value.ToString(CultureInfo.InvariantCulture)).Append(", ");
            column++;
            if (column == 16)
            {
                builder.Append("\n        ");
                column = 0;
            }
        }

        builder.Append("\n    ];\n\n");
    }

    private static void AppendSourceIntArrayClean(StringBuilder builder, string name, IEnumerable<int> values)
    {
        int[] items = values.ToArray();
        builder.Append("    static readonly int[] ").Append(name).Append(" =\n    [\n");
        for (int index = 0; index < items.Length; index++)
        {
            if (index % 16 == 0)
            {
                builder.Append("        ");
            }

            builder.Append(items[index].ToString(CultureInfo.InvariantCulture)).Append(',');
            if (index % 16 == 15 || index + 1 == items.Length)
            {
                builder.Append('\n');
            }
            else
            {
                builder.Append(' ');
            }
        }

        builder.Append("    ];\n\n");
    }

    private static void AppendSourceCharArray(StringBuilder builder, string name, IEnumerable<char> values)
    {
        builder.Append("    static readonly char[] ").Append(name).Append(" =\n    [\n        ");
        int column = 0;
        foreach (char value in values)
        {
            builder.Append("(char)").Append(((int)value).ToString(CultureInfo.InvariantCulture)).Append(", ");
            column++;
            if (column == 16)
            {
                builder.Append("\n        ");
                column = 0;
            }
        }

        builder.Append("\n    ];\n\n");
    }

    private static void AppendRangeArray(StringBuilder builder, JsonElement ranges, int component, string fieldName)
    {
        builder.Append("    static readonly int[] ").Append(fieldName).Append(" =\n    [\n        ");
        int column = 0;
        foreach (JsonElement range in ranges.EnumerateArray())
        {
            builder.Append(range[component].GetInt32().ToString(CultureInfo.InvariantCulture)).Append(", ");
            column++;
            if (column == 16)
            {
                builder.Append("\n        ");
                column = 0;
            }
        }

        builder.Append("\n    ];\n\n");
    }

    private static string ToFieldName(string name)
    {
        StringBuilder builder = new();
        bool uppercase = false;
        foreach (char value in name)
        {
            if (value == '-')
            {
                uppercase = true;
            }
            else if (uppercase)
            {
                builder.Append(char.ToUpperInvariant(value));
                uppercase = false;
            }
            else
            {
                builder.Append(value);
            }
        }

        return builder.ToString();
    }

    private static void AppendGeneratedSourceHeader(StringBuilder builder, List<VerifiedInput> inputs)
    {
        builder.Append("// <auto-generated />\n");
        builder.Append("// Do not edit directly. Update pinned inputs and rerun the generator.\n");
        builder.Append("// Generator: tools/GenerateTables ").Append(GeneratorVersion).Append('\n');
        builder.Append("// Recreate: dotnet run --project tools/GenerateTables -- --write\n");
        foreach (VerifiedInput input in inputs)
        {
            builder.Append("// Input: ").Append(input.Input.Id).Append(" | ")
                .Append(NormalizePath(input.Input.Path)).Append(" | ")
                .Append(input.Input.Revision).Append(" | SHA-256 ").Append(input.Checksum).Append('\n');
        }
    }

    private static void AppendSourceStringArray(StringBuilder builder, string name, IEnumerable<string> values)
    {
        builder.Append("    static readonly string[] ").Append(name).Append(" =\n    [\n");
        foreach (string value in values)
        {
            builder.Append("        ").Append(ToCSharpString(value)).Append(",\n");
        }

        builder.Append("    ];\n\n");
    }

    private static void AppendMappingArrays(StringBuilder builder, string name, List<NameMappingPin> mappings)
    {
        AppendSourceStringArray(builder, name + "Inputs", mappings.Select(value => value.Input));
        AppendSourceStringArray(builder, name + "Outputs", mappings.Select(value => value.Output));
    }

    private static void AppendStringArrayMethod(StringBuilder builder, string name, IEnumerable<string> values)
    {
        builder.Append("    public static string[] ").Append(name).Append("()\n    {\n        return\n        [\n");
        foreach (string value in values)
        {
            builder.Append("            ").Append(ToCSharpString(value)).Append(",\n");
        }

        builder.Append("        ];\n    }\n\n");
    }

    private static string ToCSharpString(string value)
    {
        StringBuilder builder = new(value.Length + 2);
        builder.Append('"');
        foreach (char character in value)
        {
            switch (character)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    if (character < 32)
                    {
                        builder.Append("\\u").Append(((int)character).ToString("X4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append(character);
                    }
                    break;
            }
        }

        builder.Append('"');
        return builder.ToString();
    }

    private static T ReadJson<T>(string path)
    {
        JsonSerializerOptions options = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Disallow,
            AllowTrailingCommas = false
        };
        string json = File.ReadAllText(path, Encoding.UTF8);
        return JsonSerializer.Deserialize<T>(json, options)
            ?? throw new InvalidDataException($"JSON input '{path}' is empty.");
    }

    private static JsonDocument ReadCompressedJsonDocument(string path)
    {
        byte[] compressed = Convert.FromBase64String(File.ReadAllText(path, Encoding.ASCII));
        MemoryStream memory = new(compressed, false);
        GZipStream gzip = new(memory, CompressionMode.Decompress, false);
        return JsonDocument.Parse(gzip);
    }

    private static string ResolveWithinRoot(string root, string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
        {
            throw new InvalidDataException($"Generator path must be repository-relative: {relativePath}");
        }

        string fullRoot = Path.GetFullPath(root);
        string fullPath = Path.GetFullPath(Path.Combine(fullRoot, relativePath));
        string rootPrefix = fullRoot.EndsWith(Path.DirectorySeparatorChar)
            ? fullRoot
            : fullRoot + Path.DirectorySeparatorChar;
        StringComparison comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if (!fullPath.StartsWith(rootPrefix, comparison))
        {
            throw new InvalidDataException($"Generator path escapes the repository root: {relativePath}");
        }

        return fullPath;
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(Environment.CurrentDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "html.csproj"))
                && File.Exists(Path.Combine(directory.FullName, "tools", "GenerateTables", "inputs", "manifest.json")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the UniversalHtml repository root. Pass --root <path>.");
    }

    private static void WriteAtomically(string outputPath, string content)
    {
        string? directory = Path.GetDirectoryName(outputPath);
        if (directory == null)
        {
            throw new InvalidDataException($"Generated output has no parent directory: {outputPath}");
        }

        Directory.CreateDirectory(directory);
        string temporaryPath = outputPath + $".tmp-{Environment.ProcessId}";
        try
        {
            File.WriteAllText(temporaryPath, content, new UTF8Encoding(false));
            File.Move(temporaryPath, outputPath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static string ToRepositoryPath(string root, string path)
    {
        return NormalizePath(Path.GetRelativePath(root, path));
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private static void WriteHelp()
    {
        Console.WriteLine("Generate committed UniversalHtml lookup sources from pinned local inputs.");
        Console.WriteLine();
        Console.WriteLine("Usage: dotnet run --project tools/GenerateTables -- [--check|--write|--verify] [--root <path>]");
        Console.WriteLine("  --check   Verify checksums and fail if generated output differs (default).");
        Console.WriteLine("  --write   Verify checksums and atomically update generated output.");
        Console.WriteLine("  --verify  Verify pinned input checksums without checking output.");
    }
}

internal sealed class GeneratorArguments
{
    public string Mode { get; private set; } = "check";
    public string? Root { get; private set; }
    public bool ShowHelp { get; private set; }

    public static GeneratorArguments Parse(string[] args)
    {
        GeneratorArguments result = new();
        bool modeWasSet = false;
        for (int index = 0; index < args.Length; index++)
        {
            string argument = args[index];
            if (argument is "--check" or "--write" or "--verify")
            {
                if (modeWasSet)
                {
                    throw new GeneratorUsageException("Specify only one generator mode.");
                }

                result.Mode = argument[2..];
                modeWasSet = true;
            }
            else if (argument == "--root")
            {
                index++;
                if (index >= args.Length)
                {
                    throw new GeneratorUsageException("--root requires a path.");
                }

                result.Root = args[index];
            }
            else if (argument is "--help" or "-h")
            {
                result.ShowHelp = true;
            }
            else
            {
                throw new GeneratorUsageException($"Unknown argument '{argument}'.");
            }
        }

        return result;
    }
}

internal sealed class GeneratorManifest
{
    public int SchemaVersion { get; set; }
    public string GeneratorVersion { get; set; } = "";
    public List<GeneratorInput> Inputs { get; set; } = [];
    public string ManifestOutput { get; set; } = "";
    public string HtmlNamesOutput { get; set; } = "";
    public string DoctypeOutput { get; set; } = "";
    public string ForeignNamesOutput { get; set; } = "";
    public string EncodingLabelsOutput { get; set; } = "";
    public string EncodingIndexesOutput { get; set; } = "";
    public string HtmlEntitiesOutput { get; set; } = "";
    public string CssSafeDataOutput { get; set; } = "";
}

internal sealed class CssSafeDataDocument
{
    public int SchemaVersion { get; set; }
    public List<CssSourcePin> Sources { get; set; } = [];
    public List<string> SecurityRationales { get; set; } = [];
    public List<string> PropertyProfiles { get; set; } = [];
    public List<string> FunctionProfiles { get; set; } = [];
    public List<string> UnitKinds { get; set; } = [];
    public List<CssNamedProfilePin> Properties { get; set; } = [];
    public List<CssNamedProfilePin> Functions { get; set; } = [];
    public List<string> Keywords { get; set; } = [];
    public List<CssNamedProfilePin> Units { get; set; } = [];
    public List<string> Colors { get; set; } = [];
    public List<string> ExcludedProperties { get; set; } = [];
    public List<string> ExcludedFunctions { get; set; } = [];
    public List<string> ExcludedUnits { get; set; } = [];
}

internal sealed class CssSourcePin
{
    public string Url { get; set; } = "";
    public string Revision { get; set; } = "";
}

internal sealed class CssNamedProfilePin
{
    public string Name { get; set; } = "";
    public string Profile { get; set; } = "";
    public string Kind { get; set; } = "";

    public string GetProfile()
    {
        return Profile.Length == 0 ? Kind : Profile;
    }
}

internal sealed class NamedCharacterReferencePin
{
    public string Name { get; }
    public int FirstCodePoint { get; }
    public int SecondCodePoint { get; }

    public NamedCharacterReferencePin(string name, int firstCodePoint, int secondCodePoint)
    {
        Name = name;
        FirstCodePoint = firstCodePoint;
        SecondCodePoint = secondCodePoint;
    }
}

internal sealed class NamedCharacterTrieNode
{
    public SortedDictionary<char, int> Children { get; } = new();
    public int ValueIndex { get; set; } = -1;
}

internal sealed class EncodingLabelsDocument
{
    public int SchemaVersion { get; set; }
    public List<EncodingPin> Encodings { get; set; } = [];
}

internal sealed class EncodingPin
{
    public string Name { get; set; } = "";
    public List<string> Labels { get; set; } = [];
}

internal sealed class EncodingLabelMapping
{
    public string Label { get; }
    public string Name { get; }

    public EncodingLabelMapping(string label, string name)
    {
        Label = label;
        Name = name;
    }
}

internal sealed class DoctypeForeignDocument
{
    public int SchemaVersion { get; set; }
    public DoctypePins Doctype { get; set; } = new();
    public List<NameMappingPin> MathMlAttributes { get; set; } = [];
    public List<NameMappingPin> SvgTagNames { get; set; } = [];
    public List<NameMappingPin> SvgAttributes { get; set; } = [];
    public List<ForeignAttributePin> ForeignAttributes { get; set; } = [];
}

internal sealed class DoctypePins
{
    public List<string> QuirksPublicExact { get; set; } = [];
    public List<string> QuirksPublicPrefixes { get; set; } = [];
    public List<string> Html401PublicPrefixes { get; set; } = [];
    public List<string> LimitedQuirksPublicPrefixes { get; set; } = [];
    public List<string> QuirksSystemExact { get; set; } = [];
}

internal sealed class NameMappingPin
{
    public string Input { get; set; } = "";
    public string Output { get; set; } = "";
}

internal sealed class ForeignAttributePin
{
    public string Input { get; set; } = "";
    public string Prefix { get; set; } = "";
    public string LocalName { get; set; } = "";
    public string NamespaceUri { get; set; } = "";
}

internal sealed class HtmlNamesDocument
{
    public int SchemaVersion { get; set; }
    public List<HtmlNameSet> Sets { get; set; } = [];
}

internal sealed class HtmlNameSet
{
    public string Method { get; set; } = "";
    public List<string> Names { get; set; } = [];
}

internal sealed class GeneratedOutput
{
    public string AbsolutePath { get; }
    public string Content { get; }

    public GeneratedOutput(string absolutePath, string content)
    {
        AbsolutePath = absolutePath;
        Content = content;
    }
}

internal sealed class GeneratorInput
{
    public string Id { get; set; } = "";
    public string Path { get; set; } = "";
    public string Revision { get; set; } = "";
    public string Sha256 { get; set; } = "";
}

internal sealed class VerifiedInput
{
    public GeneratorInput Input { get; }
    public string AbsolutePath { get; }
    public string Checksum { get; }

    public VerifiedInput(GeneratorInput input, string absolutePath, string checksum)
    {
        Input = input;
        AbsolutePath = absolutePath;
        Checksum = checksum;
    }
}

internal sealed class StandardsDocument
{
    public int SchemaVersion { get; set; }
    public List<StandardPin> Standards { get; set; } = [];
}

internal sealed class StandardPin
{
    public string Id { get; set; } = "";
    public string Url { get; set; } = "";
    public string Revision { get; set; } = "";
}

internal sealed class GeneratorUsageException : Exception
{
    public GeneratorUsageException(string message)
        : base(message)
    {
    }
}
