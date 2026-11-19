package com.bookinghub.app.data.api

import com.bookinghub.app.data.model.LoginDto
import com.bookinghub.app.data.model.SesionDto
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.POST

/** Operaciones de autenticación existentes en la API de BookingHub. */
interface AuthApi {
    @POST("api/Auth/login")
    suspend fun iniciarSesion(@Body credenciales: LoginDto): Response<SesionDto>
}
