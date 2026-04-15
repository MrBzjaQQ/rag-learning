using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RAG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixVectorColumnType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS vector;");
            migrationBuilder.Sql("ALTER TABLE \"Embeddings\" ALTER COLUMN vector TYPE vector USING vector::vector");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Embeddings_vector",
                table: "Embeddings");

            migrationBuilder.AlterColumn<string>(
                name: "vector",
                table: "Embeddings",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "vector");
        }
    }
}
