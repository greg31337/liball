namespace ftp.Tests;

public class PublicEnumTests
{
    [Test]
    public void Every_public_enum_member_has_a_unique_int32_value()
    {
        Type[] enumTypes = typeof(FtpCommandKind).Assembly.GetTypes()
            .Where(type => type.IsPublic && type.IsEnum)
            .OrderBy(type => type.Name)
            .ToArray();

        Assert.That(enumTypes, Has.Length.EqualTo(28));
        foreach (Type enumType in enumTypes)
        {
            string[] names = Enum.GetNames(enumType);
            int[] values = Enum.GetValues(enumType)
                .Cast<object>()
                .Select(Convert.ToInt32)
                .ToArray();
            Assert.Multiple(() =>
            {
                Assert.That(Enum.GetUnderlyingType(enumType),
                    Is.EqualTo(typeof(int)), enumType.Name);
                Assert.That(names, Is.Not.Empty, enumType.Name);
                Assert.That(names.Distinct().Count(),
                    Is.EqualTo(names.Length), enumType.Name);
                Assert.That(values.Distinct().Count(),
                    Is.EqualTo(values.Length), enumType.Name);
            });
        }
    }

    [Test]
    public void FtpSecurityMode_has_stable_names_and_values()
    {
        AssertEnum<FtpSecurityMode>(
            new[]
            {
                nameof(FtpSecurityMode.Plain),
                nameof(FtpSecurityMode.ExplicitTlsRequired),
                nameof(FtpSecurityMode.ExplicitTlsOpportunistic),
                nameof(FtpSecurityMode.ImplicitTls),
            },
            new[]
            {
                0,
                1,
                2,
                3,
            });
    }

    [Test]
    public void FtpDataProtectionLevel_has_stable_names_and_values()
    {
        AssertEnum<FtpDataProtectionLevel>(
            new[]
            {
                nameof(FtpDataProtectionLevel.Clear),
                nameof(FtpDataProtectionLevel.Private),
            },
            new[]
            {
                0,
                1,
            });
    }

    [Test]
    public void FtpDataConnectionMode_has_stable_names_and_values()
    {
        AssertEnum<FtpDataConnectionMode>(
            new[]
            {
                nameof(FtpDataConnectionMode.Automatic),
                nameof(FtpDataConnectionMode.Passive),
                nameof(FtpDataConnectionMode.Active),
            },
            new[]
            {
                0,
                1,
                2,
            });
    }

    [Test]
    public void FtpAddressFamily_has_stable_names_and_values()
    {
        AssertEnum<FtpAddressFamily>(
            new[]
            {
                nameof(FtpAddressFamily.Unspecified),
                nameof(FtpAddressFamily.IPv4),
                nameof(FtpAddressFamily.IPv6),
            },
            new[]
            {
                0,
                1,
                2,
            });
    }

    [Test]
    public void FtpTransferType_has_stable_names_and_values()
    {
        AssertEnum<FtpTransferType>(
            new[]
            {
                nameof(FtpTransferType.Ascii),
                nameof(FtpTransferType.Ebcdic),
                nameof(FtpTransferType.Image),
                nameof(FtpTransferType.LocalByte),
            },
            new[]
            {
                0,
                1,
                2,
                3,
            });
    }

    [Test]
    public void FtpTextFormat_has_stable_names_and_values()
    {
        AssertEnum<FtpTextFormat>(
            new[]
            {
                nameof(FtpTextFormat.NonPrint),
                nameof(FtpTextFormat.Telnet),
                nameof(FtpTextFormat.CarriageControl),
            },
            new[]
            {
                0,
                1,
                2,
            });
    }

    [Test]
    public void FtpFileStructure_has_stable_names_and_values()
    {
        AssertEnum<FtpFileStructure>(
            new[]
            {
                nameof(FtpFileStructure.File),
                nameof(FtpFileStructure.Record),
                nameof(FtpFileStructure.Page),
            },
            new[]
            {
                0,
                1,
                2,
            });
    }

    [Test]
    public void FtpTransferMode_has_stable_names_and_values()
    {
        AssertEnum<FtpTransferMode>(
            new[]
            {
                nameof(FtpTransferMode.Stream),
                nameof(FtpTransferMode.Block),
                nameof(FtpTransferMode.Compressed),
                nameof(FtpTransferMode.Deflate),
            },
            new[]
            {
                0,
                1,
                2,
                3,
            });
    }

    [Test]
    public void FtpReplyCategory_has_stable_names_and_values()
    {
        AssertEnum<FtpReplyCategory>(
            new[]
            {
                nameof(FtpReplyCategory.Unknown),
                nameof(FtpReplyCategory.PositivePreliminary),
                nameof(FtpReplyCategory.PositiveCompletion),
                nameof(FtpReplyCategory.PositiveIntermediate),
                nameof(FtpReplyCategory.TransientNegativeCompletion),
                nameof(FtpReplyCategory.PermanentNegativeCompletion),
            },
            new[]
            {
                0,
                1,
                2,
                3,
                4,
                5,
            });
    }

    [Test]
    public void FtpReplyKind_has_stable_names_and_values()
    {
        AssertEnum<FtpReplyKind>(
            new[]
            {
                nameof(FtpReplyKind.Unknown),
                nameof(FtpReplyKind.Syntax),
                nameof(FtpReplyKind.Information),
                nameof(FtpReplyKind.Connections),
                nameof(FtpReplyKind.AuthenticationAndAccounting),
                nameof(FtpReplyKind.Unspecified),
                nameof(FtpReplyKind.FileSystem),
            },
            new[]
            {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
            });
    }

    [Test]
    public void FtpCommandKind_has_stable_names_and_values()
    {
        AssertEnum<FtpCommandKind>(
            new[]
            {
                nameof(FtpCommandKind.Unknown),
                nameof(FtpCommandKind.Abor),
                nameof(FtpCommandKind.Acct),
                nameof(FtpCommandKind.Allo),
                nameof(FtpCommandKind.Appe),
                nameof(FtpCommandKind.Cdup),
                nameof(FtpCommandKind.Cwd),
                nameof(FtpCommandKind.Dele),
                nameof(FtpCommandKind.Help),
                nameof(FtpCommandKind.List),
                nameof(FtpCommandKind.Mkd),
                nameof(FtpCommandKind.Mode),
                nameof(FtpCommandKind.Nlst),
                nameof(FtpCommandKind.Noop),
                nameof(FtpCommandKind.Pass),
                nameof(FtpCommandKind.Pasv),
                nameof(FtpCommandKind.Port),
                nameof(FtpCommandKind.Quit),
                nameof(FtpCommandKind.Rein),
                nameof(FtpCommandKind.Rest),
                nameof(FtpCommandKind.Retr),
                nameof(FtpCommandKind.Rmd),
                nameof(FtpCommandKind.Rnfr),
                nameof(FtpCommandKind.Rnto),
                nameof(FtpCommandKind.Site),
                nameof(FtpCommandKind.Smnt),
                nameof(FtpCommandKind.Stat),
                nameof(FtpCommandKind.Stor),
                nameof(FtpCommandKind.Stou),
                nameof(FtpCommandKind.Stru),
                nameof(FtpCommandKind.Syst),
                nameof(FtpCommandKind.Type),
                nameof(FtpCommandKind.User),
                nameof(FtpCommandKind.Pwd),
                nameof(FtpCommandKind.Feat),
                nameof(FtpCommandKind.Opts),
                nameof(FtpCommandKind.Eprt),
                nameof(FtpCommandKind.Epsv),
                nameof(FtpCommandKind.Lang),
                nameof(FtpCommandKind.Mdtm),
                nameof(FtpCommandKind.Mlst),
                nameof(FtpCommandKind.Mlsd),
                nameof(FtpCommandKind.Size),
                nameof(FtpCommandKind.Auth),
                nameof(FtpCommandKind.Pbsz),
                nameof(FtpCommandKind.Prot),
                nameof(FtpCommandKind.Ccc),
                nameof(FtpCommandKind.Adat),
                nameof(FtpCommandKind.Mic),
                nameof(FtpCommandKind.Conf),
                nameof(FtpCommandKind.Enc),
                nameof(FtpCommandKind.Host),
                nameof(FtpCommandKind.Algs),
                nameof(FtpCommandKind.Lprt),
                nameof(FtpCommandKind.Lpsv),
                nameof(FtpCommandKind.Xcup),
                nameof(FtpCommandKind.Xcwd),
                nameof(FtpCommandKind.Xmkd),
                nameof(FtpCommandKind.Xpwd),
                nameof(FtpCommandKind.Xrmd),
                nameof(FtpCommandKind.Pret),
                nameof(FtpCommandKind.Clnt),
                nameof(FtpCommandKind.Mfmt),
                nameof(FtpCommandKind.Hash),
                nameof(FtpCommandKind.Md5),
                nameof(FtpCommandKind.Xmd5),
                nameof(FtpCommandKind.Xsha1),
                nameof(FtpCommandKind.Xsha256),
                nameof(FtpCommandKind.Xsha512),
                nameof(FtpCommandKind.Xcrc),
                nameof(FtpCommandKind.Rmda),
            },
            new[]
            {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
                8,
                9,
                10,
                11,
                12,
                13,
                14,
                15,
                16,
                17,
                18,
                19,
                20,
                21,
                22,
                23,
                24,
                25,
                26,
                27,
                28,
                29,
                30,
                31,
                32,
                33,
                40,
                41,
                50,
                51,
                60,
                70,
                71,
                72,
                73,
                80,
                81,
                82,
                83,
                84,
                85,
                86,
                87,
                90,
                91,
                100,
                101,
                102,
                103,
                104,
                105,
                106,
                120,
                121,
                122,
                123,
                124,
                125,
                126,
                127,
                128,
                129,
                130,
            });
    }

    [Test]
    public void FtpFeatureSupport_has_stable_names_and_values()
    {
        AssertEnum<FtpFeatureSupport>(
            new[]
            {
                nameof(FtpFeatureSupport.Unknown),
                nameof(FtpFeatureSupport.NotSupported),
                nameof(FtpFeatureSupport.Supported),
            },
            new[]
            {
                0,
                1,
                2,
            });
    }

    [Test]
    public void FtpEntryType_has_stable_names_and_values()
    {
        AssertEnum<FtpEntryType>(
            new[]
            {
                nameof(FtpEntryType.Unknown),
                nameof(FtpEntryType.File),
                nameof(FtpEntryType.Directory),
                nameof(FtpEntryType.CurrentDirectory),
                nameof(FtpEntryType.ParentDirectory),
                nameof(FtpEntryType.SymbolicLink),
            },
            new[]
            {
                0,
                1,
                2,
                3,
                4,
                5,
            });
    }

    [Test]
    public void FtpPermission_has_stable_names_and_values()
    {
        AssertEnum<FtpPermission>(
            new[]
            {
                nameof(FtpPermission.None),
                nameof(FtpPermission.Append),
                nameof(FtpPermission.CreateFile),
                nameof(FtpPermission.DeleteFile),
                nameof(FtpPermission.EnterDirectory),
                nameof(FtpPermission.Rename),
                nameof(FtpPermission.List),
                nameof(FtpPermission.CreateDirectory),
                nameof(FtpPermission.DeleteDirectory),
                nameof(FtpPermission.Read),
                nameof(FtpPermission.Write),
            },
            new[]
            {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
                8,
                9,
                10,
            });
    }

    [Test]
    public void FtpCollisionPolicy_has_stable_names_and_values()
    {
        AssertEnum<FtpCollisionPolicy>(
            new[]
            {
                nameof(FtpCollisionPolicy.Overwrite),
                nameof(FtpCollisionPolicy.Skip),
                nameof(FtpCollisionPolicy.Resume),
                nameof(FtpCollisionPolicy.Append),
                nameof(FtpCollisionPolicy.Fail),
                nameof(FtpCollisionPolicy.Unique),
            },
            new[]
            {
                0,
                1,
                2,
                3,
                4,
                5,
            });
    }

    [Test]
    public void FtpVerificationMode_has_stable_names_and_values()
    {
        AssertEnum<FtpVerificationMode>(
            new[]
            {
                nameof(FtpVerificationMode.None),
                nameof(FtpVerificationMode.Size),
                nameof(FtpVerificationMode.ModifiedTime),
                nameof(FtpVerificationMode.SizeAndModifiedTime),
                nameof(FtpVerificationMode.Checksum),
            },
            new[]
            {
                0,
                1,
                2,
                3,
                4,
            });
    }

    [Test]
    public void FtpResumePolicy_has_stable_names_and_values()
    {
        AssertEnum<FtpResumePolicy>(
            new[]
            {
                nameof(FtpResumePolicy.Disabled),
                nameof(FtpResumePolicy.IfSafe),
                nameof(FtpResumePolicy.Required),
            },
            new[]
            {
                0,
                1,
                2,
            });
    }

    [Test]
    public void FtpSymlinkPolicy_has_stable_names_and_values()
    {
        AssertEnum<FtpSymlinkPolicy>(
            new[]
            {
                nameof(FtpSymlinkPolicy.Skip),
                nameof(FtpSymlinkPolicy.Follow),
                nameof(FtpSymlinkPolicy.Preserve),
                nameof(FtpSymlinkPolicy.Fail),
            },
            new[]
            {
                0,
                1,
                2,
                3,
            });
    }

    [Test]
    public void FtpDirectorySyncMode_has_stable_names_and_values()
    {
        AssertEnum<FtpDirectorySyncMode>(
            new[]
            {
                nameof(FtpDirectorySyncMode.Copy),
                nameof(FtpDirectorySyncMode.Update),
                nameof(FtpDirectorySyncMode.Mirror),
            },
            new[]
            {
                0,
                1,
                2,
            });
    }

    [Test]
    public void FtpFailurePolicy_has_stable_names_and_values()
    {
        AssertEnum<FtpFailurePolicy>(
            new[]
            {
                nameof(FtpFailurePolicy.Stop),
                nameof(FtpFailurePolicy.Continue),
            },
            new[]
            {
                0,
                1,
            });
    }

    [Test]
    public void FtpTransferDirection_has_stable_names_and_values()
    {
        AssertEnum<FtpTransferDirection>(
            new[]
            {
                nameof(FtpTransferDirection.Download),
                nameof(FtpTransferDirection.Upload),
                nameof(FtpTransferDirection.ServerToServer),
            },
            new[]
            {
                0,
                1,
                2,
            });
    }

    [Test]
    public void FtpServerState_has_stable_names_and_values()
    {
        AssertEnum<FtpServerState>(
            new[]
            {
                nameof(FtpServerState.Stopped),
                nameof(FtpServerState.Starting),
                nameof(FtpServerState.Running),
                nameof(FtpServerState.Draining),
                nameof(FtpServerState.Stopping),
                nameof(FtpServerState.Faulted),
            },
            new[]
            {
                0,
                1,
                2,
                3,
                4,
                5,
            });
    }

    [Test]
    public void FtpSessionState_has_stable_names_and_values()
    {
        AssertEnum<FtpSessionState>(
            new[]
            {
                nameof(FtpSessionState.Disconnected),
                nameof(FtpSessionState.Connected),
                nameof(FtpSessionState.HostSelected),
                nameof(FtpSessionState.Authenticating),
                nameof(FtpSessionState.Authenticated),
                nameof(FtpSessionState.Transferring),
                nameof(FtpSessionState.Closing),
                nameof(FtpSessionState.Closed),
            },
            new[]
            {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
            });
    }

    [Test]
    public void FtpAuthenticationStatus_has_stable_names_and_values()
    {
        AssertEnum<FtpAuthenticationStatus>(
            new[]
            {
                nameof(FtpAuthenticationStatus.NotHandled),
                nameof(FtpAuthenticationStatus.Success),
                nameof(FtpAuthenticationStatus.Failure),
                nameof(FtpAuthenticationStatus.AdditionalInformationRequired),
            },
            new[]
            {
                0,
                1,
                2,
                3,
            });
    }

    [Test]
    public void FtpAuthorizationOperation_has_stable_names_and_values()
    {
        AssertEnum<FtpAuthorizationOperation>(
            new[]
            {
                nameof(FtpAuthorizationOperation.List),
                nameof(FtpAuthorizationOperation.Read),
                nameof(FtpAuthorizationOperation.Write),
                nameof(FtpAuthorizationOperation.Append),
                nameof(FtpAuthorizationOperation.CreateDirectory),
                nameof(FtpAuthorizationOperation.DeleteFile),
                nameof(FtpAuthorizationOperation.DeleteDirectory),
                nameof(FtpAuthorizationOperation.Rename),
                nameof(FtpAuthorizationOperation.ReadMetadata),
                nameof(FtpAuthorizationOperation.WriteMetadata),
                nameof(FtpAuthorizationOperation.Site),
                nameof(FtpAuthorizationOperation.ServerToServer),
            },
            new[]
            {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
                8,
                9,
                10,
                11,
            });
    }

    [Test]
    public void FtpFileOpenMode_has_stable_names_and_values()
    {
        AssertEnum<FtpFileOpenMode>(
            new[]
            {
                nameof(FtpFileOpenMode.Read),
                nameof(FtpFileOpenMode.CreateNew),
                nameof(FtpFileOpenMode.Overwrite),
                nameof(FtpFileOpenMode.Append),
                nameof(FtpFileOpenMode.Resume),
            },
            new[]
            {
                0,
                1,
                2,
                3,
                4,
            });
    }

    [Test]
    public void FtpEventKind_has_stable_names_and_values()
    {
        AssertEnum<FtpEventKind>(
            new[]
            {
                nameof(FtpEventKind.Unknown),
                nameof(FtpEventKind.Lifecycle),
                nameof(FtpEventKind.Connection),
                nameof(FtpEventKind.Authentication),
                nameof(FtpEventKind.Command),
                nameof(FtpEventKind.Reply),
                nameof(FtpEventKind.Authorization),
                nameof(FtpEventKind.FileSystem),
                nameof(FtpEventKind.TransferStarted),
                nameof(FtpEventKind.TransferProgress),
                nameof(FtpEventKind.TransferCompleted),
                nameof(FtpEventKind.Tls),
                nameof(FtpEventKind.PolicyRejection),
                nameof(FtpEventKind.Fault),
            },
            new[]
            {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
                8,
                9,
                10,
                11,
                12,
                13,
            });
    }

    [Test]
    public void FtpExtensionDisposition_has_stable_names_and_values()
    {
        AssertEnum<FtpExtensionDisposition>(
            new[]
            {
                nameof(FtpExtensionDisposition.NotHandled),
                nameof(FtpExtensionDisposition.Handled),
                nameof(FtpExtensionDisposition.Rejected),
            },
            new[]
            {
                0,
                1,
                2,
            });
    }

    private static void AssertEnum<T>(string[] expectedNames, int[] expectedValues)
        where T : struct, Enum
    {
        Assert.Multiple(() =>
        {
            Assert.That(Enum.GetUnderlyingType(typeof(T)), Is.EqualTo(typeof(int)));
            Assert.That(Enum.GetNames<T>(), Is.EqualTo(expectedNames));
            Assert.That(
                Enum.GetValues<T>().Select(value => Convert.ToInt32(value)).ToArray(),
                Is.EqualTo(expectedValues));
        });
    }
}
