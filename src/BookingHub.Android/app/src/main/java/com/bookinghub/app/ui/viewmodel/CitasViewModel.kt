package com.bookinghub.app.ui.viewmodel

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.bookinghub.app.data.api.ApiClient
import com.bookinghub.app.data.api.mensajeError
import com.bookinghub.app.data.model.CitaDto
import com.bookinghub.app.util.SesionCliente
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch

data class CitasUiState(
    val citas: List<CitaDto> = emptyList(),
    val cargando: Boolean = false,
    val error: String? = null
)

class CitasViewModel : ViewModel() {
    private val _uiState = MutableStateFlow(CitasUiState())
    val uiState: StateFlow<CitasUiState> = _uiState

    init { cargarCitas() }

    fun cargarCitas() {
        val clienteId = SesionCliente.clienteActual?.clienteId
        if (clienteId == null) {
            _uiState.value = CitasUiState(error = "No hay un perfil de cliente guardado en este dispositivo.")
            return
        }
        viewModelScope.launch {
            _uiState.value = CitasUiState(cargando = true)
            try {
                val response = ApiClient.citaApi.getCitas(clienteId = clienteId)
                if (response.isSuccessful) {
                    _uiState.value = CitasUiState(citas = response.body() ?: emptyList())
                } else {
                    _uiState.value = CitasUiState(error = response.mensajeError("No se pudieron cargar tus citas"))
                }
            } catch (cancelacion: CancellationException) {
                throw cancelacion
            } catch (e: Exception) {
                _uiState.value = CitasUiState(error = e.localizedMessage ?: "Revisa tu conexión e inténtalo de nuevo.")
            }
        }
    }
}
