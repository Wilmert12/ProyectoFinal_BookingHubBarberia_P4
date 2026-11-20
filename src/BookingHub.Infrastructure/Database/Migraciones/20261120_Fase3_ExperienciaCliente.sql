/* Fase 3: perfil, favoritos, catálogo clasificado y reseñas verificadas. */
USE BookingHubBarberia_BD_API;
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.Clientes', 'fotoPerfil') IS NULL
    ALTER TABLE dbo.Clientes ADD fotoPerfil VARBINARY(MAX) NULL;

IF COL_LENGTH('dbo.CatalogoItems', 'categoria') IS NULL
    ALTER TABLE dbo.CatalogoItems ADD categoria NVARCHAR(60) NULL;

IF COL_LENGTH('dbo.Barberos', 'especialidades') IS NULL
    ALTER TABLE dbo.Barberos ADD especialidades NVARCHAR(500) NULL;

IF OBJECT_ID('dbo.FavoritosBarberos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.FavoritosBarberos
    (
        clienteId INT NOT NULL,
        barberoId INT NOT NULL,
        fechaRegistro DATETIME2(0) NOT NULL CONSTRAINT DF_FavoritosBarberos_Fecha DEFAULT (SYSDATETIME()),
        CONSTRAINT PK_FavoritosBarberos PRIMARY KEY (clienteId, barberoId),
        CONSTRAINT FK_FavoritosBarberos_Clientes FOREIGN KEY (clienteId) REFERENCES dbo.Clientes(clienteId),
        CONSTRAINT FK_FavoritosBarberos_Barberos FOREIGN KEY (barberoId) REFERENCES dbo.Barberos(barberoId)
    );
END;

IF OBJECT_ID('dbo.Resenas', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Resenas
    (
        resenaId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Resenas PRIMARY KEY,
        citaId INT NOT NULL,
        clienteId INT NOT NULL,
        barberoId INT NOT NULL,
        puntaje TINYINT NOT NULL,
        comentario NVARCHAR(500) NULL,
        fechaRegistro DATETIME2(0) NOT NULL CONSTRAINT DF_Resenas_Fecha DEFAULT (SYSDATETIME()),
        CONSTRAINT CK_Resenas_Puntaje CHECK (puntaje BETWEEN 1 AND 5),
        CONSTRAINT FK_Resenas_Citas FOREIGN KEY (citaId) REFERENCES dbo.Citas(citaId),
        CONSTRAINT FK_Resenas_Clientes FOREIGN KEY (clienteId) REFERENCES dbo.Clientes(clienteId),
        CONSTRAINT FK_Resenas_Barberos FOREIGN KEY (barberoId) REFERENCES dbo.Barberos(barberoId)
    );
    CREATE UNIQUE INDEX UX_Resenas_Cita ON dbo.Resenas(citaId);
    CREATE INDEX IX_Resenas_Barbero_Fecha ON dbo.Resenas(barberoId, fechaRegistro);
END;

COMMIT TRANSACTION;
GO
