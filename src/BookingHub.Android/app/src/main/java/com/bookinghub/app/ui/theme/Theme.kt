package com.bookinghub.app.ui.theme

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxScope
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Shapes
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.drawscope.DrawScope
import androidx.compose.ui.unit.dp

/**
 * Tema claro con glassmorfismo, igual que la web:
 * - El fondo (background) es transparente: lo pinta [FondoVidrio] con manchas suaves de color.
 * - Las superficies (tarjetas, barras) son blanco translúcido; menús y diálogos, casi opacos.
 */
private val LightColorScheme = lightColorScheme(
    primary = Tinta,
    onPrimary = Blanco,
    primaryContainer = AcentoClaro,
    onPrimaryContainer = Tinta,
    secondary = Acento,
    onSecondary = Blanco,
    secondaryContainer = Acento.copy(alpha = 0.18f),
    onSecondaryContainer = Tinta,
    tertiary = Acento,
    background = Color.Transparent,
    onBackground = Tinta,
    surface = Vidrio,
    onSurface = Tinta,
    surfaceVariant = Color(0x8CFFFFFF),
    onSurfaceVariant = TextoSuave,
    surfaceContainerLowest = Vidrio,
    surfaceContainerLow = Vidrio,
    surfaceContainer = VidrioFuerte,
    surfaceContainerHigh = VidrioFuerte,
    surfaceContainerHighest = Vidrio,
    surfaceTint = Color.Transparent,
    outline = Color(0x4017150F),
    outlineVariant = Borde,
    error = RojoError,
    onError = Blanco
)

private val FormasVidrio = Shapes(
    extraSmall = RoundedCornerShape(8.dp),
    small = RoundedCornerShape(12.dp),
    medium = RoundedCornerShape(16.dp),
    large = RoundedCornerShape(20.dp),
    extraLarge = RoundedCornerShape(28.dp)
)

@Composable
fun BookingHubTheme(content: @Composable () -> Unit) {
    MaterialTheme(
        colorScheme = LightColorScheme,
        typography = Typography,
        shapes = FormasVidrio,
        content = content
    )
}

/** Fondo crema con tres manchas de color muy suaves: le da algo que "ver" a través del vidrio. */
@Composable
fun FondoVidrio(content: @Composable BoxScope.() -> Unit) {
    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(Papel)
            .drawBehind {
                val lado = maxOf(size.width, size.height)
                mancha(ManchaDorada.copy(alpha = 0.40f), Offset(size.width * 0.05f, -size.height * 0.05f), lado * 0.65f)
                mancha(ManchaSalvia.copy(alpha = 0.32f), Offset(size.width * 1.05f, size.height * 0.42f), lado * 0.55f)
                mancha(ManchaDorada.copy(alpha = 0.20f), Offset(size.width * 0.3f, size.height * 1.05f), lado * 0.5f)
            },
        content = content
    )
}

private fun DrawScope.mancha(color: Color, centro: Offset, radio: Float) {
    drawCircle(
        brush = Brush.radialGradient(listOf(color, Color.Transparent), center = centro, radius = radio),
        radius = radio,
        center = centro
    )
}
