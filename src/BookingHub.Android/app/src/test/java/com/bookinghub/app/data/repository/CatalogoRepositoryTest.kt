package com.bookinghub.app.data.repository

import com.bookinghub.app.data.api.ApiClient
import com.bookinghub.app.data.api.CatalogoItemApi
import com.bookinghub.app.data.model.CatalogoItemDto
import io.mockk.coEvery
import io.mockk.coVerify
import io.mockk.every
import io.mockk.mockk
import kotlinx.coroutines.test.runTest
import org.junit.Assert.assertEquals
import org.junit.Test
import retrofit2.Response

class CatalogoRepositoryTest {
    private val apiClient = mockk<ApiClient>()
    private val api = mockk<CatalogoItemApi>()
    private val repository = CatalogoRepository(apiClient)

    init { every { apiClient.catalogoItemApi } returns api }

    @Test
    fun listForwardsSearchTypeAndOrdering() = runTest {
        val item = CatalogoItemDto(catalogoItemId = 2, nombre = "Corte")
        coEvery { api.getCatalogoItems("corte", true, "precio") } returns Response.success(listOf(item))

        assertEquals(ApiResult.Success(listOf(item)), repository.listar("corte", true, "precio"))
        coVerify(exactly = 1) { api.getCatalogoItems("corte", true, "precio") }
    }
}
