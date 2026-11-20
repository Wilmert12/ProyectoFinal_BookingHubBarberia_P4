package com.bookinghub.app.data.api

import com.bookinghub.app.data.model.*
import com.bookinghub.app.data.model.SesionDto
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.POST

/** Operaciones de autenticación existentes en la API de BookingHub. */
interface AuthApi {
    @POST("api/Auth/login")
    suspend fun iniciarSesion(@Body credenciales: LoginDto): Response<SesionDto>

    @POST("api/Auth/registro-cliente")
    suspend fun registrarCliente(@Body solicitud: RegistroClienteRequest): Response<SesionDto>

    @POST("api/Auth/refresh")
    suspend fun renovarSesion(@Body solicitud: RefreshTokenRequest): Response<SesionDto>

    @POST("api/Auth/olvide-contrasena")
    suspend fun solicitarRestablecimiento(@Body solicitud: SolicitarRestablecimientoRequest): Response<Map<String, String>>

    @POST("api/Auth/restablecer-contrasena")
    suspend fun restablecerContrasena(@Body solicitud: RestablecerContrasenaRequest): Response<Unit>

    @POST("api/Auth/cambiar-contrasena")
    suspend fun cambiarContrasena(@Body solicitud: CambiarContrasenaRequest): Response<Unit>
}
