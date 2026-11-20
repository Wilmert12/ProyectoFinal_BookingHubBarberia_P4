package com.bookinghub.app.data.repository

import com.bookinghub.app.data.api.ApiClient
import com.bookinghub.app.data.api.ClienteApi
import com.bookinghub.app.data.model.ActualizarPerfilClienteRequest
import com.bookinghub.app.data.model.BarberoDto
import com.bookinghub.app.data.model.ClienteDto
import com.bookinghub.app.data.model.CrearResenaRequest
import com.bookinghub.app.data.model.ResenaDto
import io.mockk.coEvery
import io.mockk.coVerify
import io.mockk.every
import io.mockk.mockk
import kotlinx.coroutines.test.runTest
import okhttp3.MultipartBody
import org.junit.Assert.assertEquals
import org.junit.Test
import retrofit2.Response

class ClienteRepositoryTest {
    private val apiClient = mockk<ApiClient>()
    private val api = mockk<ClienteApi>()
    private val repository = ClienteRepository(apiClient)

    init { every { apiClient.clienteApi } returns api }

    @Test
    fun createAndReadProfileDelegateToApi() = runTest {
        val client = ClienteDto(clienteId = 3, nombre = "Ana")
        coEvery { api.crearCliente(client) } returns Response.success(client)
        coEvery { api.getMiPerfil() } returns Response.success(client)

        assertEquals(ApiResult.Success(client), repository.crearCliente(client))
        assertEquals(ApiResult.Success(client), repository.obtenerPerfil())
        coVerify(exactly = 1) { api.crearCliente(client) }
        coVerify(exactly = 1) { api.getMiPerfil() }
    }

    @Test
    fun profileUpdateAndPhotoUploadUseTheCorrectEndpoints() = runTest {
        val request = ActualizarPerfilClienteRequest("Ana Luna", "8091234567", "ana@test.com")
        val updated = ClienteDto(clienteId = 3, nombre = request.nombre)
        coEvery { api.actualizarMiPerfil(request) } returns Response.success(updated)
        coEvery { api.subirMiFoto(any<MultipartBody.Part>()) } returns Response.success(Unit)

        assertEquals(ApiResult.Success(updated), repository.actualizarPerfil(request))
        assertEquals(ApiResult.Success(Unit), repository.subirFoto(byteArrayOf(1, 2, 3)))
        coVerify(exactly = 1) { api.actualizarMiPerfil(request) }
        coVerify(exactly = 1) { api.subirMiFoto(any<MultipartBody.Part>()) }
    }

    @Test
    fun favoritesAndReviewsAreDelegatedWithTheRequestedIds() = runTest {
        val favorite = BarberoDto(barberoId = 8, nombre = "Luz")
        val review = ResenaDto(resenaId = 2, citaId = 4, barberoId = 8, puntaje = 5)
        val request = CrearResenaRequest(citaId = 4, puntaje = 5, comentario = "Excelente")
        coEvery { api.getFavoritos() } returns Response.success(listOf(favorite))
        coEvery { api.agregarFavorito(8) } returns Response.success(Unit)
        coEvery { api.quitarFavorito(8) } returns Response.success(Unit)
        coEvery { api.crearResena(request) } returns Response.success(review)

        assertEquals(ApiResult.Success(listOf(favorite)), repository.listarFavoritos())
        assertEquals(ApiResult.Success(Unit), repository.agregarFavorito(8))
        assertEquals(ApiResult.Success(Unit), repository.quitarFavorito(8))
        assertEquals(ApiResult.Success(review), repository.crearResena(request))
    }

    @Test
    fun accountDeletionAcceptsAnEmptySuccessResponse() = runTest {
        coEvery { api.eliminarMiCuenta() } returns Response.success(Unit)

        assertEquals(ApiResult.Success(Unit), repository.eliminarMiCuenta())
        coVerify(exactly = 1) { api.eliminarMiCuenta() }
    }
}
