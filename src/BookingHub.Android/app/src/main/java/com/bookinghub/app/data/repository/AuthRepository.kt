package com.bookinghub.app.data.repository

import com.bookinghub.app.data.api.ApiClient
import com.bookinghub.app.data.model.LoginDto
import com.bookinghub.app.data.model.RegistroClienteRequest
import com.bookinghub.app.data.model.SolicitarRestablecimientoRequest
import com.bookinghub.app.data.model.RestablecerContrasenaRequest
import com.bookinghub.app.data.model.CambiarContrasenaRequest
import com.bookinghub.app.data.repository.executeApiRequestNoContent
import com.bookinghub.app.data.model.SesionDto
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class AuthRepository @Inject constructor(private val apiClient: ApiClient) {
    suspend fun iniciarSesion(credenciales: LoginDto): ApiResult<SesionDto> =
        executeApiRequest { apiClient.authApi.iniciarSesion(credenciales) }

    suspend fun registrarCliente(solicitud: RegistroClienteRequest): ApiResult<SesionDto> =
        executeApiRequest { apiClient.authApi.registrarCliente(solicitud) }

    suspend fun solicitarRestablecimiento(solicitud: SolicitarRestablecimientoRequest): ApiResult<Map<String, String>> =
        executeApiRequest { apiClient.authApi.solicitarRestablecimiento(solicitud) }

    suspend fun restablecerContrasena(solicitud: RestablecerContrasenaRequest): ApiResult<Unit> =
        executeApiRequestNoContent { apiClient.authApi.restablecerContrasena(solicitud) }

    suspend fun cambiarContrasena(solicitud: CambiarContrasenaRequest): ApiResult<Unit> =
        executeApiRequestNoContent { apiClient.authApi.cambiarContrasena(solicitud) }
}
