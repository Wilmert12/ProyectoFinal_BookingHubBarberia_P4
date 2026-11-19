package com.bookinghub.app.data.model

data class BarberoDto(
    val barberoId: Int = 0,
    val usuarioId: Int = 0,
    val nombre: String? = null,
    val apodo: String? = null,
    val colorHex: String = "#000000",
    val activo: Boolean = true,
    val cedula: String? = null
)
