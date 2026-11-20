package com.bookinghub.app.ui.screens

import android.content.Intent
import android.net.Uri
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.automirrored.filled.Chat
import androidx.compose.material.icons.filled.Call
import androidx.compose.material.icons.filled.LocationOn
import androidx.compose.material.icons.filled.Schedule
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import com.bookinghub.app.ui.viewmodel.InformacionBarberiaViewModel
import java.net.URLEncoder
import java.nio.charset.StandardCharsets

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun InformacionBarberiaScreen(
    onVolver: () -> Unit,
    viewModel: InformacionBarberiaViewModel = hiltViewModel()
) {
    val state by viewModel.uiState.collectAsState()
    val context = LocalContext.current

    Scaffold(topBar = {
        TopAppBar(title = { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.barber_shop_help), fontWeight = FontWeight.Bold) }, navigationIcon = {
            IconButton(onClick = onVolver) { Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Volver") }
        })
    }) { padding ->
        Column(
            Modifier.fillMaxSize().padding(padding).verticalScroll(rememberScrollState()).padding(20.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            if (state.cargando) CircularProgressIndicator()
            state.error?.let { Text(it, color = MaterialTheme.colorScheme.error) }
            state.informacion?.let { info ->
                Card(shape = RoundedCornerShape(14.dp), colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)) {
                    Column(Modifier.padding(18.dp), verticalArrangement = Arrangement.spacedBy(12.dp)) {
                        Text(info.nombre, style = MaterialTheme.typography.headlineSmall, fontWeight = FontWeight.Bold)
                        LineaInfo(Icons.Default.LocationOn, info.direccion.ifBlank { "Dirección pendiente de configuración" })
                        LineaInfo(Icons.Default.Schedule, info.horario.ifBlank { "Horario pendiente de configuración" })
                        Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                            if (info.telefono.isNotBlank()) OutlinedButton(onClick = {
                                abrir(context, Intent(Intent.ACTION_DIAL, Uri.parse("tel:${Uri.encode(info.telefono)}")))
                            }) { Icon(Icons.Default.Call, contentDescription = null); Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.call), Modifier.padding(start = 6.dp)) }
                            val numeroWhatsapp = info.whatsApp.filter(Char::isDigit)
                            if (numeroWhatsapp.isNotBlank()) OutlinedButton(onClick = {
                                abrir(context, Intent(Intent.ACTION_VIEW, Uri.parse("https://wa.me/$numeroWhatsapp")))
                            }) { Icon(Icons.AutoMirrored.Filled.Chat, contentDescription = null); Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.whatsapp), Modifier.padding(start = 6.dp)) }
                        }
                        val mapa = info.urlMapa.ifBlank {
                            if (info.direccion.isBlank()) "" else "geo:0,0?q=" + URLEncoder.encode(info.direccion, StandardCharsets.UTF_8.name())
                        }
                        if (mapa.isNotBlank()) Button(onClick = { abrir(context, Intent(Intent.ACTION_VIEW, Uri.parse(mapa))) },
                            modifier = Modifier.fillMaxWidth(), shape = RoundedCornerShape(10.dp)) {
                            Icon(Icons.Default.LocationOn, contentDescription = null)
                            Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.open_map), Modifier.padding(start = 8.dp))
                        }
                    }
                }
            }
            Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.help), style = MaterialTheme.typography.titleLarge, fontWeight = FontWeight.Bold)
            SeccionLegal(
                androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.legal_booking_title),
                androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.legal_booking_body)
            )
            SeccionLegal(
                androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.legal_changes_title),
                androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.legal_changes_body)
            )
            SeccionLegal(
                androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.legal_privacy_title),
                androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.legal_privacy_body)
            )
            SeccionLegal(
                androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.legal_terms_title),
                androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.legal_terms_body)
            )
        }
    }
}

@Composable
private fun LineaInfo(icono: androidx.compose.ui.graphics.vector.ImageVector, texto: String) {
    Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(10.dp)) {
        Icon(icono, contentDescription = null, tint = MaterialTheme.colorScheme.primary)
        Text(texto, style = MaterialTheme.typography.bodyMedium)
    }
}

@Composable
private fun SeccionLegal(titulo: String, contenido: String) {
    Card(shape = RoundedCornerShape(10.dp), colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)) {
        Column(Modifier.fillMaxWidth().padding(16.dp), verticalArrangement = Arrangement.spacedBy(6.dp)) {
            Text(titulo, style = MaterialTheme.typography.titleSmall, fontWeight = FontWeight.SemiBold)
            Text(contenido, style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
        }
    }
}

private fun abrir(context: android.content.Context, intent: Intent) {
    runCatching { context.startActivity(intent) }
}
