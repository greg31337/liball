using System.Net.Http.Headers;
using uspto;

namespace uspto.Tests;

public class FoundationValuesTests
{
    [Test]
    public void DefaultEndpointsAreAbsoluteHttpsUris()
    {
        string[] endpoints =
        {
            UsptoEndpoints.OdpBaseUri,
            UsptoEndpoints.OdpApiV1BaseUri,
            UsptoEndpoints.TsdrBaseUri,
            UsptoEndpoints.PatentsViewBaseUri,
            UsptoEndpoints.BulkDataBaseUri
        };

        Assert.Multiple(() =>
        {
            foreach (string endpoint in endpoints)
            {
                Assert.That(Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? uri), Is.True, endpoint);
                Assert.That(uri?.Scheme, Is.EqualTo(Uri.UriSchemeHttps), endpoint);
                Assert.That(uri?.UserInfo, Is.Empty, endpoint);
                Assert.That(uri?.Fragment, Is.Empty, endpoint);
            }
        });
    }

    [Test]
    public void ServiceCredentialNamesMatchPublishedContracts()
    {
        Assert.Multiple(() =>
        {
            Assert.That(UsptoHeaders.OdpApiKey, Is.EqualTo("X-API-Key"));
            Assert.That(UsptoHeaders.TsdrApiKey, Is.EqualTo("USPTO-API-KEY"));
            Assert.That(UsptoHeaders.PatentsViewApiKey, Is.EqualTo("X-Api-Key"));
            Assert.That(UsptoEnvironmentVariables.OdpApiKey, Is.EqualTo("USPTO_API_KEY"));
            Assert.That(UsptoEnvironmentVariables.TsdrApiKey, Is.EqualTo("USPTO_TSDR_API_KEY"));
            Assert.That(UsptoEnvironmentVariables.PatentsViewApiKey, Is.EqualTo("PATENTSVIEW_API_KEY"));
        });
    }

    [Test]
    public void DatasetIdentifiersMatchCatalogIdentifiers()
    {
        Assert.Multiple(() =>
        {
            Assert.That(UsptoDatasetIdentifiers.PatentApplicationFullTextXml, Is.EqualTo("APPXML"));
            Assert.That(UsptoDatasetIdentifiers.PatentGrantFullTextXml, Is.EqualTo("PTGRXML"));
            Assert.That(UsptoDatasetIdentifiers.PatentFileWrapperWeekly, Is.EqualTo("PTFWPRE"));
            Assert.That(UsptoDatasetIdentifiers.PetitionDecisions, Is.EqualTo("PETDEC"));
            Assert.That(UsptoDatasetIdentifiers.OfficeActionWeekly, Is.EqualTo("OACT"));
            Assert.That(UsptoDatasetIdentifiers.TrademarkTrialAndAppealBoard, Is.EqualTo("TTAB"));
        });
    }

    [Test]
    public void MediaTypesAreValidAndContainNoParameters()
    {
        string[] mediaTypes =
        {
            UsptoMediaTypes.Json,
            UsptoMediaTypes.Xml,
            UsptoMediaTypes.TextXml,
            UsptoMediaTypes.Html,
            UsptoMediaTypes.Pdf,
            UsptoMediaTypes.Zip,
            UsptoMediaTypes.Gzip,
            UsptoMediaTypes.Tar,
            UsptoMediaTypes.Csv,
            UsptoMediaTypes.TabSeparatedValues,
            UsptoMediaTypes.Text,
            UsptoMediaTypes.FormUrlEncoded,
            UsptoMediaTypes.Binary
        };

        Assert.Multiple(() =>
        {
            foreach (string mediaType in mediaTypes)
            {
                Assert.That(MediaTypeHeaderValue.TryParse(mediaType, out MediaTypeHeaderValue? parsed), Is.True, mediaType);
                Assert.That(parsed?.Parameters, Is.Empty, mediaType);
            }
        });
    }

    [Test]
    public void DefaultsRespectPublishedAndInternalLimits()
    {
        Assert.Multiple(() =>
        {
            Assert.That(UsptoDefaults.PageSize, Is.Positive);
            Assert.That(UsptoDefaults.PageSize, Is.LessThanOrEqualTo(UsptoDefaults.PatentsViewMaximumPageSize));
            Assert.That(UsptoDefaults.PatentsViewMaximumPageSize, Is.EqualTo(1000));
            Assert.That(UsptoDefaults.TsdrGeneralRequestsPerMinute, Is.EqualTo(60));
            Assert.That(UsptoDefaults.TsdrPdfAndZipRequestsPerMinute, Is.EqualTo(4));
            Assert.That(UsptoDefaults.PatentsViewRequestsPerMinute, Is.EqualTo(45));
            Assert.That(UsptoDefaults.MaximumRawResponseBytes, Is.LessThanOrEqualTo(UsptoDefaults.MaximumResponseBytes));
            Assert.That(UsptoDefaults.ArchiveEntryBytes, Is.LessThanOrEqualTo(UsptoDefaults.ArchiveTotalBytes));
            Assert.That(UsptoDefaults.InitialRetryDelayMilliseconds, Is.LessThanOrEqualTo(UsptoDefaults.MaximumRetryDelayMilliseconds));
            Assert.That(UsptoDefaults.MaximumRetryDelayMilliseconds, Is.LessThanOrEqualTo(UsptoDefaults.RetryBudgetMilliseconds));
        });
    }

    [Test]
    public void EnumValuesPreserveRequiredDistinctions()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ApiService.Odp, Is.Not.EqualTo(ApiService.OfficeAction));
            Assert.That(ApiService.Tsdr, Is.Not.EqualTo(ApiService.TsdrPdfAndZip));
            Assert.That(IdentifierKind.Ambiguous, Is.Not.EqualTo(IdentifierKind.Unknown));
            Assert.That(TrademarkIdentifierKind.ReferenceNumber, Is.Not.EqualTo(TrademarkIdentifierKind.InternationalRegistrationNumber));
            Assert.That(DataProvenance.Odp, Is.Not.EqualTo(DataProvenance.PatentsView));
            Assert.That(DataProvenance.Bulk, Is.Not.EqualTo(DataProvenance.LocalMirror));
            Assert.That(MirrorAction.Resumable, Is.Not.EqualTo(MirrorAction.Corrupt));
            Assert.That(CompressionFormat.TarGzip, Is.Not.EqualTo(CompressionFormat.Gzip));
            Assert.That(SearchOperator.And, Is.Not.EqualTo(SearchOperator.Or));
            Assert.That(DownloadState.Completed, Is.Not.EqualTo(DownloadState.Failed));
        });
    }
}
