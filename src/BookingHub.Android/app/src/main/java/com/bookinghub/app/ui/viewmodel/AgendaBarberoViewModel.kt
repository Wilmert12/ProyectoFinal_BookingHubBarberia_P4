package com.bookinghub.app.ui.viewmodel

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.bookinghub.app.data.api.ApiClient
import com.bookinghub.app.data.api.mensajeError
import com.bookinghub.app.data.model.CitaDto
import com.bookinghub.app.util.CitasCache
import com.bookinghub.app.util.SesionBarbero
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.Job
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch
import java.time.LocalDate

data class AgendaBarberoUiState(
    val fecha: LocalDate = LocalDate.now(),
    val citas: List<CitaDto> = emptyList(),
    val cargando: Boolean = false,
    val error: String? = null,
    val aviso: String? = null,
    val sesionExpirada: Boolean = false
)

/** Consulta solo la agenda asociada al usuario autenticado como barbero. */
class AgendaBarberoViewModel(application: Application) : AndroidViewModel(application) {
    private val _uiState = MutableStateFlow(AgendaBarberoUiState())
    val uiState: StateFlow<AgendaBarberoUiState> = _uiState
    private var cargaActual: Job? = null

    init {
        cargarCitas()
    }

    fun seleccionarFecha(fecha: LocalDate) {
        if (fecha == _uiState.value.fecha) return
        _uiState.value = _uiState.value.copy(fecha = fecha, citas = emptyList())
        cargarCitas(fecha)
    }

    fun recargar() = cargarCitas(_uiState.value.fecha)

    private fun cargarCitas(fecha: LocalDate = _uiState.value.fecha) {
        val usuarioId = SesionBarbero.sesionActual?.usuarioId
        if (usuarioId == null) {
            _uiState.value = _uiState.value.copy(
                cargando = false,
                error = "Inicia sesión como barbero para consultar tu agenda."
            )
            return
        }

        val citasGuardadas = CitasCache.leerBarbero(getApplication(), usuarioId, fecha.toString())
        cargaActual?.cancel()
        _uiState.value = _uiState.value.copy(
            fecha = fecha,
            citas = citasGuardadas.orEmpty(),
            cargando = true,
            error = null,
            aviso = null
        )
        cargaActual = viewModelScope.launch {
            try {
                val response = ApiClient.citaApi.getMisCitasBarbero(fecha.toString())
                if (_uiState.value.fecha != fecha) return@launch

                if (response.isSuccessful) {
                    val citas = response.body().orEmpty()
                    CitasCache.guardarBarbero(getApplication(), usuarioId, fecha.toString(), citas)
                    _uiState.value = _uiState.value.copy(
                        citas = citas,
                        cargando = false,
                        error = null,
                        aviso = null
                    )
                } else if (response.code() == 401) {
                    _uiState.value = _uiState.value.copy(
                        cargando = false,
                        error = "Tu sesión venció. Inicia sesión nuevamente.",
                        sesionExpirada = true
                    )
                } else {
                    val mensaje = response.mensajeError("No se pudo cargar tu agenda")
                    _uiState.value = if (citasGuardadas != null) {
                        _uiState.value.copy(
                            cargando = false,
                            error = null,
                            aviso = "No se pudo actualizar. Se muestra la agenda guardada."
                        )
                    } else {
                        _uiState.value.copy(cargando = false, error = mensaje)
                    }
                }
            } catch (cancelacion: CancellationException) {
                throw cancelacion
            } catch (e: Exception) {
                if (_uiState.value.fecha == fecha) {
                    _uiState.value = if (citasGuardadas != null) {
                        _uiState.value.copy(
                            cargando = false,
                            error = null,
                            aviso = "Sin conexión. Se muestra la agenda guardada."
                        )
                    } else {
                        _uiState.value.copy(
                            cargando = false,
                            error = e.localizedMessage ?: "Revisa tu conexión e inténtalo de nuevo."
                        )
                    }
                }
            }
        }
    }
}
