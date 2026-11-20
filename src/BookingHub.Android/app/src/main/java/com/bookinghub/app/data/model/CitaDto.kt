package com.bookinghub.app.data.model

import java.math.BigDecimal

data class CitaDto(
    val citaId: Int = 0,
    val clienteId: Int = 0,
    val clienteNombre: String? = null,
    val clienteTelefono: String? = null,
    val barberoId: Int = 0,
    val barberoNombre: String? = null,
    val estadoCitaId: Int = 0,
    val estado: String? = null,
    val resenada: Boolean = false,
    val fechaInicio: String = "",
    val fechaFin: String = "",
    val total: BigDecimal = BigDecimal.ZERO,
    val notas: String? = null,
    val detalles: List<CitaDetalleDto>? = null
)

data class PaginaCitasDto(
    val items: List<CitaDto> = emptyList(),
    val pagina: Int = 1,
    val tamano: Int = 10,
    val tieneMas: Boolean = false
)

data class CitaDetalleDto(
    val citaDetalleId: Int = 0,
    val catalogoItemId: Int = 0,
    val nombre: String? = null,
    val cantidad: Int = 1,
    val precioUnitario: BigDecimal = BigDecimal.ZERO,
    val descuento: BigDecimal = BigDecimal.ZERO
)
