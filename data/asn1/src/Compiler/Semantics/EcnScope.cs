namespace asn1;

internal class EcnScope
{
    internal EcnScope? Parent;
    internal EcnSymbol? Owner;
    internal EcnSymbol[] Symbols = [];

    internal EcnSymbol? FindLocalSymbol(string name)
    {
        if (name == null)
        {
            throw new System.ArgumentNullException("name");
        }

        int index = 0;
        while (index < this.Symbols.Length)
        {
            if (this.Symbols[index].Name == name)
            {
                return this.Symbols[index];
            }

            index = index + 1;
        }

        return null;
    }
}
