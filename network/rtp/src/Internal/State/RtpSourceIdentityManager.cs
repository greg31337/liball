namespace rtp;

internal class RtpSourceIdentityManager
{
    internal RtpLocalSourceTable localSourceTable;
    internal RtpSourceTable remoteSourceTable;

    internal RtpSourceIdentityManager(
        RtpLocalSourceTable localSourceTable,
        RtpSourceTable remoteSourceTable)
    {
        if (localSourceTable == null || remoteSourceTable == null)
        {
            throw new RtpConfigurationException(
                "Local and remote RTP source tables are required.");
        }

        this.localSourceTable = localSourceTable;
        this.remoteSourceTable = remoteSourceTable;
    }

    internal RtpSourceIdentityUpdate InspectIncomingSourceIdentity(
        uint sourceIdentifier,
        RtpPacketContext context)
    {
        ValidateSourceIdentityContext(context);
        RtpSourceIdentityUpdate update = new RtpSourceIdentityUpdate();
        update.previousSourceIdentifier = sourceIdentifier;
        update.currentSourceIdentifier = sourceIdentifier;

        RtpLocalSourceState? localSource =
            localSourceTable.FindLocalSourceState(sourceIdentifier);
        if (localSource != null)
        {
            RtpSourceSnapshot localSnapshot =
                localSource.CreateLocalSourceSnapshot();
            if (localSnapshot.TransportIdentity ==
                context.TransportIdentity &&
                localSnapshot.TransportIdentity.Length > 0)
            {
                update.loopDetected = true;
                update.sourceSnapshot = localSnapshot;
                return update;
            }

            uint[] remoteSourceIdentifiers =
                remoteSourceTable.GetRemoteSourceIdentifiers();
            RtpLocalSourceState replacement =
                localSourceTable.ReselectCollidingLocalSource(
                    sourceIdentifier,
                    remoteSourceIdentifiers);
            RtcpGoodbye goodbye = new RtcpGoodbye();
            goodbye.Sources = new uint[] { sourceIdentifier };
            update.collisionDetected = true;
            update.currentSourceIdentifier =
                replacement.sourceIdentifier;
            update.goodbye = goodbye;
            update.sourceSnapshot =
                replacement.CreateLocalSourceSnapshot();
            return update;
        }

        RtpSourceSnapshot? remoteSnapshot =
            remoteSourceTable.FindRemoteSourceSnapshot(sourceIdentifier);
        if (remoteSnapshot != null &&
            remoteSnapshot.TransportIdentity.Length > 0 &&
            remoteSnapshot.TransportIdentity != context.TransportIdentity)
        {
            update.collisionDetected = true;
            update.sourceSnapshot =
                remoteSourceTable.MarkRemoteSourceCollision(
                    sourceIdentifier,
                    context.TransportIdentity);
        }

        return update;
    }

    internal void ValidateSourceIdentityContext(RtpPacketContext context)
    {
        if (context == null || context.TransportIdentity == null ||
            context.TransportIdentity.Length == 0)
        {
            throw new RtpConfigurationException(
                "An incoming RTP transport identity is required.");
        }
    }
}
