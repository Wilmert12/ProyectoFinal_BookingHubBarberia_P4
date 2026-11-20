package com.bookinghub.app.ui.viewmodel

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.bookinghub.app.data.model.CambiarContrasenaRequest
import com.bookinghub.app.data.repository.ApiResult
import com.bookinghub.app.data.repository.AuthRepository
import com.bookinghub.app.data.repository.mensajeParaUsuario
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

data class CambiarContrasenaUiState(
    val cargando: Boolean = false,
    val error: String? = null,
    val cambiado: Boolean = false
)

@HiltViewModel
class CambiarContrasenaViewModel @Inject constructor(
    private val authRepository: AuthRepository
) : ViewModel() {
    private val _uiState = MutableStateFlow(CambiarContrasenaUiState())
    val uiState: StateFlow<CambiarContrasenaUiState> = _uiState

    fun cambiar(actual: String, nueva: String, repetir: String) {
        if (actual.isBlank() || nueva.length < 8 || nueva.length > 128 || nueva != repetir ||
            nueva.none(Char::isUpperCase) || nueva.none(Char::isLowerCase) || nueva.none(Char::isDigit)
        ) {
            _uiState.value = CambiarContrasenaUiState(error = "Verifica tu contraseña actual y usa una nueva con 8 caracteres, mayúscula, minúscula y número.")
            return
        }
        viewModelScope.launch {
            _uiState.value = CambiarContrasenaUiState(cargando = true)
            when (val result = authRepository.cambiarContrasena(CambiarContrasenaRequest(actual, nueva))) {
                is ApiResult.Success -> _uiState.value = CambiarContrasenaUiState(cambiado = true)
                else -> _uiState.value = CambiarContrasenaUiState(
                    error = result.mensajeParaUsuario("No se pudo cambiar la contraseña.")
                )
            }
        }
    }
}
