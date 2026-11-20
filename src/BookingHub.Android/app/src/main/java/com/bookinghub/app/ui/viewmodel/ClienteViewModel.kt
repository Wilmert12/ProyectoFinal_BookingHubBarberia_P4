package com.bookinghub.app.ui.viewmodel

import android.content.Context
import android.util.Patterns
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.bookinghub.app.data.model.RegistroClienteRequest
import com.bookinghub.app.data.repository.ApiResult
import com.bookinghub.app.data.repository.AuthRepository
import com.bookinghub.app.data.repository.mensajeParaUsuario
import com.bookinghub.app.util.SesionCliente
import dagger.hilt.android.lifecycle.HiltViewModel
import dagger.hilt.android.qualifiers.ApplicationContext
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

data class RegistroUiState(
    val cargando: Boolean = false,
    val error: String? = null,
    val registroExitoso: Boolean = false
)

@HiltViewModel
class ClienteViewModel @Inject constructor(
    private val authRepository: AuthRepository,
    private val sesionCliente: SesionCliente,
    @ApplicationContext private val context: Context
) : ViewModel() {
    private val _uiState = MutableStateFlow(RegistroUiState())
    val uiState: StateFlow<RegistroUiState> = _uiState

    fun registrarCliente(nombre: String, telefono: String, email: String, password: String) {
        val nombreLimpio = nombre.trim()
        val telefonoLimpio = telefono.trim()
        val emailLimpio = email.trim()
        val passwordLimpia = password

        if (nombreLimpio.length < 2) {
            _uiState.value = RegistroUiState(error = "Ingresa un nombre de al menos 2 caracteres.")
            return
        }
        val digitosTelefono = telefonoLimpio.filter(Char::isDigit)
        val telefonoDominicano = if (digitosTelefono.length == 11 && digitosTelefono.startsWith("1")) {
            digitosTelefono.drop(1)
        } else digitosTelefono
        if (telefonoDominicano.length != 10 || telefonoDominicano.take(3) !in setOf("809", "829", "849")) {
            _uiState.value = RegistroUiState(error = "Ingresa un teléfono dominicano con código 809, 829 o 849.")
            return
        }
        if (!Patterns.EMAIL_ADDRESS.matcher(emailLimpio).matches()) {
            _uiState.value = RegistroUiState(error = "Ingresa un correo electrónico válido.")
            return
        }
        if (passwordLimpia.length < 8 || passwordLimpia.length > 128 ||
            passwordLimpia.none(Char::isUpperCase) || passwordLimpia.none(Char::isLowerCase) ||
            passwordLimpia.none(Char::isDigit)
        ) {
            _uiState.value = RegistroUiState(error = "Usa 8 caracteres o más, con mayúscula, minúscula y número.")
            return
        }

        viewModelScope.launch {
            _uiState.value = RegistroUiState(cargando = true)
            try {
                val solicitud = RegistroClienteRequest(
                    nombre = nombreLimpio,
                    telefono = telefonoLimpio,
                    email = emailLimpio,
                    password = passwordLimpia
                )
                when (val resultado = authRepository.registrarCliente(solicitud)) {
                    is ApiResult.Success -> {
                        if (sesionCliente.guardar(context, resultado.data)) {
                            _uiState.value = RegistroUiState(registroExitoso = true)
                        } else {
                            _uiState.value = RegistroUiState(error = "No se pudo guardar la sesión segura en el dispositivo.")
                        }
                    }
                    else -> _uiState.value = RegistroUiState(
                        error = resultado.mensajeParaUsuario("No se pudo registrar el cliente.")
                    )
                }
            } catch (cancelacion: CancellationException) {
                throw cancelacion
            } catch (error: Exception) {
                _uiState.value = RegistroUiState(
                    error = error.localizedMessage ?: "Revisa tu conexión e inténtalo de nuevo."
                )
            }
        }
    }
}
