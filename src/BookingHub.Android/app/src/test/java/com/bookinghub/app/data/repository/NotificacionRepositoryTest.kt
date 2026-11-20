package com.bookinghub.app.data.repository

import kotlinx.coroutines.test.runTest
import org.junit.Assert.assertEquals
import org.junit.Test

class NotificacionRepositoryTest {
    private val repository = NotificacionRepository()

    @Test
    fun blankTokenIsRejected() = runTest {
        assertEquals(ApiResult.UnexpectedError("El token de notificación está vacío."), repository.registrarToken(" "))
    }

    @Test
    fun nonblankTokenReportsTheMissingApiCapability() = runTest {
        assertEquals(
            ApiResult.Unsupported("La API todavía no permite registrar dispositivos para notificaciones."),
            repository.registrarToken("fcm-token")
        )
    }
}
