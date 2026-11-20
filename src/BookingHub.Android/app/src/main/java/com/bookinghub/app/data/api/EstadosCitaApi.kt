package com.bookinghub.app.data.api

import com.bookinghub.app.data.model.EstadoCitaDto
import retrofit2.Response
import retrofit2.http.GET

interface EstadosCitaApi {
    @GET("api/Catalogos/EstadosCita")
    suspend fun getEstadosCita(): Response<List<EstadoCitaDto>>
}
