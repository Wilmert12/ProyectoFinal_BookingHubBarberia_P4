package com.bookinghub.app.ui.viewmodel

import com.bookinghub.app.data.model.InformacionBarberiaDto
import com.bookinghub.app.data.repository.ApiResult
import com.bookinghub.app.data.repository.InformacionBarberiaRepository
import com.bookinghub.app.testing.MainDispatcherRule
import io.mockk.coEvery
import io.mockk.mockk
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.runCurrent
import kotlinx.coroutines.test.runTest
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Rule
import org.junit.Test

@OptIn(ExperimentalCoroutinesApi::class)
class InformacionBarberiaViewModelTest {
    @get:Rule val mainDispatcherRule = MainDispatcherRule()
    private val repository = mockk<InformacionBarberiaRepository>()

    @Test
    fun loadsPublishedBarbershopDetails() = runTest {
        val info = InformacionBarberiaDto(nombre = "Cerquillo", direccion = "Santo Domingo", telefono = "8095551234")
        coEvery { repository.obtener() } returns ApiResult.Success(info)

        val viewModel = InformacionBarberiaViewModel(repository)
        runCurrent()

        assertEquals(info, viewModel.uiState.value.informacion)
        assertNull(viewModel.uiState.value.error)
        assertEquals(false, viewModel.uiState.value.cargando)
    }

    @Test
    fun displaysApiFailure() = runTest {
        coEvery { repository.obtener() } returns ApiResult.NetworkError

        val viewModel = InformacionBarberiaViewModel(repository)
        runCurrent()

        assertEquals("Sin conexión. Revisa la red e inténtalo de nuevo.", viewModel.uiState.value.error)
    }
}
