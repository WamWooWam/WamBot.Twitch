using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace WamBot.Twitch.Migrations
{
    public partial class IdMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DbChannelUsers_DbChannels_ChannelName",
                table: "DbChannelUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_DbChannelUsers_DbUsers_UserName",
                table: "DbChannelUsers");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_DbUsers_Id",
                table: "DbUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DbUsers",
                table: "DbUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DbChannelUsers",
                table: "DbChannelUsers");

            migrationBuilder.DropIndex(
                name: "IX_DbChannelUsers_ChannelName",
                table: "DbChannelUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DbChannels",
                table: "DbChannels");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "DbChannelUsers");

            migrationBuilder.DropColumn(
                name: "ChannelName",
                table: "DbChannelUsers");

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "DbUsers",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "DbUsers",
                type: "text",
                nullable: true,
                collation: "twitch_name",
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "twitch_name");

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "DbChannels",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "DbChannels",
                type: "text",
                nullable: true,
                collation: "twitch_name",
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "twitch_name");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DbUsers",
                table: "DbUsers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DbChannelUsers",
                table: "DbChannelUsers",
                columns: new[] { "UserId", "ChannelId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_DbChannels",
                table: "DbChannels",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_DbUsers_Name",
                table: "DbUsers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_DbChannelUsers_ChannelId",
                table: "DbChannelUsers",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_DbChannels_Name",
                table: "DbChannels",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_DbChannelUsers_DbChannels_ChannelId",
                table: "DbChannelUsers",
                column: "ChannelId",
                principalTable: "DbChannels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DbChannelUsers_DbUsers_UserId",
                table: "DbChannelUsers",
                column: "UserId",
                principalTable: "DbUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DbChannelUsers_DbChannels_ChannelId",
                table: "DbChannelUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_DbChannelUsers_DbUsers_UserId",
                table: "DbChannelUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DbUsers",
                table: "DbUsers");

            migrationBuilder.DropIndex(
                name: "IX_DbUsers_Name",
                table: "DbUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DbChannelUsers",
                table: "DbChannelUsers");

            migrationBuilder.DropIndex(
                name: "IX_DbChannelUsers_ChannelId",
                table: "DbChannelUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DbChannels",
                table: "DbChannels");

            migrationBuilder.DropIndex(
                name: "IX_DbChannels_Name",
                table: "DbChannels");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "DbUsers",
                type: "text",
                nullable: false,
                defaultValue: "",
                collation: "twitch_name",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "twitch_name");

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "DbUsers",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "DbChannelUsers",
                type: "text",
                nullable: false,
                defaultValue: "",
                collation: "twitch_name");

            migrationBuilder.AddColumn<string>(
                name: "ChannelName",
                table: "DbChannelUsers",
                type: "text",
                nullable: false,
                defaultValue: "",
                collation: "twitch_name");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "DbChannels",
                type: "text",
                nullable: false,
                defaultValue: "",
                collation: "twitch_name",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "twitch_name");

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "DbChannels",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_DbUsers_Id",
                table: "DbUsers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DbUsers",
                table: "DbUsers",
                column: "Name");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DbChannelUsers",
                table: "DbChannelUsers",
                columns: new[] { "UserName", "ChannelName" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_DbChannels",
                table: "DbChannels",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_DbChannelUsers_ChannelName",
                table: "DbChannelUsers",
                column: "ChannelName");

            migrationBuilder.AddForeignKey(
                name: "FK_DbChannelUsers_DbChannels_ChannelName",
                table: "DbChannelUsers",
                column: "ChannelName",
                principalTable: "DbChannels",
                principalColumn: "Name",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DbChannelUsers_DbUsers_UserName",
                table: "DbChannelUsers",
                column: "UserName",
                principalTable: "DbUsers",
                principalColumn: "Name",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
