package com.bookinghub.app.ui.viewmodel

import androidx.lifecycle.SavedStateHandle
import com.bookinghub.app.data.model.CitaDto
import com.bookinghub.app.data.repository.ApiResult
import com.bookinghub.app.data.repository.CitaRepository
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
class DetalleCitaViewModelTest {
    @get:Rule val mainDispatcherRule = MainDispatcherRule()
    private val repository = mockk<CitaRepository>()

    @Test
    fun loadsAppointmentFromNavigationId() = runTest {
        val appointment = CitaDto(citaId = 8, estado = "Pendiente")
        coEvery { repository.buscar(8) } returns ApiResult.Success(appointment)

        val viewModel = DetalleCitaViewModel(SavedStateHandle(mapOf("citaId" to 8)), repository)
        runCurrent()

        assertEquals(appointment, viewModel.uiState.value.cita)
        assertEquals(false, viewModel.uiState.value.cargando)
    }

    @Test
    fun missingNavigationIdFailsFast() {
        org.junit.Assert.assertThrows(IllegalStateException::class.java) {
            DetalleCitaViewModel(SavedStateHandle(), repository)
        }
    }
}
