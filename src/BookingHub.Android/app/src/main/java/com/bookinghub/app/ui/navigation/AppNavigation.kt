package com.bookinghub.app.ui.navigation

import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.platform.LocalContext
import androidx.navigation.compose.*
import com.bookinghub.app.ui.screens.*
import com.bookinghub.app.ui.theme.Acento
import com.bookinghub.app.util.CitasCache
import com.bookinghub.app.util.SesionBarbero
import com.bookinghub.app.util.SesionCliente

sealed class Ruta(val ruta: String, val titulo: String, val icono: ImageVector) {
    object Home : Ruta("home", "Inicio", Icons.Default.Home)
    object Catalogo : Ruta("catalogo", "Catálogo", Icons.Default.ShoppingCart)
    object Barberos : Ruta("barberos", "Barberos", Icons.Default.Person)
    object Reservar : Ruta("reservar", "Reservar", Icons.Default.Add)
    object MisCitas : Ruta("mis_citas", "Mis Citas", Icons.Default.DateRange)
    object Registro : Ruta("registro", "Registro", Icons.Default.PersonAdd)
    object AccesoBarbero : Ruta("acceso_barbero", "Acceso barbero", Icons.Default.Badge)
    object AgendaBarbero : Ruta("agenda_barbero", "Mi agenda", Icons.Default.EventNote)
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun AppNavigation() {
    val navController = rememberNavController()
    val context = LocalContext.current
    val navBackStackEntry by navController.currentBackStackEntryAsState()
    val rutaActual = navBackStackEntry?.destination?.route
    val destinoInicial = remember {
        when {
            SesionBarbero.haySesion -> Ruta.AgendaBarbero.ruta
            SesionCliente.hayPerfilGuardado -> Ruta.Home.ruta
            else -> Ruta.Registro.ruta
        }
    }

    val pantallasConNavBar = listOf(
        Ruta.Home, Ruta.Catalogo, Ruta.Barberos, Ruta.Reservar, Ruta.MisCitas
    )
    val mostrarNavBar = rutaActual in pantallasConNavBar.map { it.ruta }

    // El controlador puede restaurar una ruta antigua después de cerrar sesión o reiniciar la app.
    LaunchedEffect(rutaActual) {
        val tieneSesionBarbero = SesionBarbero.haySesion
        val tienePerfilCliente = SesionCliente.hayPerfilGuardado
        val rutasCliente = setOf(
            Ruta.Home.ruta,
            Ruta.Catalogo.ruta,
            Ruta.Barberos.ruta,
            Ruta.Reservar.ruta,
            Ruta.MisCitas.ruta
        )
        val destinoSeguro = when {
            tieneSesionBarbero && rutaActual != Ruta.AgendaBarbero.ruta -> Ruta.AgendaBarbero.ruta
            !tieneSesionBarbero && rutaActual == Ruta.AgendaBarbero.ruta -> {
                if (tienePerfilCliente) Ruta.Home.ruta else Ruta.Registro.ruta
            }
            !tieneSesionBarbero && !tienePerfilCliente && rutaActual in rutasCliente -> Ruta.Registro.ruta
            else -> null
        }

        if (destinoSeguro != null && destinoSeguro != rutaActual) {
            navController.navigate(destinoSeguro) {
                popUpTo(navController.graph.startDestinationId) { inclusive = true }
                launchSingleTop = true
            }
        }
    }

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
            startDestination = destinoInicial,
            modifier = Modifier.padding(paddingValues)
        ) {
            composable(Ruta.Registro.ruta) {
                RegistroScreen(
                    onRegistroExitoso = {
                        navController.navigate(Ruta.Home.ruta) {
                            popUpTo(Ruta.Registro.ruta) { inclusive = true }
                        }
                    },
                    onAccesoBarbero = { navController.navigate(Ruta.AccesoBarbero.ruta) }
                )
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
            composable(Ruta.AccesoBarbero.ruta) {
                AccesoBarberoScreen(
                    onAccesoExitoso = {
                        navController.navigate(Ruta.AgendaBarbero.ruta) {
                            popUpTo(navController.graph.startDestinationId) { inclusive = true }
                            launchSingleTop = true
                        }
                    },
                    onVolver = { navController.popBackStack() }
                )
            }
            composable(Ruta.AgendaBarbero.ruta) {
                AgendaBarberoScreen(
                    onCerrarSesion = {
                        val usuarioId = SesionBarbero.sesionActual?.usuarioId
                        SesionBarbero.borrar(context)
                        if (usuarioId != null) CitasCache.borrarAgendaBarbero(context, usuarioId)
                        val siguienteRuta = if (SesionCliente.hayPerfilGuardado) {
                            Ruta.Home.ruta
                        } else {
                            Ruta.Registro.ruta
                        }
                        navController.navigate(siguienteRuta) {
                            popUpTo(navController.graph.startDestinationId) { inclusive = true }
                            launchSingleTop = true
                        }
                    }
                )
            }
        }
    }
}
