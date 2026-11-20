package com.bookinghub.app.data.repository

import com.bookinghub.app.data.api.ApiClient
import com.bookinghub.app.data.model.BarberoDto
import com.bookinghub.app.data.model.DisponibilidadDto
import com.bookinghub.app.data.model.ResenaDto
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class BarberoRepository @Inject constructor(private val apiClient: ApiClient) {
    suspend fun obtenerFoto(barberoId: Int): ByteArray? = runCatching {
        val response = apiClient.barberoApi.getFoto(barberoId)
        if (response.isSuccessful) response.body()?.use { it.bytes() } else null
    }.getOrNull()

    suspend fun listar(): ApiResult<List<BarberoDto>> =
        executeApiRequest { apiClient.barberoApi.getBarberos() }

    suspend fun consultarDisponibilidad(
        barberoId: Int,
        fecha: String,
        duracionMin: Int
    ): ApiResult<DisponibilidadDto> = executeApiRequest {
        apiClient.barberoApi.getDisponibilidad(barberoId, fecha, duracionMin)
    }

    suspend fun listarResenas(barberoId: Int): ApiResult<List<ResenaDto>> =
        executeApiRequest { apiClient.barberoApi.getResenas(barberoId) }

    suspend fun listarBloqueos(fecha: String): ApiResult<List<com.bookinghub.app.data.model.BloqueoAgendaDto>> =
        executeApiRequest { apiClient.barberoApi.getMisBloqueos(fecha, fecha) }

    suspend fun crearBloqueo(request: com.bookinghub.app.data.model.CrearBloqueoAgendaRequest): ApiResult<com.bookinghub.app.data.model.BloqueoAgendaDto> =
        executeApiRequest { apiClient.barberoApi.crearBloqueo(request) }

    suspend fun eliminarBloqueo(id: Int): ApiResult<Unit> =
        executeApiRequestNoContent { apiClient.barberoApi.eliminarBloqueo(id) }
}
