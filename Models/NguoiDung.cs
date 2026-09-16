using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace HeThongVanBangSo.Models
{
    public class NguoiDung : IdentityUser
    {
        [Required(ErrorMessage = "Họ tên không được để trống")]
        [MaxLength(150)]
        public string HoTen { get; set; } = string.Empty;

        /// <summary>
        /// Liên kết nhân viên với đơn vị phát hành (nullable cho KhachTraCuu)
        /// </summary>
        public int? MaDonVi { get; set; }

        [ForeignKey("MaDonVi")]
        public virtual DonViPhatHanh? DonViPhatHanh { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    }
}
