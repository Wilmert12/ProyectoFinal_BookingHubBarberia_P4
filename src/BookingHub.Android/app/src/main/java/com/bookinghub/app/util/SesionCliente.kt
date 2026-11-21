package com.bookinghub.app.util

import android.content.Context
import com.bookinghub.app.data.model.ClienteDto
import com.google.gson.Gson

data class ClienteSesion(
    val clienteId: Int,
    val nombre: String,
    // La API pide el teléfono para mostrar y crear citas del cliente.
    // Puede faltar en perfiles guardados antes de este cambio.
    val telefono: String? = null
)

object SesionCliente {
    private const val PREFERENCIAS = "sesion_cliente"
    private const val CLAVE_CLIENTE = "cliente_cifrado"
    private const val ALIAS_CLAVE = "bookinghub_cliente_aes"
    private val gson = Gson()

    @Volatile
    var clienteActual: ClienteSesion? = null
        private set

    val hayPerfilGuardado: Boolean
        get() = clienteActual != null

    fun inicializar(context: Context) {
        val preferencias = preferencias(context)
        val contenido = preferencias.getString(CLAVE_CLIENTE, null) ?: return
        // Un perfil guardado sin teléfono (versiones anteriores) ya no sirve: la API lo pide
        // para ver y reservar citas. Se borra y la app pide entrar de nuevo con "Ya soy cliente".
        clienteActual = runCatching { descifrar(contenido) }.getOrNull()
            ?.takeIf { !it.telefono.isNullOrBlank() }
        if (clienteActual == null) {
            preferencias.edit().remove(CLAVE_CLIENTE).apply()
        }
    }

    fun guardarPerfil(context: Context, cliente: ClienteDto) =
        guardarPerfil(context, cliente.clienteId, cliente.nombre, cliente.telefono)

    fun guardarPerfil(context: Context, clienteId: Int, nombre: String, telefono: String) {
        val sesion = ClienteSesion(clienteId, nombre, telefono.trim())
        clienteActual = sesion
        runCatching {
            val cifrado = cifrar(gson.toJson(sesion))
            preferencias(context).edit().putString(CLAVE_CLIENTE, cifrado).commit()
        }
    }

    fun borrarPerfil(context: Context) {
        preferencias(context).edit().remove(CLAVE_CLIENTE).apply()
        clienteActual = null
    }

    private fun preferencias(context: Context) =
        context.getSharedPreferences(PREFERENCIAS, Context.MODE_PRIVATE)

    private fun cifrar(valor: String): String {
        return CifradoLocal.cifrar(valor, ALIAS_CLAVE)
    }

    private fun descifrar(valor: String): ClienteSesion? {
        val contenido = CifradoLocal.descifrar(valor, ALIAS_CLAVE)
        return gson.fromJson(contenido, ClienteSesion::class.java)
    }
}
