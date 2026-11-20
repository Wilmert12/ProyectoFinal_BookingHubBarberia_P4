package com.bookinghub.app.data.api

import com.bookinghub.app.data.model.InformacionBarberiaDto
import retrofit2.Response
import retrofit2.http.GET

interface InformacionBarberiaApi {
    @GET("api/InformacionBarberia")
    suspend fun getInformacion(): Response<InformacionBarberiaDto>
}
