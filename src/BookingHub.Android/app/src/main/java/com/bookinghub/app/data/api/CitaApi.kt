package com.bookinghub.app.data.api

import com.bookinghub.app.data.model.CambioEstadoDto
import com.bookinghub.app.data.model.CitaDto
import retrofit2.Response
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Body
import retrofit2.http.Path
import retrofit2.http.Query

interface CitaApi {
    @GET("api/Citas")
    suspend fun getCitas(
        @Query("clienteId") clienteId: Int? = null,
        @Query("barberoId") barberoId: Int? = null,
        @Query("fecha") fecha: String? = null
    ): Response<List<CitaDto>>

    /** Agenda del barbero desde [fecha] por [dias] días (1 = un día, 7 = la semana). */
    @GET("api/Citas/mias")
    suspend fun getMisCitasBarbero(
        @Query("fecha") fecha: String,
        @Query("dias") dias: Int = 1
    ): Response<List<CitaDto>>

    @POST("api/Citas")
    suspend fun crearCita(@Body cita: CitaDto): Response<CitaDto>

    /** El cliente cancela una cita suya que todavía no empieza (el teléfono va en el encabezado). */
    @POST("api/Citas/{id}/cancelar")
    suspend fun cancelarCita(@Path("id") citaId: Int): Response<Unit>

    /** El barbero con sesión marca una de sus citas. */
    @PUT("api/Citas/{id}/estado")
    suspend fun cambiarEstado(@Path("id") citaId: Int, @Body cambio: CambioEstadoDto): Response<Unit>
}
