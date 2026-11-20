package com.bookinghub.app.ui.screens

import androidx.compose.foundation.Image
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Button
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import com.bookinghub.app.R
import com.bookinghub.app.ui.theme.Acento

private data class PasoBienvenida(val titulo: String, val descripcion: String)

@Composable
fun BienvenidaScreen(onFinalizar: () -> Unit, onIniciarSesion: () -> Unit) {
    val pasos = remember {
        listOf(
            PasoBienvenida("Tu próximo corte, sin llamadas", "Reserva desde el teléfono cuando te venga bien."),
            PasoBienvenida("Elige tu estilo", "Explora los servicios, precios y barberos disponibles."),
            PasoBienvenida("Ten tus citas a mano", "Consulta los detalles y recibe cambios de estado en la app.")
        )
    }
    var indice by remember { mutableIntStateOf(0) }
    val esUltimo = indice == pasos.lastIndex

    Surface(modifier = Modifier.fillMaxSize(), color = MaterialTheme.colorScheme.background) {
        Column(
            modifier = Modifier.fillMaxSize().padding(28.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.Center
        ) {
            Image(painterResource(R.drawable.ic_logo), contentDescription = "Logo de CerquilloApp", modifier = Modifier.size(104.dp))
            Spacer(Modifier.height(24.dp))
            Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.brand_name), style = MaterialTheme.typography.headlineLarge, fontWeight = FontWeight.Bold)
            Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.barber_shop_style), style = MaterialTheme.typography.titleMedium, color = Acento)
            Spacer(Modifier.height(52.dp))
            Text(pasos[indice].titulo, style = MaterialTheme.typography.headlineSmall,
                fontWeight = FontWeight.Bold, textAlign = TextAlign.Center)
            Spacer(Modifier.height(12.dp))
            Text(pasos[indice].descripcion, style = MaterialTheme.typography.bodyLarge,
                color = MaterialTheme.colorScheme.onSurfaceVariant, textAlign = TextAlign.Center)
            Spacer(Modifier.height(36.dp))
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                pasos.indices.forEach { paso ->
                    Surface(
                        modifier = Modifier.size(if (paso == indice) 24.dp else 8.dp, 8.dp),
                        shape = RoundedCornerShape(8.dp),
                        color = if (paso == indice) Acento else MaterialTheme.colorScheme.outlineVariant
                    ) {}
                }
            }
            Spacer(Modifier.height(36.dp))
            Button(onClick = { if (esUltimo) onFinalizar() else indice++ },
                modifier = Modifier.fillMaxWidth().height(52.dp), shape = RoundedCornerShape(12.dp)) {
                Text(if (esUltimo) "Crear cuenta" else "Continuar", fontWeight = FontWeight.Bold)
            }
            OutlinedButton(onClick = onIniciarSesion, modifier = Modifier.fillMaxWidth().padding(top = 8.dp),
                shape = RoundedCornerShape(12.dp)) {
                Text(androidx.compose.ui.res.stringResource(com.bookinghub.app.R.string.already_have_account))
            }
        }
    }
}
