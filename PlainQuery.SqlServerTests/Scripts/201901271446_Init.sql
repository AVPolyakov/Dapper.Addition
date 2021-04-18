CREATE TABLE Posts (
  PostId INT IDENTITY PRIMARY KEY,
  Text NVARCHAR(MAX) NULL,
  CreationDate DATETIME NOT NULL,
);

INSERT INTO Posts (Text, CreationDate)
  VALUES ('Test1', '2021-01-14');
INSERT INTO Posts (Text, CreationDate)
  VALUES (NULL, '2021-02-15');

CREATE TABLE Table2s (
    Id INT IDENTITY PRIMARY KEY,
    Text NVARCHAR(MAX) NULL,
    ReadOnlyColumn1 AS 1
);

CREATE TABLE Comment2s (
    Id INT IDENTITY PRIMARY KEY,
    Text NVARCHAR(MAX) NULL
);

CREATE TABLE Table3s (
    Id INT PRIMARY KEY,
    Text NVARCHAR(MAX) NULL
);

CREATE TABLE Table4s (
    id INT PRIMARY KEY,
    first_name NVARCHAR(MAX) NULL
);

INSERT INTO Table4s (id, first_name)
VALUES (1, 'Test1');

CREATE TYPE TVP_Customer AS TABLE
(
    [Code] [VARCHAR](20) NULL,
    [Name] [VARCHAR](20) NULL
);

CREATE TABLE Table5s (
    Id INT IDENTITY PRIMARY KEY,
    CreationDate DATETIME
);

