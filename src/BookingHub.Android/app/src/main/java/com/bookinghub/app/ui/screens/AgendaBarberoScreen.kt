package com.bookinghub.app.ui.screens

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.KeyboardArrowLeft
import androidx.compose.material.icons.automirrored.filled.KeyboardArrowRight
import androidx.compose.material.icons.filled.CalendarMonth
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.DatePicker
import androidx.compose.material3.DatePickerDialog
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FilterChip
import androidx.compose.material3.FilterChipDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.TopAppBarDefaults
import androidx.compose.material3.rememberDatePickerState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.bookinghub.app.data.model.CitaDto
import com.bookinghub.app.ui.theme.Acento
import com.bookinghub.app.ui.theme.VidrioBorde
import com.bookinghub.app.ui.viewmodel.AgendaBarberoUiState
import com.bookinghub.app.ui.viewmodel.AgendaBarberoViewModel
import com.bookinghub.app.util.EstadosCita
import com.bookinghub.app.util.SesionBarbero
import java.math.BigDecimal
import java.time.Instant
import java.time.LocalDate
import java.time.ZoneOffset
import java.time.format.DateTimeFormatter
import java.util.Locale

private val Espanol = Locale.forLanguageTag("es-DO")

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun AgendaBarberoScreen(
    onAbrirAjustes: () -> Unit,
    onSesionExpirada: () -> Unit,
    viewModel: AgendaBarberoViewModel = viewModel()
) {
    val uiState by viewModel.uiState.collectAsState()
    var mostrarSelectorFecha by remember { mutableStateOf(false) }
    val hoy = LocalDate.now()

    LaunchedEffect(uiState.sesionExpirada) {
        if (uiState.sesionExpirada) onSesionExpirada()
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Column {
                        Text(
                            text = "Hola, ${SesionBarbero.sesionActual?.nombre.orEmpty()}",
                            style = MaterialTheme.typography.labelMedium,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                        Text("Mi agenda", fontWeight = FontWeight.Bold)
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(containerColor = Color.Transparent),
                actions = {
                    IconButton(onClick = { viewModel.recargar() }) {
                        Icon(Icons.Default.Refresh, contentDescription = "Actualizar agenda")
                    }
                    IconButton(onClick = onAbrirAjustes) {
                        Icon(Icons.Default.Settings, contentDescription = "Ajustes")
                    }
                }
            )
        }
    ) { paddingValues ->
        LazyColumn(
            modifier = Modifier
                .fillMaxSize()
                .padding(paddingValues),
            contentPadding = PaddingValues(start = 16.dp, end = 16.dp, top = 4.dp, bottom = 24.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            // Día / Semana y navegación (como en la web)
            item {
                Row(horizontalArrangement = Arrangement.spacedBy(8.dp), verticalAlignment = Alignment.CenterVertically) {
                    ChipVista("Día", seleccionado = !uiState.semana) { viewModel.cambiarVista(false) }
                    ChipVista("Semana", seleccionado = uiState.semana) { viewModel.cambiarVista(true) }
                    Spacer(modifier = Modifier.weight(1f))
                    if (uiState.fecha != hoy) {
                        TextButton(onClick = { viewModel.seleccionarFecha(hoy) }) { Text("Hoy") }
                    }
                }
            }
            item {
                Row(verticalAlignment = Alignment.CenterVertically) {
                    IconButton(onClick = { viewModel.mover(-1) }) {
                        Icon(Icons.AutoMirrored.Filled.KeyboardArrowLeft, contentDescription = "Anterior")
                    }
                    OutlinedButton(
                        onClick = { mostrarSelectorFecha = true },
                        modifier = Modifier.weight(1f),
                        border = BorderStroke(1.dp, VidrioBorde),
                        colors = ButtonDefaults.outlinedButtonColors(containerColor = MaterialTheme.colorScheme.surface)
                    ) {
                        Icon(Icons.Default.CalendarMonth, contentDescription = null, modifier = Modifier.size(18.dp))
                        Spacer(modifier = Modifier.width(8.dp))
                        Text(
                            text = if (uiState.semana) {
                                "Semana del ${uiState.lunes.format(DateTimeFormatter.ofPattern("d MMM", Espanol))} al ${uiState.lunes.plusDays(6).format(DateTimeFormatter.ofPattern("d MMM", Espanol))}"
                            } else {
                                uiState.fecha.format(DateTimeFormatter.ofPattern("EEEE d 'de' MMMM", Espanol)).replaceFirstChar { it.uppercase() }
                            },
                            maxLines = 1,
                            overflow = TextOverflow.Ellipsis
                        )
                    }
                    IconButton(onClick = { viewModel.mover(1) }) {
                        Icon(Icons.AutoMirrored.Filled.KeyboardArrowRight, contentDescription = "Siguiente")
                    }
                }
            }

            // Resumen
            item { Resumen(uiState) }

            uiState.aviso?.let { aviso ->
                item { Text(aviso, style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.error) }
            }
            uiState.mensajeEstado?.let { mensaje ->
                item {
                    Text(
                        text = mensaje,
                        style = MaterialTheme.typography.bodyMedium,
                        color = MaterialTheme.colorScheme.error,
                        modifier = Modifier.clickable { viewModel.cerrarMensajeEstado() }
                    )
                }
            }

            when {
                uiState.cargando && uiState.citas.isEmpty() -> item {
                    Box(modifier = Modifier.fillMaxWidth().padding(32.dp), contentAlignment = Alignment.Center) {
                        CircularProgressIndicator(color = Acento)
                    }
                }
                uiState.error != null && uiState.citas.isEmpty() -> item {
                    Column(
                        modifier = Modifier.fillMaxWidth().padding(24.dp),
                        horizontalAlignment = Alignment.CenterHorizontally
                    ) {
                        Text(uiState.error.orEmpty(), color = MaterialTheme.colorScheme.error)
                        Spacer(modifier = Modifier.height(12.dp))
                        Button(onClick = { viewModel.recargar() }) { Text("Reintentar") }
                    }
                }
                uiState.semana -> {
                    // Un bloque por día, de lunes a domingo; tocar el día abre su vista detallada.
                    items((0L..6L).map { uiState.lunes.plusDays(it) }, key = { it.toString() }) { dia ->
                        DiaDeLaSemana(
                            dia = dia,
                            esHoy = dia == hoy,
                            citas = uiState.citas.filter { it.fechaInicio.startsWith(dia.toString()) },
                            onAbrir = { viewModel.verDia(dia) }
                        )
                    }
                }
                uiState.citas.isEmpty() -> item {
                    Text(
                        text = "No tienes citas este día.",
                        modifier = Modifier.fillMaxWidth().padding(24.dp),
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
                else -> items(uiState.citas, key = { it.citaId }) { cita ->
                    val opciones = EstadosCita.siguientesParaBarbero(cita.estado)
                    CitaDetalleCard(
                        cita = cita,
                        mostrarCliente = true,
                        acciones = if (opciones.isEmpty()) null else {
                            {
                                opciones.forEach { (estado, texto) ->
                                    val color = EstadosCita.color(estado)
                                    OutlinedButton(
                                        onClick = { viewModel.cambiarEstado(cita, estado) },
                                        enabled = uiState.cambiandoId == null,
                                        border = BorderStroke(1.dp, color.copy(alpha = 0.5f)),
                                        colors = ButtonDefaults.outlinedButtonColors(contentColor = color),
                                        contentPadding = PaddingValues(horizontal = 14.dp, vertical = 6.dp)
                                    ) { Text(texto) }
                                }
                            }
                        }
                    )
                }
            }
        }
    }

    if (mostrarSelectorFecha) {
        val selector = rememberDatePickerState(initialSelectedDateMillis = uiState.fecha.toEpochDay() * MILISEGUNDOS_POR_DIA)
        DatePickerDialog(
            onDismissRequest = { mostrarSelectorFecha = false },
            confirmButton = {
                TextButton(onClick = {
                    selector.selectedDateMillis?.let { millis ->
                        viewModel.seleccionarFecha(Instant.ofEpochMilli(millis).atZone(ZoneOffset.UTC).toLocalDate())
                    }
                    mostrarSelectorFecha = false
                }) { Text("Ver agenda") }
            },
            dismissButton = { TextButton(onClick = { mostrarSelectorFecha = false }) { Text("Cancelar") } }
        ) {
            DatePicker(state = selector)
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun ChipVista(texto: String, seleccionado: Boolean, onClick: () -> Unit) {
    FilterChip(
        selected = seleccionado,
        onClick = onClick,
        label = { Text(texto) },
        shape = MaterialTheme.shapes.extraLarge,
        colors = FilterChipDefaults.filterChipColors(
            containerColor = MaterialTheme.colorScheme.surface,
            selectedContainerColor = MaterialTheme.colorScheme.primary,
            selectedLabelColor = MaterialTheme.colorScheme.onPrimary
        ),
        border = FilterChipDefaults.filterChipBorder(
            enabled = true,
            selected = seleccionado,
            borderColor = VidrioBorde
        )
    )
}

@Composable
private fun Resumen(estado: AgendaBarberoUiState) {
    Row(horizontalArrangement = Arrangement.spacedBy(10.dp)) {
        Dato("Citas", estado.activas.size.toString(), Modifier.weight(1f))
        Dato("Completadas", estado.completadas.toString(), Modifier.weight(1f))
        Dato(if (estado.semana) "Total semana" else "Total del día", pesos(estado.total), Modifier.weight(1.4f))
    }
}

@Composable
private fun Dato(titulo: String, valor: String, modifier: Modifier = Modifier) {
    Card(
        modifier = modifier,
        shape = MaterialTheme.shapes.large,
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface),
        border = BorderStroke(1.dp, VidrioBorde)
    ) {
        Column(modifier = Modifier.padding(horizontal = 14.dp, vertical = 12.dp)) {
            Text(titulo, style = MaterialTheme.typography.labelMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
            Text(valor, style = MaterialTheme.typography.titleLarge, fontWeight = FontWeight.SemiBold, maxLines = 1)
        }
    }
}

@Composable
private fun DiaDeLaSemana(dia: LocalDate, esHoy: Boolean, citas: List<CitaDto>, onAbrir: () -> Unit) {
    Card(
        modifier = Modifier.fillMaxWidth().clickable(onClick = onAbrir),
        shape = MaterialTheme.shapes.large,
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface),
        border = BorderStroke(if (esHoy) 1.5.dp else 1.dp, if (esHoy) Acento else VidrioBorde)
    ) {
        Column(modifier = Modifier.padding(14.dp), verticalArrangement = Arrangement.spacedBy(6.dp)) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Text(
                    text = dia.format(DateTimeFormatter.ofPattern("EEEE d 'de' MMMM", Espanol)).replaceFirstChar { it.uppercase() },
                    style = MaterialTheme.typography.titleSmall,
                    fontWeight = FontWeight.SemiBold,
                    modifier = Modifier.weight(1f)
                )
                val activas = citas.count { !EstadosCita.estaLibre(it.estado) }
                Text(
                    text = if (activas == 1) "1 cita" else "$activas citas",
                    style = MaterialTheme.typography.labelMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }
            if (citas.isEmpty()) {
                Text("Libre", style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
            }
            citas.forEach { cita ->
                val color = EstadosCita.color(cita.estado)
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Text(
                        text = cita.fechaInicio.substringAfter('T').take(5),
                        style = MaterialTheme.typography.bodyMedium,
                        fontWeight = FontWeight.SemiBold,
                        modifier = Modifier.width(52.dp)
                    )
                    Text(
                        text = cita.clienteNombre.orEmpty(),
                        style = MaterialTheme.typography.bodyMedium,
                        maxLines = 1,
                        overflow = TextOverflow.Ellipsis,
                        modifier = Modifier.weight(1f),
                        color = if (EstadosCita.estaLibre(cita.estado)) MaterialTheme.colorScheme.onSurfaceVariant else MaterialTheme.colorScheme.onSurface
                    )
                    Text(
                        text = EstadosCita.texto(cita.estado),
                        style = MaterialTheme.typography.labelSmall,
                        fontWeight = FontWeight.SemiBold,
                        color = color
                    )
                }
            }
        }
    }
}

private fun pesos(valor: BigDecimal): String = "RD$" + String.format(Espanol, "%,.2f", valor)

private const val MILISEGUNDOS_POR_DIA = 86_400_000L
