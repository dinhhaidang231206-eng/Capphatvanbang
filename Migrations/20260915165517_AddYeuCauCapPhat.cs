using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeThongVanBangSo.Migrations
{
    /// <inheritdoc />
    public partial class AddYeuCauCapPhat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "YeuCauCapPhats",
                columns: table => new
                {
                    MaYeuCau = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NguoiDungId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MaDonVi = table.Column<int>(type: "int", nullable: false),
                    TenVanBang = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SoCCCD = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NgaySinh = table.Column<DateTime>(type: "date", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LyDoTuChoi = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NgayYeuCau = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YeuCauCapPhats", x => x.MaYeuCau);
                    table.ForeignKey(
                        name: "FK_YeuCauCapPhats_AspNetUsers_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_YeuCauCapPhats_DonViPhatHanh_MaDonVi",
                        column: x => x.MaDonVi,
                        principalTable: "DonViPhatHanh",
                        principalColumn: "MaDonVi",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauCapPhats_MaDonVi",
                table: "YeuCauCapPhats",
                column: "MaDonVi");

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauCapPhats_NguoiDungId",
                table: "YeuCauCapPhats",
                column: "NguoiDungId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "YeuCauCapPhats");
        }
    }
}
