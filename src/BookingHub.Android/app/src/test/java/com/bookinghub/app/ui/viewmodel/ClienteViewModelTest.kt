package com.bookinghub.app.ui.viewmodel

import android.content.Context
import com.bookinghub.app.data.repository.AuthRepository
import com.bookinghub.app.testing.MainDispatcherRule
import com.bookinghub.app.util.SesionCliente
import io.mockk.coVerify
import io.mockk.mockk
import kotlinx.coroutines.test.runTest
import org.junit.Assert.assertEquals
import org.junit.Rule
import org.junit.Test
import kotlinx.coroutines.ExperimentalCoroutinesApi

@OptIn(ExperimentalCoroutinesApi::class)
class ClienteViewModelTest {
    @get:Rule
    val mainDispatcherRule = MainDispatcherRule()

    private val authRepository = mockk<AuthRepository>()
    private val context = mockk<Context>(relaxed = true)

    @Test
    fun registration_rejectsShortNameWithoutCallingApi() = runTest {
        val viewModel = ClienteViewModel(authRepository, SesionCliente, context)

        viewModel.registrarCliente("A", "8091234567", "ana@example.com", "Password1")

        assertEquals("Ingresa un nombre de al menos 2 caracteres.", viewModel.uiState.value.error)
        coVerify(exactly = 0) { authRepository.registrarCliente(any()) }
    }

    @Test
    fun registration_rejectsNonDominicanNumber() = runTest {
        val viewModel = ClienteViewModel(authRepository, SesionCliente, context)

        viewModel.registrarCliente("Ana Luna", "5551234567", "ana@example.com", "Password1")

        assertEquals("Ingresa un teléfono dominicano con código 809, 829 o 849.", viewModel.uiState.value.error)
        coVerify(exactly = 0) { authRepository.registrarCliente(any()) }
    }

}
