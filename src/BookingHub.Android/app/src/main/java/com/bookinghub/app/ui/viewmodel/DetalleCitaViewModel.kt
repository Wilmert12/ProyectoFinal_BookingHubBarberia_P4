package com.bookinghub.app.ui.viewmodel

import androidx.lifecycle.SavedStateHandle
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.bookinghub.app.data.model.CitaDto
import com.bookinghub.app.data.repository.ApiResult
import com.bookinghub.app.data.repository.CitaRepository
import com.bookinghub.app.data.repository.mensajeParaUsuario
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

data class DetalleCitaUiState(val cita: CitaDto? = null, val cargando: Boolean = true, val error: String? = null)

@HiltViewModel
class DetalleCitaViewModel @Inject constructor(
    savedStateHandle: SavedStateHandle,
    citaRepository: CitaRepository
) : ViewModel() {
    private val _uiState = MutableStateFlow(DetalleCitaUiState())
    val uiState: StateFlow<DetalleCitaUiState> = _uiState

    init {
        val id = checkNotNull(savedStateHandle.get<Int>("citaId"))
        viewModelScope.launch {
            when (val result = citaRepository.buscar(id)) {
                is ApiResult.Success -> _uiState.value = DetalleCitaUiState(result.data, false)
                else -> _uiState.value = DetalleCitaUiState(error = result.mensajeParaUsuario("No se pudo abrir esta cita."), cargando = false)
            }
        }
    }
}
