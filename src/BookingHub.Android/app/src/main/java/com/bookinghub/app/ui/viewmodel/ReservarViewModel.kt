package com.bookinghub.app.ui.viewmodel

import androidx.lifecycle.ViewModel
import androidx.lifecycle.SavedStateHandle
import androidx.lifecycle.viewModelScope
import android.content.Context
import dagger.hilt.android.qualifiers.ApplicationContext
import com.bookinghub.app.data.model.BarberoDto
import com.bookinghub.app.data.model.CatalogoItemDto
import com.bookinghub.app.data.model.CitaDetalleDto
import com.bookinghub.app.data.model.CitaDto
import com.bookinghub.app.data.model.HorarioLibreDto
import com.bookinghub.app.data.repository.ApiResult
import com.bookinghub.app.data.repository.BarberoRepository
import com.bookinghub.app.data.repository.CatalogoRepository
import com.bookinghub.app.data.repository.CitaRepository
import com.bookinghub.app.data.repository.mensajeParaUsuario
import com.bookinghub.app.util.SesionCliente
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.Job
import kotlinx.coroutines.async
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch
import java.math.BigDecimal
import java.time.LocalDate
import javax.inject.Inject
import com.bookinghub.app.util.RecordatoriosCitas

data class ReservarUiState(
    val barberos: List<BarberoDto> = emptyList(),
    val servicios: List<CatalogoItemDto> = emptyList(),
    val barberoSeleccionado: BarberoDto? = null,
    val cualquieraDisponible: Boolean = false,
    val serviciosSeleccionados: List<CatalogoItemDto> = emptyList(),
    val fechaSeleccionada: LocalDate? = null,
    val horarioSeleccionado: HorarioLibreDto? = null,
    val horariosDisponibles: List<HorarioLibreDto> = emptyList(),
    val barberoPorHorario: Map<String, BarberoDto> = emptyMap(),
    val notas: String = "",
    val cargandoDatos: Boolean = false,
    val cargandoDisponibilidad: Boolean = false,
    val enviandoReserva: Boolean = false,
    val estadoPendienteId: Int? = null,
    val errorCargaDatos: String? = null,
    val errorDisponibilidad: String? = null,
    val error: String? = null,
    val citaCreada: Boolean = false
) {
    val duracionTotalMin: Int
        get() = serviciosSeleccionados.sumOf { it.duracionMin ?: 30 }

    val totalEstimado: BigDecimal
        get() = serviciosSeleccionados.fold(BigDecimal.ZERO) { total, servicio -> total + servicio.precio }
}

@HiltViewModel
class ReservarViewModel @Inject constructor(
    private val barberoRepository: BarberoRepository,
    private val catalogoRepository: CatalogoRepository,
    private val citaRepository: CitaRepository,
    private val sesionCliente: SesionCliente,
    @ApplicationContext private val context: Context,
    private val savedStateHandle: SavedStateHandle
) : ViewModel() {
    private val _uiState = MutableStateFlow(
        ReservarUiState(
            cargandoDatos = true,
            fechaSeleccionada = savedStateHandle.get<String>(CLAVE_FECHA)?.let { runCatching { LocalDate.parse(it) }.getOrNull() },
            notas = savedStateHandle[CLAVE_NOTAS] ?: ""
        )
    )
    val uiState: StateFlow<ReservarUiState> = _uiState
    private var disponibilidadJob: Job? = null

    init {
        cargarDatos()
    }

    fun cargarDatos() {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(cargandoDatos = true, errorCargaDatos = null)
            try {
                val (barberosResultado, catalogoResultado, estadosResultado) = coroutineScope {
                    val barberos = async { barberoRepository.listar() }
                    val catalogo = async { catalogoRepository.listar() }
                    val estados = async { citaRepository.listarEstados() }
                    Triple(barberos.await(), catalogo.await(), estados.await())
                }

                val error = when {
                    barberosResultado !is ApiResult.Success ->
                        barberosResultado.mensajeParaUsuario("No se pudieron cargar los barberos.")
                    catalogoResultado !is ApiResult.Success ->
                        catalogoResultado.mensajeParaUsuario("No se pudo cargar el catálogo.")
                    estadosResultado !is ApiResult.Success ->
                        estadosResultado.mensajeParaUsuario("No se pudieron cargar los estados de cita.")
                    else -> null
                }
                if (error != null) {
                    _uiState.value = _uiState.value.copy(cargandoDatos = false, errorCargaDatos = error)
                    return@launch
                }

                val barberos = (barberosResultado as ApiResult.Success).data
                val catalogo = (catalogoResultado as ApiResult.Success).data
                val estados = (estadosResultado as ApiResult.Success).data
                val pendiente = estados
                    .firstOrNull { it.descripcion.equals("Pendiente", ignoreCase = true) }
                    ?.estadoCitaId
                val barberoGuardado = savedStateHandle.get<Int>(CLAVE_BARBERO)
                    ?.let { id -> barberos.firstOrNull { it.barberoId == id } }
                val cualquieraGuardado = savedStateHandle.get<Boolean>(CLAVE_CUALQUIERA) == true
                val idsServiciosGuardados = savedStateHandle.get<ArrayList<Int>>(CLAVE_SERVICIOS)?.toSet().orEmpty()
                val serviciosGuardados = catalogo.filter {
                    it.esServicio && it.activo && it.catalogoItemId in idsServiciosGuardados
                }
                _uiState.value = _uiState.value.copy(
                    barberos = barberos,
                    servicios = catalogo.filter { it.esServicio && it.activo },
                    barberoSeleccionado = barberoGuardado,
                    cualquieraDisponible = cualquieraGuardado,
                    serviciosSeleccionados = serviciosGuardados,
                    estadoPendienteId = pendiente,
                    cargandoDatos = false,
                    errorCargaDatos = if (pendiente == null) {
                        "La API no tiene configurado el estado Pendiente."
                    } else {
                        null
                    }
                )
                if ((barberoGuardado != null || cualquieraGuardado) && serviciosGuardados.isNotEmpty() &&
                    _uiState.value.fechaSeleccionada != null
                ) actualizarDisponibilidad()
            } catch (cancelacion: CancellationException) {
                throw cancelacion
            } catch (error: Exception) {
                _uiState.value = _uiState.value.copy(
                    cargandoDatos = false,
                    errorCargaDatos = error.localizedMessage ?: "Revisa tu conexión e inténtalo de nuevo."
                )
            }
        }
    }

    fun seleccionarBarbero(barbero: BarberoDto) {
        _uiState.value = _uiState.value.copy(
            barberoSeleccionado = barbero,
            cualquieraDisponible = false,
            horarioSeleccionado = null
        )
        savedStateHandle[CLAVE_CUALQUIERA] = false
        guardarSeleccion()
        actualizarDisponibilidad()
    }

    fun seleccionarCualquieraDisponible() {
        _uiState.value = _uiState.value.copy(
            barberoSeleccionado = null,
            cualquieraDisponible = true,
            horarioSeleccionado = null
        )
        savedStateHandle[CLAVE_CUALQUIERA] = true
        guardarSeleccion()
        actualizarDisponibilidad()
    }

    fun toggleServicio(servicio: CatalogoItemDto) {
        val actuales = _uiState.value.serviciosSeleccionados.toMutableList()
        if (actuales.any { it.catalogoItemId == servicio.catalogoItemId }) {
            actuales.removeAll { it.catalogoItemId == servicio.catalogoItemId }
        } else {
            actuales.add(servicio)
        }
        _uiState.value = _uiState.value.copy(serviciosSeleccionados = actuales, horarioSeleccionado = null)
        guardarSeleccion()
        actualizarDisponibilidad()
    }

    fun seleccionarFecha(fecha: LocalDate) {
        if (fecha.isBefore(LocalDate.now())) return
        _uiState.value = _uiState.value.copy(fechaSeleccionada = fecha, horarioSeleccionado = null)
        guardarSeleccion()
        actualizarDisponibilidad()
    }

    fun seleccionarHorario(horario: HorarioLibreDto) {
        val disponibles = _uiState.value.horariosDisponibles
        if (disponibles.any { it.inicio == horario.inicio && it.fin == horario.fin }) {
            _uiState.value = _uiState.value.copy(horarioSeleccionado = horario, error = null)
            savedStateHandle[CLAVE_HORA_INICIO] = horario.inicio
            savedStateHandle[CLAVE_HORA_FIN] = horario.fin
        }
    }

    fun recargarDisponibilidad() = actualizarDisponibilidad()

    fun actualizarNotas(notas: String) {
        _uiState.value = _uiState.value.copy(notas = notas.take(500))
        savedStateHandle[CLAVE_NOTAS] = notas.take(500)
    }

    private fun actualizarDisponibilidad() {
        disponibilidadJob?.cancel()
        val state = _uiState.value
        val barbero = state.barberoSeleccionado
        val fecha = state.fechaSeleccionada
        val duracion = state.duracionTotalMin

        if ((barbero == null && !state.cualquieraDisponible) || fecha == null || state.serviciosSeleccionados.isEmpty()) {
            _uiState.value = state.copy(
                horariosDisponibles = emptyList(),
                barberoPorHorario = emptyMap(),
                horarioSeleccionado = null,
                cargandoDisponibilidad = false,
                errorDisponibilidad = null
            )
            return
        }

        _uiState.value = state.copy(
            horariosDisponibles = emptyList(),
            horarioSeleccionado = null,
            cargandoDisponibilidad = true,
            errorDisponibilidad = null
        )
        disponibilidadJob = viewModelScope.launch {
            try {
                val disponibilidadPorBarbero = if (state.cualquieraDisponible) {
                    coroutineScope {
                        state.barberos.filter { it.activo }.map { candidato ->
                            async {
                                candidato to barberoRepository.consultarDisponibilidad(
                                    barberoId = candidato.barberoId,
                                    fecha = fecha.toString(),
                                    duracionMin = duracion
                                )
                            }
                        }.map { it.await() }
                    }
                } else {
                    listOfNotNull(barbero).map { candidato ->
                        candidato to barberoRepository.consultarDisponibilidad(
                            barberoId = candidato.barberoId,
                            fecha = fecha.toString(),
                            duracionMin = duracion
                        )
                    }
                }
                val actual = _uiState.value
                if (actual.barberoSeleccionado?.barberoId != barbero?.barberoId ||
                    actual.cualquieraDisponible != state.cualquieraDisponible ||
                    actual.fechaSeleccionada != fecha || actual.duracionTotalMin != duracion
                ) return@launch

                val respuestasValidas = disponibilidadPorBarbero.mapNotNull { (candidato, resultado) ->
                    (resultado as? ApiResult.Success)?.let { candidato to it.data.horarios }
                }
                if (respuestasValidas.isNotEmpty()) {
                    val barberoPorHorario = linkedMapOf<String, BarberoDto>()
                    respuestasValidas.forEach { (candidato, horarios) ->
                        horarios.forEach { horario -> barberoPorHorario.putIfAbsent(horario.inicio, candidato) }
                    }
                    val horarios = if (state.cualquieraDisponible) {
                        barberoPorHorario.keys.mapNotNull { inicio ->
                            respuestasValidas.asSequence().flatMap { it.second.asSequence() }
                                .firstOrNull { it.inicio == inicio }
                        }.sortedBy { it.inicio }
                    } else respuestasValidas.first().second
                    val horaInicioGuardada = savedStateHandle.get<String>(CLAVE_HORA_INICIO)
                    val horaFinGuardada = savedStateHandle.get<String>(CLAVE_HORA_FIN)
                    val horarioRestaurado = horarios.firstOrNull {
                        it.inicio == horaInicioGuardada && it.fin == horaFinGuardada
                    }
                    _uiState.value = actual.copy(
                        horariosDisponibles = horarios,
                        barberoPorHorario = barberoPorHorario,
                        horarioSeleccionado = horarioRestaurado,
                        cargandoDisponibilidad = false,
                        errorDisponibilidad = null
                    )
                    if (horarioRestaurado == null) {
                        savedStateHandle.remove<String>(CLAVE_HORA_INICIO)
                        savedStateHandle.remove<String>(CLAVE_HORA_FIN)
                    }
                } else {
                    val resultadoError = disponibilidadPorBarbero.firstOrNull()?.second
                    _uiState.value = actual.copy(
                        horariosDisponibles = emptyList(),
                        barberoPorHorario = emptyMap(),
                        cargandoDisponibilidad = false,
                        errorDisponibilidad = resultadoError?.mensajeParaUsuario(
                            "No se pudo consultar la disponibilidad."
                        ) ?: "No se pudo consultar la disponibilidad."
                    )
                }
            } catch (cancelacion: CancellationException) {
                throw cancelacion
            } catch (error: Exception) {
                val actual = _uiState.value
                if (actual.barberoSeleccionado?.barberoId == barbero?.barberoId &&
                    actual.cualquieraDisponible == state.cualquieraDisponible &&
                    actual.fechaSeleccionada == fecha && actual.duracionTotalMin == duracion
                ) {
                    _uiState.value = actual.copy(
                        horariosDisponibles = emptyList(),
                        cargandoDisponibilidad = false,
                        errorDisponibilidad = error.localizedMessage
                            ?: "Revisa tu conexión e inténtalo de nuevo."
                    )
                }
            }
        }
    }

    fun crearCita() {
        val state = _uiState.value
        val clienteId = sesionCliente.clienteActual?.clienteId
        val barbero = state.barberoSeleccionado
        val fecha = state.fechaSeleccionada
        val horario = state.horarioSeleccionado
        val estadoPendiente = state.estadoPendienteId

        val barberoAsignado = barbero ?: horario?.let { state.barberoPorHorario[it.inicio] }
        if (clienteId == null || barberoAsignado == null || fecha == null || horario == null ||
            state.serviciosSeleccionados.isEmpty() || estadoPendiente == null
        ) {
            _uiState.value = state.copy(error = "Completa todos los datos de la reserva e inténtalo de nuevo.")
            return
        }

        if (state.horariosDisponibles.none { it.inicio == horario.inicio && it.fin == horario.fin }) {
            _uiState.value = state.copy(
                horarioSeleccionado = null,
                error = "El horario ya no está disponible. Selecciona otro."
            )
            actualizarDisponibilidad()
            return
        }

        val detalles = state.serviciosSeleccionados.map { servicio ->
            CitaDetalleDto(
                catalogoItemId = servicio.catalogoItemId,
                nombre = servicio.nombre,
                cantidad = 1,
                precioUnitario = servicio.precio
            )
        }
        val cita = CitaDto(
            clienteId = clienteId,
            barberoId = barberoAsignado.barberoId,
            estadoCitaId = estadoPendiente,
            fechaInicio = horario.inicio,
            fechaFin = horario.fin,
            total = state.totalEstimado,
            notas = state.notas.ifBlank { null },
            detalles = detalles
        )

        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(enviandoReserva = true, error = null)
            try {
                when (val resultado = citaRepository.crear(cita)) {
                    is ApiResult.Success -> {
                        RecordatoriosCitas.programar(context, resultado.data)
                        limpiarSeleccionGuardada()
                        _uiState.value = _uiState.value.copy(citaCreada = true, enviandoReserva = false)
                    }
                    else -> {
                        _uiState.value = _uiState.value.copy(
                            error = resultado.mensajeParaUsuario("No se pudo crear la cita."),
                            enviandoReserva = false
                        )
                        if (resultado is ApiResult.HttpError && resultado.statusCode == 400) {
                            actualizarDisponibilidad()
                        }
                    }
                }
            } catch (cancelacion: CancellationException) {
                throw cancelacion
            } catch (error: Exception) {
                _uiState.value = _uiState.value.copy(
                    error = error.localizedMessage ?: "Revisa tu conexión e inténtalo de nuevo.",
                    enviandoReserva = false
                )
            }
        }
    }

    private fun guardarSeleccion() {
        val state = _uiState.value
        savedStateHandle[CLAVE_BARBERO] = state.barberoSeleccionado?.barberoId
        savedStateHandle[CLAVE_CUALQUIERA] = state.cualquieraDisponible
        savedStateHandle[CLAVE_SERVICIOS] = ArrayList(state.serviciosSeleccionados.map { it.catalogoItemId })
        savedStateHandle[CLAVE_FECHA] = state.fechaSeleccionada?.toString()
        savedStateHandle[CLAVE_NOTAS] = state.notas
        if (state.horarioSeleccionado == null) {
            savedStateHandle.remove<String>(CLAVE_HORA_INICIO)
            savedStateHandle.remove<String>(CLAVE_HORA_FIN)
        }
    }

    private fun limpiarSeleccionGuardada() {
        savedStateHandle.remove<Int>(CLAVE_BARBERO)
        savedStateHandle.remove<Boolean>(CLAVE_CUALQUIERA)
        savedStateHandle.remove<ArrayList<Int>>(CLAVE_SERVICIOS)
        savedStateHandle.remove<String>(CLAVE_FECHA)
        savedStateHandle.remove<String>(CLAVE_NOTAS)
        savedStateHandle.remove<String>(CLAVE_HORA_INICIO)
        savedStateHandle.remove<String>(CLAVE_HORA_FIN)
    }

    private companion object {
        const val CLAVE_BARBERO = "reserva_barbero_id"
        const val CLAVE_CUALQUIERA = "reserva_cualquiera_disponible"
        const val CLAVE_SERVICIOS = "reserva_servicios_ids"
        const val CLAVE_FECHA = "reserva_fecha"
        const val CLAVE_NOTAS = "reserva_notas"
        const val CLAVE_HORA_INICIO = "reserva_hora_inicio"
        const val CLAVE_HORA_FIN = "reserva_hora_fin"
    }
}
