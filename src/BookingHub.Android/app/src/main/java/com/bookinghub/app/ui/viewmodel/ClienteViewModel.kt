package com.bookinghub.app.ui.viewmodel

import android.app.Application
import android.util.Patterns
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.bookinghub.app.data.api.ApiClient
import com.bookinghub.app.data.api.mensajeError
import com.bookinghub.app.data.model.ClienteDto
import com.bookinghub.app.data.model.RecuperarNumeroDto
import com.bookinghub.app.util.SesionCliente
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch

data class RegistroUiState(
    val cargando: Boolean = false,
    val error: String? = null,
    val registroExitoso: Boolean = false
)

class ClienteViewModel(application: Application) : AndroidViewModel(application) {
    private val _uiState = MutableStateFlow(RegistroUiState())
    val uiState: StateFlow<RegistroUiState> = _uiState

    fun registrarCliente(nombre: String, telefono: String, email: String?) {
        val nombreLimpio = nombre.trim()
        val telefonoLimpio = telefono.trim()
        val emailLimpio = email?.trim()?.takeIf { it.isNotEmpty() }

        if (nombreLimpio.length < 2) {
            _uiState.value = RegistroUiState(error = "Ingresa un nombre de al menos 2 caracteres.")
            return
        }
        val digitosTelefono = telefonoLimpio.count(Char::isDigit)
        if (digitosTelefono !in 7..15) {
            _uiState.value = RegistroUiState(error = "Ingresa un teléfono válido.")
            return
        }
        if (emailLimpio != null && !Patterns.EMAIL_ADDRESS.matcher(emailLimpio).matches()) {
            _uiState.value = RegistroUiState(error = "Ingresa un correo válido o déjalo vacío.")
            return
        }

        viewModelScope.launch {
            _uiState.value = RegistroUiState(cargando = true)
            try {
                val cliente = ClienteDto(
                    nombre = nombreLimpio,
                    telefono = telefonoLimpio,
                    email = emailLimpio
                )
                val response = ApiClient.clienteApi.crearCliente(cliente)
                val clienteCreado = response.body()
                if (response.isSuccessful && clienteCreado != null) {
                    SesionCliente.guardarPerfil(getApplication(), clienteCreado)
                    _uiState.value = RegistroUiState(registroExitoso = true)
                } else {
                    _uiState.value = RegistroUiState(error = response.mensajeError("No se pudo registrar el cliente"))
                }
            } catch (cancelacion: CancellationException) {
                throw cancelacion
            } catch (e: Exception) {
                _uiState.value = RegistroUiState(error = e.localizedMessage ?: "Revisa tu conexión e inténtalo de nuevo.")
            }
        }
    }

    fun limpiarError() {
        _uiState.value = _uiState.value.copy(error = null)
    }

    /**
     * Cliente que ya existe (se registró en la web o en recepción): con su nombre y teléfono
     * la API devuelve su número, y se guarda el perfil sin crear otro cliente.
     */
    fun entrarComoCliente(nombre: String, telefono: String) {
        val nombreLimpio = nombre.trim()
        val telefonoLimpio = telefono.trim()
        if (nombreLimpio.length < 2 || telefonoLimpio.count(Char::isDigit) < 7) {
            _uiState.value = RegistroUiState(error = "Escribe tu nombre completo y un teléfono válido.")
            return
        }

        viewModelScope.launch {
            _uiState.value = RegistroUiState(cargando = true)
            try {
                val response = ApiClient.clienteApi.recuperarNumero(RecuperarNumeroDto(nombreLimpio, telefonoLimpio))
                val numero = response.body()
                if (response.isSuccessful && numero != null) {
                    SesionCliente.guardarPerfil(getApplication(), numero.clienteId, numero.nombre, telefonoLimpio)
                    _uiState.value = RegistroUiState(registroExitoso = true)
                } else {
                    _uiState.value = RegistroUiState(error = response.mensajeError("No encontramos un cliente con ese nombre y teléfono"))
                }
            } catch (cancelacion: CancellationException) {
                throw cancelacion
            } catch (e: Exception) {
                _uiState.value = RegistroUiState(error = e.localizedMessage ?: "Revisa tu conexión e inténtalo de nuevo.")
            }
        }
    }
}
