package com.bookinghub.app.ui.screens

import androidx.compose.material3.pulltorefresh.PullToRefreshBox

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.compose.material3.Tab
import androidx.compose.material3.PrimaryTabRow
import android.content.Intent
import android.provider.CalendarContract
import android.app.TimePickerDialog
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalLocale
import com.bookinghub.app.data.model.EstadoCitaDto
import com.bookinghub.app.data.model.CitaDto
import com.bookinghub.app.ui.theme.Acento
import com.bookinghub.app.ui.theme.Borde
import com.bookinghub.app.ui.viewmodel.CitasViewModel
import java.time.LocalDate
import java.time.LocalDateTime
import java.time.Instant
import java.time.ZoneId
import java.time.ZoneOffset
import java.time.format.DateTimeFormatter

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MisCitasScreen(
    onRepetirCita: (CitaDto) -> Unit,
    viewModel: CitasViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsState()
    val context = LocalContext.current
    var pestana by remember { mutableIntStateOf(0) }
    var citaParaCancelar by remember { mutableStateOf<CitaDto?>(null) }
    var citaParaReprogramar by remember { mutableStateOf<CitaDto?>(null) }
    var citaParaCalificar by remember { mutableStateOf<CitaDto?>(null) }
    var puntaje by remember { mutableIntStateOf(5) }
    var comentarioResena by remember { mutableStateOf("") }

    val citasFuturas = uiState.citas.filter { it.fechaLocal() > LocalDateTime.now() && !it.estaCerrada() }
    val citasHistoricas = uiState.citas.filterNot { it in citasFuturas }.sortedByDescending { it.fechaLocal() }
    val citasVisibles = if (pestana == 0) citasFuturas else citasHistoricas

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.my_appointments), fontWeight = FontWeight.Bold) },
                actions = {
                    IconButton(onClick = { viewModel.cargarCitas() }) {
                        Icon(Icons.Default.Refresh, contentDescription = "Recargar")
                    }
                }
            )
        }
    ) { paddingValues ->
        Box(
            modifier = Modifier
                .fillMaxSize()
                .padding(paddingValues)
        ) {
            when {
                uiState.cargando && uiState.citas.isEmpty() -> {
                    Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                        CircularProgressIndicator(color = Acento)
                    }
                }
                uiState.error != null -> {
                    Box(
                        modifier = Modifier
                            .fillMaxSize()
                            .padding(24.dp),
                        contentAlignment = Alignment.Center
                    ) {
                        Column(horizontalAlignment = Alignment.CenterHorizontally) {
                            Icon(
                                imageVector = Icons.Default.Warning,
                                contentDescription = null,
                                tint = MaterialTheme.colorScheme.error,
                                modifier = Modifier.size(48.dp)
                            )
                            Spacer(modifier = Modifier.height(8.dp))
                            Text(
                                text = uiState.error!!,
                                color = MaterialTheme.colorScheme.error,
                                style = MaterialTheme.typography.bodyMedium
                            )
                            Spacer(modifier = Modifier.height(16.dp))
                            Button(onClick = { viewModel.cargarCitas() }) {
                                Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.retry))
                            }
                        }
                    }
                }
                uiState.citas.isEmpty() -> {
                    Box(
                        modifier = Modifier
                            .fillMaxSize()
                            .padding(24.dp),
                        contentAlignment = Alignment.Center
                    ) {
                        Column(horizontalAlignment = Alignment.CenterHorizontally) {
                            Icon(
                                imageVector = Icons.Default.EventBusy,
                                contentDescription = null,
                                modifier = Modifier.size(64.dp),
                                tint = MaterialTheme.colorScheme.onSurfaceVariant.copy(alpha = 0.5f)
                            )
                            Spacer(modifier = Modifier.height(16.dp))
                            Text(
                                text = "Aún no tienes citas agendadas",
                                style = MaterialTheme.typography.titleMedium,
                                fontWeight = FontWeight.SemiBold
                            )
                            Spacer(modifier = Modifier.height(6.dp))
                            Text(
                                text = "Usa la pestaña 'Reservar' para solicitar tu primera cita.",
                                style = MaterialTheme.typography.bodyMedium,
                                color = MaterialTheme.colorScheme.onSurfaceVariant
                            )
                        }
                    }
                }
                else -> {
                    Column(modifier = Modifier.fillMaxSize()) {
                        if (uiState.cargando) {
                            LinearProgressIndicator(modifier = Modifier.fillMaxWidth())
                        }
                        uiState.aviso?.let { aviso ->
                            Text(
                                text = aviso,
                                modifier = Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 8.dp),
                                style = MaterialTheme.typography.bodySmall,
                                color = MaterialTheme.colorScheme.error
                            )
                        }
                        PrimaryTabRow(selectedTabIndex = pestana) {
                            Tab(selected = pestana == 0, onClick = { pestana = 0 }, text = { Text("Próximas (${citasFuturas.size})") })
                            Tab(selected = pestana == 1, onClick = { pestana = 1 }, text = { Text("Historial (${citasHistoricas.size})") })
                        }
                        if (citasVisibles.isEmpty()) {
                            Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                                Text(if (pestana == 0) "No tienes citas próximas." else "Todavía no hay citas en el historial.")
                            }
                        } else {
                        PullToRefreshBox(
                            isRefreshing = uiState.cargando,
                            onRefresh = viewModel::cargarCitas,
                            modifier = Modifier.fillMaxSize()
                        ) {
                        LazyColumn(
                            modifier = Modifier.fillMaxSize(),
                            contentPadding = PaddingValues(16.dp),
                            verticalArrangement = Arrangement.spacedBy(12.dp)
                        ) {
                            items(citasVisibles, key = { it.citaId }) { cita ->
                                val esProxima = cita in citasFuturas
                                val enActualizacion = uiState.citaEnActualizacion == cita.citaId
                                Column(verticalArrangement = Arrangement.spacedBy(6.dp)) {
                                    CitaDetalleCard(
                                        cita = cita,
                                        onRepetir = if (pestana == 1 && cita.detalles.orEmpty().any { it.catalogoItemId > 0 }) {
                                            { onRepetirCita(cita) }
                                        } else null,
                                        onCancelar = if (esProxima && !enActualizacion) ({ citaParaCancelar = cita }) else null,
                                        onReprogramar = if (esProxima && !enActualizacion) ({ citaParaReprogramar = cita }) else null,
                                        onCalificar = if (cita.estaCompletada() && !cita.resenada && cita.citaId !in uiState.resenasEnviadas) ({
                                            citaParaCalificar = cita
                                            puntaje = 5
                                            comentarioResena = ""
                                        }) else null,
                                        onAgregarCalendario = if (esProxima) ({ agregarCalendario(context, cita) }) else null,
                                        onCompartir = { compartirCita(context, cita) }
                                    )
                                    if (enActualizacion) LinearProgressIndicator(Modifier.fillMaxWidth())
                                }
                            }
                            if (pestana == 1 && uiState.historialTieneMas) {
                                item(key = "cargar_mas_historial") {
                                    OutlinedButton(
                                        onClick = viewModel::cargarMasHistorial,
                                        enabled = !uiState.cargandoMasHistorial,
                                        modifier = Modifier.fillMaxWidth().heightIn(min = 48.dp)
                                    ) {
                                        if (uiState.cargandoMasHistorial) {
                                            CircularProgressIndicator(Modifier.size(18.dp), strokeWidth = 2.dp)
                                        } else Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.load_more_appointments))
                                    }
                                }
                            }
                        }
                        }
                        }
                    }
                }
            }
        }
    }

    citaParaCancelar?.let { cita ->
        AlertDialog(
            onDismissRequest = { citaParaCancelar = null },
            title = { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.cancel_appointment)) },
            text = { Text("¿Confirmas que deseas cancelar la cita #${cita.citaId}? La política de anticipación se validará con la barbería.") },
            confirmButton = {
                TextButton(onClick = { viewModel.cancelar(cita.citaId); citaParaCancelar = null }) { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.confirm_cancel)) }
            },
            dismissButton = { TextButton(onClick = { citaParaCancelar = null }) { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.back)) } }
        )
    }

    citaParaReprogramar?.let { cita ->
        val selector = rememberDatePickerState(initialSelectedDateMillis =
            LocalDate.now().plusDays(1).toEpochDay() * 86_400_000L)
        DatePickerDialog(
            onDismissRequest = { citaParaReprogramar = null },
            confirmButton = {
                TextButton(onClick = {
                    val millis = selector.selectedDateMillis
                    if (millis != null) {
                        val fecha = Instant.ofEpochMilli(millis).atZone(ZoneOffset.UTC).toLocalDate()
                        val horaActual = runCatching { cita.fechaLocal().toLocalTime() }.getOrNull()
                        TimePickerDialog(
                            context,
                            { _, hora, minuto ->
                                val nuevaFecha = fecha.atTime(hora, minuto).format(DateTimeFormatter.ISO_LOCAL_DATE_TIME)
                                viewModel.reprogramar(cita.citaId, nuevaFecha)
                            },
                            horaActual?.hour ?: 9,
                            horaActual?.minute ?: 0,
                            true
                        ).show()
                    }
                    citaParaReprogramar = null
                }) { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.choose_time)) }
            },
            dismissButton = { TextButton(onClick = { citaParaReprogramar = null }) { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.cancel)) } }
        ) { DatePicker(state = selector) }
    }

    citaParaCalificar?.let { cita ->
        AlertDialog(
            onDismissRequest = { citaParaCalificar = null },
            title = { Text("Califica a ${cita.barberoNombre.orEmpty()}") },
            text = {
                Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                    Row(horizontalArrangement = Arrangement.spacedBy(2.dp)) {
                        (1..5).forEach { valor ->
                            TextButton(onClick = { puntaje = valor }) {
                                Text(if (valor <= puntaje) "★" else "☆", color = Acento, style = MaterialTheme.typography.titleLarge)
                            }
                        }
                    }
                    OutlinedTextField(
                        value = comentarioResena,
                        onValueChange = { comentarioResena = it.take(500) },
                        label = { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.optional_comment)) },
                        minLines = 2,
                        maxLines = 4
                    )
                }
            },
            confirmButton = {
                TextButton(onClick = { viewModel.calificar(cita.citaId, puntaje, comentarioResena); citaParaCalificar = null }) {
                    Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.submit_review))
                }
            },
            dismissButton = { TextButton(onClick = { citaParaCalificar = null }) { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.cancel)) } }
        )
    }
}

private fun CitaDto.fechaLocal(): LocalDateTime = runCatching {
    LocalDateTime.parse(fechaInicio.take(19))
}.getOrElse { LocalDateTime.MIN }

private fun CitaDto.estaCerrada(): Boolean = when (normalizarEstadoUi(estado)) {
    "cancelada", "completada", "noshow" -> true
    else -> false
}

private fun CitaDto.estaCompletada(): Boolean = normalizarEstadoUi(estado) == "completada"

private fun agregarCalendario(context: android.content.Context, cita: CitaDto) {
    val inicio = cita.fechaLocal().atZone(ZoneId.systemDefault()).toInstant().toEpochMilli()
    val fin = runCatching { LocalDateTime.parse(cita.fechaFin.take(19)) }
        .getOrDefault(cita.fechaLocal().plusMinutes(30)).atZone(ZoneId.systemDefault()).toInstant().toEpochMilli()
    val intent = Intent(Intent.ACTION_INSERT).setData(CalendarContract.Events.CONTENT_URI)
        .putExtra(CalendarContract.Events.TITLE, "Cita en CerquilloApp")
        .putExtra(CalendarContract.Events.DESCRIPTION, "Cita #${cita.citaId} con ${cita.barberoNombre.orEmpty()}")
        .putExtra(CalendarContract.EXTRA_EVENT_BEGIN_TIME, inicio)
        .putExtra(CalendarContract.EXTRA_EVENT_END_TIME, fin)
    runCatching { context.startActivity(intent) }
}

private fun compartirCita(context: android.content.Context, cita: CitaDto) {
    val texto = "Mi cita en CerquilloApp: ${cita.fechaLocal().format(DateTimeFormatter.ofPattern("dd/MM/yyyy HH:mm"))} " +
        "con ${cita.barberoNombre.orEmpty()} · Cita #${cita.citaId}"
    val intent = Intent(Intent.ACTION_SEND).setType("text/plain").putExtra(Intent.EXTRA_TEXT, texto)
    runCatching { context.startActivity(Intent.createChooser(intent, "Compartir cita")) }
}

/** Tarjeta reutilizable para que cliente y barbero consulten los mismos detalles de una cita. */
@Composable
fun CitaDetalleCard(
    cita: CitaDto,
    mostrarCliente: Boolean = false,
    onCancelar: (() -> Unit)? = null,
    onReprogramar: (() -> Unit)? = null,
    onCalificar: (() -> Unit)? = null,
    onCambiarEstado: ((Int) -> Unit)? = null,
    onRepetir: (() -> Unit)? = null,
    estados: List<EstadoCitaDto> = emptyList(),
    onCompartir: (() -> Unit)? = null,
    onAgregarCalendario: (() -> Unit)? = null
) {
    val locale = LocalLocale.current.platformLocale
    val context = LocalContext.current
    var detallesExpandidos by remember(cita.citaId) { mutableStateOf(false) }
    var menuEstados by remember(cita.citaId) { mutableStateOf(false) }
    val fechaMostrar = remember(cita.fechaInicio) {
        try {
            cita.fechaInicio.replace("T", " ").take(16)
        } catch (_: Exception) {
            cita.fechaInicio
        }
    }

    Card(
        modifier = Modifier.fillMaxWidth(),
        shape = RoundedCornerShape(10.dp),
        colors = CardDefaults.cardColors(
            containerColor = MaterialTheme.colorScheme.surface
        ),
        border = BorderStroke(1.dp, Borde)
    ) {
        Column(
            modifier = Modifier
                .padding(16.dp)
                .fillMaxWidth()
        ) {
            Text(
                text = "Cita #${cita.citaId}",
                style = MaterialTheme.typography.labelMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
            Spacer(modifier = Modifier.height(4.dp))

            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Icon(
                        imageVector = Icons.Default.Event,
                        contentDescription = null,
                        tint = Acento,
                        modifier = Modifier.size(20.dp)
                    )
                    Spacer(modifier = Modifier.width(8.dp))
                    Text(
                        text = fechaMostrar,
                        style = MaterialTheme.typography.titleMedium,
                        fontWeight = FontWeight.Bold,
                        color = MaterialTheme.colorScheme.onSurface
                    )
                }

                AssistChip(
                    onClick = {},
                    label = { Text(cita.estado ?: "Agendada") },
                    border = BorderStroke(1.dp, Borde)
                )
            }

            Spacer(modifier = Modifier.height(10.dp))

            Text(
                text = "Barbero: ${cita.barberoNombre ?: "No asignado"}",
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurface
            )

            if (mostrarCliente && !cita.clienteNombre.isNullOrBlank()) {
                Spacer(modifier = Modifier.height(4.dp))
                Text(
                    text = "Cliente: ${cita.clienteNombre}",
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurface
                )
            }

            TextButton(onClick = { detallesExpandidos = !detallesExpandidos }) {
                Icon(
                    imageVector = if (detallesExpandidos) Icons.Default.ExpandLess else Icons.Default.ExpandMore,
                    contentDescription = null
                )
                Text(if (detallesExpandidos) "Ocultar detalles" else "Ver detalles")
            }

            if (detallesExpandidos) {
                HorizontalDivider()
                Spacer(modifier = Modifier.height(8.dp))
                Text(
                    text = "Horario: ${fechaMostrar.substringAfter(' ')} - ${formatearHora(cita.fechaFin)}",
                    style = MaterialTheme.typography.bodyMedium,
                    fontWeight = FontWeight.SemiBold
                )
                Spacer(modifier = Modifier.height(8.dp))
                Text(
                    text = "Servicios y productos",
                    style = MaterialTheme.typography.titleSmall,
                    fontWeight = FontWeight.Bold
                )
                val detalles = cita.detalles.orEmpty()
                if (detalles.isEmpty()) {
                    Text(
                        text = "La API no devolvió el detalle de esta cita.",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                } else {
                    detalles.forEach { detalle ->
                        val subtotal = detalle.precioUnitario
                            .multiply(java.math.BigDecimal(detalle.cantidad))
                            .subtract(detalle.descuento)
                        Row(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(vertical = 4.dp),
                            horizontalArrangement = Arrangement.SpaceBetween,
                            verticalAlignment = Alignment.Top
                        ) {
                            Column(modifier = Modifier.weight(1f)) {
                                Text(
                                    text = detalle.nombre?.takeIf { it.isNotBlank() }
                                        ?: "Servicio #${detalle.catalogoItemId}",
                                    style = MaterialTheme.typography.bodyMedium
                                )
                                Text(
                                    text = "${detalle.cantidad} × ${formatearMoneda(detalle.precioUnitario, locale)}" +
                                        if (detalle.descuento > java.math.BigDecimal.ZERO) {
                                            " · descuento ${formatearMoneda(detalle.descuento, locale)}"
                                        } else "",
                                    style = MaterialTheme.typography.bodySmall,
                                    color = MaterialTheme.colorScheme.onSurfaceVariant
                                )
                            }
                            Text(
                                text = formatearMoneda(subtotal, locale),
                                style = MaterialTheme.typography.bodyMedium,
                                fontWeight = FontWeight.SemiBold
                            )
                        }
                    }
                }

                if (!cita.notas.isNullOrBlank()) {
                    Spacer(modifier = Modifier.height(8.dp))
                    Text("Notas: ${cita.notas}", style = MaterialTheme.typography.bodySmall)
                }
            }

            Spacer(modifier = Modifier.height(8.dp))

            Text(
                text = "Total: RD$ ${String.format(locale, "%,.2f", cita.total)}",
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.ExtraBold,
                color = Acento
            )

            if (mostrarCliente && !cita.clienteTelefono.isNullOrBlank()) {
                Spacer(modifier = Modifier.height(8.dp))
                Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    OutlinedButton(onClick = {
                        val intent = Intent(Intent.ACTION_DIAL).apply {
                            data = android.net.Uri.parse("tel:${cita.clienteTelefono}")
                        }
                        runCatching { context.startActivity(intent) }
                    }) { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.call)) }
                    OutlinedButton(onClick = {
                        val telefono = cita.clienteTelefono.filter(Char::isDigit)
                        val intent = Intent(Intent.ACTION_VIEW, android.net.Uri.parse("https://wa.me/$telefono"))
                        runCatching { context.startActivity(intent) }
                    }) { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.whatsapp)) }
                }
            }

            if (onCancelar != null || onReprogramar != null || onCalificar != null || onRepetir != null) {
                Spacer(modifier = Modifier.height(8.dp))
                Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    onRepetir?.let { OutlinedButton(onClick = it) { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.repeat_appointment)) } }
                    onReprogramar?.let { OutlinedButton(onClick = it) { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.reschedule)) } }
                    onCancelar?.let { Button(onClick = it) { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.cancel_appointment)) } }
                    onCalificar?.let { OutlinedButton(onClick = it) { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.rate)) } }
                }
            }

            if (onCambiarEstado != null) {
                Spacer(modifier = Modifier.height(8.dp))
                Box {
                    OutlinedButton(onClick = { menuEstados = true }) { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.change_appointment_status)) }
                    DropdownMenu(expanded = menuEstados, onDismissRequest = { menuEstados = false }) {
                        estados.filter { estado -> transicionesDe(cita.estado).contains(normalizarEstadoUi(estado.descripcion)) }
                            .forEach { estado ->
                                DropdownMenuItem(
                                    text = { Text(estado.descripcion) },
                                    onClick = { menuEstados = false; onCambiarEstado(estado.estadoCitaId) }
                                )
                            }
                    }
                }
            }

            if (onCompartir != null || onAgregarCalendario != null) {
                Spacer(modifier = Modifier.height(8.dp))
                Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    onAgregarCalendario?.let { OutlinedButton(onClick = it) { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.add_to_calendar)) } }
                    onCompartir?.let { TextButton(onClick = it) { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.share)) } }
                }
            }
        }
    }
}

private fun normalizarEstadoUi(texto: String?): String = texto.orEmpty()
    .lowercase(java.util.Locale.ROOT).filter(Char::isLetterOrDigit)

private fun transicionesDe(estado: String?): Set<String> = when (normalizarEstadoUi(estado)) {
    "pendiente" -> setOf("confirmada", "cancelada", "noshow")
    "confirmada" -> setOf("enproceso", "cancelada", "noshow")
    "enproceso" -> setOf("completada")
    else -> emptySet()
}

private fun formatearHora(fecha: String): String =
    fecha.substringAfter('T', fecha).take(5).ifBlank { "--:--" }

private fun formatearMoneda(valor: java.math.BigDecimal, locale: java.util.Locale): String =
    "RD$ ${String.format(locale, "%,.2f", valor)}"
