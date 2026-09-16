using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using HeThongVanBangSo.Models;

namespace HeThongVanBangSo.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<NguoiDung> _userManager;
        private readonly SignInManager<NguoiDung> _signInManager;

        public AccountController(
            UserManager<NguoiDung> userManager,
            SignInManager<NguoiDung> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // ==================== LOGIN ====================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe = false, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ email và mật khẩu.";
                return View();
            }

            // Tìm user bằng email
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Email hoặc mật khẩu không chính xác.";
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!, password, rememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                TempData["ErrorMessage"] = "Tài khoản đã bị khóa do đăng nhập sai quá nhiều lần. Vui lòng thử lại sau 5 phút.";
                return View();
            }

            TempData["ErrorMessage"] = "Email hoặc mật khẩu không chính xác.";
            return View();
        }

        // ==================== REGISTER ====================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string hoTen, string email, string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(hoTen) || string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin.";
                return View();
            }

            if (password != confirmPassword)
            {
                TempData["ErrorMessage"] = "Mật khẩu xác nhận không khớp.";
                return View();
            }

            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                TempData["ErrorMessage"] = "Email này đã được sử dụng.";
                return View();
            }

            var user = new NguoiDung
            {
                UserName = email.Split('@')[0], // Lấy phần trước @ làm username
                Email = email,
                HoTen = hoTen.Trim(),
                EmailConfirmed = true,
                NgayTao = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                // Gán role ChuVanBang mặc định cho người đăng ký
                await _userManager.AddToRoleAsync(user, "ChuVanBang");

                // Tự động đăng nhập sau khi đăng ký
                await _signInManager.SignInAsync(user, isPersistent: false);

                TempData["SuccessMessage"] = "Đăng ký thành công! Chào mừng bạn đến với Hệ Thống Văn Bằng Số.";
                return RedirectToAction("Index", "Home");
            }

            // Hiển thị lỗi validation từ Identity
            foreach (var error in result.Errors)
            {
                string msg = error.Code switch
                {
                    "PasswordTooShort" => "Mật khẩu phải có ít nhất 6 ký tự.",
                    "PasswordRequiresUpper" => "Mật khẩu phải chứa ít nhất 1 chữ in hoa.",
                    "PasswordRequiresLower" => "Mật khẩu phải chứa ít nhất 1 chữ thường.",
                    "PasswordRequiresDigit" => "Mật khẩu phải chứa ít nhất 1 chữ số.",
                    "DuplicateUserName" => "Tên đăng nhập đã tồn tại.",
                    "DuplicateEmail" => "Email đã được sử dụng.",
                    _ => error.Description
                };
                TempData["ErrorMessage"] = msg;
                break; // Chỉ hiển thị lỗi đầu tiên
            }

            return View();
        }

        // ==================== LOGOUT ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData["SuccessMessage"] = "Bạn đã đăng xuất thành công.";
            return RedirectToAction("Index", "Home");
        }

        // ==================== ACCESS DENIED ====================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
