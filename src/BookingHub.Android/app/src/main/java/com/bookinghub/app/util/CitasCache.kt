package com.bookinghub.app.util

import android.content.Context
import com.bookinghub.app.data.model.CitaDto
import com.google.gson.Gson
import com.google.gson.reflect.TypeToken

/** Guarda copias cifradas de agendas para consultar los últimos datos sin conexión. */
object CitasCache {
    private const val PREFERENCIAS = "citas_cache"
    private const val ALIAS_CLAVE = "bookinghub_citas_cache_aes"
    private const val PREFIJO_CLIENTE = "cliente_"
    private const val PREFIJO_BARBERO = "barbero_"
    private val gson = Gson()
    private val tipoLista = object : TypeToken<List<CitaDto>>() {}.type

    fun leerCliente(context: Context, clienteId: Int): List<CitaDto>? =
        leer(context, "$PREFIJO_CLIENTE$clienteId")

    fun guardarCliente(context: Context, clienteId: Int, citas: List<CitaDto>) {
        guardar(context, "$PREFIJO_CLIENTE$clienteId", citas)
    }

    fun leerBarbero(context: Context, usuarioId: Int, fecha: String): List<CitaDto>? =
        leer(context, "${PREFIJO_BARBERO}${usuarioId}_$fecha")

    fun guardarBarbero(context: Context, usuarioId: Int, fecha: String, citas: List<CitaDto>) {
        guardar(context, "${PREFIJO_BARBERO}${usuarioId}_$fecha", citas)
    }

    fun borrarAgendaBarbero(context: Context, usuarioId: Int) {
        val preferencias = preferencias(context)
        val clavesAgenda = preferencias.all.keys
            .filter { it.startsWith("${PREFIJO_BARBERO}${usuarioId}_") }
        preferencias.edit().apply {
            clavesAgenda.forEach { clave -> remove(clave) }
        }.apply()
    }

    private fun leer(context: Context, clave: String): List<CitaDto>? {
        val cifrado = preferencias(context).getString(clave, null) ?: return null
        return runCatching {
            val json = CifradoLocal.descifrar(cifrado, ALIAS_CLAVE)
            gson.fromJson<List<CitaDto>>(json, tipoLista)
        }.getOrNull() ?: run {
            preferencias(context).edit().remove(clave).apply()
            null
        }
    }

    private fun guardar(context: Context, clave: String, citas: List<CitaDto>) {
        runCatching {
            val cifrado = CifradoLocal.cifrar(gson.toJson(citas), ALIAS_CLAVE)
            preferencias(context).edit().putString(clave, cifrado).commit()
        }
    }

    private fun preferencias(context: Context) =
        context.getSharedPreferences(PREFERENCIAS, Context.MODE_PRIVATE)
}
