package com.bookinghub.app.data.api

import com.bookinghub.app.data.model.ClienteDto
import com.bookinghub.app.data.model.NumeroClienteDto
import com.bookinghub.app.data.model.RecuperarNumeroDto
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.POST

interface ClienteApi {
    @POST("api/Clientes")
    suspend fun crearCliente(@Body cliente: ClienteDto): Response<ClienteDto>

    /** Con el nombre y el teléfono del registro devuelve el número de cliente. */
    @POST("api/Clientes/recuperar")
    suspend fun recuperarNumero(@Body datos: RecuperarNumeroDto): Response<NumeroClienteDto>
}
