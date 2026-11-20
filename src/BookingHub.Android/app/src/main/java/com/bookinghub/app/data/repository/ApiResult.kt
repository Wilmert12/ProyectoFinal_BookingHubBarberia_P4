package com.bookinghub.app.data.repository

import com.google.gson.JsonParser
import kotlinx.coroutines.CancellationException
import retrofit2.Response
import java.io.IOException

/** Resultado de red sin excepciones HTTP filtradas hasta el ViewModel. */
sealed interface ApiResult<out T> {
    data class Success<T>(val data: T) : ApiResult<T>
    data object NetworkError : ApiResult<Nothing>
    data class HttpError(val statusCode: Int, val message: String) : ApiResult<Nothing>
    data object SessionExpired : ApiResult<Nothing>
    data class UnexpectedError(val message: String) : ApiResult<Nothing>
    data class Unsupported(val message: String) : ApiResult<Nothing>
}

suspend fun <T : Any> executeApiRequest(request: suspend () -> Response<T>): ApiResult<T> = try {
    val response = request()
    if (response.isSuccessful) {
        response.body()?.let { data -> ApiResult.Success(data) }
            ?: ApiResult.UnexpectedError("La API respondió sin contenido.")
    } else if (response.code() == 401) {
        ApiResult.SessionExpired
    } else {
        ApiResult.HttpError(
            statusCode = response.code(),
            message = response.errorBody()?.string()?.let(::extractApiMessage)
                ?.takeIf(String::isNotBlank)
                ?: "La solicitud falló (HTTP ${response.code()})."
        )
    }
} catch (cancelled: CancellationException) {
    throw cancelled
} catch (_: IOException) {
    ApiResult.NetworkError
} catch (error: Exception) {
    ApiResult.UnexpectedError(error.localizedMessage ?: "Ocurrió un error inesperado.")
}

suspend fun executeApiRequestNoContent(request: suspend () -> Response<Unit>): ApiResult<Unit> = try {
    val response = request()
    if (response.isSuccessful) ApiResult.Success(Unit)
    else if (response.code() == 401) ApiResult.SessionExpired
    else ApiResult.HttpError(
        statusCode = response.code(),
        message = response.errorBody()?.string()?.let(::extractApiMessage)
            ?.takeIf(String::isNotBlank) ?: "La solicitud falló (HTTP ${response.code()})."
    )
} catch (cancelled: CancellationException) {
    throw cancelled
} catch (_: IOException) {
    ApiResult.NetworkError
} catch (error: Exception) {
    ApiResult.UnexpectedError(error.localizedMessage ?: "Ocurrió un error inesperado.")
}

fun ApiResult<*>.mensajeParaUsuario(predeterminado: String): String = when (this) {
    is ApiResult.Success -> predeterminado
    ApiResult.NetworkError -> "Sin conexión. Revisa la red e inténtalo de nuevo."
    is ApiResult.HttpError -> message.takeIf(String::isNotBlank) ?: predeterminado
    ApiResult.SessionExpired -> "Tu sesión venció. Inicia sesión nuevamente."
    is ApiResult.UnexpectedError -> message.takeIf(String::isNotBlank) ?: predeterminado
    is ApiResult.Unsupported -> message
}

private fun extractApiMessage(rawBody: String): String? {
    val raw = rawBody.trim()
    if (raw.isEmpty()) return null

    val parsed = runCatching { JsonParser.parseString(raw) }.getOrNull()
    if (parsed == null || parsed.isJsonNull) return raw.take(240)
    if (parsed.isJsonPrimitive) return parsed.asJsonPrimitive.asString.take(240)
    if (!parsed.isJsonObject) return null

    return listOf("message", "error", "title", "detail")
        .firstNotNullOfOrNull { key ->
            parsed.asJsonObject.get(key)
                ?.takeIf { it.isJsonPrimitive }
                ?.asString
                ?.takeIf(String::isNotBlank)
        }
        ?.take(240)
}
