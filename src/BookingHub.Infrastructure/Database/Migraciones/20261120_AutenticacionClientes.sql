/*
  Fase 1: autenticación de clientes y almacenamiento de tokens de un solo uso.
  Ejecutar una vez sobre BookingHubBarberia_BD_API antes de publicar la versión
  de la API que usa estos campos. El script es idempotente y no borra datos.
*/
USE BookingHubBarberia_BD_API;
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.Clientes', 'emailNormalizado') IS NULL
    ALTER TABLE dbo.Clientes ADD emailNormalizado NVARCHAR(255) NULL;

IF COL_LENGTH('dbo.Clientes', 'passwordHash') IS NULL
    ALTER TABLE dbo.Clientes ADD passwordHash VARCHAR(255) NULL;

UPDATE dbo.Clientes
SET emailNormalizado = LOWER(LTRIM(RTRIM(email)))
WHERE email IS NOT NULL AND emailNormalizado IS NULL;

IF EXISTS (
    SELECT emailNormalizado
    FROM dbo.Clientes
    WHERE emailNormalizado IS NOT NULL
    GROUP BY emailNormalizado
    HAVING COUNT(*) > 1
)
    THROW 51000, 'Hay correos de clientes duplicados ignorando mayusculas/espacios; corrígelos antes de migrar.', 1;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Clientes') AND name = 'UX_Clientes_EmailNormalizado')
    CREATE UNIQUE INDEX UX_Clientes_EmailNormalizado
        ON dbo.Clientes(emailNormalizado)
        WHERE emailNormalizado IS NOT NULL;

IF OBJECT_ID('dbo.AuthTokens', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuthTokens
    (
        authTokenId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuthTokens PRIMARY KEY,
        clienteId INT NULL,
        usuarioId INT NULL,
        tipo VARCHAR(20) NOT NULL,
        tokenHash VARCHAR(64) NOT NULL,
        fechaCreacion DATETIME2(0) NOT NULL,
        fechaExpiracion DATETIME2(0) NOT NULL,
        fechaRevocacion DATETIME2(0) NULL,
        reemplazadoPorHash VARCHAR(64) NULL,
        CONSTRAINT CK_AuthTokens_UnSoloPropietario CHECK
            ((clienteId IS NOT NULL AND usuarioId IS NULL) OR (clienteId IS NULL AND usuarioId IS NOT NULL)),
        CONSTRAINT CK_AuthTokens_Tipo CHECK (tipo IN ('Refresh', 'PasswordReset')),
        CONSTRAINT FK_AuthTokens_Clientes FOREIGN KEY (clienteId) REFERENCES dbo.Clientes(clienteId),
        CONSTRAINT FK_AuthTokens_Usuarios FOREIGN KEY (usuarioId) REFERENCES dbo.Usuarios(usuarioId)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.AuthTokens') AND name = 'UX_AuthTokens_TokenHash')
    CREATE UNIQUE INDEX UX_AuthTokens_TokenHash ON dbo.AuthTokens(tokenHash);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.AuthTokens') AND name = 'IX_AuthTokens_Cliente_Tipo')
    CREATE INDEX IX_AuthTokens_Cliente_Tipo ON dbo.AuthTokens(tipo, clienteId, fechaExpiracion);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.AuthTokens') AND name = 'IX_AuthTokens_Usuario_Tipo')
    CREATE INDEX IX_AuthTokens_Usuario_Tipo ON dbo.AuthTokens(tipo, usuarioId, fechaExpiracion);

COMMIT TRANSACTION;
GO
