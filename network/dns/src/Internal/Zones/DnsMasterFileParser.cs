using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace dns;

internal class DnsMasterRecordHeaderResult
{
    internal bool Success;
    internal DnsName? Owner;
    internal int ClassValue;
    internal uint TimeToLive;
    internal int RecordTypeValue;
    internal int RecordDataStart;
    internal bool HasExplicitClass;
    internal bool HasExplicitTimeToLive;
    internal DnsZoneDiagnostic? Diagnostic;
}

internal class DnsMasterFileParseState
{
    internal DnsZoneLoadOptions Options = new DnsZoneLoadOptions();
    internal DnsZoneLoadResult Result = new DnsZoneLoadResult();
    internal DnsZone Zone = new DnsZone();
    internal DnsName? Origin;
    internal DnsName? PreviousOwner;
    internal bool HasDefaultTimeToLive;
    internal bool DefaultTimeToLiveIsDirective;
    internal uint DefaultTimeToLive;
    internal int ClassValue;
    internal List<string> ActiveIncludePaths = new List<string>();
}

internal class DnsMasterPathResult
{
    internal bool Success;
    internal string Path = string.Empty;
    internal string Reason = string.Empty;
}

internal static class DnsMasterFileParser
{
    internal static DnsZoneLoadResult ParseZone(
        string source,
        string? sourcePath,
        DnsZoneLoadOptions options)
    {
        DnsZoneLoadResult result = new DnsZoneLoadResult();
        DnsZoneDiagnostic? optionDiagnostic = ValidateOptions(
            options, sourcePath);
        if (optionDiagnostic != null)
        {
            result.Diagnostics.Add(optionDiagnostic);
            return result;
        }

        DnsMasterFileParseState state = CreateState(options, result);
        if (sourcePath != null && File.Exists(sourcePath))
            state.ActiveIncludePaths.Add(Path.GetFullPath(sourcePath));
        if (!ProcessSource(source, sourcePath, state, 0)) return result;
        result.Zone = state.Zone;
        if (options.ValidateZone)
        {
            DnsZoneValidationResult validation =
                DnsZoneValidator.ValidateZone(state.Zone);
            result.Validation = validation;
            if (!validation.Valid)
            {
                CopyDiagnostics(validation.Diagnostics, result.Diagnostics);
                return result;
            }
        }

        result.Success = true;
        return result;
    }

    static bool ProcessSource(
        string source,
        string? sourcePath,
        DnsMasterFileParseState state,
        int includeDepth)
    {
        DnsMasterFileTokenizeResult tokenized =
            DnsMasterFileTokenizer.Tokenize(
                source, sourcePath, state.Options);
        if (!tokenized.Success)
        {
            CopyDiagnostics(
                tokenized.Diagnostics, state.Result.Diagnostics);
            return false;
        }

        int index = 0;
        while (index < tokenized.Records.Count)
        {
            DnsMasterFileRecord logicalRecord = tokenized.Records[index];
            if (logicalRecord.Tokens.Count > 0 &&
                logicalRecord.Tokens[0].RawText.StartsWith(
                    "$", System.StringComparison.Ordinal))
            {
                if (!ProcessDirective(
                    logicalRecord, state, includeDepth)) return false;
            }
            else
            {
                if (!ProcessResourceRecord(logicalRecord, state))
                    return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool ProcessResourceRecord(
        DnsMasterFileRecord logicalRecord,
        DnsMasterFileParseState state)
    {
        DnsMasterRecordHeaderResult header = ParseHeader(
            logicalRecord, state);
        if (!header.Success || header.Owner == null)
        {
            if (header.Diagnostic != null)
                state.Result.Diagnostics.Add(header.Diagnostic);
            return false;
        }

        DnsTextResult parsed = ParseCompleteRecord(
            logicalRecord, header, state.Origin);
        if (!parsed.Success || parsed.Record == null)
        {
            AddTextDiagnostics(
                parsed,
                state.Result,
                logicalRecord,
                header.RecordDataStart);
            return false;
        }

        if (state.Result.RecordsLoaded >= state.Options.MaximumRecords)
        {
            state.Result.Diagnostics.Add(CreateDiagnostic(
                DnsDiagnosticCode.ResourceLimitExceeded,
                "The zone exceeds the configured record limit.",
                logicalRecord.SourcePath,
                logicalRecord.Line,
                logicalRecord.Column));
            return false;
        }

        DnsResourceRecord record = parsed.Record;
        AddRecord(state.Zone, record);
        state.PreviousOwner = RecordSetOperations.CopyName(
            header.Owner, false);
        state.Result.RecordsLoaded = state.Result.RecordsLoaded + 1;
        if (header.HasExplicitClass)
            state.ClassValue = header.ClassValue;
        if (header.HasExplicitTimeToLive &&
            !state.DefaultTimeToLiveIsDirective)
        {
            state.HasDefaultTimeToLive = true;
            state.DefaultTimeToLive = header.TimeToLive;
            state.Zone.HasDefaultTimeToLive = true;
            state.Zone.DefaultTimeToLive = header.TimeToLive;
        }

        if (state.Zone.Apex.Labels.Count == 0 &&
            !state.Zone.Apex.IsAbsolute && header.Owner.IsAbsolute)
        {
            state.Zone.Apex = RecordSetOperations.CopyName(
                header.Owner, false);
        }

        if (header.RecordTypeValue == 6 && header.Owner.IsAbsolute)
        {
            state.Zone.Apex = RecordSetOperations.CopyName(
                header.Owner, false);
        }

        return true;
    }

    static bool ProcessDirective(
        DnsMasterFileRecord record,
        DnsMasterFileParseState state,
        int includeDepth)
    {
        string name = record.Tokens[0].Text.ToUpperInvariant();
        switch (name)
        {
            case "$ORIGIN": return ProcessOrigin(record, state);
            case "$TTL": return ProcessDefaultTimeToLive(record, state);
            case "$INCLUDE": return ProcessInclude(
                record, state, includeDepth);
            case "$GENERATE": return ProcessGenerate(record, state);
            default:
                state.Result.Diagnostics.Add(CreateDiagnostic(
                    DnsDiagnosticCode.InvalidZone,
                    "The master-file directive is not recognized.",
                    record.SourcePath,
                    record.Tokens[0].Line,
                    record.Tokens[0].Column));
                return false;
        }
    }

    static bool ProcessOrigin(
        DnsMasterFileRecord record,
        DnsMasterFileParseState state)
    {
        if (record.Tokens.Count != 2)
        {
            AddDirectiveDiagnostic(
                record, state, "$ORIGIN requires exactly one DNS name.");
            return false;
        }

        DnsTextOptions options = CreateTextOptions(state.Origin);
        DnsTextResult parsed = DnsPresentationNameText.ParseNameToken(
            record.Tokens[1].RawText, options);
        if (!parsed.Success || parsed.Name == null ||
            !parsed.Name.IsAbsolute)
        {
            AddDirectiveDiagnostic(
                record, state, "$ORIGIN must resolve to an absolute DNS name.");
            return false;
        }

        state.Origin = RecordSetOperations.CopyName(parsed.Name, false);
        if (state.Result.RecordsLoaded == 0 &&
            state.Zone.Apex.Labels.Count == 0 &&
            !state.Zone.Apex.IsAbsolute)
        {
            state.Zone.Apex = RecordSetOperations.CopyName(
                parsed.Name, false);
        }

        return true;
    }

    static bool ProcessDefaultTimeToLive(
        DnsMasterFileRecord record,
        DnsMasterFileParseState state)
    {
        if (record.Tokens.Count != 2)
        {
            AddDirectiveDiagnostic(
                record, state, "$TTL requires exactly one TTL value.");
            return false;
        }

        DnsTextNumberResult parsed = DnsTimeToLiveText.ParseTimeToLive(
            record.Tokens[1].Text);
        if (!parsed.Success)
        {
            AddDirectiveDiagnostic(
                record, state, "$TTL contains an invalid TTL value.");
            return false;
        }

        state.HasDefaultTimeToLive = true;
        state.DefaultTimeToLiveIsDirective = true;
        state.DefaultTimeToLive = (uint)parsed.Value;
        state.Zone.HasDefaultTimeToLive = true;
        state.Zone.DefaultTimeToLive = (uint)parsed.Value;
        return true;
    }

    static bool ProcessInclude(
        DnsMasterFileRecord record,
        DnsMasterFileParseState state,
        int includeDepth)
    {
        if (!state.Options.AllowInclude)
        {
            AddDirectiveDiagnostic(
                record, state, "$INCLUDE processing is disabled.");
            return false;
        }

        if (record.Tokens.Count < 2 || record.Tokens.Count > 3)
        {
            AddDirectiveDiagnostic(
                record,
                state,
                "$INCLUDE requires a file name and optional origin.");
            return false;
        }

        if (includeDepth >= state.Options.MaximumIncludeDepth)
        {
            AddResourceDiagnostic(
                record, state, "The maximum include depth was exceeded.");
            return false;
        }

        DnsMasterPathResult path = ResolveIncludePath(
            record.Tokens[1].Text,
            record.SourcePath,
            state.Options);
        if (!path.Success)
        {
            AddDirectiveDiagnostic(record, state, path.Reason);
            return false;
        }

        if (ContainsPath(state.ActiveIncludePaths, path.Path))
        {
            AddDirectiveDiagnostic(
                record, state, "An include cycle was detected.");
            return false;
        }

        DnsName? includeOrigin = state.Origin;
        if (record.Tokens.Count == 3)
        {
            DnsTextResult parsedOrigin =
                DnsPresentationNameText.ParseNameToken(
                    record.Tokens[2].RawText,
                    CreateTextOptions(state.Origin));
            if (!parsedOrigin.Success || parsedOrigin.Name == null ||
                !parsedOrigin.Name.IsAbsolute)
            {
                AddDirectiveDiagnostic(
                    record,
                    state,
                    "The include origin must resolve to an absolute DNS name.");
                return false;
            }

            includeOrigin = parsedOrigin.Name;
        }

        string includedSource = File.ReadAllText(path.Path);
        DnsName? savedOrigin = state.Origin;
        DnsName? savedOwner = state.PreviousOwner;
        state.Origin = includeOrigin;
        state.ActiveIncludePaths.Add(path.Path);
        state.Result.IncludedFiles.Add(path.Path);
        bool success = ProcessSource(
            includedSource, path.Path, state, includeDepth + 1);
        state.ActiveIncludePaths.RemoveAt(
            state.ActiveIncludePaths.Count - 1);
        state.Origin = savedOrigin;
        state.PreviousOwner = savedOwner;
        return success;
    }

    static bool ProcessGenerate(
        DnsMasterFileRecord record,
        DnsMasterFileParseState state)
    {
        if (!state.Options.AllowGenerate)
        {
            AddDirectiveDiagnostic(
                record, state, "$GENERATE processing is disabled.");
            return false;
        }

        if (record.Tokens.Count < 5)
        {
            AddDirectiveDiagnostic(
                record,
                state,
                "$GENERATE requires a range, owner, type, and record data.");
            return false;
        }

        DnsGenerateRangeResult range = DnsGenerateDirective.ParseRange(
            record.Tokens[1].Text);
        if (!range.Success)
        {
            AddDirectiveDiagnostic(record, state, range.Reason);
            return false;
        }

        if (range.Count > state.Options.MaximumGeneratedRecords)
        {
            AddResourceDiagnostic(
                record,
                state,
                "The generated record count exceeds its configured limit.");
            return false;
        }

        long remainingRecords = state.Options.MaximumRecords -
            state.Result.RecordsLoaded;
        if (range.Count > remainingRecords)
        {
            AddResourceDiagnostic(
                record,
                state,
                "The generated records exceed the zone record limit.");
            return false;
        }

        long iterator = range.Start;
        long emitted = 0;
        while (emitted < range.Count)
        {
            DnsGenerateTextResult expansion = ExpandGenerateRecord(
                record, iterator);
            if (!expansion.Success)
            {
                AddDirectiveDiagnostic(record, state, expansion.Reason);
                return false;
            }

            DnsMasterFileTokenizeResult generated =
                DnsMasterFileTokenizer.Tokenize(
                    expansion.Text, record.SourcePath, state.Options);
            if (!generated.Success || generated.Records.Count != 1)
            {
                if (generated.Diagnostics.Count > 0)
                {
                    CopyDiagnostics(
                        generated.Diagnostics, state.Result.Diagnostics);
                }
                else
                {
                    AddDirectiveDiagnostic(
                        record,
                        state,
                        "A generated resource record is invalid.");
                }

                return false;
            }

            DnsMasterFileRecord generatedRecord = generated.Records[0];
            RelocateGeneratedRecord(generatedRecord, record);
            if (!ProcessResourceRecord(generatedRecord, state))
                return false;
            emitted = emitted + 1;
            iterator = iterator + range.Step;
        }

        return true;
    }

    static DnsGenerateTextResult ExpandGenerateRecord(
        DnsMasterFileRecord record,
        long iterator)
    {
        DnsGenerateTextResult result = new DnsGenerateTextResult();
        StringBuilder builder = new StringBuilder();
        int index = 2;
        while (index < record.Tokens.Count)
        {
            DnsMasterFileToken token = record.Tokens[index];
            string template = token.RawText;
            DnsGenerateTextResult expanded =
                DnsGenerateDirective.Substitute(template, iterator);
            if (!expanded.Success) return expanded;
            if (builder.Length > 0) builder.Append(' ');
            builder.Append(expanded.Text);
            index = index + 1;
        }

        result.Success = true;
        result.Text = builder.ToString();
        return result;
    }

    static void RelocateGeneratedRecord(
        DnsMasterFileRecord generated,
        DnsMasterFileRecord directive)
    {
        generated.SourcePath = directive.SourcePath;
        generated.Line = directive.Line;
        generated.Column = directive.Column;
        generated.OwnerOmitted = false;
        int index = 0;
        while (index < generated.Tokens.Count)
        {
            generated.Tokens[index].SourcePath = directive.SourcePath;
            generated.Tokens[index].Line = directive.Line;
            generated.Tokens[index].Column = directive.Column;
            index = index + 1;
        }
    }

    static DnsMasterPathResult ResolveIncludePath(
        string fileName,
        string? sourcePath,
        DnsZoneLoadOptions options)
    {
        DnsMasterPathResult result = new DnsMasterPathResult();
        if (fileName.Length == 0 || ContainsNullCharacter(fileName))
        {
            result.Reason = "The include file name is invalid.";
            return result;
        }

        string candidate = fileName;
        if (!Path.IsPathFullyQualified(candidate))
        {
            string? baseDirectory = options.BaseDirectory;
            if ((baseDirectory == null || baseDirectory.Length == 0) &&
                sourcePath != null)
            {
                baseDirectory = Path.GetDirectoryName(sourcePath);
            }

            if (baseDirectory == null || baseDirectory.Length == 0)
                baseDirectory = Directory.GetCurrentDirectory();
            candidate = Path.Combine(baseDirectory, candidate);
        }

        if (!File.Exists(candidate))
        {
            result.Reason = "The include file does not exist.";
            return result;
        }

        result.Success = true;
        result.Path = Path.GetFullPath(candidate);
        return result;
    }

    static bool ContainsNullCharacter(string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == '\0') return true;
            index = index + 1;
        }

        return false;
    }

    static bool ContainsPath(List<string> paths, string path)
    {
        System.StringComparison comparison =
            System.StringComparison.Ordinal;
        if (Path.DirectorySeparatorChar == '\\')
            comparison = System.StringComparison.OrdinalIgnoreCase;
        int index = 0;
        while (index < paths.Count)
        {
            if (string.Equals(paths[index], path, comparison)) return true;
            index = index + 1;
        }

        return false;
    }

    static void AddDirectiveDiagnostic(
        DnsMasterFileRecord record,
        DnsMasterFileParseState state,
        string reason)
    {
        state.Result.Diagnostics.Add(CreateDiagnostic(
            DnsDiagnosticCode.InvalidZone,
            reason,
            record.SourcePath,
            record.Tokens[0].Line,
            record.Tokens[0].Column));
    }

    static void AddResourceDiagnostic(
        DnsMasterFileRecord record,
        DnsMasterFileParseState state,
        string reason)
    {
        state.Result.Diagnostics.Add(CreateDiagnostic(
            DnsDiagnosticCode.ResourceLimitExceeded,
            reason,
            record.SourcePath,
            record.Tokens[0].Line,
            record.Tokens[0].Column));
    }

    static DnsMasterRecordHeaderResult ParseHeader(
        DnsMasterFileRecord record,
        DnsMasterFileParseState state)
    {
        DnsMasterRecordHeaderResult result =
            new DnsMasterRecordHeaderResult();
        int index = 0;
        if (record.OwnerOmitted)
        {
            if (state.PreviousOwner == null)
            {
                result.Diagnostic = CreateDiagnostic(
                    DnsDiagnosticCode.InvalidZone,
                    "An omitted owner requires a preceding resource record.",
                    record.SourcePath,
                    record.Line,
                    record.Column);
                return result;
            }

            result.Owner = RecordSetOperations.CopyName(
                state.PreviousOwner, false);
        }
        else
        {
            if (record.Tokens.Count == 0)
            {
                result.Diagnostic = CreateDiagnostic(
                    DnsDiagnosticCode.InvalidZone,
                    "A resource record requires an owner name.",
                    record.SourcePath,
                    record.Line,
                    record.Column);
                return result;
            }

            DnsTextOptions textOptions = CreateTextOptions(state.Origin);
            DnsTextResult owner = DnsPresentationNameText.ParseNameToken(
                record.Tokens[0].RawText, textOptions);
            if (!owner.Success || owner.Name == null ||
                !owner.Name.IsAbsolute)
            {
                result.Diagnostic = CreateDiagnostic(
                    DnsDiagnosticCode.InvalidZone,
                    "A zone owner must resolve to an absolute DNS name.",
                    record.Tokens[0].SourcePath,
                    record.Tokens[0].Line,
                    record.Tokens[0].Column);
                return result;
            }

            result.Owner = owner.Name;
            index = 1;
        }

        result.ClassValue = state.ClassValue;
        if (state.HasDefaultTimeToLive)
            result.TimeToLive = state.DefaultTimeToLive;
        bool hasTimeToLive = state.HasDefaultTimeToLive;
        bool hasExplicitClass = false;
        bool hasExplicitTimeToLive = false;
        while (index < record.Tokens.Count)
        {
            string token = record.Tokens[index].Text;
            if (!hasExplicitClass)
            {
                DnsTextNumberResult recordClass =
                    DnsMnemonicText.ParseClass(token, true);
                if (recordClass.Success)
                {
                    result.ClassValue = (int)recordClass.Value;
                    hasExplicitClass = true;
                    result.HasExplicitClass = true;
                    index = index + 1;
                    continue;
                }
            }

            if (!hasExplicitTimeToLive)
            {
                DnsTextNumberResult ttl =
                    DnsTimeToLiveText.ParseTimeToLive(token);
                if (ttl.Success)
                {
                    result.TimeToLive = (uint)ttl.Value;
                    hasTimeToLive = true;
                    hasExplicitTimeToLive = true;
                    result.HasExplicitTimeToLive = true;
                    index = index + 1;
                    continue;
                }
            }

            DnsTextNumberResult recordType =
                DnsMnemonicText.ParseRecordType(token, true);
            if (recordType.Success)
            {
                result.RecordTypeValue = (int)recordType.Value;
                result.RecordDataStart = index + 1;
                if (!hasTimeToLive)
                {
                    result.Diagnostic = CreateDiagnostic(
                        DnsDiagnosticCode.InvalidZone,
                        "A resource record requires a TTL or configured default TTL.",
                        record.Tokens[index].SourcePath,
                        record.Tokens[index].Line,
                        record.Tokens[index].Column);
                    return result;
                }

                result.Success = true;
                return result;
            }

            result.Diagnostic = CreateDiagnostic(
                DnsDiagnosticCode.InvalidText,
                "The resource-record class, TTL, or type field is invalid.",
                record.Tokens[index].SourcePath,
                record.Tokens[index].Line,
                record.Tokens[index].Column);
            return result;
        }

        result.Diagnostic = CreateDiagnostic(
            DnsDiagnosticCode.InvalidZone,
            "A resource record requires a type field.",
            record.SourcePath,
            record.Line,
            record.Column);
        return result;
    }

    static DnsTextResult ParseCompleteRecord(
        DnsMasterFileRecord logicalRecord,
        DnsMasterRecordHeaderResult header,
        DnsName? origin)
    {
        DnsName? ownerName = header.Owner;
        if (ownerName == null)
        {
            DnsTextResult missingOwner = new DnsTextResult();
            DnsDiagnostic diagnostic = new DnsDiagnostic();
            diagnostic.Code = DnsDiagnosticCode.InvalidZone;
            diagnostic.Reason = "A resource record requires an owner name.";
            missingOwner.Diagnostics.Add(diagnostic);
            return missingOwner;
        }

        DnsTextResult owner = PresentationDnsName.FormatName(ownerName);
        if (!owner.Success) return owner;
        StringBuilder builder = new StringBuilder();
        builder.Append(owner.Text);
        builder.Append(' ');
        builder.Append(header.TimeToLive.ToString(
            CultureInfo.InvariantCulture));
        builder.Append(" CLASS");
        builder.Append(header.ClassValue.ToString(
            CultureInfo.InvariantCulture));
        builder.Append(" TYPE");
        builder.Append(header.RecordTypeValue.ToString(
            CultureInfo.InvariantCulture));
        int index = header.RecordDataStart;
        while (index < logicalRecord.Tokens.Count)
        {
            builder.Append(' ');
            builder.Append(logicalRecord.Tokens[index].RawText);
            index = index + 1;
        }

        return DnsResourceRecordText.ParseResourceRecord(
            builder.ToString(), CreateTextOptions(origin));
    }

    static void AddRecord(DnsZone zone, DnsResourceRecord record)
    {
        DnsZoneNode? node = FindNode(zone.Nodes, record.Name);
        if (node == null)
        {
            node = new DnsZoneNode();
            node.Name = RecordSetOperations.CopyName(record.Name, false);
            zone.Nodes.Add(node);
        }

        int typeValue = DnsHeaderCodec.ResolveCode(
            record.RecordTypeValue, (int)record.RecordType);
        int classValue = DnsHeaderCodec.ResolveCode(
            record.ClassValue, (int)record.Class);
        DnsResourceRecordSet? set = FindRecordSet(
            node.RecordSets, typeValue, classValue);
        if (set == null)
        {
            set = new DnsResourceRecordSet();
            set.Name = RecordSetOperations.CopyName(record.Name, false);
            set.RecordType = (DnsRecordType)typeValue;
            set.RecordTypeValue = typeValue;
            set.Class = (DnsClass)classValue;
            set.ClassValue = classValue;
            set.TimeToLive = record.TimeToLive;
            node.RecordSets.Add(set);
        }

        set.Records.Add(record);
        if (record.TimeToLive < set.TimeToLive)
            set.TimeToLive = record.TimeToLive;
    }

    static DnsZoneNode? FindNode(
        List<DnsZoneNode> nodes,
        DnsName name)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            DnsNameOperationResult comparison =
                DnsNameOperations.NamesEqual(nodes[index].Name, name);
            if (comparison.Success && comparison.BooleanValue)
                return nodes[index];
            index = index + 1;
        }

        return null;
    }

    static DnsResourceRecordSet? FindRecordSet(
        List<DnsResourceRecordSet> sets,
        int typeValue,
        int classValue)
    {
        int index = 0;
        while (index < sets.Count)
        {
            DnsResourceRecordSet set = sets[index];
            if (set.RecordTypeValue == typeValue &&
                set.ClassValue == classValue)
                return set;
            index = index + 1;
        }

        return null;
    }

    static DnsMasterFileParseState CreateState(
        DnsZoneLoadOptions options,
        DnsZoneLoadResult result)
    {
        DnsMasterFileParseState state = new DnsMasterFileParseState();
        state.Options = options;
        state.Result = result;
        state.Origin = options.Origin;
        state.HasDefaultTimeToLive = options.HasDefaultTimeToLive;
        state.DefaultTimeToLiveIsDirective =
            options.HasDefaultTimeToLive;
        state.DefaultTimeToLive = options.DefaultTimeToLive;
        state.ClassValue = DnsHeaderCodec.ResolveCode(
            options.ClassValue, (int)options.Class);
        DnsZone zone = new DnsZone();
        zone.ClassValue = state.ClassValue;
        zone.Class = (DnsClass)zone.ClassValue;
        zone.HasDefaultTimeToLive = options.HasDefaultTimeToLive;
        zone.DefaultTimeToLive = options.DefaultTimeToLive;
        if (options.Origin != null)
            zone.Apex = RecordSetOperations.CopyName(
                options.Origin, false);
        state.Zone = zone;
        return state;
    }

    static DnsTextOptions CreateTextOptions(DnsName? origin)
    {
        DnsTextOptions result = new DnsTextOptions();
        result.Origin = origin;
        result.UseMnemonicCodes = true;
        return result;
    }

    static DnsZoneDiagnostic? ValidateOptions(
        DnsZoneLoadOptions options,
        string? sourcePath)
    {
        if (options == null)
        {
            return CreateDiagnostic(
                DnsDiagnosticCode.ValueOutOfRange,
                "Zone load options are required.",
                sourcePath,
                1,
                1);
        }

        int classValue = DnsHeaderCodec.ResolveCode(
            options.ClassValue, (int)options.Class);
        if (classValue < 0 || classValue > 65535 ||
            options.MaximumRecords < 0 ||
            options.MaximumIncludeDepth < 0 ||
            options.MaximumGeneratedRecords < 0)
        {
            return CreateDiagnostic(
                DnsDiagnosticCode.ValueOutOfRange,
                "Zone class and resource limits are outside their supported ranges.",
                sourcePath,
                1,
                1);
        }

        if (options.Origin != null)
        {
            RawDnsNameResult origin = RawDnsName.ValidateName(options.Origin);
            if (!origin.Success || origin.Name == null ||
                !origin.Name.IsAbsolute)
            {
                return CreateDiagnostic(
                    DnsDiagnosticCode.InvalidZone,
                    "The initial zone origin must be an absolute DNS name.",
                    sourcePath,
                    1,
                    1);
            }
        }

        return null;
    }

    static void AddTextDiagnostics(
        DnsTextResult text,
        DnsZoneLoadResult result,
        DnsMasterFileRecord record,
        int tokenIndex)
    {
        int line = record.Line;
        int column = record.Column;
        if (tokenIndex >= 0 && tokenIndex < record.Tokens.Count)
        {
            line = record.Tokens[tokenIndex].Line;
            column = record.Tokens[tokenIndex].Column;
        }

        if (text.Diagnostics.Count == 0)
        {
            result.Diagnostics.Add(CreateDiagnostic(
                DnsDiagnosticCode.InvalidText,
                "The resource record could not be parsed.",
                record.SourcePath,
                line,
                column));
            return;
        }

        int index = 0;
        while (index < text.Diagnostics.Count)
        {
            DnsDiagnostic source = text.Diagnostics[index];
            result.Diagnostics.Add(CreateDiagnostic(
                source.Code,
                source.Reason,
                record.SourcePath,
                line,
                column));
            index = index + 1;
        }
    }

    static void CopyDiagnostics(
        List<DnsZoneDiagnostic> source,
        List<DnsZoneDiagnostic> destination)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }

    static DnsZoneDiagnostic CreateDiagnostic(
        DnsDiagnosticCode code,
        string reason,
        string? sourcePath,
        int line,
        int column)
    {
        DnsZoneDiagnostic diagnostic = new DnsZoneDiagnostic();
        diagnostic.Code = code;
        diagnostic.Reason = reason;
        diagnostic.SourcePath = sourcePath;
        diagnostic.HasLine = true;
        diagnostic.Line = line;
        diagnostic.HasColumn = true;
        diagnostic.Column = column;
        return diagnostic;
    }
}
