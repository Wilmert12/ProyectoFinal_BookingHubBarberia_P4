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
import androidx.compose.material.icons.filled.Email
import androidx.compose.material.icons.filled.Lock
import androidx.compose.material.icons.filled.Phone
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
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
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import com.bookinghub.app.ui.viewmodel.RecuperarContrasenaViewModel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun RecuperarContrasenaScreen(
    onVolver: () -> Unit,
    viewModel: RecuperarContrasenaViewModel = hiltViewModel()
) {
    val state by viewModel.uiState.collectAsState()
    var email by remember { mutableStateOf("") }
    var telefono by remember { mutableStateOf("") }
    var codigo by remember { mutableStateOf("") }
    var nuevaContrasena by remember { mutableStateOf("") }

    LaunchedEffect(state.contrasenaCambiada) {
        if (state.contrasenaCambiada) kotlinx.coroutines.delay(1800).also { onVolver() }
    }

    Scaffold(topBar = {
        TopAppBar(title = { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.recover_password)) }, navigationIcon = {
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
            Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.verify_account), style = MaterialTheme.typography.headlineSmall, fontWeight = FontWeight.Bold)
            Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.recovery_instructions),
                style = MaterialTheme.typography.bodyMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
            Spacer(Modifier.height(20.dp))
            OutlinedTextField(email, { email = it }, label = { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.email)) },
                leadingIcon = { Icon(Icons.Default.Email, null) }, keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Email),
                singleLine = true, shape = RoundedCornerShape(10.dp), modifier = Modifier.fillMaxWidth(), enabled = !state.codigoSolicitado)
            Spacer(Modifier.height(12.dp))
            OutlinedTextField(telefono, { telefono = it }, label = { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.dominican_phone)) },
                leadingIcon = { Icon(Icons.Default.Phone, null) }, keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Phone),
                singleLine = true, shape = RoundedCornerShape(10.dp), modifier = Modifier.fillMaxWidth(), enabled = !state.codigoSolicitado)

            if (state.codigoSolicitado) {
                Spacer(Modifier.height(12.dp))
                OutlinedTextField(codigo, { codigo = it }, label = { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.email_code)) },
                    leadingIcon = { Icon(Icons.Default.Lock, null) }, singleLine = true,
                    shape = RoundedCornerShape(10.dp), modifier = Modifier.fillMaxWidth())
                Spacer(Modifier.height(12.dp))
                OutlinedTextField(nuevaContrasena, { nuevaContrasena = it }, label = { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.new_password)) },
                    leadingIcon = { Icon(Icons.Default.Lock, null) }, visualTransformation = PasswordVisualTransformation(),
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Password), singleLine = true,
                    shape = RoundedCornerShape(10.dp), modifier = Modifier.fillMaxWidth())
            }

            state.mensaje?.let { Text(it, color = MaterialTheme.colorScheme.primary, modifier = Modifier.padding(top = 12.dp)) }
            state.error?.let { Text(it, color = MaterialTheme.colorScheme.error, modifier = Modifier.padding(top = 12.dp)) }
            Spacer(Modifier.height(20.dp))
            Button(
                onClick = {
                    if (state.codigoSolicitado) viewModel.restablecer(codigo, nuevaContrasena)
                    else viewModel.solicitar(email, telefono)
                },
                enabled = !state.cargando && !state.contrasenaCambiada,
                modifier = Modifier.fillMaxWidth().height(52.dp), shape = RoundedCornerShape(10.dp)
            ) {
                if (state.cargando) CircularProgressIndicator()
                else Text(if (state.codigoSolicitado) "Actualizar contraseña" else "Enviar código", fontWeight = FontWeight.Bold)
            }
            if (state.codigoSolicitado && !state.contrasenaCambiada) {
                TextButton(onClick = { viewModel.solicitar(email, telefono) }, modifier = Modifier.fillMaxWidth()) {
                    Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.request_another_code))
                }
            }
        }
    }
}
