using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using HeThongVanBangSo.Data;
using HeThongVanBangSo.Models;
using HeThongVanBangSo.Services;
using System.Text.Json.Serialization;
using Microsoft.Extensions.FileProviders;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Giới hạn kích thước request body: 15 MB (bao gồm cả metadata của multipart form)
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 15 * 1024 * 1024;
});

// 1. Cấu hình DbContext kết nối SQL Server
builder.Services.AddDbContext<HeThongVanBangDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    )
);

// 2. Cấu hình ASP.NET Core Identity
builder.Services.AddIdentity<NguoiDung, IdentityRole>(options =>
{
    // Cấu hình Password (tùy chỉnh cho phù hợp môi trường dev)
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;

    // Cấu hình Lockout
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // Cấu hình User
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<HeThongVanBangDbContext>()
.AddDefaultTokenProviders();

// Cấu hình Cookie cho Login/AccessDenied
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.LogoutPath = "/Account/Logout";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// 3. Đăng ký Dependency Injection cho Service Ký số & Băm SHA-256
builder.Services.AddSingleton<ISignatureService, SignatureService>();

// 4. Cấu hình Controllers (API & MVC) và xử lý JSON vòng lặp
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// 5. Cấu hình CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// ========== SEED DATA: Tạo Role và Admin mặc định ==========
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<NguoiDung>>();

        // Tạo 3 Role
        string[] roleNames = { "Admin", "NhanVien", "ChuVanBang" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // Tạo tài khoản Admin mặc định
        string adminEmail = "admin@hethong.vn";
        string adminPassword = "Admin@123";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new NguoiDung
            {
                UserName = "admin",
                Email = adminEmail,
                HoTen = "Quản trị viên",
                EmailConfirmed = true,
                NgayTao = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // Tạo tài khoản Sinh viên mẫu
        string studentEmail = "sinhvien@hethong.vn";
        var studentUser = await userManager.FindByEmailAsync(studentEmail);
        if (studentUser == null)
        {
            studentUser = new NguoiDung
            {
                UserName = "sinhvien",
                Email = studentEmail,
                HoTen = "Sinh Viên Mẫu",
                EmailConfirmed = true,
                NgayTao = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(studentUser, "Student@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(studentUser, "ChuVanBang");
            }
        }

        // Seed dữ liệu mẫu cho Yêu cầu cấp phát
        var dbContext = services.GetRequiredService<HeThongVanBangDbContext>();
        if (!dbContext.YeuCauCapPhats.Any() && studentUser != null)
        {
            dbContext.YeuCauCapPhats.AddRange(
                new YeuCauCapPhat
                {
                    NguoiDungId = studentUser.Id,
                    MaDonVi = 101, // ID của UIT
                    TenVanBang = "Bằng Cử nhân CNTT",
                    HoTen = "Sinh Viên Mẫu",
                    Email = studentEmail,
                    SoCCCD = "079099001234",
                    NgaySinh = new DateTime(2000, 1, 1),
                    TrangThai = "CHO_DUYET",
                    NgayYeuCau = DateTime.UtcNow
                },
                new YeuCauCapPhat
                {
                    NguoiDungId = studentUser.Id,
                    MaDonVi = 102, // ID của HCMUT
                    TenVanBang = "Chứng chỉ Tiếng Anh B1",
                    HoTen = "Sinh Viên Mẫu",
                    Email = studentEmail,
                    SoCCCD = "079099001234",
                    NgaySinh = new DateTime(2000, 1, 1),
                    TrangThai = "DA_DUYET",
                    NgayYeuCau = DateTime.UtcNow.AddDays(-2)
                },
                new YeuCauCapPhat
                {
                    NguoiDungId = studentUser.Id,
                    MaDonVi = 101,
                    TenVanBang = "Giấy chứng nhận Thực tập",
                    HoTen = "Sinh Viên Mẫu",
                    Email = studentEmail,
                    SoCCCD = "079099001234",
                    NgaySinh = new DateTime(2000, 1, 1),
                    TrangThai = "TU_CHOI",
                    LyDoTuChoi = "Thiếu giấy xác nhận của công ty",
                    NgayYeuCau = DateTime.UtcNow.AddDays(-5)
                }
            );
            await dbContext.SaveChangesAsync();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi khi seed dữ liệu Identity.");
    }
}

// 6. Cấu hình Middleware
app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseStaticFiles(); // Cho phép truy cập file tĩnh (wwwroot, css, js)

var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "Uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/Uploads"
});

app.UseRouting();

app.UseAuthentication(); // Phải đặt TRƯỚC UseAuthorization
app.UseAuthorization();

// Map API Controllers
app.MapControllers();

// Map MVC Default Route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
