using System.Globalization;
using System.Net;
using System.Text;

namespace tftp;

internal static class TftpServerRequestIdentity
{
    internal static string CreateIdentity(
        IPEndPoint clientEndPoint,
        TftpPacket requestPacket)
    {
        StringBuilder builder = new StringBuilder();
        AppendPart(builder, clientEndPoint.Address.ToString());
        AppendPart(
            builder,
            clientEndPoint.Port.ToString(CultureInfo.InvariantCulture));
        AppendPart(
            builder,
            ((int)requestPacket.Opcode).ToString(CultureInfo.InvariantCulture));
        AppendPart(builder, requestPacket.Filename);
        AppendPart(builder, requestPacket.Mode.ToLowerInvariant());
        int optionIndex = 0;
        while (optionIndex < requestPacket.Options.Count)
        {
            TftpOption option = requestPacket.Options[optionIndex];
            AppendPart(builder, option.Name.ToLowerInvariant());
            AppendPart(builder, option.Value);
            optionIndex += 1;
        }

        return builder.ToString();
    }

    internal static void AppendPart(StringBuilder builder, string value)
    {
        builder.Append(value.Length.ToString(CultureInfo.InvariantCulture));
        builder.Append(':');
        builder.Append(value);
    }
}
