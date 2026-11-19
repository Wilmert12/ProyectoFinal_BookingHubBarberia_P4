package com.bookinghub.app.data.model

import java.math.BigDecimal

data class CatalogoItemDto(
    val catalogoItemId: Int = 0,
    val nombre: String = "",
    val descripcion: String? = null,
    val esServicio: Boolean = false,
    val precio: BigDecimal = BigDecimal.ZERO,
    val duracionMin: Int? = null,
    val activo: Boolean = true
)
