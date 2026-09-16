using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HeThongVanBangSo.Migrations
{
    /// <inheritdoc />
    public partial class SeedData2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "DonViPhatHanh",
                columns: new[] { "MaDonVi", "Email", "MaCode", "TenDonVi", "TrangThaiHoatDong" },
                values: new object[,]
                {
                    { 101, "info@uit.edu.vn", "UIT", "Đại học Công nghệ thông tin", true },
                    { 102, "info@hcmut.edu.vn", "HCMUT", "Đại học Bách Khoa", true }
                });

            migrationBuilder.InsertData(
                table: "NguoiNhan",
                columns: new[] { "MaNguoiNhan", "Email", "HoTen", "NgaySinh", "SoCCCD" },
                values: new object[,]
                {
                    { 101L, "nguyenvana@gmail.com", "Nguyễn Văn A", new DateTime(1999, 1, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "079099001234" },
                    { 102L, "tranthib@gmail.com", "Trần Thị B", new DateTime(2000, 5, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "079200005678" }
                });

            migrationBuilder.InsertData(
                table: "KhoaKySo",
                columns: new[] { "MaKhoa", "MaDonVi", "NgayHetHan", "PrivateKeyMaHoa", "PublicKeyText", "TrangThai" },
                values: new object[,]
                {
                    { 101, 101, new DateTime(2030, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "sample-private-key-uit", "sample-public-key-uit", true },
                    { 102, 102, new DateTime(2030, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "sample-private-key-hcmut", "sample-public-key-hcmut", true }
                });

            migrationBuilder.InsertData(
                table: "VanBangChungChi",
                columns: new[] { "MaVanBang", "ChuKySo", "DuongDanFileDaKy", "FilePath_Draft", "FilePath_Signed", "MaBamSHA256", "MaDonVi", "MaKhoa", "MaNguoiNhan", "NgayCap", "NgayTao", "SoHieu", "TenVanBang", "TrangThai", "TrangThaiDuyet" },
                values: new object[,]
                {
                    { 101L, "signature-1", "/files/bang-1.pdf", "", "", "E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855", 101, 101, 101L, new DateTime(2023, 7, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2023, 7, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "CNTT-001", "Bằng Cử nhân CNTT", "HOP_LE", 3 },
                    { 102L, "signature-2", "/files/bang-2.pdf", "", "", "E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855", 102, 102, 102L, new DateTime(2023, 8, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2023, 8, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "CK-002", "Bằng Kỹ sư Cơ khí", "HOP_LE", 3 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "VanBangChungChi",
                keyColumn: "MaVanBang",
                keyValue: 101L);

            migrationBuilder.DeleteData(
                table: "VanBangChungChi",
                keyColumn: "MaVanBang",
                keyValue: 102L);

            migrationBuilder.DeleteData(
                table: "KhoaKySo",
                keyColumn: "MaKhoa",
                keyValue: 101);

            migrationBuilder.DeleteData(
                table: "KhoaKySo",
                keyColumn: "MaKhoa",
                keyValue: 102);

            migrationBuilder.DeleteData(
                table: "NguoiNhan",
                keyColumn: "MaNguoiNhan",
                keyValue: 101L);

            migrationBuilder.DeleteData(
                table: "NguoiNhan",
                keyColumn: "MaNguoiNhan",
                keyValue: 102L);

            migrationBuilder.DeleteData(
                table: "DonViPhatHanh",
                keyColumn: "MaDonVi",
                keyValue: 101);

            migrationBuilder.DeleteData(
                table: "DonViPhatHanh",
                keyColumn: "MaDonVi",
                keyValue: 102);
        }
    }
}
