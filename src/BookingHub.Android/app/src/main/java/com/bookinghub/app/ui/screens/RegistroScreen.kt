package com.bookinghub.app.ui.screens

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.Image
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.bookinghub.app.R
import com.bookinghub.app.ui.theme.Acento
import com.bookinghub.app.ui.theme.VidrioBorde
import com.bookinghub.app.ui.viewmodel.ClienteViewModel

/**
 * Entrada del cliente: registrarse por primera vez o, si ya es cliente (se registró en la web
 * o en recepción), entrar con su nombre y teléfono sin crear un cliente repetido.
 */
@Composable
fun RegistroScreen(
    onRegistroExitoso: () -> Unit,
    onAccesoBarbero: () -> Unit,
    viewModel: ClienteViewModel = viewModel()
) {
    val uiState by viewModel.uiState.collectAsState()
    var yaSoyCliente by rememberSaveable { mutableStateOf(false) }
    var nombre by rememberSaveable { mutableStateOf("") }
    var telefono by rememberSaveable { mutableStateOf("") }
    var email by rememberSaveable { mutableStateOf("") }

    LaunchedEffect(uiState.registroExitoso) {
        if (uiState.registroExitoso) onRegistroExitoso()
    }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .systemBarsPadding()
            .imePadding()
            .verticalScroll(rememberScrollState())
            .padding(horizontal = 20.dp, vertical = 28.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        Image(
            painter = painterResource(id = R.drawable.ic_logo),
            contentDescription = "Logo CerquilloApp",
            modifier = Modifier.size(64.dp)
        )
        Spacer(modifier = Modifier.height(12.dp))
        Text("CerquilloApp", style = MaterialTheme.typography.headlineLarge, fontWeight = FontWeight.Bold)
        Text("Barbería & Estilo", style = MaterialTheme.typography.bodyLarge, color = Acento, fontWeight = FontWeight.Medium)
        Spacer(modifier = Modifier.height(24.dp))

        // Tarjeta de vidrio con el formulario
        Card(
            modifier = Modifier.fillMaxWidth().widthIn(max = 480.dp),
            shape = MaterialTheme.shapes.extraLarge,
            colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface),
            border = BorderStroke(1.dp, VidrioBorde)
        ) {
            Column(modifier = Modifier.padding(20.dp), verticalArrangement = Arrangement.spacedBy(12.dp)) {
                // Registrarme / Ya soy cliente
                Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    listOf(false to "Soy nuevo", true to "Ya soy cliente").forEach { (opcion, texto) ->
                        val elegido = yaSoyCliente == opcion
                        if (elegido) {
                            Button(onClick = {}, modifier = Modifier.weight(1f)) { Text(texto) }
                        } else {
                            OutlinedButton(
                                onClick = { yaSoyCliente = opcion; viewModel.limpiarError() },
                                modifier = Modifier.weight(1f),
                                border = BorderStroke(1.dp, VidrioBorde)
                            ) { Text(texto) }
                        }
                    }
                }

                Text(
                    text = if (yaSoyCliente) {
                        "Escribe tu nombre y el teléfono con el que te registraste (en la web o en la barbería)."
                    } else {
                        "Regístrate para reservar y ver tus citas."
                    },
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    textAlign = TextAlign.Center,
                    modifier = Modifier.fillMaxWidth()
                )

                OutlinedTextField(
                    value = nombre,
                    onValueChange = { nombre = it },
                    label = { Text("Nombre completo") },
                    leadingIcon = { Icon(Icons.Default.Person, contentDescription = null) },
                    shape = MaterialTheme.shapes.medium,
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true
                )
                OutlinedTextField(
                    value = telefono,
                    onValueChange = { telefono = it },
                    label = { Text("Teléfono") },
                    leadingIcon = { Icon(Icons.Default.Phone, contentDescription = null) },
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Phone),
                    shape = MaterialTheme.shapes.medium,
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true
                )
                if (!yaSoyCliente) {
                    OutlinedTextField(
                        value = email,
                        onValueChange = { email = it },
                        label = { Text("Correo electrónico (opcional)") },
                        leadingIcon = { Icon(Icons.Default.Email, contentDescription = null) },
                        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Email),
                        shape = MaterialTheme.shapes.medium,
                        modifier = Modifier.fillMaxWidth(),
                        singleLine = true
                    )
                }

                uiState.error?.let { error ->
                    Text(error, color = MaterialTheme.colorScheme.error, style = MaterialTheme.typography.bodySmall)
                }

                Button(
                    onClick = {
                        if (yaSoyCliente) viewModel.entrarComoCliente(nombre, telefono)
                        else viewModel.registrarCliente(nombre, telefono, email)
                    },
                    modifier = Modifier.fillMaxWidth().height(50.dp),
                    enabled = nombre.isNotBlank() && telefono.isNotBlank() && !uiState.cargando
                ) {
                    if (uiState.cargando) {
                        CircularProgressIndicator(modifier = Modifier.size(22.dp), color = MaterialTheme.colorScheme.onPrimary, strokeWidth = 2.dp)
                    } else {
                        Text(if (yaSoyCliente) "Entrar" else "Registrarme", fontWeight = FontWeight.Bold)
                    }
                }
            }
        }

        Spacer(modifier = Modifier.height(8.dp))
        TextButton(onClick = onAccesoBarbero) {
            Text("¿Eres barbero? Inicia sesión aquí")
        }
    }
}
