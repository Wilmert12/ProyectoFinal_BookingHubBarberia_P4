package com.bookinghub.app.ui.viewmodel

import android.content.Context
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import android.graphics.BitmapFactory
import androidx.compose.ui.graphics.ImageBitmap
import androidx.compose.ui.graphics.asImageBitmap
import com.bookinghub.app.data.model.BarberoDto
import com.bookinghub.app.data.repository.ApiResult
import com.bookinghub.app.data.repository.BarberoRepository
import com.bookinghub.app.data.repository.mensajeParaUsuario
import com.bookinghub.app.data.repository.ClienteRepository
import com.bookinghub.app.util.SesionCliente
import com.bookinghub.app.util.CitasCache
import dagger.hilt.android.qualifiers.ApplicationContext
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch
import java.time.LocalDate
import java.time.LocalDateTime
import java.time.format.DateTimeFormatter
import java.util.Locale
import javax.inject.Inject

data class BarberosUiState(
    val barberos: List<BarberoDto> = emptyList(),
    val favoritos: Set<Int> = emptySet(),
    val fotos: Map<Int, ImageBitmap> = emptyMap(),
    val proximosHorarios: Map<Int, String> = emptyMap(),
    val cargando: Boolean = false,
    val error: String? = null,
    val aviso: String? = null
)

@HiltViewModel
class BarberosViewModel @Inject constructor(
    private val barberoRepository: BarberoRepository,
    private val clienteRepository: ClienteRepository,
    @ApplicationContext private val context: Context
) : ViewModel() {
    private val _uiState = MutableStateFlow(BarberosUiState())
    val uiState: StateFlow<BarberosUiState> = _uiState

    init {
        cargarBarberos()
    }

    fun cargarBarberos() {
        val cache = CitasCache.leerBarberos(context)
        viewModelScope.launch {
            _uiState.value = BarberosUiState(barberos = cache.orEmpty(), cargando = true)
            when (val resultado = barberoRepository.listar()) {
                is ApiResult.Success -> {
                    CitasCache.guardarBarberos(context, resultado.data)
                    val favoritos = if (SesionCliente.hayPerfilGuardado) {
                        (clienteRepository.listarFavoritos() as? ApiResult.Success)?.data.orEmpty().map { it.barberoId }.toSet()
                    } else emptySet()
                    _uiState.value = BarberosUiState(barberos = resultado.data, favoritos = favoritos)
                    resultado.data.forEach { barbero ->
                        launch {
                            val proximo = consultarProximoHorario(barbero.barberoId)
                            if (proximo != null) {
                                _uiState.value = _uiState.value.copy(
                                    proximosHorarios = _uiState.value.proximosHorarios + (barbero.barberoId to proximo)
                                )
                            }
                        }
                        launch {
                            val bytes = barberoRepository.obtenerFoto(barbero.barberoId) ?: return@launch
                            val imagen = BitmapFactory.decodeByteArray(bytes, 0, bytes.size)?.asImageBitmap() ?: return@launch
                            _uiState.value = _uiState.value.copy(fotos = _uiState.value.fotos + (barbero.barberoId to imagen))
                        }
                    }
                }
                else -> _uiState.value = if (cache != null) BarberosUiState(
                    barberos = cache,
                    aviso = "Sin conexión. Se muestran los últimos barberos guardados."
                ) else BarberosUiState(error = resultado.mensajeParaUsuario("No se pudieron cargar los barberos."))
            }
        }
    }

    private suspend fun consultarProximoHorario(barberoId: Int): String? {
        val formato = DateTimeFormatter.ofPattern("EEE d MMM · HH:mm", Locale.getDefault())
        for (diasAdelante in 0..6) {
            val fecha = LocalDate.now().plusDays(diasAdelante.toLong())
            when (val resultado = barberoRepository.consultarDisponibilidad(barberoId, fecha.toString(), 30)) {
                is ApiResult.Success -> {
                    val hora = resultado.data.horarios.firstOrNull()?.inicio ?: continue
                    val fechaHora = runCatching {
                        LocalDateTime.parse(hora.take(19), DateTimeFormatter.ISO_LOCAL_DATE_TIME)
                    }.getOrNull() ?: continue
                    return fechaHora.format(formato).replaceFirstChar { it.uppercase(Locale.getDefault()) }
                }
                ApiResult.NetworkError -> return null
                else -> continue
            }
        }
        return null
    }

    fun alternarFavorito(barberoId: Int) {
        if (!SesionCliente.hayPerfilGuardado) {
            _uiState.value = _uiState.value.copy(aviso = "Inicia sesión como cliente para guardar favoritos.")
            return
        }
        viewModelScope.launch {
            val quitar = barberoId in _uiState.value.favoritos
            val resultado = if (quitar) clienteRepository.quitarFavorito(barberoId)
                else clienteRepository.agregarFavorito(barberoId)
            if (resultado is ApiResult.Success) {
                val actualizados = _uiState.value.favoritos.toMutableSet().apply {
                    if (quitar) remove(barberoId) else add(barberoId)
                }
                _uiState.value = _uiState.value.copy(favoritos = actualizados, aviso = null)
            } else {
                _uiState.value = _uiState.value.copy(aviso = resultado.mensajeParaUsuario("No se pudo guardar el favorito."))
            }
        }
    }
}
