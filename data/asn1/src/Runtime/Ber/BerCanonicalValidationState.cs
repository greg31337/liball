namespace asn1;

internal class BerCanonicalValidationState
{
    internal bool HasViolation;
    internal bool Invalid;
    internal int Offset = int.MaxValue;
    internal string Code = "";
    internal string Message = "";

    internal void RecordViolation(
        int offset,
        string code,
        string message,
        bool invalid)
    {
        if (this.HasViolation
            && (offset > this.Offset
                || offset == this.Offset && (!invalid || this.Invalid)))
        {
            return;
        }

        this.HasViolation = true;
        this.Invalid = invalid;
        this.Offset = offset;
        this.Code = code;
        this.Message = message;
    }
}
