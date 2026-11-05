using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace iso20022;

internal class SignatureKeyMaterial
{
    internal RSA? Key;
    internal X509Certificate2? Certificate;
    internal byte[] CertificateBytes = [];
    internal bool Malformed;
}
