package com.bookinghub.app.ui.viewmodel

import android.content.Context
import com.bookinghub.app.data.model.CitaDto
import com.bookinghub.app.data.model.PaginaCitasDto
import com.bookinghub.app.data.repository.ApiResult
import com.bookinghub.app.data.repository.CitaRepository
import com.bookinghub.app.data.repository.ClienteRepository
import com.bookinghub.app.testing.MainDispatcherRule
import com.bookinghub.app.util.CitasCache
import com.bookinghub.app.util.ClienteSesion
import com.bookinghub.app.util.RecordatoriosCitas
import com.bookinghub.app.util.SesionCliente
import io.mockk.coEvery
import io.mockk.coVerify
import io.mockk.every
import io.mockk.just
import io.mockk.mockk
import io.mockk.Runs
import io.mockk.mockkObject
import io.mockk.unmockkAll
import kotlinx.coroutines.test.runCurrent
import kotlinx.coroutines.test.runTest
import kotlinx.coroutines.ExperimentalCoroutinesApi
import app.cash.turbine.test
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import java.time.LocalDateTime

@OptIn(ExperimentalCoroutinesApi::class)
class CitasViewModelTest {
    @get:Rule
    val mainDispatcherRule = MainDispatcherRule()

    private val citaRepository = mockk<CitaRepository>()
    private val clienteRepository = mockk<ClienteRepository>()
    private val context = mockk<Context>(relaxed = true)

    @After
    fun tearDown() = unmockkAll()

    @Test
    fun noSavedProfileStopsLoadingAndDoesNotCallApi() = runTest {
        mockkObject(SesionCliente)
        every { SesionCliente.clienteActual } returns null

        val viewModel = CitasViewModel(citaRepository, clienteRepository, CitasCache, SesionCliente, context)

        assertEquals("No hay un perfil de cliente guardado en este dispositivo.", viewModel.uiState.value.error)
        coVerify(exactly = 0) { citaRepository.listarDelCliente() }
    }

    @Test
    fun successfulLoadDeduplicatesUpcomingAndHistoryAndRecordsPagination() = runTest {
        prepareClientSession()
        val shared = cita(1, "Pendiente")
        val past = cita(2, "Completada")
        coEvery { citaRepository.listarDelCliente() } returns ApiResult.Success(listOf(shared))
        coEvery { citaRepository.listarHistorial(1, 10) } returns
            ApiResult.Success(PaginaCitasDto(listOf(shared, past), pagina = 1, tamano = 10, tieneMas = true))

        val viewModel = CitasViewModel(citaRepository, clienteRepository, CitasCache, SesionCliente, context)
        viewModel.uiState.test {
            assertTrue(awaitItem().cargando)
            runCurrent()
            val loaded = awaitItem()
            assertEquals(listOf(1, 2), loaded.citas.map { it.citaId })
            assertEquals(1, loaded.paginaHistorial)
            assertTrue(loaded.historialTieneMas)
            assertFalse(loaded.cargando)
            cancelAndIgnoreRemainingEvents()
        }
        coVerify(exactly = 1) { citaRepository.listarDelCliente() }
        coVerify(exactly = 1) { citaRepository.listarHistorial(1, 10) }
    }

    @Test
    fun loadingMoreHistoryUsesNextPageAndAppendsDistinctAppointments() = runTest {
        prepareClientSession()
        val first = cita(1, "Pendiente")
        val second = cita(2, "Completada")
        coEvery { citaRepository.listarDelCliente() } returns ApiResult.Success(listOf(first))
        coEvery { citaRepository.listarHistorial(1, 10) } returns
            ApiResult.Success(PaginaCitasDto(listOf(first), 1, 10, true))
        coEvery { citaRepository.listarHistorial(2, 10) } returns
            ApiResult.Success(PaginaCitasDto(listOf(first, second), 2, 10, false))
        val viewModel = CitasViewModel(citaRepository, clienteRepository, CitasCache, SesionCliente, context)
        runCurrent()

        viewModel.cargarMasHistorial()
        runCurrent()

        assertEquals(listOf(1, 2), viewModel.uiState.value.citas.map { it.citaId })
        assertEquals(2, viewModel.uiState.value.paginaHistorial)
        assertFalse(viewModel.uiState.value.historialTieneMas)
        coVerify(exactly = 1) { citaRepository.listarHistorial(2, 10) }
    }

    @Test
    fun cancellationUpdatesTheAppointmentAndCancelsItsReminder() = runTest {
        prepareClientSession()
        mockkObject(CitasCache, RecordatoriosCitas)
        every { CitasCache.leerCliente(context, 1) } returns null
        every { CitasCache.guardarCliente(context, 1, any()) } just Runs
        every { RecordatoriosCitas.sincronizar(context, any()) } just Runs
        every { RecordatoriosCitas.cancelar(context, any()) } just Runs
        val before = cita(1, "Pendiente")
        val cancelled = before.copy(estado = "Cancelada")
        coEvery { citaRepository.listarDelCliente() } returns ApiResult.Success(listOf(before))
        coEvery { citaRepository.listarHistorial(1, 10) } returns ApiResult.Success(PaginaCitasDto(emptyList(), 1, 10, false))
        coEvery { citaRepository.cancelar(1, "Cancelada desde la app") } returns ApiResult.Success(cancelled)
        val viewModel = CitasViewModel(citaRepository, clienteRepository, CitasCache, SesionCliente, context)
        runCurrent()

        viewModel.cancelar(1)
        runCurrent()

        assertEquals("Cancelada", viewModel.uiState.value.citas.single().estado)
        assertEquals(null, viewModel.uiState.value.citaEnActualizacion)
        coVerify(exactly = 1) { RecordatoriosCitas.cancelar(context, 1) }
    }

    private fun prepareClientSession() {
        mockkObject(SesionCliente, CitasCache, RecordatoriosCitas)
        every { SesionCliente.clienteActual } returns ClienteSesion(1, "Ana", token = "jwt")
        every { CitasCache.leerCliente(context, 1) } returns null
        every { CitasCache.guardarCliente(context, 1, any()) } just Runs
        every { RecordatoriosCitas.sincronizar(context, any()) } just Runs
        every { RecordatoriosCitas.cancelar(context, any()) } just Runs
    }

    private fun cita(id: Int, estado: String) = CitaDto(
        citaId = id,
        fechaInicio = LocalDateTime.now().plusDays(5).withSecond(0).withNano(0).toString(),
        estado = estado
    )
}
