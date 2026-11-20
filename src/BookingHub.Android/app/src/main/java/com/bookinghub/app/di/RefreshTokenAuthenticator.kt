package com.bookinghub.app.di

import android.content.Context
import com.bookinghub.app.BuildConfig
import com.bookinghub.app.data.model.RefreshTokenRequest
import com.bookinghub.app.data.model.SesionDto
import com.bookinghub.app.util.SesionBarbero
import com.bookinghub.app.util.SesionCliente
import com.bookinghub.app.util.SesionExpiradaBus
import com.bookinghub.app.util.CitasCache
import com.google.gson.Gson
import dagger.hilt.android.qualifiers.ApplicationContext
import okhttp3.Authenticator
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.Response
import okhttp3.Route
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.RequestBody.Companion.toRequestBody
import java.io.IOException
import java.util.concurrent.TimeUnit
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class RefreshTokenAuthenticator @Inject constructor(
    @ApplicationContext private val context: Context,
    private val gson: Gson
) : Authenticator {
    private val lock = Any()
    private val refreshClient = OkHttpClient.Builder()
        .connectTimeout(15, TimeUnit.SECONDS)
        .readTimeout(15, TimeUnit.SECONDS)
        .writeTimeout(15, TimeUnit.SECONDS)
        .build()

    override fun authenticate(route: Route?, response: Response): Request? {
        if (numeroReintentos(response) >= 1 || response.request.header("Authorization").isNullOrBlank()) return null
        val path = response.request.url.encodedPath
        if (path.endsWith("/api/Auth/refresh") || path.endsWith("/api/Auth/login")) return null

        synchronized(lock) {
            val sesionBarbero = SesionBarbero.sesionActual
            val sesionCliente = SesionCliente.clienteActual
            val tokenActual = sesionBarbero?.token ?: sesionCliente?.token
            if (tokenActual.isNullOrBlank()) return null
            if (response.request.header("Authorization") != "Bearer $tokenActual") {
                return response.request.newBuilder().header("Authorization", "Bearer $tokenActual").build()
            }

            val refreshToken = sesionBarbero?.refreshToken ?: sesionCliente?.refreshToken
            if (refreshToken.isNullOrBlank()) {
                expirarSesion()
                return null
            }

            val json = gson.toJson(RefreshTokenRequest(refreshToken))
            val request = Request.Builder()
                .url(BuildConfig.API_BASE_URL + "api/Auth/refresh")
                .post(json.toRequestBody("application/json; charset=utf-8".toMediaType()))
                .build()

            return try {
                refreshClient.newCall(request).execute().use { refreshResponse ->
                    if (!refreshResponse.isSuccessful) {
                        expirarSesion()
                        return null
                    }
                    val sesion = gson.fromJson(refreshResponse.body.string(), SesionDto::class.java)
                    if (sesion == null || sesion.token.isBlank() || sesion.refreshToken.isNullOrBlank()) {
                        expirarSesion()
                        return null
                    }
                    val guardada = when {
                        sesion.rol.equals("Barbero", ignoreCase = true) -> SesionBarbero.guardar(context, sesion)
                        sesion.rol.equals("Cliente", ignoreCase = true) -> SesionCliente.reemplazarTokens(context, sesion)
                        else -> false
                    }
                    if (!guardada) {
                        expirarSesion()
                        return null
                    }
                    response.request.newBuilder().header("Authorization", "Bearer ${sesion.token}").build()
                }
            } catch (_: IOException) {
                // No borramos una sesión válida por un fallo de red temporal.
                null
            } catch (_: Exception) {
                expirarSesion()
                null
            }
        }
    }

    private fun expirarSesion() {
        val idBarbero = SesionBarbero.sesionActual?.usuarioId
        val idCliente = SesionCliente.clienteActual?.clienteId
        SesionBarbero.borrar(context)
        SesionCliente.borrarPerfil(context)
        if (idBarbero != null) CitasCache.borrarAgendaBarbero(context, idBarbero)
        if (idCliente != null) CitasCache.borrarCliente(context, idCliente)
        SesionExpiradaBus.avisar()
    }

    private fun numeroReintentos(response: Response): Int {
        var actual: Response? = response
        var cantidad = 0
        while (actual != null) {
            cantidad++
            actual = actual.priorResponse
        }
        return cantidad - 1
    }
}
