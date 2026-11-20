package com.bookinghub.app.data.model

data class ActualizarPerfilClienteRequest(val nombre: String, val telefono: String, val email: String?)

data class CrearResenaRequest(val citaId: Int, val puntaje: Int, val comentario: String?)

data class ResenaDto(
    val resenaId: Int = 0,
    val citaId: Int = 0,
    val barberoId: Int = 0,
    val barberoNombre: String = "",
    val puntaje: Int = 0,
    val comentario: String? = null,
    val fechaRegistro: String = ""
)

data class CrearBloqueoAgendaRequest(
    val fechaInicio: String,
    val fechaFin: String,
    val tipo: String = "Descanso",
    val descripcion: String? = null
)

data class BloqueoAgendaDto(
    val indisponibilidadId: Int = 0,
    val fechaInicio: String = "",
    val fechaFin: String = "",
    val tipo: String = "",
    val descripcion: String? = null
)
