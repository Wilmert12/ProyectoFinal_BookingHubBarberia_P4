package com.bookinghub.app.ui.viewmodel

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.bookinghub.app.data.repository.ApiResult
import com.bookinghub.app.data.repository.ClienteRepository
import com.bookinghub.app.data.repository.mensajeParaUsuario
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

data class EliminarCuentaUiState(val cargando: Boolean = false, val error: String? = null, val eliminada: Boolean = false)

@HiltViewModel
class EliminarCuentaViewModel @Inject constructor(
    private val clienteRepository: ClienteRepository
) : ViewModel() {
    private val _uiState = MutableStateFlow(EliminarCuentaUiState())
    val uiState: StateFlow<EliminarCuentaUiState> = _uiState

    fun eliminar() {
        viewModelScope.launch {
            _uiState.value = EliminarCuentaUiState(cargando = true)
            when (val result = clienteRepository.eliminarMiCuenta()) {
                is ApiResult.Success -> _uiState.value = EliminarCuentaUiState(eliminada = true)
                else -> _uiState.value = EliminarCuentaUiState(error = result.mensajeParaUsuario("No se pudo eliminar la cuenta."))
            }
        }
    }
}
