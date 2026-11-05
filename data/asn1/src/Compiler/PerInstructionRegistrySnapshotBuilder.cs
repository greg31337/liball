namespace asn1;

internal class PerInstructionRegistrySnapshotBuilder
{
    internal PerInstructionRegistrySnapshotResult CreateSnapshot(
        IPerInstructionRegistry? registry)
    {
        if (registry == null)
        {
            return new PerInstructionRegistrySnapshotResult();
        }

        AsnPerInstructionRegistration[]? supplied = registry.GetRegistrations();
        if (supplied == null)
        {
            return new PerInstructionRegistrySnapshotResult
            {
                Diagnostics =
                [
                    this.CreateDiagnostic(
                        "ASNPER0001",
                        "The PER instruction registry returned no registration collection.")
                ]
            };
        }

        AsnPerInstructionRegistration[] registrations =
            new AsnPerInstructionRegistration[supplied.Length];
        int index = 0;
        while (index < supplied.Length)
        {
            AsnPerInstructionRegistration? registration = supplied[index];
            registrations[index] = registration == null
                ? new AsnPerInstructionRegistration()
                : this.CloneRegistration(registration);
            index = index + 1;
        }

        this.SortRegistrations(registrations);
        System.Collections.Generic.List<AsnPerInstructionRegistration> valid =
            new System.Collections.Generic.List<AsnPerInstructionRegistration>();
        System.Collections.Generic.List<IPerInstructionHandler> handlers =
            new System.Collections.Generic.List<IPerInstructionHandler>();
        System.Collections.Generic.List<AsnDiagnostic> diagnostics =
            new System.Collections.Generic.List<AsnDiagnostic>();
        index = 0;
        while (index < registrations.Length)
        {
            AsnPerInstructionRegistration registration = registrations[index];
            if (registration.Keyword.Length == 0)
            {
                diagnostics.Add(this.CreateDiagnostic(
                    "ASNPER0002",
                    "A PER instruction registration requires an identifying keyword."));
            }
            else if (index > 0
                && registrations[index - 1].Keyword == registration.Keyword)
            {
                diagnostics.Add(this.CreateDiagnostic(
                    "ASNPER0003",
                    "Duplicate PER instruction identifying keyword: "
                        + registration.Keyword));
            }
            else if (registration.HandlerId.Length == 0)
            {
                diagnostics.Add(this.CreateDiagnostic(
                    "ASNPER0004",
                    "PER instruction registration requires a stable handler identity: "
                        + registration.Keyword));
            }
            else
            {
                IPerInstructionHandler? handler = registry.ResolveHandler(
                    registration.Keyword);
                if (handler == null)
                {
                    diagnostics.Add(this.CreateDiagnostic(
                        "ASNPER0005",
                        "PER instruction registration has no handler: "
                            + registration.Keyword));
                }
                else
                {
                    valid.Add(registration);
                    handlers.Add(handler);
                }
            }

            index = index + 1;
        }

        return new PerInstructionRegistrySnapshotResult
        {
            Snapshot = new PerInstructionRegistrySnapshot
            {
                Registrations = valid.ToArray(),
                Handlers = handlers.ToArray()
            },
            Diagnostics = diagnostics.ToArray()
        };
    }

    internal void SortRegistrations(AsnPerInstructionRegistration[] registrations)
    {
        int insertionIndex = 1;
        while (insertionIndex < registrations.Length)
        {
            AsnPerInstructionRegistration current = registrations[insertionIndex];
            int position = insertionIndex;
            while (position > 0
                && this.CompareRegistrations(
                    registrations[position - 1],
                    current) > 0)
            {
                registrations[position] = registrations[position - 1];
                position = position - 1;
            }

            registrations[position] = current;
            insertionIndex = insertionIndex + 1;
        }
    }

    internal int CompareRegistrations(
        AsnPerInstructionRegistration left,
        AsnPerInstructionRegistration right)
    {
        int keyword = System.String.CompareOrdinal(left.Keyword, right.Keyword);
        return keyword != 0
            ? keyword
            : System.String.CompareOrdinal(left.HandlerId, right.HandlerId);
    }

    internal AsnPerInstructionRegistration CloneRegistration(
        AsnPerInstructionRegistration source)
    {
        return new AsnPerInstructionRegistration
        {
            Keyword = source.Keyword,
            HandlerId = source.HandlerId,
            SupportsAligned = source.SupportsAligned,
            SupportsUnaligned = source.SupportsUnaligned,
            Reversible = source.Reversible,
            ConflictingKeywords = this.CopyStrings(source.ConflictingKeywords)
        };
    }

    internal string[] CopyStrings(string[] source)
    {
        string[] result = new string[source.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = source[index];
            index = index + 1;
        }

        return result;
    }

    internal AsnDiagnostic CreateDiagnostic(string code, string message)
    {
        return new AsnDiagnostic
        {
            Severity = AsnDiagnosticSeverity.Error,
            Category = AsnDiagnosticCategory.Encoding,
            Code = code,
            Message = message
        };
    }
}
