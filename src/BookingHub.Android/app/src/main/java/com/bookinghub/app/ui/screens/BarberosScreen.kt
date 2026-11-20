package com.bookinghub.app.ui.screens

import androidx.compose.material3.pulltorefresh.PullToRefreshBox

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.ImageBitmap
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import com.bookinghub.app.data.model.BarberoDto
import com.bookinghub.app.ui.theme.Acento
import com.bookinghub.app.ui.theme.Borde
import com.bookinghub.app.ui.theme.VerdeExito
import com.bookinghub.app.ui.theme.RojoError
import com.bookinghub.app.ui.viewmodel.BarberosViewModel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun BarberosScreen(
    viewModel: BarberosViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsState()

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.our_barbers), fontWeight = FontWeight.Bold) },
                actions = {
                    IconButton(onClick = { viewModel.cargarBarberos() }) {
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
                uiState.cargando && uiState.barberos.isEmpty() -> {
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
                            Button(onClick = { viewModel.cargarBarberos() }) {
                                Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.retry))
                            }
                        }
                    }
                }
                uiState.barberos.isEmpty() -> {
                    Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                        Text(
                            text = "No hay barberos registrados",
                            style = MaterialTheme.typography.bodyLarge,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }
                }
                else -> {
                    if (uiState.cargando) LinearProgressIndicator(Modifier.fillMaxWidth())
                    uiState.aviso?.let { aviso ->
                        Text(aviso, Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 8.dp),
                            color = MaterialTheme.colorScheme.error, style = MaterialTheme.typography.bodySmall)
                    }
                    PullToRefreshBox(
                        isRefreshing = uiState.cargando,
                        onRefresh = viewModel::cargarBarberos,
                        modifier = Modifier.fillMaxSize()
                    ) {
                    LazyColumn(
                        modifier = Modifier.fillMaxSize(),
                        contentPadding = PaddingValues(16.dp),
                        verticalArrangement = Arrangement.spacedBy(12.dp)
                    ) {
                        items(uiState.barberos, key = { it.barberoId }) { barbero ->
                            BarberoCard(barbero = barbero, favorito = barbero.barberoId in uiState.favoritos,
                                foto = uiState.fotos[barbero.barberoId],
                                proximoHorario = uiState.proximosHorarios[barbero.barberoId],
                                onFavorito = { viewModel.alternarFavorito(barbero.barberoId) })
                        }
                    }
                    }
                }
            }
        }
    }
}

@Composable
private fun BarberoCard(
    barbero: BarberoDto,
    favorito: Boolean,
    foto: ImageBitmap?,
    proximoHorario: String?,
    onFavorito: () -> Unit
) {
    val barberoColor = remember(barbero.colorHex) {
        try {
            Color(android.graphics.Color.parseColor(barbero.colorHex))
        } catch (_: Exception) {
            Acento
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
        Row(
            modifier = Modifier
                .padding(16.dp)
                .fillMaxWidth(),
            verticalAlignment = Alignment.CenterVertically
        ) {
            // Avatar con color distintivo del barbero
            Box(
                modifier = Modifier
                    .size(54.dp)
                    .clip(CircleShape)
                    .background(barberoColor),
                contentAlignment = Alignment.Center
            ) {
                if (foto != null) Image(foto, contentDescription = "Foto de ${barbero.nombre}", modifier = Modifier.size(54.dp))
                else Text(
                    text = (barbero.nombre?.firstOrNull() ?: 'B').uppercase(),
                    color = Color.White,
                    style = MaterialTheme.typography.titleLarge,
                    fontWeight = FontWeight.Bold
                )
            }

            Spacer(modifier = Modifier.width(16.dp))

            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = barbero.nombre ?: "Barbero sin nombre",
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold,
                    color = MaterialTheme.colorScheme.onSurface
                )

                if (!barbero.apodo.isNullOrBlank()) {
                    Spacer(modifier = Modifier.height(2.dp))
                    Text(
                        text = "\"${barbero.apodo}\"",
                        style = MaterialTheme.typography.bodyMedium,
                        color = Acento,
                        fontWeight = FontWeight.SemiBold
                    )
                }

                Spacer(modifier = Modifier.height(6.dp))

                Text(
                    text = if (barbero.totalResenas > 0) "★ ${String.format(java.util.Locale.US, "%.1f", barbero.calificacionPromedio)} · ${barbero.totalResenas} reseñas" else "Sin reseñas aún",
                    style = MaterialTheme.typography.bodySmall,
                    color = Acento
                )
                if (!barbero.especialidades.isNullOrBlank()) {
                    Text(barbero.especialidades, style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant)
                }
                Spacer(modifier = Modifier.height(4.dp))
                Text(
                    text = proximoHorario?.let { "Próximo espacio: $it" }
                        ?: androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.check_slots_when_booking),
                    style = MaterialTheme.typography.bodySmall,
                    color = if (proximoHorario != null) Acento else MaterialTheme.colorScheme.onSurfaceVariant
                )

                Row(verticalAlignment = Alignment.CenterVertically) {
                    Box(
                        modifier = Modifier
                            .size(8.dp)
                            .clip(CircleShape)
                            .background(if (barbero.activo) VerdeExito else RojoError)
                    )
                    Spacer(modifier = Modifier.width(6.dp))
                    Text(
                        text = if (barbero.activo) "Activo" else "Inactivo",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
            }
            IconButton(onClick = onFavorito) {
                Icon(if (favorito) Icons.Default.Favorite else Icons.Default.FavoriteBorder,
                    contentDescription = if (favorito) "Quitar de favoritos" else "Agregar a favoritos",
                    tint = if (favorito) Acento else MaterialTheme.colorScheme.onSurfaceVariant)
            }
        }
    }
}
