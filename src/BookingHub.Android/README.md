# CerquilloApp - Cliente Móvil Android

Aplicación móvil nativa desarrollada en **Kotlin** y **Jetpack Compose** para **CerquilloApp** (sistema de reservas para barbería), conectada a la API backend de la solución BookingHub.

---

## 🎨 Identidad Visual y Paleta de Colores

La aplicación utiliza la misma paleta oficial definida en `app.css` del cliente web:

| Token CSS | Color Hex | Uso en la App |
|---|---|---|
| `--tinta` | `#17150F` | Texto principal, fondos oscuros y botones primarios |
| `--acento` | `#C9A15B` | Dorado característico (resaltados, badges, precios, acentos) |
| `--papel` | `#FAF8F3` | Fondo crema elegante de las pantallas |
| `--borde` | `#E6E3DC` | Bordes de tarjetas y divisores de secciones |
| `--texto-suave` | `#78736A` | Subtítulos, etiquetas y descripciones |

El poste de barbero con terminales doradas está integrado como vector nativo (`ic_logo.xml`) y como icono adaptativo a color y monocromático para Android 8+.

---

## 📱 Tecnologías y Arquitectura

- **Nombre de la App:** CerquilloApp
- **Lenguaje:** Kotlin 2.4.21 (Target JVM 17)
- **Build:** Android Gradle Plugin 9.5.1, Gradle 9.6, compileSdk 37 y targetSdk 36
- **UI:** Jetpack Compose (Material Design 3)
- **Arquitectura:** MVVM + StateFlow, repositorios y DI con Hilt
- **Navegación:** Jetpack Navigation Compose con barra inferior persistente
- **Conectividad:** Retrofit 3 + Gson + OkHttp 5 Logging Interceptor
- **Asincronía:** Kotlin Coroutines
- **Sesión y caché local:** AES-GCM con claves protegidas por Android Keystore
- **Recordatorios:** WorkManager con avisos de 24 y 1 hora antes

Las versiones de plugins y bibliotecas se mantienen en `gradle/libs.versions.toml`. Hilt crea el grafo de red, repositorios, sesión, caché y ViewModels. Los ViewModels consumen repositorios, no servicios Retrofit. Las respuestas de red se representan mediante `ApiResult`: éxito, error de conexión, error HTTP, sesión expirada o función aún no soportada por la API. Gson se conserva porque los DTOs actuales ya dependen de él y reemplazarlo no aporta una mejora necesaria en esta fase.

La API de producción sigue siendo Azure. Android programa recordatorios locales persistentes con WorkManager. Las notificaciones push FCM requieren configurar Firebase para esta app y el backend; el proyecto aún no incluye esas credenciales.

---

## 💈 Funcionalidades

1. **Onboarding y cuenta:** Presentación inicial de tres pasos y registro de cliente con email único y contraseña segura.
2. **Inicio (Home):** Saludo personalizado, número de cliente, accesos rápidos y ajustes de cuenta.
3. **Catálogo:** Búsqueda, filtro por servicio/producto, orden por nombre, precio o duración y precios en RD$.
4. **Equipo de Barberos:** Perfil, especialidades, promedio de reseñas, próximo horario disponible, estado activo y favoritos del cliente.
5. **Reservar Cita:** Selección guiada de servicios, barbero o cualquiera disponible, fechas y horas consultadas a la disponibilidad real.
6. **Mis Citas:** Próximas e historial, detalles, cancelación, reprogramación, repetición de una cita pasada, reseñas y acciones de calendario/compartir.
7. **Acceso unificado:** Un mismo formulario distingue cliente y barbero por el rol de la API. Incluye recuperación de contraseña y cambio de contraseña.
8. **Agenda de barbero:** Vistas de día y semana, cambio de estados permitido, llamada/WhatsApp al cliente y gestión de bloqueos.
9. **Persistencia local:** Las últimas respuestas de citas y agendas quedan cifradas; los recordatorios se guardan con WorkManager.
10. **Perfil y ayuda:** Edición de datos y foto, información de la barbería, privacidad, términos y ajustes de sesión.

---

## 🚀 Cómo abrir y ejecutar en Android Studio

1. Abre **Android Studio**.
2. Selecciona **File > Open** y elige:
   `c:\PROGRAMACION 4\ProyectoFinal_BookingHubBarberia_P4\src\BookingHub.Android`
3. Espera la sincronización de Gradle.
4. Las variantes **debug** y **release** usan por defecto la API HTTPS de Azure, accesible desde emuladores y dispositivos físicos. Para probar una API local, configura `bookinghubApiBaseUrl` en `local.properties` (archivo local ignorado por Git) o pásala como `-PbookinghubApiBaseUrl=http://<IP_DE_TU_PC>:5058/`. Desde un teléfono físico, la API local debe escuchar en la interfaz de red y el firewall debe permitir el puerto `5058`.
5. La URL de producción se puede sustituir con `bookinghubProductionApiBaseUrl`. El tráfico HTTP claro queda habilitado solo en debug.
6. Presiona **Run ▶ (Shift + F10)**.

Para compilar desde una terminal, ejecuta `gradlew.bat :app:assembleDebug` en Windows o `./gradlew :app:assembleDebug` en macOS/Linux. El APK se genera en `app/build/outputs/apk/debug/app-debug.apk`.

La compilación limita el paralelismo de Gradle para reducir el consumo de memoria en equipos con recursos acotados.

## Autenticación y preparación del backend

Antes de desplegar esta versión de la API, aplica en orden los scripts idempotentes `src/BookingHub.Infrastructure/Database/Migraciones/20261120_AutenticacionClientes.sql`, `20261120_Fase2_AgendaReservas.sql` y `20261120_Fase3_ExperienciaCliente.sql`. Respalda la base de datos antes de ejecutarlos en Azure.

La recuperación de contraseña necesita SMTP configurado en el App Service mediante secretos de aplicación, nunca en el repositorio:

- `Email__SmtpHost`, `Email__Port`, `Email__User`, `Email__Password`, `Email__From`
- `Email__EnableSsl` (por defecto queda activo)

Si falta SMTP, el endpoint de recuperación responde `503` para que la app informe que el servicio no está listo. Los códigos tienen 30 minutos de vigencia y la BD guarda su hash. Las contraseñas se almacenan con `PasswordHasher<Cliente>`.

Para fotos en Azure Blob, configura `AzureBlob__FotoPerfilSasUrl` como URL HTTPS de contenedor con SAS de creación, escritura y lectura. Sin ese valor, la API las guarda en SQL. Los datos de dirección, horario, teléfono y mapa se configuran con las claves `Barberia__...` descritas en [la guía de Fases 2 y 3](docs/fase-2-y-3.md).

Los JWT de clientes duran 15 minutos y los refresh tokens rotan, vencen en 30 días y se guardan hasheados en el servidor. Android cifra ambos tokens con Android Keystore. El cambio de contraseña revoca los refresh tokens activos y pide iniciar sesión de nuevo.

## Comprobar la integración

- Android guarda el `ClienteId` que devuelve `POST /api/Auth/registro-cliente`; en **Inicio** aparece como **Número de cliente** y se puede copiar. Es el identificador que relaciona las citas en la base compartida.
- Inicia sesión desde Android con una cuenta de cliente y comprueba **Mis Citas**. La app llama `GET /api/Citas/mias`; el backend toma el ID de los claims del JWT.
- Para reclamar un cliente legado creado desde Blazor/WinForms, usa **Olvidé mi contraseña** e ingresa el correo y teléfono ya registrados. El backend envía el código de restablecimiento al correo.
- Blazor no se modificó. Su página actual sigue consultando `GET /api/Citas?clienteId=<id>` por compatibilidad con el flujo existente.
- Android y Blazor publicado usan por defecto la misma API de Azure. WinForms y Blazor en desarrollo apuntan a `https://localhost:7109/`; para probar Android contra esa API local, configura `bookinghubApiBaseUrl=http://<IP_DE_TU_PC>:5058/` en el `local.properties` de Android. Los tres clientes solo comparten citas si consultan el mismo servidor y la misma base de datos.
- En **Mis Citas** de Android también aparece el ID de cada cita, para poder contrastarlo con la agenda del personal en WinForms.

Consulta las rutas, reglas, migraciones y configuración en [la guía de Fases 2 y 3](docs/fase-2-y-3.md).

## Acceso de barberos y persistencia

- Desde el registro, elige **Ya tengo una cuenta** para usar el inicio unificado. `POST /api/Auth/login` valida a clientes y personal, y Android decide el área por el rol recibido. Las cuentas de personal siguen administrándose desde el sistema existente.
- Los tokens se cifran con AES-GCM y Android Keystore. OkHttp intenta renovarlos una vez ante un `401`; si el refresh ya no es válido, borra la sesión y caché y vuelve al acceso.
- El área **Mi agenda** consulta `GET /api/Citas/mias?fecha=AAAA-MM-DD`. La API identifica al usuario por el token y filtra las citas del barbero asociado; la app permite elegir el día y mostrar cliente, servicios, horario, estado, total y notas.
- En **Mis Citas** del cliente y en **Mi agenda** del barbero, cada tarjeta se puede expandir para revisar precios por servicio, cantidad, descuento y notas.
- Las respuestas se guardan cifradas en el dispositivo y separadas por `ClienteId` o por usuario de barbero y fecha. Si no hay conexión, Android presenta la última copia disponible e indica que no pudo actualizarla. El servidor sigue siendo la fuente oficial de las citas.
- En **Ajustes**, el usuario puede cerrar sesión. El cliente también puede cambiar contraseña o pedir la eliminación de su cuenta; el backend conserva las citas históricas y anonimiza los datos personales asociados.

## Compatibilidad de la API con Blazor y WinForms

- El endpoint antiguo de consulta `GET /api/Citas?clienteId=<id>` sigue disponible para la página actual de Blazor, que no usa una sesión de cliente. Cuando un JWT de rol `Cliente` acompaña esa ruta, la API fuerza el ID del token y rechaza otro ID. Android ya usa la nueva ruta `/api/Citas/mias`. Para cerrar también el acceso anónimo legado se necesita adaptar Blazor o coordinar su migración, por lo que ese cierre queda pendiente para no romperlo.
- Para solicitudes autenticadas de Android, el backend valida propietario, servicios activos, precios del catálogo, horario, bloqueos, anticipación y choques al guardar.
- Cancelación y reprogramación del cliente están disponibles en `PUT /api/Citas/{id}/cancelar` y `/reprogramar`, sujetas a la política de anticipación.
- El barbero actualiza el estado solo de las citas de su propia agenda con `PUT /api/Citas/{id}/estado`; las transiciones se limitan en el servicio.

## Calidad, release y privacidad

- Los detalles de calidad de UI, caché offline, paginación, deep links, política local de privacidad y configuración de release están en [docs/fase-4-y-5.md](docs/fase-4-y-5.md).
- Las pruebas JVM, instrumentadas Compose, cobertura, resultados backend y checklist manual están en [docs/fase-6-pruebas.md](docs/fase-6-pruebas.md). Las pruebas Compose instrumentadas están compiladas pero necesitan ejecutarse en un emulador o dispositivo.
- La CI y release firmada por etiquetas `vMAJOR.MINOR.PATCH`, los secretos requeridos, la ficha de Play y los requisitos externos están en [docs/fase-7-entrega.md](docs/fase-7-entrega.md) y [docs/ficha-play-store.md](docs/ficha-play-store.md).
- `gradlew :app:assembleDebug` y `gradlew :app:assembleRelease -x lintVitalRelease -x lintVitalAnalyzeRelease` generan artefactos con R8. La release se firma solo cuando `ANDROID_KEYSTORE_PATH`, `ANDROID_KEYSTORE_PASSWORD`, `ANDROID_KEY_ALIAS` y `ANDROID_KEY_PASSWORD` se configuran de forma segura.
- Para producción, configura en Azure `Jwt__Clave` (secreto de al menos 32 bytes) y `Cors__OrigenesPermitidos` (dominios HTTPS autorizados). Ejecuta manualmente los scripts SQL documentados antes de desplegar la API.
- La API aún requiere configuración del proyecto/credenciales Firebase para FCM; hasta entonces, la app usa recordatorios locales de WorkManager.
