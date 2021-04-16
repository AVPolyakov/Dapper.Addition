CREATE TABLE Post (
  PostId INT IDENTITY PRIMARY KEY,
  Text NVARCHAR(MAX) NULL,
  CreationDate DATETIME NOT NULL,
);

INSERT INTO Post (Text, CreationDate)
  VALUES ('Test1', '2021-01-14');
INSERT INTO Post (Text, CreationDate)
  VALUES (NULL, '2021-02-15');

CREATE TABLE Table2 (
    Id INT IDENTITY PRIMARY KEY,
    Text NVARCHAR(MAX) NULL,
    ComputedColumn1 AS 1
);

CREATE TABLE Comments (
    Id INT IDENTITY PRIMARY KEY,
    Text NVARCHAR(MAX) NULL
);

CREATE TABLE Table3 (
    Id INT PRIMARY KEY,
    Text NVARCHAR(MAX) NULL
);

CREATE TABLE Table4 (
    id INT PRIMARY KEY,
    first_name NVARCHAR(MAX) NULL
);

INSERT INTO Table4 (id, first_name)
VALUES (1, 'Test1');

CREATE TYPE TVP_Customer AS TABLE
(
    [Code] [VARCHAR](20) NULL,
    [Name] [VARCHAR](20) NULL
);
