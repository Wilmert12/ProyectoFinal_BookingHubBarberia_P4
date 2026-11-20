package com.bookinghub.app.data.repository

import javax.inject.Inject
import javax.inject.Singleton

/** Punto de integración explícito; el backend aún no publica registro de dispositivos. */
@Singleton
class NotificacionRepository @Inject constructor() {
    suspend fun registrarToken(token: String): ApiResult<Unit> = when {
        token.isBlank() -> ApiResult.UnexpectedError("El token de notificación está vacío.")
        else -> ApiResult.Unsupported("La API todavía no permite registrar dispositivos para notificaciones.")
    }
}
