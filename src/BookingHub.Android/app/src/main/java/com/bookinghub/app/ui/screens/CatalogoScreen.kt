package com.bookinghub.app.ui.screens

import androidx.compose.material3.pulltorefresh.PullToRefreshBox

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.Sort
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalLocale
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import com.bookinghub.app.data.model.CatalogoItemDto
import com.bookinghub.app.ui.theme.Acento
import com.bookinghub.app.ui.theme.Borde
import com.bookinghub.app.ui.viewmodel.CatalogoViewModel
import kotlinx.coroutines.delay

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun CatalogoScreen(
    viewModel: CatalogoViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsState()
    var filtroSeleccionado by remember { mutableStateOf(0) } // 0: Todos, 1: Servicios, 2: Productos
    var busqueda by remember { mutableStateOf("") }
    var orden by remember { mutableStateOf("nombre") }
    var mostrarOrden by remember { mutableStateOf(false) }

    LaunchedEffect(busqueda, filtroSeleccionado, orden) {
        delay(250)
        viewModel.cargarCatalogo(busqueda, when (filtroSeleccionado) { 1 -> true; 2 -> false; else -> null }, orden)
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.service_catalog), fontWeight = FontWeight.Bold) },
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
            PrimaryTabRow(
                selectedTabIndex = filtroSeleccionado,
                modifier = Modifier.fillMaxWidth(),
                containerColor = MaterialTheme.colorScheme.surface,
                contentColor = MaterialTheme.colorScheme.onSurface
            ) {
                Tab(
                    selected = filtroSeleccionado == 0,
                    onClick = { filtroSeleccionado = 0 },
                    text = { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.all_items), fontWeight = if (filtroSeleccionado == 0) FontWeight.Bold else FontWeight.Normal) }
                )
                Tab(
                    selected = filtroSeleccionado == 1,
                    onClick = { filtroSeleccionado = 1 },
                    text = { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.services), fontWeight = if (filtroSeleccionado == 1) FontWeight.Bold else FontWeight.Normal) }
                )
                Tab(
                    selected = filtroSeleccionado == 2,
                    onClick = { filtroSeleccionado = 2 },
                    text = { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.products), fontWeight = if (filtroSeleccionado == 2) FontWeight.Bold else FontWeight.Normal) }
                )
            }

            Row(Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 8.dp), verticalAlignment = Alignment.CenterVertically) {
                OutlinedTextField(
                    value = busqueda,
                    onValueChange = { busqueda = it },
                    modifier = Modifier.weight(1f),
                    singleLine = true,
                    label = { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.search_services_products)) },
                    leadingIcon = { Icon(Icons.Default.Search, contentDescription = null) }
                )
                Box {
                    IconButton(onClick = { mostrarOrden = true }) { Icon(Icons.AutoMirrored.Filled.Sort, contentDescription = "Ordenar") }
                    DropdownMenu(expanded = mostrarOrden, onDismissRequest = { mostrarOrden = false }) {
                        listOf("nombre" to "Nombre", "precio" to "Precio: menor", "precio-desc" to "Precio: mayor", "duracion" to "Duración")
                            .forEach { (valor, etiqueta) -> DropdownMenuItem(text = { Text(etiqueta) }, onClick = { orden = valor; mostrarOrden = false }) }
                    }
                }
            }

            uiState.aviso?.let { aviso ->
                Text(
                    text = aviso,
                    modifier = Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 6.dp),
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.error
                )
            }

            when {
                uiState.cargando && uiState.items.isEmpty() -> {
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
                                Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.retry))
                            }
                        }
                    }
                }
                uiState.items.isEmpty() -> {
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
                    if (uiState.cargando) LinearProgressIndicator(Modifier.fillMaxWidth())
                    PullToRefreshBox(
                        isRefreshing = uiState.cargando,
                        onRefresh = { viewModel.cargarCatalogo(busqueda, when (filtroSeleccionado) { 1 -> true; 2 -> false; else -> null }, orden) },
                        modifier = Modifier.fillMaxSize()
                    ) {
                    LazyColumn(
                        modifier = Modifier.fillMaxSize(),
                        contentPadding = PaddingValues(16.dp),
                        verticalArrangement = Arrangement.spacedBy(12.dp)
                    ) {
                        items(uiState.items, key = { it.catalogoItemId }) { item ->
                            CatalogoItemCard(item = item)
                        }
                    }
                    }
                }
            }
        }
    }
}

@Composable
private fun CatalogoItemCard(item: CatalogoItemDto) {
    val locale = LocalLocale.current.platformLocale
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
                    text = "RD$ ${String.format(locale, "%,.2f", item.precio)}",
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
