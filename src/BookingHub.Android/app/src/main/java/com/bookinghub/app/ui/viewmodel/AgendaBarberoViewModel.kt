package com.bookinghub.app.ui.viewmodel

import android.content.Context
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.bookinghub.app.data.model.CitaDto
import com.bookinghub.app.data.model.BloqueoAgendaDto
import com.bookinghub.app.data.model.CrearBloqueoAgendaRequest
import com.bookinghub.app.data.repository.ApiResult
import com.bookinghub.app.data.repository.CitaRepository
import com.bookinghub.app.data.repository.BarberoRepository
import com.bookinghub.app.data.repository.mensajeParaUsuario
import com.bookinghub.app.util.CitasCache
import com.bookinghub.app.util.SesionBarbero
import dagger.hilt.android.lifecycle.HiltViewModel
import dagger.hilt.android.qualifiers.ApplicationContext
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.Job
import kotlinx.coroutines.async
import kotlinx.coroutines.awaitAll
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch
import java.time.LocalDate
import java.time.DayOfWeek
import javax.inject.Inject

data class AgendaBarberoUiState(
    val fecha: LocalDate = LocalDate.now(),
    val vistaSemanal: Boolean = false,
    val citas: List<CitaDto> = emptyList(),
    val citasPorDia: Map<LocalDate, List<CitaDto>> = emptyMap(),
    val cargando: Boolean = false,
    val error: String? = null,
    val aviso: String? = null,
    val estados: List<com.bookinghub.app.data.model.EstadoCitaDto> = emptyList(),
    val bloqueos: List<BloqueoAgendaDto> = emptyList(),
    val cargandoBloqueos: Boolean = false,
    val citaEnActualizacion: Int? = null,
    val sesionExpirada: Boolean = false
)

/** Consulta la agenda del barbero y conserva la última respuesta cifrada para modo sin conexión. */
@HiltViewModel
class AgendaBarberoViewModel @Inject constructor(
    private val citaRepository: CitaRepository,
    private val barberoRepository: BarberoRepository,
    private val citasCache: CitasCache,
    private val sesionBarbero: SesionBarbero,
    @ApplicationContext private val context: Context
) : ViewModel() {
    private val _uiState = MutableStateFlow(AgendaBarberoUiState())
    val uiState: StateFlow<AgendaBarberoUiState> = _uiState
    private var cargaActual: Job? = null
    private var cargaSemanaActual: Job? = null

    init {
        cargarEstados()
        cargarCitas()
        cargarBloqueos(_uiState.value.fecha)
    }

    private fun cargarEstados() {
        viewModelScope.launch {
            val result = citaRepository.listarEstados()
            if (result is ApiResult.Success) _uiState.value = _uiState.value.copy(estados = result.data)
        }
    }

    fun cambiarEstado(citaId: Int, estadoCitaId: Int) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(citaEnActualizacion = citaId, aviso = null)
            when (val result = citaRepository.cambiarEstado(citaId, estadoCitaId)) {
                is ApiResult.Success -> _uiState.value = _uiState.value.copy(
                    citas = _uiState.value.citas.map { if (it.citaId == citaId) result.data else it },
                    citasPorDia = _uiState.value.citasPorDia.mapValues { (_, citas) ->
                        citas.map { if (it.citaId == citaId) result.data else it }
                    },
                    citaEnActualizacion = null
                )
                ApiResult.SessionExpired -> _uiState.value = _uiState.value.copy(
                    citaEnActualizacion = null,
                    sesionExpirada = true,
                    aviso = "Tu sesión venció. Inicia sesión de nuevo."
                )
                else -> _uiState.value = _uiState.value.copy(
                    citaEnActualizacion = null,
                    aviso = result.mensajeParaUsuario("No se pudo cambiar el estado de la cita.")
                )
            }
        }
    }

    fun seleccionarFecha(fecha: LocalDate) {
        if (fecha == _uiState.value.fecha) return
        _uiState.value = _uiState.value.copy(fecha = fecha, citas = emptyList(), citasPorDia = emptyMap())
        if (_uiState.value.vistaSemanal) cargarSemana(fecha) else cargarCitas(fecha)
        cargarBloqueos(fecha)
    }

    fun alternarVistaSemanal(semanal: Boolean) {
        if (_uiState.value.vistaSemanal == semanal) return
        _uiState.value = _uiState.value.copy(vistaSemanal = semanal)
        if (semanal) cargarSemana(_uiState.value.fecha) else cargarCitas(_uiState.value.fecha)
    }

    fun recargar() {
        if (_uiState.value.vistaSemanal) cargarSemana(_uiState.value.fecha) else cargarCitas(_uiState.value.fecha)
    }

    fun crearBloqueoDia(fecha: LocalDate, tipo: String = "Descanso") {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(cargandoBloqueos = true, aviso = null)
            val request = CrearBloqueoAgendaRequest(fecha.atStartOfDay().toString(), fecha.plusDays(1).atStartOfDay().toString(), tipo)
            when (val result = barberoRepository.crearBloqueo(request)) {
                is ApiResult.Success -> {
                    cargarBloqueos(fecha)
                    _uiState.value = _uiState.value.copy(aviso = "El día quedó bloqueado en tu agenda.")
                }
                else -> _uiState.value = _uiState.value.copy(cargandoBloqueos = false,
                    aviso = result.mensajeParaUsuario("No se pudo crear el bloqueo."))
            }
        }
    }

    fun eliminarBloqueo(id: Int) {
        val fecha = _uiState.value.fecha
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(cargandoBloqueos = true, aviso = null)
            when (val result = barberoRepository.eliminarBloqueo(id)) {
                is ApiResult.Success -> cargarBloqueos(fecha)
                else -> _uiState.value = _uiState.value.copy(cargandoBloqueos = false,
                    aviso = result.mensajeParaUsuario("No se pudo quitar el bloqueo."))
            }
        }
    }

    private fun cargarBloqueos(fecha: LocalDate) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(cargandoBloqueos = true)
            when (val result = barberoRepository.listarBloqueos(fecha.toString())) {
                is ApiResult.Success -> if (_uiState.value.fecha == fecha) {
                    _uiState.value = _uiState.value.copy(bloqueos = result.data, cargandoBloqueos = false)
                }
                ApiResult.SessionExpired -> _uiState.value = _uiState.value.copy(cargandoBloqueos = false, sesionExpirada = true)
                else -> _uiState.value = _uiState.value.copy(cargandoBloqueos = false,
                    aviso = result.mensajeParaUsuario("No se pudieron consultar los bloqueos del día."))
            }
        }
    }

    private fun cargarCitas(fecha: LocalDate = _uiState.value.fecha) {
        val usuarioId = sesionBarbero.sesionActual?.usuarioId
        if (usuarioId == null) {
            _uiState.value = _uiState.value.copy(
                cargando = false,
                error = "Inicia sesión como barbero para consultar tu agenda."
            )
            return
        }

        val citasGuardadas = citasCache.leerBarbero(context, usuarioId, fecha.toString())
        cargaSemanaActual?.cancel()
        cargaActual?.cancel()
        _uiState.value = _uiState.value.copy(
            fecha = fecha,
            vistaSemanal = false,
            citasPorDia = emptyMap(),
            citas = citasGuardadas.orEmpty(),
            cargando = true,
            error = null,
            aviso = null,
            sesionExpirada = false
        )
        cargaActual = viewModelScope.launch {
            try {
                when (val resultado = citaRepository.listarAgendaBarbero(fecha.toString())) {
                    is ApiResult.Success -> {
                        if (_uiState.value.fecha != fecha) return@launch
                        citasCache.guardarBarbero(context, usuarioId, fecha.toString(), resultado.data)
                        _uiState.value = _uiState.value.copy(
                            citas = resultado.data,
                            cargando = false,
                            error = null,
                            aviso = null
                        )
                    }
                    ApiResult.SessionExpired -> {
                        _uiState.value = _uiState.value.copy(
                            cargando = false,
                            error = resultado.mensajeParaUsuario("Tu sesión venció."),
                            sesionExpirada = true
                        )
                    }
                    else -> {
                        val mensaje = resultado.mensajeParaUsuario("No se pudo cargar tu agenda.")
                        _uiState.value = if (citasGuardadas != null) {
                            _uiState.value.copy(
                                cargando = false,
                                error = null,
                                aviso = if (resultado == ApiResult.NetworkError) {
                                    "Sin conexión. Se muestra la agenda guardada."
                                } else {
                                    "No se pudo actualizar. Se muestra la agenda guardada."
                                }
                            )
                        } else {
                            _uiState.value.copy(cargando = false, error = mensaje)
                        }
                    }
                }
            } catch (cancelacion: CancellationException) {
                throw cancelacion
            } catch (error: Exception) {
                if (_uiState.value.fecha == fecha) {
                    _uiState.value = if (citasGuardadas != null) {
                        _uiState.value.copy(
                            cargando = false,
                            error = null,
                            aviso = "No se pudo actualizar. Se muestra la agenda guardada."
                        )
                    } else {
                        _uiState.value.copy(
                            cargando = false,
                            error = error.localizedMessage ?: "Revisa tu conexión e inténtalo de nuevo."
                        )
                    }
                }
            }
        }
    }

    private fun cargarSemana(fechaReferencia: LocalDate) {
        val usuarioId = sesionBarbero.sesionActual?.usuarioId
        if (usuarioId == null) {
            _uiState.value = _uiState.value.copy(cargando = false, error = "Inicia sesión como barbero para consultar tu agenda.")
            return
        }

        val inicioSemana = fechaReferencia.with(DayOfWeek.MONDAY)
        val fechas = (0..6).map { inicioSemana.plusDays(it.toLong()) }
        val cachePorDia = fechas.mapNotNull { dia ->
            citasCache.leerBarbero(context, usuarioId, dia.toString())?.let { dia to it }
        }.toMap()
        cargaActual?.cancel()
        cargaSemanaActual?.cancel()
        _uiState.value = _uiState.value.copy(
            fecha = fechaReferencia,
            vistaSemanal = true,
            citas = emptyList(),
            citasPorDia = cachePorDia,
            cargando = true,
            error = null,
            aviso = null,
            sesionExpirada = false
        )

        cargaSemanaActual = viewModelScope.launch {
            try {
                val resultados = coroutineScope {
                    fechas.map { dia -> async { dia to citaRepository.listarAgendaBarbero(dia.toString()) } }.awaitAll()
                }
                if (!_uiState.value.vistaSemanal || _uiState.value.fecha != fechaReferencia) return@launch

                var sesionExpirada = false
                var huboFallo = false
                val datos = fechas.associateWith { dia ->
                    val resultado = resultados.first { it.first == dia }.second
                    when (resultado) {
                        is ApiResult.Success -> {
                            citasCache.guardarBarbero(context, usuarioId, dia.toString(), resultado.data)
                            resultado.data
                        }
                        ApiResult.SessionExpired -> {
                            sesionExpirada = true
                            huboFallo = true
                            cachePorDia[dia].orEmpty()
                        }
                        else -> {
                            huboFallo = true
                            cachePorDia[dia].orEmpty()
                        }
                    }
                }
                val sinCache = huboFallo && datos.all { (dia, citas) -> dia !in cachePorDia && citas.isEmpty() }
                _uiState.value = _uiState.value.copy(
                    citasPorDia = datos,
                    cargando = false,
                    error = if (sinCache) "No se pudo cargar la agenda semanal." else null,
                    aviso = if (huboFallo && !sinCache) "Algunos días no se pudieron actualizar; se conservaron los datos guardados." else null,
                    sesionExpirada = sesionExpirada
                )
            } catch (cancelacion: CancellationException) {
                throw cancelacion
            } catch (error: Exception) {
                if (_uiState.value.vistaSemanal && _uiState.value.fecha == fechaReferencia) {
                    _uiState.value = _uiState.value.copy(
                        cargando = false,
                        error = if (cachePorDia.isEmpty()) error.localizedMessage ?: "No se pudo cargar la agenda semanal." else null,
                        aviso = if (cachePorDia.isNotEmpty()) "Sin conexión. Se muestra la agenda semanal guardada." else null
                    )
                }
            }
        }
    }
}
