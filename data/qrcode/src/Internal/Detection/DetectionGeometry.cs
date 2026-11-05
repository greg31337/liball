namespace qrcode;

internal class DetectionGeometry
{
    internal QrDetection Detection = new QrDetection();

    internal QrPoint[] Corners = new QrPoint[0];

    internal double MinimumX;

    internal double MinimumY;

    internal double Tolerance;

    internal global::System.Collections.Generic.List<QrDetection> Members =
        new global::System.Collections.Generic.List<QrDetection>();
}
