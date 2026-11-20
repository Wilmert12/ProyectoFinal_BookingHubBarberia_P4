package com.bookinghub.app.data.api

import com.bookinghub.app.data.model.CitaDto
import retrofit2.Response
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.Body
import retrofit2.http.Path
import retrofit2.http.Query
import retrofit2.http.PUT

interface CitaApi {
    @GET("api/Citas")
    suspend fun getCitas(
        @Query("clienteId") clienteId: Int? = null,
        @Query("barberoId") barberoId: Int? = null,
        @Query("fecha") fecha: String? = null
    ): Response<List<CitaDto>>

    @GET("api/Citas/mias")
    suspend fun getMisCitasBarbero(@Query("fecha") fecha: String? = null): Response<List<CitaDto>>

    @GET("api/Citas/mias")
    suspend fun getMisCitasCliente(@Query("proximas") proximas: Boolean = true): Response<List<CitaDto>>

    @GET("api/Citas/mias/historial")
    suspend fun getHistorial(@Query("pagina") pagina: Int, @Query("tamano") tamano: Int): Response<com.bookinghub.app.data.model.PaginaCitasDto>

    @GET("api/Citas/{id}")
    suspend fun getCita(@Path("id") id: Int): Response<CitaDto>

    @POST("api/Citas")
    suspend fun crearCita(@Body cita: CitaDto): Response<CitaDto>

    @PUT("api/Citas/{id}/cancelar")
    suspend fun cancelarCita(@Path("id") id: Int, @Body request: CancelarCitaRequest): Response<CitaDto>

    @PUT("api/Citas/{id}/reprogramar")
    suspend fun reprogramarCita(@Path("id") id: Int, @Body request: ReprogramarCitaRequest): Response<CitaDto>

    @PUT("api/Citas/{id}/estado")
    suspend fun cambiarEstado(@Path("id") id: Int, @Body request: CambiarEstadoCitaRequest): Response<CitaDto>
}

data class CancelarCitaRequest(val motivo: String? = null)
data class ReprogramarCitaRequest(val nuevaFechaInicio: String)
data class CambiarEstadoCitaRequest(val estadoCitaId: Int)
