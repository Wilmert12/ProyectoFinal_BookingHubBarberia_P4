package com.bookinghub.app.ui

import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.assertIsNotEnabled
import androidx.compose.ui.test.junit4.v2.createComposeRule
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performTextInput
import androidx.test.ext.junit.runners.AndroidJUnit4
import androidx.test.platform.app.InstrumentationRegistry
import com.bookinghub.app.data.model.BarberoDto
import com.bookinghub.app.data.model.EstadoCitaDto
import com.bookinghub.app.data.model.LoginDto
import com.bookinghub.app.data.model.RegistroClienteRequest
import com.bookinghub.app.data.model.SesionDto
import com.bookinghub.app.data.repository.ApiResult
import com.bookinghub.app.data.repository.AuthRepository
import com.bookinghub.app.data.repository.BarberoRepository
import com.bookinghub.app.data.repository.CatalogoRepository
import com.bookinghub.app.data.repository.CitaRepository
import com.bookinghub.app.ui.screens.AgendaBarberoScreen
import com.bookinghub.app.ui.screens.RegistroScreen
import com.bookinghub.app.ui.screens.ReservarScreen
import com.bookinghub.app.ui.theme.BookingHubTheme
import com.bookinghub.app.ui.viewmodel.AgendaBarberoViewModel
import com.bookinghub.app.ui.viewmodel.AccesoBarberoViewModel
import com.bookinghub.app.ui.viewmodel.ClienteViewModel
import com.bookinghub.app.ui.viewmodel.ReservarViewModel
import com.bookinghub.app.util.CitasCache
import com.bookinghub.app.util.SesionCliente
import com.bookinghub.app.util.SesionBarbero
import io.mockk.coEvery
import io.mockk.coVerify
import io.mockk.every
import io.mockk.just
import io.mockk.mockk
import io.mockk.mockkObject
import io.mockk.Runs
import io.mockk.unmockkAll
import kotlinx.coroutines.ExperimentalCoroutinesApi
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith

@OptIn(ExperimentalCoroutinesApi::class)
@RunWith(AndroidJUnit4::class)
class FlujosPrincipalesUiTest {
    @get:Rule
    val composeRule = createComposeRule()

    @After
    fun tearDown() = unmockkAll()

    @Test
    fun registrationStartsWithRequiredFieldsAndBlocksAnInvalidSubmission() {
        val context = InstrumentationRegistry.getInstrumentation().targetContext
        val viewModel = ClienteViewModel(mockk<AuthRepository>(), SesionCliente, context)

        composeRule.setContent {
            BookingHubTheme {
                RegistroScreen(onRegistroExitoso = {}, onAccesoBarbero = {}, viewModel = viewModel)
            }
        }

        composeRule.onNodeWithText("Regístrate para reservar").assertIsDisplayed()
        composeRule.onNodeWithText("Nombre completo").assertIsDisplayed()
        composeRule.onNodeWithText("Teléfono").assertIsDisplayed()
        composeRule.onNodeWithText("Registrarme").assertIsNotEnabled()
    }

    @Test
    fun registrationSubmitsTheFormAndCompletesTheFlow() {
        val context = InstrumentationRegistry.getInstrumentation().targetContext
        val authRepository = mockk<AuthRepository>()
        val request = RegistroClienteRequest("Ana Luna", "8091234567", "ana@example.com", "Password1")
        val session = SesionDto(
            token = "jwt-client",
            expira = "2099-01-01T00:00:00Z",
            nombre = "Ana Luna",
            email = request.email,
            rol = "Cliente",
            clienteId = 5
        )
        mockkObject(SesionCliente)
        every { SesionCliente.guardar(context, session) } returns true
        coEvery { authRepository.registrarCliente(request) } returns ApiResult.Success(session)
        val viewModel = ClienteViewModel(authRepository, SesionCliente, context)
        val registroTerminado = java.util.concurrent.atomic.AtomicBoolean(false)
        composeRule.setContent {
            BookingHubTheme {
                RegistroScreen(
                    onRegistroExitoso = { registroTerminado.set(true) },
                    onAccesoBarbero = {},
                    viewModel = viewModel
                )
            }
        }

        composeRule.onNodeWithText("Nombre completo").performTextInput(request.nombre)
        composeRule.onNodeWithText("Teléfono").performTextInput(request.telefono)
        composeRule.onNodeWithText("Correo electrónico").performTextInput(request.email)
        composeRule.onNodeWithText("Contraseña").performTextInput(request.password)
        composeRule.onNodeWithText("Registrarme").assertIsDisplayed().performClick()
        composeRule.waitUntil(10_000) { registroTerminado.get() }

        coVerify(exactly = 1) { authRepository.registrarCliente(request) }
    }

    @Test
    fun barberLoginStoresOnlyTheBarberSession() {
        val context = InstrumentationRegistry.getInstrumentation().targetContext
        val authRepository = mockk<AuthRepository>()
        val session = SesionDto(
            token = "jwt-barber",
            expira = "2099-01-01T00:00:00Z",
            usuarioId = 42,
            nombre = "Luz",
            email = "luz@example.com",
            rol = "Barbero"
        )
        mockkObject(SesionBarbero, SesionCliente)
        every { SesionBarbero.guardar(context, session) } returns true
        every { SesionCliente.borrarPerfil(context) } just Runs
        coEvery { authRepository.iniciarSesion(LoginDto(session.email, "Password1")) } returns ApiResult.Success(session)
        val viewModel = AccesoBarberoViewModel(authRepository, SesionBarbero, SesionCliente, context)
        composeRule.setContent { BookingHubTheme { androidx.compose.material3.Text("Acceso") } }

        composeRule.runOnIdle { viewModel.iniciarSesion(" ${session.email} ", "Password1") }
        composeRule.waitUntil(10_000) { viewModel.uiState.value.sesionIniciada }

        assertEquals("Barbero", viewModel.uiState.value.rol)
        coVerify(exactly = 1) { authRepository.iniciarSesion(LoginDto(session.email, "Password1")) }
        io.mockk.verify(exactly = 1) { SesionBarbero.guardar(context, session) }
        io.mockk.verify(exactly = 1) { SesionCliente.borrarPerfil(context) }
    }

    @Test
    fun loginRefusesAnUnsupportedAccountRole() {
        val context = InstrumentationRegistry.getInstrumentation().targetContext
        val authRepository = mockk<AuthRepository>()
        val session = SesionDto(
            token = "jwt-admin",
            expira = "2099-01-01T00:00:00Z",
            nombre = "Admin",
            email = "admin@example.com",
            rol = "Administrador"
        )
        mockkObject(SesionBarbero, SesionCliente)
        coEvery { authRepository.iniciarSesion(LoginDto(session.email, "Password1")) } returns ApiResult.Success(session)
        val viewModel = AccesoBarberoViewModel(authRepository, SesionBarbero, SesionCliente, context)
        composeRule.setContent { BookingHubTheme { androidx.compose.material3.Text("Acceso") } }

        composeRule.runOnIdle { viewModel.iniciarSesion(session.email, "Password1") }
        composeRule.waitUntil(10_000) { viewModel.uiState.value.error != null }

        assertEquals("Esta cuenta no tiene acceso desde la aplicación móvil.", viewModel.uiState.value.error)
        io.mockk.verify(exactly = 0) { SesionBarbero.guardar(any(), any()) }
        io.mockk.verify(exactly = 0) { SesionCliente.guardar(any(), any()) }
    }

    @Test
    fun reservationScreenRendersBarberAndServiceOptions() {
        val context = InstrumentationRegistry.getInstrumentation().targetContext
        val barberos = mockk<BarberoRepository>()
        val catalogo = mockk<CatalogoRepository>()
        val citas = mockk<CitaRepository>()
        coEvery { barberos.listar() } returns ApiResult.Success(
            listOf(BarberoDto(barberoId = 11, nombre = "Luz", activo = true))
        )
        coEvery { catalogo.listar() } returns ApiResult.Success(
            listOf(com.bookinghub.app.data.model.CatalogoItemDto(7, "Corte", esServicio = true))
        )
        coEvery { citas.listarEstados() } returns ApiResult.Success(listOf(EstadoCitaDto(1, "Pendiente")))
        val viewModel = ReservarViewModel(barberos, catalogo, citas, SesionCliente, context, androidx.lifecycle.SavedStateHandle())

        composeRule.setContent {
            BookingHubTheme { ReservarScreen(onCitaCreada = {}, viewModel = viewModel) }
        }
        composeRule.waitUntil(10_000) { !viewModel.uiState.value.cargandoDatos }

        composeRule.onNodeWithText("1. Selecciona tu Barbero").assertIsDisplayed()
        composeRule.onNodeWithText("Luz").assertIsDisplayed()
        composeRule.onNodeWithText("2. Selecciona los Servicios").assertIsDisplayed()
        composeRule.onNodeWithText("Corte").assertIsDisplayed()
    }

    @Test
    fun barberCanSwitchToWeeklyAgendaAndSeeTheEmptyState() {
        val context = InstrumentationRegistry.getInstrumentation().targetContext
        val citas = mockk<CitaRepository>()
        val barberos = mockk<BarberoRepository>()
        mockkObject(SesionBarbero, CitasCache)
        every { SesionBarbero.sesionActual } returns SesionDto(
            token = "token",
            expira = "2099-01-01T00:00:00Z",
            usuarioId = 42,
            nombre = "Luz",
            email = "luz@example.com",
            rol = "Barbero"
        )
        every { CitasCache.leerBarbero(context, 42, any()) } returns null
        every { CitasCache.guardarBarbero(context, 42, any(), any()) } returns Unit
        coEvery { citas.listarEstados() } returns ApiResult.Success(listOf(EstadoCitaDto(1, "Pendiente")))
        coEvery { citas.listarAgendaBarbero(any()) } returns ApiResult.Success(emptyList())
        coEvery { barberos.listarBloqueos(any()) } returns ApiResult.Success(emptyList())
        val viewModel = AgendaBarberoViewModel(citas, barberos, CitasCache, SesionBarbero, context)

        composeRule.setContent {
            BookingHubTheme { AgendaBarberoScreen(onAbrirAjustes = {}, onSesionExpirada = {}, viewModel = viewModel) }
        }
        composeRule.waitUntil(10_000) { !viewModel.uiState.value.cargando && !viewModel.uiState.value.cargandoBloqueos }
        composeRule.onNodeWithText("Semana").performClick()
        composeRule.waitUntil(10_000) { viewModel.uiState.value.vistaSemanal && !viewModel.uiState.value.cargando }

        assertEquals(7, viewModel.uiState.value.citasPorDia.size)
        composeRule.onNodeWithText("No tienes citas para esta semana.").assertIsDisplayed()
    }
}
