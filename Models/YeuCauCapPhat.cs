using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeThongVanBangSo.Models
{
    public class YeuCauCapPhat
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long MaYeuCau { get; set; }

        [Required]
        public string NguoiDungId { get; set; } = string.Empty;

        [Required]
        public int MaDonVi { get; set; }

        [Required(ErrorMessage = "Tên văn bằng không được để trống")]
        [MaxLength(255)]
        public string TenVanBang { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ tên không được để trống")]
        [MaxLength(150)]
        public string HoTen { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc để nhận thông báo")]
        [MaxLength(100)]
        [EmailAddress(ErrorMessage = "Địa chỉ Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số CCCD/CMND là bắt buộc")]
        [MaxLength(20)]
        public string SoCCCD { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngày sinh là bắt buộc")]
        [Column(TypeName = "date")]
        public DateTime NgaySinh { get; set; }

        [MaxLength(20)]
        public string TrangThai { get; set; } = "CHO_DUYET"; // CHO_DUYET, DA_DUYET, TU_CHOI

        [MaxLength(500)]
        public string? LyDoTuChoi { get; set; }

        public DateTime NgayYeuCau { get; set; } = DateTime.UtcNow;

        [ForeignKey("NguoiDungId")]
        public virtual NguoiDung? NguoiDung { get; set; }

        [ForeignKey("MaDonVi")]
        public virtual DonViPhatHanh? DonViPhatHanh { get; set; }
    }
}
