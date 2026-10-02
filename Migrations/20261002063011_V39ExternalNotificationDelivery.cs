using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39ExternalNotificationDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                table: "NotificationDeliveryAttempts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "NotificationDeliveryAttempts",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderReference",
                table: "NotificationDeliveryAttempts",
                type: "nvarchar(240)",
                maxLength: 240,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponseDetail",
                table: "NotificationDeliveryAttempts",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttemptCount",
                table: "NotificationDeliveryAttempts");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "NotificationDeliveryAttempts");

            migrationBuilder.DropColumn(
                name: "ProviderReference",
                table: "NotificationDeliveryAttempts");

            migrationBuilder.DropColumn(
                name: "ResponseDetail",
                table: "NotificationDeliveryAttempts");

        }
    }
}
