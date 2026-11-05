namespace raw;

/// <summary>Stores validated built-in and runtime custom profiles with deterministic source precedence.</summary>
public class RawProfileCatalogue : IRawProfileCatalogue, IRawProfileRegistry
{
    /// <summary>Registered read profiles in stable insertion order.</summary>
    internal RawReadProfile[]? ReadProfiles;
    /// <summary>The source corresponding to each registered read profile.</summary>
    internal RawProfileRegistrationSource[]? ReadProfileSources;
    /// <summary>Registered write profiles in stable insertion order.</summary>
    internal RawWriteProfile[]? WriteProfiles;
    /// <summary>The source corresponding to each registered write profile.</summary>
    internal RawProfileRegistrationSource[]? WriteProfileSources;
    /// <summary>All registration diagnostics in production order.</summary>
    internal RawDiagnostic[]? Diagnostics;

    internal System.Collections.Generic.List<RawReadProfile> ReadList;
    internal System.Collections.Generic.List<RawProfileRegistrationSource> ReadSourceList;
    internal System.Collections.Generic.List<RawWriteProfile> WriteList;
    internal System.Collections.Generic.List<RawProfileRegistrationSource> WriteSourceList;
    internal System.Collections.Generic.List<RawDiagnostic> DiagnosticList;
    internal object SyncRoot;

    /// <summary>Creates a catalogue containing the library's reviewed built-in read profiles.</summary>
    /// <param name="limits">Resource limits used to serialize, validate, and register the catalogue.</param>
    /// <returns>A new independently copied built-in catalogue.</returns>
    internal static RawProfileCatalogue CreateBuiltIn(RawLimits limits)
    {
        byte[] json = RawBuiltInProfileCatalogue.CreateJson(limits);
        return CreateFromBuiltInJson(json, limits);
    }

    /// <summary>Creates an empty runtime profile catalogue.</summary>
    public RawProfileCatalogue()
    {
        SyncRoot = new object();
        ReadList = new System.Collections.Generic.List<RawReadProfile>();
        ReadSourceList = new System.Collections.Generic.List<RawProfileRegistrationSource>();
        WriteList = new System.Collections.Generic.List<RawWriteProfile>();
        WriteSourceList = new System.Collections.Generic.List<RawProfileRegistrationSource>();
        DiagnosticList = new System.Collections.Generic.List<RawDiagnostic>();
        Refresh();
    }

    /// <summary>Returns the source corresponding to each registered read profile.</summary>
    /// <returns>An independently replaceable source array in profile order.</returns>
    public RawProfileRegistrationSource[] GetReadProfileSources()
    {
        lock (SyncRoot)
        {
            RawProfileRegistrationSource[] sources = new RawProfileRegistrationSource[(ReadProfileSources ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length];
            System.Array.Copy(ReadProfileSources, sources, sources.Length);
            return sources;
        }
    }

    /// <summary>Returns the source corresponding to each registered write profile.</summary>
    /// <returns>An independently replaceable source array in profile order.</returns>
    public RawProfileRegistrationSource[] GetWriteProfileSources()
    {
        lock (SyncRoot)
        {
            RawProfileRegistrationSource[] sources = new RawProfileRegistrationSource[(WriteProfileSources ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length];
            System.Array.Copy(WriteProfileSources, sources, sources.Length);
            return sources;
        }
    }

    /// <summary>Returns all registration diagnostics accumulated by the catalogue.</summary>
    /// <returns>An operation result containing an independent diagnostic snapshot.</returns>
    public RawOperationResult GetRegistrationDiagnostics()
    {
        lock (SyncRoot)
        {
            RawDiagnostic[] diagnostics = new RawDiagnostic[(Diagnostics ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length];
            System.Array.Copy(Diagnostics, diagnostics, diagnostics.Length);
            return new RawOperationResult(null, diagnostics);
        }
    }

    /// <summary>Creates a catalogue and loads one validated built-in JSON document.</summary>
    /// <param name="builtInJson">The complete built-in profile JSON.</param>
    /// <param name="limits">Resource limits.</param>
    /// <exception cref="RawProfileException">The built-in document conflicts with an existing entry.</exception>
    /// <returns>The populated catalogue.</returns>
    internal static RawProfileCatalogue CreateFromBuiltInJson(byte[] builtInJson, RawLimits limits)
    {
        RawProfileCatalogue catalogue = new RawProfileCatalogue();
        RawProfileRegistrationResult result = catalogue.LoadBuiltIn(builtInJson, limits);
        if (!result.Accepted)
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "The built-in profile catalogue could not be registered.", -1);
        }
        return catalogue;
    }

    /// <summary>Loads a validated built-in JSON document without permitting replacement of registered identifiers.</summary>
    /// <param name="json">The complete profile JSON.</param>
    /// <param name="limits">Resource limits.</param>
    /// <returns>The atomic registration result.</returns>
    public RawProfileRegistrationResult LoadBuiltIn(byte[] json, RawLimits limits)
    {
        RawProfileSchemaDocument document = RawProfileSchemaCodec.Read(json, limits);
        return Register(document, RawProfileRegistrationSource.BuiltIn, false, limits);
    }

    /// <summary>Registers caller-supplied custom JSON using custom-over-built-in precedence.</summary>
    /// <param name="json">The complete profile JSON.</param>
    /// <param name="replaceCustom">Whether an existing custom entry with the same identifier may be replaced.</param>
    /// <param name="limits">Resource limits.</param>
    /// <returns>The atomic registration result.</returns>
    public RawProfileRegistrationResult RegisterCustom(byte[] json, bool replaceCustom, RawLimits limits)
    {
        RawProfileSchemaDocument document = RawProfileSchemaCodec.Read(json, limits);
        return Register(document, RawProfileRegistrationSource.Custom, replaceCustom, limits);
    }

    /// <summary>Registers a runtime profile document after validating and independently copying it.</summary>
    /// <param name="document">The runtime profile document.</param>
    /// <param name="replaceCustom">Whether an existing custom entry with the same identifier may be replaced.</param>
    /// <param name="limits">Resource limits.</param>
    /// <returns>The atomic registration result.</returns>
    public RawProfileRegistrationResult RegisterCustomDocument(RawProfileSchemaDocument? document, bool replaceCustom, RawLimits limits)
    {
        if (document == null)
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidArgument, "A runtime profile document is required.", -1);
        }
        RawProfileSchemaDocument copied = RawProfileSchemaCodec.Read(RawProfileSchemaCodec.Write(document, limits), limits);
        return Register(copied, RawProfileRegistrationSource.Custom, replaceCustom, limits);
    }

    /// <summary>Finds a unique registered read profile by exact stable identifier.</summary>
    /// <param name="id">The exact profile identifier.</param>
    /// <returns>The registered profile, or <c>null</c>.</returns>
    public RawReadProfile? FindReadProfile(string id)
    {
        lock (SyncRoot)
        {
            int index = FindReadIndex(id);
            return index < 0 ? null : ReadList[index];
        }
    }

    /// <summary>Finds a unique registered write profile by exact stable identifier.</summary>
    /// <param name="id">The exact profile identifier.</param>
    /// <returns>The registered profile, or <c>null</c>.</returns>
    public RawWriteProfile? FindWriteProfile(string id)
    {
        lock (SyncRoot)
        {
            int index = FindWriteIndex(id);
            return index < 0 ? null : WriteList[index];
        }
    }

    /// <summary>Matches registered read profiles using normalized identity, firmware bounds, structural scoring, and experimental policy.</summary>
    /// <param name="input">Observed identity and structural evidence.</param>
    /// <returns>The deterministic read-profile match result.</returns>
    public RawProfileMatchResult MatchReadProfile(RawProfileMatchInput? input)
    {
        return RawProfileMatcher.MatchRead(GetReadProfilesSnapshot(), input);
    }

    /// <summary>Matches separately qualified write profiles using normalized identity, firmware bounds, structural scoring, and experimental policy.</summary>
    /// <param name="input">Requested identity and output structural evidence.</param>
    /// <returns>The deterministic write-profile match result.</returns>
    public RawProfileMatchResult MatchWriteProfile(RawProfileMatchInput input)
    {
        return RawProfileMatcher.MatchWrite(GetWriteProfilesSnapshot(), input);
    }

    /// <summary>Builds the application/version compatibility matrix for registered writer profiles of one format.</summary>
    /// <param name="format">The defined output format to include.</param>
    /// <returns>Rows in profile registration and evidence order, including placeholders for unconfirmed profiles.</returns>
    /// <exception cref="RawProfileException">The requested format is not defined.</exception>
    public RawWriteCompatibilityMatrix GetWriteCompatibilityMatrix(RawFormat format)
    {
        return RawWriteCompatibilityMatrixBuilder.Create(GetWriteProfilesSnapshot(), format);
    }

    /// <summary>Returns a stable snapshot of the registered read profiles.</summary>
    /// <returns>An independently replaceable profile array.</returns>
    public RawReadProfile[] GetReadProfiles()
    {
        return GetReadProfilesSnapshot();
    }

    /// <summary>Returns a stable snapshot of the registered write profiles.</summary>
    /// <returns>An independently replaceable profile array.</returns>
    public RawWriteProfile[] GetWriteProfiles()
    {
        return GetWriteProfilesSnapshot();
    }

    internal RawProfileRegistrationResult Register(RawProfileSchemaDocument document, RawProfileRegistrationSource source, bool replaceCustom, RawLimits limits)
    {
        if (source != RawProfileRegistrationSource.BuiltIn && source != RawProfileRegistrationSource.Custom)
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidArgument, "A defined registration source is required.", -1);
        }
        if (document == null || !document.Validate(limits).IsValid())
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "A validated profile document is required for registration.", -1);
        }
        lock (SyncRoot)
        {
            return RegisterLocked(document, source, replaceCustom);
        }
    }

    internal RawProfileRegistrationResult RegisterLocked(RawProfileSchemaDocument document, RawProfileRegistrationSource source, bool replaceCustom)
    {
        System.Collections.Generic.List<RawDiagnostic> attempt = new System.Collections.Generic.List<RawDiagnostic>();
        if (!CanRegisterRead(document.ReadProfiles, source, replaceCustom, attempt) || !CanRegisterWrite(document.WriteProfiles, source, replaceCustom, attempt))
        {
            AppendDiagnostics(attempt);
            Refresh();
            return new RawProfileRegistrationResult(false, null, null, null, null, attempt.ToArray());
        }
        System.Collections.Generic.List<RawReadProfile> addedRead = new System.Collections.Generic.List<RawReadProfile>();
        System.Collections.Generic.List<RawReadProfile> replacedRead = new System.Collections.Generic.List<RawReadProfile>();
        System.Collections.Generic.List<RawWriteProfile> addedWrite = new System.Collections.Generic.List<RawWriteProfile>();
        System.Collections.Generic.List<RawWriteProfile> replacedWrite = new System.Collections.Generic.List<RawWriteProfile>();
        int index = 0;
        while (index < document.ReadProfiles.Length)
        {
            RawReadProfile profile = document.ReadProfiles[index];
            int existing = FindReadIndex(profile.Id);
            if (existing < 0)
            {
                ReadList.Add(profile);
                ReadSourceList.Add(source);
                addedRead.Add(profile);
            }
            else
            {
                replacedRead.Add(ReadList[existing]);
                ReadList[existing] = profile;
                ReadSourceList[existing] = source;
                attempt.Add(Diagnostic(RawDiagnosticCode.InvalidProfile, RawSeverity.Information, profile.Identity.Format, "Custom read profile '" + profile.Id + "' replaced the lower-precedence built-in entry.", true));
            }
            index++;
        }
        index = 0;
        while (index < document.WriteProfiles.Length)
        {
            RawWriteProfile profile = document.WriteProfiles[index];
            int existing = FindWriteIndex(profile.Id);
            if (existing < 0)
            {
                WriteList.Add(profile);
                WriteSourceList.Add(source);
                addedWrite.Add(profile);
            }
            else
            {
                replacedWrite.Add(WriteList[existing]);
                WriteList[existing] = profile;
                WriteSourceList[existing] = source;
                attempt.Add(Diagnostic(RawDiagnosticCode.InvalidProfile, RawSeverity.Information, profile.Identity.Format, "Custom write profile '" + profile.Id + "' replaced the lower-precedence entry.", true));
            }
            index++;
        }
        AddIdentityConflicts(document.ReadProfiles, attempt);
        AppendDiagnostics(attempt);
        Refresh();
        return new RawProfileRegistrationResult(true, addedRead.ToArray(), replacedRead.ToArray(), addedWrite.ToArray(), replacedWrite.ToArray(), attempt.ToArray());
    }

    internal RawReadProfile[] GetReadProfilesSnapshot()
    {
        lock (SyncRoot)
        {
            RawReadProfile[] profiles = new RawReadProfile[(ReadProfiles ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length];
            System.Array.Copy(ReadProfiles, profiles, profiles.Length);
            return profiles;
        }
    }

    internal RawWriteProfile[] GetWriteProfilesSnapshot()
    {
        lock (SyncRoot)
        {
            RawWriteProfile[] profiles = new RawWriteProfile[(WriteProfiles ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length];
            System.Array.Copy(WriteProfiles, profiles, profiles.Length);
            return profiles;
        }
    }

    internal bool CanRegisterRead(RawReadProfile[] profiles, RawProfileRegistrationSource source, bool replaceCustom, System.Collections.Generic.List<RawDiagnostic> diagnostics)
    {
        int index = 0;
        while (index < profiles.Length)
        {
            RawReadProfile profile = profiles[index];
            int existing = FindReadIndex(profile.Id);
            if (existing >= 0 && !CanReplace(ReadSourceList[existing], source, replaceCustom))
            {
                diagnostics.Add(Diagnostic(RawDiagnosticCode.InvalidProfile, RawSeverity.Error, profile.Identity.Format, "Read profile identifier '" + profile.Id + "' is already registered at equal or higher precedence.", false));
                return false;
            }
            index++;
        }
        return true;
    }

    internal bool CanRegisterWrite(RawWriteProfile[] profiles, RawProfileRegistrationSource source, bool replaceCustom, System.Collections.Generic.List<RawDiagnostic> diagnostics)
    {
        int index = 0;
        while (index < profiles.Length)
        {
            RawWriteProfile profile = profiles[index];
            int existing = FindWriteIndex(profile.Id);
            if (existing >= 0 && !CanReplace(WriteSourceList[existing], source, replaceCustom))
            {
                diagnostics.Add(Diagnostic(RawDiagnosticCode.InvalidProfile, RawSeverity.Error, profile.Identity.Format, "Write profile identifier '" + profile.Id + "' is already registered at equal or higher precedence.", false));
                return false;
            }
            index++;
        }
        return true;
    }

    internal static bool CanReplace(RawProfileRegistrationSource existing, RawProfileRegistrationSource incoming, bool replaceCustom)
    {
        if (incoming != RawProfileRegistrationSource.Custom)
        {
            return false;
        }
        if (existing == RawProfileRegistrationSource.BuiltIn)
        {
            return true;
        }
        return existing == RawProfileRegistrationSource.Custom && replaceCustom;
    }

    internal void AddIdentityConflicts(RawReadProfile[] incoming, System.Collections.Generic.List<RawDiagnostic> diagnostics)
    {
        System.Collections.Generic.HashSet<string> reportedPairs = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
        int incomingIndex = 0;
        while (incomingIndex < incoming.Length)
        {
            RawReadProfile profile = incoming[incomingIndex];
            int index = 0;
            while (index < ReadList.Count)
            {
                RawReadProfile other = ReadList[index];
                if (other.Id != profile.Id && SameIdentity(other.Identity, profile.Identity))
                {
                    string first = string.CompareOrdinal(other.Id, profile.Id) < 0 ? other.Id : profile.Id;
                    string second = first == other.Id ? profile.Id : other.Id;
                    string pair = first.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + first + second;
                    if (reportedPairs.Add(pair))
                    {
                        diagnostics.Add(Diagnostic(RawDiagnosticCode.AmbiguousProfile, RawSeverity.Warning, profile.Identity.Format, "Read profiles '" + first + "' and '" + second + "' declare the same normalized canonical camera identity; fingerprint matching must disambiguate them.", true));
                    }
                }
                index++;
            }
            incomingIndex++;
        }
    }

    internal static bool SameIdentity(RawProfileIdentity first, RawProfileIdentity second)
    {
        if (first == null || second == null || first.Format != second.Format)
        {
            return false;
        }
        return RawProfileMatcher.NormalizeIdentityText(first.Make) == RawProfileMatcher.NormalizeIdentityText(second.Make) && RawProfileMatcher.NormalizeIdentityText(first.Model) == RawProfileMatcher.NormalizeIdentityText(second.Model) && RawProfileMatcher.NormalizeIdentityText(first.CameraId) == RawProfileMatcher.NormalizeIdentityText(second.CameraId);
    }

    internal int FindReadIndex(string id)
    {
        if (id == null)
        {
            return -1;
        }
        int index = 0;
        while (index < ReadList.Count)
        {
            if (ReadList[index].Id == id)
            {
                return index;
            }
            index++;
        }
        return -1;
    }

    internal int FindWriteIndex(string id)
    {
        if (id == null)
        {
            return -1;
        }
        int index = 0;
        while (index < WriteList.Count)
        {
            if (WriteList[index].Id == id)
            {
                return index;
            }
            index++;
        }
        return -1;
    }

    internal void AppendDiagnostics(System.Collections.Generic.List<RawDiagnostic> diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            DiagnosticList.Add(diagnostics[index]);
            index++;
        }
    }

    internal void Refresh()
    {
        ReadProfiles = ReadList.ToArray();
        ReadProfileSources = ReadSourceList.ToArray();
        WriteProfiles = WriteList.ToArray();
        WriteProfileSources = WriteSourceList.ToArray();
        Diagnostics = DiagnosticList.ToArray();
    }

    internal static RawDiagnostic Diagnostic(RawDiagnosticCode code, RawSeverity severity, RawFormat format, string message, bool recovered)
    {
        return new RawDiagnostic(code, severity, format, -1, string.Empty, -1, "ProfileCatalogue", message, recovered);
    }
}
