package com.bookinghub.app.data.repository

import com.bookinghub.app.data.api.ApiClient
import com.bookinghub.app.data.model.ClienteDto
import com.bookinghub.app.data.model.ActualizarPerfilClienteRequest
import com.bookinghub.app.data.model.BarberoDto
import com.bookinghub.app.data.model.CrearResenaRequest
import com.bookinghub.app.data.model.ResenaDto
import javax.inject.Inject
import javax.inject.Singleton
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.MultipartBody
import okhttp3.RequestBody.Companion.toRequestBody

@Singleton
class ClienteRepository @Inject constructor(private val apiClient: ApiClient) {
    suspend fun crearCliente(cliente: ClienteDto): ApiResult<ClienteDto> =
        executeApiRequest { apiClient.clienteApi.crearCliente(cliente) }

    suspend fun eliminarMiCuenta(): ApiResult<Unit> =
        executeApiRequestNoContent { apiClient.clienteApi.eliminarMiCuenta() }

    suspend fun obtenerPerfil(): ApiResult<ClienteDto> =
        executeApiRequest { apiClient.clienteApi.getMiPerfil() }

    suspend fun actualizarPerfil(request: ActualizarPerfilClienteRequest): ApiResult<ClienteDto> =
        executeApiRequest { apiClient.clienteApi.actualizarMiPerfil(request) }

    suspend fun subirFoto(bytesJpeg: ByteArray): ApiResult<Unit> {
        val body = bytesJpeg.toRequestBody("image/jpeg".toMediaType())
        val part = MultipartBody.Part.createFormData("foto", "perfil.jpg", body)
        return executeApiRequestNoContent { apiClient.clienteApi.subirMiFoto(part) }
    }

    suspend fun listarFavoritos(): ApiResult<List<BarberoDto>> =
        executeApiRequest { apiClient.clienteApi.getFavoritos() }

    suspend fun agregarFavorito(id: Int): ApiResult<Unit> =
        executeApiRequestNoContent { apiClient.clienteApi.agregarFavorito(id) }

    suspend fun quitarFavorito(id: Int): ApiResult<Unit> =
        executeApiRequestNoContent { apiClient.clienteApi.quitarFavorito(id) }

    suspend fun crearResena(request: CrearResenaRequest): ApiResult<ResenaDto> =
        executeApiRequest { apiClient.clienteApi.crearResena(request) }
}
