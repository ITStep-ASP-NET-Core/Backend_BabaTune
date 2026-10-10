using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BabaTune.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class typedNotices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notices_Users_RecipientId",
                table: "Notices");

            migrationBuilder.DropForeignKey(
                name: "FK_Notices_Users_SenderId",
                table: "Notices");

            migrationBuilder.DropIndex(
                name: "IX_Notices_RecipientId",
                table: "Notices");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Notices");

            migrationBuilder.AddColumn<Guid>(
                name: "AlbumId",
                table: "Notices",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AuthorId",
                table: "Notices",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ChatId",
                table: "Notices",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FriendShipId",
                table: "Notices",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "NewAlbumNotice_AuthorId",
                table: "Notices",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "Notices",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RoomId",
                table: "Notices",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServerNotice_Text",
                table: "Notices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SongId",
                table: "Notices",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubscribeId",
                table: "Notices",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notices_AlbumId",
                table: "Notices",
                column: "AlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_Notices_AuthorId",
                table: "Notices",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Notices_ChatId",
                table: "Notices",
                column: "ChatId");

            migrationBuilder.CreateIndex(
                name: "IX_Notices_FriendShipId",
                table: "Notices",
                column: "FriendShipId",
                unique: true,
                filter: "[FriendShipId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Notices_NewAlbumNotice_AuthorId",
                table: "Notices",
                column: "NewAlbumNotice_AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Notices_OwnerId",
                table: "Notices",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Notices_RecipientId_IsRead_CreatedAt",
                table: "Notices",
                columns: new[] { "RecipientId", "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notices_RoomId",
                table: "Notices",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Notices_SongId",
                table: "Notices",
                column: "SongId");

            migrationBuilder.CreateIndex(
                name: "IX_Notices_SubscribeId",
                table: "Notices",
                column: "SubscribeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notices_Albums_AlbumId",
                table: "Notices",
                column: "AlbumId",
                principalTable: "Albums",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notices_Chat_ChatId",
                table: "Notices",
                column: "ChatId",
                principalTable: "Chat",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Notices_Friendship_FriendShipId",
                table: "Notices",
                column: "FriendShipId",
                principalTable: "Friendship",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Notices_Room_RoomId",
                table: "Notices",
                column: "RoomId",
                principalTable: "Room",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notices_Songs_SongId",
                table: "Notices",
                column: "SongId",
                principalTable: "Songs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notices_Subscribes_SubscribeId",
                table: "Notices",
                column: "SubscribeId",
                principalTable: "Subscribes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Notices_Users_AuthorId",
                table: "Notices",
                column: "AuthorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notices_Users_NewAlbumNotice_AuthorId",
                table: "Notices",
                column: "NewAlbumNotice_AuthorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notices_Users_OwnerId",
                table: "Notices",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notices_Users_RecipientId",
                table: "Notices",
                column: "RecipientId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Notices_Users_SenderId",
                table: "Notices",
                column: "SenderId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notices_Albums_AlbumId",
                table: "Notices");

            migrationBuilder.DropForeignKey(
                name: "FK_Notices_Chat_ChatId",
                table: "Notices");

            migrationBuilder.DropForeignKey(
                name: "FK_Notices_Friendship_FriendShipId",
                table: "Notices");

            migrationBuilder.DropForeignKey(
                name: "FK_Notices_Room_RoomId",
                table: "Notices");

            migrationBuilder.DropForeignKey(
                name: "FK_Notices_Songs_SongId",
                table: "Notices");

            migrationBuilder.DropForeignKey(
                name: "FK_Notices_Subscribes_SubscribeId",
                table: "Notices");

            migrationBuilder.DropForeignKey(
                name: "FK_Notices_Users_AuthorId",
                table: "Notices");

            migrationBuilder.DropForeignKey(
                name: "FK_Notices_Users_NewAlbumNotice_AuthorId",
                table: "Notices");

            migrationBuilder.DropForeignKey(
                name: "FK_Notices_Users_OwnerId",
                table: "Notices");

            migrationBuilder.DropForeignKey(
                name: "FK_Notices_Users_RecipientId",
                table: "Notices");

            migrationBuilder.DropForeignKey(
                name: "FK_Notices_Users_SenderId",
                table: "Notices");

            migrationBuilder.DropIndex(
                name: "IX_Notices_AlbumId",
                table: "Notices");

            migrationBuilder.DropIndex(
                name: "IX_Notices_AuthorId",
                table: "Notices");

            migrationBuilder.DropIndex(
                name: "IX_Notices_ChatId",
                table: "Notices");

            migrationBuilder.DropIndex(
                name: "IX_Notices_FriendShipId",
                table: "Notices");

            migrationBuilder.DropIndex(
                name: "IX_Notices_NewAlbumNotice_AuthorId",
                table: "Notices");

            migrationBuilder.DropIndex(
                name: "IX_Notices_OwnerId",
                table: "Notices");

            migrationBuilder.DropIndex(
                name: "IX_Notices_RecipientId_IsRead_CreatedAt",
                table: "Notices");

            migrationBuilder.DropIndex(
                name: "IX_Notices_RoomId",
                table: "Notices");

            migrationBuilder.DropIndex(
                name: "IX_Notices_SongId",
                table: "Notices");

            migrationBuilder.DropIndex(
                name: "IX_Notices_SubscribeId",
                table: "Notices");

            migrationBuilder.DropColumn(
                name: "AlbumId",
                table: "Notices");

            migrationBuilder.DropColumn(
                name: "AuthorId",
                table: "Notices");

            migrationBuilder.DropColumn(
                name: "ChatId",
                table: "Notices");

            migrationBuilder.DropColumn(
                name: "FriendShipId",
                table: "Notices");

            migrationBuilder.DropColumn(
                name: "NewAlbumNotice_AuthorId",
                table: "Notices");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Notices");

            migrationBuilder.DropColumn(
                name: "RoomId",
                table: "Notices");

            migrationBuilder.DropColumn(
                name: "ServerNotice_Text",
                table: "Notices");

            migrationBuilder.DropColumn(
                name: "SongId",
                table: "Notices");

            migrationBuilder.DropColumn(
                name: "SubscribeId",
                table: "Notices");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Notices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Notices_RecipientId",
                table: "Notices",
                column: "RecipientId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notices_Users_RecipientId",
                table: "Notices",
                column: "RecipientId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notices_Users_SenderId",
                table: "Notices",
                column: "SenderId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
