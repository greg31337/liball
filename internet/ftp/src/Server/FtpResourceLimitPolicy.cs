namespace ftp;

internal static class FtpResourceLimitPolicy
{
    internal static bool AreValid(FtpResourceLimits? limits)
    {
        return limits != null &&
            (!limits.HasStorageQuotaBytes ? limits.StorageQuotaBytes == 0 :
                limits.StorageQuotaBytes >= 0) &&
            (!limits.HasTransferByteCeiling ? limits.TransferByteCeiling == 0 :
                limits.TransferByteCeiling >= 0) &&
            (!limits.HasReadBytesPerSecond ? limits.ReadBytesPerSecond == 0 :
                limits.ReadBytesPerSecond > 0) &&
            (!limits.HasWriteBytesPerSecond ? limits.WriteBytesPerSecond == 0 :
                limits.WriteBytesPerSecond > 0) &&
            (!limits.HasListingEntryCap ? limits.ListingEntryCap == 0 :
                limits.ListingEntryCap > 0) &&
            (!limits.HasListingByteCap ? limits.ListingByteCap == 0 :
                limits.ListingByteCap >= 0);
    }

    internal static bool IsLimitFailure(System.Exception? failure)
    {
        System.Exception? current = failure;
        while (current != null)
        {
            if (current as FtpResourceLimitException != null)
            {
                return true;
            }
            current = current.InnerException;
        }
        return false;
    }
}
