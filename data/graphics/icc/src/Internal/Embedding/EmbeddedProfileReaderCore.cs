namespace icc;

internal class EmbeddedProfileReaderCore
{
    internal EmbeddedProfileReaderCore()
    {
    }

    internal IccEmbeddedFormat DetectBytes(System.ReadOnlySpan<byte> carrierBytes)
    {
        return DetectCarrier(carrierBytes);
    }

    internal IccEmbeddedFormat DetectMemory(System.ReadOnlyMemory<byte> carrierMemory)
    {
        return DetectCarrier(carrierMemory.Span);
    }

    internal IccEmbeddedFormat DetectStream(System.IO.Stream carrierStream)
    {
        ValidateReadableStream(carrierStream);
        long start = carrierStream.CanSeek ? carrierStream.Position : 0;
        byte[] prefix = new byte[65536];
        int count = ProfileReaderCore.ReadAtMost(carrierStream, prefix, 0,
            prefix.Length);
        if (carrierStream.CanSeek)
        {
            carrierStream.Position = start;
        }
        return DetectCarrier(new System.ReadOnlySpan<byte>(prefix, 0, count));
    }

    internal IccEmbeddedProfileResult EnumerateBytes(
        System.ReadOnlySpan<byte> carrierBytes, IccEmbeddingOptions options)
    {
        ValidateEmbeddingOptions(options);
        return EnumerateMemoryAtOffset(carrierBytes.ToArray(), options, 0);
    }

    internal IccEmbeddedProfileResult EnumerateMemory(
        System.ReadOnlyMemory<byte> carrierMemory, IccEmbeddingOptions options)
    {
        ValidateEmbeddingOptions(options);
        return EnumerateMemoryAtOffset(carrierMemory, options, 0);
    }

    internal IccEmbeddedProfileResult EnumerateStream(
        System.IO.Stream carrierStream, IccEmbeddingOptions options)
    {
        ValidateEmbeddingOptions(options);
        EmbeddedCarrierBuffer buffer = ReadCarrierStream(carrierStream,
            options.ResourceLimits.MaximumCarrierBytes);
        if (buffer.LimitExceeded)
        {
            IccEmbeddedProfileResult limited = CreateEnumerationResult(options,
                IccEmbeddedFormat.Unknown);
            limited.Result = CreateResult(IccOperationStatus.ResourceLimitExceeded,
                $"The carrier exceeds the configured stream-enumeration limit of {options.ResourceLimits.MaximumCarrierBytes} bytes.",
                buffer.BaseOffset, options.ResourceLimits.MaximumCarrierBytes);
            return limited;
        }
        return EnumerateMemoryAtOffset(buffer.Bytes, options,
            buffer.BaseOffset);
    }

    internal IccProfile[] ExtractBytes(System.ReadOnlySpan<byte> carrierBytes,
        IccReadOptions options)
    {
        ProfileCodec.ValidateEnvelopeOptions(options);
        TagTableCodec.ValidateTableLimits(options.ResourceLimits);
        return ExtractMemoryAtOffset(carrierBytes.ToArray(), options, 0);
    }

    internal IccProfile[] ExtractMemory(System.ReadOnlyMemory<byte> carrierMemory,
        IccReadOptions options)
    {
        ProfileCodec.ValidateEnvelopeOptions(options);
        TagTableCodec.ValidateTableLimits(options.ResourceLimits);
        return ExtractMemoryAtOffset(carrierMemory, options, 0);
    }

    internal IccProfile[] ExtractStream(System.IO.Stream carrierStream,
        IccReadOptions options)
    {
        ProfileCodec.ValidateEnvelopeOptions(options);
        TagTableCodec.ValidateTableLimits(options.ResourceLimits);
        EmbeddedCarrierBuffer buffer = ReadCarrierStream(carrierStream,
            options.ResourceLimits.MaximumCarrierBytes);
        if (buffer.LimitExceeded)
        {
            throw new IccEmbeddingException(
                $"The carrier exceeds the configured stream-extraction limit of {options.ResourceLimits.MaximumCarrierBytes} bytes.");
        }
        return ExtractMemoryAtOffset(buffer.Bytes, options, buffer.BaseOffset);
    }

    internal IccOperationResult ProbeBytes(System.ReadOnlySpan<byte> carrierBytes,
        IccResourceLimits limits)
    {
        IccEmbeddingOptions options = new IccEmbeddingOptions();
        options.ResourceLimits = limits;
        IccEmbeddedProfileResult result = EnumerateBytes(carrierBytes, options);
        return result.Result;
    }

    internal IccEmbeddedProfileResult EnumerateMemoryAtOffset(
        System.ReadOnlyMemory<byte> carrierMemory, IccEmbeddingOptions options,
        long baseOffset)
    {
        IccEmbeddedFormat format = DetectCarrier(carrierMemory.Span);
        IccEmbeddedProfileResult result = CreateEnumerationResult(options,
            format);
        if (carrierMemory.Length > options.ResourceLimits.MaximumCarrierBytes)
        {
            result.Result = CreateResult(IccOperationStatus.ResourceLimitExceeded,
                $"The carrier contains {carrierMemory.Length} bytes, exceeding the configured maximum of {options.ResourceLimits.MaximumCarrierBytes} bytes.",
                baseOffset, carrierMemory.Length);
            return result;
        }
        if (options.Format != IccEmbeddedFormat.Unknown
            && options.Format != format)
        {
            result.Result = CreateResult(IccOperationStatus.Conflict,
                $"The requested {options.Format} carrier format does not match the detected {format} format.",
                baseOffset, carrierMemory.Length);
            return result;
        }

        if (format == IccEmbeddedFormat.Unsupported)
        {
            string carrierName = UnsupportedCarrierDetector.DetectName(
                carrierMemory.Span);
            result.Result = CreateResult(IccOperationStatus.UnsupportedFeature,
                CreateUnsupportedCarrierMessage(carrierName), baseOffset,
                carrierMemory.Length);
            return result;
        }

        if (format == IccEmbeddedFormat.Jpeg)
        {
            JpegProfileData jpeg = JpegEmbeddingCore.Analyze(
                carrierMemory.Span, options.ResourceLimits);
            if (!jpeg.IsValid && options.StrictCarrierValidation)
            {
                result.Result = CreateResult(jpeg.Status, jpeg.Message,
                    baseOffset + jpeg.ErrorOffset, jpeg.ErrorLength);
                return result;
            }
            if (jpeg.IsValid && jpeg.HasProfile)
            {
                IccEmbeddedProfile embedded = CreateJpegEmbeddedProfile(jpeg,
                    options.ResourceLimits, baseOffset);
                result.Profiles = new IccEmbeddedProfile[] { embedded };
                result.Result = CreateResult(IccOperationStatus.Success,
                    jpeg.Message, embedded.Location.Offset,
                    embedded.Location.Length);
                return result;
            }
            if (jpeg.IsValid)
            {
                result.Result = CreateResult(IccOperationStatus.NotFound,
                    "The detected JPEG carrier contains no complete ICC APP2 profile sequence.",
                    baseOffset, carrierMemory.Length);
                return result;
            }
        }
        if (format == IccEmbeddedFormat.Png)
        {
            PngProfileData png = PngEmbeddingCore.Analyze(carrierMemory,
                options.ResourceLimits);
            if (!png.IsValid && options.StrictCarrierValidation)
            {
                result.Result = CreateResult(png.Status, png.Message,
                    baseOffset + png.ErrorOffset, png.ErrorLength);
                return result;
            }
            if (png.IsValid && png.HasProfile)
            {
                IccEmbeddedProfile embedded = CreatePngEmbeddedProfile(png,
                    options.ResourceLimits, baseOffset);
                result.Profiles = new IccEmbeddedProfile[] { embedded };
                result.Result = CreateResult(png.Status, png.Message,
                    embedded.Location.Offset, embedded.Location.Length);
                return result;
            }
            if (png.IsValid)
            {
                result.Result = CreateResult(IccOperationStatus.NotFound,
                    "The detected PNG carrier contains no iCCP profile chunk.",
                    baseOffset, carrierMemory.Length);
                return result;
            }
        }
        if (format == IccEmbeddedFormat.Tiff
            || format == IccEmbeddedFormat.BigTiff
            || format == IccEmbeddedFormat.Dng)
        {
            TiffProfileData tiff = TiffEmbeddingCore.Analyze(carrierMemory,
                options.ResourceLimits);
            if (!tiff.IsValid && options.StrictCarrierValidation)
            {
                result.Result = CreateResult(tiff.Status, tiff.Message,
                    baseOffset + tiff.ErrorOffset, tiff.ErrorLength);
                return result;
            }
            if (tiff.IsValid && tiff.Profiles.Length != 0)
            {
                IccEmbeddedProfile[] tiffProfiles =
                    new IccEmbeddedProfile[tiff.Profiles.Length];
                long minimumOffset = long.MaxValue;
                long maximumEnd = 0;
                int profileIndex = 0;
                while (profileIndex < tiffProfiles.Length)
                {
                    IccEmbeddedProfile embedded = CreateTiffEmbeddedProfile(
                        carrierMemory, tiff.Profiles[profileIndex],
                        profileIndex, format, options.ResourceLimits,
                        baseOffset);
                    tiffProfiles[profileIndex] = embedded;
                    if (embedded.Location.Offset < minimumOffset)
                    {
                        minimumOffset = embedded.Location.Offset;
                    }
                    long profileEnd = embedded.Location.Offset
                        + embedded.Location.Length;
                    if (profileEnd > maximumEnd)
                    {
                        maximumEnd = profileEnd;
                    }
                    profileIndex = profileIndex + 1;
                }
                result.Profiles = tiffProfiles;
                result.Result = CreateResult(IccOperationStatus.Success,
                    tiff.Message, minimumOffset, maximumEnd - minimumOffset);
                return result;
            }
            if (tiff.IsValid)
            {
                result.Result = CreateResult(IccOperationStatus.NotFound,
                    $"The detected {format} carrier contains no TIFF tag 34675 ICC profile.",
                    baseOffset, carrierMemory.Length);
                return result;
            }
        }
        if (format == IccEmbeddedFormat.WebP)
        {
            WebPProfileData webP = WebPEmbeddingCore.Analyze(carrierMemory,
                options.ResourceLimits);
            if (!webP.IsValid && options.StrictCarrierValidation)
            {
                result.Result = CreateResult(webP.Status, webP.Message,
                    baseOffset + webP.ErrorOffset, webP.ErrorLength);
                return result;
            }
            if (webP.IsValid && webP.HasProfile)
            {
                IccEmbeddedProfile embedded = CreateWebPEmbeddedProfile(
                    carrierMemory, webP, options.ResourceLimits, baseOffset);
                result.Profiles = new IccEmbeddedProfile[] { embedded };
                result.Result = CreateResult(IccOperationStatus.Success,
                    webP.Message, embedded.Location.Offset,
                    embedded.Location.Length);
                return result;
            }
            if (webP.IsValid)
            {
                result.Result = CreateResult(IccOperationStatus.NotFound,
                    "The detected WebP carrier contains no ICCP profile chunk.",
                    baseOffset, carrierMemory.Length);
                return result;
            }
        }
        if (format == IccEmbeddedFormat.Jpeg2000)
        {
            Jpeg2000ProfileData jpeg2000 = Jpeg2000EmbeddingCore.Analyze(
                carrierMemory, options.ResourceLimits);
            if (!jpeg2000.IsValid && options.StrictCarrierValidation)
            {
                result.Result = CreateResult(jpeg2000.Status,
                    jpeg2000.Message, baseOffset + jpeg2000.ErrorOffset,
                    jpeg2000.ErrorLength);
                return result;
            }
            if (jpeg2000.IsValid && jpeg2000.Profiles.Length != 0)
            {
                IccEmbeddedProfile[] jpeg2000Profiles =
                    new IccEmbeddedProfile[jpeg2000.Profiles.Length];
                int profileIndex = 0;
                while (profileIndex < jpeg2000Profiles.Length)
                {
                    jpeg2000Profiles[profileIndex] =
                        CreateJpeg2000EmbeddedProfile(carrierMemory,
                            jpeg2000.Profiles[profileIndex], profileIndex,
                            options.ResourceLimits, baseOffset);
                    profileIndex = profileIndex + 1;
                }
                result.Profiles = jpeg2000Profiles;
                IccEmbeddedProfile first = jpeg2000Profiles[0];
                IccEmbeddedProfile last =
                    jpeg2000Profiles[jpeg2000Profiles.Length - 1];
                result.Result = CreateResult(IccOperationStatus.Success,
                    jpeg2000.Message, first.Location.Offset,
                    last.Location.Offset + last.Location.Length
                        - first.Location.Offset);
                return result;
            }
            if (jpeg2000.IsValid)
            {
                result.Result = CreateResult(IccOperationStatus.NotFound,
                    jpeg2000.Message, baseOffset, carrierMemory.Length);
                return result;
            }
        }
        if (format == IccEmbeddedFormat.Avif
            || format == IccEmbeddedFormat.Heif)
        {
            IsoBmffProfileData isoBmff = IsoBmffEmbeddingCore.Analyze(
                carrierMemory, options.ResourceLimits);
            if (!isoBmff.IsValid && options.StrictCarrierValidation)
            {
                result.Result = CreateResult(isoBmff.Status,
                    isoBmff.Message, baseOffset + isoBmff.ErrorOffset,
                    isoBmff.ErrorLength);
                return result;
            }
            if (isoBmff.IsValid && isoBmff.Profiles.Length != 0)
            {
                IccEmbeddedProfile[] isoProfiles =
                    new IccEmbeddedProfile[isoBmff.Profiles.Length];
                int profileIndex = 0;
                while (profileIndex < isoProfiles.Length)
                {
                    isoProfiles[profileIndex] =
                        CreateIsoBmffEmbeddedProfile(carrierMemory,
                            isoBmff.Profiles[profileIndex], profileIndex,
                            isoBmff.Format, options.ResourceLimits,
                            baseOffset);
                    profileIndex = profileIndex + 1;
                }
                result.Profiles = isoProfiles;
                IccEmbeddedProfile first = isoProfiles[0];
                IccEmbeddedProfile last =
                    isoProfiles[isoProfiles.Length - 1];
                result.Result = CreateResult(IccOperationStatus.Success,
                    isoBmff.Message, first.Location.Offset,
                    last.Location.Offset + last.Location.Length
                        - first.Location.Offset);
                return result;
            }
            if (isoBmff.IsValid)
            {
                result.Result = CreateResult(IccOperationStatus.NotFound,
                    isoBmff.Message, baseOffset, carrierMemory.Length);
                return result;
            }
        }

        IccByteLocation[] locations = RawProfileScanner.FindProfileLocations(
            carrierMemory.Span, options.ResourceLimits);
        if (locations.Length == 0)
        {
            result.Result = CreateResult(IccOperationStatus.NotFound,
                $"No bounded ICC profile was found in the detected {format} carrier.",
                baseOffset, carrierMemory.Length);
            return result;
        }

        IccReadOptions readOptions = CreateEnumerationReadOptions(
            options.ResourceLimits);
        IccEmbeddedProfile[] profiles = new IccEmbeddedProfile[locations.Length];
        int index = 0;
        while (index < locations.Length)
        {
            IccByteLocation relative = locations[index];
            int offset = (int)relative.Offset;
            int length = (int)relative.Length;
            IccEmbeddedProfile embedded = new IccEmbeddedProfile();
            embedded.Format = format == IccEmbeddedFormat.Unknown
                ? IccEmbeddedFormat.RawIcc : format;
            embedded.Index = index;
            embedded.Location.Offset = baseOffset + relative.Offset;
            embedded.Location.Length = relative.Length;
            embedded.Parts = new IccByteLocation[]
            {
                CloneLocation(embedded.Location)
            };
            embedded.Profile = ProfileCodec.ReadEnvelope(
                carrierMemory.Slice(offset, length), 0,
                embedded.Location.Offset, readOptions);
            profiles[index] = embedded;
            index = index + 1;
        }

        result.Profiles = profiles;
        long firstOffset = profiles[0].Location.Offset;
        IccByteLocation finalLocation = profiles[profiles.Length - 1].Location;
        long resultLength = finalLocation.Offset + finalLocation.Length
            - firstOffset;
        result.Result = CreateResult(IccOperationStatus.Success,
            $"Found {profiles.Length} bounded ICC profile(s) using {format} detection and raw scanning.",
            firstOffset, resultLength);
        return result;
    }

    internal IccProfile[] ExtractMemoryAtOffset(
        System.ReadOnlyMemory<byte> carrierMemory, IccReadOptions options,
        long baseOffset)
    {
        if (carrierMemory.Length > options.ResourceLimits.MaximumCarrierBytes)
        {
            throw new IccEmbeddingException(
                $"The carrier contains {carrierMemory.Length} bytes, exceeding the configured maximum of {options.ResourceLimits.MaximumCarrierBytes} bytes.");
        }
        IccEmbeddedFormat format = DetectCarrier(carrierMemory.Span);
        if (format == IccEmbeddedFormat.Unsupported)
        {
            string carrierName = UnsupportedCarrierDetector.DetectName(
                carrierMemory.Span);
            throw new IccEmbeddingException(
                CreateUnsupportedCarrierMessage(carrierName));
        }
        if (format == IccEmbeddedFormat.Jpeg)
        {
            JpegProfileData jpeg = JpegEmbeddingCore.Analyze(
                carrierMemory.Span, options.ResourceLimits);
            if (!jpeg.IsValid)
            {
                throw new IccEmbeddingException(jpeg.Message);
            }
            if (jpeg.HasProfile)
            {
                IccProfile profile = ProfileCodec.ReadEnvelope(
                    jpeg.ProfileBytes, 0, 0, options);
                profile.Location.Offset = -1;
                profile.Location.Length = jpeg.ProfileBytes.Length;
                profile.ReadResult.Location.Offset = -1;
                profile.ReadResult.Location.Length = jpeg.ProfileBytes.Length;
                return new IccProfile[] { profile };
            }
            throw new IccEmbeddingException(
                "The JPEG carrier does not contain a complete ICC APP2 profile sequence.");
        }
        if (format == IccEmbeddedFormat.Png)
        {
            PngProfileData png = PngEmbeddingCore.Analyze(carrierMemory,
                options.ResourceLimits);
            if (!png.IsValid)
            {
                throw new IccEmbeddingException(png.Message);
            }
            if (png.HasProfile)
            {
                IccProfile profile = ProfileCodec.ReadEnvelope(
                    png.ProfileBytes, 0, 0, options);
                profile.Location.Offset = -1;
                profile.Location.Length = png.ProfileBytes.Length;
                profile.ReadResult.Location.Offset = -1;
                profile.ReadResult.Location.Length = png.ProfileBytes.Length;
                return new IccProfile[] { profile };
            }
            throw new IccEmbeddingException(
                "The PNG carrier does not contain an iCCP profile chunk.");
        }
        if (format == IccEmbeddedFormat.Tiff
            || format == IccEmbeddedFormat.BigTiff
            || format == IccEmbeddedFormat.Dng)
        {
            TiffProfileData tiff = TiffEmbeddingCore.Analyze(carrierMemory,
                options.ResourceLimits);
            if (!tiff.IsValid)
            {
                throw new IccEmbeddingException(tiff.Message);
            }
            if (tiff.Profiles.Length != 0)
            {
                IccProfile[] tiffProfiles =
                    new IccProfile[tiff.Profiles.Length];
                int profileIndex = 0;
                while (profileIndex < tiffProfiles.Length)
                {
                    TiffProfileEntry entry = tiff.Profiles[profileIndex];
                    tiffProfiles[profileIndex] = ProfileCodec.ReadEnvelope(
                        carrierMemory.Slice(entry.DataOffset,
                            entry.DataLength), 0,
                        baseOffset + entry.DataOffset, options);
                    profileIndex = profileIndex + 1;
                }
                return tiffProfiles;
            }
            throw new IccEmbeddingException(
                "The TIFF carrier does not contain tag 34675 ICC profile data.");
        }
        if (format == IccEmbeddedFormat.WebP)
        {
            WebPProfileData webP = WebPEmbeddingCore.Analyze(carrierMemory,
                options.ResourceLimits);
            if (!webP.IsValid)
            {
                throw new IccEmbeddingException(webP.Message);
            }
            if (webP.HasProfile)
            {
                WebPChunkRegion profile =
                    WebPEmbeddingCore.RequireProfileChunk(webP);
                return new IccProfile[]
                {
                    ProfileCodec.ReadEnvelope(carrierMemory.Slice(
                        profile.DataOffset, profile.DataLength), 0,
                        baseOffset + profile.DataOffset, options)
                };
            }
            throw new IccEmbeddingException(
                "The WebP carrier does not contain an ICCP profile chunk.");
        }
        if (format == IccEmbeddedFormat.Jpeg2000)
        {
            Jpeg2000ProfileData jpeg2000 = Jpeg2000EmbeddingCore.Analyze(
                carrierMemory, options.ResourceLimits);
            if (!jpeg2000.IsValid)
            {
                throw new IccEmbeddingException(jpeg2000.Message);
            }
            if (jpeg2000.Profiles.Length == 0)
            {
                throw new IccEmbeddingException(
                    "The JPEG 2000 carrier does not contain a method 2 or 3 ICC profile.");
            }
            IccProfile[] jpeg2000Profiles =
                new IccProfile[jpeg2000.Profiles.Length];
            int profileIndex = 0;
            while (profileIndex < jpeg2000Profiles.Length)
            {
                Jpeg2000ProfileEntry entry =
                    jpeg2000.Profiles[profileIndex];
                jpeg2000Profiles[profileIndex] = ProfileCodec.ReadEnvelope(
                    carrierMemory.Slice(entry.ProfileOffset,
                        entry.ProfileLength), 0,
                    baseOffset + entry.ProfileOffset, options);
                profileIndex = profileIndex + 1;
            }
            return jpeg2000Profiles;
        }
        if (format == IccEmbeddedFormat.Avif
            || format == IccEmbeddedFormat.Heif)
        {
            IsoBmffProfileData isoBmff = IsoBmffEmbeddingCore.Analyze(
                carrierMemory, options.ResourceLimits);
            if (!isoBmff.IsValid)
            {
                throw new IccEmbeddingException(isoBmff.Message);
            }
            if (isoBmff.Profiles.Length == 0)
            {
                throw new IccEmbeddingException(
                    $"The {format} carrier does not contain a prof or rICC profile.");
            }
            IccProfile[] isoProfiles =
                new IccProfile[isoBmff.Profiles.Length];
            int profileIndex = 0;
            while (profileIndex < isoProfiles.Length)
            {
                IsoBmffProfileEntry entry = isoBmff.Profiles[profileIndex];
                isoProfiles[profileIndex] = ProfileCodec.ReadEnvelope(
                    carrierMemory.Slice(entry.ProfileOffset,
                        entry.ProfileLength), 0,
                    baseOffset + entry.ProfileOffset, options);
                profileIndex = profileIndex + 1;
            }
            return isoProfiles;
        }

        IccByteLocation[] locations = RawProfileScanner.FindProfileLocations(
            carrierMemory.Span, options.ResourceLimits);
        if (locations.Length == 0)
        {
            throw new IccEmbeddingException(
                "The carrier does not contain a bounded raw ICC profile.");
        }

        IccProfile[] profiles = new IccProfile[locations.Length];
        int index = 0;
        while (index < locations.Length)
        {
            int offset = (int)locations[index].Offset;
            int length = (int)locations[index].Length;
            profiles[index] = ProfileCodec.ReadEnvelope(
                carrierMemory.Slice(offset, length), 0,
                baseOffset + locations[index].Offset, options);
            index = index + 1;
        }
        return profiles;
    }

    internal IccEmbeddedProfile CreateJpegEmbeddedProfile(JpegProfileData data,
        IccResourceLimits limits, long baseOffset)
    {
        IccEmbeddedProfile embedded = new IccEmbeddedProfile();
        embedded.Format = IccEmbeddedFormat.Jpeg;
        embedded.Index = 0;
        embedded.Parts = new IccByteLocation[data.Segments.Length];
        int index = 0;
        while (index < data.Segments.Length)
        {
            JpegIccSegment segment = data.Segments[index];
            IccByteLocation part = new IccByteLocation();
            part.Offset = baseOffset + segment.Start;
            part.Length = segment.End - segment.Start;
            embedded.Parts[index] = part;
            index = index + 1;
        }
        IccByteLocation first = embedded.Parts[0];
        IccByteLocation last = embedded.Parts[embedded.Parts.Length - 1];
        embedded.Location.Offset = first.Offset;
        embedded.Location.Length = last.Offset + last.Length - first.Offset;
        IccReadOptions readOptions = CreateEnumerationReadOptions(limits);
        embedded.Profile = ProfileCodec.ReadEnvelope(data.ProfileBytes, 0, 0,
            readOptions);
        embedded.Profile.Location.Offset = -1;
        embedded.Profile.Location.Length = data.ProfileBytes.Length;
        embedded.Profile.ReadResult.Location.Offset = -1;
        embedded.Profile.ReadResult.Location.Length = data.ProfileBytes.Length;
        return embedded;
    }

    internal IccEmbeddedProfile CreatePngEmbeddedProfile(PngProfileData data,
        IccResourceLimits limits, long baseOffset)
    {
        PngChunkRegion chunk = PngEmbeddingCore.RequireProfileChunk(data);
        IccEmbeddedProfile embedded = new IccEmbeddedProfile();
        embedded.Format = IccEmbeddedFormat.Png;
        embedded.Index = 0;
        embedded.Location.Offset = baseOffset + chunk.Start;
        embedded.Location.Length = chunk.End - chunk.Start;
        embedded.Parts = new IccByteLocation[]
        {
            CloneLocation(embedded.Location)
        };
        IccReadOptions readOptions = CreateEnumerationReadOptions(limits);
        embedded.Profile = ProfileCodec.ReadEnvelope(data.ProfileBytes, 0, 0,
            readOptions);
        embedded.Profile.Location.Offset = -1;
        embedded.Profile.Location.Length = data.ProfileBytes.Length;
        embedded.Profile.ReadResult.Location.Offset = -1;
        embedded.Profile.ReadResult.Location.Length = data.ProfileBytes.Length;
        return embedded;
    }

    internal IccEmbeddedProfile CreateTiffEmbeddedProfile(
        System.ReadOnlyMemory<byte> carrierMemory, TiffProfileEntry entry,
        int profileIndex, IccEmbeddedFormat format, IccResourceLimits limits,
        long baseOffset)
    {
        IccEmbeddedProfile embedded = new IccEmbeddedProfile();
        embedded.Format = format;
        embedded.Index = profileIndex;
        embedded.Location.Offset = baseOffset + entry.DataOffset;
        embedded.Location.Length = entry.DataLength;
        embedded.Parts = new IccByteLocation[]
        {
            CloneLocation(embedded.Location)
        };
        IccReadOptions readOptions = CreateEnumerationReadOptions(limits);
        embedded.Profile = ProfileCodec.ReadEnvelope(
            carrierMemory.Slice(entry.DataOffset, entry.DataLength), 0,
            embedded.Location.Offset, readOptions);
        return embedded;
    }

    internal IccEmbeddedProfile CreateWebPEmbeddedProfile(
        System.ReadOnlyMemory<byte> carrierMemory, WebPProfileData data,
        IccResourceLimits limits, long baseOffset)
    {
        WebPChunkRegion profile = WebPEmbeddingCore.RequireProfileChunk(data);
        IccEmbeddedProfile embedded = new IccEmbeddedProfile();
        embedded.Format = IccEmbeddedFormat.WebP;
        embedded.Index = 0;
        embedded.Location.Offset = baseOffset + profile.DataOffset;
        embedded.Location.Length = profile.DataLength;
        embedded.Parts = new IccByteLocation[]
        {
            CloneLocation(embedded.Location)
        };
        IccReadOptions readOptions = CreateEnumerationReadOptions(limits);
        embedded.Profile = ProfileCodec.ReadEnvelope(carrierMemory.Slice(
            profile.DataOffset, profile.DataLength), 0,
            embedded.Location.Offset, readOptions);
        return embedded;
    }

    internal IccEmbeddedProfile CreateJpeg2000EmbeddedProfile(
        System.ReadOnlyMemory<byte> carrierMemory,
        Jpeg2000ProfileEntry entry, int profileIndex,
        IccResourceLimits limits, long baseOffset)
    {
        IccEmbeddedProfile embedded = new IccEmbeddedProfile();
        embedded.Format = IccEmbeddedFormat.Jpeg2000;
        embedded.Index = profileIndex;
        embedded.Location.Offset = baseOffset + entry.ProfileOffset;
        embedded.Location.Length = entry.ProfileLength;
        embedded.Parts = new IccByteLocation[]
        {
            CloneLocation(embedded.Location)
        };
        IccReadOptions readOptions = CreateEnumerationReadOptions(limits);
        embedded.Profile = ProfileCodec.ReadEnvelope(carrierMemory.Slice(
            entry.ProfileOffset, entry.ProfileLength), 0,
            embedded.Location.Offset, readOptions);
        return embedded;
    }

    internal IccEmbeddedProfile CreateIsoBmffEmbeddedProfile(
        System.ReadOnlyMemory<byte> carrierMemory,
        IsoBmffProfileEntry entry, int profileIndex,
        IccEmbeddedFormat format, IccResourceLimits limits,
        long baseOffset)
    {
        IccEmbeddedProfile embedded = new IccEmbeddedProfile();
        embedded.Format = format;
        embedded.Index = profileIndex;
        embedded.Location.Offset = baseOffset + entry.ProfileOffset;
        embedded.Location.Length = entry.ProfileLength;
        embedded.Parts = new IccByteLocation[]
        {
            CloneLocation(embedded.Location)
        };
        IccReadOptions readOptions = CreateEnumerationReadOptions(limits);
        embedded.Profile = ProfileCodec.ReadEnvelope(carrierMemory.Slice(
            entry.ProfileOffset, entry.ProfileLength), 0,
            embedded.Location.Offset, readOptions);
        return embedded;
    }

    internal IccEmbeddedFormat DetectCarrier(
        System.ReadOnlySpan<byte> carrierBytes)
    {
        if (IsPng(carrierBytes))
        {
            return IccEmbeddedFormat.Png;
        }
        if (IsJpeg(carrierBytes))
        {
            return IccEmbeddedFormat.Jpeg;
        }
        if (IsWebP(carrierBytes))
        {
            return IccEmbeddedFormat.WebP;
        }
        if (IsJpeg2000(carrierBytes))
        {
            return IccEmbeddedFormat.Jpeg2000;
        }
        IccEmbeddedFormat tiffFormat =
            TiffEmbeddingCore.DetectFormat(carrierBytes);
        if (tiffFormat != IccEmbeddedFormat.Unknown)
        {
            return tiffFormat;
        }
        IccEmbeddedFormat isoFormat = DetectIsoBmff(carrierBytes);
        if (isoFormat != IccEmbeddedFormat.Unknown)
        {
            return isoFormat;
        }
        if (IsRawIcc(carrierBytes))
        {
            return IccEmbeddedFormat.RawIcc;
        }
        if (UnsupportedCarrierDetector.DetectName(carrierBytes) != "")
        {
            return IccEmbeddedFormat.Unsupported;
        }
        return IccEmbeddedFormat.Unknown;
    }

    internal string CreateUnsupportedCarrierMessage(string carrierName)
    {
        return $"{carrierName} carrier parsing and ICC embedding are integration-only and are not implemented. Extract the raw ICC profile bytes with a format-specific parser and pass them to IccProfileReader.ReadProfileBytes.";
    }

    internal bool IsPng(System.ReadOnlySpan<byte> bytes)
    {
        return bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50
            && bytes[2] == 0x4e && bytes[3] == 0x47 && bytes[4] == 0x0d
            && bytes[5] == 0x0a && bytes[6] == 0x1a && bytes[7] == 0x0a;
    }

    internal bool IsJpeg(System.ReadOnlySpan<byte> bytes)
    {
        return bytes.Length >= 2 && bytes[0] == 0xff && bytes[1] == 0xd8;
    }

    internal bool IsWebP(System.ReadOnlySpan<byte> bytes)
    {
        return bytes.Length >= 12 && ReadUInt32(bytes, 0) == 0x52494646
            && ReadUInt32(bytes, 8) == 0x57454250;
    }

    internal bool IsJpeg2000(System.ReadOnlySpan<byte> bytes)
    {
        return bytes.Length >= 12 && ReadUInt32(bytes, 0) == 12
            && ReadUInt32(bytes, 4) == 0x6a502020
            && ReadUInt32(bytes, 8) == 0x0d0a870a;
    }

    internal bool IsTiff(System.ReadOnlySpan<byte> bytes)
    {
        return bytes.Length >= 4
            && ((bytes[0] == 0x49 && bytes[1] == 0x49
                    && bytes[2] == 0x2a && bytes[3] == 0)
                || (bytes[0] == 0x4d && bytes[1] == 0x4d
                    && bytes[2] == 0 && bytes[3] == 0x2a));
    }

    internal bool IsBigTiff(System.ReadOnlySpan<byte> bytes)
    {
        return bytes.Length >= 4
            && ((bytes[0] == 0x49 && bytes[1] == 0x49
                    && bytes[2] == 0x2b && bytes[3] == 0)
                || (bytes[0] == 0x4d && bytes[1] == 0x4d
                    && bytes[2] == 0 && bytes[3] == 0x2b));
    }

    internal bool IsRawIcc(System.ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 40)
        {
            return false;
        }
        uint encodedSize = ReadUInt32(bytes, 0);
        return encodedSize >= IccConstants.MinimumProfileSize
            && ReadUInt32(bytes, 36) == IccConstants.ProfileFileSignature;
    }

    internal IccEmbeddedFormat DetectIsoBmff(System.ReadOnlySpan<byte> bytes)
    {
        return IsoBmffEmbeddingCore.DetectFormat(bytes);
    }

    internal void ValidateEmbeddingOptions(IccEmbeddingOptions options)
    {
        if (options == null || options.ResourceLimits == null)
        {
            throw new IccProfileValidationException(
                "Embedding options and resource limits cannot be null.");
        }
        if (options.Format < IccEmbeddedFormat.Unknown
            || options.Format > IccEmbeddedFormat.Unsupported)
        {
            throw new IccProfileValidationException(
                "The requested embedded carrier format is invalid.");
        }
        RawProfileScanner.ValidateScanLimits(options.ResourceLimits);
    }

    internal void ValidateReadableStream(System.IO.Stream carrierStream)
    {
        if (carrierStream == null)
        {
            throw new IccProfileValidationException(
                "A carrier stream cannot be null.");
        }
        if (!carrierStream.CanRead)
        {
            throw new IccEmbeddingException("The carrier stream is not readable.");
        }
    }

    internal EmbeddedCarrierBuffer ReadCarrierStream(
        System.IO.Stream carrierStream, int maximumBytes)
    {
        ValidateReadableStream(carrierStream);
        EmbeddedCarrierBuffer result = new EmbeddedCarrierBuffer();
        result.BaseOffset = carrierStream.CanSeek ? carrierStream.Position : 0;
        if (carrierStream.CanSeek
            && carrierStream.Length - carrierStream.Position > maximumBytes)
        {
            result.LimitExceeded = true;
            return result;
        }

        using (System.IO.MemoryStream memory = new System.IO.MemoryStream())
        {
            byte[] block = new byte[4096];
            bool reading = true;
            while (reading)
            {
                long allowance = (long)maximumBytes - memory.Length;
                int requested = block.Length;
                if (allowance < requested)
                {
                    requested = (int)allowance + 1;
                }
                int read = carrierStream.Read(block, 0, requested);
                if (read == 0)
                {
                    reading = false;
                }
                else if (memory.Length + read > maximumBytes)
                {
                    result.LimitExceeded = true;
                    reading = false;
                }
                else
                {
                    memory.Write(block, 0, read);
                }
            }
            if (!result.LimitExceeded)
            {
                result.Bytes = memory.ToArray();
            }
        }
        return result;
    }

    internal IccReadOptions CreateEnumerationReadOptions(
        IccResourceLimits limits)
    {
        IccReadOptions options = new IccReadOptions();
        options.Mode = IccReadMode.Recovery;
        options.ValidationMode = IccValidationMode.SecurityStrict;
        options.ResourceLimits = CloneResourceLimits(limits);
        options.CopyInputBytes = true;
        options.DecodeTagsLazily = true;
        options.RequireExactProfileSize = true;
        options.PreserveOriginalBytes = true;
        return options;
    }

    internal IccEmbeddedProfileResult CreateEnumerationResult(
        IccEmbeddingOptions options, IccEmbeddedFormat detectedFormat)
    {
        IccEmbeddedProfileResult result = new IccEmbeddedProfileResult();
        result.Operation = IccEmbeddedOperation.Enumerate;
        result.Options = CloneEmbeddingOptions(options);
        result.Options.Format = detectedFormat;
        return result;
    }

    internal IccEmbeddingOptions CloneEmbeddingOptions(
        IccEmbeddingOptions source)
    {
        IccEmbeddingOptions clone = new IccEmbeddingOptions();
        clone.Format = source.Format;
        clone.ProfileIndex = source.ProfileIndex;
        clone.AllowCompression = source.AllowCompression;
        clone.StrictCarrierValidation = source.StrictCarrierValidation;
        clone.ResourceLimits = CloneResourceLimits(source.ResourceLimits);
        return clone;
    }

    internal IccResourceLimits CloneResourceLimits(IccResourceLimits source)
    {
        IccResourceLimits clone = new IccResourceLimits();
        clone.MaximumProfileBytes = source.MaximumProfileBytes;
        clone.MaximumCarrierBytes = source.MaximumCarrierBytes;
        clone.MaximumTagCount = source.MaximumTagCount;
        clone.MaximumTagBytes = source.MaximumTagBytes;
        clone.MaximumTotalTagBytes = source.MaximumTotalTagBytes;
        clone.MaximumNestingDepth = source.MaximumNestingDepth;
        clone.MaximumDecompressedBytes = source.MaximumDecompressedBytes;
        clone.MaximumCalculatorInstructions = source.MaximumCalculatorInstructions;
        clone.MaximumCalculatorStackDepth = source.MaximumCalculatorStackDepth;
        clone.MaximumCalculatorStorageValues = source.MaximumCalculatorStorageValues;
        clone.MaximumDiagnosticCount = source.MaximumDiagnosticCount;
        return clone;
    }

    internal IccByteLocation CloneLocation(IccByteLocation source)
    {
        IccByteLocation clone = new IccByteLocation();
        clone.Offset = source.Offset;
        clone.Length = source.Length;
        return clone;
    }

    internal IccOperationResult CreateResult(IccOperationStatus status,
        string message, long offset, long length)
    {
        IccOperationResult result = new IccOperationResult();
        result.Status = status;
        result.Message = message;
        result.Location.Offset = offset;
        result.Location.Length = length;
        return result;
    }

    internal uint ReadUInt32(System.ReadOnlySpan<byte> bytes, int offset)
    {
        return ((uint)bytes[offset] << 24)
            | ((uint)bytes[offset + 1] << 16)
            | ((uint)bytes[offset + 2] << 8)
            | bytes[offset + 3];
    }
}
