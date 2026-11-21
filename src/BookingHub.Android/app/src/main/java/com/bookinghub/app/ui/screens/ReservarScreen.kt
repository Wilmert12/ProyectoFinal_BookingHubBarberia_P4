package com.bookinghub.app.ui.screens

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.bookinghub.app.ui.theme.Acento
import com.bookinghub.app.ui.theme.Borde
import com.bookinghub.app.ui.theme.VidrioBorde
import com.bookinghub.app.ui.viewmodel.ReservarViewModel
import java.time.LocalDate
import java.time.LocalDateTime
import java.time.format.DateTimeFormatter
import java.util.Locale

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ReservarScreen(
    onCitaCreada: () -> Unit,
    viewModel: ReservarViewModel = viewModel()
) {
    val uiState by viewModel.uiState.collectAsState()
    val scrollState = rememberScrollState()

    LaunchedEffect(uiState.citaCreada) {
        if (uiState.citaCreada) {
            onCitaCreada()
        }
    }

    val proximosDias = remember {
        val hoy = LocalDate.now()
        (0..6).map { hoy.plusDays(it.toLong()) }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Reservar Cita", fontWeight = FontWeight.Bold) }
            )
        }
    ) { paddingValues ->
        when {
            uiState.cargandoDatos -> {
                Box(
                    modifier = Modifier.fillMaxSize().padding(paddingValues),
                    contentAlignment = Alignment.Center
                ) {
                    CircularProgressIndicator(color = Acento)
                }
            }
            uiState.errorCargaDatos != null -> {
                Box(
                    modifier = Modifier.fillMaxSize().padding(paddingValues).padding(24.dp),
                    contentAlignment = Alignment.Center
                ) {
                    Column(horizontalAlignment = Alignment.CenterHorizontally) {
                        Text(
                            text = uiState.errorCargaDatos.orEmpty(),
                            color = MaterialTheme.colorScheme.error,
                            style = MaterialTheme.typography.bodyMedium
                        )
                        Spacer(modifier = Modifier.height(16.dp))
                        Button(onClick = { viewModel.cargarDatos() }) { Text("Reintentar") }
                    }
                }
            }
            else -> {
            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(paddingValues)
                    .padding(horizontal = 16.dp)
                    .verticalScroll(scrollState)
            ) {
                Spacer(modifier = Modifier.height(8.dp))

                // 1. SELECCIONAR BARBERO
                Text(
                    text = "1. Selecciona tu Barbero",
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold,
                    color = MaterialTheme.colorScheme.onBackground
                )
                Spacer(modifier = Modifier.height(8.dp))

                LazyRow(
                    horizontalArrangement = Arrangement.spacedBy(8.dp),
                    modifier = Modifier.fillMaxWidth()
                ) {
                    items(uiState.barberos, key = { it.barberoId }) { barbero ->
                        val seleccionado = uiState.barberoSeleccionado?.barberoId == barbero.barberoId
                        FilterChip(
                            selected = seleccionado,
                            onClick = { viewModel.seleccionarBarbero(barbero) },
                            label = { Text(barbero.nombre ?: "Barbero") },
                            border = BorderStroke(1.dp, if (seleccionado) Acento else VidrioBorde),
                            colors = FilterChipDefaults.filterChipColors(
                                selectedContainerColor = Acento.copy(alpha = 0.2f),
                                selectedLabelColor = MaterialTheme.colorScheme.onBackground
                            ),
                            leadingIcon = if (seleccionado) {
                                { Icon(Icons.Default.Check, contentDescription = null, modifier = Modifier.size(16.dp), tint = Acento) }
                            } else null
                        )
                    }
                }
                if (uiState.barberos.isEmpty()) {
                    Text("No hay barberos disponibles.", color = MaterialTheme.colorScheme.onSurfaceVariant)
                }

                Spacer(modifier = Modifier.height(20.dp))

                // 2. SELECCIONAR SERVICIOS
                Text(
                    text = "2. Selecciona los Servicios",
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold,
                    color = MaterialTheme.colorScheme.onBackground
                )
                Spacer(modifier = Modifier.height(8.dp))

                uiState.servicios.forEach { servicio ->
                    val seleccionado = uiState.serviciosSeleccionados.any { it.catalogoItemId == servicio.catalogoItemId }
                    Card(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(vertical = 4.dp)
                            .clickable { viewModel.toggleServicio(servicio) },
                        shape = RoundedCornerShape(10.dp),
                        colors = CardDefaults.cardColors(
                            containerColor = MaterialTheme.colorScheme.surface
                        ),
                        border = BorderStroke(if (seleccionado) 1.5.dp else 1.dp, if (seleccionado) Acento else VidrioBorde)
                    ) {
                        Row(
                            modifier = Modifier
                                .padding(12.dp)
                                .fillMaxWidth(),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Checkbox(
                                checked = seleccionado,
                                onCheckedChange = { viewModel.toggleServicio(servicio) },
                                colors = CheckboxDefaults.colors(
                                    checkedColor = Acento
                                )
                            )
                            Spacer(modifier = Modifier.width(8.dp))
                            Column(modifier = Modifier.weight(1f)) {
                                Text(
                                    text = servicio.nombre,
                                    style = MaterialTheme.typography.bodyLarge,
                                    fontWeight = FontWeight.SemiBold,
                                    color = MaterialTheme.colorScheme.onSurface
                                )
                                Text(
                                    text = "${servicio.duracionMin ?: 30} min",
                                    style = MaterialTheme.typography.bodySmall,
                                    color = MaterialTheme.colorScheme.onSurfaceVariant
                                )
                            }
                            Text(
                                text = "RD$ ${String.format(Locale.forLanguageTag("es-DO"), "%.2f", servicio.precio)}",
                                style = MaterialTheme.typography.titleMedium,
                                fontWeight = FontWeight.Bold,
                                color = Acento
                            )
                        }
                    }
                }
                if (uiState.servicios.isEmpty()) {
                    Text("No hay servicios disponibles.", color = MaterialTheme.colorScheme.onSurfaceVariant)
                }

                Spacer(modifier = Modifier.height(20.dp))

                // 3. SELECCIONAR DÍA
                Text(
                    text = "3. Selecciona la Fecha",
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold,
                    color = MaterialTheme.colorScheme.onBackground
                )
                Spacer(modifier = Modifier.height(8.dp))

                val diaFormatter = remember { DateTimeFormatter.ofPattern("EEE d", Locale.forLanguageTag("es")) }
                LazyRow(
                    horizontalArrangement = Arrangement.spacedBy(8.dp),
                    modifier = Modifier.fillMaxWidth()
                ) {
                    items(proximosDias) { fecha ->
                        val seleccionado = uiState.fechaSeleccionada == fecha
                        FilterChip(
                            selected = seleccionado,
                            onClick = { viewModel.seleccionarFecha(fecha) },
                            label = { Text(fecha.format(diaFormatter).replaceFirstChar { it.uppercase() }) },
                            border = BorderStroke(1.dp, if (seleccionado) Acento else VidrioBorde),
                            colors = FilterChipDefaults.filterChipColors(
                                selectedContainerColor = Acento.copy(alpha = 0.2f),
                                selectedLabelColor = MaterialTheme.colorScheme.onBackground
                            ),
                            leadingIcon = if (seleccionado) {
                                { Icon(Icons.Default.Check, contentDescription = null, modifier = Modifier.size(16.dp), tint = Acento) }
                            } else null
                        )
                    }
                }

                Spacer(modifier = Modifier.height(20.dp))

                // 4. SELECCIONAR HORA
                Text(
                    text = "4. Selecciona la Hora",
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold,
                    color = MaterialTheme.colorScheme.onBackground
                )
                Spacer(modifier = Modifier.height(8.dp))

                when {
                    uiState.cargandoDisponibilidad -> {
                        Row(verticalAlignment = Alignment.CenterVertically) {
                            CircularProgressIndicator(modifier = Modifier.size(20.dp), color = Acento, strokeWidth = 2.dp)
                            Spacer(modifier = Modifier.width(10.dp))
                            Text("Consultando horarios…", color = MaterialTheme.colorScheme.onSurfaceVariant)
                        }
                    }
                    uiState.errorDisponibilidad != null -> {
                        Column {
                            Text(uiState.errorDisponibilidad.orEmpty(), color = MaterialTheme.colorScheme.error)
                            TextButton(onClick = { viewModel.recargarDisponibilidad() }) { Text("Reintentar horarios") }
                        }
                    }
                    uiState.barberoSeleccionado == null || uiState.fechaSeleccionada == null || uiState.serviciosSeleccionados.isEmpty() -> {
                        Text("Selecciona barbero, servicios y fecha para consultar horarios.", color = MaterialTheme.colorScheme.onSurfaceVariant)
                    }
                    uiState.horariosDisponibles.isEmpty() -> {
                        Text("No hay horarios disponibles para esta selección.", color = MaterialTheme.colorScheme.onSurfaceVariant)
                    }
                    else -> {
                        val horaFormatter = remember { DateTimeFormatter.ofPattern("HH:mm") }
                        LazyRow(
                            horizontalArrangement = Arrangement.spacedBy(8.dp),
                            modifier = Modifier.fillMaxWidth()
                        ) {
                            items(uiState.horariosDisponibles, key = { it.inicio }) { horario ->
                                val seleccionado = uiState.horarioSeleccionado?.inicio == horario.inicio
                                val hora = runCatching {
                                    LocalDateTime.parse(horario.inicio.take(19), DateTimeFormatter.ISO_LOCAL_DATE_TIME)
                                        .format(horaFormatter)
                                }.getOrElse { horario.inicio.substringAfter('T').take(5) }
                                FilterChip(
                                    selected = seleccionado,
                                    onClick = { viewModel.seleccionarHorario(horario) },
                                    label = { Text(hora) },
                                    border = BorderStroke(1.dp, if (seleccionado) Acento else VidrioBorde),
                                    colors = FilterChipDefaults.filterChipColors(
                                        selectedContainerColor = Acento.copy(alpha = 0.2f),
                                        selectedLabelColor = MaterialTheme.colorScheme.onBackground
                                    ),
                                    leadingIcon = if (seleccionado) {
                                        { Icon(Icons.Default.Check, contentDescription = null, modifier = Modifier.size(16.dp), tint = Acento) }
                                    } else null
                                )
                            }
                        }
                    }
                }

                Spacer(modifier = Modifier.height(20.dp))

                // 5. NOTAS
                Text(
                    text = "5. Notas adicionales (opcional)",
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold,
                    color = MaterialTheme.colorScheme.onBackground
                )
                Spacer(modifier = Modifier.height(8.dp))

                OutlinedTextField(
                    value = uiState.notas,
                    onValueChange = { viewModel.actualizarNotas(it) },
                    label = { Text("Instrucciones o preferencias") },
                    shape = RoundedCornerShape(10.dp),
                    modifier = Modifier.fillMaxWidth(),
                    maxLines = 3
                )

                Spacer(modifier = Modifier.height(20.dp))

                // RESUMEN
                if (uiState.serviciosSeleccionados.isNotEmpty() && uiState.barberoSeleccionado != null) {
                    Card(
                        modifier = Modifier.fillMaxWidth(),
                        shape = RoundedCornerShape(10.dp),
                        colors = CardDefaults.cardColors(
                            containerColor = MaterialTheme.colorScheme.surface
                        ),
                        border = BorderStroke(1.dp, Acento)
                    ) {
                        Column(modifier = Modifier.padding(16.dp)) {
                            Text(
                                text = "Resumen de la Cita",
                                style = MaterialTheme.typography.titleMedium,
                                fontWeight = FontWeight.Bold,
                                color = MaterialTheme.colorScheme.onSurface
                            )
                            Spacer(modifier = Modifier.height(8.dp))
                            Text("Barbero: ${uiState.barberoSeleccionado?.nombre}")
                            Text("Duración estimada: ${uiState.duracionTotalMin} minutos")
                            Text(
                                text = "Total estimado: RD$ ${String.format(Locale.forLanguageTag("es-DO"), "%.2f", uiState.totalEstimado)}",
                                fontWeight = FontWeight.Bold,
                                color = Acento
                            )
                        }
                    }
                    Spacer(modifier = Modifier.height(16.dp))
                }

                if (uiState.error != null) {
                    Text(
                        text = uiState.error!!,
                        color = MaterialTheme.colorScheme.error,
                        style = MaterialTheme.typography.bodyMedium
                    )
                    Spacer(modifier = Modifier.height(8.dp))
                }

                // BOTÓN CREAR CITA
                val formularioValido = uiState.barberoSeleccionado != null &&
                        uiState.serviciosSeleccionados.isNotEmpty() &&
                        uiState.fechaSeleccionada != null &&
                        uiState.horarioSeleccionado != null &&
                        !uiState.cargandoDisponibilidad &&
                        !uiState.enviandoReserva

                Button(
                    onClick = { viewModel.crearCita() },
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(50.dp),
                    shape = RoundedCornerShape(10.dp),
                    enabled = formularioValido
                ) {
                    if (uiState.enviandoReserva) {
                        CircularProgressIndicator(
                            modifier = Modifier.size(24.dp),
                            color = MaterialTheme.colorScheme.onPrimary
                        )
                    } else {
                        Text(
                            text = "Confirmar Reserva",
                            style = MaterialTheme.typography.labelLarge,
                            fontWeight = FontWeight.Bold
                        )
                    }
                }

                Spacer(modifier = Modifier.height(24.dp))
            }
        }
        }
    }
}
