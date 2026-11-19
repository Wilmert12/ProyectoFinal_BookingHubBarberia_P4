package com.bookinghub.app.ui.theme

import androidx.compose.material3.*
import androidx.compose.runtime.Composable

private val DarkColorScheme = darkColorScheme(
    primary = Acento,
    onPrimary = Tinta,
    primaryContainer = TintaHover,
    onPrimaryContainer = Acento,
    secondary = AcentoClaro,
    onSecondary = Tinta,
    background = Tinta,
    onBackground = Papel,
    surface = TintaHover,
    onSurface = Papel,
    surfaceVariant = TintaHover,
    onSurfaceVariant = Borde,
    outline = TextoSuave,
    outlineVariant = TintaHover,
    error = RojoError,
    onError = Blanco
)

private val LightColorScheme = lightColorScheme(
    primary = Tinta,
    onPrimary = Blanco,
    primaryContainer = Acento,
    onPrimaryContainer = Tinta,
    secondary = Acento,
    onSecondary = Blanco,
    background = Papel,
    onBackground = Tinta,
    surface = Blanco,
    onSurface = Tinta,
    surfaceVariant = Papel,
    onSurfaceVariant = TextoSuave,
    outline = Borde,
    outlineVariant = Borde,
    error = RojoError,
    onError = Blanco
)

@Composable
fun BookingHubTheme(
    darkTheme: Boolean = false,
    content: @Composable () -> Unit
) {
    val colorScheme = if (darkTheme) DarkColorScheme else LightColorScheme
    MaterialTheme(
        colorScheme = colorScheme,
        typography = Typography,
        content = content
    )
}
