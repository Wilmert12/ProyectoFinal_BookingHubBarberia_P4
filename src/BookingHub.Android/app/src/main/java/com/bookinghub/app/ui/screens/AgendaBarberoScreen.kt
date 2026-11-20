package com.bookinghub.app.ui.screens

import androidx.compose.material3.pulltorefresh.PullToRefreshBox

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
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CalendarMonth
import androidx.compose.material.icons.filled.Event
import androidx.compose.material.icons.filled.History
import androidx.compose.material.icons.filled.MoreVert
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material.icons.filled.Today
import androidx.compose.material3.Button
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.DatePicker
import androidx.compose.material3.DatePickerDialog
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import com.bookinghub.app.ui.theme.Acento
import com.bookinghub.app.ui.viewmodel.AgendaBarberoViewModel
import com.bookinghub.app.util.SesionBarbero
import java.time.Instant
import java.time.LocalDate
import java.time.ZoneOffset
import java.time.format.DateTimeFormatter
import java.util.Locale

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun AgendaBarberoScreen(
    onAbrirAjustes: () -> Unit,
    onSesionExpirada: () -> Unit,
    viewModel: AgendaBarberoViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsState()
    val citasVisibles = if (uiState.vistaSemanal) uiState.citasPorDia.values.flatten() else uiState.citas
    var mostrarSelectorFecha by remember { mutableStateOf(false) }
    var menuAbierto by remember { mutableStateOf(false) }
    var mostrarBloqueos by remember { mutableStateOf(false) }

    LaunchedEffect(uiState.sesionExpirada) {
        if (uiState.sesionExpirada) onSesionExpirada()
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Column {
                        Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.my_schedule))
                        Text(
                            text = SesionBarbero.sesionActual?.nombre.orEmpty(),
                            style = MaterialTheme.typography.labelMedium
                        )
                    }
                },
                actions = {
                    IconButton(onClick = { viewModel.recargar() }) {
                        Icon(Icons.Default.Refresh, contentDescription = "Actualizar agenda")
                    }
                    IconButton(onClick = { menuAbierto = true }) {
                        Icon(Icons.Default.MoreVert, contentDescription = "Más opciones")
                    }
                    DropdownMenu(
                        expanded = menuAbierto,
                        onDismissRequest = { menuAbierto = false }
                    ) {
                        DropdownMenuItem(
                            text = { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.go_to_today)) },
                            leadingIcon = { Icon(Icons.Default.Today, contentDescription = null) },
                            onClick = {
                                menuAbierto = false
                                viewModel.seleccionarFecha(LocalDate.now())
                            }
                        )
                        DropdownMenuItem(
                            text = { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.previous_day)) },
                            leadingIcon = { Icon(Icons.Default.History, contentDescription = null) },
                            onClick = {
                                menuAbierto = false
                                viewModel.seleccionarFecha(uiState.fecha.minusDays(1))
                            }
                        )
                        DropdownMenuItem(
                            text = { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.next_day)) },
                            leadingIcon = { Icon(Icons.Default.CalendarMonth, contentDescription = null) },
                            onClick = {
                                menuAbierto = false
                                viewModel.seleccionarFecha(uiState.fecha.plusDays(1))
                            }
                        )
                        DropdownMenuItem(
                            text = { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.manage_day_blocks)) },
                            leadingIcon = { Icon(Icons.Default.Event, contentDescription = null) },
                            onClick = { menuAbierto = false; mostrarBloqueos = true }
                        )
                        DropdownMenuItem(
                            text = { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.settings)) },
                            leadingIcon = { Icon(Icons.Default.Settings, contentDescription = null) },
                            onClick = {
                                menuAbierto = false
                                onAbrirAjustes()
                            }
                        )
                    }
                }
            )
        }
    ) { paddingValues ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(paddingValues)
                .padding(horizontal = 16.dp)
        ) {
            Spacer(modifier = Modifier.height(12.dp))
            OutlinedButton(
                onClick = { mostrarSelectorFecha = true },
                modifier = Modifier.fillMaxWidth()
            ) {
                Icon(Icons.Default.CalendarMonth, contentDescription = null)
                Spacer(modifier = Modifier.size(8.dp))
                Text(formatearFechaAgenda(uiState.fecha))
            }
            Spacer(modifier = Modifier.height(8.dp))
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp), modifier = Modifier.fillMaxWidth()) {
                if (uiState.vistaSemanal) {
                    OutlinedButton(onClick = { viewModel.alternarVistaSemanal(false) }, modifier = Modifier.weight(1f)) {
                        Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.agenda_day))
                    }
                    Button(onClick = { viewModel.alternarVistaSemanal(true) }, modifier = Modifier.weight(1f)) {
                        Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.agenda_week))
                    }
                } else {
                    Button(onClick = { viewModel.alternarVistaSemanal(false) }, modifier = Modifier.weight(1f)) {
                        Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.agenda_day))
                    }
                    OutlinedButton(onClick = { viewModel.alternarVistaSemanal(true) }, modifier = Modifier.weight(1f)) {
                        Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.agenda_week))
                    }
                }
            }
            Spacer(modifier = Modifier.height(12.dp))

            if (uiState.cargando && citasVisibles.isEmpty()) {
                Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                    CircularProgressIndicator(color = Acento)
                }
            } else if (uiState.error != null && citasVisibles.isEmpty()) {
                Column(
                    modifier = Modifier
                        .fillMaxSize()
                        .padding(24.dp),
                    horizontalAlignment = Alignment.CenterHorizontally,
                    verticalArrangement = Arrangement.Center
                ) {
                    Text(
                        text = uiState.error.orEmpty(),
                        color = MaterialTheme.colorScheme.error,
                        style = MaterialTheme.typography.bodyMedium
                    )
                    Spacer(modifier = Modifier.height(12.dp))
                    Button(onClick = { viewModel.recargar() }) { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.retry)) }
                }
            } else if (citasVisibles.isEmpty()) {
                Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                    Text(
                        text = if (uiState.vistaSemanal) "No tienes citas para esta semana." else "No tienes citas para esta fecha.",
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
            } else {
                uiState.aviso?.let { aviso ->
                    Text(
                        text = aviso,
                        modifier = Modifier.fillMaxWidth().padding(vertical = 6.dp),
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.error
                    )
                }
                if (uiState.cargando) {
                    CircularProgressIndicator(
                        modifier = Modifier
                            .align(Alignment.CenterHorizontally)
                            .size(24.dp),
                        strokeWidth = 2.dp,
                        color = Acento
                    )
                }
                PullToRefreshBox(
                    isRefreshing = uiState.cargando,
                    onRefresh = viewModel::recargar,
                    modifier = Modifier.weight(1f).fillMaxWidth()
                ) {
                LazyColumn(
                    modifier = Modifier.fillMaxSize(),
                    contentPadding = PaddingValues(top = 4.dp, bottom = 20.dp),
                    verticalArrangement = Arrangement.spacedBy(12.dp)
                ) {
                    if (uiState.vistaSemanal) {
                        item {
                            Text(
                                text = "${citasVisibles.size} ${if (citasVisibles.size == 1) "cita" else "citas"} esta semana",
                                style = MaterialTheme.typography.titleSmall,
                                fontWeight = FontWeight.SemiBold
                            )
                        }
                        items(uiState.citasPorDia.toSortedMap().toList(), key = { "dia_${it.first}" }) { (dia, citas) ->
                            Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                                Text(formatearFechaAgenda(dia), style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
                                if (citas.isEmpty()) {
                                    Text("Sin citas", color = MaterialTheme.colorScheme.onSurfaceVariant)
                                } else {
                                    citas.forEach { cita ->
                                        Column(verticalArrangement = Arrangement.spacedBy(6.dp)) {
                                            CitaDetalleCard(
                                                cita = cita,
                                                mostrarCliente = true,
                                                estados = uiState.estados,
                                                onCambiarEstado = { estadoId -> viewModel.cambiarEstado(cita.citaId, estadoId) }
                                            )
                                            if (uiState.citaEnActualizacion == cita.citaId) {
                                                CircularProgressIndicator(Modifier.align(Alignment.CenterHorizontally).size(22.dp), strokeWidth = 2.dp)
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    } else {
                        item {
                            Text(
                                text = "${uiState.citas.size} ${if (uiState.citas.size == 1) "cita" else "citas"} en la agenda",
                                style = MaterialTheme.typography.titleSmall,
                                fontWeight = FontWeight.SemiBold
                            )
                        }
                        items(uiState.citas, key = { it.citaId }) { cita ->
                            Column(verticalArrangement = Arrangement.spacedBy(6.dp)) {
                                CitaDetalleCard(
                                    cita = cita,
                                    mostrarCliente = true,
                                    estados = uiState.estados,
                                    onCambiarEstado = { estadoId -> viewModel.cambiarEstado(cita.citaId, estadoId) }
                                )
                                if (uiState.citaEnActualizacion == cita.citaId) {
                                    CircularProgressIndicator(Modifier.align(Alignment.CenterHorizontally).size(22.dp), strokeWidth = 2.dp)
                                }
                            }
                        }
                    }
                }
                }
            }
        }
    }

    if (mostrarSelectorFecha) {
        val fechaInicialUtc = uiState.fecha.toEpochDay() * MILISEGUNDOS_POR_DIA
        val selectorFecha = androidx.compose.material3.rememberDatePickerState(
            initialSelectedDateMillis = fechaInicialUtc
        )
        DatePickerDialog(
            onDismissRequest = { mostrarSelectorFecha = false },
            confirmButton = {
                TextButton(onClick = {
                    selectorFecha.selectedDateMillis?.let { millis ->
                        viewModel.seleccionarFecha(
                            Instant.ofEpochMilli(millis).atZone(ZoneOffset.UTC).toLocalDate()
                        )
                    }
                    mostrarSelectorFecha = false
                }) { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.view_schedule)) }
            },
            dismissButton = {
                TextButton(onClick = { mostrarSelectorFecha = false }) { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.cancel)) }
            }
        ) {
            DatePicker(state = selectorFecha)
        }
    }

    if (mostrarBloqueos) {
        AlertDialog(
            onDismissRequest = { mostrarBloqueos = false },
            title = { Text("Bloqueos · ${formatearFechaAgenda(uiState.fecha)}") },
            text = {
                Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                    if (uiState.cargandoBloqueos) CircularProgressIndicator(Modifier.size(24.dp))
                    if (uiState.bloqueos.isEmpty() && !uiState.cargandoBloqueos) {
                        Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.no_blocks_today))
                    }
                    uiState.bloqueos.forEach { bloqueo ->
                        Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
                            Column(Modifier.weight(1f)) {
                                Text(bloqueo.tipo, fontWeight = FontWeight.SemiBold)
                                Text("${bloqueo.fechaInicio.replace('T', ' ').take(16)} – ${bloqueo.fechaFin.replace('T', ' ').take(16)}",
                                    style = MaterialTheme.typography.bodySmall)
                            }
                            TextButton(onClick = { viewModel.eliminarBloqueo(bloqueo.indisponibilidadId) }) { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.remove)) }
                        }
                    }
                }
            },
            confirmButton = {
                TextButton(
                    enabled = uiState.bloqueos.isEmpty() && !uiState.cargandoBloqueos,
                    onClick = { viewModel.crearBloqueoDia(uiState.fecha); mostrarBloqueos = false }
                ) { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.block_full_day)) }
            },
            dismissButton = { TextButton(onClick = { mostrarBloqueos = false }) { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.close)) } }
        )
    }
}

private fun formatearFechaAgenda(fecha: LocalDate): String =
    fecha.format(DateTimeFormatter.ofLocalizedDate(java.time.format.FormatStyle.FULL).withLocale(Locale.getDefault()))

private const val MILISEGUNDOS_POR_DIA = 86_400_000L
