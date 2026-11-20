# Fases 2 y 3: reservas y experiencia móvil

## Fase 2: reservas y agenda

La API valida las reservas autenticadas del rol `Cliente` en `ReservaCitaService`: toma el dueño desde `sub`, verifica barbero y servicios activos, recalcula precio y duración desde el catálogo, aplica anticipación, límite de citas y jornada, y revisa bloqueos y choques dentro de una transacción serializable. `GET /api/Citas/{id}` valida dueño para clientes y barbero asignado para barberos.

Rutas nuevas:

- `PUT /api/Citas/{id}/cancelar` y `PUT /api/Citas/{id}/reprogramar`: solo cliente propietario. Respetan las horas mínimas configuradas y recalculan el total vigente.
- `PUT /api/Citas/{id}/estado`: solo rol `Barbero`, solo citas de su agenda y transiciones de estado explícitas.
- `GET|POST /api/Barberos/me/bloqueos` y `DELETE /api/Barberos/me/bloqueos/{id}`: el barbero solo lista, crea o quita sus propios bloqueos; el servidor impide bloquear citas existentes.
- `GET /api/Barberos/{id}/disponibilidad` sigue siendo la fuente de horas abiertas. La app renueva disponibilidad si falla la reserva por conflicto.

Android agrega pestañas de citas próximas e historial, ver detalles, cancelar, reprogramar, repetir una cita pasada desde el historial, compartir y añadir una cita al calendario. El flujo de reserva permite elegir un barbero o cualquier barbero disponible; al elegir una hora se muestra y se envía el barbero concreto cuya disponibilidad fue consultada. La agenda de barbero permite cambiar entre vistas de día y semana, actualizar estados, llamar o escribir por WhatsApp al cliente y administrar bloqueos de día completo. WorkManager persiste recordatorios locales de 24 y 1 hora antes; Android 13+ ofrece activar el permiso desde Reservar.

Reglas editables con configuración de API (`Reservas__...`):

- `AnticipacionMinimaHoras` (2 por defecto).
- `DiasMaximosAnticipacion` (90).
- `CitasActivasMaximas` (3).
- `HorasMinimasParaCancelar` (6).

Antes de desplegar la API, ejecutar el script idempotente `src/BookingHub.Infrastructure/Database/Migraciones/20261120_Fase2_AgendaReservas.sql`; agrega tipos de bloqueos de agenda sin borrar datos.

## Fase 3: perfil, favoritos y catálogo

- `GET|PUT /api/Clientes/me` consulta y edita el perfil a partir de la identidad JWT; el email se comprueba único.
- `POST /api/Clientes/me/foto` valida JPEG/PNG de hasta 1 MB. Si existe `AzureBlob__FotoPerfilSasUrl` (SAS HTTPS de contenedor con permisos de lectura, escritura y creación), la foto se guarda como `clientes/{clienteId}.jpg|png`; si no, se guarda en SQL. Android redimensiona y comprime la imagen antes de enviarla. Borrar la cuenta elimina la foto del almacenamiento activo.
- `GET|POST|DELETE /api/Clientes/me/favoritos[/barberoId]` mantiene favoritos personales.
- `POST /api/Resenas` admite una sola reseña de 1–5 estrellas por cita completada y propia. `GET /api/Barberos/{id}/resenas` muestra reseñas; los barberos incluyen promedio y cantidad en sus tarjetas.
- `GET /api/CatalogoItems?q=&esServicio=&ordenar=` permite búsqueda, filtro por tipo y orden por nombre, precio o duración.
- Las tarjetas públicas muestran el primer horario disponible para una cita de 30 minutos dentro de los próximos siete días; al reservar, la disponibilidad se vuelve a consultar con la duración real de los servicios.
- `GET /api/InformacionBarberia` publica los datos configurables de contacto, dirección, mapa y horario.
- Ajustes Android ofrece perfil, información de barbería, ayuda, términos y privacidad.

Ejecutar la migración idempotente `src/BookingHub.Infrastructure/Database/Migraciones/20261120_Fase3_ExperienciaCliente.sql` antes de publicar la nueva API. Agrega foto, categorías, especialidades, favoritos y reseñas. Las categorías son texto opcional; los filtros principales usan servicio/producto porque el catálogo actual no tiene un catálogo normalizado de categorías.

Configurar datos de barbería mediante `Barberia__Nombre`, `Barberia__Direccion`, `Barberia__Telefono`, `Barberia__WhatsApp`, `Barberia__Horario` y `Barberia__UrlMapa`. La app no inventa la dirección o teléfonos cuando aún no están configurados.

## Integración y límites

- No se alteraron proyectos Blazor ni WinForms. Se mantuvieron los contratos heredados usados por ellos.
- Las migraciones SQL se dejaron preparadas, no se ejecutaron contra Azure. El operador del despliegue debe respaldar la base y ejecutar los scripts en orden: autenticación, Fase 2, Fase 3.
- Los recordatorios locales ya funcionan sin conectividad push. FCM no está activado: el repositorio no incluye `google-services.json`, Firebase Project/App IDs ni una credencial de servicio del backend. Esos recursos son necesarios para registrar tokens reales y enviar avisos remotos.
- Azure Blob es opcional mientras `AzureBlob__FotoPerfilSasUrl` no esté definido; en ese entorno local/de desarrollo las fotos quedan en SQL.
