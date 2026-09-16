using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using HeThongVanBangSo.Models;

namespace HeThongVanBangSo.Data
{
    public class HeThongVanBangDbContext : IdentityDbContext<NguoiDung>
    {
        public HeThongVanBangDbContext(DbContextOptions<HeThongVanBangDbContext> options)
            : base(options)
        {
        }

        public DbSet<DonViPhatHanh> DonViPhatHanhs { get; set; } = null!;
        public DbSet<NguoiNhan> NguoiNhans { get; set; } = null!;
        public DbSet<KhoaKySo> KhoaKySos { get; set; } = null!;
        public DbSet<VanBangChungChi> VanBangChungChis { get; set; } = null!;
        public DbSet<LichSuKiemTra> LichSuKiemTras { get; set; } = null!;
        public DbSet<NguoiDung> NguoiDungs { get; set; } = null!;
        public DbSet<YeuCauCapPhat> YeuCauCapPhats { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình bảng NguoiDung (Identity)
            modelBuilder.Entity<NguoiDung>(entity =>
            {
                entity.HasOne(n => n.DonViPhatHanh)
                      .WithMany()
                      .HasForeignKey(n => n.MaDonVi)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // 1. Cấu hình bảng DonViPhatHanh
            modelBuilder.Entity<DonViPhatHanh>(entity =>
            {
                entity.HasIndex(e => e.MaCode).IsUnique();
            });

            // 2. Cấu hình bảng NguoiNhan
            modelBuilder.Entity<NguoiNhan>(entity =>
            {
                entity.HasIndex(e => e.SoCCCD).IsUnique();
            });

            // 3. Cấu hình bảng KhoaKySo
            modelBuilder.Entity<KhoaKySo>(entity =>
            {
                entity.HasOne(k => k.DonViPhatHanh)
                      .WithMany(d => d.DanhSachKhoaKySo)
                      .HasForeignKey(k => k.MaDonVi)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // 4. Cấu hình bảng VanBangChungChi
            modelBuilder.Entity<VanBangChungChi>(entity =>
            {
                entity.HasIndex(e => e.SoHieu).IsUnique();
                entity.HasIndex(e => e.MaBamSHA256).HasDatabaseName("IDX_VanBang_MaBam");

                entity.HasOne(v => v.DonViPhatHanh)
                      .WithMany(d => d.DanhSachVanBang)
                      .HasForeignKey(v => v.MaDonVi)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(v => v.NguoiNhan)
                      .WithMany(n => n.DanhSachVanBang)
                      .HasForeignKey(v => v.MaNguoiNhan)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(v => v.KhoaKySo)
                      .WithMany(k => k.DanhSachVanBang)
                      .HasForeignKey(v => v.MaKhoa)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // 5. Cấu hình bảng LichSuKiemTra
            modelBuilder.Entity<LichSuKiemTra>(entity =>
            {
                entity.HasIndex(e => e.MaBamFileTaiLen);
            });

            // 6. Seed Data (Dữ liệu mẫu)
            modelBuilder.Entity<DonViPhatHanh>().HasData(
                new DonViPhatHanh { MaDonVi = 101, TenDonVi = "Đại học Công nghệ thông tin", MaCode = "UIT", Email = "info@uit.edu.vn", TrangThaiHoatDong = true },
                new DonViPhatHanh { MaDonVi = 102, TenDonVi = "Đại học Bách Khoa", MaCode = "HCMUT", Email = "info@hcmut.edu.vn", TrangThaiHoatDong = true }
            );

            modelBuilder.Entity<NguoiNhan>().HasData(
                new NguoiNhan { MaNguoiNhan = 101, HoTen = "Nguyễn Văn A", SoCCCD = "079099001234", Email = "nguyenvana@gmail.com", NgaySinh = new DateTime(1999, 1, 15) },
                new NguoiNhan { MaNguoiNhan = 102, HoTen = "Trần Thị B", SoCCCD = "079200005678", Email = "tranthib@gmail.com", NgaySinh = new DateTime(2000, 5, 20) }
            );

            string samplePublicKey = @"-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEArYGBenyzXBPEfZds5kTX
uh4qsXH0cIXIt02tkdThcViej+R0TsVsUPyEheLga7DyljMuQkwE+bcffjyhQf4P
E3viqdMPwU1lmD4gkwhUmHfDap4/SZWR26CahZJ+UmdbkzgkLJo1KX32mTDaIyki
l+S7zTFzXboixN84L97H4urCX81SAjeNPacv9rw0xX+YwqZ0eRs8QsXX3hZTmv5j
FQlVS/dDg3zHyXtmHQeoMj4k1/JXM0iKwZxFBkGfVkfVnm/jVIS0dkVFLBSMS46s
22t6fwklNcqP9oNqwdeN/xYr9HGXqwO6ceE0ZHO6tkSeZdq8QTWPTiSsCnsbtaG3
IQIDAQAB
-----END PUBLIC KEY-----";

            string samplePrivateKey = @"-----BEGIN PRIVATE KEY-----
MIIEvwIBADANBgkqhkiG9w0BAQEFAASCBKkwggSlAgEAAoIBAQCtgYF6fLNcE8R9
l2zmRNe6HiqxcfRwhci3Ta2R1OFxWJ6P5HROxWxQ/ISF4uBrsPKWMy5CTAT5tx9+
PKFB/g8Te+Kp0w/BTWWYPiCTCFSYd8Nqnj9JlZHboJqFkn5SZ1uTOCQsmjUpffaZ
MNojKSKX5LvNMXNduiLE3zgv3sfi6sJfzVICN409py/2vDTFf5jCpnR5GzxCxdfe
FlOa/mMVCVVL90ODfMfJe2YdB6gyPiTX8lczSIrBnEUGQZ9WR9Web+NUhLR2RUUs
FIxLjqzba3p/CSU1yo/2g2rB143/Fiv0cZerA7px4TRkc7q2RJ5l2rxBNY9OJKwK
exu1obchAgMBAAECggEAOjDhGA+SqCMJF4Ydw+z63TUY5IJvlP2Jn9CxkeNv6O/r
3h6k03ZrKY5HdA/vbK7f7Xgk359XW9kK+u+itdtbexFlp8dd1Vr749+SVT3KAYjJ
RYzldOxxCtQGfx3ut/xqPinqF/twMQZKGn7D6l71dzQDuIIJSzoORzEGSyfQqk28
cfWZEld00mCP4nGo7ODOU5TFAh0iwBvKPNB6ZeehQUVeCvuL2o5SIoANGy63QeYH
UrN1Q6qCpw8aMjvOmfH8dbvKirLjLnuh1jm+T9a+pT+VYW8gb2QXVaLw35I9KV46
4G86AxIk2+e80PWQ0vkZ6/YIFI4kV3DFzRbn823pMQKBgQDdPG8k0zthQyXrQ12K
bR55iv+y0WBRDBG1V7wr6u/jt7s2s3P7sp5GEh6/pIEhDLXGav5gzlzjCzCj8jFk
28Zez+DYBHdwIm6y6USVfJ6OqJGJFXNEPNH5JC6xLO+WhSKaY3osWaa9oY4KSXmE
AHcF9yM/+XJkcd352o6tNNiHKwKBgQDIxQzkXhREdl2mIJmnJc+7DJF1f0RXv7BV
fB4ou0K7c8bKOJ3ZYNjEszCtDHGgzzDFAOIqlTAopjvonya4emfb/7S7A04YYRU6
a9xxgJChb8MhhpqP+T6UMUwHxfkyFqIhWg/Bkln1zT489x+mSBzsjsUKwyXIirAc
Gy8KQ/eU4wKBgQC3Y/9l8UvJxlXKfZ8uvmGCszxeyL0ksfKD9mRfq3KLu6QPJhbA
0EHvJ9ohVoZMFTMhdVPEf4v0ETSS0pMrXhtEQOHLb3hqlcBZwpA9sn3lF8r8bbGN
ITWVZu7lo7A/f8E9ZbTCytYzX5ZU5K88Qv5nDYRE5cxzgwhUs12OKr5K3QKBgQCm
DTRIfPdOWIfEKxpqgH97OT3lbEleOhDh4zIehiL/XxZ/kqwbalpe9cXAmpYwZqzz
g3OLvLCELllGYLtpwPO9pZQZSPaCe2lPVH6S8b6thv5g8C9N/NuhPdSgaUFeCBI4
e4CknSMChaqASfRHV0V08fBOonDRmMNnu6QAXY6b+QKBgQDBV8rTIqyBIOsw3pWe
1TCv6dSRFAZILrmi/lQkTJHFDH0trE3jDh7ksMjtCm23YQYsrHa5MkVfWrSoML32
Uvahi+PJuU3CyNhrfyPYCkDrrN5uQ8HymZFytyWNaL8tKioBhrzl7HWX67rNBFQP
F5AbUyfBa74dJ3Gj8LSL7A50iA==
-----END PRIVATE KEY-----";

            modelBuilder.Entity<KhoaKySo>().HasData(
                new KhoaKySo { MaKhoa = 101, MaDonVi = 101, PublicKeyText = samplePublicKey, PrivateKeyMaHoa = samplePrivateKey, NgayHetHan = new DateTime(2030, 1, 1), TrangThai = true },
                new KhoaKySo { MaKhoa = 102, MaDonVi = 102, PublicKeyText = samplePublicKey, PrivateKeyMaHoa = samplePrivateKey, NgayHetHan = new DateTime(2030, 1, 1), TrangThai = true }
            );

            modelBuilder.Entity<VanBangChungChi>().HasData(
                new VanBangChungChi { MaVanBang = 101, MaDonVi = 101, MaNguoiNhan = 101, MaKhoa = 101, TenVanBang = "Bằng Cử nhân CNTT", SoHieu = "CNTT-001", DuongDanFileDaKy = "/files/bang-1.pdf", ChuKySo = "signature-1", MaBamSHA256 = "E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855", NgayCap = new DateTime(2023, 7, 15), TrangThai = "HOP_LE", TrangThaiDuyet = TrangThaiPhanLuong.DaBanHanh, NgayTao = new DateTime(2023, 7, 10) },
                new VanBangChungChi { MaVanBang = 102, MaDonVi = 102, MaNguoiNhan = 102, MaKhoa = 102, TenVanBang = "Bằng Kỹ sư Cơ khí", SoHieu = "CK-002", DuongDanFileDaKy = "/files/bang-2.pdf", ChuKySo = "signature-2", MaBamSHA256 = "E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855", NgayCap = new DateTime(2023, 8, 20), TrangThai = "HOP_LE", TrangThaiDuyet = TrangThaiPhanLuong.DaBanHanh, NgayTao = new DateTime(2023, 8, 15) }
            );
        }
    }
}
