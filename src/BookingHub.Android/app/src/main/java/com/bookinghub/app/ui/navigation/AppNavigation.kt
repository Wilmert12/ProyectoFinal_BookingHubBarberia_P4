package com.bookinghub.app.ui.navigation

import androidx.compose.foundation.layout.*
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.EventNote
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.runtime.SideEffect
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.unit.dp
import androidx.compose.ui.platform.LocalContext
import android.app.Activity
import com.bookinghub.app.util.Claves
import androidx.navigation.compose.*
import androidx.navigation.NavType
import androidx.navigation.navArgument
import com.bookinghub.app.ui.screens.*
import com.bookinghub.app.ui.theme.Acento
import com.bookinghub.app.util.CitasCache
import com.bookinghub.app.util.SesionBarbero
import com.bookinghub.app.util.SesionCliente

sealed class Ruta(val ruta: String, val titulo: String, val icono: ImageVector) {
    object Home : Ruta("home", "Inicio", Icons.Default.Home)
    object Bienvenida : Ruta("bienvenida", "Bienvenida", Icons.Default.ContentCut)
    object Catalogo : Ruta("catalogo", "Catálogo", Icons.Default.ShoppingCart)
    object Barberos : Ruta("barberos", "Barberos", Icons.Default.Person)
    object Reservar : Ruta("reservar", "Reservar", Icons.Default.Add)
    object MisCitas : Ruta("mis_citas", "Mis Citas", Icons.Default.DateRange)
    object Registro : Ruta("registro", "Registro", Icons.Default.PersonAdd)
    object AccesoBarbero : Ruta("acceso_barbero", "Acceso barbero", Icons.Default.Badge)
    object RecuperarContrasena : Ruta("recuperar_contrasena", "Recuperar contraseña", Icons.Default.Lock)
    object CambiarContrasena : Ruta("cambiar_contrasena", "Cambiar contraseña", Icons.Default.Lock)
    object AgendaBarbero : Ruta("agenda_barbero", "Mi agenda", Icons.AutoMirrored.Filled.EventNote)
    object Ajustes : Ruta("ajustes", "Ajustes", Icons.Default.Settings)
    object Perfil : Ruta("perfil_cliente", "Mi perfil", Icons.Default.Person)
    object Informacion : Ruta("informacion_barberia", "Barbería y ayuda", Icons.Default.Info)
    object DetalleCita : Ruta("cita_detalle/{citaId}", "Detalle de cita", Icons.AutoMirrored.Filled.EventNote)
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
            !context.getSharedPreferences("bookinghub_app", android.content.Context.MODE_PRIVATE)
                .getBoolean("onboarding_completado", false) -> Ruta.Bienvenida.ruta
            else -> Ruta.Registro.ruta
        }
    }

    LaunchedEffect(Unit) {
        com.bookinghub.app.util.SesionExpiradaBus.expiraciones.collect {
            val idBarbero = SesionBarbero.sesionActual?.usuarioId
            val idCliente = SesionCliente.clienteActual?.clienteId
            SesionBarbero.borrar(context)
            SesionCliente.borrarPerfil(context)
            if (idBarbero != null) CitasCache.borrarAgendaBarbero(context, idBarbero)
            if (idCliente != null) CitasCache.borrarCliente(context, idCliente)
            navController.navigate(Ruta.AccesoBarbero.ruta) {
                popUpTo(navController.graph.startDestinationId) { inclusive = true }
                launchSingleTop = true
            }
        }
    }

    LaunchedEffect(rutaActual) {
        val actividad = context as? Activity ?: return@LaunchedEffect
        val uri = actividad.intent?.data
        val idDesdeUri = uri?.takeIf { it.scheme == "bookinghub" && it.host == "cita" }
            ?.pathSegments?.firstOrNull()?.toIntOrNull()
        val id = idDesdeUri ?: actividad.intent?.getIntExtra(Claves.CITA_ID, 0)?.takeIf { it > 0 }
        if (id != null) {
            actividad.intent.data = null
            actividad.intent.removeExtra(Claves.CITA_ID)
            actividad.intent.removeExtra(Claves.ABRIR_CITAS)
            navController.navigate("cita_detalle/$id") {
                launchSingleTop = true
            }
        }
    }

    SideEffect {
        val sensible = rutaActual in setOf(
            Ruta.Registro.ruta, Ruta.AccesoBarbero.ruta, Ruta.CambiarContrasena.ruta,
            Ruta.RecuperarContrasena.ruta
        )
        (context as? com.bookinghub.app.MainActivity)?.establecerPantallaSegura(sensible)
    }

    val pantallasConNavBar = listOf(
        Ruta.Home, Ruta.Catalogo, Ruta.Barberos, Ruta.Reservar, Ruta.MisCitas
    )
    val mostrarNavBar = rutaActual in pantallasConNavBar.map { it.ruta }

    // El controlador puede restaurar una ruta antigua después de cerrar sesión o reiniciar la app.
    LaunchedEffect(rutaActual) {
        val tieneSesionBarbero = SesionBarbero.haySesion
        val tienePerfilCliente = SesionCliente.hayPerfilGuardado
        val esDetalleCita = rutaActual?.startsWith("cita_detalle/") == true
        val rutasCliente = setOf(
            Ruta.Home.ruta,
            Ruta.Catalogo.ruta,
            Ruta.Barberos.ruta,
            Ruta.Reservar.ruta,
            Ruta.MisCitas.ruta,
            Ruta.Ajustes.ruta,
            Ruta.Perfil.ruta,
            Ruta.Informacion.ruta
        )
        val rutasBarbero = setOf(Ruta.AgendaBarbero.ruta, Ruta.Ajustes.ruta, Ruta.Informacion.ruta)
        val destinoSeguro = when {
            tieneSesionBarbero && rutaActual !in rutasBarbero && !esDetalleCita -> Ruta.AgendaBarbero.ruta
            !tieneSesionBarbero && rutaActual == Ruta.AgendaBarbero.ruta -> {
                if (tienePerfilCliente) Ruta.Home.ruta else Ruta.Registro.ruta
            }
            !tieneSesionBarbero && !tienePerfilCliente && (rutaActual in rutasCliente || esDetalleCita) -> Ruta.Registro.ruta
            !tieneSesionBarbero && tienePerfilCliente && rutaActual == Ruta.Registro.ruta -> Ruta.Home.ruta
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
        Box(
            modifier = Modifier.fillMaxSize().padding(paddingValues),
            contentAlignment = androidx.compose.ui.Alignment.TopCenter
        ) {
        NavHost(
            navController = navController,
            startDestination = destinoInicial,
            modifier = Modifier.fillMaxSize().widthIn(max = 840.dp)
        ) {
            composable(Ruta.Bienvenida.ruta) {
                BienvenidaScreen(
                    onFinalizar = {
                        context.getSharedPreferences("bookinghub_app", android.content.Context.MODE_PRIVATE)
                            .edit().putBoolean("onboarding_completado", true).apply()
                        navController.navigate(Ruta.Registro.ruta) {
                            popUpTo(Ruta.Bienvenida.ruta) { inclusive = true }
                        }
                    },
                    onIniciarSesion = {
                        context.getSharedPreferences("bookinghub_app", android.content.Context.MODE_PRIVATE)
                            .edit().putBoolean("onboarding_completado", true).apply()
                        navController.navigate(Ruta.AccesoBarbero.ruta) {
                            popUpTo(Ruta.Bienvenida.ruta) { inclusive = true }
                        }
                    }
                )
            }
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
            composable(Ruta.MisCitas.ruta) {
                MisCitasScreen(onRepetirCita = { cita ->
                    val servicios = cita.detalles.orEmpty().mapNotNull { it.catalogoItemId.takeIf { id -> id > 0 } }
                    navController.navigate(Ruta.Reservar.ruta)
                    navController.currentBackStackEntry?.savedStateHandle?.apply {
                        set("reserva_barbero_id", cita.barberoId.takeIf { it > 0 })
                        set("reserva_servicios_ids", ArrayList(servicios))
                        set("reserva_fecha", null as String?)
                        set("reserva_notas", "")
                        set("reserva_cualquiera_disponible", false)
                    }
                })
            }
            composable(Ruta.AccesoBarbero.ruta) {
                AccesoBarberoScreen(
                    onAccesoExitoso = { rol ->
                        val destino = if (rol.equals("Barbero", ignoreCase = true)) Ruta.AgendaBarbero.ruta else Ruta.Home.ruta
                        navController.navigate(destino) {
                            popUpTo(navController.graph.startDestinationId) { inclusive = true }
                            launchSingleTop = true
                        }
                    },
                    onOlvideContrasena = { navController.navigate(Ruta.RecuperarContrasena.ruta) },
                    onVolver = {
                        if (!navController.popBackStack()) {
                            val rutaDeRegreso = if (SesionCliente.hayPerfilGuardado) {
                                Ruta.Home.ruta
                            } else {
                                Ruta.Registro.ruta
                            }
                            navController.navigate(rutaDeRegreso) { launchSingleTop = true }
                        }
                    }
                )
            }
            composable(Ruta.RecuperarContrasena.ruta) {
                RecuperarContrasenaScreen(onVolver = { navController.popBackStack() })
            }
            composable(Ruta.CambiarContrasena.ruta) {
                CambiarContrasenaScreen(
                    onVolver = { navController.popBackStack() },
                    onContrasenaCambiada = {
                        val idCliente = SesionCliente.clienteActual?.clienteId
                        SesionCliente.borrarPerfil(context)
                        if (idCliente != null) CitasCache.borrarCliente(context, idCliente)
                        navController.navigate(Ruta.AccesoBarbero.ruta) {
                            popUpTo(navController.graph.startDestinationId) { inclusive = true }
                            launchSingleTop = true
                        }
                    }
                )
            }
            composable(Ruta.AgendaBarbero.ruta) {
                AgendaBarberoScreen(
                    onAbrirAjustes = { navController.navigate(Ruta.Ajustes.ruta) },
                    onSesionExpirada = {
                        val usuarioId = SesionBarbero.sesionActual?.usuarioId
                        SesionBarbero.borrar(context)
                        if (usuarioId != null) CitasCache.borrarAgendaBarbero(context, usuarioId)
                        navController.navigate(Ruta.AccesoBarbero.ruta) {
                            popUpTo(Ruta.AgendaBarbero.ruta) { inclusive = true }
                            launchSingleTop = true
                        }
                    }
                )
            }
            composable(Ruta.Ajustes.ruta) {
                AjustesScreen(
                    onVolver = { navController.popBackStack() },
                    onEditarPerfil = { navController.navigate(Ruta.Perfil.ruta) },
                    onInformacion = { navController.navigate(Ruta.Informacion.ruta) },
                    onCambiarContrasena = { navController.navigate(Ruta.CambiarContrasena.ruta) },
                    onCuentaEliminada = {
                        val idCliente = SesionCliente.clienteActual?.clienteId
                        SesionCliente.borrarPerfil(context)
                        if (idCliente != null) CitasCache.borrarCliente(context, idCliente)
                        navController.navigate(Ruta.AccesoBarbero.ruta) {
                            popUpTo(navController.graph.startDestinationId) { inclusive = true }
                            launchSingleTop = true
                        }
                    },
                    onCerrarSesion = {
                        val idBarbero = SesionBarbero.sesionActual?.usuarioId
                        val idCliente = SesionCliente.clienteActual?.clienteId
                        if (idBarbero != null) {
                            SesionBarbero.borrar(context)
                            CitasCache.borrarAgendaBarbero(context, idBarbero)
                        } else {
                            SesionCliente.borrarPerfil(context)
                            if (idCliente != null) CitasCache.borrarCliente(context, idCliente)
                        }
                        val siguienteRuta = if (SesionBarbero.haySesion) {
                            Ruta.AgendaBarbero.ruta
                        } else if (SesionCliente.hayPerfilGuardado) {
                            Ruta.Home.ruta
                        } else {
                            Ruta.Registro.ruta
                        }
                        navController.navigate(siguienteRuta) {
                            popUpTo(navController.graph.startDestinationId) { inclusive = true }
                            launchSingleTop = true
                        }
                    },
                    onRegistrarOtroCliente = {
                        val idCliente = SesionCliente.clienteActual?.clienteId
                        SesionCliente.borrarPerfil(context)
                        if (idCliente != null) CitasCache.borrarCliente(context, idCliente)
                        navController.navigate(Ruta.Registro.ruta) {
                            popUpTo(navController.graph.startDestinationId) { inclusive = true }
                            launchSingleTop = true
                        }
                    }
                )
            }
            composable(Ruta.Perfil.ruta) { PerfilClienteScreen(onVolver = { navController.popBackStack() }) }
            composable(Ruta.Informacion.ruta) { InformacionBarberiaScreen(onVolver = { navController.popBackStack() }) }
            composable(
                route = Ruta.DetalleCita.ruta,
                arguments = listOf(navArgument("citaId") { type = NavType.IntType })
            ) { DetalleCitaScreen(onVolver = { navController.popBackStack() }) }
        }
        }
    }
}
