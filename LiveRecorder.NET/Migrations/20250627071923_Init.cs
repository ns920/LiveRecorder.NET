using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiveRecorder.NET.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Lives",
                columns: table => new
                {
                    liveId = table.Column<string>(type: "TEXT", nullable: false),
                    uid = table.Column<int>(type: "INTEGER", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    streamName = table.Column<string>(type: "TEXT", nullable: false),
                    title = table.Column<string>(type: "TEXT", nullable: false),
                    startTime = table.Column<long>(type: "INTEGER", nullable: false),
                    url = table.Column<string>(type: "TEXT", nullable: true),
                    url_backup = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lives", x => x.liveId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lives_startTime",
                table: "Lives",
                column: "startTime");

            migrationBuilder.CreateIndex(
                name: "IX_Lives_uid",
                table: "Lives",
                column: "uid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Lives");
        }
    }
}
