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

El logo oficial del poste de barbero con terminales doradas (`logo.svg` / `icon-192.png`) ha sido integrado en formato vectorial nativo (`ic_logo.xml`) y rasterizado (`icon_app.png`), configurado también como icono del lanzador (`ic_launcher.xml`).

---

## 📱 Tecnologías y Arquitectura

- **Nombre de la App:** CerquilloApp
- **Lenguaje:** Kotlin 2.3.21 (Target JVM 17)
- **UI:** Jetpack Compose (Material Design 3)
- **Arquitectura:** MVVM (Model - View - ViewModel) + StateFlow
- **Navegación:** Jetpack Navigation Compose con barra inferior persistente
- **Conectividad:** Retrofit 2 + Gson + OkHttp Logging Interceptor
- **Asincronía:** Kotlin Coroutines
- **Sesión y caché local:** AES-GCM con claves protegidas por Android Keystore

---

## 💈 Funcionalidades

1. **Registro:** Registro local de cliente con nombre, teléfono y correo electrónico opcional.
2. **Inicio (Home):** Saludo personalizado, número de cliente, accesos rápidos y ajustes de cuenta.
3. **Catálogo:** Lista de servicios y productos con filtros, duración estimada y precios en RD$.
4. **Equipo de Barberos:** Especialistas con sus identificadores de color y estado activo.
5. **Reservar Cita:** Selección de servicios, fechas y horas consultadas a la disponibilidad real de cada barbero.
6. **Mis Citas:** Historial con estado y una vista expandible con horario, cantidades, precios, descuentos y notas.
7. **Acceso de barbero:** Inicio con las credenciales del personal, sesión guardada de forma cifrada, agenda propia por fecha y opciones para recorrer días.
8. **Persistencia local:** La última respuesta de citas y agendas queda cifrada para poder consultarla si no hay conexión.
9. **Ajustes:** Cierre de sesión y acceso al registro de otro cliente desde el perfil actual.

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

## Comprobar la integración

- Android guarda el `ClienteId` que devuelve `POST /api/Clientes`; en **Inicio** aparece como **Número de cliente** y se puede copiar. Es el identificador de cliente, no un `UsuarioId` ni una cuenta con contraseña.
- En Blazor abre **Mis Citas**, pega ese número en **Número de cliente**, escribe el teléfono del registro y pulsa **Buscar**. La página consulta `GET /api/Citas?clienteId=<id>`.
- La API solo muestra, crea o cancela citas de un cliente si recibe su teléfono en el encabezado `X-Cliente-Telefono`. Android lo guarda al registrarse y lo envía solo. Un perfil guardado sin teléfono (versiones anteriores) se descarta al abrir la app.
- **Ya soy cliente**: quien se registró en la web o en recepción entra con su nombre y teléfono (`POST /api/Clientes/recuperar`), sin crear un cliente repetido.
- En **Mis Citas** se pueden cancelar las citas pendientes o confirmadas que todavía no empiezan (`POST /api/Citas/{id}/cancelar`).
- Android y Blazor publicado usan por defecto la misma API de Azure. WinForms y Blazor en desarrollo apuntan a `https://localhost:7109/`; para probar Android contra esa API local, configura `bookinghubApiBaseUrl=http://<IP_DE_TU_PC>:5058/` en el `local.properties` de Android. Los tres clientes solo comparten citas si consultan el mismo servidor y la misma base de datos.
- En **Mis Citas** de Android también aparece el ID de cada cita, para poder contrastarlo con la agenda del personal en WinForms.

## Acceso de barberos y persistencia

- Desde Registro o Inicio, elige **Acceso para barberos** e inicia sesión con una cuenta existente cuyo rol sea `Barbero`. Android usa `POST /api/Auth/login`; las cuentas y contraseñas las administra el personal en el sistema existente.
- La API entrega un token JWT válido por ocho horas. Android cifra el token y el perfil con AES-GCM y Android Keystore. Al abrir la app, revisa la expiración; si ya venció, vuelve al flujo de acceso.
- El área **Mi agenda** consulta `GET /api/Citas/mias?fecha=AAAA-MM-DD`. La API identifica al usuario por el token y filtra las citas del barbero asociado; la app permite elegir el día y mostrar cliente, servicios, horario, estado, total y notas.
- En **Mis Citas** del cliente y en **Mi agenda** del barbero, cada tarjeta se puede expandir para revisar precios por servicio, cantidad, descuento y notas.
- Las respuestas se guardan cifradas en el dispositivo y separadas por `ClienteId` o por usuario de barbero y fecha. Si no hay conexión, Android presenta la última copia disponible e indica que no pudo actualizarla. El servidor sigue siendo la fuente oficial de las citas.
- En **Ajustes**, el barbero puede cerrar sesión y el cliente puede cerrar su perfil o registrar a otra persona. Al salir, Android elimina la sesión y la caché del perfil correspondiente.

## Límites del contrato actual de la API

- El backend actual crea clientes sin credenciales y permite consultar citas pasando un `clienteId`. La sesión cifrada del dispositivo conserva el perfil entre reinicios, pero **no autentica al cliente ante la API** ni protege sus citas contra acceso desde otro cliente. Se necesita autenticación/autorización de clientes en el backend antes de publicar la app con datos reales.
- La app consulta la disponibilidad y envía los detalles de servicio, pero el backend todavía debe volver a validar al guardar que el cliente sea dueño de la reserva, que cada servicio exista y esté activo, que sus precios vengan del catálogo y que el horario siga dentro de la jornada disponible.
- La API actual solo permite al personal eliminar citas. Por eso la app no muestra una acción de cancelación; indica al cliente que contacte a la barbería.
- Los barberos ven su agenda por día o por semana (`GET /api/Citas/mias?fecha=AAAA-MM-DD&dias=1..7`) y marcan sus citas como confirmada, en proceso, completada o "no vino" (`PUT /api/Citas/{id}/estado`).
- Los mensajes de error muestran el motivo que da la API (`description`), por ejemplo "Esa hora ya no está disponible" o el límite de intentos.
