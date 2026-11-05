namespace uspto;

internal static class TsdrPolicy
{
    internal static ApiService ClassifyRateService(DocumentFormat format)
    {
        switch (format)
        {
            case DocumentFormat.Pdf:
            case DocumentFormat.Zip:
                return ApiService.TsdrPdfAndZip;
            default:
                return ApiService.Tsdr;
        }
    }

    internal static bool IsTsdrService(ApiService service)
    {
        return service == ApiService.Tsdr || service == ApiService.TsdrPdfAndZip;
    }
}
