package com.bookinghub.app.data.api

import com.google.gson.JsonParser
import retrofit2.Response

fun <T> Response<T>.mensajeError(predeterminado: String): String {
    val raw = runCatching { errorBody()?.string()?.trim().orEmpty() }.getOrDefault("")
    val message = if (raw.startsWith("{") || raw.startsWith("\"")) {
        runCatching {
            val json = JsonParser().parse(raw)
            when {
                json.isJsonPrimitive -> json.asJsonPrimitive.asString
                json.isJsonObject -> listOf("description", "message", "error", "title", "detail")
                    .firstNotNullOfOrNull { key ->
                        json.asJsonObject.get(key)?.let { value ->
                            if (value.isJsonPrimitive) value.asJsonPrimitive.asString else null
                        }
                    }
                else -> null
            }
        }.getOrNull()
    } else {
        raw
    }

    return message?.takeIf { it.isNotBlank() }?.take(240)
        ?: when (code()) {
            429 -> "Demasiados intentos seguidos. Espera un minuto y vuelve a intentar."
            else -> "$predeterminado (HTTP ${code()})"
        }
}
