package com.bookinghub.app.data.api

import com.bookinghub.app.data.model.CatalogoItemDto
import retrofit2.Response
import retrofit2.http.GET
import retrofit2.http.Path
import retrofit2.http.Query

interface CatalogoItemApi {
    @GET("api/CatalogoItems")
    suspend fun getCatalogoItems(
        @Query("q") q: String? = null,
        @Query("esServicio") esServicio: Boolean? = null,
        @Query("ordenar") ordenar: String? = null
    ): Response<List<CatalogoItemDto>>

    @GET("api/CatalogoItems/{id}")
    suspend fun getCatalogoItem(@Path("id") id: Int): Response<CatalogoItemDto>
}
