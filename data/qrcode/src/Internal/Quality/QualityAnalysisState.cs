namespace qrcode;

internal class QualityAnalysisState
{
    internal QrQualityReport Report = new QrQualityReport();
    internal MatrixRecoveryResult Recovery = new MatrixRecoveryResult();
    internal QrMatrix? Reference;
}
