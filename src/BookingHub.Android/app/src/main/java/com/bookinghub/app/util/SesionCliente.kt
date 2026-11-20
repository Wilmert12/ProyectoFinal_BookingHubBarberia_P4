package com.bookinghub.app.util

import android.content.Context
import com.bookinghub.app.data.model.SesionDto
import com.google.gson.Gson
import java.time.Instant

data class ClienteSesion(
    val clienteId: Int,
    val nombre: String,
    val email: String = "",
    val token: String = "",
    val refreshToken: String? = null,
    val expira: String = ""
)

/** Guarda con Android Keystore únicamente la sesión autenticada del cliente. */
object SesionCliente {
    private const val PREFERENCIAS = "sesion_cliente"
    private const val CLAVE_SESION = "sesion_cifrada"
    private const val CLAVE_PERFIL_ANTERIOR = "cliente_cifrado"
    private const val ALIAS_CLAVE = "bookinghub_cliente_aes"
    private val gson = Gson()

    @Volatile
    var clienteActual: ClienteSesion? = null
        private set

    val hayPerfilGuardado: Boolean
        get() = sesionValida(clienteActual)

    fun inicializar(context: Context) {
        preferencias(context).edit().remove(CLAVE_PERFIL_ANTERIOR).apply()
        val cifrado = preferencias(context).getString(CLAVE_SESION, null) ?: return
        val sesion = runCatching {
            gson.fromJson(CifradoLocal.descifrar(cifrado, ALIAS_CLAVE), ClienteSesion::class.java)
        }.getOrNull()
        if (sesionValida(sesion)) {
            clienteActual = sesion
        } else {
            borrarPerfil(context)
        }
    }

    fun guardar(context: Context, sesion: SesionDto): Boolean {
        val clienteId = sesion.clienteId ?: return false
        if (!sesion.rol.equals("Cliente", ignoreCase = true) || sesion.token.isBlank()) return false
        val perfil = ClienteSesion(
            clienteId = clienteId,
            nombre = sesion.nombre,
            email = sesion.email,
            token = sesion.token,
            refreshToken = sesion.refreshToken,
            expira = sesion.expira
        )
        return runCatching {
            val cifrado = CifradoLocal.cifrar(gson.toJson(perfil), ALIAS_CLAVE)
            val guardado = preferencias(context).edit().putString(CLAVE_SESION, cifrado).commit()
            if (guardado) clienteActual = perfil
            guardado
        }.getOrDefault(false)
    }

    fun reemplazarTokens(context: Context, sesion: SesionDto): Boolean = guardar(context, sesion)

    fun actualizarDatos(context: Context, nombre: String, email: String?): Boolean {
        val actual = clienteActual ?: return false
        val perfil = actual.copy(nombre = nombre, email = email.orEmpty())
        return runCatching {
            val cifrado = CifradoLocal.cifrar(gson.toJson(perfil), ALIAS_CLAVE)
            val guardado = preferencias(context).edit().putString(CLAVE_SESION, cifrado).commit()
            if (guardado) clienteActual = perfil
            guardado
        }.getOrDefault(false)
    }

    fun borrarPerfil(context: Context) {
        preferencias(context).edit().remove(CLAVE_SESION).remove(CLAVE_PERFIL_ANTERIOR).commit()
        clienteActual = null
    }

    private fun sesionValida(sesion: ClienteSesion?): Boolean {
        if (sesion == null || sesion.clienteId <= 0 || sesion.token.isBlank()) return false
        if (!sesion.refreshToken.isNullOrBlank()) return true
        return runCatching { Instant.parse(sesion.expira).isAfter(Instant.now()) }.getOrDefault(false)
    }

    private fun preferencias(context: Context) =
        context.getSharedPreferences(PREFERENCIAS, Context.MODE_PRIVATE)
}
