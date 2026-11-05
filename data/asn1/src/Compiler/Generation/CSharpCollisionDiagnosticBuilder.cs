namespace asn1;

internal class CSharpCollisionDiagnosticBuilder
{
    internal AsnDiagnostic[] BuildDiagnostics(
        CSharpNameMapping[] mappings,
        AsnSourceSpan[] sourceSpans)
    {
        System.Collections.Generic.List<AsnDiagnostic> diagnostics =
            new System.Collections.Generic.List<AsnDiagnostic>();
        System.Collections.Generic.List<string> identities =
            new System.Collections.Generic.List<string>();
        int index = 0;
        while (index < mappings.Length)
        {
            CSharpNameMapping mapping = mappings[index];
            string identity = mapping.StableId + "|" + mapping.GeneratedName;
            if (mapping.CollisionIndex > 1 && !identities.Contains(identity))
            {
                identities.Add(identity);
                diagnostics.Add(new AsnDiagnostic
                {
                    Severity = AsnDiagnosticSeverity.Warning,
                    Category = AsnDiagnosticCategory.Generation,
                    Code = "ASNGEN0001",
                    Message = "Generated name '" + mapping.GeneratedName
                        + "' disambiguates source name '" + mapping.SourceName
                        + "' after a collision.",
                    Span = sourceSpans.Length == 0
                        ? null
                        : this.CloneSpan(sourceSpans[0])
                });
            }

            index = index + 1;
        }

        return diagnostics.ToArray();
    }

    internal AsnSourceSpan CloneSpan(AsnSourceSpan span)
    {
        return new AsnSourceSpan
        {
            Start = new AsnSourcePosition
            {
                SourceName = span.Start.SourceName,
                Offset = span.Start.Offset,
                Line = span.Start.Line,
                Column = span.Start.Column
            },
            End = new AsnSourcePosition
            {
                SourceName = span.End.SourceName,
                Offset = span.End.Offset,
                Line = span.End.Line,
                Column = span.End.Column
            }
        };
    }
}
