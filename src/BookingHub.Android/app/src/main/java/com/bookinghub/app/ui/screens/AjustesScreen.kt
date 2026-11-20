package com.bookinghub.app.ui.screens

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.automirrored.filled.Logout
import androidx.compose.material.icons.filled.Badge
import androidx.compose.material.icons.filled.Logout
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.PersonAdd
import androidx.compose.material3.Button
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
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
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import com.bookinghub.app.ui.viewmodel.EliminarCuentaViewModel
import com.bookinghub.app.util.SesionBarbero
import com.bookinghub.app.util.SesionCliente

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun AjustesScreen(
    onVolver: () -> Unit,
    onCerrarSesion: () -> Unit,
    onEditarPerfil: () -> Unit,
    onInformacion: () -> Unit,
    onRegistrarOtroCliente: () -> Unit,
    onCambiarContrasena: () -> Unit,
    onCuentaEliminada: () -> Unit,
    eliminarCuentaViewModel: EliminarCuentaViewModel = hiltViewModel()
) {
    val barbero = SesionBarbero.sesionActual
    val cliente = SesionCliente.clienteActual
    val scrollState = rememberScrollState()
    val estadoEliminar by eliminarCuentaViewModel.uiState.collectAsState()
    var confirmarEliminacion by remember { mutableStateOf(false) }

    LaunchedEffect(estadoEliminar.eliminada) {
        if (estadoEliminar.eliminada) onCuentaEliminada()
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.settings)) },
                navigationIcon = {
                    IconButton(onClick = onVolver) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Volver")
                    }
                }
            )
        }
    ) { paddingValues ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(paddingValues)
                .verticalScroll(scrollState)
                .padding(24.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Text(
                text = "Cuenta activa",
                style = MaterialTheme.typography.titleLarge,
                fontWeight = FontWeight.Bold
            )

            OutlinedButton(onClick = onInformacion, modifier = Modifier.fillMaxWidth(), shape = RoundedCornerShape(10.dp)) {
                Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.barber_shop_help_privacy))
            }

            Card(
                modifier = Modifier.fillMaxWidth(),
                shape = RoundedCornerShape(12.dp),
                colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
            ) {
                Column(modifier = Modifier.padding(18.dp)) {
                    Icon(
                        imageVector = if (barbero != null) Icons.Default.Badge else Icons.Default.Person,
                        contentDescription = null,
                        tint = MaterialTheme.colorScheme.primary
                    )
                    Spacer(modifier = Modifier.height(8.dp))
                    Text(
                        text = barbero?.nombre ?: cliente?.nombre ?: "Sin perfil activo",
                        style = MaterialTheme.typography.titleMedium,
                        fontWeight = FontWeight.SemiBold
                    )
                    Text(
                        text = when {
                            barbero != null -> "Barbero · Usuario #${barbero.usuarioId}"
                            cliente != null -> "Cliente · Número #${cliente.clienteId}"
                            else -> "Inicia sesión o registra un cliente para continuar."
                        },
                        style = MaterialTheme.typography.bodyMedium,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                    if (barbero != null) {
                        Text(
                            text = barbero.email,
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }
                    if (cliente != null) {
                        Text(
                            text = cliente.email,
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }
                }
            }

            Spacer(modifier = Modifier.height(8.dp))
            if (cliente != null) {
                OutlinedButton(
                    onClick = onEditarPerfil,
                    modifier = Modifier.fillMaxWidth(),
                    shape = RoundedCornerShape(10.dp)
                ) {
                    Icon(Icons.Default.Person, contentDescription = null)
                    Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.edit_profile), modifier = Modifier.padding(start = 8.dp))
                }
                OutlinedButton(
                    onClick = onCambiarContrasena,
                    modifier = Modifier.fillMaxWidth(),
                    shape = RoundedCornerShape(10.dp)
                ) {
                    Icon(Icons.Default.Person, contentDescription = null)
                    Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.change_password), modifier = Modifier.padding(start = 8.dp))
                }
            }
            Button(
                onClick = onCerrarSesion,
                modifier = Modifier.fillMaxWidth(),
                shape = RoundedCornerShape(10.dp)
            ) {
                Icon(Icons.AutoMirrored.Filled.Logout, contentDescription = null)
                Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.sign_out), modifier = Modifier.padding(start = 8.dp))
            }

            if (barbero == null && cliente != null) {
                OutlinedButton(
                    onClick = onRegistrarOtroCliente,
                    modifier = Modifier.fillMaxWidth(),
                    shape = RoundedCornerShape(10.dp)
                ) {
                    Icon(Icons.Default.PersonAdd, contentDescription = null)
                    Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.register_another_client), modifier = Modifier.padding(start = 8.dp))
                }
            }

            if (cliente != null) {
                OutlinedButton(
                    onClick = { confirmarEliminacion = true },
                    enabled = !estadoEliminar.cargando,
                    modifier = Modifier.fillMaxWidth(),
                    shape = RoundedCornerShape(10.dp)
                ) {
                    Text(if (estadoEliminar.cargando) "Eliminando cuenta…" else "Eliminar mi cuenta",
                        color = MaterialTheme.colorScheme.error)
                }
                estadoEliminar.error?.let {
                    Text(it, color = MaterialTheme.colorScheme.error, style = MaterialTheme.typography.bodySmall)
                }
                Text(
                    text = "Tu sesión y credenciales se guardan cifradas en este dispositivo.",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }

            TextButton(onClick = onVolver, modifier = Modifier.fillMaxWidth()) {
                Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.back))
            }
        }
    }

    if (confirmarEliminacion) {
        AlertDialog(
            onDismissRequest = { if (!estadoEliminar.cargando) confirmarEliminacion = false },
            title = { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.account_delete_title)) },
            text = { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.account_delete_body)) },
            confirmButton = {
                TextButton(onClick = {
                    confirmarEliminacion = false
                    eliminarCuentaViewModel.eliminar()
                }) { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.delete_account), color = MaterialTheme.colorScheme.error) }
            },
            dismissButton = {
                TextButton(onClick = { confirmarEliminacion = false }, enabled = !estadoEliminar.cargando) {
                    Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.cancel))
                }
            }
        )
    }
}
