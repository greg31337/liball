namespace iso20022;

internal class CompatibilityImpact
{
    internal bool ReaderCompatible = true;
    internal bool WriterCompatible = true;
    internal bool SchemaCompatible = true;
    internal bool Lossless = true;
    internal bool SemanticallyCompatible = true;
    internal bool Warning;
    internal bool Conditional;
    internal bool DefinitiveIncompatibility;
    internal decimal Confidence = 1m;
    internal string Rationale = string.Empty;
}
