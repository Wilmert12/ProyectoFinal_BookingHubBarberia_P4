package com.bookinghub.app.ui.viewmodel

import android.content.Context
import com.bookinghub.app.data.model.CatalogoItemDto
import com.bookinghub.app.data.repository.ApiResult
import com.bookinghub.app.data.repository.CatalogoRepository
import com.bookinghub.app.testing.MainDispatcherRule
import com.bookinghub.app.util.CitasCache
import io.mockk.coEvery
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
import java.math.BigDecimal

@OptIn(ExperimentalCoroutinesApi::class)
class CatalogoViewModelTest {
    @get:Rule val mainDispatcherRule = MainDispatcherRule()
    private val repository = mockk<CatalogoRepository>()
    private val context = mockk<Context>(relaxed = true)

    @After fun tearDown() = unmockkAll()

    @Test
    fun successfulRefreshStoresAndDisplaysApiItems() = runTest {
        prepareCache(null)
        val items = listOf(item(1, "Corte"), item(2, "Barba"))
        coEvery { repository.listar(null, null, null) } returns ApiResult.Success(items)
        val viewModel = CatalogoViewModel(repository, context)

        runCurrent()

        assertEquals(items, viewModel.uiState.value.items)
        io.mockk.verify(exactly = 1) { CitasCache.guardarCatalogo(context, items) }
    }

    @Test
    fun networkFailureUsesFilteredAndSortedCachedItems() = runTest {
        val cached = listOf(item(1, "Corte", true), item(2, "Cera", false), item(3, "Barba", true))
        prepareCache(cached)
        coEvery { repository.listar(null, null, null) } returns ApiResult.Success(emptyList())
        coEvery { repository.listar("r", true, "nombre") } returns ApiResult.NetworkError
        val viewModel = CatalogoViewModel(repository, context)

        viewModel.cargarCatalogo(q = "r", esServicio = true, ordenar = "nombre")
        runCurrent()

        assertEquals(listOf("Barba", "Corte"), viewModel.uiState.value.items.map { it.nombre })
        assertEquals("Sin conexión. Se muestra el último catálogo guardado.", viewModel.uiState.value.aviso)
    }

    @Test
    fun apiFailureWithoutCacheShowsErrorState() = runTest {
        prepareCache(null)
        coEvery { repository.listar(null, null, null) } returns ApiResult.HttpError(503, "API ocupada")
        val viewModel = CatalogoViewModel(repository, context)

        runCurrent()

        assertEquals("API ocupada", viewModel.uiState.value.error)
    }

    private fun prepareCache(items: List<CatalogoItemDto>?) {
        mockkObject(CitasCache)
        every { CitasCache.leerCatalogo(context) } returns items
        every { CitasCache.guardarCatalogo(context, any()) } just Runs
    }

    private fun item(id: Int, name: String, service: Boolean = true) = CatalogoItemDto(
        catalogoItemId = id,
        nombre = name,
        esServicio = service,
        precio = BigDecimal("100")
    )
}
