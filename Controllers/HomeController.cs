using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HeThongVanBangSo.Data;
using HeThongVanBangSo.Models;
using HeThongVanBangSo.Services;

namespace HeThongVanBangSo.Controllers
{
    public class HomeController : Controller
    {
        private readonly HeThongVanBangDbContext _context;
        private readonly ISignatureService _signatureService;

        private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

        public HomeController(HeThongVanBangDbContext context, ISignatureService signatureService)
        {
            _context = context;
            _signatureService = signatureService;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        // GET: /Home/TraCuu
        [AllowAnonymous]
        public IActionResult TraCuu()
        {
            return View();
        }

        // POST: /Home/TraCuu — Kiểm tra qua file upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> TraCuuByFile(IFormFile fileVanBang)
        {
            if (fileVanBang == null || fileVanBang.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn tệp tin văn bằng để kiểm tra.";
                return RedirectToAction(nameof(TraCuu));
            }

            if (fileVanBang.Length > MaxFileSize)
            {
                TempData["ErrorMessage"] = "Kích thước tệp tin vượt quá giới hạn 10 MB.";
                return RedirectToAction(nameof(TraCuu));
            }

            // Đọc file và tính hash
            byte[] fileBytes;
            using (var ms = new MemoryStream())
            {
                await fileVanBang.CopyToAsync(ms);
                fileBytes = ms.ToArray();
            }

            // Lưu tạm để iText7 xác thực
            string tempFilePath = Path.GetTempFileName();
            await System.IO.File.WriteAllBytesAsync(tempFilePath, fileBytes);
            
            var verifyResult = _signatureService.VerifyPdfSignature(tempFilePath);
            System.IO.File.Delete(tempFilePath);

            string maBamFile = _signatureService.ComputeSha256Hash(fileBytes);
            string tenFile = Path.GetFileName(fileVanBang.FileName);
            string? ipClient = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Đối chiếu trong DB
            var vanBang = await _context.VanBangChungChis
                .Include(v => v.DonViPhatHanh)
                .Include(v => v.NguoiNhan)
                .Include(v => v.KhoaKySo)
                .FirstOrDefaultAsync(v => v.MaBamSHA256 == maBamFile);

            bool ketQuaHopLe = false;
            bool chuKySoHopLe = false;
            string ghiChu;

            if (vanBang != null)
            {
                if (vanBang.TrangThai == "THU_HOI" || vanBang.TrangThaiDuyet == TrangThaiPhanLuong.ThuHoi)
                {
                    ghiChu = "CẢNH BÁO: Văn bằng này đã bị đơn vị cấp thu hồi hoặc hủy bỏ hiệu lực.";
                }
                else if (vanBang.TrangThaiDuyet != TrangThaiPhanLuong.DaBanHanh)
                {
                    ghiChu = "CẢNH BÁO: Văn bằng này chưa được ban hành chính thức.";
                }
                else
                {
                    // Xác thực chữ ký số nhúng
                    chuKySoHopLe = verifyResult.IsValid;

                    if (chuKySoHopLe)
                    {
                        ketQuaHopLe = true;
                        ghiChu = "CHÍNH XÁC: Văn bằng hợp lệ, toàn vẹn dữ liệu và có chữ ký số. Người ký: " + verifyResult.SignerName;
                    }
                    else
                    {
                        ghiChu = "CẢNH BÁO: " + verifyResult.Message;
                    }
                }
            }
            else
            {
                if (!verifyResult.IsValid)
                {
                    ghiChu = "CẢNH BÁO: Tệp tin không có chữ ký số hợp lệ hoặc đã bị sửa đổi. " + verifyResult.Message;
                }
                else
                {
                    ghiChu = "CẢNH BÁO: Tệp tin có chữ ký nhưng không tồn tại trong hệ thống (mã băm không khớp).";
                }
            }

            // Lưu lịch sử
            var lichSu = new LichSuKiemTra
            {
                TenFileTaiLen = tenFile,
                MaBamFileTaiLen = maBamFile,
                KetQuaHopLe = ketQuaHopLe,
                GhiChu = ghiChu,
                IPNguoiKiemTra = ipClient,
                ThoiGianKiemTra = DateTime.UtcNow
            };
            _context.LichSuKiemTras.Add(lichSu);
            await _context.SaveChangesAsync();

            // Trả kết quả về View
            ViewBag.KetQuaHopLe = ketQuaHopLe;
            ViewBag.ChuKySoHopLe = chuKySoHopLe;
            ViewBag.GhiChu = ghiChu;
            ViewBag.MaBamFile = maBamFile;
            ViewBag.TenFile = tenFile;
            ViewBag.VanBang = vanBang;
            ViewBag.DaTraCuu = true;

            return View(nameof(TraCuu));
        }

        // POST: /Home/TraCuuByThongTin — Kiểm tra qua thông tin cá nhân
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> TraCuuByThongTin(string hoTen, string soCCCD, DateTime ngaySinh)
        {
            if (string.IsNullOrWhiteSpace(hoTen) || string.IsNullOrWhiteSpace(soCCCD))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập đủ Họ Tên, CCCD và Ngày sinh.";
                return RedirectToAction(nameof(TraCuu));
            }

            var danhSachVanBang = await _context.VanBangChungChis
                .Include(v => v.DonViPhatHanh)
                .Include(v => v.NguoiNhan)
                .Where(v => v.NguoiNhan.SoCCCD == soCCCD.Trim() && 
                            v.NguoiNhan.NgaySinh.Date == ngaySinh.Date && 
                            v.NguoiNhan.HoTen.Contains(hoTen.Trim()))
                .ToListAsync();

            if (danhSachVanBang == null || !danhSachVanBang.Any())
            {
                ViewBag.KetQuaHopLe = false;
                ViewBag.GhiChu = "Không tìm thấy văn bằng nào khớp với thông tin cá nhân.";
                ViewBag.DanhSachVanBang = null;
            }
            else
            {
                ViewBag.KetQuaHopLe = true;
                ViewBag.GhiChu = $"Tìm thấy {danhSachVanBang.Count} văn bằng khớp với thông tin.";
                ViewBag.DanhSachVanBang = danhSachVanBang;
            }

            ViewBag.DaTraCuu = true;
            ViewBag.TenFile = "Tra cứu bằng thông tin cá nhân";

            return View(nameof(TraCuu));
        }
    }
}
