package com.bookinghub.app.ui.screens

import android.graphics.Bitmap
import android.graphics.BitmapFactory
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
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
import androidx.compose.material.icons.filled.Person
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import com.bookinghub.app.ui.viewmodel.PerfilClienteViewModel
import java.io.ByteArrayOutputStream

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun PerfilClienteScreen(
    onVolver: () -> Unit,
    viewModel: PerfilClienteViewModel = hiltViewModel()
) {
    val state by viewModel.uiState.collectAsState()
    val context = LocalContext.current
    var nombre by remember { mutableStateOf("") }
    var telefono by remember { mutableStateOf("") }
    var email by remember { mutableStateOf("") }

    LaunchedEffect(state.perfil?.clienteId) {
        state.perfil?.let {
            nombre = it.nombre
            telefono = it.telefono
            email = it.email.orEmpty()
        }
    }

    val selectorFoto = rememberLauncherForActivityResult(ActivityResultContracts.GetContent()) { uri ->
        if (uri != null) runCatching {
            context.contentResolver.openInputStream(uri)?.use { BitmapFactory.decodeStream(it) }
        }.getOrNull()?.let { bitmap ->
            val maxLado = 640
            val escala = minOf(1f, maxLado.toFloat() / maxOf(bitmap.width, bitmap.height))
            val reducida = if (escala < 1f) Bitmap.createScaledBitmap(bitmap,
                (bitmap.width * escala).toInt().coerceAtLeast(1), (bitmap.height * escala).toInt().coerceAtLeast(1), true) else bitmap
            val salida = ByteArrayOutputStream()
            reducida.compress(Bitmap.CompressFormat.JPEG, 78, salida)
            if (reducida !== bitmap) reducida.recycle()
            if (bitmap !== reducida) bitmap.recycle()
            viewModel.subirFoto(salida.toByteArray())
        }
    }

    Scaffold(topBar = {
        TopAppBar(title = { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.my_profile), fontWeight = FontWeight.Bold) }, navigationIcon = {
            IconButton(onClick = onVolver) { Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Volver") }
        })
    }) { padding ->
        Column(
            Modifier.fillMaxSize().padding(padding).verticalScroll(rememberScrollState()).padding(24.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            if (state.cargando && state.perfil == null) {
                CircularProgressIndicator()
            } else {
                Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.account_details), style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.SemiBold)
                OutlinedTextField(nombre, { nombre = it.take(100) }, Modifier.fillMaxWidth(), label = { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.full_name)) }, singleLine = true)
                OutlinedTextField(telefono, { telefono = it.take(20) }, Modifier.fillMaxWidth(), label = { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.phone)) }, singleLine = true)
                OutlinedTextField(email, { email = it.take(255) }, Modifier.fillMaxWidth(), label = { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.email)) }, singleLine = true)
                Button(onClick = { viewModel.guardar(nombre, telefono, email) },
                    enabled = !state.guardando && !state.cargando, modifier = Modifier.fillMaxWidth(), shape = RoundedCornerShape(10.dp)) {
                    if (state.guardando) CircularProgressIndicator(strokeWidth = 2.dp, modifier = Modifier.height(20.dp))
                    else Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.save_changes))
                }
                OutlinedButton(onClick = { selectorFoto.launch("image/*") }, enabled = !state.subiendoFoto,
                    modifier = Modifier.fillMaxWidth(), shape = RoundedCornerShape(10.dp)) {
                    Icon(Icons.Default.Person, contentDescription = null)
                    Text(if (state.subiendoFoto) "Subiendo foto…" else "Cambiar foto de perfil", Modifier.padding(start = 8.dp))
                }
                Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.photo_optimization_note), style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant)
            }
            state.error?.let { Text(it, color = MaterialTheme.colorScheme.error) }
            state.mensaje?.let { Text(it, color = MaterialTheme.colorScheme.primary) }
            Spacer(Modifier.height(8.dp))
        }
    }
}
