using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlbumViajes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceYouTubeWithTracks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // YouTube dejo de permitir la reproduccion automatica en el embed, asi
            // que sus fuentes ya no se pueden reproducir: se borran antes de tocar
            // las columnas para no dejar filas mudas con un Kind que ya no existe.
            // La ciudad que se quede sin la que sonaba no queda huerfana: el
            // agregado marca otra como predeterminada la proxima vez que se toca.
            migrationBuilder.Sql("DELETE FROM music_sources WHERE \"Kind\" = 'YouTube';");

            migrationBuilder.RenameColumn(
                name: "YouTubeVideoId",
                table: "music_sources",
                newName: "TrackId");

            migrationBuilder.AddColumn<string>(
                name: "TrackArtist",
                table: "music_sources",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrackAudioUrl",
                table: "music_sources",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrackArtist",
                table: "music_sources");

            migrationBuilder.DropColumn(
                name: "TrackAudioUrl",
                table: "music_sources");

            migrationBuilder.RenameColumn(
                name: "TrackId",
                table: "music_sources",
                newName: "YouTubeVideoId");
        }
    }
}
