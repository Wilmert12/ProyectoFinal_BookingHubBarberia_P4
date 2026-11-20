package com.bookinghub.app.ui.viewmodel

import android.content.Context
import com.bookinghub.app.data.model.CitaDto
import com.bookinghub.app.data.model.EstadoCitaDto
import com.bookinghub.app.data.repository.ApiResult
import com.bookinghub.app.data.repository.BarberoRepository
import com.bookinghub.app.data.repository.CitaRepository
import com.bookinghub.app.testing.MainDispatcherRule
import com.bookinghub.app.util.CitasCache
import com.bookinghub.app.util.RecordatoriosCitas
import com.bookinghub.app.util.SesionBarbero
import com.bookinghub.app.data.model.SesionDto
import io.mockk.coEvery
import io.mockk.every
import io.mockk.just
import io.mockk.mockk
import io.mockk.Runs
import io.mockk.mockkObject
import io.mockk.unmockkAll
import kotlinx.coroutines.test.runCurrent
import kotlinx.coroutines.test.runTest
import kotlinx.coroutines.ExperimentalCoroutinesApi
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import java.time.LocalDate
import java.time.DayOfWeek

@OptIn(ExperimentalCoroutinesApi::class)
class AgendaBarberoViewModelTest {
    @get:Rule
    val mainDispatcherRule = MainDispatcherRule()

    private val citaRepository = mockk<CitaRepository>()
    private val barberoRepository = mockk<BarberoRepository>()
    private val context = mockk<Context>(relaxed = true)

    @After
    fun tearDown() = unmockkAll()

    @Test
    fun weeklyViewLoadsSevenDaysAroundTheSelectedWeek() = runTest {
        prepareSession()
        coEvery { citaRepository.listarEstados() } returns ApiResult.Success(emptyList())
        coEvery { citaRepository.listarAgendaBarbero(any()) } returns ApiResult.Success(emptyList())
        coEvery { barberoRepository.listarBloqueos(any()) } returns ApiResult.Success(emptyList())
        val viewModel = AgendaBarberoViewModel(citaRepository, barberoRepository, CitasCache, SesionBarbero, context)
        runCurrent()

        viewModel.alternarVistaSemanal(true)
        runCurrent()

        assertTrue(viewModel.uiState.value.vistaSemanal)
        assertEquals(7, viewModel.uiState.value.citasPorDia.size)
        val inicio = viewModel.uiState.value.citasPorDia.keys.minOrNull()!!
        assertEquals(DayOfWeek.MONDAY, inicio.dayOfWeek)
    }

    @Test
    fun barberCanApplyAValidStatusChangeToTheirLoadedAppointment() = runTest {
        prepareSession()
        val pendiente = CitaDto(citaId = 5, estado = "Pendiente")
        val confirmada = pendiente.copy(estado = "Confirmada", estadoCitaId = 4)
        coEvery { citaRepository.listarEstados() } returns ApiResult.Success(listOf(EstadoCitaDto(4, "Confirmada")))
        coEvery { citaRepository.listarAgendaBarbero(any()) } returns ApiResult.Success(listOf(pendiente))
        coEvery { barberoRepository.listarBloqueos(any()) } returns ApiResult.Success(emptyList())
        coEvery { citaRepository.cambiarEstado(5, 4) } returns ApiResult.Success(confirmada)
        val viewModel = AgendaBarberoViewModel(citaRepository, barberoRepository, CitasCache, SesionBarbero, context)
        runCurrent()

        viewModel.cambiarEstado(5, 4)
        runCurrent()

        assertEquals("Confirmada", viewModel.uiState.value.citas.single().estado)
        assertEquals(null, viewModel.uiState.value.citaEnActualizacion)
    }

    private fun prepareSession() {
        mockkObject(SesionBarbero, CitasCache)
        every { SesionBarbero.sesionActual } returns SesionDto(
            token = "jwt",
            expira = "2099-01-01T00:00:00Z",
            usuarioId = 42,
            nombre = "Luz",
            email = "luz@example.com",
            rol = "Barbero"
        )
        every { CitasCache.leerBarbero(context, 42, any()) } returns null
        every { CitasCache.guardarBarbero(context, 42, any(), any()) } just Runs
    }
}
