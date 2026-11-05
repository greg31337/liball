using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: ApiCoverageGate <qrcode.dll> <fresh-results-directory>");
    return 2;
}

string assemblyPath = Path.GetFullPath(args[0]);
string resultsDirectory = Path.GetFullPath(args[1]);
string[] reports = Directory.GetFiles(resultsDirectory, "coverage.json", SearchOption.AllDirectories);
if (reports.Length == 0)
{
    Console.Error.WriteLine("Expected a fresh coverage.json, found none.");
    return 2;
}
byte[] reportBytes = File.ReadAllBytes(reports[0]);
// The VSTest TRX logger copies collector attachments into its own results tree.
// Accept identical copies, but never silently select among different reports.
if (reports.Skip(1).Any(path => !reportBytes.SequenceEqual(File.ReadAllBytes(path))))
{
    Console.Error.WriteLine("Expected one unique fresh coverage.json, found conflicting reports.");
    return 2;
}
using JsonDocument report = JsonDocument.Parse(reportBytes);
Assembly assembly = Assembly.LoadFrom(assemblyPath);
if (!report.RootElement.TryGetProperty(Path.GetFileName(assemblyPath), out JsonElement module))
{
    Console.Error.WriteLine("The coverage report does not contain the library assembly.");
    return 2;
}
using FileStream pdbStream = File.OpenRead(Path.ChangeExtension(assemblyPath, ".pdb"));
using MetadataReaderProvider pdbProvider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
MetadataReader pdb = pdbProvider.GetMetadataReader();
var methods = new Dictionary<string, (string File, JsonElement Data)>();
foreach (JsonProperty file in module.EnumerateObject())
    foreach (JsonProperty type in file.Value.EnumerateObject())
        foreach (JsonProperty method in type.Value.EnumerateObject())
            methods.Add(method.Name, (file.Name, method.Value));

int measured = 0, sourceFreeConstructors = 0, lines = 0, hitLines = 0, branches = 0, hitBranches = 0;
var failures = new List<string>();
var details = new List<object>();
foreach (Type type in assembly.GetExportedTypes().OrderBy(t => t.FullName, StringComparer.Ordinal))
{
    // Abstract contracts contain no executable implementation. Enumerating the
    // assembly, rather than the report alone, detects missing new API methods.
    IEnumerable<MethodBase> members = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
        .Cast<MethodBase>().Concat(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
    foreach (MethodBase member in members.Where(m => !m.IsAbstract).OrderBy(m => m.Name, StringComparer.Ordinal))
    {
        var handle = MetadataTokens.MethodDefinitionHandle(member.MetadataToken & 0x00ffffff);
        var points = pdb.GetMethodDebugInformation(handle).GetSequencePoints().Where(p => !p.IsHidden).ToArray();
        if (member.IsConstructor && points.Length == 0)
        {
            sourceFreeConstructors++;
            continue;
        }
        string returnType = member is MethodInfo methodInfo ? TypeName(methodInfo.ReturnType) : "System.Void";
        string signature = $"{returnType} {type.FullName}::{member.Name}({string.Join(',', member.GetParameters().Select(p => TypeName(p.ParameterType)))})";
        measured++;
        if (points.Length == 0 || !methods.TryGetValue(signature, out var coverage))
        {
            failures.Add($"Missing source/coverage for public member: {signature}");
            continue;
        }
        string source = coverage.File.Replace('\\', '/');
        if (!source.Contains("/src/Api/", StringComparison.Ordinal))
        {
            failures.Add($"Public member has unexpected source: {signature}: {source}");
            continue;
        }
        JsonProperty[] methodLines = coverage.Data.GetProperty("Lines").EnumerateObject().ToArray();
        JsonElement[] methodBranches = coverage.Data.GetProperty("Branches").EnumerateArray().ToArray();
        string[] missingLines = methodLines.Where(l => l.Value.GetInt64() == 0).Select(l => l.Name).ToArray();
        var missingBranches = methodBranches.Where(b => b.GetProperty("Hits").GetInt64() == 0).ToArray();
        lines += methodLines.Length;
        hitLines += methodLines.Length - missingLines.Length;
        branches += methodBranches.Length;
        hitBranches += methodBranches.Length - missingBranches.Length;
        if (methodLines.Length == 0) failures.Add($"No instrumented lines for {signature}");
        foreach (string line in missingLines) failures.Add($"Uncovered line {source}:{line}: {signature}");
        foreach (JsonElement branch in missingBranches)
            failures.Add($"Uncovered branch {source}:{branch.GetProperty("Line")}, offset {branch.GetProperty("Offset")}, path {branch.GetProperty("Path")}: {signature}");
        details.Add(new { Signature = signature, File = source, Lines = methodLines.Length, CoveredLines = methodLines.Length - missingLines.Length,
            Branches = methodBranches.Length, CoveredBranches = methodBranches.Length - missingBranches.Length });
    }
}
if (measured == 0 || lines == 0) failures.Add("The report contains no measurable public API.");
var summary = new { Passed = failures.Count == 0, PublicMembers = measured, SourceFreeConstructors = sourceFreeConstructors,
    Lines = lines, CoveredLines = hitLines, Branches = branches, CoveredBranches = hitBranches, Methods = details, Failures = failures };
string summaryPath = Path.Combine(resultsDirectory, "api-coverage-summary.json");
File.WriteAllText(summaryPath, JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"Public API: {measured} measurable members; lines {hitLines}/{lines}; branches {hitBranches}/{branches}.");
Console.WriteLine($"Source-free implicit constructors: {sourceFreeConstructors}. Report: {summaryPath}");
foreach (string failure in failures) Console.Error.WriteLine(failure);
return failures.Count == 0 ? 0 : 1;

static string TypeName(Type type) => type.FullName ?? throw new InvalidOperationException($"Unnamed API type: {type}");
