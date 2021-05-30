using Microsoft.EntityFrameworkCore.Migrations;

namespace WamBot.Twitch.Migrations
{
    public partial class Reset : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DbChannels",
                columns: table => new
                {
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    LastStreamId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbChannels", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "DbUsers",
                columns: table => new
                {
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    OnyxPoints = table.Column<long>(type: "INTEGER", nullable: false),
                    PenisOffset = table.Column<int>(type: "INTEGER", nullable: false),
                    PenisType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbUsers", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "DbChannelUsers",
                columns: table => new
                {
                    UserName = table.Column<string>(type: "TEXT", nullable: false),
                    ChannelName = table.Column<string>(type: "TEXT", nullable: false),
                    Balance = table.Column<decimal>(type: "TEXT", nullable: false),
                    LastStreamId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbChannelUsers", x => new { x.UserName, x.ChannelName });
                    table.ForeignKey(
                        name: "FK_DbChannelUsers_DbChannels_ChannelName",
                        column: x => x.ChannelName,
                        principalTable: "DbChannels",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DbChannelUsers_DbUsers_UserName",
                        column: x => x.UserName,
                        principalTable: "DbUsers",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DbChannelUsers_ChannelName",
                table: "DbChannelUsers",
                column: "ChannelName");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DbChannelUsers");

            migrationBuilder.DropTable(
                name: "DbChannels");

            migrationBuilder.DropTable(
                name: "DbUsers");
        }
    }
}
