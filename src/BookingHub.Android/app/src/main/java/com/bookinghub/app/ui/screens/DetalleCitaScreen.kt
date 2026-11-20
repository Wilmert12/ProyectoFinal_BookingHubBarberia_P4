package com.bookinghub.app.ui.screens

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import com.bookinghub.app.ui.viewmodel.DetalleCitaViewModel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun DetalleCitaScreen(onVolver: () -> Unit, viewModel: DetalleCitaViewModel = hiltViewModel()) {
    val state by viewModel.uiState.collectAsState()
    val cita = state.cita
    Scaffold(topBar = {
        TopAppBar(title = { Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.appointment_details), fontWeight = FontWeight.Bold) }, navigationIcon = {
            IconButton(onClick = onVolver) { Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Volver") }
        })
    }) { padding ->
        Box(Modifier.fillMaxSize().padding(padding).padding(16.dp), contentAlignment = Alignment.Center) {
            when {
                state.cargando -> CircularProgressIndicator()
                state.error != null -> Text(state.error.orEmpty(), color = MaterialTheme.colorScheme.error)
                cita != null -> CitaDetalleCard(cita)
            }
        }
    }
}
