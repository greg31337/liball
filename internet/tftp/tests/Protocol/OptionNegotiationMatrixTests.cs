using tftp;

namespace tftp.Tests;

public class OptionNegotiationMatrixTests
{
    [TestCase(false, false, false, "")]
    [TestCase(true, false, false, "blksize")]
    [TestCase(false, true, false, "timeout")]
    [TestCase(false, false, true, "tsize")]
    [TestCase(true, true, false, "blksize,timeout")]
    [TestCase(true, false, true, "blksize,tsize")]
    [TestCase(false, true, true, "timeout,tsize")]
    [TestCase(true, true, true, "blksize,timeout,tsize")]
    public void CreatesEveryIndependentAndCombinedRequestOptionSet(
        bool requestBlockSize,
        bool requestTimeout,
        bool requestTransferSize,
        string expectedNames)
    {
        TftpTransferOptions options = new TftpTransferOptions();
        options.RequestBlockSize = requestBlockSize;
        options.BlockSize = 1024;
        options.RequestTimeout = requestTimeout;
        options.TimeoutSeconds = 3;
        options.RequestTransferSize = requestTransferSize;
        options.InitialTimeoutSeconds = 7;

        TftpOptionNegotiationResult result =
            TftpOptionNegotiator.CreateCombinedRequestOptions(
                options,
                TftpTransferDirection.Write,
                true,
                4096,
                20);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(string.Join(",", result.Options.Select(option => option.Name)),
                Is.EqualTo(expectedNames));
            Assert.That(result.EffectiveTimeoutSeconds, Is.EqualTo(7));
        });
    }

    [TestCase(TftpProtocolValues.MinimumBlockSize,
        TftpProtocolValues.MinimumTimeoutSeconds,
        0)]
    [TestCase(TftpProtocolValues.MaximumBlockSize,
        TftpProtocolValues.MaximumTimeoutSeconds,
        long.MaxValue)]
    public void CreatesCombinedRequestsAtEveryNumericBoundary(
        int blockSize,
        int timeoutSeconds,
        long transferSize)
    {
        TftpTransferOptions options = CreateAllEnabledOptions();
        options.BlockSize = blockSize;
        options.TimeoutSeconds = timeoutSeconds;

        TftpOptionNegotiationResult result =
            TftpOptionNegotiator.CreateCombinedRequestOptions(
                options,
                TftpTransferDirection.Write,
                true,
                transferSize,
                20);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Options.Select(option => option.Value), Is.EqualTo(new[]
            {
                blockSize.ToString(),
                timeoutSeconds.ToString(),
                transferSize.ToString()
            }));
        });
    }

    [Test]
    public void CombinedWriteRequestOmitsOnlyUnknownTransferSize()
    {
        TftpTransferOptions options = CreateAllEnabledOptions();

        TftpOptionNegotiationResult result =
            TftpOptionNegotiator.CreateCombinedRequestOptions(
                options,
                TftpTransferDirection.Write,
                false,
                0,
                20);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Options.Select(option => option.Name),
                Is.EqualTo(new[] { "blksize", "timeout" }));
        });
    }

    [TestCase(7, 3, 10, "blksize")]
    [TestCase(512, 0, 10, "timeout")]
    [TestCase(512, 3, -1, "tsize")]
    public void CombinedRequestRejectsInvalidEnabledNumericValues(
        int blockSize,
        int timeoutSeconds,
        long transferSize,
        string expectedOption)
    {
        TftpTransferOptions options = CreateAllEnabledOptions();
        options.BlockSize = blockSize;
        options.TimeoutSeconds = timeoutSeconds;

        TftpOptionNegotiationResult result =
            TftpOptionNegotiator.CreateCombinedRequestOptions(
                options,
                TftpTransferDirection.Write,
                true,
                transferSize,
                20);

        AssertOptionError(result, expectedOption);
    }

    [Test]
    public void CombinedRequestRejectsMissingOptionsInvalidDirectionAndOversize()
    {
        TftpOptionNegotiationResult missing =
            TftpOptionNegotiator.CreateCombinedRequestOptions(
                null!,
                TftpTransferDirection.Read,
                false,
                0,
                20);
        TftpOptionNegotiationResult direction =
            TftpOptionNegotiator.CreateCombinedRequestOptions(
                new TftpTransferOptions(),
                (TftpTransferDirection)99,
                false,
                0,
                20);
        TftpTransferOptions enabled = CreateAllEnabledOptions();
        TftpOptionNegotiationResult oversized =
            TftpOptionNegotiator.CreateCombinedRequestOptions(
                enabled,
                TftpTransferDirection.Read,
                false,
                0,
                TftpProtocolValues.MaximumRequestPacketLength);

        Assert.Multiple(() =>
        {
            AssertOptionError(missing, "");
            AssertOptionError(direction, "");
            AssertOptionError(oversized, "blksize");
        });
    }

    [Test]
    public void IndependentRequestBuildersRejectMissingTransferOptions()
    {
        TftpOptionNegotiationResult timeout =
            TftpOptionNegotiator.CreateTimeoutRequest(null!, 20);
        TftpOptionNegotiationResult transferSize =
            TftpOptionNegotiator.CreateTransferSizeRequest(
                null!,
                TftpTransferDirection.Read,
                false,
                0,
                20);

        Assert.Multiple(() =>
        {
            AssertOptionError(timeout, "timeout");
            AssertOptionError(transferSize, "tsize");
        });
    }

    [TestCase(false, false, false, "")]
    [TestCase(true, false, false, "BLKSIZE")]
    [TestCase(false, true, false, "TiMeOuT")]
    [TestCase(false, false, true, "TsIzE")]
    [TestCase(true, true, false, "TiMeOuT,BLKSIZE")]
    [TestCase(true, false, true, "TsIzE,BLKSIZE")]
    [TestCase(false, true, true, "TsIzE,TiMeOuT")]
    [TestCase(true, true, true, "TsIzE,TiMeOuT,BLKSIZE")]
    public void ServerAppliesEveryEnableCombinationInRequestOrder(
        bool enableBlockSize,
        bool enableTimeout,
        bool enableTransferSize,
        string expectedNames)
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "TsIzE", Value = "0" },
            new TftpOption { Name = "vendor", Value = "ignored" },
            new TftpOption { Name = "TiMeOuT", Value = "003" },
            new TftpOption { Name = "BLKSIZE", Value = "2048" }
        };
        TftpServerOptions options = new TftpServerOptions();
        options.EnableBlockSize = enableBlockSize;
        options.MaximumBlockSize = 1024;
        options.EnableTimeout = enableTimeout;
        options.EnableTransferSize = enableTransferSize;
        options.InitialTimeoutSeconds = 9;

        TftpOptionNegotiationResult result =
            TftpOptionNegotiator.SelectServerTransferSizeOptions(
                requested,
                options,
                20,
                TftpTransferDirection.Read,
                true,
                55);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(string.Join(",", result.Options.Select(option => option.Name)),
                Is.EqualTo(expectedNames));
            Assert.That(result.EffectiveBlockSize,
                Is.EqualTo(enableBlockSize ? 1024 : TftpProtocolValues.DefaultBlockSize));
            Assert.That(result.EffectiveTimeoutSeconds,
                Is.EqualTo(enableTimeout ? 3 : 9));
            Assert.That(result.HasTransferSize, Is.EqualTo(enableTransferSize));
            Assert.That(result.TransferSize, Is.EqualTo(enableTransferSize ? 55 : 0));
        });
    }

    [Test]
    public void ServerTransferSizeSelectionPropagatesMalformedRequest()
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "tsize", Value = "0" },
            new TftpOption { Name = "TSIZE", Value = "0" }
        };

        TftpOptionNegotiationResult result =
            TftpOptionNegotiator.SelectServerTransferSizeOptions(
                requested,
                new TftpServerOptions(),
                20,
                TftpTransferDirection.Read,
                true,
                10);

        AssertOptionError(result, "TSIZE");
    }

    [Test]
    public void ServerTransferSizeSelectionRejectsInvalidDirection()
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "tsize", Value = "0" }
        };

        TftpOptionNegotiationResult result =
            TftpOptionNegotiator.SelectServerTransferSizeOptions(
                requested,
                new TftpServerOptions(),
                20,
                (TftpTransferDirection)99,
                true,
                10);

        AssertOptionError(result, "tsize");
    }

    [Test]
    public void ServerReadTransferSizeRejectsNegativeKnownSize()
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "tsize", Value = "0" }
        };

        TftpOptionNegotiationResult result =
            TftpOptionNegotiator.SelectServerTransferSizeOptions(
                requested,
                new TftpServerOptions(),
                20,
                TftpTransferDirection.Read,
                true,
                -1);

        AssertOptionError(result, "tsize");
    }

    [TestCase(9, 10)]
    [TestCase(-1, 0)]
    public void ServerReadTransferSizeEnforcesCapacity(
        long maximumTransferSize,
        long encodedSize)
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "tsize", Value = "0" }
        };
        TftpServerOptions options = new TftpServerOptions();
        options.MaximumTransferSize = maximumTransferSize;

        TftpOptionNegotiationResult result =
            TftpOptionNegotiator.SelectServerTransferSizeOptions(
                requested,
                options,
                20,
                TftpTransferDirection.Read,
                true,
                encodedSize);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.HasError, Is.True);
            Assert.That(result.ErrorCode, Is.EqualTo(TftpErrorCode.StorageExhausted));
        });
    }

    [Test]
    public void ClientCombinesPartialMixedCaseAcknowledgement()
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "blksize", Value = "2048" },
            new TftpOption { Name = "TIMEOUT", Value = "003" },
            new TftpOption { Name = "tsize", Value = "0" }
        };
        List<TftpOption> acknowledged = new List<TftpOption>
        {
            new TftpOption { Name = "TsIzE", Value = "4096" },
            new TftpOption { Name = "timeout", Value = "003" }
        };

        TftpOptionNegotiationResult result =
            TftpOptionNegotiator.ValidateCombinedAcknowledgedOptions(
                requested,
                acknowledged,
                TftpTransferDirection.Read,
                9,
                5000);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Options.Select(option => option.Name),
                Is.EqualTo(new[] { "TsIzE", "timeout" }));
            Assert.That(result.EffectiveBlockSize,
                Is.EqualTo(TftpProtocolValues.DefaultBlockSize));
            Assert.That(result.EffectiveTimeoutSeconds, Is.EqualTo(3));
            Assert.That(result.HasTransferSize, Is.True);
            Assert.That(result.TransferSize, Is.EqualTo(4096));
        });
    }

    [Test]
    public void ClientCombinesFullWriteAcknowledgementInPeerOrder()
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "blksize", Value = "2048" },
            new TftpOption { Name = "timeout", Value = "3" },
            new TftpOption { Name = "tsize", Value = "00055" }
        };
        List<TftpOption> acknowledged = new List<TftpOption>
        {
            new TftpOption { Name = "TSIZE", Value = "00055" },
            new TftpOption { Name = "BLKSIZE", Value = "1024" },
            new TftpOption { Name = "TIMEOUT", Value = "3" }
        };

        TftpOptionNegotiationResult result =
            TftpOptionNegotiator.ValidateCombinedAcknowledgedOptions(
                requested,
                acknowledged,
                TftpTransferDirection.Write,
                9,
                100);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Options.Select(option => option.Name),
                Is.EqualTo(new[] { "TSIZE", "BLKSIZE", "TIMEOUT" }));
            Assert.That(result.EffectiveBlockSize, Is.EqualTo(1024));
            Assert.That(result.EffectiveTimeoutSeconds, Is.EqualTo(3));
            Assert.That(result.HasTransferSize, Is.True);
            Assert.That(result.TransferSize, Is.EqualTo(55));
        });
    }

    [TestCase("blksize", "2147483648")]
    [TestCase("timeout", "2147483648")]
    [TestCase("tsize", "9223372036854775808")]
    public void CombinedClientValidationRejectsEveryNumericOverflow(
        string optionName,
        string overflowValue)
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "blksize", Value = "1024" },
            new TftpOption { Name = "timeout", Value = "3" },
            new TftpOption { Name = "tsize", Value = "10" }
        };
        List<TftpOption> acknowledged = new List<TftpOption>
        {
            new TftpOption { Name = "blksize", Value = "1024" },
            new TftpOption { Name = "timeout", Value = "3" },
            new TftpOption { Name = "tsize", Value = "10" }
        };
        acknowledged.Single(option => option.Name == optionName).Value = overflowValue;

        TftpOptionNegotiationResult result =
            TftpOptionNegotiator.ValidateCombinedAcknowledgedOptions(
                requested,
                acknowledged,
                TftpTransferDirection.Write,
                9,
                long.MaxValue);

        AssertOptionError(result, optionName);
    }

    [Test]
    public void CombinedClientValidationRejectsEachCoordinationFailure()
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "timeout", Value = "3" },
            new TftpOption { Name = "tsize", Value = "0" }
        };
        List<TftpOption> acknowledged = new List<TftpOption>
        {
            new TftpOption { Name = "timeout", Value = "3" },
            new TftpOption { Name = "tsize", Value = "10" }
        };

        TftpOptionNegotiationResult timeout =
            TftpOptionNegotiator.ValidateCombinedAcknowledgedOptions(
                requested,
                acknowledged,
                TftpTransferDirection.Read,
                0,
                100);
        TftpOptionNegotiationResult direction =
            TftpOptionNegotiator.ValidateCombinedAcknowledgedOptions(
                requested,
                acknowledged,
                (TftpTransferDirection)99,
                1,
                100);

        Assert.Multiple(() =>
        {
            AssertOptionError(timeout, "timeout");
            AssertOptionError(direction, "tsize");
        });
    }

    [Test]
    public void GenericAcknowledgementValidationPropagatesMalformedRequest()
    {
        List<TftpOption> acknowledged = new List<TftpOption>
        {
            new TftpOption { Name = "timeout", Value = "3" }
        };

        TftpOptionNegotiationResult result =
            TftpOptionNegotiator.ValidateAcknowledgedOptions(null!, acknowledged);

        AssertOptionError(result, "");
    }

    [Test]
    public void TimeoutAcknowledgementWithoutTimeoutUsesLocalInterval()
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "blksize", Value = "1024" }
        };
        List<TftpOption> acknowledged = new List<TftpOption>
        {
            new TftpOption { Name = "BLKSIZE", Value = "512" }
        };

        TftpOptionNegotiationResult result =
            TftpOptionNegotiator.ValidateAcknowledgedTimeoutOptions(
                requested,
                acknowledged,
                7);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.EffectiveTimeoutSeconds, Is.EqualTo(7));
        });
    }

    [Test]
    public void TransferSizeAcknowledgementPropagatesMalformedOptions()
    {
        List<TftpOption> acknowledged = new List<TftpOption>
        {
            new TftpOption { Name = "tsize", Value = "0" }
        };

        TftpOptionNegotiationResult result =
            TftpOptionNegotiator.ValidateAcknowledgedTransferSizeOptions(
                null!,
                acknowledged,
                TftpTransferDirection.Read,
                100);

        AssertOptionError(result, "");
    }

    [Test]
    public void TransferSizeAcknowledgementWithoutRequestLeavesSizeUnknown()
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "blksize", Value = "1024" }
        };
        List<TftpOption> acknowledged = new List<TftpOption>
        {
            new TftpOption { Name = "BLKSIZE", Value = "512" }
        };

        TftpOptionNegotiationResult result =
            TftpOptionNegotiator.ValidateAcknowledgedTransferSizeOptions(
                requested,
                acknowledged,
                TftpTransferDirection.Read,
                100);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.HasTransferSize, Is.False);
        });
    }

    [TestCase("-1", TftpTransferDirection.Write)]
    [TestCase("1", TftpTransferDirection.Read)]
    public void TransferSizeAcknowledgementRejectsInvalidRequestedValue(
        string requestedValue,
        TftpTransferDirection direction)
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "tsize", Value = requestedValue }
        };
        List<TftpOption> acknowledged = new List<TftpOption>
        {
            new TftpOption { Name = "tsize", Value = requestedValue }
        };

        TftpOptionNegotiationResult result =
            TftpOptionNegotiator.ValidateAcknowledgedTransferSizeOptions(
                requested,
                acknowledged,
                direction,
                long.MaxValue);

        AssertOptionError(result, "tsize");
    }

    [TestCase(-1)]
    [TestCase(0)]
    public void TransferSizeAcknowledgementRejectsInvalidLocalCapacity(
        long acknowledgedSize)
    {
        string value = acknowledgedSize.ToString();
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "tsize", Value = value }
        };
        List<TftpOption> acknowledged = new List<TftpOption>
        {
            new TftpOption { Name = "tsize", Value = value }
        };

        TftpOptionNegotiationResult result =
            TftpOptionNegotiator.ValidateAcknowledgedTransferSizeOptions(
                requested,
                acknowledged,
                TftpTransferDirection.Write,
                -1);

        if (acknowledgedSize < 0)
        {
            AssertOptionError(result, "tsize");
        }
        else
        {
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.ErrorCode, Is.EqualTo(TftpErrorCode.StorageExhausted));
            });
        }
    }

    [Test]
    public void LegacyFallbackRestoresAllLocalNegotiationDefaults()
    {
        TftpOptionNegotiationResult result =
            TftpOptionNegotiator.CreateLegacyTimeoutFallback(7);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Options, Is.Empty);
            Assert.That(result.EffectiveBlockSize,
                Is.EqualTo(TftpProtocolValues.DefaultBlockSize));
            Assert.That(result.EffectiveTimeoutSeconds, Is.EqualTo(7));
            Assert.That(result.HasTransferSize, Is.False);
            Assert.That(result.TransferSize, Is.Zero);
        });
    }

    [TestCase(true, true, false, TftpErrorCode.OptionNegotiationFailed, true)]
    [TestCase(true, true, false, TftpErrorCode.IllegalOperation, true)]
    [TestCase(true, true, false, TftpErrorCode.StorageExhausted, false)]
    [TestCase(false, true, false, TftpErrorCode.OptionNegotiationFailed, false)]
    [TestCase(true, false, false, TftpErrorCode.OptionNegotiationFailed, false)]
    [TestCase(true, true, true, TftpErrorCode.OptionNegotiationFailed, false)]
    public void RetryWithoutOptionsRequiresOptInOptionsFirstAttemptAndRejection(
        bool retryEnabled,
        bool hasRequestedOptions,
        bool retryAlreadyAttempted,
        TftpErrorCode errorCode,
        bool expected)
    {
        TftpTransferOptions options = new TftpTransferOptions();
        options.RetryWithoutOptions = retryEnabled;
        List<TftpOption> requested = new List<TftpOption>();
        if (hasRequestedOptions)
        {
            requested.Add(new TftpOption { Name = "blksize", Value = "1024" });
        }

        bool result = TftpOptionNegotiator.CanRetryRequestWithoutOptions(
            options,
            requested,
            retryAlreadyAttempted,
            errorCode);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void RetryWithoutOptionsSafelyRejectsMissingInputs()
    {
        TftpTransferOptions options = new TftpTransferOptions();
        options.RetryWithoutOptions = true;
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "blksize", Value = "1024" }
        };

        Assert.Multiple(() =>
        {
            Assert.That(TftpOptionNegotiator.CanRetryRequestWithoutOptions(
                null!,
                requested,
                false,
                TftpErrorCode.OptionNegotiationFailed), Is.False);
            Assert.That(TftpOptionNegotiator.CanRetryRequestWithoutOptions(
                options,
                null!,
                false,
                TftpErrorCode.OptionNegotiationFailed), Is.False);
        });
    }

    [Test]
    public void DecimalOptionParsersRejectMissingValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(TftpOptionNegotiator.ParsePositiveDecimalOption(null!), Is.EqualTo(-1));
            Assert.That(TftpOptionNegotiator.ParsePositiveDecimalOption(""), Is.EqualTo(-1));
            Assert.That(TftpOptionNegotiator.ParseNonnegativeDecimalOption(null!),
                Is.EqualTo(-1));
            Assert.That(TftpOptionNegotiator.ParseNonnegativeDecimalOption(""), Is.EqualTo(-1));
        });
    }

    private static TftpTransferOptions CreateAllEnabledOptions()
    {
        TftpTransferOptions options = new TftpTransferOptions();
        options.RequestBlockSize = true;
        options.BlockSize = 1024;
        options.RequestTimeout = true;
        options.TimeoutSeconds = 3;
        options.RequestTransferSize = true;
        return options;
    }

    private static void AssertOptionError(
        TftpOptionNegotiationResult result,
        string optionName)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.HasError, Is.True);
            Assert.That(result.ErrorCode,
                Is.EqualTo(TftpErrorCode.OptionNegotiationFailed));
            Assert.That(result.OptionName, Is.EqualTo(optionName));
        });
    }
}
