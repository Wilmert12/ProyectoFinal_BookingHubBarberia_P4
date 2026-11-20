package com.bookinghub.app.ui.viewmodel

import android.content.Context
import com.bookinghub.app.data.model.ActualizarPerfilClienteRequest
import com.bookinghub.app.data.model.ClienteDto
import com.bookinghub.app.data.repository.ApiResult
import com.bookinghub.app.data.repository.ClienteRepository
import com.bookinghub.app.testing.MainDispatcherRule
import com.bookinghub.app.util.SesionCliente
import io.mockk.coEvery
import io.mockk.coVerify
import io.mockk.every
import io.mockk.just
import io.mockk.mockk
import io.mockk.mockkObject
import io.mockk.Runs
import io.mockk.unmockkAll
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.runCurrent
import kotlinx.coroutines.test.runTest
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Rule
import org.junit.Test

@OptIn(ExperimentalCoroutinesApi::class)
class PerfilClienteViewModelTest {
    @get:Rule val mainDispatcherRule = MainDispatcherRule()
    private val repository = mockk<ClienteRepository>()
    private val context = mockk<Context>(relaxed = true)

    @After fun tearDown() = unmockkAll()

    @Test
    fun profileLoadUpdatesTheVisibleState() = runTest {
        val profile = ClienteDto(clienteId = 4, nombre = "Ana", telefono = "8091234567", email = "ana@test.com")
        coEvery { repository.obtenerPerfil() } returns ApiResult.Success(profile)
        val viewModel = PerfilClienteViewModel(repository, context)

        runCurrent()

        assertEquals(profile, viewModel.uiState.value.perfil)
        assertEquals(false, viewModel.uiState.value.cargando)
    }

    @Test
    fun savingProfileSendsTrimmedFieldsAndRefreshesSessionData() = runTest {
        mockkObject(SesionCliente)
        every { SesionCliente.actualizarDatos(context, "Ana Luna", "ana@test.com") } returns true
        coEvery { repository.obtenerPerfil() } returns ApiResult.Success(ClienteDto(clienteId = 4, nombre = "Ana"))
        val request = ActualizarPerfilClienteRequest("Ana Luna", "8091234567", "ana@test.com")
        val updated = ClienteDto(clienteId = 4, nombre = request.nombre, telefono = request.telefono, email = request.email)
        coEvery { repository.actualizarPerfil(request) } returns ApiResult.Success(updated)
        val viewModel = PerfilClienteViewModel(repository, context)
        runCurrent()

        viewModel.guardar(request.nombre, request.telefono, request.email.orEmpty())
        runCurrent()

        assertEquals(updated, viewModel.uiState.value.perfil)
        assertEquals("Perfil actualizado.", viewModel.uiState.value.mensaje)
        coVerify(exactly = 1) { repository.actualizarPerfil(request) }
        io.mockk.verify(exactly = 1) { SesionCliente.actualizarDatos(context, "Ana Luna", "ana@test.com") }
    }

    @Test
    fun photoUploadFailureClearsProgressAndShowsError() = runTest {
        coEvery { repository.obtenerPerfil() } returns ApiResult.Success(ClienteDto(clienteId = 4, nombre = "Ana"))
        coEvery { repository.subirFoto(any()) } returns ApiResult.NetworkError
        val viewModel = PerfilClienteViewModel(repository, context)
        runCurrent()

        viewModel.subirFoto(byteArrayOf(1, 2, 3))
        runCurrent()

        assertEquals("Sin conexión. Revisa la red e inténtalo de nuevo.", viewModel.uiState.value.error)
        assertEquals(false, viewModel.uiState.value.subiendoFoto)
    }
}
