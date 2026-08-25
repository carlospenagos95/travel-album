using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlbumViajes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhotosMusicAndGoogleAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "google_accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    ProtectedRefreshToken = table.Column<string>(type: "text", nullable: false),
                    ConnectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_google_accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "music_sources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    RadioStationUuid = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RadioStreamUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    YouTubeVideoId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_music_sources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_music_sources_cities_CityId",
                        column: x => x.CityId,
                        principalTable: "cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "photos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CityId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceMediaId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    StoredPath = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    ThumbnailPath = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: false),
                    TakenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Caption = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_photos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_photos_cities_CityId",
                        column: x => x.CityId,
                        principalTable: "cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_google_accounts_email",
                table: "google_accounts",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_music_sources_city",
                table: "music_sources",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "ix_photos_city_sort_order",
                table: "photos",
                columns: new[] { "CityId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "ix_photos_city_source_media",
                table: "photos",
                columns: new[] { "CityId", "SourceMediaId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "google_accounts");

            migrationBuilder.DropTable(
                name: "music_sources");

            migrationBuilder.DropTable(
                name: "photos");
        }
    }
}
