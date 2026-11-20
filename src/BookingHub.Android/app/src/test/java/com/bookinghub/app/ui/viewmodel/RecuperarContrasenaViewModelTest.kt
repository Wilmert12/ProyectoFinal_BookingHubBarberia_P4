package com.bookinghub.app.ui.viewmodel

import com.bookinghub.app.data.model.RestablecerContrasenaRequest
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
class RecuperarContrasenaViewModelTest {
    @get:Rule val mainDispatcherRule = MainDispatcherRule()
    private val repository = mockk<AuthRepository>()

    @Test
    fun invalidResetCodeAndPasswordDoNotCallApi() = runTest {
        val viewModel = RecuperarContrasenaViewModel(repository)

        viewModel.restablecer(" ", "debil")

        assertEquals("El código o la contraseña no son válidos. Usa 8 caracteres, mayúscula, minúscula y número.", viewModel.uiState.value.error)
        coVerify(exactly = 0) { repository.restablecerContrasena(any()) }
    }

    @Test
    fun validResetTrimsCodeAndPublishesConfirmation() = runTest {
        val request = RestablecerContrasenaRequest("codigo-123", "NuevaClave1")
        coEvery { repository.restablecerContrasena(request) } returns ApiResult.Success(Unit)
        val viewModel = RecuperarContrasenaViewModel(repository)

        viewModel.restablecer(" codigo-123 ", request.nuevaContrasena)
        runCurrent()

        assertEquals(true, viewModel.uiState.value.contrasenaCambiada)
        assertFalse(viewModel.uiState.value.cargando)
        coVerify(exactly = 1) { repository.restablecerContrasena(request) }
    }

    @Test
    fun resetApiFailureIsShown() = runTest {
        coEvery { repository.restablecerContrasena(any()) } returns ApiResult.HttpError(400, "Código vencido.")
        val viewModel = RecuperarContrasenaViewModel(repository)

        viewModel.restablecer("codigo", "NuevaClave1")
        runCurrent()

        assertEquals("Código vencido.", viewModel.uiState.value.error)
    }
}
