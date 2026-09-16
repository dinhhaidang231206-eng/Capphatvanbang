using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HeThongVanBangSo.Data;
using HeThongVanBangSo.Models;
using HeThongVanBangSo.Services;

namespace HeThongVanBangSo.Controllers
{
    [Authorize(Roles = "NhanVien,Admin")]
    public class VanBangChungChiController : Controller
    {
        private readonly HeThongVanBangDbContext _context;
        private readonly ISignatureService _signatureService;
        private readonly IWebHostEnvironment _env;

        private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB
        private static readonly string[] AllowedExtensions = { ".pdf" };

        public VanBangChungChiController(
            HeThongVanBangDbContext context,
            ISignatureService signatureService,
            IWebHostEnvironment env)
        {
            _context = context;
            _signatureService = signatureService;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var vanBangs = await _context.VanBangChungChis
                .Include(v => v.DonViPhatHanh)
                .Include(v => v.NguoiNhan)
                .OrderByDescending(v => v.NgayTao)
                .ToListAsync();

            return View(vanBangs);
        }

        // GET: /VanBangChungChi/Create
        public async Task<IActionResult> Create(long? maNguoiNhan, string? tenVanBang, int? maDonVi)
        {
            ViewBag.DanhSachDonVi = await _context.DonViPhatHanhs
                .Where(d => d.TrangThaiHoatDong)
                .OrderBy(d => d.TenDonVi)
                .ToListAsync();

            ViewBag.DanhSachNguoiNhan = await _context.NguoiNhans
                .OrderBy(n => n.HoTen)
                .ToListAsync();

            ViewBag.DanhSachYeuCau = await _context.YeuCauCapPhats
                .Where(y => y.TrangThai == "DA_DUYET")
                .Select(y => new { y.SoCCCD, y.TenVanBang })
                .ToListAsync();

            ViewBag.SelectedMaNguoiNhan = maNguoiNhan;
            ViewBag.SelectedTenVanBang = tenVanBang;
            ViewBag.SelectedMaDonVi = maDonVi;

            return View();
        }

        // POST: /VanBangChungChi/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int maDonVi, long maNguoiNhan, string tenVanBang, string soHieu, DateTime ngayCap, IFormFile fileVanBang)
        {
            // Load lại danh sách cho dropdown khi cần trả về View
            async Task LoadViewBags()
            {
                ViewBag.DanhSachDonVi = await _context.DonViPhatHanhs
                    .Where(d => d.TrangThaiHoatDong)
                    .OrderBy(d => d.TenDonVi)
                    .ToListAsync();
                ViewBag.DanhSachNguoiNhan = await _context.NguoiNhans
                    .OrderBy(n => n.HoTen)
                    .ToListAsync();
                ViewBag.DanhSachYeuCau = await _context.YeuCauCapPhats
                    .Where(y => y.TrangThai == "DA_DUYET")
                    .Select(y => new { y.SoCCCD, y.TenVanBang })
                    .ToListAsync();
            }

            // Validate inputs
            if (string.IsNullOrWhiteSpace(tenVanBang) || string.IsNullOrWhiteSpace(soHieu))
            {
                TempData["ErrorMessage"] = "Tên văn bằng và Số hiệu không được để trống.";
                await LoadViewBags();
                return View();
            }

            // Validate file
            if (fileVanBang == null || fileVanBang.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng đính kèm tệp tin văn bằng (PDF).";
                await LoadViewBags();
                return View();
            }

            if (fileVanBang.Length > MaxFileSize)
            {
                TempData["ErrorMessage"] = "Kích thước tệp tin vượt quá giới hạn 10 MB.";
                await LoadViewBags();
                return View();
            }

            string ext = Path.GetExtension(fileVanBang.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
            {
                TempData["ErrorMessage"] = "Chỉ chấp nhận tệp tin định dạng PDF.";
                await LoadViewBags();
                return View();
            }

            // 1. Kiểm tra đơn vị
            var donVi = await _context.DonViPhatHanhs.FindAsync(maDonVi);
            if (donVi == null || !donVi.TrangThaiHoatDong)
            {
                TempData["ErrorMessage"] = "Đơn vị phát hành không tồn tại hoặc đã ngừng hoạt động.";
                await LoadViewBags();
                return View();
            }

            // 2. Kiểm tra người nhận
            var nguoiNhan = await _context.NguoiNhans.FindAsync(maNguoiNhan);
            if (nguoiNhan == null)
            {
                TempData["ErrorMessage"] = "Người nhận văn bằng không tồn tại.";
                await LoadViewBags();
                return View();
            }

            // 3. Kiểm tra trùng số hiệu
            bool isSoHieuExist = await _context.VanBangChungChis.AnyAsync(v => v.SoHieu == soHieu.Trim());
            if (isSoHieuExist)
            {
                TempData["ErrorMessage"] = $"Số hiệu văn bằng '{soHieu}' đã tồn tại.";
                await LoadViewBags();
                return View();
            }

            // 4. Lấy khóa ký số hợp lệ
            var khoaKy = await _context.KhoaKySos
                .Where(k => k.MaDonVi == maDonVi && k.TrangThai && k.NgayHetHan > DateTime.UtcNow)
                .OrderByDescending(k => k.MaKhoa)
                .FirstOrDefaultAsync();

            if (khoaKy == null)
            {
                TempData["ErrorMessage"] = "Đơn vị chưa có khóa ký số hợp lệ. Hãy tạo khóa mới trước.";
                await LoadViewBags();
                return View();
            }

            // 5. Lưu file vào Drafts
            string draftsFolder = Path.Combine(_env.ContentRootPath, "Uploads", "Drafts");
            if (!Directory.Exists(draftsFolder))
            {
                Directory.CreateDirectory(draftsFolder);
            }

            string safeFileName = $"{soHieu.Trim()}_{Path.GetFileName(fileVanBang.FileName)}";
            string draftFilePath = Path.Combine(draftsFolder, safeFileName);
            
            using (var stream = new FileStream(draftFilePath, FileMode.Create))
            {
                await fileVanBang.CopyToAsync(stream);
            }

            string duongDanDraft = $"/Uploads/Drafts/{safeFileName}";

            // 6. Lưu vào DB (Trạng thái Dự thảo, chưa có chữ ký thực)
            var vanBang = new VanBangChungChi
            {
                MaDonVi = maDonVi,
                MaNguoiNhan = maNguoiNhan,
                MaKhoa = khoaKy.MaKhoa,
                TenVanBang = tenVanBang.Trim(),
                SoHieu = soHieu.Trim(),
                FilePath_Draft = duongDanDraft,
                TrangThaiDuyet = TrangThaiPhanLuong.DuThao,
                DuongDanFileDaKy = "", // Sẽ cập nhật khi duyệt
                ChuKySo = "",
                MaBamSHA256 = "0000000000000000000000000000000000000000000000000000000000000000",
                NgayCap = ngayCap.Date,
                TrangThai = "HOP_LE",
                NgayTao = DateTime.UtcNow
            };

            _context.VanBangChungChis.Add(vanBang);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã tạo dự thảo văn bằng '{vanBang.TenVanBang}' (Số hiệu: {vanBang.SoHieu}) thành công! Vui lòng duyệt và ký số.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /VanBangChungChi/ApproveAndSign/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAndSign(long id)
        {
            var vanBang = await _context.VanBangChungChis
                .Include(v => v.KhoaKySo)
                .Include(v => v.DonViPhatHanh)
                .FirstOrDefaultAsync(v => v.MaVanBang == id);

            if (vanBang == null) return NotFound();

            if (vanBang.TrangThaiDuyet == TrangThaiPhanLuong.DaBanHanh)
            {
                TempData["ErrorMessage"] = "Văn bằng đã được ký và ban hành.";
                return RedirectToAction(nameof(Index));
            }

            var khoaKy = vanBang.KhoaKySo;
            if (khoaKy == null || !khoaKy.TrangThai || khoaKy.NgayHetHan <= DateTime.UtcNow)
            {
                TempData["ErrorMessage"] = "Khóa ký số không hợp lệ hoặc đã hết hạn.";
                return RedirectToAction(nameof(Index));
            }

            string draftsFolder = Path.Combine(_env.ContentRootPath, "Uploads", "Drafts");
            string certsFolder = Path.Combine(_env.ContentRootPath, "Uploads", "Certificates");
            if (!Directory.Exists(certsFolder)) Directory.CreateDirectory(certsFolder);

            string draftFileName = Path.GetFileName(vanBang.FilePath_Draft);
            string inputPdfPath = Path.Combine(draftsFolder, draftFileName);
            string outputFileName = $"Signed_{draftFileName}";
            string outputPdfPath = Path.Combine(certsFolder, outputFileName);

            if (!System.IO.File.Exists(inputPdfPath))
            {
                TempData["ErrorMessage"] = "Không tìm thấy file dự thảo để ký.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Sinh chứng thư X509 tự ký từ khóa PEM (Do database chỉ lưu PEM)
                using var rsa = System.Security.Cryptography.RSA.Create();
                rsa.ImportFromPem(khoaKy.PrivateKeyMaHoa);
                var req = new System.Security.Cryptography.X509Certificates.CertificateRequest(
                    new System.Security.Cryptography.X509Certificates.X500DistinguishedName($"CN={vanBang.DonViPhatHanh?.TenDonVi ?? "DonVi"}"), 
                    rsa, 
                    System.Security.Cryptography.HashAlgorithmName.SHA256, 
                    System.Security.Cryptography.RSASignaturePadding.Pkcs1);
                
                using var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(5));

                bool isSuccess = _signatureService.SignPdfFile(
                    inputPdfPath, 
                    outputPdfPath, 
                    cert, 
                    "Phê duyệt cấp phát văn bằng", 
                    "Việt Nam");

                if (isSuccess)
                {
                    vanBang.FilePath_Signed = $"/Uploads/Certificates/{outputFileName}";
                    vanBang.DuongDanFileDaKy = vanBang.FilePath_Signed; // Cập nhật luôn cho tương thích cũ
                    vanBang.TrangThaiDuyet = TrangThaiPhanLuong.DaBanHanh;
                    
                    // Tính lại SHA256 cho file đã ký nhúng
                    byte[] signedBytes = await System.IO.File.ReadAllBytesAsync(outputPdfPath);
                    vanBang.MaBamSHA256 = _signatureService.ComputeSha256Hash(signedBytes);
                    vanBang.ChuKySo = _signatureService.SignData(signedBytes, khoaKy.PrivateKeyMaHoa);

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Đã phê duyệt và ký số nhúng thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Lỗi trong quá trình nhúng chữ ký số (iText7).";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi hệ thống: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /VanBangChungChi/ApprovalList
        [Authorize(Roles = "Admin,Approver")]
        public async Task<IActionResult> ApprovalList()
        {
            var vanBangs = await _context.VanBangChungChis
                .Include(v => v.DonViPhatHanh)
                .Include(v => v.NguoiNhan)
                .Where(v => v.TrangThaiDuyet == TrangThaiPhanLuong.DuThao || v.TrangThaiDuyet == TrangThaiPhanLuong.ChoKyChinhThuc)
                .OrderByDescending(v => v.NgayTao)
                .ToListAsync();

            return View(vanBangs);
        }

        // POST: /VanBangChungChi/BatchApproveAndSign
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Approver")]
        public async Task<IActionResult> BatchApproveAndSign([FromBody] List<long> selectedIds)
        {
            if (selectedIds == null || !selectedIds.Any())
            {
                return Json(new { success = false, message = "Vui lòng chọn ít nhất một văn bằng để duyệt." });
            }

            int successCount = 0;
            int errorCount = 0;

            string draftsFolder = Path.Combine(_env.ContentRootPath, "Uploads", "Drafts");
            string certsFolder = Path.Combine(_env.ContentRootPath, "Uploads", "Certificates");
            if (!Directory.Exists(certsFolder)) Directory.CreateDirectory(certsFolder);

            foreach (var id in selectedIds)
            {
                var vanBang = await _context.VanBangChungChis
                    .Include(v => v.KhoaKySo)
                    .Include(v => v.DonViPhatHanh)
                    .Include(v => v.NguoiNhan)
                    .FirstOrDefaultAsync(v => v.MaVanBang == id);

                if (vanBang == null || vanBang.TrangThaiDuyet == TrangThaiPhanLuong.DaBanHanh)
                {
                    errorCount++;
                    continue;
                }

                var khoaKy = vanBang.KhoaKySo;
                if (khoaKy == null || !khoaKy.TrangThai || khoaKy.NgayHetHan <= DateTime.UtcNow)
                {
                    errorCount++;
                    continue;
                }

                string draftFileName = Path.GetFileName(vanBang.FilePath_Draft);
                string inputPdfPath = Path.Combine(draftsFolder, draftFileName);
                string outputFileName = $"Signed_{Guid.NewGuid().ToString("N").Substring(0, 8)}_{draftFileName}";
                string outputPdfPath = Path.Combine(certsFolder, outputFileName);

                if (!System.IO.File.Exists(inputPdfPath))
                {
                    errorCount++;
                    continue;
                }

                try
                {
                    using var rsa = System.Security.Cryptography.RSA.Create();
                    rsa.ImportFromPem(khoaKy.PrivateKeyMaHoa);
                    var req = new System.Security.Cryptography.X509Certificates.CertificateRequest(
                        new System.Security.Cryptography.X509Certificates.X500DistinguishedName($"CN={vanBang.DonViPhatHanh?.TenDonVi ?? "DonVi"}"),
                        rsa,
                        System.Security.Cryptography.HashAlgorithmName.SHA256,
                        System.Security.Cryptography.RSASignaturePadding.Pkcs1);

                    using var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(5));

                    bool isSuccess = _signatureService.SignPdfFile(
                        inputPdfPath,
                        outputPdfPath,
                        cert,
                        "Phê duyệt cấp phát văn bằng",
                        "Việt Nam");

                    if (isSuccess)
                    {
                        vanBang.FilePath_Signed = $"/Uploads/Certificates/{outputFileName}";
                        vanBang.DuongDanFileDaKy = vanBang.FilePath_Signed;
                        vanBang.TrangThaiDuyet = TrangThaiPhanLuong.DaBanHanh;

                        // Tính lại SHA256 cho file đã ký nhúng
                        byte[] signedBytes = await System.IO.File.ReadAllBytesAsync(outputPdfPath);
                        vanBang.MaBamSHA256 = _signatureService.ComputeSha256Hash(signedBytes);

                        // Tính ChuKySo logic cho metadata
                        string dataToSign = $"{vanBang.MaVanBang}{vanBang.NguoiNhan?.HoTen}{vanBang.NguoiNhan?.SoCCCD}{vanBang.TenVanBang}{vanBang.NgayCap:yyyyMMdd}";
                        byte[] dataBytes = System.Text.Encoding.UTF8.GetBytes(dataToSign);
                        vanBang.ChuKySo = _signatureService.SignData(dataBytes, khoaKy.PrivateKeyMaHoa);

                        successCount++;
                    }
                    else
                    {
                        errorCount++;
                    }
                }
                catch (Exception)
                {
                    errorCount++;
                }
            }

            if (successCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, message = $"Đã duyệt thành công {successCount} văn bằng. Lỗi: {errorCount}." });
        }

        // GET: /VanBangChungChi/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();

            var vanBang = await _context.VanBangChungChis
                .Include(v => v.DonViPhatHanh)
                .Include(v => v.NguoiNhan)
                .Include(v => v.KhoaKySo)
                .FirstOrDefaultAsync(v => v.MaVanBang == id);

            if (vanBang == null) return NotFound();

            return View(vanBang);
        }

        // POST: /VanBangChungChi/ThuHoi/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ThuHoi(long id, string? lyDo)
        {
            var vanBang = await _context.VanBangChungChis.FindAsync(id);
            if (vanBang == null) return NotFound();

            if (vanBang.TrangThai == "THU_HOI")
            {
                TempData["ErrorMessage"] = "Văn bằng này đã bị thu hồi trước đó.";
                return RedirectToAction(nameof(Index));
            }

            vanBang.TrangThai = "THU_HOI";
            vanBang.TrangThaiDuyet = TrangThaiPhanLuong.ThuHoi;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Văn bằng số hiệu '{vanBang.SoHieu}' đã bị thu hồi. Lý do: {lyDo ?? "Không nêu rõ"}";
            return RedirectToAction(nameof(Index));
        }
    }
}
