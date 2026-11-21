package com.bookinghub.app.ui.viewmodel

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.bookinghub.app.data.api.ApiClient
import com.bookinghub.app.data.api.mensajeError
import com.bookinghub.app.data.model.CambioEstadoDto
import com.bookinghub.app.data.model.CitaDto
import com.bookinghub.app.util.CitasCache
import com.bookinghub.app.util.EstadosCita
import com.bookinghub.app.util.SesionBarbero
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.Job
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch
import java.math.BigDecimal
import java.time.DayOfWeek
import java.time.LocalDate

data class AgendaBarberoUiState(
    val fecha: LocalDate = LocalDate.now(),
    val semana: Boolean = false,
    val citas: List<CitaDto> = emptyList(),
    val cargando: Boolean = false,
    val error: String? = null,
    val aviso: String? = null,
    val sesionExpirada: Boolean = false,
    val cambiandoId: Int? = null,
    val mensajeEstado: String? = null
) {
    /** Lunes de la semana que contiene la fecha elegida. */
    val lunes: LocalDate get() = fecha.with(DayOfWeek.MONDAY)
    val desde: LocalDate get() = if (semana) lunes else fecha
    val activas: List<CitaDto> get() = citas.filterNot { EstadosCita.estaLibre(it.estado) }
    val completadas: Int get() = citas.count { it.estado == "Completada" }
    val total: BigDecimal get() = activas.fold(BigDecimal.ZERO) { suma, cita -> suma + cita.total }
}

/** Agenda del barbero con sesión: un día o una semana, y los estados que él puede marcar. */
class AgendaBarberoViewModel(application: Application) : AndroidViewModel(application) {
    private val _uiState = MutableStateFlow(AgendaBarberoUiState())
    val uiState: StateFlow<AgendaBarberoUiState> = _uiState
    private var cargaActual: Job? = null
    private var estados: Map<String, Int> = emptyMap()

    init {
        cargarCitas()
    }

    fun seleccionarFecha(fecha: LocalDate) {
        if (fecha == _uiState.value.fecha) return
        _uiState.value = _uiState.value.copy(fecha = fecha, citas = emptyList(), mensajeEstado = null)
        cargarCitas()
    }

    /** Día anterior o siguiente; en la vista de semana, una semana completa. */
    fun mover(pasos: Int) {
        val estado = _uiState.value
        seleccionarFecha(if (estado.semana) estado.fecha.plusWeeks(pasos.toLong()) else estado.fecha.plusDays(pasos.toLong()))
    }

    fun cambiarVista(semana: Boolean) {
        if (semana == _uiState.value.semana) return
        _uiState.value = _uiState.value.copy(semana = semana, citas = emptyList(), mensajeEstado = null)
        cargarCitas()
    }

    /** Desde la semana, tocar un día abre ese día. */
    fun verDia(dia: LocalDate) {
        _uiState.value = _uiState.value.copy(semana = false, fecha = dia, citas = emptyList(), mensajeEstado = null)
        cargarCitas()
    }

    fun recargar() = cargarCitas()

    fun cerrarMensajeEstado() {
        _uiState.value = _uiState.value.copy(mensajeEstado = null)
    }

    private fun cargarCitas() {
        val usuarioId = SesionBarbero.sesionActual?.usuarioId
        if (usuarioId == null) {
            _uiState.value = _uiState.value.copy(
                cargando = false,
                error = "Inicia sesión como barbero para consultar tu agenda."
            )
            return
        }

        val pedido = _uiState.value
        val desde = pedido.desde
        val semana = pedido.semana
        val claveCache = if (semana) "semana_$desde" else desde.toString()
        val citasGuardadas = CitasCache.leerBarbero(getApplication(), usuarioId, claveCache)

        cargaActual?.cancel()
        _uiState.value = pedido.copy(
            citas = citasGuardadas.orEmpty(),
            cargando = true,
            error = null,
            aviso = null
        )
        cargaActual = viewModelScope.launch {
            try {
                val response = ApiClient.citaApi.getMisCitasBarbero(desde.toString(), if (semana) 7 else 1)
                if (_uiState.value.desde != desde || _uiState.value.semana != semana) return@launch

                when {
                    response.isSuccessful -> {
                        val citas = response.body().orEmpty().sortedBy { it.fechaInicio }
                        CitasCache.guardarBarbero(getApplication(), usuarioId, claveCache, citas)
                        _uiState.value = _uiState.value.copy(citas = citas, cargando = false, error = null, aviso = null)
                    }
                    response.code() == 401 -> _uiState.value = _uiState.value.copy(
                        cargando = false,
                        error = "Tu sesión venció. Inicia sesión nuevamente.",
                        sesionExpirada = true
                    )
                    else -> {
                        val mensaje = response.mensajeError("No se pudo cargar tu agenda")
                        _uiState.value = if (citasGuardadas != null) {
                            _uiState.value.copy(cargando = false, aviso = "No se pudo actualizar. Se muestra la agenda guardada.")
                        } else {
                            _uiState.value.copy(cargando = false, error = mensaje)
                        }
                    }
                }
            } catch (cancelacion: CancellationException) {
                throw cancelacion
            } catch (e: Exception) {
                if (_uiState.value.desde == desde && _uiState.value.semana == semana) {
                    _uiState.value = if (citasGuardadas != null) {
                        _uiState.value.copy(cargando = false, aviso = "Sin conexión. Se muestra la agenda guardada.")
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

    /** Marca una cita (Confirmada, EnProceso, Completada o NoShow) y recarga la agenda. */
    fun cambiarEstado(cita: CitaDto, estado: String) {
        if (_uiState.value.cambiandoId != null) return
        _uiState.value = _uiState.value.copy(cambiandoId = cita.citaId, mensajeEstado = null)

        viewModelScope.launch {
            try {
                if (estados.isEmpty()) {
                    val respuesta = ApiClient.estadosCitaApi.getEstadosCita()
                    if (respuesta.isSuccessful) {
                        estados = respuesta.body().orEmpty().associate { it.descripcion to it.estadoCitaId }
                    }
                }
                val estadoId = estados[estado]
                if (estadoId == null) {
                    _uiState.value = _uiState.value.copy(cambiandoId = null, mensajeEstado = "No se encontró el estado $estado.")
                    return@launch
                }

                val response = ApiClient.citaApi.cambiarEstado(cita.citaId, CambioEstadoDto(estadoId))
                when {
                    response.isSuccessful -> {
                        _uiState.value = _uiState.value.copy(cambiandoId = null)
                        cargarCitas()
                    }
                    response.code() == 401 -> _uiState.value = _uiState.value.copy(cambiandoId = null, sesionExpirada = true)
                    else -> _uiState.value = _uiState.value.copy(
                        cambiandoId = null,
                        mensajeEstado = response.mensajeError("No se pudo cambiar el estado")
                    )
                }
            } catch (cancelacion: CancellationException) {
                throw cancelacion
            } catch (e: Exception) {
                _uiState.value = _uiState.value.copy(
                    cambiandoId = null,
                    mensajeEstado = e.localizedMessage ?: "Revisa tu conexión e inténtalo de nuevo."
                )
            }
        }
    }
}
