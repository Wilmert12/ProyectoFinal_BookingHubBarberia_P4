package com.bookinghub.app.data.api

import com.bookinghub.app.data.model.ClienteDto
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.POST

interface ClienteApi {
    @POST("api/Clientes")
    suspend fun crearCliente(@Body cliente: ClienteDto): Response<ClienteDto>
}
