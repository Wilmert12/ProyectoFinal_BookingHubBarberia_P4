package com.bookinghub.app.data.model

/** Credenciales que acepta POST /api/Auth/login. */
data class LoginDto(
    val email: String,
    val password: String
)

/** Sesión JWT devuelta por la API al autenticar una cuenta de personal. */
data class SesionDto(
    val token: String,
    val expira: String,
    val usuarioId: Int,
    val nombre: String,
    val email: String,
    val rol: String
)
