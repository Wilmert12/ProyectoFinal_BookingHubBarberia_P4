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
import java.util.Locale
import com.bookinghub.app.data.model.CatalogoItemDto
import com.bookinghub.app.ui.theme.Acento
import com.bookinghub.app.ui.theme.Borde
import com.bookinghub.app.ui.viewmodel.CatalogoViewModel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun CatalogoScreen(
    viewModel: CatalogoViewModel = viewModel()
) {
    val uiState by viewModel.uiState.collectAsState()
    var filtroSeleccionado by remember { mutableStateOf(0) } // 0: Todos, 1: Servicios, 2: Productos

    val itemsFiltrados = remember(uiState.items, filtroSeleccionado) {
        when (filtroSeleccionado) {
            1 -> uiState.items.filter { it.esServicio }
            2 -> uiState.items.filter { !it.esServicio }
            else -> uiState.items
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Catálogo de Servicios", fontWeight = FontWeight.Bold) },
                actions = {
                    IconButton(onClick = { viewModel.cargarCatalogo() }) {
                        Icon(Icons.Default.Refresh, contentDescription = "Recargar")
                    }
                }
            )
        }
    ) { paddingValues ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(paddingValues)
        ) {
            // Pestañas de filtro
            TabRow(
                selectedTabIndex = filtroSeleccionado,
                modifier = Modifier.fillMaxWidth(),
                containerColor = MaterialTheme.colorScheme.surface,
                contentColor = MaterialTheme.colorScheme.onSurface
            ) {
                Tab(
                    selected = filtroSeleccionado == 0,
                    onClick = { filtroSeleccionado = 0 },
                    text = { Text("Todos", fontWeight = if (filtroSeleccionado == 0) FontWeight.Bold else FontWeight.Normal) }
                )
                Tab(
                    selected = filtroSeleccionado == 1,
                    onClick = { filtroSeleccionado = 1 },
                    text = { Text("Servicios", fontWeight = if (filtroSeleccionado == 1) FontWeight.Bold else FontWeight.Normal) }
                )
                Tab(
                    selected = filtroSeleccionado == 2,
                    onClick = { filtroSeleccionado = 2 },
                    text = { Text("Productos", fontWeight = if (filtroSeleccionado == 2) FontWeight.Bold else FontWeight.Normal) }
                )
            }

            when {
                uiState.cargando -> {
                    Box(
                        modifier = Modifier.fillMaxSize(),
                        contentAlignment = Alignment.Center
                    ) {
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
                            Button(onClick = { viewModel.cargarCatalogo() }) {
                                Text("Reintentar")
                            }
                        }
                    }
                }
                itemsFiltrados.isEmpty() -> {
                    Box(
                        modifier = Modifier.fillMaxSize(),
                        contentAlignment = Alignment.Center
                    ) {
                        Text(
                            text = "No hay elementos en esta categoría",
                            style = MaterialTheme.typography.bodyLarge,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }
                }
                else -> {
                    LazyColumn(
                        modifier = Modifier.fillMaxSize(),
                        contentPadding = PaddingValues(16.dp),
                        verticalArrangement = Arrangement.spacedBy(12.dp)
                    ) {
                        items(itemsFiltrados, key = { it.catalogoItemId }) { item ->
                            CatalogoItemCard(item = item)
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun CatalogoItemCard(item: CatalogoItemDto) {
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
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    text = item.nombre,
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold,
                    color = MaterialTheme.colorScheme.onSurface,
                    modifier = Modifier.weight(1f)
                )

                Text(
                    text = "RD$ ${String.format(Locale("es", "DO"), "%.2f", item.precio)}",
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.ExtraBold,
                    color = Acento
                )
            }

            if (!item.descripcion.isNullOrBlank()) {
                Spacer(modifier = Modifier.height(4.dp))
                Text(
                    text = item.descripcion,
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }

            Spacer(modifier = Modifier.height(10.dp))

            Row(
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                SuggestionChip(
                    onClick = {},
                    label = { Text(if (item.esServicio) "Servicio" else "Producto") },
                    border = BorderStroke(1.dp, Borde)
                )

                if (item.esServicio && item.duracionMin != null) {
                    AssistChip(
                        onClick = {},
                        label = { Text("${item.duracionMin} min") },
                        border = BorderStroke(1.dp, Borde),
                        leadingIcon = {
                            Icon(
                                Icons.Default.Schedule,
                                contentDescription = null,
                                modifier = Modifier.size(16.dp),
                                tint = Acento
                            )
                        }
                    )
                }
            }
        }
    }
}
