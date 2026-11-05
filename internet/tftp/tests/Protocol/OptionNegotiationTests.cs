using tftp;

namespace tftp.Tests;

public class OptionNegotiationTests
{
    [TestCase(TftpProtocolValues.MinimumBlockSize)]
    [TestCase(TftpProtocolValues.DefaultBlockSize)]
    [TestCase(TftpProtocolValues.MaximumBlockSize)]
    public void CreatesBlockSizeRequestsAtRfcBoundaries(int blockSize)
    {
        TftpTransferOptions options = new TftpTransferOptions();
        options.RequestBlockSize = true;
        options.BlockSize = blockSize;

        TftpOptionNegotiationResult result = TftpOptionNegotiator.CreateBlockSizeRequest(
            options,
            20);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.EffectiveBlockSize,
                Is.EqualTo(TftpProtocolValues.DefaultBlockSize));
            Assert.That(result.Options, Has.Count.EqualTo(1));
            Assert.That(result.Options[0].Name, Is.EqualTo(TftpProtocolValues.BlockSizeOption));
            Assert.That(result.Options[0].Value, Is.EqualTo(blockSize.ToString()));
            Assert.That(result.EncodedLength,
                Is.EqualTo(20 + "blksize".Length + 1 + blockSize.ToString().Length + 1));
        });
    }

    [TestCase(TftpProtocolValues.MinimumBlockSize - 1)]
    [TestCase(TftpProtocolValues.MaximumBlockSize + 1)]
    public void RejectsBlockSizeRequestsOutsideRfcRange(int blockSize)
    {
        TftpTransferOptions options = new TftpTransferOptions();
        options.RequestBlockSize = true;
        options.BlockSize = blockSize;

        TftpOptionNegotiationResult result = TftpOptionNegotiator.CreateBlockSizeRequest(
            options,
            20);

        AssertOptionError(
            result,
            "outside the RFC 2348 range",
            TftpProtocolValues.BlockSizeOption,
            blockSize.ToString());
    }

    [Test]
    public void DisabledBlockSizeRequestUsesDefaultWithoutOption()
    {
        TftpTransferOptions options = new TftpTransferOptions();
        options.RequestBlockSize = false;
        options.BlockSize = -1;

        TftpOptionNegotiationResult result = TftpOptionNegotiator.CreateBlockSizeRequest(
            options,
            20);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Options, Is.Empty);
            Assert.That(result.EffectiveBlockSize,
                Is.EqualTo(TftpProtocolValues.DefaultBlockSize));
        });
    }

    [Test]
    public void MissingTransferOptionsProduceErrorEightOutcome()
    {
        TftpOptionNegotiationResult result = TftpOptionNegotiator.CreateBlockSizeRequest(
            null!,
            20);

        AssertOptionError(
            result,
            "cannot be null",
            TftpProtocolValues.BlockSizeOption,
            "");
    }

    [TestCase(TftpProtocolValues.MinimumTimeoutSeconds)]
    [TestCase(TftpProtocolValues.MaximumTimeoutSeconds)]
    public void CreatesTimeoutRequestsAtRfcBoundaries(int timeoutSeconds)
    {
        TftpTransferOptions options = new TftpTransferOptions();
        options.RequestTimeout = true;
        options.TimeoutSeconds = timeoutSeconds;
        options.InitialTimeoutSeconds = 4;

        TftpOptionNegotiationResult result = TftpOptionNegotiator.CreateTimeoutRequest(
            options,
            20);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Options, Has.Count.EqualTo(1));
            Assert.That(result.Options[0].Name, Is.EqualTo(TftpProtocolValues.TimeoutOption));
            Assert.That(result.Options[0].Value, Is.EqualTo(timeoutSeconds.ToString()));
            Assert.That(result.EffectiveTimeoutSeconds, Is.EqualTo(4));
        });
    }

    [TestCase(TftpProtocolValues.MinimumTimeoutSeconds - 1)]
    [TestCase(TftpProtocolValues.MaximumTimeoutSeconds + 1)]
    public void RejectsTimeoutRequestsOutsideRfcRange(int timeoutSeconds)
    {
        TftpTransferOptions options = new TftpTransferOptions();
        options.RequestTimeout = true;
        options.TimeoutSeconds = timeoutSeconds;

        TftpOptionNegotiationResult result = TftpOptionNegotiator.CreateTimeoutRequest(
            options,
            20);

        AssertOptionError(
            result,
            "outside the RFC 2349 range",
            TftpProtocolValues.TimeoutOption,
            timeoutSeconds.ToString());
    }

    [Test]
    public void DisabledTimeoutRequestRetainsLocalInitialInterval()
    {
        TftpTransferOptions options = new TftpTransferOptions();
        options.RequestTimeout = false;
        options.TimeoutSeconds = -1;
        options.InitialTimeoutSeconds = 7;

        TftpOptionNegotiationResult result = TftpOptionNegotiator.CreateTimeoutRequest(
            options,
            20);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Options, Is.Empty);
            Assert.That(result.EffectiveTimeoutSeconds, Is.EqualTo(7));
        });
    }

    [Test]
    public void ReadTransferSizeRequestAlwaysSendsZero()
    {
        TftpTransferOptions options = new TftpTransferOptions();
        options.RequestTransferSize = true;

        TftpOptionNegotiationResult result = TftpOptionNegotiator.CreateTransferSizeRequest(
            options,
            TftpTransferDirection.Read,
            false,
            -1,
            20);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Options, Has.Count.EqualTo(1));
            Assert.That(result.Options[0].Name,
                Is.EqualTo(TftpProtocolValues.TransferSizeOption));
            Assert.That(result.Options[0].Value, Is.EqualTo("0"));
        });
    }

    [TestCase(0)]
    [TestCase(4096)]
    [TestCase(long.MaxValue)]
    public void WriteTransferSizeRequestUsesKnownEncodedSize(long encodedSize)
    {
        TftpTransferOptions options = new TftpTransferOptions();
        options.RequestTransferSize = true;

        TftpOptionNegotiationResult result = TftpOptionNegotiator.CreateTransferSizeRequest(
            options,
            TftpTransferDirection.Write,
            true,
            encodedSize,
            20);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Options, Has.Count.EqualTo(1));
            Assert.That(result.Options[0].Value, Is.EqualTo(encodedSize.ToString()));
        });
    }

    [Test]
    public void UnknownEncodedWriteSizeOmitsTransferSizeOption()
    {
        TftpTransferOptions options = new TftpTransferOptions();
        options.RequestTransferSize = true;

        TftpOptionNegotiationResult result = TftpOptionNegotiator.CreateTransferSizeRequest(
            options,
            TftpTransferDirection.Write,
            false,
            0,
            20);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Options, Is.Empty);
            Assert.That(result.HasTransferSize, Is.False);
        });
    }

    [Test]
    public void DisabledTransferSizeRequestOmitsOptionWithoutSizeValidation()
    {
        TftpTransferOptions options = new TftpTransferOptions();
        options.RequestTransferSize = false;

        TftpOptionNegotiationResult result = TftpOptionNegotiator.CreateTransferSizeRequest(
            options,
            TftpTransferDirection.Write,
            true,
            -1,
            20);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Options, Is.Empty);
        });
    }

    [Test]
    public void RejectsNegativeKnownEncodedWriteSize()
    {
        TftpTransferOptions options = new TftpTransferOptions();
        options.RequestTransferSize = true;

        TftpOptionNegotiationResult result = TftpOptionNegotiator.CreateTransferSizeRequest(
            options,
            TftpTransferDirection.Write,
            true,
            -1,
            20);

        AssertOptionError(result, "cannot be negative", "tsize", "-1");
    }

    [Test]
    public void RejectsInvalidTransferDirectionForSizeRequest()
    {
        TftpOptionNegotiationResult result = TftpOptionNegotiator.CreateTransferSizeRequest(
            new TftpTransferOptions(),
            (TftpTransferDirection)99,
            false,
            0,
            20);

        AssertOptionError(result, "direction is invalid", "tsize", "");
    }

    [Test]
    public void ValidatesRequestedOptionsAndAccountsForPacketLength()
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "BLKSIZE", Value = "1024" },
            new TftpOption { Name = "vendor-option", Value = "opaque" }
        };

        TftpOptionNegotiationResult result = TftpOptionNegotiator.ValidateRequestOptions(
            requested,
            20);
        requested[0].Name = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.HasError, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(TftpErrorCode.Undefined));
            Assert.That(result.EncodedLength, Is.EqualTo(54));
            Assert.That(result.Options.Select(option => option.Name),
                Is.EqualTo(new[] { "BLKSIZE", "vendor-option" }));
            Assert.That(result.Options.Select(option => option.Value),
                Is.EqualTo(new[] { "1024", "opaque" }));
            Assert.That(result.Options[0], Is.Not.SameAs(requested[0]));
        });
    }

    [Test]
    public void AcceptsEmptyRequestOptionList()
    {
        TftpOptionNegotiationResult result = TftpOptionNegotiator.ValidateRequestOptions(
            new List<TftpOption>(),
            14);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Options, Is.Empty);
            Assert.That(result.EncodedLength, Is.EqualTo(14));
        });
    }

    [Test]
    public void AcceptsRequestAtMaximumPacketLengthAndRejectsNextByte()
    {
        List<TftpOption> exactOptions = new List<TftpOption>
        {
            new TftpOption { Name = "x", Value = new string('v', 489) }
        };
        List<TftpOption> oversizedOptions = new List<TftpOption>
        {
            new TftpOption { Name = "x", Value = new string('v', 490) }
        };

        TftpOptionNegotiationResult exact = TftpOptionNegotiator.ValidateRequestOptions(
            exactOptions,
            20);
        TftpOptionNegotiationResult oversized = TftpOptionNegotiator.ValidateRequestOptions(
            oversizedOptions,
            20);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Success, Is.True);
            Assert.That(exact.EncodedLength,
                Is.EqualTo(TftpProtocolValues.MaximumRequestPacketLength));
            AssertOptionError(oversized, "512-byte", "x", oversizedOptions[0].Value);
        });
    }

    [TestCaseSource(nameof(InvalidRequestedOptions))]
    public void RejectsMalformedRequestedOptions(
        List<TftpOption>? requested,
        string messagePart,
        string optionName,
        string optionValue)
    {
        TftpOptionNegotiationResult result = TftpOptionNegotiator.ValidateRequestOptions(
            requested!,
            20);

        AssertOptionError(result, messagePart, optionName, optionValue);
    }

    [TestCase(1)]
    [TestCase(TftpProtocolValues.MaximumRequestPacketLength + 1)]
    public void RejectsInvalidRequestBaseLengths(int requestBaseLength)
    {
        TftpOptionNegotiationResult result = TftpOptionNegotiator.ValidateRequestOptions(
            new List<TftpOption>(),
            requestBaseLength);

        AssertOptionError(result, "outside the protocol range", "", "");
    }

    [Test]
    public void ServerSelectsKnownEnabledOptionsInRequestOrder()
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "vendor-window", Value = "not-interpreted" },
            new TftpOption { Name = "TIMEOUT", Value = "003" },
            new TftpOption { Name = "blksize", Value = "1024" },
            new TftpOption { Name = "TSize", Value = "0" }
        };
        TftpServerOptions policy = new TftpServerOptions();
        policy.EnableBlockSize = false;

        TftpOptionNegotiationResult result = TftpOptionNegotiator.SelectServerOptions(
            requested,
            policy,
            20);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Options.Select(option => option.Name),
                Is.EqualTo(new[] { "TIMEOUT", "TSize" }));
            Assert.That(result.Options.Select(option => option.Value),
                Is.EqualTo(new[] { "003", "0" }));
            Assert.That(result.Options[0], Is.Not.SameAs(requested[1]));
        });
    }

    [Test]
    public void ServerOmitsUnknownAndDisabledOptionsWithoutFailure()
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "blksize", Value = "512" },
            new TftpOption { Name = "timeout", Value = "3" },
            new TftpOption { Name = "tsize", Value = "0" },
            new TftpOption { Name = "private", Value = "value" }
        };
        TftpServerOptions policy = new TftpServerOptions();
        policy.EnableBlockSize = false;
        policy.EnableTimeout = false;
        policy.EnableTransferSize = false;

        TftpOptionNegotiationResult result = TftpOptionNegotiator.SelectServerOptions(
            requested,
            policy,
            20);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Options, Is.Empty);
            Assert.That(result.HasError, Is.False);
        });
    }

    [TestCase("8", TftpProtocolValues.MaximumBlockSize, 8)]
    [TestCase("1024", 1400, 1024)]
    [TestCase("2048", 1400, 1400)]
    [TestCase("65464", TftpProtocolValues.MaximumBlockSize, 65464)]
    [TestCase("0008", TftpProtocolValues.MaximumBlockSize, 8)]
    public void ServerNegotiatesAndReducesBlockSize(
        string requestedValue,
        int serverMaximum,
        int expectedValue)
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "BLKSIZE", Value = requestedValue }
        };
        TftpServerOptions policy = new TftpServerOptions();
        policy.MaximumBlockSize = serverMaximum;

        TftpOptionNegotiationResult result = TftpOptionNegotiator.SelectServerOptions(
            requested,
            policy,
            20);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.EffectiveBlockSize, Is.EqualTo(expectedValue));
            Assert.That(result.Options, Has.Count.EqualTo(1));
            Assert.That(result.Options[0].Name, Is.EqualTo("BLKSIZE"));
            Assert.That(result.Options[0].Value, Is.EqualTo(expectedValue.ToString()));
        });
    }

    [TestCase("1", 1)]
    [TestCase("255", 255)]
    [TestCase("003", 3)]
    public void ServerEchoesExactTimeoutAndAppliesActiveInterval(
        string requestedValue,
        int expectedInterval)
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "TIMEOUT", Value = requestedValue }
        };
        TftpServerOptions policy = new TftpServerOptions();
        policy.InitialTimeoutSeconds = 9;

        TftpOptionNegotiationResult result = TftpOptionNegotiator.SelectServerOptions(
            requested,
            policy,
            20);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Options, Has.Count.EqualTo(1));
            Assert.That(result.Options[0].Name, Is.EqualTo("TIMEOUT"));
            Assert.That(result.Options[0].Value, Is.EqualTo(requestedValue));
            Assert.That(result.EffectiveTimeoutSeconds, Is.EqualTo(expectedInterval));
        });
    }

    [TestCase("0")]
    [TestCase("256")]
    [TestCase("-1")]
    [TestCase("+1")]
    [TestCase(" 1")]
    [TestCase("1 ")]
    [TestCase("1.0")]
    [TestCase("999999999999999999999")]
    public void ServerRejectsInvalidRequestedTimeout(string requestedValue)
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "timeout", Value = requestedValue }
        };

        TftpOptionNegotiationResult result = TftpOptionNegotiator.SelectServerOptions(
            requested,
            new TftpServerOptions(),
            20);

        AssertOptionError(result, "RFC 2349 range", "timeout", requestedValue);
    }

    [Test]
    public void DisabledServerTimeoutIgnoresInvalidValueAndUsesLocalInterval()
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "timeout", Value = "invalid" }
        };
        TftpServerOptions policy = new TftpServerOptions();
        policy.EnableTimeout = false;
        policy.InitialTimeoutSeconds = 6;

        TftpOptionNegotiationResult result = TftpOptionNegotiator.SelectServerOptions(
            requested,
            policy,
            20);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Options, Is.Empty);
            Assert.That(result.EffectiveTimeoutSeconds, Is.EqualTo(6));
        });
    }

    [Test]
    public void ServerNegotiatesBlockSizeAndTimeoutTogether()
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "blksize", Value = "2048" },
            new TftpOption { Name = "timeout", Value = "5" }
        };
        TftpServerOptions policy = new TftpServerOptions();
        policy.MaximumBlockSize = 1400;

        TftpOptionNegotiationResult result = TftpOptionNegotiator.SelectServerOptions(
            requested,
            policy,
            20);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Options.Select(option => option.Name),
                Is.EqualTo(new[] { "blksize", "timeout" }));
            Assert.That(result.Options.Select(option => option.Value),
                Is.EqualTo(new[] { "1400", "5" }));
            Assert.That(result.EffectiveBlockSize, Is.EqualTo(1400));
            Assert.That(result.EffectiveTimeoutSeconds, Is.EqualTo(5));
        });
    }

    [TestCase(0)]
    [TestCase(4096)]
    [TestCase(long.MaxValue)]
    public void ServerReturnsKnownReadTransferSize(long encodedSize)
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "TSize", Value = "0" }
        };

        TftpOptionNegotiationResult result =
            TftpOptionNegotiator.SelectServerTransferSizeOptions(
                requested,
                new TftpServerOptions(),
                20,
                TftpTransferDirection.Read,
                true,
                encodedSize);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Options, Has.Count.EqualTo(1));
            Assert.That(result.Options[0].Name, Is.EqualTo("TSize"));
            Assert.That(result.Options[0].Value, Is.EqualTo(encodedSize.ToString()));
            Assert.That(result.HasTransferSize, Is.True);
            Assert.That(result.TransferSize, Is.EqualTo(encodedSize));
        });
    }

    [Test]
    public void ServerOmitsReadTransferSizeWhenEncodedSizeIsUnknown()
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
                false,
                0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Options, Is.Empty);
            Assert.That(result.HasTransferSize, Is.False);
        });
    }

    [TestCase("1")]
    [TestCase("00")]
    [TestCase("invalid")]
    public void ServerRejectsReadTransferSizeRequestOtherThanLiteralZero(string value)
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "tsize", Value = value }
        };

        TftpOptionNegotiationResult result =
            TftpOptionNegotiator.SelectServerTransferSizeOptions(
                requested,
                new TftpServerOptions(),
                20,
                TftpTransferDirection.Read,
                true,
                10);

        AssertOptionError(result, "exactly zero", "tsize", value);
    }

    [TestCase("0", 0)]
    [TestCase("004096", 4096)]
    [TestCase("9223372036854775807", long.MaxValue)]
    public void ServerEchoesValidAnnouncedWriteSize(string value, long expectedSize)
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "tsize", Value = value }
        };

        TftpOptionNegotiationResult result =
            TftpOptionNegotiator.SelectServerTransferSizeOptions(
                requested,
                new TftpServerOptions(),
                20,
                TftpTransferDirection.Write,
                false,
                0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Options[0].Value, Is.EqualTo(value));
            Assert.That(result.HasTransferSize, Is.True);
            Assert.That(result.TransferSize, Is.EqualTo(expectedSize));
        });
    }

    [TestCase("-1")]
    [TestCase("+1")]
    [TestCase("1.0")]
    [TestCase(" 1")]
    [TestCase("9223372036854775808")]
    [TestCase("999999999999999999999999")]
    public void ServerRejectsInvalidAnnouncedWriteSize(string value)
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "tsize", Value = value }
        };

        TftpOptionNegotiationResult result =
            TftpOptionNegotiator.SelectServerTransferSizeOptions(
                requested,
                new TftpServerOptions(),
                20,
                TftpTransferDirection.Write,
                false,
                0);

        AssertOptionError(result, "nonnegative 64-bit", "tsize", value);
    }

    [Test]
    public void ServerReportsStorageExhaustedWhenTransferSizeExceedsCapacity()
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "tsize", Value = "101" }
        };
        TftpServerOptions options = new TftpServerOptions();
        options.MaximumTransferSize = 100;

        TftpOptionNegotiationResult result =
            TftpOptionNegotiator.SelectServerTransferSizeOptions(
                requested,
                options,
                20,
                TftpTransferDirection.Write,
                false,
                0);
        TftpPacket errorPacket = TftpOptionNegotiator.CreateOptionErrorPacket(result);

        Assert.Multiple(() =>
        {
            AssertCapacityError(result, "server transfer capacity", "tsize", "101");
            Assert.That(errorPacket.Opcode, Is.EqualTo(TftpOpcode.Error));
            Assert.That(errorPacket.ErrorCode, Is.EqualTo(TftpErrorCode.StorageExhausted));
        });
    }

    [Test]
    public void DisabledServerTransferSizeOmitsMalformedValue()
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "tsize", Value = "invalid" }
        };
        TftpServerOptions options = new TftpServerOptions();
        options.EnableTransferSize = false;

        TftpOptionNegotiationResult result =
            TftpOptionNegotiator.SelectServerTransferSizeOptions(
                requested,
                options,
                20,
                TftpTransferDirection.Write,
                false,
                0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Options, Is.Empty);
            Assert.That(result.HasTransferSize, Is.False);
        });
    }

    [TestCase("7")]
    [TestCase("65465")]
    [TestCase("-8")]
    [TestCase("+8")]
    [TestCase(" 8")]
    [TestCase("8 ")]
    [TestCase("1.5")]
    [TestCase("999999999999999999999")]
    public void ServerRejectsInvalidRequestedBlockSize(string requestedValue)
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "blksize", Value = requestedValue }
        };

        TftpOptionNegotiationResult result = TftpOptionNegotiator.SelectServerOptions(
            requested,
            new TftpServerOptions(),
            20);

        AssertOptionError(result, "RFC 2348 range", "blksize", requestedValue);
    }

    [TestCase(TftpProtocolValues.MinimumBlockSize - 1)]
    [TestCase(TftpProtocolValues.MaximumBlockSize + 1)]
    public void ServerRejectsInvalidConfiguredBlockSizeLimit(int serverMaximum)
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "blksize", Value = "1024" }
        };
        TftpServerOptions policy = new TftpServerOptions();
        policy.MaximumBlockSize = serverMaximum;

        TftpOptionNegotiationResult result = TftpOptionNegotiator.SelectServerOptions(
            requested,
            policy,
            20);

        AssertOptionError(result, "server block-size limit", "blksize", "1024");
    }

    [Test]
    public void DisabledServerBlockSizeIgnoresInvalidRequestedValue()
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "blksize", Value = "invalid" }
        };
        TftpServerOptions policy = new TftpServerOptions();
        policy.EnableBlockSize = false;

        TftpOptionNegotiationResult result = TftpOptionNegotiator.SelectServerOptions(
            requested,
            policy,
            20);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Options, Is.Empty);
            Assert.That(result.EffectiveBlockSize,
                Is.EqualTo(TftpProtocolValues.DefaultBlockSize));
        });
    }

    [Test]
    public void ServerSelectionReportsInvalidRequestAndPolicy()
    {
        List<TftpOption> duplicate = new List<TftpOption>
        {
            new TftpOption { Name = "timeout", Value = "1" },
            new TftpOption { Name = "TIMEOUT", Value = "2" }
        };

        TftpOptionNegotiationResult requestFailure = TftpOptionNegotiator.SelectServerOptions(
            duplicate,
            new TftpServerOptions(),
            20);
        TftpOptionNegotiationResult policyFailure = TftpOptionNegotiator.SelectServerOptions(
            new List<TftpOption>(),
            null!,
            20);

        Assert.Multiple(() =>
        {
            AssertOptionError(requestFailure, "duplicate", "TIMEOUT", "2");
            AssertOptionError(policyFailure, "policy cannot be null", "", "");
        });
    }

    [Test]
    public void ValidatesAcknowledgedSubsetCaseInsensitively()
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "blksize", Value = "1024" },
            new TftpOption { Name = "TIMEOUT", Value = "3" },
            new TftpOption { Name = "tsize", Value = "0" }
        };
        List<TftpOption> acknowledged = new List<TftpOption>
        {
            new TftpOption { Name = "timeout", Value = "3" },
            new TftpOption { Name = "BLKSIZE", Value = "512" }
        };

        TftpOptionNegotiationResult result = TftpOptionNegotiator.ValidateAcknowledgedOptions(
            requested,
            acknowledged);
        acknowledged[0].Value = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Options.Select(option => option.Name),
                Is.EqualTo(new[] { "timeout", "BLKSIZE" }));
            Assert.That(result.Options.Select(option => option.Value),
                Is.EqualTo(new[] { "3", "512" }));
            Assert.That(result.Options[0], Is.Not.SameAs(acknowledged[0]));
            Assert.That(result.EffectiveBlockSize, Is.EqualTo(512));
        });
    }

    [TestCase("1024", 1024)]
    [TestCase("512", 512)]
    [TestCase("0008", 8)]
    public void ClientAcceptsEqualOrReducedAcknowledgedBlockSize(
        string acknowledgedValue,
        int expectedValue)
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "blksize", Value = "1024" }
        };
        List<TftpOption> acknowledged = new List<TftpOption>
        {
            new TftpOption { Name = "BLKSIZE", Value = acknowledgedValue }
        };

        TftpOptionNegotiationResult result = TftpOptionNegotiator.ValidateAcknowledgedOptions(
            requested,
            acknowledged);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.EffectiveBlockSize, Is.EqualTo(expectedValue));
            Assert.That(result.Options[0].Value, Is.EqualTo(acknowledgedValue));
        });
    }

    [TestCase("1", 1)]
    [TestCase("255", 255)]
    [TestCase("003", 3)]
    public void ClientAcceptsExactTimeoutEcho(
        string requestedValue,
        int expectedInterval)
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "timeout", Value = requestedValue }
        };
        List<TftpOption> acknowledged = new List<TftpOption>
        {
            new TftpOption { Name = "TIMEOUT", Value = requestedValue }
        };

        TftpOptionNegotiationResult result =
            TftpOptionNegotiator.ValidateAcknowledgedTimeoutOptions(
                requested,
                acknowledged,
                9);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.EffectiveTimeoutSeconds, Is.EqualTo(expectedInterval));
        });
    }

    [TestCase("3", "4", "does not exactly match")]
    [TestCase("003", "3", "does not exactly match")]
    [TestCase("3", "003", "does not exactly match")]
    [TestCase("3", "0", "outside the RFC 2349 range")]
    [TestCase("3", "256", "outside the RFC 2349 range")]
    [TestCase("3", "invalid", "outside the RFC 2349 range")]
    public void ClientRejectsInvalidOrInexactTimeoutEcho(
        string requestedValue,
        string acknowledgedValue,
        string messagePart)
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "timeout", Value = requestedValue }
        };
        List<TftpOption> acknowledged = new List<TftpOption>
        {
            new TftpOption { Name = "timeout", Value = acknowledgedValue }
        };

        TftpOptionNegotiationResult result =
            TftpOptionNegotiator.ValidateAcknowledgedTimeoutOptions(
                requested,
                acknowledged,
                9);

        AssertOptionError(result, messagePart, "timeout", acknowledgedValue);
    }

    [TestCase("0")]
    [TestCase("256")]
    [TestCase("invalid")]
    public void ClientRejectsInvalidRequestedTimeoutDuringAcknowledgement(
        string requestedValue)
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "timeout", Value = requestedValue },
            new TftpOption { Name = "blksize", Value = "512" }
        };
        List<TftpOption> acknowledged = new List<TftpOption>
        {
            new TftpOption { Name = "blksize", Value = "512" }
        };

        TftpOptionNegotiationResult result =
            TftpOptionNegotiator.ValidateAcknowledgedTimeoutOptions(
                requested,
                acknowledged,
                9);

        AssertOptionError(result, "requested timeout", "timeout", requestedValue);
    }

    [Test]
    public void OmittedAndLegacyTimeoutUseLocalInitialInterval()
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "timeout", Value = "3" },
            new TftpOption { Name = "blksize", Value = "512" }
        };
        List<TftpOption> acknowledged = new List<TftpOption>
        {
            new TftpOption { Name = "blksize", Value = "512" }
        };

        TftpOptionNegotiationResult omitted =
            TftpOptionNegotiator.ValidateAcknowledgedTimeoutOptions(
                requested,
                acknowledged,
                7);
        TftpOptionNegotiationResult legacy =
            TftpOptionNegotiator.CreateLegacyTimeoutFallback(7);

        Assert.Multiple(() =>
        {
            Assert.That(omitted.Success, Is.True);
            Assert.That(omitted.EffectiveTimeoutSeconds, Is.EqualTo(7));
            Assert.That(legacy.Success, Is.True);
            Assert.That(legacy.Options, Is.Empty);
            Assert.That(legacy.EffectiveTimeoutSeconds, Is.EqualTo(7));
        });
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void RejectsInvalidLocalTimeoutFallback(int localTimeoutSeconds)
    {
        TftpOptionNegotiationResult legacy =
            TftpOptionNegotiator.CreateLegacyTimeoutFallback(localTimeoutSeconds);

        AssertOptionError(
            legacy,
            "must be positive",
            TftpProtocolValues.TimeoutOption,
            localTimeoutSeconds.ToString());
    }

    [TestCase("0", 0)]
    [TestCase("4096", 4096)]
    [TestCase("9223372036854775807", long.MaxValue)]
    [TestCase("0007", 7)]
    public void ClientAcceptsReadTransferSizeWithinCapacity(string value, long expectedSize)
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "tsize", Value = "0" }
        };
        List<TftpOption> acknowledged = new List<TftpOption>
        {
            new TftpOption { Name = "TSize", Value = value }
        };

        TftpOptionNegotiationResult result =
            TftpOptionNegotiator.ValidateAcknowledgedTransferSizeOptions(
                requested,
                acknowledged,
                TftpTransferDirection.Read,
                long.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.HasTransferSize, Is.True);
            Assert.That(result.TransferSize, Is.EqualTo(expectedSize));
        });
    }

    [TestCase("0")]
    [TestCase("004096")]
    [TestCase("9223372036854775807")]
    public void ClientRequiresExactWriteTransferSizeEcho(string value)
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "tsize", Value = value }
        };
        List<TftpOption> acknowledged = new List<TftpOption>
        {
            new TftpOption { Name = "TSIZE", Value = value }
        };

        TftpOptionNegotiationResult result =
            TftpOptionNegotiator.ValidateAcknowledgedTransferSizeOptions(
                requested,
                acknowledged,
                TftpTransferDirection.Write,
                long.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.HasTransferSize, Is.True);
            Assert.That(result.TransferSize,
                Is.EqualTo(TftpOptionNegotiator.ParseNonnegativeDecimalOption(value)));
        });
    }

    [TestCase("7", "8")]
    [TestCase("007", "7")]
    [TestCase("7", "007")]
    public void ClientRejectsInexactWriteTransferSizeEcho(
        string requestedValue,
        string acknowledgedValue)
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "tsize", Value = requestedValue }
        };
        List<TftpOption> acknowledged = new List<TftpOption>
        {
            new TftpOption { Name = "tsize", Value = acknowledgedValue }
        };

        TftpOptionNegotiationResult result =
            TftpOptionNegotiator.ValidateAcknowledgedTransferSizeOptions(
                requested,
                acknowledged,
                TftpTransferDirection.Write,
                long.MaxValue);

        AssertOptionError(result, "does not exactly match", "tsize", acknowledgedValue);
    }

    [TestCase("-1")]
    [TestCase("+1")]
    [TestCase("1.0")]
    [TestCase("9223372036854775808")]
    public void ClientRejectsInvalidAcknowledgedTransferSize(string value)
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "tsize", Value = "0" }
        };
        List<TftpOption> acknowledged = new List<TftpOption>
        {
            new TftpOption { Name = "tsize", Value = value }
        };

        TftpOptionNegotiationResult result =
            TftpOptionNegotiator.ValidateAcknowledgedTransferSizeOptions(
                requested,
                acknowledged,
                TftpTransferDirection.Read,
                long.MaxValue);

        AssertOptionError(result, "nonnegative 64-bit", "tsize", value);
    }

    [Test]
    public void ClientReportsStorageExhaustedAboveLocalCapacity()
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "tsize", Value = "0" }
        };
        List<TftpOption> acknowledged = new List<TftpOption>
        {
            new TftpOption { Name = "tsize", Value = "101" }
        };

        TftpOptionNegotiationResult result =
            TftpOptionNegotiator.ValidateAcknowledgedTransferSizeOptions(
                requested,
                acknowledged,
                TftpTransferDirection.Read,
                100);

        AssertCapacityError(result, "local transfer capacity", "tsize", "101");
    }

    [Test]
    public void OmittedTransferSizeLeavesSizeUnknown()
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "tsize", Value = "0" },
            new TftpOption { Name = "timeout", Value = "3" }
        };
        List<TftpOption> acknowledged = new List<TftpOption>
        {
            new TftpOption { Name = "timeout", Value = "3" }
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
            Assert.That(result.TransferSize, Is.Zero);
        });
    }

    [TestCase("1025", "exceeds the requested")]
    [TestCase("7", "outside the RFC 2348 range")]
    [TestCase("65465", "outside the RFC 2348 range")]
    [TestCase("invalid", "outside the RFC 2348 range")]
    public void ClientRejectsInvalidAcknowledgedBlockSize(
        string acknowledgedValue,
        string messagePart)
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "blksize", Value = "1024" }
        };
        List<TftpOption> acknowledged = new List<TftpOption>
        {
            new TftpOption { Name = "blksize", Value = acknowledgedValue }
        };

        TftpOptionNegotiationResult result = TftpOptionNegotiator.ValidateAcknowledgedOptions(
            requested,
            acknowledged);

        AssertOptionError(result, messagePart, "blksize", acknowledgedValue);
    }

    [Test]
    public void ClientRejectsInvalidRequestedBlockSizeDuringAcknowledgement()
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "blksize", Value = "7" },
            new TftpOption { Name = "timeout", Value = "3" }
        };
        List<TftpOption> acknowledged = new List<TftpOption>
        {
            new TftpOption { Name = "timeout", Value = "3" }
        };

        TftpOptionNegotiationResult result = TftpOptionNegotiator.ValidateAcknowledgedOptions(
            requested,
            acknowledged);

        AssertOptionError(result, "requested block size", "blksize", "7");
    }

    [Test]
    public void OmittedAndLegacyBlockSizeUseDefault()
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "blksize", Value = "1024" },
            new TftpOption { Name = "timeout", Value = "3" }
        };
        List<TftpOption> acknowledged = new List<TftpOption>
        {
            new TftpOption { Name = "timeout", Value = "3" }
        };

        TftpOptionNegotiationResult omitted =
            TftpOptionNegotiator.ValidateAcknowledgedOptions(requested, acknowledged);
        TftpOptionNegotiationResult legacy =
            TftpOptionNegotiator.CreateLegacyFallbackOptions();

        Assert.Multiple(() =>
        {
            Assert.That(omitted.Success, Is.True);
            Assert.That(omitted.EffectiveBlockSize,
                Is.EqualTo(TftpProtocolValues.DefaultBlockSize));
            Assert.That(legacy.Success, Is.True);
            Assert.That(legacy.Options, Is.Empty);
            Assert.That(legacy.EffectiveBlockSize,
                Is.EqualTo(TftpProtocolValues.DefaultBlockSize));
        });
    }

    [Test]
    public void RejectsUnrequestedAcknowledgedOptionWithErrorEight()
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "blksize", Value = "1024" }
        };
        List<TftpOption> acknowledged = new List<TftpOption>
        {
            new TftpOption { Name = "timeout", Value = "3" }
        };

        TftpOptionNegotiationResult result = TftpOptionNegotiator.ValidateAcknowledgedOptions(
            requested,
            acknowledged);

        AssertOptionError(result, "not requested", "timeout", "3");
    }

    [Test]
    public void RejectsEmptyDuplicateAndMalformedAcknowledgements()
    {
        List<TftpOption> requested = new List<TftpOption>
        {
            new TftpOption { Name = "timeout", Value = "3" }
        };
        List<TftpOption> duplicate = new List<TftpOption>
        {
            new TftpOption { Name = "timeout", Value = "3" },
            new TftpOption { Name = "TIMEOUT", Value = "3" }
        };
        List<TftpOption> malformed = new List<TftpOption>
        {
            new TftpOption { Name = "timeout", Value = "" }
        };

        TftpOptionNegotiationResult emptyResult =
            TftpOptionNegotiator.ValidateAcknowledgedOptions(
                requested,
                new List<TftpOption>());
        TftpOptionNegotiationResult duplicateResult =
            TftpOptionNegotiator.ValidateAcknowledgedOptions(requested, duplicate);
        TftpOptionNegotiationResult malformedResult =
            TftpOptionNegotiator.ValidateAcknowledgedOptions(requested, malformed);

        Assert.Multiple(() =>
        {
            AssertOptionError(emptyResult, "at least one", "", "");
            AssertOptionError(duplicateResult, "duplicate", "TIMEOUT", "3");
            AssertOptionError(malformedResult, "cannot be empty", "timeout", "");
        });
    }

    [Test]
    public void CreatesWireEncodableErrorEightPacketFromFailure()
    {
        TftpOptionNegotiationResult failure =
            TftpOptionNegotiator.ValidateAcknowledgedOptions(
                new List<TftpOption>(),
                new List<TftpOption>
                {
                    new TftpOption { Name = "timeout", Value = "3" }
                });
        TftpPacket errorPacket = TftpOptionNegotiator.CreateOptionErrorPacket(failure);
        TftpPacketCodec codec = new TftpPacketCodec();
        byte[] encoded = new byte[codec.GetEncodedLength(errorPacket)];

        codec.Encode(errorPacket, encoded.AsSpan());
        TftpPacket decoded = codec.Decode(encoded.AsSpan(), 0);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Opcode, Is.EqualTo(TftpOpcode.Error));
            Assert.That(decoded.ErrorCode, Is.EqualTo(TftpErrorCode.OptionNegotiationFailed));
            Assert.That(decoded.ErrorMessage, Does.Contain("not requested"));
        });
    }

    [Test]
    public void RefusesToCreateErrorPacketFromSuccessfulOrMissingOutcome()
    {
        TftpOptionNegotiationResult success = TftpOptionNegotiator.ValidateRequestOptions(
            new List<TftpOption>(),
            20);

        TftpOptionNegotiationException successFailure =
            Assert.Throws<TftpOptionNegotiationException>(
                () => TftpOptionNegotiator.CreateOptionErrorPacket(success));
        TftpOptionNegotiationException nullFailure =
            Assert.Throws<TftpOptionNegotiationException>(
                () => TftpOptionNegotiator.CreateOptionErrorPacket(null!));

        Assert.Multiple(() =>
        {
            Assert.That(successFailure.Message, Does.Contain("failure is required"));
            Assert.That(nullFailure.Message, Does.Contain("failure is required"));
        });
    }

    private static void AssertOptionError(
        TftpOptionNegotiationResult result,
        string messagePart,
        string optionName,
        string optionValue)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.HasError, Is.True);
            Assert.That(result.ErrorCode, Is.EqualTo(TftpErrorCode.OptionNegotiationFailed));
            Assert.That(result.FailureMessage, Does.Contain(messagePart));
            Assert.That(result.OptionName, Is.EqualTo(optionName));
            Assert.That(result.OptionValue, Is.EqualTo(optionValue));
        });
    }

    private static void AssertCapacityError(
        TftpOptionNegotiationResult result,
        string messagePart,
        string optionName,
        string optionValue)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.HasError, Is.True);
            Assert.That(result.ErrorCode, Is.EqualTo(TftpErrorCode.StorageExhausted));
            Assert.That(result.FailureMessage, Does.Contain(messagePart));
            Assert.That(result.OptionName, Is.EqualTo(optionName));
            Assert.That(result.OptionValue, Is.EqualTo(optionValue));
        });
    }

    private static IEnumerable<TestCaseData> InvalidRequestedOptions()
    {
        yield return new TestCaseData(null, "list cannot be null", "", "");
        yield return new TestCaseData(
            new List<TftpOption> { null! },
            "null option",
            "",
            "");
        yield return new TestCaseData(
            new List<TftpOption> { new TftpOption { Name = null!, Value = "1" } },
            "name cannot be null",
            "",
            "1");
        yield return new TestCaseData(
            new List<TftpOption> { new TftpOption { Name = "", Value = "1" } },
            "name cannot be empty",
            "",
            "1");
        yield return new TestCaseData(
            new List<TftpOption> { new TftpOption { Name = "time\0out", Value = "1" } },
            "embedded NUL",
            "time\0out",
            "1");
        yield return new TestCaseData(
            new List<TftpOption> { new TftpOption { Name = "t\u00edmeout", Value = "1" } },
            "ASCII",
            "t\u00edmeout",
            "1");
        yield return new TestCaseData(
            new List<TftpOption> { new TftpOption { Name = "timeout", Value = null! } },
            "value cannot be null",
            "timeout",
            "");
        yield return new TestCaseData(
            new List<TftpOption> { new TftpOption { Name = "timeout", Value = "" } },
            "value cannot be empty",
            "timeout",
            "");
        yield return new TestCaseData(
            new List<TftpOption> { new TftpOption { Name = "timeout", Value = "1\0x" } },
            "embedded NUL",
            "timeout",
            "1\0x");
        yield return new TestCaseData(
            new List<TftpOption> { new TftpOption { Name = "timeout", Value = "tr\u00eas" } },
            "ASCII",
            "timeout",
            "tr\u00eas");
        yield return new TestCaseData(
            new List<TftpOption>
            {
                new TftpOption { Name = "timeout", Value = "1" },
                new TftpOption { Name = "TIMEOUT", Value = "2" }
            },
            "duplicate",
            "TIMEOUT",
            "2");
    }
}
