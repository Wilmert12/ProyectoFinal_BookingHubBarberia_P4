package com.bookinghub.app.ui.viewmodel

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.bookinghub.app.data.api.ApiClient
import com.bookinghub.app.data.model.CatalogoItemDto
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch

data class CatalogoUiState(
    val items: List<CatalogoItemDto> = emptyList(),
    val cargando: Boolean = false,
    val error: String? = null
)

class CatalogoViewModel : ViewModel() {
    private val _uiState = MutableStateFlow(CatalogoUiState())
    val uiState: StateFlow<CatalogoUiState> = _uiState

    init { cargarCatalogo() }

    fun cargarCatalogo() {
        viewModelScope.launch {
            _uiState.value = CatalogoUiState(cargando = true)
            try {
                val response = ApiClient.catalogoItemApi.getCatalogoItems()
                if (response.isSuccessful) {
                    _uiState.value = CatalogoUiState(items = response.body() ?: emptyList())
                } else {
                    _uiState.value = CatalogoUiState(error = "Error al cargar: ${response.message()}")
                }
            } catch (e: Exception) {
                _uiState.value = CatalogoUiState(error = "Error de conexión: ${e.localizedMessage}")
            }
        }
    }
}
