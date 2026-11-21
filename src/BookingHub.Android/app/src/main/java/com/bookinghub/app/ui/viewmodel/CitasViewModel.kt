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
import java.time.LocalDateTime

data class CitasUiState(
    val citas: List<CitaDto> = emptyList(),
    val cargando: Boolean = false,
    val error: String? = null,
    val aviso: String? = null,
    val cancelandoId: Int? = null,
    val mensaje: String? = null
)

/** Próximas primero (la más cercana arriba) y después el historial (la más reciente arriba). */
private fun ordenar(citas: List<CitaDto>): List<CitaDto> {
    val ahora = LocalDateTime.now().toString()
    val (proximas, pasadas) = citas.partition { it.fechaInicio >= ahora }
    return proximas.sortedBy { it.fechaInicio } + pasadas.sortedByDescending { it.fechaInicio }
}

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
        val mensaje = _uiState.value.mensaje
        _uiState.value = CitasUiState(citas = ordenar(citasGuardadas.orEmpty()), cargando = true, mensaje = mensaje)
        cargaActual = viewModelScope.launch {
            try {
                val response = ApiClient.citaApi.getCitas(clienteId = clienteId)
                if (response.isSuccessful) {
                    val citas = response.body().orEmpty()
                    CitasCache.guardarCliente(getApplication(), clienteId, citas)
                    _uiState.value = CitasUiState(citas = ordenar(citas), mensaje = mensaje)
                } else {
                    val textoError = response.mensajeError("No se pudieron cargar tus citas")
                    _uiState.value = if (citasGuardadas != null) {
                        CitasUiState(
                            citas = ordenar(citasGuardadas),
                            aviso = "No se pudo actualizar. Se muestran las últimas citas guardadas."
                        )
                    } else {
                        CitasUiState(error = textoError)
                    }
                }
            } catch (cancelacion: CancellationException) {
                throw cancelacion
            } catch (e: Exception) {
                _uiState.value = if (citasGuardadas != null) {
                    CitasUiState(
                        citas = ordenar(citasGuardadas),
                        aviso = "Sin conexión. Se muestran las últimas citas guardadas."
                    )
                } else {
                    CitasUiState(error = e.localizedMessage ?: "Revisa tu conexión e inténtalo de nuevo.")
                }
            }
        }
    }

    fun cerrarMensaje() {
        _uiState.value = _uiState.value.copy(mensaje = null)
    }

    /** Cancela una cita propia que todavía no empieza; la API verifica el teléfono del cliente. */
    fun cancelar(cita: CitaDto) {
        if (_uiState.value.cancelandoId != null) return
        _uiState.value = _uiState.value.copy(cancelandoId = cita.citaId, mensaje = null)
        viewModelScope.launch {
            try {
                val response = ApiClient.citaApi.cancelarCita(cita.citaId)
                if (response.isSuccessful) {
                    _uiState.value = _uiState.value.copy(cancelandoId = null, mensaje = "Cita cancelada. La hora quedó libre.")
                    cargarCitas()
                } else {
                    _uiState.value = _uiState.value.copy(
                        cancelandoId = null,
                        mensaje = response.mensajeError("No se pudo cancelar la cita")
                    )
                }
            } catch (cancelacion: CancellationException) {
                throw cancelacion
            } catch (e: Exception) {
                _uiState.value = _uiState.value.copy(
                    cancelandoId = null,
                    mensaje = e.localizedMessage ?: "Revisa tu conexión e inténtalo de nuevo."
                )
            }
        }
    }
}
