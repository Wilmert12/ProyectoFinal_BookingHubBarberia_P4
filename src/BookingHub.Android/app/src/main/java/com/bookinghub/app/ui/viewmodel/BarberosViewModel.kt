package com.bookinghub.app.ui.viewmodel

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.bookinghub.app.data.api.ApiClient
import com.bookinghub.app.data.model.BarberoDto
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch

data class BarberosUiState(
    val barberos: List<BarberoDto> = emptyList(),
    val cargando: Boolean = false,
    val error: String? = null
)

class BarberosViewModel : ViewModel() {
    private val _uiState = MutableStateFlow(BarberosUiState())
    val uiState: StateFlow<BarberosUiState> = _uiState

    init { cargarBarberos() }

    fun cargarBarberos() {
        viewModelScope.launch {
            _uiState.value = BarberosUiState(cargando = true)
            try {
                val response = ApiClient.barberoApi.getBarberos()
                if (response.isSuccessful) {
                    _uiState.value = BarberosUiState(barberos = response.body() ?: emptyList())
                } else {
                    _uiState.value = BarberosUiState(error = "Error al cargar: ${response.message()}")
                }
            } catch (e: Exception) {
                _uiState.value = BarberosUiState(error = "Error de conexión: ${e.localizedMessage}")
            }
        }
    }
}
