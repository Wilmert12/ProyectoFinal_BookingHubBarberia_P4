# Fase 7 — CI, versiones y entrega Android

Esta fase deja automatizados los controles Android y la generación de una release firmada. GitHub todavía debe recibir los secretos de firma y ejecutar los workflows al subir los cambios; no se publicará una versión de producción sin esos pasos.

## Automatización de PR y `main`

`.github/workflows/android-ci.yml` valida únicamente el proyecto Android cuando el PR o commit incluye Android o sus workflows. Ejecuta Lint debug, pruebas JVM, reporte y umbral JaCoCo, compilación de tests Compose, empaquetado debug/release y pruebas Compose en un emulador API 36. Si el runner no ofrece aceleración KVM, habilita TCG y espera hasta 20 minutos para que inicie el emulador. Los resultados y APK/AAB de comprobación quedan como artefactos de la ejecución. El workflow tiene permiso de solo lectura.

El código compila con API 37 porque las versiones actuales de Compose y Lifecycle lo requieren; Android 17 sigue en Preview. Cada job acepta licencias sin interacción e instala `platforms;android-37.0` desde el canal Canary. La app conserva `targetSdk = 36`, probado en emulador API 36 y alineado con Play Console.

## Versiones y release firmada

Se aceptan etiquetas estables con formato `vMAJOR.MINOR.PATCH`, por ejemplo `v1.2.3`. El `versionName` se toma de la etiqueta y el `versionCode` se calcula como `MAJOR * 1 000 000 + MINOR * 1 000 + PATCH`; `MINOR` y `PATCH` deben ser menores que 1000 para evitar colisiones. Las compilaciones locales usan `1.0.0`/`1` si no se especifican propiedades.

Antes de crear la etiqueta, agrega estos cuatro **Repository secrets** en GitHub. Se usan solo en el workflow de release:

| Secreto | Contenido |
|---|---|
| `ANDROID_KEYSTORE_BASE64` | Archivo `.jks` de publicación codificado en Base64, sin saltos de línea. |
| `ANDROID_KEYSTORE_PASSWORD` | Contraseña del keystore. |
| `ANDROID_KEY_ALIAS` | Alias de la llave de publicación. |
| `ANDROID_KEY_PASSWORD` | Contraseña de la llave. |

Conserva una copia de respaldo del keystore fuera del repositorio. No reutilices una clave que ya haya sido expuesta ni subas el archivo, contraseñas o `local.properties` a Git. El workflow valida los cuatro secretos, decodifica la llave solo en el runner temporal, exige firma, genera APK y AAB, comprueba ambas firmas y crea un GitHub Release con notas. Solo requiere `contents: write` en el workflow de release.

Después de confirmar que CI está verde y de haber cargado los secretos:

```powershell
git tag v1.0.0
git push origin v1.0.0
```

No uses `v1.0.0` si esa versión ya fue publicada: Android exige que `versionCode` aumente en cada entrega a Play Console. El flujo rechaza etiquetas mal formadas y códigos fuera de rango.

Para compilar localmente con una llave privada ya provisionada en el entorno:

```powershell
.\gradlew.bat -PbookinghubRequireReleaseSigning=true -PbookinghubVersionName=1.2.3 -PbookinghubVersionCode=1002003 :app:assembleRelease :app:bundleRelease
```

Configura `ANDROID_KEYSTORE_PATH`, `ANDROID_KEYSTORE_PASSWORD`, `ANDROID_KEY_ALIAS` y `ANDROID_KEY_PASSWORD` en la sesión segura de build; no escribas los valores directamente en el comando ni en un archivo versionado.

## Play Console

La aplicación ahora tiene icono adaptativo con capa monocromática. El borrador de ficha de tienda está en [ficha-play-store.md](ficha-play-store.md). Antes de enviar a revisión aún se deben proporcionar el icono PNG de 512×512, gráfico promocional 1024×500, al menos dos capturas reales de teléfono, capturas de tablet si se distribuye a tablets, URL pública de privacidad, email de soporte y declaraciones de contenido/recopilación en Play Console. Las capturas deben generarse desde la app instalada; no se incluyen imágenes simuladas como si fueran la app real.

## FCM, fallos y configuración externa

No hay un proyecto Firebase ni `google-services.json` en el árbol Android, y el backend actual no registra tokens de dispositivo ni envía FCM. La app mantiene recordatorios locales con WorkManager. Crashlytics tampoco se habilitó: requiere un Firebase App ID/configuración real y definir consentimiento/retención antes de recopilar informes. Cuando se entregue esa configuración, se podrá agregarla mediante secretos y completar una prueba extremo a extremo; inventar un `google-services.json` no daría servicio operativo.

## Azure y cambios de base de datos

Los workflows de esta fase no despliegan la API ni ejecutan SQL. Las migraciones para las funciones de Android permanecen documentadas en `docs/fase-2-y-3.md`; para aplicarlas en Azure se necesita acceso autenticado al servidor y una copia de seguridad comprobable de la base compartida. No se ejecuta una migración contra producción desde CI Android.

## Estado de verificación

- El YAML y la ejecución de GitHub Actions deben confirmarse después de subir este cambio; este entorno local no está autenticado con `gh` ni puede iniciar el workflow remoto.
- La validación local completó `:app:lintDebug`, 59 pruebas JVM, reporte JaCoCo, gate core de 70 %, compilación Kotlin de AndroidTest, `assembleDebug`, `assembleRelease` y `bundleRelease`. Lint Vital de release queda excluido; `lintDebug` pasó y reporta avisos menores de actualización de dependencias y SharedPreferences.
- La API compila localmente con sus proyectos compartidos (0 errores); el restore local aislado todavía requiere revisión y los assets previos muestran un aviso transitorio de una dependencia vulnerable. El pin corregido quedó ubicado en Infrastructure y debe confirmarse tras un restore limpio.
- La publicación firmada depende de los cuatro secretos de keystore y la creación de una etiqueta nueva. No se generó ni almacenó una clave privada en el repositorio.

