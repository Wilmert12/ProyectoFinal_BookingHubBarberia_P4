package com.bookinghub.app.ui.viewmodel

import android.app.Application
import android.util.Patterns
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.bookinghub.app.data.api.ApiClient
import com.bookinghub.app.data.api.mensajeError
import com.bookinghub.app.data.model.LoginDto
import com.bookinghub.app.data.model.SesionDto
import com.bookinghub.app.util.SesionBarbero
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch

data class AccesoBarberoUiState(
    val cargando: Boolean = false,
    val error: String? = null,
    val sesionIniciada: Boolean = false
)

/** Autentica contra la API y solo guarda sesiones cuyo rol corresponde a Barbero. */
class AccesoBarberoViewModel(application: Application) : AndroidViewModel(application) {
    private val _uiState = MutableStateFlow(AccesoBarberoUiState())
    val uiState: StateFlow<AccesoBarberoUiState> = _uiState

    fun iniciarSesion(email: String, password: String) {
        val emailLimpio = email.trim()
        if (!Patterns.EMAIL_ADDRESS.matcher(emailLimpio).matches() || password.isBlank()) {
            _uiState.value = AccesoBarberoUiState(error = "Escribe un correo y una contraseña válidos.")
            return
        }

        viewModelScope.launch {
            _uiState.value = AccesoBarberoUiState(cargando = true)
            try {
                val response = ApiClient.authApi.iniciarSesion(LoginDto(emailLimpio, password))
                val sesion: SesionDto? = response.body()
                when {
                    !response.isSuccessful -> {
                        _uiState.value = AccesoBarberoUiState(
                            error = response.mensajeError("No se pudo iniciar sesión")
                        )
                    }
                    sesion == null -> {
                        _uiState.value = AccesoBarberoUiState(error = "La API no devolvió una sesión válida.")
                    }
                    !sesion.rol.equals("Barbero", ignoreCase = true) -> {
                        _uiState.value = AccesoBarberoUiState(
                            error = "Esta cuenta no tiene acceso al área de barberos."
                        )
                    }
                    !SesionBarbero.guardar(getApplication(), sesion) -> {
                        _uiState.value = AccesoBarberoUiState(
                            error = "No se pudo guardar la sesión segura en este dispositivo."
                        )
                    }
                    else -> _uiState.value = AccesoBarberoUiState(sesionIniciada = true)
                }
            } catch (cancelacion: CancellationException) {
                throw cancelacion
            } catch (e: Exception) {
                _uiState.value = AccesoBarberoUiState(
                    error = e.localizedMessage ?: "Revisa tu conexión e inténtalo de nuevo."
                )
            }
        }
    }
}
