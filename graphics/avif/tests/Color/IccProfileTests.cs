using System.Buffers.Binary;

namespace avif.Tests;

public class IccProfileTests
{
    [TestCase(2, false)]
    [TestCase(4, true)]
    public void MatrixTrcProfilesParseAndRoundTrip(
        int version,
        bool parametric)
    {
        byte[] profileBytes = BuildMatrixProfile(
            version, parametric, new double[] { 0.9642, 1, 0.8249 });
        AvifIccProfile profile = AvifIccProfile.Create(profileBytes);
        double[] source = new double[] { 0.25, 0.5, 0.75 };
        double[] pcs = new double[3];
        double[] restored = new double[3];

        AvifResultCode forward = profile.TransformToPcs(
            source, pcs, AvifRenderingIntent.RelativeColorimetric);
        AvifResultCode reverse = profile.TransformFromPcs(
            pcs, restored, AvifRenderingIntent.RelativeColorimetric);

        Assert.Multiple(() =>
        {
            Assert.That(profile.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(profile.GetVersionMajor(), Is.EqualTo(version));
            Assert.That(profile.GetDeviceClass(), Is.EqualTo("mntr"));
            Assert.That(profile.GetColorSpace(), Is.EqualTo("RGB "));
            Assert.That(profile.GetProfileConnectionSpace(), Is.EqualTo("XYZ "));
            Assert.That(profile.GetRenderingIntent(),
                Is.EqualTo(AvifRenderingIntent.RelativeColorimetric));
            Assert.That(profile.GetChannelCount(), Is.EqualTo(3));
            Assert.That(profile.GetTagCount(), Is.EqualTo(8));
            Assert.That(profile.GetTagSignature(0), Is.EqualTo("rXYZ"));
            Assert.That(profile.GetTagType(0), Is.EqualTo("XYZ "));
            Assert.That(profile.CanTransformToPcs(), Is.True);
            Assert.That(profile.CanTransformFromPcs(), Is.True);
            Assert.That(forward, Is.EqualTo(AvifResultCode.Success));
            Assert.That(reverse, Is.EqualTo(AvifResultCode.Success));
            Assert.That(restored, Is.EqualTo(source).Within(2e-8));
            Assert.That(profile.GetData(), Is.EqualTo(profileBytes));
        });
    }

    [Test]
    public void MatrixColorantsAndParametricCurvesProduceReferenceXyz()
    {
        AvifIccProfile profile = AvifIccProfile.Create(BuildMatrixProfile(
            4, true, new double[] { 0.9642, 1, 0.8249 }));
        double[] redPcs = new double[3];
        double[] grayPcs = new double[3];

        profile.TransformToPcs(
            new double[] { 1, 0, 0 },
            redPcs,
            AvifRenderingIntent.Perceptual);
        profile.TransformToPcs(
            new double[] { 0.5, 0.5, 0.5 },
            grayPcs,
            AvifRenderingIntent.Saturation);

        Assert.Multiple(() =>
        {
            Assert.That(redPcs[0], Is.EqualTo(0.43608).Within(2e-5));
            Assert.That(redPcs[1], Is.EqualTo(0.2225).Within(2e-5));
            Assert.That(redPcs[2], Is.EqualTo(0.01393).Within(2e-5));
            Assert.That(grayPcs[0], Is.EqualTo(0.24105).Within(3e-5));
            Assert.That(grayPcs[1], Is.EqualTo(0.25).Within(3e-5));
            Assert.That(grayPcs[2], Is.EqualTo(0.2063).Within(3e-5));
        });
    }

    [Test]
    public void AbsoluteIntentUsesMediaWhiteAndProfileConversion()
    {
        AvifIccProfile sourceProfile = AvifIccProfile.Create(BuildMatrixProfile(
            4, false, new double[] { 0.8, 0.9, 0.7 }));
        AvifIccProfile destinationProfile = AvifIccProfile.Create(
            BuildMatrixProfile(
                4, false, new double[] { 0.9642, 1, 0.8249 }));
        double[] relative = new double[3];
        double[] absolute = new double[3];
        double[] converted = new double[3];

        sourceProfile.TransformToPcs(
            new double[] { 1, 1, 1 },
            relative,
            AvifRenderingIntent.RelativeColorimetric);
        sourceProfile.TransformToPcs(
            new double[] { 1, 1, 1 },
            absolute,
            AvifRenderingIntent.AbsoluteColorimetric);
        AvifResultCode conversion = sourceProfile.Convert(
            new double[] { 0.2, 0.4, 0.6 },
            destinationProfile,
            converted,
            AvifRenderingIntent.RelativeColorimetric);

        Assert.Multiple(() =>
        {
            Assert.That(relative[0], Is.EqualTo(0.96422).Within(3e-5));
            Assert.That(absolute[0], Is.EqualTo(0.8).Within(3e-5));
            Assert.That(absolute[1], Is.EqualTo(0.9).Within(3e-5));
            Assert.That(absolute[2], Is.EqualTo(0.7).Within(4e-4));
            Assert.That(conversion, Is.EqualTo(AvifResultCode.Success));
            Assert.That(converted,
                Is.EqualTo(new double[] { 0.2, 0.4, 0.6 }).Within(2e-8));
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Lut8AndMultiProcessClutsRoundTrip(bool multiProcess)
    {
        byte[] forward = multiProcess ? BuildMab("mAB ") : BuildLut8();
        byte[] reverse = multiProcess ? BuildMab("mBA ") : BuildLut8();
        AvifIccProfile profile = AvifIccProfile.Create(BuildProfile(
            4,
            "RGB ",
            "XYZ ",
            new IccTestTag[]
            {
                new IccTestTag("A2B1", forward),
                new IccTestTag("B2A1", reverse)
            },
            AvifRenderingIntent.RelativeColorimetric));
        double[] source = new double[] { 0.2, 0.4, 0.6 };
        double[] pcs = new double[3];
        double[] restored = new double[3];

        AvifResultCode toPcs = profile.TransformToPcs(
            source, pcs, AvifRenderingIntent.RelativeColorimetric);
        AvifResultCode fromPcs = profile.TransformFromPcs(
            pcs, restored, AvifRenderingIntent.RelativeColorimetric);

        Assert.Multiple(() =>
        {
            Assert.That(profile.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(toPcs, Is.EqualTo(AvifResultCode.Success));
            Assert.That(fromPcs, Is.EqualTo(AvifResultCode.Success));
            Assert.That(pcs[0], Is.EqualTo(0.4).Within(0.01));
            Assert.That(pcs[1], Is.EqualTo(0.8).Within(0.01));
            Assert.That(pcs[2], Is.EqualTo(1.2).Within(0.01));
            Assert.That(restored, Is.EqualTo(source).Within(0.01));
        });
    }

    [Test]
    public void Lut16TablesAndClutUseFullSixteenBitPrecision()
    {
        byte[] lut = BuildLut16();
        AvifIccProfile profile = AvifIccProfile.Create(BuildProfile(
            4,
            "RGB ",
            "XYZ ",
            new IccTestTag[]
            {
                new IccTestTag("A2B0", lut),
                new IccTestTag("B2A0", lut)
            },
            AvifRenderingIntent.Perceptual));
        double[] source = new double[] { 0.1234, 0.4567, 0.7891 };
        double[] pcs = new double[3];
        double[] restored = new double[3];

        profile.TransformToPcs(
            source, pcs, AvifRenderingIntent.Perceptual);
        profile.TransformFromPcs(
            pcs, restored, AvifRenderingIntent.Perceptual);

        Assert.Multiple(() =>
        {
            Assert.That(profile.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(restored, Is.EqualTo(source).Within(2e-5));
        });
    }

    [Test]
    public void LabPcsLutConvertsThroughD50Xyz()
    {
        AvifIccProfile profile = AvifIccProfile.Create(BuildProfile(
            4,
            "RGB ",
            "Lab ",
            new IccTestTag[]
            {
                new IccTestTag("A2B0", BuildLut8()),
                new IccTestTag("B2A0", BuildLut8())
            },
            AvifRenderingIntent.Perceptual));
        double[] neutralLabEncoding = new double[]
        {
            0.5,
            128.0 / 255,
            128.0 / 255
        };
        double[] xyz = new double[3];
        double[] restored = new double[3];

        profile.TransformToPcs(
            neutralLabEncoding, xyz, AvifRenderingIntent.Perceptual);
        profile.TransformFromPcs(
            xyz, restored, AvifRenderingIntent.Perceptual);

        Assert.Multiple(() =>
        {
            Assert.That(xyz[0], Is.EqualTo(0.1776).Within(0.01));
            Assert.That(xyz[1], Is.EqualTo(0.1842).Within(0.01));
            Assert.That(xyz[2], Is.EqualTo(0.1519).Within(0.01));
            Assert.That(restored, Is.EqualTo(neutralLabEncoding).Within(0.01));
        });
    }

    [Test]
    public void GrayTrcProfilesTransformThroughTheirMediaWhite()
    {
        AvifIccProfile profile = AvifIccProfile.Create(BuildProfile(
            2,
            "GRAY",
            "XYZ ",
            new IccTestTag[]
            {
                new IccTestTag("kTRC", BuildCurve(2)),
                new IccTestTag("wtpt", BuildXyz(0.9642, 1, 0.8249))
            },
            AvifRenderingIntent.RelativeColorimetric));
        double[] xyz = new double[3];
        double[] gray = new double[1];

        AvifResultCode forward = profile.TransformToPcs(
            new double[] { 0.5 }, xyz,
            AvifRenderingIntent.RelativeColorimetric);
        AvifResultCode reverse = profile.TransformFromPcs(
            xyz, gray, AvifRenderingIntent.RelativeColorimetric);

        Assert.Multiple(() =>
        {
            Assert.That(forward, Is.EqualTo(AvifResultCode.Success));
            Assert.That(reverse, Is.EqualTo(AvifResultCode.Success));
            Assert.That(xyz[1], Is.EqualTo(0.25).Within(1e-8));
            Assert.That(gray[0], Is.EqualTo(0.5).Within(1e-8));
        });
    }

    [Test]
    public void ValidProfileWithoutTransformsRemainsInspectable()
    {
        AvifIccProfile profile = AvifIccProfile.Create(BuildProfile(
            4,
            "CMYK",
            "Lab ",
            Array.Empty<IccTestTag>(),
            AvifRenderingIntent.Perceptual));
        double[] destination = new double[3];

        Assert.Multiple(() =>
        {
            Assert.That(profile.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(profile.GetChannelCount(), Is.EqualTo(4));
            Assert.That(profile.CanTransformToPcs(), Is.False);
            Assert.That(profile.TransformToPcs(
                    new double[] { 0, 0, 0, 1 },
                    destination,
                    AvifRenderingIntent.Perceptual),
                Is.EqualTo(AvifResultCode.UnsupportedFeature));
        });
    }

    [Test]
    public void InvalidProfilesExposeStableDefaultsAndPropagateTheirError()
    {
        AvifIccProfile invalid = AvifIccProfile.Create(new byte[20]);
        AvifIccProfile valid = AvifIccProfile.Create(BuildMatrixProfile(
            4, false, new double[] { 0.9642, 1, 0.8249 }));
        double[] destination = new double[] { 7, 8, 9 };

        Assert.Multiple(() =>
        {
            Assert.That(invalid.GetCode(),
                Is.EqualTo(AvifResultCode.InvalidMetadata));
            Assert.That(invalid.GetData(), Is.Empty);
            Assert.That(invalid.GetVersionMajor(), Is.Zero);
            Assert.That(invalid.GetVersionMinor(), Is.Zero);
            Assert.That(invalid.GetDeviceClass(), Is.Empty);
            Assert.That(invalid.GetColorSpace(), Is.Empty);
            Assert.That(invalid.GetProfileConnectionSpace(), Is.Empty);
            Assert.That(invalid.GetChannelCount(), Is.Zero);
            Assert.That(invalid.GetTagCount(), Is.Zero);
            Assert.That(invalid.GetTagSignature(0), Is.Empty);
            Assert.That(invalid.GetTagType(0), Is.Empty);
            Assert.That(invalid.CanTransformToPcs(), Is.False);
            Assert.That(invalid.CanTransformFromPcs(), Is.False);
            Assert.That(invalid.TransformToPcs(
                    new double[] { 1, 1, 1 },
                    destination,
                    AvifRenderingIntent.Perceptual),
                Is.EqualTo(AvifResultCode.InvalidMetadata));
            Assert.That(invalid.TransformFromPcs(
                    new double[] { 1, 1, 1 },
                    destination,
                    AvifRenderingIntent.Perceptual),
                Is.EqualTo(AvifResultCode.InvalidMetadata));
            Assert.That(invalid.Convert(
                    new double[] { 1, 1, 1 },
                    valid,
                    destination,
                    AvifRenderingIntent.Perceptual),
                Is.EqualTo(AvifResultCode.InvalidMetadata));
            Assert.That(destination, Is.EqualTo(new double[] { 7, 8, 9 }));
        });
    }

    [Test]
    public void ParserRejectsMalformedHeadersTablesOverlapsAndCurves()
    {
        byte[] valid = BuildMatrixProfile(
            4, false, new double[] { 0.9642, 1, 0.8249 });
        byte[] wrongSize = (byte[])valid.Clone();
        WriteUInt32(wrongSize, 0, (uint)(valid.Length - 1));
        byte[] wrongSignature = (byte[])valid.Clone();
        WriteSignature(wrongSignature, 36, "nope");
        byte[] badVersion = (byte[])valid.Clone();
        badVersion[8] = 3;
        byte[] badOffset = (byte[])valid.Clone();
        WriteUInt32(badOffset, 136, 130);
        byte[] overlap = (byte[])valid.Clone();
        uint firstOffset = ReadUInt32(overlap, 136);
        WriteUInt32(overlap, 148, firstOffset + 4);
        byte[] malformedCurve = BuildProfile(
            4,
            "RGB ",
            "XYZ ",
            new IccTestTag[]
            {
                new IccTestTag("rTRC", BuildMalformedCurve())
            },
            AvifRenderingIntent.Perceptual);

        Assert.Multiple(() =>
        {
            Assert.That(AvifIccProfile.Create(new byte[20]).GetCode(),
                Is.EqualTo(AvifResultCode.InvalidMetadata));
            Assert.That(AvifIccProfile.Create(wrongSize).GetCode(),
                Is.EqualTo(AvifResultCode.InvalidMetadata));
            Assert.That(AvifIccProfile.Create(wrongSignature).GetCode(),
                Is.EqualTo(AvifResultCode.InvalidMetadata));
            Assert.That(AvifIccProfile.Create(badVersion).GetCode(),
                Is.EqualTo(AvifResultCode.InvalidMetadata));
            Assert.That(AvifIccProfile.Create(badOffset).GetCode(),
                Is.EqualTo(AvifResultCode.InvalidMetadata));
            Assert.That(AvifIccProfile.Create(overlap).GetCode(),
                Is.EqualTo(AvifResultCode.InvalidMetadata));
            Assert.That(AvifIccProfile.Create(malformedCurve).GetCode(),
                Is.EqualTo(AvifResultCode.InvalidMetadata));
        });
    }

    [Test]
    public void ParserEnforcesIccAndMetadataLimitsAndCallerBuffers()
    {
        byte[] data = BuildMatrixProfile(
            4, false, new double[] { 0.9642, 1, 0.8249 });
        AvifSecurityLimits limits = new AvifSecurityLimits();
        limits.MaxIccBytes = data.Length - 1;
        AvifIccProfile limited = AvifIccProfile.Create(data, limits);
        AvifIccProfile valid = AvifIccProfile.Create(data);

        Assert.Multiple(() =>
        {
            Assert.That(limited.GetCode(),
                Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(valid.TransformToPcs(
                    new double[] { 1, 1 },
                    new double[3],
                    AvifRenderingIntent.Perceptual),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(valid.TransformFromPcs(
                    new double[] { 1, 1, 1 },
                    new double[2],
                    AvifRenderingIntent.Perceptual),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(valid.TransformToPcs(
                    new double[] { 1, 1, 1 },
                    new double[3],
                    (AvifRenderingIntent)9),
                Is.EqualTo(AvifResultCode.InvalidArgument));
        });
    }

    [Test]
    public void ImagesPreserveProfilesAndColorConverterHonorsPrecedence()
    {
        AvifIccProfile swapped = AvifIccProfile.Create(
            BuildSwappedMatrixProfile());
        AvifIccProfile invalid = AvifIccProfile.Create(new byte[12]);
        AvifImage image = AvifImage.Create(
            1, 1, 8, AvifChromaFormat.Yuv444, false);
        image.SetColorInfo(AvifColorInfo.Create(
            AvifColorPrimaries.Bt709,
            AvifTransferCharacteristics.Srgb,
            AvifMatrixCoefficients.Identity,
            AvifRange.Full));
        (image.GetPlane(AvifPlaneKind.Y) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(0)[0] = 0;
        (image.GetPlane(AvifPlaneKind.U) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(0)[0] = 0;
        (image.GetPlane(AvifPlaneKind.V) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(0)[0] = 255;
        AvifResultCode attached = image.SetIccProfile(swapped);
        AvifImage view = image.CreateView(0, 0, 1, 1);
        AvifImage copy = image.CreateCopy();
        AvifPixelBuffer profiled = AvifColorConverter.Create().Convert(
            image, AvifPixelFormat.Rgb);
        AvifColorConversionOptions cicpOptions =
            new AvifColorConversionOptions();
        cicpOptions.UseEmbeddedIccProfile = false;
        AvifPixelBuffer cicp = AvifColorConverter.Create(cicpOptions).Convert(
            image, AvifPixelFormat.Rgb);

        Assert.Multiple(() =>
        {
            Assert.That(attached, Is.EqualTo(AvifResultCode.Success));
            Assert.That(image.SetIccProfile(invalid),
                Is.EqualTo(AvifResultCode.InvalidMetadata));
            Assert.That((view.GetIccProfile() ?? throw new InvalidOperationException("Invalid internal state.")).GetCode(),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That((copy.GetIccProfile() ?? throw new InvalidOperationException("Invalid internal state.")).GetData(),
                Is.EqualTo(swapped.GetData()));
            Assert.That(profiled.GetRowSpan(0)[0], Is.LessThan(3));
            Assert.That(profiled.GetRowSpan(0)[2], Is.GreaterThan(252));
            Assert.That(cicp.GetRowSpan(0).ToArray(),
                Is.EqualTo(new byte[] { 255, 0, 0 }));
        });

        profiled.Close();
        cicp.Close();
        view.Close();
        copy.Close();
        image.Close();
    }

    static byte[] BuildMatrixProfile(
        int version,
        bool parametric,
        double[] white)
    {
        byte[] curve = parametric
            ? BuildParametricCurve(2) : BuildCurve(1);
        return BuildProfile(
            version,
            "RGB ",
            "XYZ ",
            new IccTestTag[]
            {
                new IccTestTag("rXYZ", BuildXyz(0.43608, 0.2225, 0.01393)),
                new IccTestTag("gXYZ", BuildXyz(0.38507, 0.71687, 0.0971)),
                new IccTestTag("bXYZ", BuildXyz(0.14307, 0.06061, 0.71417)),
                new IccTestTag("rTRC", curve),
                new IccTestTag("gTRC", curve),
                new IccTestTag("bTRC", curve),
                new IccTestTag("wtpt", BuildXyz(white[0], white[1], white[2])),
                new IccTestTag("chad", BuildS15Array(new double[]
                {
                    1, 0, 0,
                    0, 1, 0,
                    0, 0, 1
                }))
            },
            AvifRenderingIntent.RelativeColorimetric);
    }

    static byte[] BuildSwappedMatrixProfile()
    {
        byte[] curve = BuildCurve(1);
        return BuildProfile(
            4,
            "RGB ",
            "XYZ ",
            new IccTestTag[]
            {
                new IccTestTag("rXYZ", BuildXyz(0.14307, 0.06061, 0.71417)),
                new IccTestTag("gXYZ", BuildXyz(0.38507, 0.71687, 0.0971)),
                new IccTestTag("bXYZ", BuildXyz(0.43608, 0.2225, 0.01393)),
                new IccTestTag("rTRC", curve),
                new IccTestTag("gTRC", curve),
                new IccTestTag("bTRC", curve),
                new IccTestTag("wtpt", BuildXyz(0.9642, 1, 0.8249))
            },
            AvifRenderingIntent.RelativeColorimetric);
    }

    static byte[] BuildProfile(
        int version,
        string colorSpace,
        string pcs,
        IccTestTag[] tags,
        AvifRenderingIntent intent)
    {
        int tableEnd = 132 + tags.Length * 12;
        int position = AlignFour(tableEnd);
        for (int index = 0; index < tags.Length; index++)
        {
            tags[index].Offset = position;
            position += AlignFour(tags[index].Data.Length);
        }
        byte[] profile = new byte[position];
        WriteUInt32(profile, 0, (uint)profile.Length);
        profile[8] = (byte)version;
        profile[9] = version == 4 ? (byte)0x40 : (byte)0x10;
        WriteSignature(profile, 12, "mntr");
        WriteSignature(profile, 16, colorSpace);
        WriteSignature(profile, 20, pcs);
        WriteSignature(profile, 36, "acsp");
        WriteUInt32(profile, 64, (uint)intent);
        WriteS15(profile, 68, 0.9642);
        WriteS15(profile, 72, 1);
        WriteS15(profile, 76, 0.8249);
        WriteUInt32(profile, 128, (uint)tags.Length);
        for (int index = 0; index < tags.Length; index++)
        {
            int entry = 132 + index * 12;
            WriteSignature(profile, entry, tags[index].Signature);
            WriteUInt32(profile, entry + 4, (uint)tags[index].Offset);
            WriteUInt32(profile, entry + 8, (uint)tags[index].Data.Length);
            tags[index].Data.CopyTo(profile, tags[index].Offset);
        }
        return profile;
    }

    static byte[] BuildXyz(double x, double y, double z)
    {
        byte[] data = new byte[20];
        WriteSignature(data, 0, "XYZ ");
        WriteS15(data, 8, x);
        WriteS15(data, 12, y);
        WriteS15(data, 16, z);
        return data;
    }

    static byte[] BuildCurve(double gamma)
    {
        byte[] data = new byte[14];
        WriteSignature(data, 0, "curv");
        WriteUInt32(data, 8, 1);
        WriteUInt16(data, 12, (ushort)Math.Round(gamma * 256));
        return data;
    }

    static byte[] BuildParametricCurve(double gamma)
    {
        byte[] data = new byte[16];
        WriteSignature(data, 0, "para");
        WriteUInt16(data, 8, 0);
        WriteS15(data, 12, gamma);
        return data;
    }

    static byte[] BuildMalformedCurve()
    {
        byte[] data = new byte[12];
        WriteSignature(data, 0, "curv");
        WriteUInt32(data, 8, 100);
        return data;
    }

    static byte[] BuildS15Array(double[] values)
    {
        byte[] data = new byte[8 + values.Length * 4];
        WriteSignature(data, 0, "sf32");
        for (int index = 0; index < values.Length; index++)
        {
            WriteS15(data, 8 + index * 4, values[index]);
        }
        return data;
    }

    static byte[] BuildLut8()
    {
        byte[] data = new byte[48 + 3 * 256 + 8 * 3 + 3 * 256];
        WriteSignature(data, 0, "mft1");
        data[8] = 3;
        data[9] = 3;
        data[10] = 2;
        WriteS15(data, 12, 1);
        WriteS15(data, 28, 1);
        WriteS15(data, 44, 1);
        int position = 48;
        for (int channel = 0; channel < 3; channel++)
        {
            for (int entry = 0; entry < 256; entry++)
            {
                data[position++] = (byte)entry;
            }
        }
        for (int first = 0; first < 2; first++)
        {
            for (int second = 0; second < 2; second++)
            {
                for (int third = 0; third < 2; third++)
                {
                    data[position++] = (byte)(first * 255);
                    data[position++] = (byte)(second * 255);
                    data[position++] = (byte)(third * 255);
                }
            }
        }
        for (int channel = 0; channel < 3; channel++)
        {
            for (int entry = 0; entry < 256; entry++)
            {
                data[position++] = (byte)entry;
            }
        }
        return data;
    }

    static byte[] BuildLut16()
    {
        byte[] data = new byte[52 + 3 * 2 * 2 + 8 * 3 * 2 + 3 * 2 * 2];
        WriteSignature(data, 0, "mft2");
        data[8] = 3;
        data[9] = 3;
        data[10] = 2;
        WriteS15(data, 12, 1);
        WriteS15(data, 28, 1);
        WriteS15(data, 44, 1);
        WriteUInt16(data, 48, 2);
        WriteUInt16(data, 50, 2);
        int position = 52;
        for (int channel = 0; channel < 3; channel++)
        {
            WriteUInt16(data, position, 0);
            WriteUInt16(data, position + 2, 65535);
            position += 4;
        }
        for (int first = 0; first < 2; first++)
        {
            for (int second = 0; second < 2; second++)
            {
                for (int third = 0; third < 2; third++)
                {
                    WriteUInt16(data, position, (ushort)(first * 65535));
                    WriteUInt16(data, position + 2, (ushort)(second * 65535));
                    WriteUInt16(data, position + 4, (ushort)(third * 65535));
                    position += 6;
                }
            }
        }
        for (int channel = 0; channel < 3; channel++)
        {
            WriteUInt16(data, position, 0);
            WriteUInt16(data, position + 2, 65535);
            position += 4;
        }
        return data;
    }

    static byte[] BuildMab(string type)
    {
        byte[] identityCurve = new byte[12];
        WriteSignature(identityCurve, 0, "curv");
        byte[] data = new byte[148];
        WriteSignature(data, 0, type);
        data[8] = 3;
        data[9] = 3;
        WriteUInt32(data, 12, 32);
        WriteUInt32(data, 24, 104);
        WriteUInt32(data, 28, 68);
        for (int curve = 0; curve < 3; curve++)
        {
            identityCurve.CopyTo(data, 32 + curve * 12);
            identityCurve.CopyTo(data, 68 + curve * 12);
        }
        int clut = 104;
        data[clut] = 2;
        data[clut + 1] = 2;
        data[clut + 2] = 2;
        data[clut + 16] = 1;
        int position = clut + 20;
        for (int first = 0; first < 2; first++)
        {
            for (int second = 0; second < 2; second++)
            {
                for (int third = 0; third < 2; third++)
                {
                    data[position++] = (byte)(first * 255);
                    data[position++] = (byte)(second * 255);
                    data[position++] = (byte)(third * 255);
                }
            }
        }
        return data;
    }

    static void WriteSignature(byte[] data, int offset, string value)
    {
        for (int index = 0; index < 4; index++)
        {
            data[offset + index] = (byte)value[index];
        }
    }

    static void WriteUInt16(byte[] data, int offset, ushort value)
    {
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(offset, 2), value);
    }

    static void WriteUInt32(byte[] data, int offset, uint value)
    {
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(offset, 4), value);
    }

    static uint ReadUInt32(byte[] data, int offset)
    {
        return BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset, 4));
    }

    static void WriteS15(byte[] data, int offset, double value)
    {
        WriteUInt32(data, offset, (uint)(int)Math.Round(value * 65536));
    }

    static int AlignFour(int value)
    {
        return (value + 3) & -4;
    }

    class IccTestTag
    {
        internal string Signature;
        internal byte[] Data;
        internal int Offset;

        internal IccTestTag(string signature, byte[] data)
        {
            Signature = signature;
            Data = data;
        }
    }
}
