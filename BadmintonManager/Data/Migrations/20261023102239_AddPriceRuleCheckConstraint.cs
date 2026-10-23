using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BadmintonManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceRuleCheckConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_PriceRule_Minutes",
                table: "PriceRules",
                sql: "[StartMinute] >= 0 AND [EndMinute] <= 1440 AND [EndMinute] > [StartMinute]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_PriceRule_Minutes",
                table: "PriceRules");
        }
    }
}
