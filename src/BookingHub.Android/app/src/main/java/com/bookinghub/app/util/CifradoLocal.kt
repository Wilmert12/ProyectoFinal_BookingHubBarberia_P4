package com.bookinghub.app.util

import android.security.keystore.KeyGenParameterSpec
import android.security.keystore.KeyProperties
import android.util.Base64
import java.security.KeyStore
import javax.crypto.Cipher
import javax.crypto.KeyGenerator
import javax.crypto.SecretKey
import javax.crypto.spec.GCMParameterSpec

/** Cifra información local con AES-GCM y claves que Android protege en su Keystore. */
object CifradoLocal {
    private const val ALMACEN_ANDROID = "AndroidKeyStore"
    private const val TAMANO_IV = 12

    fun cifrar(valor: String, alias: String): String {
        val cipher = Cipher.getInstance("AES/GCM/NoPadding")
        cipher.init(Cipher.ENCRYPT_MODE, obtenerClave(alias))
        val contenido = cipher.doFinal(valor.toByteArray(Charsets.UTF_8))
        return Base64.encodeToString(cipher.iv + contenido, Base64.NO_WRAP)
    }

    fun descifrar(valor: String, alias: String): String {
        val bytes = Base64.decode(valor, Base64.NO_WRAP)
        require(bytes.size > TAMANO_IV) { "Los datos guardados no son válidos." }
        val iv = bytes.copyOfRange(0, TAMANO_IV)
        val contenido = bytes.copyOfRange(TAMANO_IV, bytes.size)
        val cipher = Cipher.getInstance("AES/GCM/NoPadding")
        cipher.init(Cipher.DECRYPT_MODE, obtenerClave(alias), GCMParameterSpec(128, iv))
        return String(cipher.doFinal(contenido), Charsets.UTF_8)
    }

    private fun obtenerClave(alias: String): SecretKey {
        val keyStore = KeyStore.getInstance(ALMACEN_ANDROID).apply { load(null) }
        (keyStore.getKey(alias, null) as? SecretKey)?.let { return it }

        val keyGenerator = KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES, ALMACEN_ANDROID)
        keyGenerator.init(
            KeyGenParameterSpec.Builder(
                alias,
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
