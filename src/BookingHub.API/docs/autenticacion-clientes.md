# Autenticación de clientes

## Despliegue de esquema

Antes de desplegar la API con esta funcionalidad, respalda la base y ejecuta una vez `src/BookingHub.Infrastructure/Database/Migraciones/20261120_AutenticacionClientes.sql` en `BookingHubBarberia_BD_API`. Es idempotente, agrega credenciales opcionales a clientes existentes y crea `AuthTokens`. Los clientes legados conservan su ficha y pueden reclamar la cuenta mediante restablecimiento de correo y teléfono.

La configuración EF del proyecto refleja el esquema actualizado. La API no crea el esquema automáticamente, porque la BD se mantiene con scripts y no tiene migraciones EF aplicadas por el pipeline.

## Rutas añadidas

| Método y ruta | Uso | Acceso |
|---|---|---|
| `POST /api/Auth/registro-cliente` | Crea cuenta, contraseña hasheada y sesión | Público, limitado por IP |
| `POST /api/Auth/login` | Autentica cliente o personal por email y contraseña | Público, limitado por IP |
| `POST /api/Auth/refresh` | Rota refresh token y emite un JWT nuevo | Público, limitado por IP |
| `POST /api/Auth/olvide-contrasena` | Envía código de un solo uso después de validar email y teléfono | Público, limitado por IP |
| `POST /api/Auth/restablecer-contrasena` | Cambia contraseña con código de 30 minutos | Público, limitado por IP |
| `POST /api/Auth/cambiar-contrasena` | Cambia contraseña de la sesión del cliente | Rol `Cliente` |
| `DELETE /api/Clientes/me` | Anonimiza al titular y conserva sus citas | Rol `Cliente` |
| `GET /api/Citas/mias` | Lista citas del cliente o agenda del barbero autenticado | Rol `Cliente` o `Barbero` |

Las contraseñas usan `PasswordHasher<Cliente>`. Los refresh y los códigos de recuperación son valores aleatorios; SQL almacena su SHA-256, fecha de expiración, revocación y hash sustituto. El JWT cliente vive 15 minutos y el refresh hasta 30 días. La contraseña nueva requiere al menos ocho caracteres, mayúscula, minúscula y número. El teléfono debe pertenecer a los códigos móviles dominicanos 809, 829 o 849.

## Correo de recuperación

Configura los siguientes valores como configuración protegida del App Service, no en `appsettings.json` ni en Git:

- `Email__SmtpHost`
- `Email__Port` (opcional; valor por defecto `587`)
- `Email__User` y `Email__Password` (credenciales SMTP)
- `Email__From`
- `Email__EnableSsl` (valor por defecto `true`)

La respuesta de solicitud no confirma si una cuenta existe. Si SMTP no está configurado, la API responde `503` antes de aceptar la solicitud. Si SMTP falla, el código recién creado se revoca y se registra el error técnico sin incluir el código en logs.

## Contratos existentes

El JSON de sesión de personal mantiene los campos previos (`token`, `expira`, `usuarioId`, `nombre`, `email`, `rol`) y añade `refreshToken`. Los deserializadores de Blazor y WinForms ignoran el campo adicional, y sus tokens conservan el plazo actual de ocho horas. Las rutas existentes de clientes y citas siguen publicadas.

`GET /api/Citas?clienteId=<id>` permanece disponible para el Blazor actual, que no autentica a los clientes. Los JWT con rol `Cliente` quedan restringidos al `sub` propio, pero la ruta anónima todavía permite la consulta legado por ID. Retirarla requiere migrar Blazor a una sesión de cliente; mantenerla es una excepción temporal de compatibilidad y deja expuestos los datos básicos de las citas a quien conozca o enumere IDs.

Las cuentas existentes creadas desde Blazor/WinForms no reciben contraseña automáticamente. La recuperación verifica correo y teléfono y entrega al email un código para reclamar la cuenta.
