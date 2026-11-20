# Auditoría de CerquilloApp — Fase 0

Fecha: 20 de noviembre de 2026. Alcance: `src/BookingHub.Android` y lectura de los controladores, servicios, DTOs, configuración de seguridad y pruebas de la API para documentar compatibilidad. No se modificó la API, Blazor ni WinForms.

## Hallazgos de integración existentes

- Android registra clientes mediante `POST /api/Clientes`, sin contraseña ni autenticación de cliente. El identificador retornado es `ClienteId`, no `UsuarioId`.
- El acceso de personal usa `POST /api/Auth/login`; `GET /api/Auth/yo` y las agendas `/api/Citas/mias` usan JWT. La API no tiene refresh token ni autenticación de clientes.
- La agenda del cliente se pide con `GET /api/Citas?clienteId=...`, ruta que acepta un identificador de cliente sin comprobar que quien llama sea ese cliente.
- `POST /api/Citas` no exige identidad de cliente. El servicio valida campos y solapamientos, pero no vuelve a calcular precios desde el catálogo ni protege la inserción con concurrencia transaccional.
- La edición completa de citas requiere rol de personal. No hay endpoint de estado limitado a citas propias del barbero, ni operaciones de cancelación/reprogramación para clientes.
- La disponibilidad consulta jornada, citas y registros de indisponibilidad existentes; no se encontraron endpoints de gestión de bloqueos para el barbero.
- La API configura JWT, HTTPS redirection y CORS con orígenes configurados. No se encontró rate limiting de autenticación ni flujo de refresh token. La clave JWT puede caer en una clave aleatoria temporal cuando falta configuración.
- Hay pruebas unitarias .NET en `tests/BookingHub.Application.UnitTests` y un workflow general .NET. No hay fuentes de pruebas Android (`app/src/test` o `app/src/androidTest`) ni workflow de Android.

Referencias revisadas: [AuthController](../../BookingHub.API/Controllers/AuthController.cs), [ClientesController](../../BookingHub.API/Controllers/ClientesController.cs), [CitasController](../../BookingHub.API/Controllers/CitasController.cs), [BarberosController](../../BookingHub.API/Controllers/BarberosController.cs), `src/BookingHub.API/Program.cs`, servicios `UsuarioService`, `ClienteService`, `CitaService`, `BarberoService`, `DisponibilidadService` y DTOs de esos casos.

## Matriz de auditoría

`Existe`: se encontró implementado. `Parcial`: existe una parte, con brechas descritas. `Falta`: no se encontró implementado.

### Base Android actual

| Requisito | Estado | Evidencia |
|---|---|---|
| Aplicación Kotlin con Compose, Material 3, MVVM y navegación | Existe | `app/src/main`, pantallas Compose, ViewModels y Navigation Compose. |
| Registro básico de cliente y persistencia local del perfil | Existe | `RegistroScreen`, `ClienteViewModel`, `SesionCliente`; la API no autentica al cliente. |
| Catálogo, listado de barberos y reserva con disponibilidad | Existe | `CatalogoScreen`, `BarberosScreen`, `ReservarScreen` y sus ViewModels. |
| Historial de citas con detalle expandible | Existe | `MisCitasScreen` y DTOs con líneas, importes y estado. |
| Acceso JWT de barbero y agenda diaria | Existe | `AccesoBarberoScreen`, `AgendaBarberoScreen`, cifrado de sesión y endpoint `/api/Citas/mias`. |
| Caché cifrada de las citas del cliente y agenda del barbero | Existe | `CitasCache` y `CifradoLocal`; usa la última respuesta guardada. |

### Fase 0 — Auditoría y base técnica

| Requisito | Estado | Evidencia |
|---|---|---|
| Auditar Android y el contrato real de la API antes de cambios funcionales | Existe | Se revisaron pantallas, ViewModels, API clients, DTOs, controladores, servicios y workflows. |
| compileSdk para dependencias Android actuales | Existe | `compileSdk = 37` (Android 17 Preview); CI instala la plataforma desde el canal Canary. `targetSdk = 36` sigue el requisito vigente de Play. |
| targetSdk actualizado al requisito vigente de Google Play | Existe | `targetSdk = 36`. |
| Actualizar Compose BOM, Navigation, Lifecycle, Retrofit, OkHttp y Coroutines | Existe | Versiones centralizadas en `gradle/libs.versions.toml`. |
| Migrar plugins y dependencias al version catalog | Existe | `gradle/libs.versions.toml`, build raíz y build de `app`. |
| Incorporar Hilt a Application, actividad, red, repositorios, sesión, caché y ViewModels | Existe | `BookingHubApp`, `MainActivity`, módulos `di`, `ApiClient` y ViewModels. |
| Crear repositorios Auth, Cliente, Barbero, Catálogo, Cita y Notificación | Existe | Clases en `data/repository`; notificaciones quedan explícitamente no soportadas por la API actual. |
| Resultado tipado para éxito, red, HTTP, sesión expirada y funciones no disponibles | Existe | `ApiResult.kt`; conserva el mensaje HTTP útil para la interfaz. |
| Sustituir Gson solo si mejora compatibilidad con los DTOs | Existe | Se conservó Gson; los DTOs actuales ya lo usan y el cambio no aporta valor en Fase 0. |
| Ignorar configuraciones locales, cachés, builds y logs de JVM | Existe | `src/BookingHub.Android/.gitignore`, incluido el home de Gradle local usado para esta validación. |
| Limpiar datos sensibles que ya estuvieran en el historial Git | Parcial | Se debe comprobar el historial y, si aplica, rotar secretos y limpiar mediante reescritura coordinada; no se reescribió historial en esta fase. |

### Fase 1 — Autenticación completa de clientes

| Requisito | Estado | Evidencia |
|---|---|---|
| Credenciales de cliente, email único y hash ASP.NET Identity/BCrypt | Falta | `ClienteDto` no incluye contraseña; alta actual crea un perfil sin credenciales. |
| Endpoints de registro, refresh, recuperación/cambio de contraseña y baja de cuenta | Falta | No existen en `AuthController` / `ClientesController`. |
| Login único para clientes y personal que devuelva rol | Parcial | Existe login JWT para usuarios de personal; no existe login de clientes ni redirección unificada. |
| JWT corto y refresh token rotativo guardado con hash | Falta | No se encontró almacenamiento ni endpoint de refresh. |
| Autorización del cliente por dueño para consultar/crear/cancelar citas | Falta | Se acepta `clienteId` arbitrario y la creación es anónima. |
| Rate limiting de login y registro | Falta | No se encontró configuración de rate limiter. |
| Validaciones en español | Parcial | Hay validaciones de negocio/campos, pero no cubren el flujo de credenciales y sus reglas. |
| Reclamo seguro de cuentas antiguas sin contraseña | Falta | No hay flujo de reclamación o restablecimiento. |
| Onboarding, login, registro con contraseña y recuperación en Android | Falta | El registro Android no maneja contraseña; solo existe acceso de barbero. |
| Un único flujo Android que enrute según el rol recibido | Parcial | El acceso de barbero existe separado del perfil de cliente. |
| Authenticator OkHttp para renovar token y reintentar una vez | Falta | Solo se agrega el bearer JWT guardado; no existe refresh ni Authenticator. |
| Cifrado de tokens con Keystore y borrado al cerrar sesión | Parcial | Existe para la sesión del barbero; el perfil de cliente local no es una sesión autenticada. |
| Validación en vivo de email, teléfono dominicano y contraseña | Parcial | Hay validación básica del formulario de cliente y visibilidad de clave en acceso barbero, pero no alta de credenciales ni validador completo. |

### Fase 2 — Reservas completas

| Requisito | Estado | Evidencia |
|---|---|---|
| Revalidar dueño, catálogo activo/precios, jornada y hueco al guardar | Parcial | Servicio valida datos y conflictos; faltan dueño, precio autoritativo y concurrencia segura. |
| Impedir doble reserva con transacción o control de concurrencia | Parcial | Se comprueba solapamiento, sin serialización de reservas concurrentes. |
| Anticipación mínima, ventana máxima y límite configurable de citas activas | Falta | No se encontraron reglas configurables. |
| Consultar una cita propia, cancelarla con regla horaria y reprogramarla | Parcial | Hay lectura individual para personal; no hay flujo de cliente propietario, cancelación ni reprogramación. |
| Cambio de estado del barbero limitado a sus citas y transiciones válidas | Falta | `PUT /api/Citas/{id}` está limitado al personal; no hay endpoint de barbero propio. |
| Crear, consultar y borrar descansos/bloqueos propios del barbero | Parcial | La disponibilidad considera indisponibilidades existentes, pero no hay endpoints de gestión `/api/Barberos/me/bloqueos`. |
| Reserva guiada y opción “cualquiera disponible” | Parcial | Se eligen servicios, barbero, fecha, hora y resumen; no aparece selección de cualquier barbero. |
| Refrescar disponibilidad ante conflicto al confirmar | Falta | No hay manejo específico de hueco tomado y recarga automática. |
| Próximas/Historial, cancelar, reprogramar y repetir una cita | Parcial | Mis citas tiene detalle; no tiene pestañas ni acciones respaldadas por API. |
| Añadir al calendario y compartir una cita | Falta | No se encontraron intents de calendario o compartir. |
| Agenda de día y semana del barbero | Parcial | Existe agenda por día; no vista semanal. |
| Cambiar estado desde la agenda | Falta | El backend actual lo rechaza para el rol Barbero. |
| Contactar al cliente por llamada o WhatsApp | Falta | Se muestra información de cita, sin acción externa de llamada/WhatsApp. |
| Gestionar bloqueos y días libres desde Android | Falta | No hay endpoints ni interfaz de gestión. |

### Fase 3 — Funciones de producto

| Requisito | Estado | Evidencia |
|---|---|---|
| Editar perfil del cliente y subir foto comprimida | Falta | No hay endpoints ni interfaz de edición/subida. |
| Barbero favorito y reservar con favorito desde Inicio | Falta | No se encontró persistencia ni acción de favorito. |
| Reseñas de 1–5 estrellas, comentario y promedio | Falta | No hay endpoints ni modelo/tabla auditados para reseñas. |
| Ficha de barbero con imagen, especialidades, calificación y huecos próximos | Parcial | Hay catálogo/listado de barberos y disponibilidad; falta ficha enriquecida, imagen/calificación. |
| Búsqueda, filtro por categoría y orden del catálogo | Parcial | Solo hay filtro básico de servicios/productos. |
| FCM, registro de dispositivo y eventos push | Falta | No existe `/api/Dispositivos`; el repositorio documenta esta limitación. |
| Permiso Android 13+, canal y navegación a cita al tocar push | Falta | No se encontró integración FCM ni permiso/canal. |
| Recordatorio local de respaldo con WorkManager | Falta | No hay dependencia ni tarea WorkManager. |
| Información de barbería, mapa, llamada y WhatsApp | Falta | No hay pantalla ni acciones con esos datos. |
| Ayuda, términos y política de privacidad dentro de la app | Falta | No se encontraron pantallas dedicadas. |

### Fase 4 — Calidad de producto

| Requisito | Estado | Evidencia |
|---|---|---|
| Carga, vacío, error con reintento y modo sin conexión en todas las pantallas | Parcial | Hay estados y caché en flujos principales, no una cobertura uniforme de todas las pantallas. |
| Pull-to-refresh en listas y paginación del historial | Parcial | Hay acciones de recarga en algunas pantallas; no pull-to-refresh ni paginación. |
| Tema claro/oscuro y color dinámico apagado | Parcial | Hay esquemas claro y oscuro; falta preferencia del usuario/sistema integrada. |
| Splash screen oficial de Android 12+ | Falta | No se encontró API SplashScreen. |
| Todos los textos en `strings.xml` español y traducción inglesa | Falta | Persisten muchos textos en Kotlin; no existe `values-en`. |
| Fechas, horas y moneda por locale | Parcial | Se formatea en español dominicano y RD$; no se localiza toda la interfaz. |
| Accesibilidad: descripciones, objetivos 48dp, contraste y TalkBack | Parcial | Hay descripciones en iconos interactivos principales; cobertura no uniforme ni auditada. |
| Pantallas grandes y orientación horizontal | Parcial | Compose evita layouts de vistas fijas, pero no hay diseños adaptativos específicos ni verificación. |
| Atrás, rotación y proceso eliminado con SavedStateHandle | Parcial | Navigation Compose maneja navegación; no se encontró restauración completa vía SavedStateHandle. |
| Deep link `bookinghub://cita/{id}` | Falta | No hay filtros deep link ni destino para una cita. |

### Fase 5 — Seguridad y privacidad

| Requisito | Estado | Evidencia |
|---|---|---|
| Sin logs HTTP en release | Existe | `NetworkModule` usa nivel `NONE` en release. |
| Minify, shrinkResources y reglas R8 probadas | Parcial | Reglas ProGuard existen, pero minify está apagado y no se probó APK release. |
| HTTPS en release y HTTP local solo en debug | Existe | Placeholder `usesCleartextTraffic` false en release y configurable en debug. |
| Network Security Config y evaluación de pinning/cambio de certificados | Falta | No hay configuración de certificados ni pinning. |
| `allowBackup=false` y reglas de extracción | Parcial | Backup está desactivado; no hay `data_extraction_rules`. |
| No guardar secretos en logs/cache/capturas y `FLAG_SECURE` en credenciales | Parcial | Sesiones y caché cifradas; falta `FLAG_SECURE` y una auditoría integral de exposición de datos. |
| CORS, HTTPS y secretos del backend | Parcial | CORS tiene orígenes configurados y HTTPS redirection; falta endurecimiento completo y la API genera una clave JWT temporal si no está configurada. |
| Eliminar cuenta y datos personales desde Android | Falta | No hay endpoint ni interfaz para baja/autonomía de datos. |
| Política de privacidad | Falta | No se encontró documento integrado. |

### Fase 6 — Pruebas

| Requisito | Estado | Evidencia |
|---|---|---|
| Unit tests para ViewModels/repositorios y escenarios de red/401/conflicto | Existe | 59 pruebas JVM Android implementadas y ejecutadas; desglose y límites en [Fase 6](fase-6-pruebas.md). |
| UI tests Compose para autenticación, reserva y agenda | Parcial | El source set `androidTest` compila; no se ejecutó porque el entorno no tenía dispositivo conectado ni AVD configurado. |
| Tests backend de lógica y autorización | Existe | 46 pruebas de Application pasaron, incluidas reglas de reserva, propiedad y transiciones de estado. |
| Cobertura mínima 70 % | Parcial | 73,79 % para modelos/repositorios/ViewModels y gate Gradle acotado a esos paquetes; la cobertura global de app es 22,31 %. |
| Lista manual de dispositivos/red/orientación/tema | Existe | Checklist previo a entrega documentado en [Fase 6](fase-6-pruebas.md), pendiente de ejecución manual. |

### Fase 7 — Entrega y publicación

| Requisito | Estado | Evidencia |
|---|---|---|
| GitHub Actions Android para PR con build, lint, análisis y pruebas | Parcial | `.github/workflows/android-ci.yml` ejecuta lint, pruebas JVM, cobertura, Compose en emulador y empaquetado; pendiente confirmar una corrida en GitHub. |
| AAB/APK firmado en tags con keystore en secrets | Parcial | `.github/workflows/android-release.yml` valida y firma con cuatro secretos, verifica APK/AAB y crea release; secretos aún no están provisionados. |
| Versionado automático desde tags | Existe | Tags `vMAJOR.MINOR.PATCH` se transforman a `versionName` y `versionCode`; fórmula y límites documentados en `docs/fase-7-entrega.md`. |
| Crashlytics/alternativa y analítica mínima respetuosa | Falta | No hay proyecto Firebase ni credenciales; se mantiene sin recopilación remota y requiere decisión/configuración externa. |
| Verificar que despliegue Azure siga funcionando y migraciones seguras | Parcial | Se inspeccionaron workflows existentes; no se modificaron ni ejecutaron porque Fase 0 limita cambios a Android. |
| README con arquitectura, backend, variables y publicación | Existe | El README enlaza guía de CI, firma, versionado, Firebase y Play Console. |
| Material de Play: iconos adaptativos, capturas y fichas de tienda | Parcial | Icono adaptativo/monocromático y borrador de ficha listos; faltan imágenes PNG, capturas reales y datos del titular de Play Console. |

### Definición final de terminado

| Resultado esperado | Estado | Brecha principal |
|---|---|---|
| Cliente se registra con contraseña, reserva, recibe avisos, modifica y borra cuenta | Falta | Requiere Fases 1–3 y endpoints de API compatibles. |
| Barbero gestiona estado, agenda y bloqueos, y contacta clientes | Parcial | Agenda de lectura existe; estados/bloqueos necesitan API y UI. |
| Un cliente no puede acceder a datos de otro | Falta | La API acepta `clienteId` libre y citas públicas. |
| Dos clientes no reservan el mismo hueco | Parcial | Hay chequeo de solapamiento sin transacción/control concurrente. |
| Sin conexión, recuperación automática y datos cifrados | Parcial | Hay caché cifrada; la actualización al recuperar red no está automatizada en todos los flujos. |
| Debug/release, lint y pruebas pasan sin pantallas/botones incompletos | Parcial | Debug compila tras Fase 0; no hay tests Android y release/lint no están configurados. |
| Blazor y WinForms siguen operando contra la misma API | Existe (contrato actual) | No se hicieron cambios fuera de Android; nuevas capacidades requieren cambios retrocompatibles futuros. |

## Cambios implementados en Fase 0

1. Se centralizaron plugins y dependencias en `gradle/libs.versions.toml`; se elevó compileSdk a 37 y targetSdk a 36. Para AGP 9 se migró al soporte Kotlin integrado y se quitó el plugin `kotlin-android` incompatible con el DSL nuevo.
2. Se añadió Hilt para `Application`, `MainActivity`, red, sesiones/caché y ViewModels. La construcción de servicios Retrofit queda dentro de `ApiClient` inyectado.
3. Se añadieron repositorios por dominio y `ApiResult` para éxitos, conectividad, HTTP, sesión expirada y funciones no soportadas. Gson se mantiene por compatibilidad de DTOs.
4. Se migraron los siete ViewModels y las pantallas que los crean a Hilt. Se eliminaron APIs Compose/Java obsoletas detectadas por la compilación.
5. Se amplió el ignore Android para el home Gradle aislado y se actualizó el README con la arquitectura/versiones actuales.

## Validación de la Fase 0

- `:app:assembleDebug`: completado correctamente; APK debug generado en el directorio de build aislado bajo `.gradle-user-home` para no interferir con archivos abiertos por Android Studio.
- No hay pruebas Android existentes. El backend tiene su propio suite .NET, que no se modificó ni se ejecutó como parte del cambio Android.
- Persisten avisos del entorno JDK 25 sobre acceso nativo restringido y creación de archivo temporal del daemon Kotlin; el build terminó correctamente y esos avisos no son errores de compilación del código.
- No se probó release, instalación en dispositivo ni llamadas en Azure en esta fase.
