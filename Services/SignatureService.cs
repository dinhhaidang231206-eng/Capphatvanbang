using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using iText.Kernel.Pdf;
using iText.Signatures;
using iText.Bouncycastle.X509;
using iText.Commons.Bouncycastle.Cert;
using Org.BouncyCastle.X509;

namespace HeThongVanBangSo.Services
{
    public class X509Certificate2Signature : IExternalSignature
    {
        private readonly X509Certificate2 _certificate;
        private readonly string _digestAlgorithm;

        public X509Certificate2Signature(X509Certificate2 certificate, string digestAlgorithm)
        {
            _certificate = certificate;
            _digestAlgorithm = digestAlgorithm;
        }

        public string GetDigestAlgorithmName() => _digestAlgorithm;
        public string GetSignatureAlgorithmName() => "RSA";
        public iText.Signatures.ISignatureMechanismParams GetSignatureMechanismParameters() => null;

        public byte[] Sign(byte[] message)
        {
            using (var rsa = _certificate.GetRSAPrivateKey())
            {
                if (rsa == null) throw new InvalidOperationException("Certificate does not contain an RSA private key.");
                return rsa.SignData(message, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
        }
    }

    public class SignatureService : ISignatureService
    {
        private readonly ILogger<SignatureService> _logger;

        public SignatureService(ILogger<SignatureService> logger)
        {
            _logger = logger;
        }

        public async Task<string> ComputeSha256HashAsync(Stream stream)
        {
            if (stream.CanSeek) stream.Position = 0;
            using var sha256 = SHA256.Create();
            byte[] hashBytes = await sha256.ComputeHashAsync(stream);
            if (stream.CanSeek) stream.Position = 0;
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        public string ComputeSha256Hash(byte[] data)
        {
            using var sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(data);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        public (string PublicKey, string PrivateKey) GenerateRsaKeyPair(int keySize = 2048)
        {
            using var rsa = RSA.Create(keySize);
            string publicKeyPem = rsa.ExportSubjectPublicKeyInfoPem();
            string privateKeyPem = rsa.ExportPkcs8PrivateKeyPem();
            return (publicKeyPem, privateKeyPem);
        }

        public string SignData(byte[] data, string privateKeyPem)
        {
            try
            {
                using var rsa = RSA.Create();
                rsa.ImportFromPem(privateKeyPem);
                byte[] signatureBytes = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                return Convert.ToBase64String(signatureBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong quá trình ký số bằng Private Key");
                throw new InvalidOperationException("Không thể ký số dữ liệu: " + ex.Message, ex);
            }
        }

        public bool VerifySignature(byte[] data, string signatureBase64, string publicKeyPem)
        {
            try
            {
                using var rsa = RSA.Create();
                rsa.ImportFromPem(publicKeyPem);
                byte[] signatureBytes = Convert.FromBase64String(signatureBase64);
                return rsa.VerifyData(data, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi xác thực chữ ký số");
                return false;
            }
        }

        public byte[] SignPdf(byte[] pdfBytes, string privateKeyPem)
        {
            // Keeping old implementation for compatibility if needed, but not strictly required if we move to SignPdfFile
            throw new NotImplementedException("Dùng SignPdfFile thay thế.");
        }

        public bool SignPdfFile(string inputPdfPath, string outputPdfPath, X509Certificate2 certificate, string reason, string location)
        {
            try
            {
                using (var reader = new PdfReader(inputPdfPath))
                using (var os = new FileStream(outputPdfPath, FileMode.Create))
                {
                    var properties = new StampingProperties();
                    properties.UseAppendMode();
                    var signer = new PdfSigner(reader, os, properties);

                    // In iText 8/9, appearance setup is different for visual signatures. 
                    // We use an invisible signature by default.

                    IExternalSignature pks = new X509Certificate2Signature(certificate, "SHA256");

                    var certParser = new X509CertificateParser();
                    var bcCert = certParser.ReadCertificate(certificate.RawData);
                    var itextCert = new X509CertificateBC(bcCert);
                    IX509Certificate[] chain = new[] { itextCert };

                    signer.SignDetached(
                        new BouncyCastleDigest(),
                        pks,
                        chain,
                        null,
                        null,
                        null,
                        0,
                        PdfSigner.CryptoStandard.CMS);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi nhúng chữ ký vào PDF");
                return false;
            }
        }

        public Models.PdfSignatureVerificationResult VerifyPdfSignature(string pdfFilePath)
        {
            var result = new Models.PdfSignatureVerificationResult();
            try
            {
                using var reader = new PdfReader(pdfFilePath);
                using var document = new PdfDocument(reader);
                var signatureUtil = new SignatureUtil(document);
                var names = signatureUtil.GetSignatureNames();

                if (names == null || names.Count == 0)
                {
                    result.IsValid = false;
                    result.Message = "Không tìm thấy chữ ký số trong tài liệu.";
                    return result;
                }

                string sigName = names[names.Count - 1]; // Lấy chữ ký cuối cùng
                var pkcs7 = signatureUtil.ReadSignatureData(sigName);
                
                bool sigValid = pkcs7.VerifySignatureIntegrityAndAuthenticity();
                bool isModified = !signatureUtil.SignatureCoversWholeDocument(sigName);
                
                result.IsValid = sigValid && !isModified;
                result.IsDocumentModified = isModified;
                var cert = pkcs7.GetSigningCertificate();
                var subjectFields = iText.Signatures.CertificateInfo.GetSubjectFields(cert);
                string cn = subjectFields.GetField("CN");
                result.SignerName = !string.IsNullOrEmpty(cn) ? cn : "Unknown Signer";
                
                // GetSignDate usually returns DateTime
                result.SignDate = pkcs7.GetSignDate();
                
                result.Message = result.IsValid ? "Chữ ký hợp lệ." : "Chữ ký không hợp lệ hoặc tài liệu đã bị sửa đổi.";

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi kiểm tra chữ ký");
                result.IsValid = false;
                result.Message = "Lỗi khi đọc file: " + ex.Message;
                return result;
            }
        }
    }
}
