-- Comisión de cada barbero (porcentaje de lo cobrado sin ITBIS que le toca).
-- Correr UNA vez en cada base (local y Azure) ANTES de publicar la versión de la API que la usa.
-- Se puede repetir sin problema: si la columna ya existe, no hace nada.
-- En el Query editor de Azure no se usan USE ni GO: pega solo este bloque.
-- Va con EXEC porque SQL Server revisa todo el bloque antes de correrlo y la columna aún no existe.

IF COL_LENGTH('Barberos', 'comisionPorcentaje') IS NULL
    EXEC('ALTER TABLE Barberos
              ADD comisionPorcentaje DECIMAL(5, 2) NOT NULL
                  CONSTRAINT DF_Barberos_comisionPorcentaje DEFAULT (50)
                  CONSTRAINT CK_Barberos_comisionPorcentaje CHECK (comisionPorcentaje BETWEEN 0 AND 100);');

EXEC('SELECT b.barberoId, u.nombre, b.comisionPorcentaje
      FROM Barberos b
      JOIN Usuarios u ON u.usuarioId = b.usuarioId;');
