using System;

namespace rtp;

internal static class RtcpIntervalCalculator
{
    internal static double CalculateIntervalSeconds(
        RtcpSchedulerConfiguration configuration,
        int memberCount,
        int senderCount,
        bool localIsSender,
        int averagePacketSize,
        bool isInitial,
        double randomValue)
    {
        double intervalSeconds = CalculateDeterministicIntervalSeconds(
            configuration,
            memberCount,
            senderCount,
            localIsSender,
            averagePacketSize,
            isInitial);
        ValidateIntervalRandomValue(randomValue);
        intervalSeconds = intervalSeconds * (randomValue + 0.5) /
            RtcpProtocolConstants.IntervalCompensation;
        if (double.IsNaN(intervalSeconds) || double.IsInfinity(intervalSeconds))
        {
            throw new RtpConfigurationException(
                "The calculated RTCP interval cannot be represented.");
        }

        return intervalSeconds;
    }

    internal static double CalculateDeterministicIntervalSeconds(
        RtcpSchedulerConfiguration configuration,
        int memberCount,
        int senderCount,
        bool localIsSender,
        int averagePacketSize,
        bool isInitial)
    {
        RtpConfigurationValidator.ValidateRtcpSchedulerConfiguration(configuration);
        ValidateDeterministicIntervalInputs(
            memberCount,
            senderCount,
            localIsSender,
            averagePacketSize);

        int reportingMemberCount = memberCount;
        double bandwidthFraction = 1.0;
        if (senderCount <= memberCount * configuration.SenderBandwidthFraction)
        {
            if (localIsSender)
            {
                reportingMemberCount = senderCount;
                bandwidthFraction = configuration.SenderBandwidthFraction;
            }
            else
            {
                reportingMemberCount = memberCount - senderCount;
                bandwidthFraction = configuration.ReceiverBandwidthFraction;
            }
        }

        double intervalSeconds =
            averagePacketSize * (double)reportingMemberCount * 8.0 /
            configuration.SessionBandwidthBitsPerSecond /
            configuration.RtcpBandwidthFraction /
            bandwidthFraction;
        double minimumIntervalSeconds = configuration.MinimumIntervalSeconds;
        if (isInitial)
        {
            minimumIntervalSeconds = configuration.InitialMinimumIntervalSeconds;
        }

        if (intervalSeconds < minimumIntervalSeconds)
        {
            intervalSeconds = minimumIntervalSeconds;
        }

        if (double.IsNaN(intervalSeconds) || double.IsInfinity(intervalSeconds))
        {
            throw new RtpConfigurationException(
                "The calculated RTCP interval cannot be represented.");
        }

        return intervalSeconds;
    }

    internal static void ValidateDeterministicIntervalInputs(
        int memberCount,
        int senderCount,
        bool localIsSender,
        int averagePacketSize)
    {
        if (memberCount <= 0)
        {
            throw new RtpConfigurationException(
                "The RTCP member count must be positive.");
        }

        if (senderCount < 0 || senderCount > memberCount)
        {
            throw new RtpConfigurationException(
                "The RTCP sender count must be between zero and the member count.");
        }

        if (localIsSender && senderCount == 0)
        {
            throw new RtpConfigurationException(
                "A local RTCP sender requires at least one active sender.");
        }

        if (averagePacketSize <= 0)
        {
            throw new RtpConfigurationException(
                "The average RTCP packet size must be positive.");
        }
    }

    internal static void ValidateIntervalRandomValue(double randomValue)
    {
        if (double.IsNaN(randomValue) || double.IsInfinity(randomValue) ||
            randomValue < 0.0 || randomValue > 1.0)
        {
            throw new RtpConfigurationException(
                "The RTCP random value must be finite and between zero and one.");
        }
    }
}
