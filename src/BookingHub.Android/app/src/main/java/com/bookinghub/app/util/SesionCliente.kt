package com.bookinghub.app.util

import android.content.Context
import android.security.keystore.KeyGenParameterSpec
import android.security.keystore.KeyProperties
import android.util.Base64
import com.bookinghub.app.data.model.ClienteDto
import com.google.gson.Gson
import java.security.KeyStore
import javax.crypto.Cipher
import javax.crypto.KeyGenerator
import javax.crypto.SecretKey
import javax.crypto.spec.GCMParameterSpec

data class ClienteSesion(
    val clienteId: Int,
    val nombre: String
)

object SesionCliente {
    private const val PREFERENCIAS = "sesion_cliente"
    private const val CLAVE_CLIENTE = "cliente_cifrado"
    private const val ALIAS_CLAVE = "bookinghub_cliente_aes"
    private const val ALMACEN_ANDROID = "AndroidKeyStore"
    private const val TAMANO_IV = 12
    private val gson = Gson()

    @Volatile
    var clienteActual: ClienteSesion? = null
        private set

    val hayPerfilGuardado: Boolean
        get() = clienteActual != null

    fun inicializar(context: Context) {
        val preferencias = preferencias(context)
        val contenido = preferencias.getString(CLAVE_CLIENTE, null) ?: return
        clienteActual = runCatching { descifrar(contenido) }.getOrNull()
        if (clienteActual == null) {
            preferencias.edit().remove(CLAVE_CLIENTE).apply()
        }
    }

    fun guardarPerfil(context: Context, cliente: ClienteDto) {
        val sesion = ClienteSesion(cliente.clienteId, cliente.nombre)
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
        val cipher = Cipher.getInstance("AES/GCM/NoPadding")
        cipher.init(Cipher.ENCRYPT_MODE, obtenerClave())
        val iv = cipher.iv
        val contenido = cipher.doFinal(valor.toByteArray(Charsets.UTF_8))
        return Base64.encodeToString(iv + contenido, Base64.NO_WRAP)
    }

    private fun descifrar(valor: String): ClienteSesion? {
        val bytes = Base64.decode(valor, Base64.NO_WRAP)
        require(bytes.size > TAMANO_IV) { "La sesión guardada no es válida." }
        val iv = bytes.copyOfRange(0, TAMANO_IV)
        val contenido = bytes.copyOfRange(TAMANO_IV, bytes.size)
        val cipher = Cipher.getInstance("AES/GCM/NoPadding")
        cipher.init(Cipher.DECRYPT_MODE, obtenerClave(), GCMParameterSpec(128, iv))
        return gson.fromJson(String(cipher.doFinal(contenido), Charsets.UTF_8), ClienteSesion::class.java)
    }

    private fun obtenerClave(): SecretKey {
        val keyStore = KeyStore.getInstance(ALMACEN_ANDROID).apply { load(null) }
        (keyStore.getKey(ALIAS_CLAVE, null) as? SecretKey)?.let { return it }

        val keyGenerator = KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES, ALMACEN_ANDROID)
        keyGenerator.init(
            KeyGenParameterSpec.Builder(
                ALIAS_CLAVE,
                KeyProperties.PURPOSE_ENCRYPT or KeyProperties.PURPOSE_DECRYPT
            )
                .setBlockModes(KeyProperties.BLOCK_MODE_GCM)
                .setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE)
                .setRandomizedEncryptionRequired(true)
                .build()
        )
        return keyGenerator.generateKey()
    }
}
