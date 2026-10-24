using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMS.Web.Api.Data.Migrations;

/// <inheritdoc />
public partial class _20261024030123_InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "MenuItems",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MenuItemSuperiorId = table.Column<Guid>(type: "uuid", nullable: true),
                Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Icon = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                URL = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                Page = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                Ordem = table.Column<int>(type: "integer", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                SystemIsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                SystemDateDeleted = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MenuItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_MenuItems_MenuItems_MenuItemSuperiorId",
                    column: x => x.MenuItemSuperiorId,
                    principalTable: "MenuItems",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "UserLoginAttempts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: true),
                Login = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Success = table.Column<bool>(type: "boolean", nullable: false),
                IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                SystemIsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                SystemDateDeleted = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserLoginAttempts", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Login = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                IsAdmin = table.Column<bool>(type: "boolean", nullable: false),
                MustChangePassword = table.Column<bool>(type: "boolean", nullable: false),
                FailedAttempts = table.Column<int>(type: "integer", nullable: false),
                LockedUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                TwoFactorSecret = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                SystemIsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                SystemDateDeleted = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "UserMenuPermissions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                MenuItemId = table.Column<Guid>(type: "uuid", nullable: false),
                CanView = table.Column<bool>(type: "boolean", nullable: false),
                CanCreate = table.Column<bool>(type: "boolean", nullable: false),
                CanEdit = table.Column<bool>(type: "boolean", nullable: false),
                CanDelete = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                SystemIsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                SystemDateDeleted = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserMenuPermissions", x => x.Id);
                table.ForeignKey(
                    name: "FK_UserMenuPermissions_MenuItems_MenuItemId",
                    column: x => x.MenuItemId,
                    principalTable: "MenuItems",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_UserMenuPermissions_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserSessions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                RefreshTokenHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                RefreshExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                LastActivityAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                IsRevoked = table.Column<bool>(type: "boolean", nullable: false),
                RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                RevokedReason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                SystemIsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                SystemDateDeleted = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserSessions", x => x.Id);
                table.ForeignKey(
                    name: "FK_UserSessions_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MenuItems_Code",
            table: "MenuItems",
            column: "Code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_MenuItems_MenuItemSuperiorId",
            table: "MenuItems",
            column: "MenuItemSuperiorId");

        migrationBuilder.CreateIndex(
            name: "IX_UserLoginAttempts_Login",
            table: "UserLoginAttempts",
            column: "Login");

        migrationBuilder.CreateIndex(
            name: "IX_UserMenuPermissions_MenuItemId",
            table: "UserMenuPermissions",
            column: "MenuItemId");

        migrationBuilder.CreateIndex(
            name: "IX_UserMenuPermissions_UserId_MenuItemId",
            table: "UserMenuPermissions",
            columns: new[] { "UserId", "MenuItemId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Users_Email",
            table: "Users",
            column: "Email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Users_Login",
            table: "Users",
            column: "Login",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_UserSessions_UserId",
            table: "UserSessions",
            column: "UserId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "UserLoginAttempts");

        migrationBuilder.DropTable(
            name: "UserMenuPermissions");

        migrationBuilder.DropTable(
            name: "UserSessions");

        migrationBuilder.DropTable(
            name: "MenuItems");

        migrationBuilder.DropTable(
            name: "Users");
    }
}
