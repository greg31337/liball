namespace asn1;

internal class CSharpNameScope
{
    internal bool CaseInsensitive;
    internal System.Collections.Generic.List<string> ReservedNames =
        new System.Collections.Generic.List<string>();

    internal CSharpNameScope(bool caseInsensitive)
    {
        this.CaseInsensitive = caseInsensitive;
    }

    internal CSharpNameReservation ReserveName(string baseName)
    {
        if (baseName == null)
        {
            throw new System.ArgumentNullException("baseName");
        }

        string candidate = baseName;
        int collisionIndex = 1;
        while (this.ContainsReservedName(candidate))
        {
            collisionIndex = collisionIndex + 1;
            candidate = baseName + "_" + collisionIndex.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
        }

        this.ReservedNames.Add(candidate);
        return new CSharpNameReservation
        {
            Name = candidate,
            CollisionIndex = collisionIndex
        };
    }

    internal bool ContainsReservedName(string candidate)
    {
        int index = 0;
        while (index < this.ReservedNames.Count)
        {
            System.StringComparison comparison = this.CaseInsensitive
                ? System.StringComparison.OrdinalIgnoreCase
                : System.StringComparison.Ordinal;
            if (string.Equals(
                this.ReservedNames[index],
                candidate,
                comparison))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
