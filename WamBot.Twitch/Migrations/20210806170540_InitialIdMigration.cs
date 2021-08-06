using Microsoft.EntityFrameworkCore.Migrations;

namespace WamBot.Twitch.Migrations
{
    public partial class InitialIdMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ChannelId",
                table: "DbChannelUsers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "DbChannelUsers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Id",
                table: "DbChannels",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChannelId",
                table: "DbChannelUsers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "DbChannelUsers");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "DbChannels");
        }
    }
}
