package com.bookinghub.app.data.model

data class BarberoDto(
    val barberoId: Int = 0,
    val usuarioId: Int = 0,
    val nombre: String? = null,
    val apodo: String? = null,
    val colorHex: String = "#000000",
    val activo: Boolean = true,
    val cedula: String? = null,
    val calificacionPromedio: Double = 0.0,
    val totalResenas: Int = 0,
    val especialidades: String? = null
)
