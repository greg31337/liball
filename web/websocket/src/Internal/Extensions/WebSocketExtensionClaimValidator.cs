namespace websocket;

internal static class WebSocketExtensionClaimValidator
{
    internal static bool ValidateClaims(
        WebSocketExtensionNegotiation[] negotiations)
    {
        if (negotiations == null)
        {
            return false;
        }

        bool rsv1Claimed = false;
        bool rsv2Claimed = false;
        bool rsv3Claimed = false;
        bool[] opcodeClaimed = new bool[16];
        int negotiationIndex = 0;
        while (negotiationIndex < negotiations.Length)
        {
            WebSocketExtensionNegotiation negotiation =
                negotiations[negotiationIndex];
            if (negotiation == null || negotiation.ClaimedOpcodes == null)
            {
                return false;
            }

            if (negotiation.Status == WebSocketExtensionStatus.Negotiated)
            {
                if (negotiation.ClaimsRsv1)
                {
                    if (rsv1Claimed)
                    {
                        return false;
                    }

                    rsv1Claimed = true;
                }

                if (negotiation.ClaimsRsv2)
                {
                    if (rsv2Claimed)
                    {
                        return false;
                    }

                    rsv2Claimed = true;
                }

                if (negotiation.ClaimsRsv3)
                {
                    if (rsv3Claimed)
                    {
                        return false;
                    }

                    rsv3Claimed = true;
                }

                int opcodeIndex = 0;
                while (opcodeIndex < negotiation.ClaimedOpcodes.Length)
                {
                    int opcode = (int)negotiation.ClaimedOpcodes[opcodeIndex];
                    bool reserved = (opcode >= 3 && opcode <= 7) ||
                        (opcode >= 11 && opcode <= 15);
                    if (!reserved || opcodeClaimed[opcode])
                    {
                        return false;
                    }

                    opcodeClaimed[opcode] = true;
                    opcodeIndex = opcodeIndex + 1;
                }
            }

            negotiationIndex = negotiationIndex + 1;
        }

        return true;
    }
}
