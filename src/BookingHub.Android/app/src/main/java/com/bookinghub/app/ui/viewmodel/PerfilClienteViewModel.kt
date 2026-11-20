package com.bookinghub.app.ui.viewmodel

import android.content.Context
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.bookinghub.app.data.model.ActualizarPerfilClienteRequest
import com.bookinghub.app.data.model.ClienteDto
import com.bookinghub.app.data.repository.ApiResult
import com.bookinghub.app.data.repository.ClienteRepository
import com.bookinghub.app.data.repository.mensajeParaUsuario
import com.bookinghub.app.util.SesionCliente
import dagger.hilt.android.lifecycle.HiltViewModel
import dagger.hilt.android.qualifiers.ApplicationContext
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

data class PerfilClienteUiState(
    val perfil: ClienteDto? = null,
    val cargando: Boolean = false,
    val guardando: Boolean = false,
    val subiendoFoto: Boolean = false,
    val error: String? = null,
    val mensaje: String? = null
)

@HiltViewModel
class PerfilClienteViewModel @Inject constructor(
    private val clienteRepository: ClienteRepository,
    @ApplicationContext private val context: Context
) : ViewModel() {
    private val _uiState = MutableStateFlow(PerfilClienteUiState())
    val uiState: StateFlow<PerfilClienteUiState> = _uiState

    init { cargar() }

    fun cargar() {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(cargando = true, error = null, mensaje = null)
            when (val result = clienteRepository.obtenerPerfil()) {
                is ApiResult.Success -> _uiState.value = _uiState.value.copy(perfil = result.data, cargando = false)
                else -> _uiState.value = _uiState.value.copy(cargando = false,
                    error = result.mensajeParaUsuario("No se pudo cargar tu perfil."))
            }
        }
    }

    fun guardar(nombre: String, telefono: String, email: String) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(guardando = true, error = null, mensaje = null)
            when (val result = clienteRepository.actualizarPerfil(ActualizarPerfilClienteRequest(nombre, telefono, email.ifBlank { null }))) {
                is ApiResult.Success -> {
                    SesionCliente.actualizarDatos(context, result.data.nombre, result.data.email)
                    _uiState.value = _uiState.value.copy(perfil = result.data, guardando = false, mensaje = "Perfil actualizado.")
                }
                else -> _uiState.value = _uiState.value.copy(guardando = false,
                    error = result.mensajeParaUsuario("No se pudo actualizar el perfil."))
            }
        }
    }

    fun subirFoto(bytes: ByteArray) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(subiendoFoto = true, error = null, mensaje = null)
            when (val result = clienteRepository.subirFoto(bytes)) {
                is ApiResult.Success -> _uiState.value = _uiState.value.copy(subiendoFoto = false, mensaje = "Foto guardada en tu perfil.")
                else -> _uiState.value = _uiState.value.copy(subiendoFoto = false,
                    error = result.mensajeParaUsuario("No se pudo guardar la foto."))
            }
        }
    }
}
