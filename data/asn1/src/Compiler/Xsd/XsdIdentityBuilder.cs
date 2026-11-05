namespace asn1;

internal class XsdIdentityBuilder
{
    internal string DocumentIdentity(string resolverUri)
    {
        return "xsd:document:" + this.EscapeIdentity(resolverUri);
    }

    internal string InstanceIdentity(
        string documentId,
        string targetNamespace)
    {
        return documentId + "/target/"
            + this.EscapeIdentity(targetNamespace);
    }

    internal string ComponentIdentity(
        string instanceId,
        string kind,
        string name,
        int duplicateOrdinal)
    {
        string result = instanceId + "/component/"
            + this.EscapeIdentity(kind) + "/"
            + this.EscapeIdentity(name);
        return duplicateOrdinal == 0
            ? result
            : result + "/duplicate/"
                + duplicateOrdinal.ToString(
                    System.Globalization.CultureInfo.InvariantCulture);
    }

    internal string EscapeIdentity(string value)
    {
        if (value.Length == 0)
        {
            return "_empty";
        }
        System.Text.StringBuilder result =
            new System.Text.StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character >= 'A' && character <= 'Z'
                || character >= 'a' && character <= 'z'
                || character >= '0' && character <= '9'
                || character == '-'
                || character == '.')
            {
                result.Append(character);
            }
            else
            {
                result.Append('_');
                result.Append(((int)character).ToString(
                    "X4",
                    System.Globalization.CultureInfo.InvariantCulture));
            }
            index = index + 1;
        }
        return result.ToString();
    }
}
