package com.bookinghub.app.data.model

/** Para entrar con un número de cliente que ya existe (registrado en la web o en recepción). */
data class RecuperarNumeroDto(
    val nombre: String,
    val telefono: String
)

data class NumeroClienteDto(
    val clienteId: Int = 0,
    val nombre: String = ""
)

/** El barbero marca una de sus citas (Confirmada, EnProceso, Completada o NoShow). */
data class CambioEstadoDto(
    val estadoCitaId: Int
)
