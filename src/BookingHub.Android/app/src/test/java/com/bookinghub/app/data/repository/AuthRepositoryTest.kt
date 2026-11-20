package com.bookinghub.app.data.repository

import com.bookinghub.app.data.api.ApiClient
import com.bookinghub.app.data.api.AuthApi
import com.bookinghub.app.data.model.LoginDto
import com.bookinghub.app.data.model.RegistroClienteRequest
import com.bookinghub.app.data.model.SesionDto
import com.bookinghub.app.data.model.SolicitarRestablecimientoRequest
import com.bookinghub.app.data.model.RestablecerContrasenaRequest
import com.bookinghub.app.data.model.CambiarContrasenaRequest
import io.mockk.coEvery
import io.mockk.every
import io.mockk.mockk
import io.mockk.coVerify
import kotlinx.coroutines.test.runTest
import org.junit.Assert.assertEquals
import org.junit.Test
import retrofit2.Response
import okhttp3.ResponseBody.Companion.toResponseBody

class AuthRepositoryTest {
    private val apiClient = mockk<ApiClient>()
    private val authApi = mockk<AuthApi>()
    private val repository = AuthRepository(apiClient)

    init {
        every { apiClient.authApi } returns authApi
    }

    @Test
    fun login_returnsSessionAndSendsCredentials() = runTest {
        val credentials = LoginDto("cliente@example.com", "Password1")
        val session = SesionDto("jwt", "2099-01-01T00:00:00Z", nombre = "Ana", email = credentials.email, rol = "Cliente")
        coEvery { authApi.iniciarSesion(credentials) } returns Response.success(session)

        assertEquals(ApiResult.Success(session), repository.iniciarSesion(credentials))
        coVerify(exactly = 1) { authApi.iniciarSesion(credentials) }
    }

    @Test
    fun login_returnsSessionExpiredFor401() = runTest {
        coEvery { authApi.iniciarSesion(any()) } returns Response.error(401, "".toResponseBody())

        assertEquals(ApiResult.SessionExpired, repository.iniciarSesion(LoginDto("a@b.com", "Password1")))
    }

    @Test
    fun registerReturnsTheCreatedSession() = runTest {
        val request = RegistroClienteRequest("Ana Luna", "8091234567", "ana@example.com", "Password1")
        val response = SesionDto("jwt", "2099-01-01T00:00:00Z", nombre = "Ana", email = request.email, rol = "Cliente")
        coEvery { authApi.registrarCliente(request) } returns Response.success(response)

        assertEquals(ApiResult.Success(response), repository.registrarCliente(request))
        coVerify(exactly = 1) { authApi.registrarCliente(request) }
    }

    @Test
    fun passwordRecoveryReturnsTheApiMessage() = runTest {
        val request = SolicitarRestablecimientoRequest("ana@example.com", "8091234567")
        val response = mapOf("mensaje" to "Si los datos coinciden, recibirás un código.")
        coEvery { authApi.solicitarRestablecimiento(request) } returns Response.success(response)

        assertEquals(ApiResult.Success(response), repository.solicitarRestablecimiento(request))
    }

    @Test
    fun passwordChangeAcceptsSuccessfulEmptyResponse() = runTest {
        val request = CambiarContrasenaRequest("Anterior1", "NuevaClave1")
        coEvery { authApi.cambiarContrasena(request) } returns Response.success(Unit)

        assertEquals(ApiResult.Success(Unit), repository.cambiarContrasena(request))
        coVerify(exactly = 1) { authApi.cambiarContrasena(request) }
    }

    @Test
    fun resetPasswordUsesTheRequestedCodeAndPassword() = runTest {
        val request = RestablecerContrasenaRequest("token", "NuevaClave1")
        coEvery { authApi.restablecerContrasena(request) } returns Response.success(Unit)

        assertEquals(ApiResult.Success(Unit), repository.restablecerContrasena(request))
        coVerify(exactly = 1) { authApi.restablecerContrasena(request) }
    }
}
