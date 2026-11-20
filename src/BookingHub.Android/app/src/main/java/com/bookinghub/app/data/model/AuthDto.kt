package com.bookinghub.app.data.model

/** Credenciales que acepta POST /api/Auth/login. */
data class LoginDto(
    val email: String,
    val password: String
)

/** Sesión JWT compartida por clientes y personal; el rol determina el área de la app. */
data class SesionDto(
    val token: String,
    val expira: String,
    val usuarioId: Int = 0,
    val nombre: String,
    val email: String,
    val rol: String,
    val refreshToken: String? = null,
    val clienteId: Int? = null
)

data class RegistroClienteRequest(
    val nombre: String,
    val telefono: String,
    val email: String,
    val password: String
)

data class RefreshTokenRequest(val refreshToken: String)

data class SolicitarRestablecimientoRequest(val email: String, val telefono: String)

data class RestablecerContrasenaRequest(val token: String, val nuevaContrasena: String)

data class CambiarContrasenaRequest(val contrasenaActual: String, val nuevaContrasena: String)
