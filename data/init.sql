SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
GO

IF DB_ID('ExpenseDb') IS NULL
BEGIN
    CREATE DATABASE ExpenseDb;
END
GO

USE ExpenseDb;
GO

IF OBJECT_ID('dbo.Categories','U') IS NULL
BEGIN
    CREATE TABLE dbo.Categories(
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Categories)
BEGIN
    INSERT INTO dbo.Categories (Name)
    VALUES (N'Food'), (N'Transport'), (N'Bills'), (N'Other');
END
GO

IF OBJECT_ID('dbo.Expenses','U') IS NULL
BEGIN
    CREATE TABLE dbo.Expenses(
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Amount DECIMAL(18,2) NOT NULL,
        Date DATETIME2 NOT NULL,
        Note NVARCHAR(MAX) NULL,
        CategoryId INT NOT NULL,
        CONSTRAINT FK_Expenses_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(Id)
    );
END
GO

IF OBJECT_ID('dbo.SchemaMigrations','U') IS NULL
BEGIN
    CREATE TABLE dbo.SchemaMigrations(
        MigrationId NVARCHAR(100) NOT NULL CONSTRAINT PK_SchemaMigrations PRIMARY KEY,
        AppliedAt DATETIME2 NOT NULL CONSTRAINT DF_SchemaMigrations_AppliedAt DEFAULT SYSUTCDATETIME()
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM dbo.SchemaMigrations
    WHERE MigrationId = N'2026-09-data-integrity'
)
BEGIN
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        IF EXISTS (
            SELECT 1
            FROM dbo.Categories
            WHERE LEN(LTRIM(RTRIM(Name))) = 0
               OR DATALENGTH(Name) <> DATALENGTH(LTRIM(RTRIM(Name)))
        )
        BEGIN
            THROW 51000, 'Category-name migration blocked: remove blank or space-padded category names before retrying setup.', 1;
        END;

        IF EXISTS (
            SELECT UPPER(LTRIM(RTRIM(Name))) COLLATE Latin1_General_100_CI_AS
            FROM dbo.Categories
            GROUP BY UPPER(LTRIM(RTRIM(Name))) COLLATE Latin1_General_100_CI_AS
            HAVING COUNT(*) > 1
        )
        BEGIN
            THROW 51001, 'Category-name migration blocked: resolve duplicate category names ignoring case before retrying setup.', 1;
        END;

        IF EXISTS (SELECT 1 FROM dbo.Expenses WHERE Amount <= 0)
        BEGIN
            THROW 51002, 'Data-integrity migration blocked: resolve non-positive expense amounts before retrying setup.', 1;
        END;

        IF COL_LENGTH('dbo.Categories', 'NameKey') IS NULL
        BEGIN
            ALTER TABLE dbo.Categories
            ADD NameKey AS (UPPER(LTRIM(RTRIM(Name))) COLLATE Latin1_General_100_CI_AS) PERSISTED;
        END;

        IF NOT EXISTS (
            SELECT 1
            FROM sys.check_constraints
            WHERE parent_object_id = OBJECT_ID('dbo.Categories')
              AND name = 'CK_Categories_Name_Valid'
        )
        BEGIN
            ALTER TABLE dbo.Categories
            ADD CONSTRAINT CK_Categories_Name_Valid
            CHECK (
                LEN(LTRIM(RTRIM(Name))) > 0
                AND DATALENGTH(Name) = DATALENGTH(LTRIM(RTRIM(Name)))
            );
        END;

        IF NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('dbo.Categories')
              AND name = 'UX_Categories_NameKey'
        )
        BEGIN
            CREATE UNIQUE INDEX UX_Categories_NameKey ON dbo.Categories(NameKey);
        END;

        IF NOT EXISTS (
            SELECT 1
            FROM sys.check_constraints
            WHERE parent_object_id = OBJECT_ID('dbo.Expenses')
              AND name = 'CK_Expenses_Amount_Positive'
        )
        BEGIN
            ALTER TABLE dbo.Expenses
            ADD CONSTRAINT CK_Expenses_Amount_Positive CHECK (Amount > 0);
        END;

        INSERT INTO dbo.SchemaMigrations (MigrationId)
        VALUES (N'2026-09-data-integrity');

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END
GO
