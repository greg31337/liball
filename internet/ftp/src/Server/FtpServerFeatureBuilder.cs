using System;
using System.Collections.Generic;

namespace ftp;

internal static class FtpServerFeatureBuilder
{
    internal static FtpFeatureSet BuildFeatureSet(
        FtpServerSession session,
        FtpFeatureSet configuredFeatures)
    {
        List<FtpFeature> features = new List<FtpFeature>();
        AddTlsFeatures(features, session);
        AddSimpleFeature(features, FtpFeatureNames.Utf8);
        AddConfiguredLanguage(features, configuredFeatures);
        if (session.ActiveFileSystem != null)
        {
            AddSimpleFeature(features, FtpFeatureNames.Mdtm);
            AddSimpleFeature(features, FtpFeatureNames.Size);
            AddSimpleFeature(features, FtpFeatureNames.RestStream);
            AddSimpleFeature(features, FtpFeatureNames.Tvfs);
            AddMlstFeature(features, configuredFeatures);
        }
        if (session.HostResolver != null)
        {
            AddSimpleFeature(features, FtpFeatureNames.Host);
        }
        if (session.Settings.EnablePret)
        {
            AddSimpleFeature(features, FtpFeatureNames.Pret);
        }
        if (session.Settings.EnableClnt)
        {
            AddSimpleFeature(features, FtpFeatureNames.Clnt);
        }
        if (session.Settings.EnableMfmt && session.ActiveFileSystem != null)
        {
            AddSimpleFeature(features, FtpFeatureNames.Mfmt);
        }
        if (session.Settings.EnableModeZ && session.ActiveFileSystem != null)
        {
            AddSimpleFeature(features, FtpFeatureNames.ModeZ);
        }
        if (session.ActiveFileSystem != null)
        {
            AddChecksumFeatures(features, session);
        }
        if (session.Settings.EnableCompatibilityExtensions &&
            session.ActiveFileSystem != null)
        {
            AddSimpleFeature(features, FtpFeatureNames.SiteChmod);
            AddSimpleFeature(features, FtpFeatureNames.SiteUmask);
            AddSimpleFeature(features, FtpFeatureNames.Rmda);
        }
        AddConfiguredExtensions(features, configuredFeatures, session);
        if (features.Count > FtpDefaults.MaximumFeatureCount)
        {
            throw new FtpConfigurationException(
                "The derived FTP server feature set exceeded its limit.",
                null);
        }

        FtpFeatureSet result = new FtpFeatureSet();
        result.Features = features.ToArray();
        result.Negotiated = true;
        return result;
    }

    internal static void AddTlsFeatures(
        List<FtpFeature> features,
        FtpServerSession session)
    {
        FtpSecurityMode mode =
            session.Context.ListenerSettings.SecurityMode;
        bool explicitTls =
            mode == FtpSecurityMode.ExplicitTlsRequired ||
            mode == FtpSecurityMode.ExplicitTlsOpportunistic;
        bool implicitTls = mode == FtpSecurityMode.ImplicitTls;
        FtpTlsOptions tls =
            FtpServerLoginCommandHandler.ResolveTlsOptions(session);
        bool certificateAvailable =
            session.ActiveCertificateProvider != null ||
            tls.ServerCertificate != null;
        if (!certificateAvailable || (!explicitTls && !implicitTls))
        {
            return;
        }
        if (explicitTls)
        {
            features.Add(FtpFeatureCodec.CreateAuthenticationFeature(
                new string[] { "TLS" }));
        }
        AddSimpleFeature(features, FtpFeatureNames.Pbsz);
        AddSimpleFeature(features, FtpFeatureNames.Prot);
    }

    internal static void AddConfiguredLanguage(
        List<FtpFeature> features,
        FtpFeatureSet configuredFeatures)
    {
        FtpFeature? language = FindConfiguredFeature(
            configuredFeatures, FtpFeatureNames.Lang);
        if (language != null &&
            language.Support != FtpFeatureSupport.NotSupported)
        {
            features.Add(CopyFeature(language));
        }
    }

    internal static void AddMlstFeature(
        List<FtpFeature> features,
        FtpFeatureSet configuredFeatures)
    {
        FtpFeature? configured = FindConfiguredFeature(
            configuredFeatures, FtpFeatureNames.Mlst);
        if (configured != null &&
            configured.Support != FtpFeatureSupport.NotSupported)
        {
            FtpFeatureSet selected = new FtpFeatureSet();
            selected.Features = new FtpFeature[] { CopyFeature(configured) };
            FtpMlstFeatureInfo info =
                FtpFeatureCodec.ExtractMlstFacts(selected);
            features.Add(FtpFeatureCodec.CreateMlstFeature(
                info.Facts, info.DefaultFacts));
            return;
        }
        features.Add(FtpFeatureCodec.CreateMlstFeature(
            CreateMlstFacts(), CreateDefaultMlstFacts()));
    }

    internal static string[] CreateMlstFacts()
    {
        return new string[]
        {
            FtpFactNames.Type,
            FtpFactNames.Size,
            FtpFactNames.Modify,
            FtpFactNames.Create,
            FtpFactNames.Unique,
            FtpFactNames.Perm,
            FtpFactNames.Lang,
            FtpFactNames.MediaType,
            FtpFactNames.Charset
        };
    }

    internal static string[] CreateDefaultMlstFacts()
    {
        return new string[]
        {
            FtpFactNames.Type,
            FtpFactNames.Size,
            FtpFactNames.Modify,
            FtpFactNames.Unique
        };
    }

    internal static void AddConfiguredExtensions(
        List<FtpFeature> features,
        FtpFeatureSet configuredFeatures,
        FtpServerSession session)
    {
        int index = 0;
        while (index < configuredFeatures.Features.Length)
        {
            FtpFeature feature = configuredFeatures.Features[index];
            if (feature.Support != FtpFeatureSupport.NotSupported &&
                !IsCoreFeature(feature.Name) &&
                !string.Equals(
                    feature.Name,
                    FtpFeatureNames.Lang,
                    StringComparison.OrdinalIgnoreCase) &&
                (!IsCompatibilityFeature(feature.Name) ||
                    session.ActiveExtensionHandlers.Length > 0) &&
                !IsEnabledBuiltInCompatibilityFeature(
                    session, feature.Name))
            {
                features.Add(CopyFeature(feature));
            }
            index++;
        }
    }

    internal static bool IsEnabledBuiltInCompatibilityFeature(
        FtpServerSession session,
        string featureName)
    {
        return (session.Settings.EnablePret && string.Equals(
                featureName,
                FtpFeatureNames.Pret,
                StringComparison.OrdinalIgnoreCase)) ||
            (session.Settings.EnableClnt && string.Equals(
                featureName,
                FtpFeatureNames.Clnt,
                StringComparison.OrdinalIgnoreCase)) ||
            (session.Settings.EnableMfmt &&
                session.ActiveFileSystem != null && string.Equals(
                    featureName,
                    FtpFeatureNames.Mfmt,
                    StringComparison.OrdinalIgnoreCase)) ||
            (session.Settings.EnableModeZ &&
                session.ActiveFileSystem != null && string.Equals(
                    featureName,
                    FtpFeatureNames.ModeZ,
                    StringComparison.OrdinalIgnoreCase)) ||
            (session.Settings.EnableHash &&
                session.ActiveFileSystem != null && string.Equals(
                featureName,
                FtpFeatureNames.Hash,
                StringComparison.OrdinalIgnoreCase)) ||
            (session.Settings.EnableMd5 &&
                session.ActiveFileSystem != null && string.Equals(
                featureName,
                FtpFeatureNames.Md5,
                StringComparison.OrdinalIgnoreCase)) ||
            (session.Settings.EnableXmd5 &&
                session.ActiveFileSystem != null && string.Equals(
                featureName,
                FtpFeatureNames.Xmd5,
                StringComparison.OrdinalIgnoreCase)) ||
            (session.Settings.EnableXsha1 &&
                session.ActiveFileSystem != null && string.Equals(
                featureName,
                FtpFeatureNames.Xsha1,
                StringComparison.OrdinalIgnoreCase)) ||
            (session.Settings.EnableXsha256 &&
                session.ActiveFileSystem != null && string.Equals(
                featureName,
                FtpFeatureNames.Xsha256,
                StringComparison.OrdinalIgnoreCase)) ||
            (session.Settings.EnableXsha512 &&
                session.ActiveFileSystem != null && string.Equals(
                featureName,
                FtpFeatureNames.Xsha512,
                StringComparison.OrdinalIgnoreCase)) ||
            (session.Settings.EnableXcrc &&
                session.ActiveFileSystem != null && string.Equals(
                featureName,
                FtpFeatureNames.Xcrc,
                StringComparison.OrdinalIgnoreCase)) ||
            (session.Settings.EnableCompatibilityExtensions &&
                session.ActiveFileSystem != null &&
                (string.Equals(
                    featureName,
                    FtpFeatureNames.SiteChmod,
                    StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(
                    featureName,
                    FtpFeatureNames.SiteUmask,
                    StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(
                    featureName,
                    FtpFeatureNames.Rmda,
                    StringComparison.OrdinalIgnoreCase)));
    }

    internal static void AddChecksumFeatures(
        List<FtpFeature> features,
        FtpServerSession session)
    {
        if (session.Settings.EnableHash)
        {
            FtpFeature feature = new FtpFeature();
            feature.Name = FtpFeatureNames.Hash;
            feature.Parameters = FtpChecksumExtensions.FormatHashFeature(
                FtpChecksumExtensions.DefaultHashAlgorithm);
            feature.RawLine = " " + feature.Name + " " +
                feature.Parameters;
            feature.Support = FtpFeatureSupport.Supported;
            features.Add(feature);
        }
        if (session.Settings.EnableMd5)
        {
            AddSimpleFeature(features, FtpFeatureNames.Md5);
        }
        if (session.Settings.EnableXmd5)
        {
            AddSimpleFeature(features, FtpFeatureNames.Xmd5);
        }
        if (session.Settings.EnableXsha1)
        {
            AddSimpleFeature(features, FtpFeatureNames.Xsha1);
        }
        if (session.Settings.EnableXsha256)
        {
            AddSimpleFeature(features, FtpFeatureNames.Xsha256);
        }
        if (session.Settings.EnableXsha512)
        {
            AddSimpleFeature(features, FtpFeatureNames.Xsha512);
        }
        if (session.Settings.EnableXcrc)
        {
            AddSimpleFeature(features, FtpFeatureNames.Xcrc);
        }
    }

    internal static bool IsCoreFeature(string featureName)
    {
        return string.Equals(
                featureName, FtpFeatureNames.Auth,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                featureName, FtpFeatureNames.Pbsz,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                featureName, FtpFeatureNames.Prot,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                featureName, FtpFeatureNames.Utf8,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                featureName, FtpFeatureNames.Mdtm,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                featureName, FtpFeatureNames.Size,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                featureName, FtpFeatureNames.RestStream,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                featureName, FtpFeatureNames.Tvfs,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                featureName, FtpFeatureNames.Mlst,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                featureName, FtpFeatureNames.Host,
                StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsCompatibilityFeature(string featureName)
    {
        return string.Equals(
                featureName, FtpFeatureNames.Pret,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                featureName, FtpFeatureNames.ModeZ,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                featureName, FtpFeatureNames.Clnt,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                featureName, FtpFeatureNames.Mfmt,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                featureName, FtpFeatureNames.Hash,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                featureName, FtpFeatureNames.Md5,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                featureName, FtpFeatureNames.Xmd5,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                featureName, FtpFeatureNames.Xsha1,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                featureName, FtpFeatureNames.Xsha256,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                featureName, FtpFeatureNames.Xsha512,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                featureName, FtpFeatureNames.Xcrc,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                featureName, FtpFeatureNames.SiteChmod,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                featureName, FtpFeatureNames.SiteUmask,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                featureName, FtpFeatureNames.Rmda,
                StringComparison.OrdinalIgnoreCase);
    }

    internal static FtpFeature? FindConfiguredFeature(
        FtpFeatureSet configuredFeatures,
        string featureName)
    {
        int index = 0;
        while (index < configuredFeatures.Features.Length)
        {
            FtpFeature feature = configuredFeatures.Features[index];
            if (string.Equals(
                feature.Name,
                featureName,
                StringComparison.OrdinalIgnoreCase))
            {
                return feature;
            }
            index++;
        }
        return null;
    }

    internal static void AddSimpleFeature(
        List<FtpFeature> features,
        string name)
    {
        FtpFeature feature = new FtpFeature();
        feature.Name = name;
        feature.RawLine = " " + name;
        feature.Support = FtpFeatureSupport.Supported;
        features.Add(feature);
    }

    internal static FtpFeature CopyFeature(FtpFeature source)
    {
        FtpFeature feature = new FtpFeature();
        feature.Name = source.Name;
        feature.Parameters = source.Parameters;
        feature.RawLine = source.RawLine;
        feature.Support = FtpFeatureSupport.Supported;
        return feature;
    }
}
