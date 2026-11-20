package com.bookinghub.app.data.api

import com.bookinghub.app.data.model.BarberoDto
import com.bookinghub.app.data.model.DisponibilidadDto
import com.bookinghub.app.data.model.BloqueoAgendaDto
import com.bookinghub.app.data.model.CrearBloqueoAgendaRequest
import com.bookinghub.app.data.model.ResenaDto
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.DELETE
import retrofit2.http.GET
import retrofit2.http.Path
import retrofit2.http.Query
import retrofit2.http.POST
import okhttp3.ResponseBody
import retrofit2.http.Streaming

interface BarberoApi {
    @GET("api/Barberos")
    suspend fun getBarberos(): Response<List<BarberoDto>>

    @GET("api/Barberos/{id}")
    suspend fun getBarbero(@Path("id") id: Int): Response<BarberoDto>

    @GET("api/Barberos/{id}/disponibilidad")
    suspend fun getDisponibilidad(
        @Path("id") id: Int,
        @Query("fecha") fecha: String,
        @Query("duracionMin") duracionMin: Int
    ): Response<DisponibilidadDto>

    @GET("api/Barberos/{id}/resenas")
    suspend fun getResenas(@Path("id") id: Int): Response<List<ResenaDto>>

    @Streaming
    @GET("api/Barberos/{id}/foto")
    suspend fun getFoto(@Path("id") id: Int): Response<ResponseBody>

    @GET("api/Barberos/me/bloqueos")
    suspend fun getMisBloqueos(@Query("desde") desde: String, @Query("hasta") hasta: String): Response<List<BloqueoAgendaDto>>

    @POST("api/Barberos/me/bloqueos")
    suspend fun crearBloqueo(@Body request: CrearBloqueoAgendaRequest): Response<BloqueoAgendaDto>

    @DELETE("api/Barberos/me/bloqueos/{id}")
    suspend fun eliminarBloqueo(@Path("id") id: Int): Response<Unit>
}
