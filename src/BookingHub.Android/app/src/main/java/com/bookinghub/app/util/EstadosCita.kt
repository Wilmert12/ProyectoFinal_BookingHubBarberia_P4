package com.bookinghub.app.util

import androidx.compose.ui.graphics.Color
import com.bookinghub.app.ui.theme.AmbarPendiente
import com.bookinghub.app.ui.theme.AzulConfirmada
import com.bookinghub.app.ui.theme.MoradoProceso
import com.bookinghub.app.ui.theme.RojoError
import com.bookinghub.app.ui.theme.TextoSuave
import com.bookinghub.app.ui.theme.VerdeExito

/** Nombres para mostrar y colores de los estados de cita, iguales a los de la web. */
object EstadosCita {
    fun texto(estado: String?): String = when (estado) {
        "EnProceso" -> "En proceso"
        "NoShow" -> "No asistió"
        null, "" -> "Agendada"
        else -> estado
    }

    fun color(estado: String?): Color = when (estado) {
        "Pendiente" -> AmbarPendiente
        "Confirmada" -> AzulConfirmada
        "EnProceso" -> MoradoProceso
        "Completada" -> VerdeExito
        "Cancelada", "NoShow" -> RojoError
        else -> TextoSuave
    }

    /** Cancelada o no-show: la hora quedó libre y la cita se muestra atenuada. */
    fun estaLibre(estado: String?) = estado == "Cancelada" || estado == "NoShow"

    /** Botones que tiene sentido darle al barbero según el estado actual (igual que la web). */
    fun siguientesParaBarbero(estado: String?): List<Pair<String, String>> = when (estado) {
        "Pendiente" -> listOf("Confirmada" to "Confirmar", "EnProceso" to "Empezar", "NoShow" to "No vino")
        "Confirmada" -> listOf("EnProceso" to "Empezar", "Completada" to "Completada", "NoShow" to "No vino")
        "EnProceso" -> listOf("Completada" to "Completada")
        else -> emptyList()
    }

    /** El cliente puede cancelar las pendientes o confirmadas que aún no empiezan. */
    fun cancelablePorCliente(estado: String?) = estado == "Pendiente" || estado == "Confirmada"
}
