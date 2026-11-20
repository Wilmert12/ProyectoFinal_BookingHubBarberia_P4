package com.bookinghub.app.ui.viewmodel

import android.content.Context
import android.util.Patterns
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.bookinghub.app.data.model.LoginDto
import com.bookinghub.app.data.repository.ApiResult
import com.bookinghub.app.data.repository.AuthRepository
import com.bookinghub.app.data.repository.mensajeParaUsuario
import com.bookinghub.app.util.SesionBarbero
import com.bookinghub.app.util.SesionCliente
import dagger.hilt.android.lifecycle.HiltViewModel
import dagger.hilt.android.qualifiers.ApplicationContext
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

data class AccesoBarberoUiState(
    val cargando: Boolean = false,
    val error: String? = null,
    val sesionIniciada: Boolean = false,
    val rol: String? = null
)

/** Autentica contra la API y solo guarda sesiones cuyo rol corresponde a Barbero. */
@HiltViewModel
class AccesoBarberoViewModel @Inject constructor(
    private val authRepository: AuthRepository,
    private val sesionBarbero: SesionBarbero,
    private val sesionCliente: SesionCliente,
    @ApplicationContext private val context: Context
) : ViewModel() {
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
            when (val resultado = authRepository.iniciarSesion(LoginDto(emailLimpio, password))) {
                is ApiResult.Success -> {
                    val sesion = resultado.data
                    when {
                        sesion.rol.equals("Cliente", ignoreCase = true) && sesionCliente.guardar(context, sesion) -> {
                            sesionBarbero.borrar(context)
                            _uiState.value = AccesoBarberoUiState(sesionIniciada = true, rol = "Cliente")
                        }
                        sesion.rol.equals("Barbero", ignoreCase = true) && sesionBarbero.guardar(context, sesion) -> {
                            sesionCliente.borrarPerfil(context)
                            _uiState.value = AccesoBarberoUiState(sesionIniciada = true, rol = "Barbero")
                        }
                        sesion.rol.equals("Cliente", ignoreCase = true) || sesion.rol.equals("Barbero", ignoreCase = true) -> {
                            _uiState.value = AccesoBarberoUiState(error = "No se pudo guardar la sesión segura en este dispositivo.")
                        }
                        else -> _uiState.value = AccesoBarberoUiState(
                            error = "Esta cuenta no tiene acceso desde la aplicación móvil."
                        )
                    }
                }
                else -> _uiState.value = AccesoBarberoUiState(
                    error = resultado.mensajeParaUsuario("No se pudo iniciar sesión.")
                )
            }
        }
    }
}
