/* Fase 2: tipos de bloqueo que usa la agenda móvil del barbero. */
USE BookingHubBarberia_BD_API;
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;

INSERT INTO dbo.TiposIndisponibilidad (descripcion)
SELECT tipo.descripcion
FROM (VALUES (N'Descanso'), (N'Vacaciones'), (N'Bloqueo')) AS tipo(descripcion)
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.TiposIndisponibilidad actual
    WHERE LOWER(actual.descripcion) = LOWER(tipo.descripcion)
);

COMMIT TRANSACTION;
GO
