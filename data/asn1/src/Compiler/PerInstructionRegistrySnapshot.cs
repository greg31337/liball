namespace asn1;

internal class PerInstructionRegistrySnapshot : IPerInstructionRegistry
{
    internal AsnPerInstructionRegistration[] Registrations = [];
    internal IPerInstructionHandler[] Handlers = [];

    public void RegisterHandler(
        AsnPerInstructionRegistration registration,
        IPerInstructionHandler handler)
    {
        throw new System.InvalidOperationException(
            "A PER instruction registry snapshot cannot be modified.");
    }

    public IPerInstructionHandler? ResolveHandler(string keyword)
    {
        int index = 0;
        while (index < this.Registrations.Length)
        {
            if (this.Registrations[index].Keyword == keyword)
            {
                return this.Handlers[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal AsnPerInstructionRegistration? ResolveRegistration(string keyword)
    {
        int index = 0;
        while (index < this.Registrations.Length)
        {
            if (this.Registrations[index].Keyword == keyword)
            {
                return this.Registrations[index];
            }

            index = index + 1;
        }

        return null;
    }

    public AsnPerInstructionRegistration[] GetRegistrations()
    {
        AsnPerInstructionRegistration[] result =
            new AsnPerInstructionRegistration[this.Registrations.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = this.CloneRegistration(this.Registrations[index]);
            index = index + 1;
        }

        return result;
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
}
