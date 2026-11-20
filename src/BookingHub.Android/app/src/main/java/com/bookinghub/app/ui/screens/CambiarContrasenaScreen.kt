package com.bookinghub.app.ui.screens

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Lock
import androidx.compose.material.icons.filled.Visibility
import androidx.compose.material.icons.filled.VisibilityOff
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
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
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import com.bookinghub.app.ui.viewmodel.CambiarContrasenaViewModel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun CambiarContrasenaScreen(
    onVolver: () -> Unit,
    onContrasenaCambiada: () -> Unit,
    viewModel: CambiarContrasenaViewModel = hiltViewModel()
) {
    val state by viewModel.uiState.collectAsState()
    var actual by remember { mutableStateOf("") }
    var nueva by remember { mutableStateOf("") }
    var repetir by remember { mutableStateOf("") }
    var visible by remember { mutableStateOf(false) }

    LaunchedEffect(state.cambiado) {
        if (state.cambiado) {
            kotlinx.coroutines.delay(1200)
            onContrasenaCambiada()
        }
    }

    Scaffold(topBar = {
        TopAppBar(title = { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.change_password)) }, navigationIcon = {
            IconButton(onClick = onVolver) {
                Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Volver")
            }
        })
    }) { padding ->
        Column(
            modifier = Modifier.fillMaxSize().imePadding().verticalScroll(rememberScrollState())
                .padding(padding).padding(24.dp),
            verticalArrangement = Arrangement.Center
        ) {
            Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.protect_account), style = MaterialTheme.typography.headlineSmall, fontWeight = FontWeight.Bold)
            Spacer(Modifier.height(20.dp))
            CampoContrasena("Contraseña actual", actual, { actual = it }, visible) { visible = !visible }
            Spacer(Modifier.height(12.dp))
            CampoContrasena("Nueva contraseña", nueva, { nueva = it }, visible) { visible = !visible }
            Spacer(Modifier.height(12.dp))
            CampoContrasena("Repetir nueva contraseña", repetir, { repetir = it }, visible) { visible = !visible }
            if (nueva.isNotEmpty()) {
                Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.password_requirements),
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    style = MaterialTheme.typography.bodySmall, modifier = Modifier.padding(top = 6.dp))
            }
            state.error?.let { Text(it, color = MaterialTheme.colorScheme.error, modifier = Modifier.padding(top = 12.dp)) }
            if (state.cambiado) Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.password_updated_sign_in),
                color = MaterialTheme.colorScheme.primary, modifier = Modifier.padding(top = 12.dp))
            Spacer(Modifier.height(20.dp))
            Button(onClick = { viewModel.cambiar(actual, nueva, repetir) },
                enabled = !state.cargando && !state.cambiado,
                modifier = Modifier.fillMaxWidth().height(52.dp), shape = RoundedCornerShape(10.dp)) {
                if (state.cargando) CircularProgressIndicator()
                else Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.update_password), fontWeight = FontWeight.Bold)
            }
        }
    }
}

@Composable
private fun CampoContrasena(
    titulo: String,
    valor: String,
    onValor: (String) -> Unit,
    visible: Boolean,
    alCambiarVisibilidad: () -> Unit
) {
    OutlinedTextField(
        value = valor,
        onValueChange = onValor,
        label = { Text(titulo) },
        leadingIcon = { Icon(Icons.Default.Lock, contentDescription = null) },
        trailingIcon = {
            IconButton(onClick = alCambiarVisibilidad) {
                Icon(if (visible) Icons.Default.VisibilityOff else Icons.Default.Visibility,
                    contentDescription = if (visible) "Ocultar contraseña" else "Mostrar contraseña")
            }
        },
        visualTransformation = if (visible) VisualTransformation.None else PasswordVisualTransformation(),
        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Password),
        singleLine = true,
        shape = RoundedCornerShape(10.dp),
        modifier = Modifier.fillMaxWidth()
    )
}
