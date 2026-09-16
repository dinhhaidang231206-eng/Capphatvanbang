namespace HeThongVanBangSo.Models
{
    public class PdfSignatureVerificationResult
    {
        public bool IsValid { get; set; }
        public bool IsDocumentModified { get; set; }
        public string SignerName { get; set; } = string.Empty;
        public DateTime? SignDate { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
