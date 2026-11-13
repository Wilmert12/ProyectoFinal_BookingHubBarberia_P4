USE BookingHubBarberia_BD_API;
GO
SET NOCOUNT ON;

INSERT INTO Roles (descripcion)
SELECT v.d FROM (VALUES ('Admin'), ('Recepcion'), ('Barbero'), ('Cliente')) v(d)
WHERE NOT EXISTS (SELECT 1 FROM Roles r WHERE r.descripcion = v.d);

INSERT INTO EstadosCita (descripcion, liberaHorario)
SELECT v.d, v.l FROM (VALUES
    ('Pendiente',  0),
    ('Confirmada', 0),
    ('EnProceso',  0),
    ('Completada', 0),
    ('Cancelada',  1),
    ('NoShow',     1)) v(d, l)
WHERE NOT EXISTS (SELECT 1 FROM EstadosCita e WHERE e.descripcion = v.d);

DECLARE @rolAdmin   INT = (SELECT rolId FROM Roles WHERE descripcion = 'Admin');
DECLARE @rolBarbero INT = (SELECT rolId FROM Roles WHERE descripcion = 'Barbero');

INSERT INTO Usuarios (nombre, email, passwordHash, telefono, rolId)
SELECT v.n, v.e, 'HASH_DE_PRUEBA', v.t, v.r FROM (VALUES
    ('Administrador', 'admin@bookinghub.com',  '8090000000', @rolAdmin),
    ('Carlos Pérez',  'carlos@bookinghub.com', '8090000001', @rolBarbero),
    ('Luis Gómez',    'luis@bookinghub.com',   '8090000002', @rolBarbero)) v(n, e, t, r)
WHERE NOT EXISTS (SELECT 1 FROM Usuarios u WHERE u.email = v.e);

INSERT INTO Barberos (usuarioId, apodo, cedula, colorHex)
SELECT u.usuarioId, v.apodo, v.cedula, v.color
FROM (VALUES
    ('carlos@bookinghub.com', 'Carlitos', '40212837459', '#1E88E5'),
    ('luis@bookinghub.com',   'Luis',  '00100000002', '#43A047')) v(email, apodo, cedula, color)
JOIN Usuarios u ON u.email = v.email
WHERE NOT EXISTS (SELECT 1 FROM Barberos b WHERE b.usuarioId = u.usuarioId);

INSERT INTO Clientes (nombre, telefono, email)
SELECT v.n, v.t, v.e FROM (VALUES
    ('Cliente Demo', '8091234567', 'cliente@correo.com'),
    ('Juan Walk-in', '8097654321', NULL)) v(n, t, e)
WHERE NOT EXISTS (SELECT 1 FROM Clientes c WHERE c.telefono = v.t);

SELECT 'Cliente' AS tipo, clienteId AS id, nombre AS nombre FROM Clientes
UNION ALL
SELECT 'Barbero', b.barberoId, u.nombre FROM Barberos b JOIN Usuarios u ON u.usuarioId = b.usuarioId
UNION ALL
SELECT 'Estado', estadoCitaId, descripcion FROM EstadosCita;
GO
