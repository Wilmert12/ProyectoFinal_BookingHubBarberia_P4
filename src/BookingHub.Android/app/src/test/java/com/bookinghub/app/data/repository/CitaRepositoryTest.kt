package com.bookinghub.app.data.repository

import com.bookinghub.app.data.api.ApiClient
import com.bookinghub.app.data.api.CambiarEstadoCitaRequest
import com.bookinghub.app.data.api.CancelarCitaRequest
import com.bookinghub.app.data.api.CitaApi
import com.bookinghub.app.data.api.ReprogramarCitaRequest
import com.bookinghub.app.data.model.CitaDto
import com.bookinghub.app.data.model.EstadoCitaDto
import com.bookinghub.app.data.model.PaginaCitasDto
import io.mockk.coEvery
import io.mockk.coVerify
import io.mockk.every
import io.mockk.mockk
import kotlinx.coroutines.test.runTest
import org.junit.Assert.assertEquals
import org.junit.Test
import retrofit2.Response

class CitaRepositoryTest {
    private val apiClient = mockk<ApiClient>()
    private val api = mockk<CitaApi>()
    private val estadosApi = mockk<com.bookinghub.app.data.api.EstadosCitaApi>()
    private val repository = CitaRepository(apiClient)

    init {
        every { apiClient.citaApi } returns api
        every { apiClient.estadosCitaApi } returns estadosApi
    }

    @Test
    fun listMethodsKeepTheirFiltersAndPagination() = runTest {
        val appointment = CitaDto(citaId = 7)
        val page = PaginaCitasDto(listOf(appointment), pagina = 2, tamano = 10, tieneMas = false)
        val status = EstadoCitaDto(1, "Pendiente")
        coEvery { api.getCitas(2, null, "2030-01-01") } returns Response.success(listOf(appointment))
        coEvery { api.getMisCitasCliente() } returns Response.success(listOf(appointment))
        coEvery { api.getMisCitasBarbero("2030-01-01") } returns Response.success(listOf(appointment))
        coEvery { api.getHistorial(2, 10) } returns Response.success(page)
        coEvery { estadosApi.getEstadosCita() } returns Response.success(listOf(status))

        assertEquals(ApiResult.Success(listOf(appointment)), executeApiRequest { api.getCitas(2, null, "2030-01-01") })
        assertEquals(ApiResult.Success(listOf(appointment)), repository.listarDelCliente())
        assertEquals(ApiResult.Success(listOf(appointment)), repository.listarAgendaBarbero("2030-01-01"))
        assertEquals(ApiResult.Success(page), repository.listarHistorial(2, 10))
        assertEquals(ApiResult.Success(listOf(status)), repository.listarEstados())
    }

    @Test
    fun appointmentMutationsSendTheirPayloadAndReturnTheUpdatedAppointment() = runTest {
        val appointment = CitaDto(citaId = 7)
        coEvery { api.getCita(7) } returns Response.success(appointment)
        coEvery { api.crearCita(appointment) } returns Response.success(appointment)
        coEvery { api.cancelarCita(7, CancelarCitaRequest("Cambio de planes")) } returns Response.success(appointment)
        coEvery { api.reprogramarCita(7, ReprogramarCitaRequest("2030-01-01T11:00:00")) } returns Response.success(appointment)
        coEvery { api.cambiarEstado(7, CambiarEstadoCitaRequest(2)) } returns Response.success(appointment)

        assertEquals(ApiResult.Success(appointment), repository.buscar(7))
        assertEquals(ApiResult.Success(appointment), repository.crear(appointment))
        assertEquals(ApiResult.Success(appointment), repository.cancelar(7, "Cambio de planes"))
        assertEquals(ApiResult.Success(appointment), repository.reprogramar(7, "2030-01-01T11:00:00"))
        assertEquals(ApiResult.Success(appointment), repository.cambiarEstado(7, 2))
        coVerify(exactly = 1) { api.cancelarCita(7, CancelarCitaRequest("Cambio de planes")) }
        coVerify(exactly = 1) { api.reprogramarCita(7, ReprogramarCitaRequest("2030-01-01T11:00:00")) }
        coVerify(exactly = 1) { api.cambiarEstado(7, CambiarEstadoCitaRequest(2)) }
    }
}
