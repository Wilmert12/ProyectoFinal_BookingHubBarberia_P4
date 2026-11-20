package com.bookinghub.app.ui.viewmodel

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.bookinghub.app.data.model.InformacionBarberiaDto
import com.bookinghub.app.data.repository.ApiResult
import com.bookinghub.app.data.repository.InformacionBarberiaRepository
import com.bookinghub.app.data.repository.mensajeParaUsuario
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

data class InformacionBarberiaUiState(
    val informacion: InformacionBarberiaDto? = null,
    val cargando: Boolean = false,
    val error: String? = null
)

@HiltViewModel
class InformacionBarberiaViewModel @Inject constructor(
    private val repository: InformacionBarberiaRepository
) : ViewModel() {
    private val _uiState = MutableStateFlow(InformacionBarberiaUiState(cargando = true))
    val uiState: StateFlow<InformacionBarberiaUiState> = _uiState

    init {
        viewModelScope.launch {
            when (val result = repository.obtener()) {
                is ApiResult.Success -> _uiState.value = InformacionBarberiaUiState(informacion = result.data)
                else -> _uiState.value = InformacionBarberiaUiState(error = result.mensajeParaUsuario("No se pudo cargar la información."))
            }
        }
    }
}
