using Microsoft.EntityFrameworkCore.Migrations;

namespace WamBot.Twitch.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:CollationDefinition:twitch_name", "en-u-ks-primary,en-u-ks-primary,icu,False");

            migrationBuilder.CreateTable(
                name: "DbChannels",
                columns: table => new
                {
                    Name = table.Column<string>(type: "text", nullable: false, collation: "twitch_name"),
                    LastStreamId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbChannels", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "DbUsers",
                columns: table => new
                {
                    Name = table.Column<string>(type: "text", nullable: false, collation: "twitch_name"),
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OnyxPoints = table.Column<long>(type: "bigint", nullable: false),
                    PenisOffset = table.Column<int>(type: "integer", nullable: false),
                    PenisType = table.Column<int>(type: "integer", nullable: false),
                    ConsecutiveWins = table.Column<int>(type: "integer", nullable: false),
                    ConsecutiveLosses = table.Column<int>(type: "integer", nullable: false),
                    AllTimeConsecutiveWins = table.Column<int>(type: "integer", nullable: false),
                    AllTimeConsecutiveLosses = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbUsers", x => x.Name);
                    table.UniqueConstraint("AK_DbUsers_Id", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DbChannelUsers",
                columns: table => new
                {
                    UserName = table.Column<string>(type: "text", nullable: false, collation: "twitch_name"),
                    ChannelName = table.Column<string>(type: "text", nullable: false, collation: "twitch_name"),
                    Balance = table.Column<decimal>(type: "numeric", nullable: false),
                    LastStreamId = table.Column<string>(type: "text", nullable: true)
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
