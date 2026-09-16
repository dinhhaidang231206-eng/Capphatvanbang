using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeThongVanBangSo.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailToYeuCauCapPhat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "YeuCauCapPhats",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "YeuCauCapPhats");
        }
    }
}
