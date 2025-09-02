CREATE TABLE "Entry" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(50) NOT NULL,
    "Title" VARCHAR(200) NOT NULL,
    "Summary" TEXT NOT NULL,
    "IsVisible" BOOLEAN NOT NULL,
    "Published" TIMESTAMP NOT NULL,
    "LatestRevisionId" INT NULL
);

CREATE TABLE "Feed" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL,
    "Title" VARCHAR(255) NOT NULL
);
