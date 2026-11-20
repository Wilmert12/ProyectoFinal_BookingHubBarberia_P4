package com.bookinghub.app.ui.viewmodel

import android.content.Context
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.bookinghub.app.data.model.CitaDto
import com.bookinghub.app.data.repository.ApiResult
import com.bookinghub.app.data.repository.CitaRepository
import com.bookinghub.app.data.repository.ClienteRepository
import com.bookinghub.app.data.model.CrearResenaRequest
import com.bookinghub.app.data.repository.mensajeParaUsuario
import com.bookinghub.app.util.CitasCache
import com.bookinghub.app.util.SesionCliente
import com.bookinghub.app.util.RecordatoriosCitas
import dagger.hilt.android.lifecycle.HiltViewModel
import dagger.hilt.android.qualifiers.ApplicationContext
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.Job
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch
import kotlinx.coroutines.async
import kotlinx.coroutines.coroutineScope
import javax.inject.Inject

data class CitasUiState(
    val citas: List<CitaDto> = emptyList(),
    val cargando: Boolean = false,
    val error: String? = null,
    val aviso: String? = null,
    val citaEnActualizacion: Int? = null,
    val resenasEnviadas: Set<Int> = emptySet(),
    val paginaHistorial: Int = 0,
    val historialTieneMas: Boolean = false,
    val cargandoMasHistorial: Boolean = false
)

@HiltViewModel
class CitasViewModel @Inject constructor(
    private val citaRepository: CitaRepository,
    private val clienteRepository: ClienteRepository,
    private val citasCache: CitasCache,
    private val sesionCliente: SesionCliente,
    @ApplicationContext private val context: Context
) : ViewModel() {
    private val _uiState = MutableStateFlow(CitasUiState())
    val uiState: StateFlow<CitasUiState> = _uiState
    private var cargaActual: Job? = null

    init {
        cargarCitas()
    }

    fun cargarCitas() {
        val clienteId = sesionCliente.clienteActual?.clienteId
        if (clienteId == null) {
            _uiState.value = CitasUiState(error = "No hay un perfil de cliente guardado en este dispositivo.")
            return
        }

        val citasGuardadas = citasCache.leerCliente(context, clienteId)
        cargaActual?.cancel()
        _uiState.value = CitasUiState(citas = citasGuardadas.orEmpty(), cargando = true)
        cargaActual = viewModelScope.launch {
            try {
                val (proximas, historial) = coroutineScope {
                    val proximasDeferred = async { citaRepository.listarDelCliente() }
                    val historialDeferred = async { citaRepository.listarHistorial(pagina = 1) }
                    proximasDeferred.await() to historialDeferred.await()
                }
                val datosProximos = (proximas as? ApiResult.Success)?.data
                    ?: citasGuardadas?.filterNot { it.esHistorial() }
                val pagina = (historial as? ApiResult.Success)?.data
                val datosHistorial = pagina?.items ?: citasGuardadas?.filter { it.esHistorial() }
                val combinadas = (datosProximos.orEmpty() + datosHistorial.orEmpty()).distinctBy { it.citaId }
                val avisos = buildList {
                    if (proximas !is ApiResult.Success) add("No se pudieron actualizar las próximas citas.")
                    if (historial !is ApiResult.Success) add("No se pudo actualizar el historial.")
                    if (citasGuardadas != null && (proximas !is ApiResult.Success || historial !is ApiResult.Success)) {
                        clear()
                        add("Sin conexión o con conexión inestable. Se muestran las últimas citas guardadas.")
                    }
                }
                if (combinadas.isNotEmpty() || proximas is ApiResult.Success || historial is ApiResult.Success) {
                    RecordatoriosCitas.sincronizar(context, combinadas)
                    citasCache.guardarCliente(context, clienteId, combinadas)
                    _uiState.value = CitasUiState(
                        citas = combinadas,
                        aviso = avisos.joinToString(" ").ifBlank { null },
                        paginaHistorial = pagina?.pagina ?: 0,
                        historialTieneMas = pagina?.tieneMas ?: false
                    )
                } else {
                    _uiState.value = CitasUiState(error = proximas.mensajeParaUsuario("No se pudieron cargar tus citas."))
                }
            } catch (cancelacion: CancellationException) {
                throw cancelacion
            } catch (error: Exception) {
                _uiState.value = if (citasGuardadas != null) {
                    CitasUiState(
                        citas = citasGuardadas,
                        aviso = "No se pudo actualizar. Se muestran las últimas citas guardadas."
                    )
                } else {
                    CitasUiState(
                        error = error.localizedMessage ?: "Revisa tu conexión e inténtalo de nuevo."
                    )
                }
            }
        }
    }

    fun cancelar(citaId: Int) = actualizarCita(citaId) { citaRepository.cancelar(citaId, "Cancelada desde la app") }

    fun reprogramar(citaId: Int, nuevaFechaInicio: String) = actualizarCita(citaId) {
        citaRepository.reprogramar(citaId, nuevaFechaInicio)
    }

    fun calificar(citaId: Int, puntaje: Int, comentario: String?) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(citaEnActualizacion = citaId, aviso = null)
            val resultado = clienteRepository.crearResena(CrearResenaRequest(citaId, puntaje, comentario))
            if (resultado is ApiResult.Success) {
                _uiState.value = _uiState.value.copy(
                    citaEnActualizacion = null,
                    resenasEnviadas = _uiState.value.resenasEnviadas + citaId,
                    aviso = "Gracias por calificar tu cita."
                )
            } else {
                _uiState.value = _uiState.value.copy(
                    citaEnActualizacion = null,
                    aviso = resultado.mensajeParaUsuario("No se pudo guardar la reseña.")
                )
            }
        }
    }

    fun cargarMasHistorial() {
        val state = _uiState.value
        if (!state.historialTieneMas || state.cargandoMasHistorial) return
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(cargandoMasHistorial = true, aviso = null)
            when (val resultado = citaRepository.listarHistorial(state.paginaHistorial + 1)) {
                is ApiResult.Success -> {
                    val citas = (_uiState.value.citas + resultado.data.items).distinctBy { it.citaId }
                    _uiState.value = _uiState.value.copy(
                        citas = citas,
                        paginaHistorial = resultado.data.pagina,
                        historialTieneMas = resultado.data.tieneMas,
                        cargandoMasHistorial = false
                    )
                    sesionCliente.clienteActual?.clienteId?.let { citasCache.guardarCliente(context, it, citas) }
                }
                else -> _uiState.value = _uiState.value.copy(
                    cargandoMasHistorial = false,
                    aviso = resultado.mensajeParaUsuario("No se pudo cargar más historial. Inténtalo de nuevo.")
                )
            }
        }
    }

    private fun actualizarCita(citaId: Int, operacion: suspend () -> ApiResult<CitaDto>) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(citaEnActualizacion = citaId, aviso = null)
            try {
                when (val resultado = operacion()) {
                    is ApiResult.Success -> {
                        val actualizadas = _uiState.value.citas.map { if (it.citaId == citaId) resultado.data else it }
                        if (resultado.data.estaCerrada()) RecordatoriosCitas.cancelar(context, citaId)
                        else RecordatoriosCitas.programar(context, resultado.data)
                        _uiState.value = _uiState.value.copy(citas = actualizadas, citaEnActualizacion = null, aviso = null)
                        val clienteId = sesionCliente.clienteActual?.clienteId
                        if (clienteId != null) citasCache.guardarCliente(context, clienteId, actualizadas)
                    }
                    else -> _uiState.value = _uiState.value.copy(
                        citaEnActualizacion = null,
                        aviso = resultado.mensajeParaUsuario("No se pudo actualizar la cita.")
                    )
                }
            } catch (cancelacion: CancellationException) {
                throw cancelacion
            } catch (error: Exception) {
                _uiState.value = _uiState.value.copy(
                    citaEnActualizacion = null,
                    aviso = error.localizedMessage ?: "No se pudo actualizar la cita."
                )
            }
        }
    }

    private fun CitaDto.estaCerrada(): Boolean = estado.orEmpty().lowercase(java.util.Locale.ROOT)
        .filter(Char::isLetterOrDigit) in setOf("cancelada", "completada", "noshow")

    private fun CitaDto.esHistorial(): Boolean = estaCerrada() || runCatching {
        val hora = runCatching { java.time.Instant.parse(fechaInicio) }
            .getOrElse { java.time.LocalDateTime.parse(fechaInicio.take(19)).atZone(java.time.ZoneId.systemDefault()).toInstant() }
        hora.isBefore(java.time.Instant.now())
    }.getOrDefault(false)
}
