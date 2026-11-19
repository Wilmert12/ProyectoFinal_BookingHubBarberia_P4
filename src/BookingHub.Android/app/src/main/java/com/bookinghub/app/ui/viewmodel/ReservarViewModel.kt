package com.bookinghub.app.ui.viewmodel

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.bookinghub.app.data.api.ApiClient
import com.bookinghub.app.data.api.mensajeError
import com.bookinghub.app.data.model.BarberoDto
import com.bookinghub.app.data.model.CitaDetalleDto
import com.bookinghub.app.data.model.CitaDto
import com.bookinghub.app.data.model.DisponibilidadDto
import com.bookinghub.app.data.model.HorarioLibreDto
import com.bookinghub.app.data.model.CatalogoItemDto
import com.bookinghub.app.util.SesionCliente
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.Job
import kotlinx.coroutines.async
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch
import java.math.BigDecimal
import java.time.LocalDate

data class ReservarUiState(
    val barberos: List<BarberoDto> = emptyList(),
    val servicios: List<CatalogoItemDto> = emptyList(),
    val barberoSeleccionado: BarberoDto? = null,
    val serviciosSeleccionados: List<CatalogoItemDto> = emptyList(),
    val fechaSeleccionada: LocalDate? = null,
    val horarioSeleccionado: HorarioLibreDto? = null,
    val horariosDisponibles: List<HorarioLibreDto> = emptyList(),
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

class ReservarViewModel : ViewModel() {
    private val _uiState = MutableStateFlow(ReservarUiState(cargandoDatos = true))
    val uiState: StateFlow<ReservarUiState> = _uiState
    private var disponibilidadJob: Job? = null

    init {
        cargarDatos()
    }

    fun cargarDatos() {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(cargandoDatos = true, errorCargaDatos = null)
            try {
                val (barberosResp, catalogoResp, estadosResp) = coroutineScope {
                    val barberos = async { ApiClient.barberoApi.getBarberos() }
                    val catalogo = async { ApiClient.catalogoItemApi.getCatalogoItems() }
                    val estados = async { ApiClient.estadosCitaApi.getEstadosCita() }
                    Triple(barberos.await(), catalogo.await(), estados.await())
                }

                val error = when {
                    !barberosResp.isSuccessful -> barberosResp.mensajeError("No se pudieron cargar los barberos")
                    !catalogoResp.isSuccessful -> catalogoResp.mensajeError("No se pudo cargar el catálogo")
                    !estadosResp.isSuccessful -> estadosResp.mensajeError("No se pudieron cargar los estados de cita")
                    barberosResp.body() == null -> "La API devolvió una lista de barberos vacía."
                    catalogoResp.body() == null -> "La API devolvió un catálogo vacío."
                    estadosResp.body() == null -> "La API no devolvió los estados de cita."
                    else -> null
                }
                if (error != null) {
                    _uiState.value = _uiState.value.copy(cargandoDatos = false, errorCargaDatos = error)
                    return@launch
                }

                val pendiente = estadosResp.body().orEmpty()
                    .firstOrNull { it.descripcion.equals("Pendiente", ignoreCase = true) }
                    ?.estadoCitaId
                _uiState.value = _uiState.value.copy(
                    barberos = barberosResp.body().orEmpty(),
                    servicios = catalogoResp.body().orEmpty().filter { it.esServicio && it.activo },
                    estadoPendienteId = pendiente,
                    cargandoDatos = false,
                    errorCargaDatos = if (pendiente == null) "La API no tiene configurado el estado Pendiente." else null
                )
            } catch (cancelacion: CancellationException) {
                throw cancelacion
            } catch (e: Exception) {
                _uiState.value = _uiState.value.copy(
                    cargandoDatos = false,
                    errorCargaDatos = e.localizedMessage ?: "Revisa tu conexión e inténtalo de nuevo."
                )
            }
        }
    }

    fun seleccionarBarbero(barbero: BarberoDto) {
        _uiState.value = _uiState.value.copy(barberoSeleccionado = barbero, horarioSeleccionado = null)
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
        actualizarDisponibilidad()
    }

    fun seleccionarFecha(fecha: LocalDate) {
        if (fecha.isBefore(LocalDate.now())) return
        _uiState.value = _uiState.value.copy(fechaSeleccionada = fecha, horarioSeleccionado = null)
        actualizarDisponibilidad()
    }

    fun seleccionarHorario(horario: HorarioLibreDto) {
        val disponibles = _uiState.value.horariosDisponibles
        if (disponibles.any { it.inicio == horario.inicio && it.fin == horario.fin }) {
            _uiState.value = _uiState.value.copy(horarioSeleccionado = horario, error = null)
        }
    }

    fun recargarDisponibilidad() = actualizarDisponibilidad()

    fun actualizarNotas(notas: String) {
        _uiState.value = _uiState.value.copy(notas = notas.take(500))
    }

    private fun actualizarDisponibilidad() {
        disponibilidadJob?.cancel()
        val state = _uiState.value
        val barbero = state.barberoSeleccionado
        val fecha = state.fechaSeleccionada
        val duracion = state.duracionTotalMin

        if (barbero == null || fecha == null || state.serviciosSeleccionados.isEmpty()) {
            _uiState.value = state.copy(
                horariosDisponibles = emptyList(),
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
                val response = ApiClient.barberoApi.getDisponibilidad(
                    id = barbero.barberoId,
                    fecha = fecha.toString(),
                    duracionMin = duracion
                )
                val actual = _uiState.value
                if (actual.barberoSeleccionado?.barberoId != barbero.barberoId ||
                    actual.fechaSeleccionada != fecha || actual.duracionTotalMin != duracion
                ) return@launch

                if (!response.isSuccessful) {
                    _uiState.value = actual.copy(
                        horariosDisponibles = emptyList(),
                        cargandoDisponibilidad = false,
                        errorDisponibilidad = response.mensajeError("No se pudo consultar la disponibilidad")
                    )
                    return@launch
                }

                val disponibilidad: DisponibilidadDto? = response.body()
                if (disponibilidad == null) {
                    _uiState.value = actual.copy(
                        cargandoDisponibilidad = false,
                        errorDisponibilidad = "La API no devolvió horarios disponibles."
                    )
                } else {
                    _uiState.value = actual.copy(
                        horariosDisponibles = disponibilidad.horarios,
                        cargandoDisponibilidad = false,
                        errorDisponibilidad = null
                    )
                }
            } catch (cancelacion: CancellationException) {
                throw cancelacion
            } catch (e: Exception) {
                val actual = _uiState.value
                if (actual.barberoSeleccionado?.barberoId == barbero.barberoId &&
                    actual.fechaSeleccionada == fecha && actual.duracionTotalMin == duracion
                ) {
                    _uiState.value = actual.copy(
                        horariosDisponibles = emptyList(),
                        cargandoDisponibilidad = false,
                        errorDisponibilidad = e.localizedMessage ?: "Revisa tu conexión e inténtalo de nuevo."
                    )
                }
            }
        }
    }

    fun crearCita() {
        val state = _uiState.value
        val clienteId = SesionCliente.clienteActual?.clienteId
        val barbero = state.barberoSeleccionado
        val fecha = state.fechaSeleccionada
        val horario = state.horarioSeleccionado
        val estadoPendiente = state.estadoPendienteId

        if (clienteId == null || barbero == null || fecha == null || horario == null ||
            state.serviciosSeleccionados.isEmpty() || estadoPendiente == null
        ) {
            _uiState.value = state.copy(error = "Completa todos los datos de la reserva e inténtalo de nuevo.")
            return
        }

        if (state.horariosDisponibles.none { it.inicio == horario.inicio && it.fin == horario.fin }) {
            _uiState.value = state.copy(horarioSeleccionado = null, error = "El horario ya no está disponible. Selecciona otro.")
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
            barberoId = barbero.barberoId,
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
                val response = ApiClient.citaApi.crearCita(cita)
                if (response.isSuccessful) {
                    _uiState.value = _uiState.value.copy(citaCreada = true, enviandoReserva = false)
                } else {
                    _uiState.value = _uiState.value.copy(
                        error = response.mensajeError("No se pudo crear la cita"),
                        enviandoReserva = false
                    )
                    if (response.code() == 400) actualizarDisponibilidad()
                }
            } catch (cancelacion: CancellationException) {
                throw cancelacion
            } catch (e: Exception) {
                _uiState.value = _uiState.value.copy(
                    error = e.localizedMessage ?: "Revisa tu conexión e inténtalo de nuevo.",
                    enviandoReserva = false
                )
            }
        }
    }
}
