using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace canteen_management.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GroupOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsOpen = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupOrders_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupOrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupOrderItems_GroupOrders_GroupOrderId",
                        column: x => x.GroupOrderId,
                        principalTable: "GroupOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupOrderItems_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroupOrderItems_Users_AddedByUserId",
                        column: x => x.AddedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GroupOrderItems_AddedByUserId",
                table: "GroupOrderItems",
                column: "AddedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupOrderItems_GroupOrderId",
                table: "GroupOrderItems",
                column: "GroupOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupOrderItems_MenuItemId",
                table: "GroupOrderItems",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupOrders_OwnerId",
                table: "GroupOrders",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupOrderItems");

            migrationBuilder.DropTable(
                name: "GroupOrders");
        }
    }
}
