namespace HeThongVanBangSo.Services
{
    public interface ISignatureService
    {
        /// <summary>
        /// Tính toán mã băm SHA-256 (64 ký tự hex) từ Stream dữ liệu tệp
        /// </summary>
        Task<string> ComputeSha256HashAsync(Stream stream);

        /// <summary>
        /// Tính toán mã băm SHA-256 từ mảng byte
        /// </summary>
        string ComputeSha256Hash(byte[] data);

        /// <summary>
        /// Sinh cặp khóa RSA 2048-bit (Khóa công khai & Khóa bí mật dạng XML/PEM)
        /// </summary>
        (string PublicKey, string PrivateKey) GenerateRsaKeyPair(int keySize = 2048);

        /// <summary>
        /// Ký số dữ liệu bằng khóa bí mật (Private Key) sử dụng thuật toán RSA-SHA256
        /// </summary>
        string SignData(byte[] data, string privateKeyXml);

        /// <summary>
        /// Xác thực chữ ký số bằng khóa công khai (Public Key)
        /// </summary>
        bool VerifySignature(byte[] data, string signatureBase64, string publicKeyXml);

        /// <summary>
        /// Ký số trực tiếp vào nội dung file PDF
        /// </summary>
        byte[] SignPdf(byte[] pdfBytes, string privateKeyPem);

        /// <summary>
        /// Ký số nhúng trực tiếp chứng thư số vào file PDF và lưu ra file mới
        /// </summary>
        bool SignPdfFile(string inputPdfPath, string outputPdfPath, System.Security.Cryptography.X509Certificates.X509Certificate2 certificate, string reason, string location);

        /// <summary>
        /// Đọc file PDF và kiểm tra chữ ký nhúng
        /// </summary>
        HeThongVanBangSo.Models.PdfSignatureVerificationResult VerifyPdfSignature(string pdfFilePath);
    }
}
