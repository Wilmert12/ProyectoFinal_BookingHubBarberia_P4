package com.bookinghub.app.data.repository

import com.bookinghub.app.data.api.ApiClient
import com.bookinghub.app.data.api.InformacionBarberiaApi
import com.bookinghub.app.data.model.InformacionBarberiaDto
import io.mockk.coEvery
import io.mockk.every
import io.mockk.mockk
import kotlinx.coroutines.test.runTest
import org.junit.Assert.assertEquals
import org.junit.Test
import retrofit2.Response

class InformacionBarberiaRepositoryTest {
    private val apiClient = mockk<ApiClient>()
    private val api = mockk<InformacionBarberiaApi>()
    private val repository = InformacionBarberiaRepository(apiClient)

    init { every { apiClient.informacionBarberiaApi } returns api }

    @Test
    fun returnsTheConfiguredBarbershopDetails() = runTest {
        val info = InformacionBarberiaDto(nombre = "Cerquillo", direccion = "Santo Domingo")
        coEvery { api.getInformacion() } returns Response.success(info)

        assertEquals(ApiResult.Success(info), repository.obtener())
    }
}
