package com.bookinghub.app.data.model

data class DisponibilidadDto(
    val barberoId: Int = 0,
    val fecha: String = "",
    val duracionMin: Int = 0,
    val horarios: List<HorarioLibreDto> = emptyList()
)

data class HorarioLibreDto(
    val inicio: String = "",
    val fin: String = ""
)
