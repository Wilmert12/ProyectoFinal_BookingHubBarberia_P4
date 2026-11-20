package com.bookinghub.app.ui.viewmodel

import android.content.Context
import com.bookinghub.app.data.model.BarberoDto
import com.bookinghub.app.data.model.DisponibilidadDto
import com.bookinghub.app.data.model.HorarioLibreDto
import com.bookinghub.app.data.repository.ApiResult
import com.bookinghub.app.data.repository.BarberoRepository
import com.bookinghub.app.data.repository.ClienteRepository
import com.bookinghub.app.testing.MainDispatcherRule
import com.bookinghub.app.util.CitasCache
import com.bookinghub.app.util.SesionCliente
import io.mockk.coEvery
import io.mockk.coVerify
import io.mockk.every
import io.mockk.just
import io.mockk.mockk
import io.mockk.mockkObject
import io.mockk.Runs
import io.mockk.unmockkAll
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.runCurrent
import kotlinx.coroutines.test.runTest
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Rule
import org.junit.Test
import java.time.LocalDate

@OptIn(ExperimentalCoroutinesApi::class)
class BarberosViewModelTest {
    @get:Rule val mainDispatcherRule = MainDispatcherRule()
    private val barberoRepository = mockk<BarberoRepository>()
    private val clienteRepository = mockk<ClienteRepository>()
    private val context = mockk<Context>(relaxed = true)

    @After fun tearDown() = unmockkAll()

    @Test
    fun successfulListFindsTheFirstAvailableSlotAndCachesTheBarber() = runTest {
        prepareCache(null)
        val barber = BarberoDto(barberoId = 5, nombre = "Luz")
        coEvery { barberoRepository.listar() } returns ApiResult.Success(listOf(barber))
        coEvery { barberoRepository.obtenerFoto(5) } returns null
        val date = LocalDate.now().toString()
        coEvery { barberoRepository.consultarDisponibilidad(5, any(), 30) } returns ApiResult.Success(
            DisponibilidadDto(5, date, 30, listOf(HorarioLibreDto("${date}T10:00:00", "${date}T10:30:00")))
        )
        val viewModel = BarberosViewModel(barberoRepository, clienteRepository, context)

        runCurrent()

        assertEquals(listOf(barber), viewModel.uiState.value.barberos)
        assertNotNull(viewModel.uiState.value.proximosHorarios[5])
        io.mockk.verify(exactly = 1) { CitasCache.guardarBarberos(context, listOf(barber)) }
    }

    @Test
    fun networkFailureShowsTheCachedBarberList() = runTest {
        val cached = listOf(BarberoDto(barberoId = 9, nombre = "Carlos"))
        prepareCache(cached)
        coEvery { barberoRepository.listar() } returns ApiResult.NetworkError
        val viewModel = BarberosViewModel(barberoRepository, clienteRepository, context)

        runCurrent()

        assertEquals(cached, viewModel.uiState.value.barberos)
        assertEquals("Sin conexión. Se muestran los últimos barberos guardados.", viewModel.uiState.value.aviso)
    }

    @Test
    fun favoriteActionWithoutClientSessionExplainsTheRequirement() = runTest {
        prepareCache(null)
        mockkObject(SesionCliente)
        every { SesionCliente.hayPerfilGuardado } returns false
        coEvery { barberoRepository.listar() } returns ApiResult.NetworkError
        val viewModel = BarberosViewModel(barberoRepository, clienteRepository, context)
        runCurrent()

        viewModel.alternarFavorito(5)

        assertEquals("Inicia sesión como cliente para guardar favoritos.", viewModel.uiState.value.aviso)
        coVerify(exactly = 0) { clienteRepository.agregarFavorito(any()) }
        coVerify(exactly = 0) { clienteRepository.quitarFavorito(any()) }
    }

    private fun prepareCache(barberos: List<BarberoDto>?) {
        mockkObject(CitasCache)
        every { CitasCache.leerBarberos(context) } returns barberos
        every { CitasCache.guardarBarberos(context, any()) } just Runs
    }
}
