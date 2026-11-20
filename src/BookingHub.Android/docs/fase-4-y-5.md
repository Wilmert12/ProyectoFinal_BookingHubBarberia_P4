# Fases 4 y 5: calidad, seguridad y privacidad

## Fase 4 — Calidad de producto

- Catálogo, barberos, citas y agenda de barbero permiten actualizar con gesto de pull-to-refresh. Las listas que tienen datos conservan su contenido mientras actualizan.
- El catálogo y la lista pública de barberos usan una copia local cifrada para mostrar la última información disponible sin red. Las citas continúan separadas por cliente o barbero y también se cifran con una clave de Android Keystore.
- `GET /api/Citas/mias/historial?pagina=1&tamano=10` devuelve como máximo 20 citas por página. Requiere rol `Cliente` y deriva el propietario del JWT. Android solicita bloques de diez y carga más al pulsar **Cargar más citas**. La API antigua de Blazor (`GET /api/Citas?clienteId=...`) permanece intacta.
- El tema claro/oscuro sigue el modo del sistema y conserva la paleta de CerquilloApp. El color dinámico permanece desactivado.
- Android 12 o superior usa el splash nativo de plataforma definido en `values-v31`.
- Los rótulos y mensajes fijos principales de las pantallas tienen recursos en español e inglés. Fechas y separadores numéricos usan el locale del dispositivo; los importes conservan la moneda RD$.
- La navegación limita el contenido a 840 dp y lo centra en pantallas anchas. La navegación Compose restaura la ruta al recrear la actividad. `SavedStateHandle` restaura barbería, servicios, fecha, notas y hora de la reserva; la hora solo se conserva si la API confirma que sigue disponible. El detalle acepta `bookinghub://cita/{id}`.
- Los botones de acción mantienen el tamaño mínimo de Material y los iconos de acciones principales tienen descripción. Las pantallas de registro, acceso, recuperación y cambio de contraseña activan `FLAG_SECURE`.

## Fase 5 — Seguridad y privacidad

- Release activa R8 (`minifyEnabled`) y `shrinkResources`; las reglas conservan DTOs Gson/Retrofit. El interceptor HTTP solo registra nivel BASIC en debug y queda desactivado en release.
- Release desactiva explícitamente el tráfico HTTP. En debug, `usesCleartextTraffic` solo queda activo cuando la URL configurada empieza por `http://`, para permitir la API local durante desarrollo. Se usan las autoridades de certificación del sistema; no se aceptan certificados de usuario de forma global. No se añadió certificate pinning: Azure App Service puede rotar certificado, cadena o frontend; un pin obsoleto bloquearía el acceso de todas las instalaciones hasta publicar otra app.
- Las copias de seguridad de Android y la transferencia entre dispositivos están desactivadas y excluyen archivos, bases de datos, preferencias y almacenamiento externo.
- El backend filtra CORS a orígenes HTTPS no locales en producción, usa respuestas genéricas para errores no controlados, agrega cabeceras de seguridad y exige `Jwt__Clave` en Azure. Configura `Cors__OrigenesPermitidos` como lista de orígenes HTTPS del sitio publicado y `Jwt__Clave` como secreto aleatorio de al menos 32 bytes. No pongas el secreto en `appsettings.json` ni en Git.
- La pantalla de privacidad explica qué datos procesa CerquilloApp y cómo solicitar la baja. La eliminación desde Ajustes anonimiza el titular, quita favoritos y foto activa, y conserva citas históricas anónimas.
- `System.Security.Cryptography.Pkcs` se fija en Infrastructure a `10.0.13`, versión corregida para .NET 10. Infrastructure ya se restauró con esa versión; el `project.assets.json` antiguo de API todavía registra `9.0.11`. En este equipo el restore del grafo de API falla sin diagnóstico de NuGet y debe repetirse en un entorno .NET reparado para regenerar sus assets y confirmar que desaparecen los avisos.

## Pendientes externos y verificación

- FCM no está activado: faltan el proyecto Firebase de Android (`google-services.json`, App ID) y la credencial de servicio de Firebase para la API. Los recordatorios locales de WorkManager sí están disponibles; no se debe considerar que existen notificaciones push hasta configurar y verificar esos secretos.
- Apliquen en la base de datos, en orden, los scripts idempotentes de autenticación, Fase 2 y Fase 3 documentados en `fase-2-y-3.md` antes de desplegar la API nueva. El código no ejecuta cambios de esquema automáticamente.
- La validación más reciente de Android terminó con `lintDebug`, 59 pruebas JVM, reporte JaCoCo, gate de cobertura core al 70 %, compilación Kotlin de AndroidTest y `assembleDebug`, `assembleRelease` y `bundleRelease` correctos. La cobertura core fue 73.79 %; la cobertura de toda la app es 22.31 %. El APK debug verificó firma v2 con `apksigner`; los paquetes declaran `com.bookinghub.app`, minSdk 26, targetSdk 36 y versión local 1.0.0/1.
- El release local y el AAB local son de validación, sin una clave privada de publicación; el workflow de release exige los secretos y verifica ambas firmas. No hay dispositivo ADB conectado para instalar y recorrer la aplicación, así que las pruebas instrumentadas quedan para el job de emulador GitHub Actions.
- La API se compiló localmente con sus proyectos Application e Infrastructure: 0 errores. El build aún reporta `System.Security.Cryptography.Pkcs 9.0.11` desde los assets obsoletos de API; el asset restaurado de Infrastructure ya usa `10.0.13`. Falta regenerar el grafo de API con un restore limpio y confirmar que desaparecieron los avisos.
- Las 46 pruebas de Application aprobaron en la validación previa. La restauración aislada posterior de API terminó sin diagnóstico útil; se debe repetir restore/build/tests luego de resolver el restore local. La conexión segura a Azure y la aplicación de migraciones continúan pendientes.
- El APK release se genera sin firma en `app-release-unsigned.apk`; el keystore y sus contraseñas deben configurarse fuera del repositorio para crear un artefacto publicable.
