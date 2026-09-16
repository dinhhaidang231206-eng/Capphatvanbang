using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HeThongVanBangSo.Data;
using HeThongVanBangSo.Models;

namespace HeThongVanBangSo.Controllers
{
    [Authorize(Roles = "Admin")]
    public class QuanLyNguoiDungController : Controller
    {
        private readonly UserManager<NguoiDung> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly HeThongVanBangDbContext _context;

        public QuanLyNguoiDungController(
            UserManager<NguoiDung> userManager,
            RoleManager<IdentityRole> roleManager,
            HeThongVanBangDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // GET: /QuanLyNguoiDung
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users
                .Include(u => u.DonViPhatHanh)
                .OrderByDescending(u => u.NgayTao)
                .ToListAsync();

            // Lấy role của từng user
            var userRoles = new Dictionary<string, IList<string>>();
            foreach (var user in users)
            {
                userRoles[user.Id] = await _userManager.GetRolesAsync(user);
            }

            ViewBag.UserRoles = userRoles;
            return View(users);
        }

        // GET: /QuanLyNguoiDung/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            ViewBag.DanhSachDonVi = await _context.DonViPhatHanhs
                .Where(d => d.TrangThaiHoatDong)
                .OrderBy(d => d.TenDonVi)
                .ToListAsync();
            return View();
        }

        // POST: /QuanLyNguoiDung/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string hoTen, string email, string password, string role, int? maDonVi)
        {
            if (string.IsNullOrWhiteSpace(hoTen) || string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(role))
            {
                TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin.";
                return RedirectToAction(nameof(Create));
            }

            var user = new NguoiDung
            {
                UserName = email.Split('@')[0],
                Email = email,
                HoTen = hoTen.Trim(),
                MaDonVi = maDonVi,
                EmailConfirmed = true,
                NgayTao = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, role);
                TempData["SuccessMessage"] = $"Đã tạo tài khoản '{email}' với vai trò '{role}' thành công.";
                return RedirectToAction(nameof(Index));
            }

            string errorMsg = string.Join("; ", result.Errors.Select(e => e.Description));
            TempData["ErrorMessage"] = "Lỗi tạo tài khoản: " + errorMsg;
            return RedirectToAction(nameof(Create));
        }

        // GET: /QuanLyNguoiDung/Edit/id
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            ViewBag.CurrentRole = currentRoles.FirstOrDefault() ?? "ChuVanBang";
            ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            ViewBag.DanhSachDonVi = await _context.DonViPhatHanhs
                .Where(d => d.TrangThaiHoatDong)
                .OrderBy(d => d.TenDonVi)
                .ToListAsync();

            return View(user);
        }

        // POST: /QuanLyNguoiDung/Edit/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, string hoTen, string email, string role, int? maDonVi, string? newPassword)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.HoTen = hoTen.Trim();
            user.Email = email;
            user.MaDonVi = maDonVi;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                TempData["ErrorMessage"] = "Lỗi cập nhật: " + string.Join("; ", updateResult.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Edit), new { id });
            }

            // Cập nhật Role
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, role);

            // Đổi mật khẩu (nếu có)
            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passResult = await _userManager.ResetPasswordAsync(user, token, newPassword);
                if (!passResult.Succeeded)
                {
                    TempData["ErrorMessage"] = "Lỗi đổi mật khẩu: " + string.Join("; ", passResult.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(Edit), new { id });
                }
            }

            TempData["SuccessMessage"] = $"Đã cập nhật tài khoản '{user.Email}' thành công.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /QuanLyNguoiDung/Delete/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Không cho xóa chính mình
            if (user.UserName == User.Identity?.Name)
            {
                TempData["ErrorMessage"] = "Không thể xóa tài khoản đang đăng nhập.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Đã xóa tài khoản '{user.Email}'.";
            }
            else
            {
                TempData["ErrorMessage"] = "Lỗi xóa tài khoản.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
