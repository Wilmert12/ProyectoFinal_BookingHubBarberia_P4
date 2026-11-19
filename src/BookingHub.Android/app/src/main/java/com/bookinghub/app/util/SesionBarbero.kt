package com.bookinghub.app.util

import android.content.Context
import com.bookinghub.app.data.model.SesionDto
import com.google.gson.Gson
import java.time.Instant

/** Conserva de forma cifrada el token y el perfil autenticado del barbero. */
object SesionBarbero {
    private const val PREFERENCIAS = "sesion_barbero"
    private const val CLAVE_SESION = "sesion_cifrada"
    private const val ALIAS_CLAVE = "bookinghub_barbero_aes"
    private val gson = Gson()

    @Volatile
    var sesionActual: SesionDto? = null
        private set

    val haySesion: Boolean
        get() = sesionActual != null

    fun inicializar(context: Context) {
        val preferencias = preferencias(context)
        val cifrado = preferencias.getString(CLAVE_SESION, null) ?: return
        val sesion = runCatching {
            gson.fromJson(CifradoLocal.descifrar(cifrado, ALIAS_CLAVE), SesionDto::class.java)
        }.getOrNull()

        if (sesion != null && sesion.token.isNotBlank() && sesion.rol.equals("Barbero", ignoreCase = true) && sigueVigente(sesion)) {
            sesionActual = sesion
        } else {
            preferencias.edit().remove(CLAVE_SESION).apply()
            sesionActual = null
        }
    }

    /** Escribe el JWT cifrado antes de permitir que la navegación abra la agenda. */
    fun guardar(context: Context, sesion: SesionDto): Boolean = runCatching {
        val cifrado = CifradoLocal.cifrar(gson.toJson(sesion), ALIAS_CLAVE)
        val guardado = preferencias(context).edit().putString(CLAVE_SESION, cifrado).commit()
        if (guardado) sesionActual = sesion
        guardado
    }.getOrDefault(false)

    fun borrar(context: Context) {
        preferencias(context).edit().remove(CLAVE_SESION).commit()
        sesionActual = null
    }

    private fun sigueVigente(sesion: SesionDto): Boolean = runCatching {
        Instant.parse(sesion.expira).isAfter(Instant.now())
    }.getOrDefault(false)

    private fun preferencias(context: Context) =
        context.getSharedPreferences(PREFERENCIAS, Context.MODE_PRIVATE)
}
