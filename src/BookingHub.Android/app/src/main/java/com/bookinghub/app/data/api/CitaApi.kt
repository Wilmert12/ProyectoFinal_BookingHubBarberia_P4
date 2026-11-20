package com.bookinghub.app.data.api

import com.bookinghub.app.data.model.CitaDto
import retrofit2.Response
import retrofit2.http.GET
import retrofit2.http.POST
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

    @GET("api/Citas/mias")
    suspend fun getMisCitasBarbero(@Query("fecha") fecha: String): Response<List<CitaDto>>

    @POST("api/Citas")
    suspend fun crearCita(@Body cita: CitaDto): Response<CitaDto>
}
