package com.bookinghub.app.data.model

data class ClienteDto(
    val clienteId: Int = 0,
    val nombre: String = "",
    val telefono: String = "",
    val email: String? = null,
    val activo: Boolean = true
)
