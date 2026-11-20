package com.bookinghub.app.ui.viewmodel

import com.bookinghub.app.data.model.CambiarContrasenaRequest
import com.bookinghub.app.data.repository.ApiResult
import com.bookinghub.app.data.repository.AuthRepository
import com.bookinghub.app.testing.MainDispatcherRule
import io.mockk.coEvery
import io.mockk.coVerify
import io.mockk.mockk
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.runCurrent
import kotlinx.coroutines.test.runTest
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Rule
import org.junit.Test

@OptIn(ExperimentalCoroutinesApi::class)
class CambiarContrasenaViewModelTest {
    @get:Rule val mainDispatcherRule = MainDispatcherRule()
    private val repository = mockk<AuthRepository>()

    @Test
    fun mismatchedPasswordIsRejectedBeforeCallingTheApi() = runTest {
        val viewModel = CambiarContrasenaViewModel(repository)

        viewModel.cambiar("Actual1", "NuevaClave1", "OtraClave2")

        assertEquals("Verifica tu contraseña actual y usa una nueva con 8 caracteres, mayúscula, minúscula y número.", viewModel.uiState.value.error)
        coVerify(exactly = 0) { repository.cambiarContrasena(any()) }
    }

    @Test
    fun validPasswordReportsSuccess() = runTest {
        val request = CambiarContrasenaRequest("Actual1", "NuevaClave1")
        coEvery { repository.cambiarContrasena(request) } returns ApiResult.Success(Unit)
        val viewModel = CambiarContrasenaViewModel(repository)

        viewModel.cambiar(request.contrasenaActual, request.nuevaContrasena, request.nuevaContrasena)
        runCurrent()

        assertEquals(true, viewModel.uiState.value.cambiado)
        assertFalse(viewModel.uiState.value.cargando)
        coVerify(exactly = 1) { repository.cambiarContrasena(request) }
    }

    @Test
    fun serverErrorIsShownToTheCustomer() = runTest {
        coEvery { repository.cambiarContrasena(any()) } returns ApiResult.HttpError(400, "La contraseña actual no coincide.")
        val viewModel = CambiarContrasenaViewModel(repository)

        viewModel.cambiar("Actual1", "NuevaClave1", "NuevaClave1")
        runCurrent()

        assertEquals("La contraseña actual no coincide.", viewModel.uiState.value.error)
    }
}
