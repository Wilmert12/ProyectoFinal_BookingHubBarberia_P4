package com.bookinghub.app.data.api

import com.bookinghub.app.data.model.ClienteDto
import com.bookinghub.app.data.model.ActualizarPerfilClienteRequest
import com.bookinghub.app.data.model.BarberoDto
import com.bookinghub.app.data.model.CrearResenaRequest
import com.bookinghub.app.data.model.ResenaDto
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path
import retrofit2.http.Multipart
import retrofit2.http.Part
import okhttp3.MultipartBody
import retrofit2.http.DELETE
import retrofit2.http.GET

interface ClienteApi {
    @POST("api/Clientes")
    suspend fun crearCliente(@Body cliente: ClienteDto): Response<ClienteDto>

    @DELETE("api/Clientes/me")
    suspend fun eliminarMiCuenta(): Response<Unit>

    @GET("api/Clientes/me")
    suspend fun getMiPerfil(): Response<ClienteDto>

    @PUT("api/Clientes/me")
    suspend fun actualizarMiPerfil(@Body request: ActualizarPerfilClienteRequest): Response<ClienteDto>

    @Multipart
    @POST("api/Clientes/me/foto")
    suspend fun subirMiFoto(@Part foto: MultipartBody.Part): Response<Unit>

    @GET("api/Clientes/me/favoritos")
    suspend fun getFavoritos(): Response<List<BarberoDto>>

    @POST("api/Clientes/me/favoritos/{barberoId}")
    suspend fun agregarFavorito(@Path("barberoId") barberoId: Int): Response<Unit>

    @DELETE("api/Clientes/me/favoritos/{barberoId}")
    suspend fun quitarFavorito(@Path("barberoId") barberoId: Int): Response<Unit>

    @POST("api/Resenas")
    suspend fun crearResena(@Body request: CrearResenaRequest): Response<ResenaDto>
}
