using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HeThongVanBangSo.Data;

namespace HeThongVanBangSo.Controllers
{
    [Authorize(Roles = "NhanVien,Admin")]
    public class LichSuKiemTraController : Controller
    {
        private readonly HeThongVanBangDbContext _context;

        public LichSuKiemTraController(HeThongVanBangDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var lichSu = await _context.LichSuKiemTras
                .OrderByDescending(l => l.ThoiGianKiemTra)
                .ToListAsync();

            return View(lichSu);
        }
    }
}
