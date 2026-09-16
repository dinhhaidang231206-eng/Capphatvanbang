using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HeThongVanBangSo.Data;
using HeThongVanBangSo.Models;
using System.Security.Claims;

namespace HeThongVanBangSo.Controllers
{
    [Authorize]
    public class YeuCauCapPhatController : Controller
    {
        private readonly HeThongVanBangDbContext _context;
        private readonly UserManager<NguoiDung> _userManager;

        public YeuCauCapPhatController(HeThongVanBangDbContext context, UserManager<NguoiDung> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ================= CHO SINH VIÊN / CHỦ VĂN BẰNG =================

        // GET: /YeuCauCapPhat
        [Authorize(Roles = "ChuVanBang")]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var danhSachYeuCau = await _context.YeuCauCapPhats
                .Include(y => y.DonViPhatHanh)
                .Where(y => y.NguoiDungId == userId)
                .OrderByDescending(y => y.NgayYeuCau)
                .ToListAsync();

            return View(danhSachYeuCau);
        }

        // GET: /YeuCauCapPhat/Create
        [Authorize(Roles = "ChuVanBang")]
        public async Task<IActionResult> Create()
        {
            ViewBag.DanhSachDonVi = await _context.DonViPhatHanhs
                .Where(d => d.TrangThaiHoatDong)
                .OrderBy(d => d.TenDonVi)
                .ToListAsync();
            return View();
        }

        // POST: /YeuCauCapPhat/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ChuVanBang")]
        public async Task<IActionResult> Create(int maDonVi, string tenVanBang, string hoTen, string email, string soCCCD, DateTime ngaySinh)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            if (maDonVi <= 0 || string.IsNullOrWhiteSpace(tenVanBang) || string.IsNullOrWhiteSpace(hoTen) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(soCCCD))
            {
                TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin.";
                ViewBag.DanhSachDonVi = await _context.DonViPhatHanhs.Where(d => d.TrangThaiHoatDong).ToListAsync();
                return View();
            }

            var yeuCau = new YeuCauCapPhat
            {
                NguoiDungId = userId,
                MaDonVi = maDonVi,
                TenVanBang = tenVanBang.Trim(),
                HoTen = hoTen.Trim(),
                Email = email.Trim(),
                SoCCCD = soCCCD.Trim(),
                NgaySinh = ngaySinh,
                TrangThai = "CHO_DUYET",
                NgayYeuCau = DateTime.UtcNow
            };

            _context.YeuCauCapPhats.Add(yeuCau);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã gửi yêu cầu cấp phát thành công. Vui lòng chờ phản hồi.";
            return RedirectToAction(nameof(Index));
        }

        // ================= CHO NHÂN VIÊN / ADMIN =================

        // GET: /YeuCauCapPhat/QuanLy
        [Authorize(Roles = "NhanVien,Admin")]
        public async Task<IActionResult> QuanLy()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            IQueryable<YeuCauCapPhat> query = _context.YeuCauCapPhats.Include(y => y.DonViPhatHanh);

            // Nếu là Nhân Viên, chỉ xem yêu cầu thuộc đơn vị của mình
            if (await _userManager.IsInRoleAsync(user, "NhanVien") && user.MaDonVi.HasValue)
            {
                query = query.Where(y => y.MaDonVi == user.MaDonVi.Value);
            }

            var danhSachYeuCau = await query.OrderByDescending(y => y.NgayYeuCau).ToListAsync();
            return View(danhSachYeuCau);
        }

        // POST: /YeuCauCapPhat/Duyet/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "NhanVien,Admin")]
        public async Task<IActionResult> Duyet(long id)
        {
            var yeuCau = await _context.YeuCauCapPhats.FindAsync(id);
            if (yeuCau == null) return NotFound();

            if (yeuCau.TrangThai != "CHO_DUYET")
            {
                TempData["ErrorMessage"] = "Yêu cầu này không ở trạng thái Chờ duyệt.";
                return RedirectToAction(nameof(QuanLy));
            }

            // Kiểm tra xem người nhận (CCCD) đã tồn tại chưa
            var nguoiNhan = await _context.NguoiNhans.FirstOrDefaultAsync(n => n.SoCCCD == yeuCau.SoCCCD);
            if (nguoiNhan == null)
            {
                // Tự động tạo người nhận mới
                nguoiNhan = new NguoiNhan
                {
                    HoTen = yeuCau.HoTen,
                    SoCCCD = yeuCau.SoCCCD,
                    NgaySinh = yeuCau.NgaySinh
                };
                _context.NguoiNhans.Add(nguoiNhan);
                await _context.SaveChangesAsync(); // Lưu để lấy MaNguoiNhan
            }

            yeuCau.TrangThai = "DA_DUYET";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã duyệt yêu cầu. Đang chuyển sang giao diện Tạo văn bằng cho {nguoiNhan.HoTen}.";
            
            // Chuyển hướng sang trang Create của VanBangChungChi, truyền tham số
            return RedirectToAction("Create", "VanBangChungChi", new 
            { 
                maNguoiNhan = nguoiNhan.MaNguoiNhan,
                tenVanBang = yeuCau.TenVanBang,
                maDonVi = yeuCau.MaDonVi
            });
        }

        // POST: /YeuCauCapPhat/TuChoi/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "NhanVien,Admin")]
        public async Task<IActionResult> TuChoi(long id, string lyDoTuChoi)
        {
            var yeuCau = await _context.YeuCauCapPhats.FindAsync(id);
            if (yeuCau == null) return NotFound();

            if (yeuCau.TrangThai != "CHO_DUYET")
            {
                TempData["ErrorMessage"] = "Yêu cầu này không ở trạng thái Chờ duyệt.";
                return RedirectToAction(nameof(QuanLy));
            }

            if (string.IsNullOrWhiteSpace(lyDoTuChoi))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập lý do từ chối.";
                return RedirectToAction(nameof(QuanLy));
            }

            yeuCau.TrangThai = "TU_CHOI";
            yeuCau.LyDoTuChoi = lyDoTuChoi.Trim();
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã từ chối yêu cầu cấp phát.";
            return RedirectToAction(nameof(QuanLy));
        }
    }
}
