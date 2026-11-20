package com.bookinghub.app.data.repository

import com.bookinghub.app.data.api.ApiClient
import com.bookinghub.app.data.api.BarberoApi
import com.bookinghub.app.data.model.BarberoDto
import com.bookinghub.app.data.model.BloqueoAgendaDto
import com.bookinghub.app.data.model.CrearBloqueoAgendaRequest
import com.bookinghub.app.data.model.DisponibilidadDto
import com.bookinghub.app.data.model.HorarioLibreDto
import com.bookinghub.app.data.model.ResenaDto
import io.mockk.coEvery
import io.mockk.coVerify
import io.mockk.every
import io.mockk.mockk
import kotlinx.coroutines.test.runTest
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.ResponseBody.Companion.toResponseBody
import org.junit.Assert.assertArrayEquals
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test
import retrofit2.Response

class BarberoRepositoryTest {
    private val apiClient = mockk<ApiClient>()
    private val api = mockk<BarberoApi>()
    private val repository = BarberoRepository(apiClient)

    init { every { apiClient.barberoApi } returns api }

    @Test
    fun listAndAvailabilityDelegateTheirArguments() = runTest {
        val barber = BarberoDto(barberoId = 5, nombre = "Luz")
        val slot = HorarioLibreDto("2030-01-01T10:00:00", "2030-01-01T10:30:00")
        val availability = DisponibilidadDto(5, "2030-01-01", 30, listOf(slot))
        coEvery { api.getBarberos() } returns Response.success(listOf(barber))
        coEvery { api.getDisponibilidad(5, "2030-01-01", 30) } returns Response.success(availability)

        assertEquals(ApiResult.Success(listOf(barber)), repository.listar())
        assertEquals(ApiResult.Success(availability), repository.consultarDisponibilidad(5, "2030-01-01", 30))
        coVerify(exactly = 1) { api.getDisponibilidad(5, "2030-01-01", 30) }
    }

    @Test
    fun reviewsAndDayBlocksAreMappedToTheApi() = runTest {
        val review = ResenaDto(resenaId = 3, barberoId = 5, puntaje = 5)
        val block = BloqueoAgendaDto(indisponibilidadId = 9, tipo = "Descanso")
        val request = CrearBloqueoAgendaRequest("2030-01-01T00:00:00", "2030-01-02T00:00:00")
        coEvery { api.getResenas(5) } returns Response.success(listOf(review))
        coEvery { api.getMisBloqueos("2030-01-01", "2030-01-01") } returns Response.success(listOf(block))
        coEvery { api.crearBloqueo(request) } returns Response.success(block)
        coEvery { api.eliminarBloqueo(9) } returns Response.success(Unit)

        assertEquals(ApiResult.Success(listOf(review)), repository.listarResenas(5))
        assertEquals(ApiResult.Success(listOf(block)), repository.listarBloqueos("2030-01-01"))
        assertEquals(ApiResult.Success(block), repository.crearBloqueo(request))
        assertEquals(ApiResult.Success(Unit), repository.eliminarBloqueo(9))
    }

    @Test
    fun photoResponseReturnsBytesAndUnavailablePhotoReturnsNull() = runTest {
        val bytes = byteArrayOf(4, 8, 15, 16)
        coEvery { api.getFoto(5) } returns Response.success(bytes.toResponseBody("image/jpeg".toMediaType()))
        coEvery { api.getFoto(6) } returns Response.error(404, "".toResponseBody())

        assertArrayEquals(bytes, repository.obtenerFoto(5))
        assertNull(repository.obtenerFoto(6))
    }
}
