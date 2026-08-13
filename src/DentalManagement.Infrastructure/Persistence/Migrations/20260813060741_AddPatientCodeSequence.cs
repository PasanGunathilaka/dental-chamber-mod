using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientCodeSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "patient_code_seq");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSequence(
                name: "patient_code_seq");
        }
    }
}
