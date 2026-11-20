package com.bookinghub.app.ui.screens

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
import androidx.compose.material.icons.filled.History
import androidx.compose.material.icons.filled.MoreVert
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material.icons.filled.Today
import androidx.compose.material3.Button
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
import androidx.lifecycle.viewmodel.compose.viewModel
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
    viewModel: AgendaBarberoViewModel = viewModel()
) {
    val uiState by viewModel.uiState.collectAsState()
    var mostrarSelectorFecha by remember { mutableStateOf(false) }
    var menuAbierto by remember { mutableStateOf(false) }

    LaunchedEffect(uiState.sesionExpirada) {
        if (uiState.sesionExpirada) onSesionExpirada()
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Column {
                        Text("Mi agenda")
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
                            text = { Text("Ir a hoy") },
                            leadingIcon = { Icon(Icons.Default.Today, contentDescription = null) },
                            onClick = {
                                menuAbierto = false
                                viewModel.seleccionarFecha(LocalDate.now())
                            }
                        )
                        DropdownMenuItem(
                            text = { Text("Día anterior") },
                            leadingIcon = { Icon(Icons.Default.History, contentDescription = null) },
                            onClick = {
                                menuAbierto = false
                                viewModel.seleccionarFecha(uiState.fecha.minusDays(1))
                            }
                        )
                        DropdownMenuItem(
                            text = { Text("Día siguiente") },
                            leadingIcon = { Icon(Icons.Default.CalendarMonth, contentDescription = null) },
                            onClick = {
                                menuAbierto = false
                                viewModel.seleccionarFecha(uiState.fecha.plusDays(1))
                            }
                        )
                        DropdownMenuItem(
                            text = { Text("Ajustes") },
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
            Spacer(modifier = Modifier.height(12.dp))

            if (uiState.cargando && uiState.citas.isEmpty()) {
                Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                    CircularProgressIndicator(color = Acento)
                }
            } else if (uiState.error != null && uiState.citas.isEmpty()) {
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
                    Button(onClick = { viewModel.recargar() }) { Text("Reintentar") }
                }
            } else if (uiState.citas.isEmpty()) {
                Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                    Text(
                        text = "No tienes citas para esta fecha.",
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
                LazyColumn(
                    contentPadding = PaddingValues(top = 4.dp, bottom = 20.dp),
                    verticalArrangement = Arrangement.spacedBy(12.dp)
                ) {
                    item {
                        Text(
                            text = "${uiState.citas.size} ${if (uiState.citas.size == 1) "cita" else "citas"} en la agenda",
                            style = MaterialTheme.typography.titleSmall,
                            fontWeight = FontWeight.SemiBold
                        )
                    }
                    items(uiState.citas, key = { it.citaId }) { cita ->
                        CitaDetalleCard(cita = cita, mostrarCliente = true)
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
                }) { Text("Ver agenda") }
            },
            dismissButton = {
                TextButton(onClick = { mostrarSelectorFecha = false }) { Text("Cancelar") }
            }
        ) {
            DatePicker(state = selectorFecha)
        }
    }
}

private fun formatearFechaAgenda(fecha: LocalDate): String =
    fecha.format(DateTimeFormatter.ofPattern("EEEE d 'de' MMMM, yyyy", Locale("es", "DO")))

private const val MILISEGUNDOS_POR_DIA = 86_400_000L
