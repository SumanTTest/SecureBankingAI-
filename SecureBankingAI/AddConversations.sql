BEGIN TRANSACTION;
CREATE TABLE "Conversations" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Conversations" PRIMARY KEY,
    "UserId" TEXT NOT NULL,
    "Title" TEXT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    "UpdatedAtUtc" TEXT NOT NULL
);

CREATE TABLE "ConversationMessages" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_ConversationMessages" PRIMARY KEY,
    "ConversationId" TEXT NOT NULL,
    "Role" TEXT NOT NULL,
    "Content" TEXT NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "FK_ConversationMessages_Conversations_ConversationId" FOREIGN KEY ("ConversationId") REFERENCES "Conversations" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_ConversationMessages_ConversationId_CreatedAtUtc" ON "ConversationMessages" ("ConversationId", "CreatedAtUtc");

CREATE INDEX "IX_Conversations_UserId_UpdatedAtUtc" ON "Conversations" ("UserId", "UpdatedAtUtc");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261127014116_AddConversations', '10.0.13');

COMMIT;

