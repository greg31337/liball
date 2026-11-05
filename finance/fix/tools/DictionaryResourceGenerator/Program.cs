using System.Security.Cryptography;
using fix;

string repositoryRoot = args.Length == 0 ? Directory.GetCurrentDirectory() : Path.GetFullPath(args[0]);
string outputDirectory = Path.Combine(repositoryRoot, "src", "Resources", "Dictionaries");
Directory.CreateDirectory(outputDirectory);

ResourceSource[] sources =
[
    new("FIX40", "specs/fix/quickfix-dictionaries/FIX40.xml", "2", false, 0, false),
    new("FIX41", "specs/fix/quickfix-dictionaries/FIX41.xml", "3", false, 0, false),
    new("FIX42", "specs/fix/quickfix-dictionaries/FIX42.xml", "4", false, 0, false),
    new("FIX43", "specs/fix/quickfix-dictionaries/FIX43.xml", "5", false, 0, false),
    new("FIX44", "specs/fix/quickfix-dictionaries/FIX44.xml", "6", false, 0, false),
    new("FIXT11", "specs/fix/quickfix-dictionaries/FIXT11.xml", "", false, 0, false),
    new("FIX50", "specs/fix/quickfix-dictionaries/FIX50.xml", "7", false, 0, true),
    new("FIX50SP1", "specs/fix/quickfix-dictionaries/FIX50SP1.xml", "8", false, 0, true),
    new("FIX50SP2", "specs/fix/quickfix-dictionaries/FIX50SP2.xml", "9", false, 0, true),
    new("FIXLatestEP309", "specs/fix/FIXOrchestra_FIX.Latest_EP309/FIXOrchestra_FIX.Latest_EP309.xml", "10", true, 309, true)
];

List<string> manifestLines = ["resource\tsource\tname\tbeginString\tapplVerID\textensionPack\tdatatypes\tcodeSets\tcodes\tfields\tcomponents\tgroups\tmessages\tscenarios\tdefinitions\tsha256"];
foreach (ResourceSource source in sources)
{
    string inputPath = Path.Combine(repositoryRoot, source.RelativeSource.Replace('/', Path.DirectorySeparatorChar));
    if (!File.Exists(inputPath))
    {
        throw new FileNotFoundException("Dictionary source not found.", inputPath);
    }

    DictionaryLoadResult loadResult;
    using (FileStream input = File.OpenRead(inputPath))
    {
        IDictionaryLoader loader = source.IsOrchestra ? new OrchestraDictionaryLoader() : new QuickFixDictionaryLoader();
        FixDictionaryOptions options = new FixDictionaryOptions();
        options.AllowDuplicateDefinitions = true;
        loadResult = loader.LoadDictionary(input, options);
    }
    if (!loadResult.Success || loadResult.Dictionary == null)
    {
        WriteDiagnostics(source.ResourceName, loadResult.Diagnostics);
        throw new InvalidOperationException("Unable to normalize " + source.RelativeSource + ".");
    }

    FixDictionary dictionary = loadResult.Dictionary;
    NormalizeIdentity(dictionary.Identity, source);
    ApplyProvenance(dictionary, source.RelativeSource);
    string resourceFileName = source.ResourceName + ".fixdict.gz";
    string resourcePath = Path.Combine(outputDirectory, resourceFileName);
    using (FileStream output = File.Create(resourcePath))
    {
        EmbeddedDictionaryFormat.WriteResource(output, dictionary);
    }

    FixDictionary roundTrip;
    using (FileStream validation = File.OpenRead(resourcePath))
    {
        roundTrip = EmbeddedDictionaryFormat.ReadResource(validation);
    }
    ValidateRoundTrip(dictionary, roundTrip, resourceFileName);
    int codeCount = CountCodes(dictionary);
    int definitionCount = CountDefinitions(dictionary);
    string digest = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(resourcePath))).ToLowerInvariant();
    string extensionPack = dictionary.Identity.HasExtensionPack ? dictionary.Identity.ExtensionPack.ToString(System.Globalization.CultureInfo.InvariantCulture) : "";
    manifestLines.Add(string.Join('\t', resourceFileName, source.RelativeSource, dictionary.Identity.Name, dictionary.Identity.BeginString, dictionary.Identity.ApplicationVersion, extensionPack, dictionary.Datatypes.Count, dictionary.CodeSets.Count, codeCount, dictionary.Fields.Count, dictionary.Components.Count, dictionary.Groups.Count, dictionary.Messages.Count, dictionary.Scenarios.Count, definitionCount, digest));
    Console.WriteLine(resourceFileName + " " + dictionary.Fields.Count + " fields " + dictionary.Messages.Count + " messages " + definitionCount + " definitions");
    WriteDiagnostics(source.ResourceName, loadResult.Diagnostics);
}

File.WriteAllText(Path.Combine(outputDirectory, "DictionaryResources.tsv"), string.Join('\n', manifestLines) + "\n", new System.Text.UTF8Encoding(false));

static void NormalizeIdentity(FixDictionaryIdentity identity, ResourceSource source)
{
    identity.ApplicationVersion = source.ApplicationVersion;
    identity.HasExtensionPack = source.HasExtensionPack;
    identity.ExtensionPack = source.ExtensionPack;
    identity.CustomVersion = "";
    if (source.ClearBeginString)
    {
        identity.BeginString = "";
    }
}

static void ApplyProvenance(FixDictionary dictionary, string source)
{
    dictionary.Identity.Source = source;
    foreach (FixDatatypeDefinition definition in dictionary.Datatypes)
    {
        definition.Source = source;
    }
    foreach (FixCodeSetDefinition definition in dictionary.CodeSets)
    {
        definition.Source = source;
        foreach (FixCodeDefinition code in definition.Codes)
        {
            code.Source = source;
        }
    }
    foreach (FixFieldDefinition definition in dictionary.Fields)
    {
        definition.Source = source;
    }
    ApplyMemberProvenance(dictionary.Header, source);
    ApplyMemberProvenance(dictionary.Trailer, source);
    foreach (FixComponentDefinition definition in dictionary.Components)
    {
        definition.Source = source;
        ApplyMemberProvenance(definition.Members, source);
    }
    foreach (FixGroupDefinition definition in dictionary.Groups)
    {
        definition.Source = source;
        ApplyMemberProvenance(definition.Members, source);
    }
    foreach (FixMessageDefinition definition in dictionary.Messages)
    {
        definition.Source = source;
        ApplyMemberProvenance(definition.Members, source);
    }
    foreach (FixScenarioDefinition definition in dictionary.Scenarios)
    {
        definition.Source = source;
        ApplyMemberProvenance(definition.Members, source);
    }
}

static void ApplyMemberProvenance(List<FixMemberDefinition> members, string source)
{
    foreach (FixMemberDefinition member in members)
    {
        member.Source = source;
    }
}

static int CountDefinitions(FixDictionary dictionary)
{
    int count = dictionary.Datatypes.Count + dictionary.CodeSets.Count + dictionary.Fields.Count + dictionary.Components.Count + dictionary.Groups.Count + dictionary.Messages.Count + dictionary.Scenarios.Count;
    return count + CountCodes(dictionary);
}

static int CountCodes(FixDictionary dictionary)
{
    int count = 0;
    foreach (FixCodeSetDefinition codeSet in dictionary.CodeSets)
    {
        count += codeSet.Codes.Count;
    }
    return count;
}

static void ValidateRoundTrip(FixDictionary expected, FixDictionary actual, string resourceName)
{
    bool matches = expected.Identity.Name == actual.Identity.Name
        && expected.Identity.BeginString == actual.Identity.BeginString
        && expected.Identity.ApplicationVersion == actual.Identity.ApplicationVersion
        && expected.Identity.HasExtensionPack == actual.Identity.HasExtensionPack
        && expected.Identity.ExtensionPack == actual.Identity.ExtensionPack
        && expected.Datatypes.Count == actual.Datatypes.Count
        && expected.CodeSets.Count == actual.CodeSets.Count
        && expected.Fields.Count == actual.Fields.Count
        && expected.Header.Count == actual.Header.Count
        && expected.Trailer.Count == actual.Trailer.Count
        && expected.Components.Count == actual.Components.Count
        && expected.Groups.Count == actual.Groups.Count
        && expected.Messages.Count == actual.Messages.Count
        && expected.Scenarios.Count == actual.Scenarios.Count;
    if (!matches)
    {
        throw new InvalidOperationException("Embedded dictionary round trip failed for " + resourceName + ".");
    }
}

static void WriteDiagnostics(string resourceName, List<DictionaryDiagnostic> diagnostics)
{
    int warningCount = 0;
    foreach (DictionaryDiagnostic diagnostic in diagnostics)
    {
        if (diagnostic.Severity == ValidationSeverity.Warning || diagnostic.Severity == ValidationSeverity.Information)
        {
            warningCount++;
        }
        else
        {
            Console.WriteLine(resourceName + " " + diagnostic.Severity + " " + diagnostic.Code + " " + diagnostic.DefinitionName);
        }
    }
    if (warningCount != 0)
    {
        Console.WriteLine(resourceName + " " + warningCount + " normalization warnings");
    }
}

internal sealed record ResourceSource(string ResourceName, string RelativeSource, string ApplicationVersion, bool HasExtensionPack, int ExtensionPack, bool ClearBeginString)
{
    internal bool IsOrchestra => RelativeSource.Contains("Orchestra", StringComparison.Ordinal);
}
