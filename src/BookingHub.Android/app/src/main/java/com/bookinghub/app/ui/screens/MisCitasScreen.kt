package com.bookinghub.app.ui.screens

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
import androidx.lifecycle.viewmodel.compose.viewModel
import com.bookinghub.app.data.model.CitaDto
import com.bookinghub.app.ui.theme.Acento
import com.bookinghub.app.ui.theme.Borde
import com.bookinghub.app.ui.viewmodel.CitasViewModel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MisCitasScreen(
    viewModel: CitasViewModel = viewModel()
) {
    val uiState by viewModel.uiState.collectAsState()

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Mis Citas", fontWeight = FontWeight.Bold) },
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
                uiState.cargando -> {
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
                                Text("Reintentar")
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
                        Text(
                            text = "Para cancelar una cita, comunícate directamente con la barbería.",
                            modifier = Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 8.dp),
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                        LazyColumn(
                            modifier = Modifier.fillMaxSize(),
                            contentPadding = PaddingValues(16.dp),
                            verticalArrangement = Arrangement.spacedBy(12.dp)
                        ) {
                            items(uiState.citas, key = { it.citaId }) { cita ->
                                CitaDetalleCard(cita = cita)
                            }
                        }
                    }
                }
            }
        }
    }
}

/** Tarjeta reutilizable para que cliente y barbero consulten los mismos detalles de una cita. */
@Composable
fun CitaDetalleCard(cita: CitaDto, mostrarCliente: Boolean = false) {
    var detallesExpandidos by remember(cita.citaId) { mutableStateOf(false) }
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
                                    text = "${detalle.cantidad} × ${formatearMoneda(detalle.precioUnitario)}" +
                                        if (detalle.descuento > java.math.BigDecimal.ZERO) {
                                            " · descuento ${formatearMoneda(detalle.descuento)}"
                                        } else "",
                                    style = MaterialTheme.typography.bodySmall,
                                    color = MaterialTheme.colorScheme.onSurfaceVariant
                                )
                            }
                            Text(
                                text = formatearMoneda(subtotal),
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
                text = "Total: RD$ ${String.format(java.util.Locale("es", "DO"), "%.2f", cita.total)}",
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.ExtraBold,
                color = Acento
            )
        }
    }
}

private fun formatearHora(fecha: String): String =
    fecha.substringAfter('T', fecha).take(5).ifBlank { "--:--" }

private fun formatearMoneda(valor: java.math.BigDecimal): String =
    "RD$ ${String.format(java.util.Locale("es", "DO"), "%.2f", valor)}"
