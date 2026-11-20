# Fase 6 — Pruebas y verificación

Fecha de revisión: 20 de noviembre de 2026. Alcance: pruebas Android del cliente móvil y pruebas de aplicación del backend necesarias para validar los contratos usados por Android. No se modificaron Blazor ni WinForms.

## Cobertura implementada

- **59 pruebas JVM Android** para modelos, repositorios y ViewModels. Incluyen registro e inicio de sesión, manejo HTTP/401, recuperación y cambio de contraseña, consultas de barbería/catálogo/citas, conflictos de reserva y estados de agenda.
- **Pruebas Compose instrumentadas** para validaciones y registro, acceso y roles, pantalla de reserva y agenda. El source set instrumentado compila, pero estas pruebas no se ejecutaron: no había teléfono conectado ni AVD configurado en el entorno.
- **46 pruebas .NET** de `BookingHub.Application.UnitTests` pasaron, incluidas las nuevas reglas de creación de reservas, propiedad de la cita y transiciones del barbero. Se ejecutaron para validar el contrato del backend que consume Android; no se editó la interfaz de Blazor ni la de WinForms.
- El reporte JaCoCo mide **22,31 % de líneas en toda la app** y **73,79 %** en el conjunto de modelos, repositorios y ViewModels. El umbral del 70 % que bloquea `check` se limita expresamente a esos paquetes de lógica; el objetivo de 70 % para toda la app sigue pendiente.

Los números describen la última ejecución verificada del entorno. Al añadir o retirar pruebas, vuelve a generar el reporte antes de usarlos para comparar cambios.

## Comandos

Desde `src/BookingHub.Android`:

```powershell
.\gradlew.bat :app:testDebugUnitTest
.\gradlew.bat :app:compileDebugAndroidTestKotlin
.\gradlew.bat :app:jacocoDebugUnitTestReport
.\gradlew.bat :app:jacocoCoreCoverageVerification
```

La verificación de cobertura también se ejecuta con `check`. El HTML se genera en `app/build/reports/jacoco/jacocoDebugUnitTestReport/html/index.html`.

Para ejecutar instrumentación, conecta un dispositivo con depuración USB o inicia un AVD compatible y ejecuta:

```powershell
.\gradlew.bat :app:connectedDebugAndroidTest
```

Las pruebas de servicios del backend están en `tests/BookingHub.Application.UnitTests`. En un entorno .NET normal:

```powershell
dotnet test tests/BookingHub.Application.UnitTests/BookingHub.Application.UnitTests.csproj
```

En la máquina de revisión se usaron `--no-restore`, `--no-build` y opciones para desactivar el resolver de workloads, porque el proceso de MSBuild se quedaba esperando al resolver local. El resultado fue 46 aprobadas, 0 fallidas.

## Checklist manual antes de entregar

La automatización no reemplaza la validación con API Azure y un dispositivo real. Marcar cada fila al realizarla:

| Flujo | Comprobación |
|---|---|
| Instalación y arranque | Instalar debug desde cero y actualización sobre una versión anterior; confirmar splash, navegación inicial y ausencia de cierre inesperado. |
| Cuenta | Registrar cliente, comprobar errores por campos/email repetido, cerrar sesión, volver a iniciar, recuperar clave, cambiarla y verificar reautenticación. |
| Roles | Iniciar como cliente y como barbero; verificar que cada uno entra solo a su área y que un rol no admitido recibe un mensaje claro. |
| Reserva | Consultar disponibilidad real, reservar, confirmar precio/duración del servidor, probar hueco ocupado, cancelar dentro y fuera del plazo permitido y reservar nuevamente. |
| Citas del cliente | Abrir detalle, verificar servicios, totales, estado e identificadores; probar cancelar, reprogramar, repetir, compartir y añadir al calendario donde estén disponibles. |
| Agenda del barbero | Cambiar día/semana, abrir detalles, actualizar a cada estado permitido, comprobar transiciones rechazadas y confirmar que no se modifica una cita ajena. |
| Red y caché | Probar conexión lenta, modo avión, error 401 y recuperación de conexión; verificar indicador de datos guardados y que las citas visibles pertenecen a la sesión activa. |
| Ciclo de vida | Girar pantalla, enviar app al fondo, cerrarla desde recientes y relanzarla; confirmar navegación/sesión esperadas y que no se repitan reservas o recordatorios. |
| Notificaciones | Android 13 o superior: aceptar y denegar permiso; revisar canal y horarios de recordatorios locales. Push FCM queda fuera hasta configurar credenciales. |
| Privacidad | Cerrar sesión, eliminar cuenta, verificar borrado local, política de retención de citas históricas y que no aparezcan datos sensibles en capturas/logs. |
| Visual y accesibilidad | Tema claro/oscuro, tamaño de fuente, TalkBack, contraste, objetivos táctiles, teclado abierto, orientación horizontal, pantalla pequeña y tablet. |
| Idioma y formato | Español e inglés; fechas, hora y moneda según locale. |
| Integración | Comparar en la misma base de Azure los IDs/estados de la cita con la consulta existente de Blazor/WinForms sin cambiar esos clientes. |
| Release | Generar release minificado, instalarlo, revisar HTTPS/Azure y verificar firma con el keystore de publicación fuera del repositorio antes de distribuir. |

## Pendientes conocidos

1. Ejecutar `connectedDebugAndroidTest` en al menos un dispositivo físico y un emulador (incluida la API mínima soportada).
2. Completar el checklist manual, especialmente regresión con la API Azure, rotación/proceso eliminado y accesibilidad.
3. Ampliar cobertura de toda la app: el 22,31 % global está por debajo del objetivo general del 70 % aunque la lógica de modelos/repositorios/ViewModels supera el umbral configurado.
4. Incorporar las validaciones de dispositivos a CI Android en una fase de entrega; esta fase deja pruebas reproducibles localmente.

