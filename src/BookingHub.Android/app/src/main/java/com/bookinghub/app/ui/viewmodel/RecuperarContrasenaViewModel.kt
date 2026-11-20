package com.bookinghub.app.ui.viewmodel

import android.util.Patterns
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.bookinghub.app.data.model.RestablecerContrasenaRequest
import com.bookinghub.app.data.model.SolicitarRestablecimientoRequest
import com.bookinghub.app.data.repository.ApiResult
import com.bookinghub.app.data.repository.AuthRepository
import com.bookinghub.app.data.repository.mensajeParaUsuario
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

data class RecuperarContrasenaUiState(
    val cargando: Boolean = false,
    val codigoSolicitado: Boolean = false,
    val mensaje: String? = null,
    val error: String? = null,
    val contrasenaCambiada: Boolean = false
)

@HiltViewModel
class RecuperarContrasenaViewModel @Inject constructor(
    private val authRepository: AuthRepository
) : ViewModel() {
    private val _uiState = MutableStateFlow(RecuperarContrasenaUiState())
    val uiState: StateFlow<RecuperarContrasenaUiState> = _uiState

    fun solicitar(email: String, telefono: String) {
        val correo = email.trim()
        val digits = telefono.filter(Char::isDigit)
        val national = if (digits.length == 11 && digits.startsWith("1")) digits.drop(1) else digits
        if (!Patterns.EMAIL_ADDRESS.matcher(correo).matches() || national.length != 10 || national.take(3) !in setOf("809", "829", "849")) {
            _uiState.value = RecuperarContrasenaUiState(error = "Revisa el correo y el teléfono dominicano.")
            return
        }
        viewModelScope.launch {
            _uiState.value = RecuperarContrasenaUiState(cargando = true)
            when (val result = authRepository.solicitarRestablecimiento(SolicitarRestablecimientoRequest(correo, telefono.trim()))) {
                is ApiResult.Success -> _uiState.value = RecuperarContrasenaUiState(
                    codigoSolicitado = true,
                    mensaje = result.data["mensaje"] ?: "Si los datos coinciden, recibirás un código por correo."
                )
                else -> _uiState.value = RecuperarContrasenaUiState(error = result.mensajeParaUsuario("No se pudo solicitar el restablecimiento."))
            }
        }
    }

    fun restablecer(token: String, nuevaContrasena: String) {
        if (token.isBlank() || nuevaContrasena.length < 8 || nuevaContrasena.length > 128 ||
            nuevaContrasena.none(Char::isUpperCase) || nuevaContrasena.none(Char::isLowerCase) ||
            nuevaContrasena.none(Char::isDigit)
        ) {
            _uiState.value = _uiState.value.copy(error = "El código o la contraseña no son válidos. Usa 8 caracteres, mayúscula, minúscula y número.")
            return
        }
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(cargando = true, error = null, mensaje = null)
            when (val result = authRepository.restablecerContrasena(RestablecerContrasenaRequest(token.trim(), nuevaContrasena))) {
                is ApiResult.Success -> _uiState.value = _uiState.value.copy(
                    cargando = false, contrasenaCambiada = true,
                    mensaje = "Contraseña actualizada. Ya puedes iniciar sesión."
                )
                else -> _uiState.value = _uiState.value.copy(
                    cargando = false,
                    error = result.mensajeParaUsuario("No se pudo cambiar la contraseña.")
                )
            }
        }
    }
}
