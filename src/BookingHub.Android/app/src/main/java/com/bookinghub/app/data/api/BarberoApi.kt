package com.bookinghub.app.data.api

import com.bookinghub.app.data.model.BarberoDto
import com.bookinghub.app.data.model.DisponibilidadDto
import retrofit2.Response
import retrofit2.http.GET
import retrofit2.http.Path
import retrofit2.http.Query

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
}
