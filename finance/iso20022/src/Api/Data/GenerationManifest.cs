using System.Collections.Generic;

namespace iso20022;

/// <summary>Records deterministic generation settings, source provenance, symbol mappings, owned files, and hashes.</summary>
public class GenerationManifest
{
    /// <summary>The generation-manifest format version.</summary>
    public string FormatVersion = "1";
    /// <summary>The identifier of the generator implementation that produced the manifest.</summary>
    public string GeneratorIdentifier = string.Empty;
    /// <summary>The exact generator version.</summary>
    public string GeneratorVersion = string.Empty;
    /// <summary>The exact repository or artifact edition used for generation.</summary>
    public string ArtifactEdition = string.Empty;
    /// <summary>The exact market-practice or processing profile used for generation.</summary>
    public string Profile = string.Empty;
    /// <summary>The root C# namespace used for generated symbols.</summary>
    public string RootNamespace = string.Empty;
    /// <summary>The exact settings used for generation.</summary>
    public GenerationOptions Options = new GenerationOptions();
    /// <summary>The ordered source artifact manifests that contributed to generated output.</summary>
    public List<ArtifactManifest> SourceArtifacts = new List<ArtifactManifest>();
    /// <summary>The complete deterministic repository, XML, C#, and output symbol mappings.</summary>
    public List<GenerationSymbol> Symbols = new List<GenerationSymbol>();
    /// <summary>The complete deterministic set of files owned by this manifest.</summary>
    public List<GeneratedFile> Files = new List<GeneratedFile>();
    /// <summary>The canonical generation-settings hash.</summary>
    public IsoBinaryValue SettingsHash = new IsoBinaryValue();
    /// <summary>The aggregate ordered generated-output hash.</summary>
    public IsoBinaryValue OutputHash = new IsoBinaryValue();
    /// <summary>The canonical manifest hash.</summary>
    public IsoBinaryValue ManifestHash = new IsoBinaryValue();
    /// <summary>The name of the algorithm used for manifest and output hashes.</summary>
    public string HashAlgorithm = string.Empty;
}
