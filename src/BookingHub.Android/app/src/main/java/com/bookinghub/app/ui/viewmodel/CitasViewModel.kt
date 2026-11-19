package com.bookinghub.app.ui.viewmodel

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.bookinghub.app.data.api.ApiClient
import com.bookinghub.app.data.api.mensajeError
import com.bookinghub.app.data.model.CitaDto
import com.bookinghub.app.util.CitasCache
import com.bookinghub.app.util.SesionCliente
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.Job
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch

data class CitasUiState(
    val citas: List<CitaDto> = emptyList(),
    val cargando: Boolean = false,
    val error: String? = null,
    val aviso: String? = null
)

class CitasViewModel(application: Application) : AndroidViewModel(application) {
    private val _uiState = MutableStateFlow(CitasUiState())
    val uiState: StateFlow<CitasUiState> = _uiState
    private var cargaActual: Job? = null

    init { cargarCitas() }

    fun cargarCitas() {
        val clienteId = SesionCliente.clienteActual?.clienteId
        if (clienteId == null) {
            _uiState.value = CitasUiState(error = "No hay un perfil de cliente guardado en este dispositivo.")
            return
        }

        val citasGuardadas = CitasCache.leerCliente(getApplication(), clienteId)
        cargaActual?.cancel()
        _uiState.value = CitasUiState(citas = citasGuardadas.orEmpty(), cargando = true)
        cargaActual = viewModelScope.launch {
            try {
                val response = ApiClient.citaApi.getCitas(clienteId = clienteId)
                if (response.isSuccessful) {
                    val citas = response.body().orEmpty()
                    CitasCache.guardarCliente(getApplication(), clienteId, citas)
                    _uiState.value = CitasUiState(citas = citas)
                } else {
                    val mensaje = response.mensajeError("No se pudieron cargar tus citas")
                    _uiState.value = if (citasGuardadas != null) {
                        CitasUiState(
                            citas = citasGuardadas,
                            aviso = "No se pudo actualizar. Se muestran las últimas citas guardadas."
                        )
                    } else {
                        CitasUiState(error = mensaje)
                    }
                }
            } catch (cancelacion: CancellationException) {
                throw cancelacion
            } catch (e: Exception) {
                _uiState.value = if (citasGuardadas != null) {
                    CitasUiState(
                        citas = citasGuardadas,
                        aviso = "Sin conexión. Se muestran las últimas citas guardadas."
                    )
                } else {
                    CitasUiState(error = e.localizedMessage ?: "Revisa tu conexión e inténtalo de nuevo.")
                }
            }
        }
    }
}
