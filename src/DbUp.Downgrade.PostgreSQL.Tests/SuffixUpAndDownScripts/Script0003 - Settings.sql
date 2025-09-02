CREATE TABLE "Setting" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(50) NOT NULL,
    "Description" TEXT NOT NULL,
    "DisplayName" VARCHAR(200) NOT NULL,
    "Value" TEXT NOT NULL
);

-- Insert initial settings
INSERT INTO "Setting" ("Name", "DisplayName", "Value", "Description") 
VALUES ('ui-title', 'Title', 'My FunnelWeb Site', 'Text: The title shown at the top in the browser.');

INSERT INTO "Setting" ("Name", "DisplayName", "Value", "Description") 
VALUES ('ui-introduction', 'Introduction', 'Welcome to your FunnelWeb blog. You can <a href="/login">login</a> and edit this message in the administration section. The default username and password is <code>test/test</code>.', 'Markdown: The introductory text that is shown on the home page.');

INSERT INTO "Setting" ("Name", "DisplayName", "Value", "Description") 
VALUES ('ui-links', 'Main Links', '<li><a href="/projects">Projects</a></li>', 'HTML: A list of links shown at the top of each page.');

INSERT INTO "Setting" ("Name", "DisplayName", "Value", "Description") 
VALUES ('search-author', 'Author', 'Daffy Duck', 'Text: Your name.');

INSERT INTO "Setting" ("Name", "DisplayName", "Value", "Description") 
VALUES ('search-keywords', 'Keywords', '.net, c#, test', 'Comma-separated text: Keywords shown to search engines.');

INSERT INTO "Setting" ("Name", "DisplayName", "Value", "Description") 
VALUES ('search-description', 'Description', 'My website.', 'Text: The description shown to search engines in the meta description tag.');
