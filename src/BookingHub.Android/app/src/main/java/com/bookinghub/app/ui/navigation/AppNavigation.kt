package com.bookinghub.app.ui.navigation

import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.navigation.compose.*
import com.bookinghub.app.ui.screens.*
import com.bookinghub.app.ui.theme.Acento
import com.bookinghub.app.util.SesionCliente

sealed class Ruta(val ruta: String, val titulo: String, val icono: ImageVector) {
    object Home : Ruta("home", "Inicio", Icons.Default.Home)
    object Catalogo : Ruta("catalogo", "Catálogo", Icons.Default.ShoppingCart)
    object Barberos : Ruta("barberos", "Barberos", Icons.Default.Person)
    object Reservar : Ruta("reservar", "Reservar", Icons.Default.Add)
    object MisCitas : Ruta("mis_citas", "Mis Citas", Icons.Default.DateRange)
    object Registro : Ruta("registro", "Registro", Icons.Default.PersonAdd)
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun AppNavigation() {
    val navController = rememberNavController()
    val navBackStackEntry by navController.currentBackStackEntryAsState()
    val rutaActual = navBackStackEntry?.destination?.route

    val pantallasConNavBar = listOf(
        Ruta.Home, Ruta.Catalogo, Ruta.Barberos, Ruta.Reservar, Ruta.MisCitas
    )
    val mostrarNavBar = rutaActual in pantallasConNavBar.map { it.ruta }

    Scaffold(
        bottomBar = {
            if (mostrarNavBar) {
                NavigationBar(
                    containerColor = MaterialTheme.colorScheme.surface
                ) {
                    pantallasConNavBar.forEach { pantalla ->
                        val seleccionada = rutaActual == pantalla.ruta
                        NavigationBarItem(
                            icon = { Icon(pantalla.icono, contentDescription = pantalla.titulo) },
                            label = { Text(pantalla.titulo) },
                            selected = seleccionada,
                            colors = NavigationBarItemDefaults.colors(
                                indicatorColor = Acento.copy(alpha = 0.2f),
                                selectedIconColor = Acento,
                                selectedTextColor = MaterialTheme.colorScheme.onSurface,
                                unselectedIconColor = MaterialTheme.colorScheme.onSurfaceVariant,
                                unselectedTextColor = MaterialTheme.colorScheme.onSurfaceVariant
                            ),
                            onClick = {
                                navController.navigate(pantalla.ruta) {
                                    popUpTo(navController.graph.startDestinationId) { saveState = true }
                                    launchSingleTop = true
                                    restoreState = true
                                }
                            }
                        )
                    }
                }
            }
        }
    ) { paddingValues ->
        NavHost(
            navController = navController,
            startDestination = if (SesionCliente.hayPerfilGuardado) Ruta.Home.ruta else Ruta.Registro.ruta,
            modifier = Modifier.padding(paddingValues)
        ) {
            composable(Ruta.Registro.ruta) {
                RegistroScreen(onRegistroExitoso = {
                    navController.navigate(Ruta.Home.ruta) {
                        popUpTo(Ruta.Registro.ruta) { inclusive = true }
                    }
                })
            }
            composable(Ruta.Home.ruta) {
                HomeScreen(onNavigate = { ruta -> navController.navigate(ruta) })
            }
            composable(Ruta.Catalogo.ruta) { CatalogoScreen() }
            composable(Ruta.Barberos.ruta) { BarberosScreen() }
            composable(Ruta.Reservar.ruta) {
                ReservarScreen(onCitaCreada = {
                    navController.navigate(Ruta.MisCitas.ruta) {
                        popUpTo(Ruta.Reservar.ruta) { inclusive = true }
                    }
                })
            }
            composable(Ruta.MisCitas.ruta) { MisCitasScreen() }
        }
    }
}
