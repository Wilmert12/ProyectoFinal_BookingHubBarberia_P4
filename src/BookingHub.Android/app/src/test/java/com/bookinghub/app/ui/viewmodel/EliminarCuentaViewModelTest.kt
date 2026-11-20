package com.bookinghub.app.ui.viewmodel

import com.bookinghub.app.data.repository.ApiResult
import com.bookinghub.app.data.repository.ClienteRepository
import com.bookinghub.app.testing.MainDispatcherRule
import io.mockk.coEvery
import io.mockk.mockk
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.runCurrent
import kotlinx.coroutines.test.runTest
import org.junit.Assert.assertEquals
import org.junit.Rule
import org.junit.Test

@OptIn(ExperimentalCoroutinesApi::class)
class EliminarCuentaViewModelTest {
    @get:Rule val mainDispatcherRule = MainDispatcherRule()
    private val repository = mockk<ClienteRepository>()

    @Test
    fun successfulDeletionSetsTerminalState() = runTest {
        coEvery { repository.eliminarMiCuenta() } returns ApiResult.Success(Unit)
        val viewModel = EliminarCuentaViewModel(repository)

        viewModel.eliminar()
        runCurrent()

        assertEquals(true, viewModel.uiState.value.eliminada)
        assertEquals(false, viewModel.uiState.value.cargando)
    }

    @Test
    fun deletionFailurePreservesAccountAndShowsReason() = runTest {
        coEvery { repository.eliminarMiCuenta() } returns ApiResult.HttpError(409, "Hay una cita activa.")
        val viewModel = EliminarCuentaViewModel(repository)

        viewModel.eliminar()
        runCurrent()

        assertEquals("Hay una cita activa.", viewModel.uiState.value.error)
        assertEquals(false, viewModel.uiState.value.eliminada)
    }
}
