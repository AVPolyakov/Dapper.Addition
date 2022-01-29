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

CREATE TYPE TVP_Customer AS TABLE
(
    [Code] [VARCHAR](20) NULL,
    [Name] [VARCHAR](20) NULL
);

CREATE TABLE Table5s (
    Id INT IDENTITY PRIMARY KEY,
    CreationDate DATETIME
);

CREATE TABLE Clients (
    Id INT PRIMARY KEY,
    Name NVARCHAR(MAX) NULL
);

INSERT INTO Clients (Id, Name)
VALUES (1, 'Client1');

CREATE TABLE Documents (
    Id INT IDENTITY PRIMARY KEY,
    ClientId INT NOT NULL,
    CreationDate DATETIME NOT NULL
);

INSERT INTO Documents (ClientId, CreationDate)
VALUES (1, '2021-02-15');
