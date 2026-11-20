package com.bookinghub.app.ui.viewmodel

import android.content.Context
import androidx.lifecycle.SavedStateHandle
import com.bookinghub.app.data.model.BarberoDto
import com.bookinghub.app.data.model.CatalogoItemDto
import com.bookinghub.app.data.model.CitaDto
import com.bookinghub.app.data.model.DisponibilidadDto
import com.bookinghub.app.data.model.EstadoCitaDto
import com.bookinghub.app.data.model.HorarioLibreDto
import com.bookinghub.app.data.repository.ApiResult
import com.bookinghub.app.data.repository.BarberoRepository
import com.bookinghub.app.data.repository.CatalogoRepository
import com.bookinghub.app.data.repository.CitaRepository
import com.bookinghub.app.testing.MainDispatcherRule
import com.bookinghub.app.util.ClienteSesion
import com.bookinghub.app.util.RecordatoriosCitas
import com.bookinghub.app.util.SesionCliente
import io.mockk.coEvery
import io.mockk.coVerify
import io.mockk.every
import io.mockk.just
import io.mockk.mockkObject
import io.mockk.mockk
import io.mockk.Runs
import io.mockk.unmockkAll
import kotlinx.coroutines.test.runCurrent
import kotlinx.coroutines.test.runTest
import kotlinx.coroutines.ExperimentalCoroutinesApi
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import java.math.BigDecimal
import java.time.LocalDate

@OptIn(ExperimentalCoroutinesApi::class)
class ReservarViewModelTest {
    @get:Rule
    val mainDispatcherRule = MainDispatcherRule()

    private val barberoRepository = mockk<BarberoRepository>()
    private val catalogoRepository = mockk<CatalogoRepository>()
    private val citaRepository = mockk<CitaRepository>()
    private val context = mockk<Context>(relaxed = true)

    @After
    fun tearDown() = unmockkAll()

    @Test
    fun anyAvailableBarber_mergesOpenSlotsAndKeepsTheAssignedBarber() = runTest {
        val barbero = BarberoDto(barberoId = 11, nombre = "Luz", activo = true)
        val servicio = CatalogoItemDto(
            catalogoItemId = 7,
            nombre = "Corte",
            esServicio = true,
            precio = BigDecimal("450.00"),
            duracionMin = 45
        )
        val horario = HorarioLibreDto("2030-05-07T10:00:00", "2030-05-07T10:45:00")
        coEvery { barberoRepository.listar() } returns ApiResult.Success(listOf(barbero))
        coEvery { catalogoRepository.listar() } returns ApiResult.Success(listOf(servicio))
        coEvery { citaRepository.listarEstados() } returns ApiResult.Success(listOf(EstadoCitaDto(1, "Pendiente")))
        coEvery { barberoRepository.consultarDisponibilidad(11, any(), 45) } returns
            ApiResult.Success(DisponibilidadDto(11, LocalDate.now().plusDays(2).toString(), 45, listOf(horario)))

        val viewModel = ReservarViewModel(
            barberoRepository,
            catalogoRepository,
            citaRepository,
            SesionCliente,
            context,
            SavedStateHandle()
        )
        runCurrent()
        viewModel.toggleServicio(servicio)
        viewModel.seleccionarCualquieraDisponible()
        viewModel.seleccionarFecha(LocalDate.now().plusDays(2))
        runCurrent()

        val result = viewModel.uiState.value
        assertFalse(result.cargandoDatos)
        assertTrue(result.cualquieraDisponible)
        assertEquals(listOf(horario), result.horariosDisponibles)
        assertEquals(barbero, result.barberoPorHorario[horario.inicio])
        assertEquals(45, result.duracionTotalMin)
        assertEquals(BigDecimal("450.00"), result.totalEstimado)
        coVerify(exactly = 1) { barberoRepository.consultarDisponibilidad(11, any(), 45) }
    }

    @Test
    fun restoredBookingSelectionIgnoresInactiveServices() = runTest {
        val barbero = BarberoDto(barberoId = 11, nombre = "Luz")
        val servicioActivo = CatalogoItemDto(7, "Corte", esServicio = true, precio = BigDecimal("450"), duracionMin = 30)
        val servicioInactivo = CatalogoItemDto(8, "Barba", esServicio = true, activo = false, precio = BigDecimal("250"), duracionMin = 30)
        coEvery { barberoRepository.listar() } returns ApiResult.Success(listOf(barbero))
        coEvery { catalogoRepository.listar() } returns ApiResult.Success(listOf(servicioActivo, servicioInactivo))
        coEvery { citaRepository.listarEstados() } returns ApiResult.Success(listOf(EstadoCitaDto(1, "Pendiente")))
        val estado = SavedStateHandle(
            mapOf(
                "reserva_barbero_id" to 11,
                "reserva_servicios_ids" to arrayListOf(7, 8),
                "reserva_fecha" to LocalDate.now().plusDays(2).toString()
            )
        )

        val viewModel = ReservarViewModel(barberoRepository, catalogoRepository, citaRepository, SesionCliente, context, estado)
        runCurrent()

        assertEquals(listOf(servicioActivo), viewModel.uiState.value.serviciosSeleccionados)
        assertEquals(listOf(servicioActivo), viewModel.uiState.value.servicios)
    }

    @Test
    fun bookingConflictShowsServerMessageAndKeepsTheSelectedService() = runTest {
        mockkObject(SesionCliente)
        every { SesionCliente.clienteActual } returns ClienteSesion(1, "Ana", token = "jwt")
        val barbero = BarberoDto(barberoId = 11, nombre = "Luz", activo = true)
        val servicio = CatalogoItemDto(7, "Corte", esServicio = true, precio = BigDecimal("450"), duracionMin = 30)
        val fecha = LocalDate.now().plusDays(3)
        val horario = HorarioLibreDto("${fecha}T10:00:00", "${fecha}T10:30:00")
        coEvery { barberoRepository.listar() } returns ApiResult.Success(listOf(barbero))
        coEvery { catalogoRepository.listar() } returns ApiResult.Success(listOf(servicio))
        coEvery { citaRepository.listarEstados() } returns ApiResult.Success(listOf(EstadoCitaDto(1, "Pendiente")))
        coEvery { barberoRepository.consultarDisponibilidad(11, fecha.toString(), 30) } returns
            ApiResult.Success(DisponibilidadDto(11, fecha.toString(), 30, listOf(horario)))
        coEvery { citaRepository.crear(any()) } returns ApiResult.HttpError(409, "El horario fue tomado")

        val viewModel = ReservarViewModel(
            barberoRepository,
            catalogoRepository,
            citaRepository,
            SesionCliente,
            context,
            SavedStateHandle()
        )
        runCurrent()
        viewModel.seleccionarBarbero(barbero)
        viewModel.toggleServicio(servicio)
        viewModel.seleccionarFecha(fecha)
        runCurrent()
        viewModel.seleccionarHorario(horario)
        viewModel.crearCita()
        runCurrent()

        assertEquals("El horario fue tomado", viewModel.uiState.value.error)
        assertEquals(listOf(servicio), viewModel.uiState.value.serviciosSeleccionados)
        assertFalse(viewModel.uiState.value.enviandoReserva)
        assertFalse(viewModel.uiState.value.citaCreada)
        coVerify(exactly = 1) { citaRepository.crear(match { it.barberoId == 11 && it.clienteId == 1 }) }
    }

    @Test
    fun successfulBookingSchedulesReminderAndClearsRestoredSelection() = runTest {
        mockkObject(SesionCliente, RecordatoriosCitas)
        every { SesionCliente.clienteActual } returns ClienteSesion(1, "Ana", token = "jwt")
        val barbero = BarberoDto(barberoId = 11, nombre = "Luz", activo = true)
        val servicio = CatalogoItemDto(7, "Corte", esServicio = true, precio = BigDecimal("450"), duracionMin = 30)
        val fecha = LocalDate.now().plusDays(3)
        val slot = HorarioLibreDto("${fecha}T10:00:00", "${fecha}T10:30:00")
        val creada = CitaDto(citaId = 21, clienteId = 1, barberoId = 11, estado = "Pendiente")
        coEvery { barberoRepository.listar() } returns ApiResult.Success(listOf(barbero))
        coEvery { catalogoRepository.listar() } returns ApiResult.Success(listOf(servicio))
        coEvery { citaRepository.listarEstados() } returns ApiResult.Success(listOf(EstadoCitaDto(1, "Pendiente")))
        coEvery { barberoRepository.consultarDisponibilidad(11, fecha.toString(), 30) } returns
            ApiResult.Success(DisponibilidadDto(11, fecha.toString(), 30, listOf(slot)))
        coEvery { citaRepository.crear(any()) } returns ApiResult.Success(creada)
        every { RecordatoriosCitas.programar(context, creada) } just Runs
        val savedState = SavedStateHandle()
        val viewModel = ReservarViewModel(barberoRepository, catalogoRepository, citaRepository, SesionCliente, context, savedState)
        runCurrent()
        viewModel.seleccionarBarbero(barbero)
        viewModel.toggleServicio(servicio)
        viewModel.seleccionarFecha(fecha)
        runCurrent()
        viewModel.seleccionarHorario(slot)

        viewModel.crearCita()
        runCurrent()

        assertTrue(viewModel.uiState.value.citaCreada)
        assertFalse(viewModel.uiState.value.enviandoReserva)
        coVerify(exactly = 1) { citaRepository.crear(match { it.total == BigDecimal("450") && it.fechaInicio == slot.inicio }) }
        io.mockk.verify(exactly = 1) { RecordatoriosCitas.programar(context, creada) }
        assertEquals(null, savedState.get<Int>("reserva_barbero_id"))
        assertEquals(null, savedState.get<String>("reserva_fecha"))
    }

    @Test
    fun incompleteBookingIsRejectedWithoutCallingTheApi() = runTest {
        coEvery { barberoRepository.listar() } returns ApiResult.Success(emptyList())
        coEvery { catalogoRepository.listar() } returns ApiResult.Success(emptyList())
        coEvery { citaRepository.listarEstados() } returns ApiResult.Success(listOf(EstadoCitaDto(1, "Pendiente")))
        val viewModel = ReservarViewModel(
            barberoRepository,
            catalogoRepository,
            citaRepository,
            SesionCliente,
            context,
            SavedStateHandle()
        )
        runCurrent()

        viewModel.crearCita()

        assertEquals("Completa todos los datos de la reserva e inténtalo de nuevo.", viewModel.uiState.value.error)
        coVerify(exactly = 0) { citaRepository.crear(any()) }
    }
}
