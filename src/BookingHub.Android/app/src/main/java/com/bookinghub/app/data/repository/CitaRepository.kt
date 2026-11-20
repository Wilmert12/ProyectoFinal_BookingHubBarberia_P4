package com.bookinghub.app.data.repository

import com.bookinghub.app.data.api.ApiClient
import com.bookinghub.app.data.model.CitaDto
import com.bookinghub.app.data.model.EstadoCitaDto
import com.bookinghub.app.data.model.PaginaCitasDto
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class CitaRepository @Inject constructor(private val apiClient: ApiClient) {
    suspend fun buscar(id: Int): ApiResult<CitaDto> =
        executeApiRequest { apiClient.citaApi.getCita(id) }

    suspend fun listarDelCliente(): ApiResult<List<CitaDto>> =
        executeApiRequest { apiClient.citaApi.getMisCitasCliente() }

    suspend fun listarHistorial(pagina: Int, tamano: Int = 10): ApiResult<PaginaCitasDto> =
        executeApiRequest { apiClient.citaApi.getHistorial(pagina, tamano) }

    suspend fun listarAgendaBarbero(fecha: String): ApiResult<List<CitaDto>> =
        executeApiRequest { apiClient.citaApi.getMisCitasBarbero(fecha) }

    suspend fun listarEstados(): ApiResult<List<EstadoCitaDto>> =
        executeApiRequest { apiClient.estadosCitaApi.getEstadosCita() }

    suspend fun crear(cita: CitaDto): ApiResult<CitaDto> =
        executeApiRequest { apiClient.citaApi.crearCita(cita) }

    suspend fun cancelar(id: Int, motivo: String? = null): ApiResult<CitaDto> =
        executeApiRequest { apiClient.citaApi.cancelarCita(id, com.bookinghub.app.data.api.CancelarCitaRequest(motivo)) }

    suspend fun reprogramar(id: Int, nuevaFechaInicio: String): ApiResult<CitaDto> =
        executeApiRequest { apiClient.citaApi.reprogramarCita(id, com.bookinghub.app.data.api.ReprogramarCitaRequest(nuevaFechaInicio)) }

    suspend fun cambiarEstado(id: Int, estadoCitaId: Int): ApiResult<CitaDto> =
        executeApiRequest { apiClient.citaApi.cambiarEstado(id, com.bookinghub.app.data.api.CambiarEstadoCitaRequest(estadoCitaId)) }
}
