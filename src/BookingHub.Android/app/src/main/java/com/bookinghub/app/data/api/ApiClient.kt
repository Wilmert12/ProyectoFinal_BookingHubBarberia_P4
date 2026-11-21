package com.bookinghub.app.data.api

import com.bookinghub.app.BuildConfig
import com.bookinghub.app.util.SesionBarbero
import com.bookinghub.app.util.SesionCliente
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import java.util.concurrent.TimeUnit

object ApiClient {
    private val loggingInterceptor = HttpLoggingInterceptor().apply {
        level = if (BuildConfig.DEBUG) {
            HttpLoggingInterceptor.Level.BASIC
        } else {
            HttpLoggingInterceptor.Level.NONE
        }
    }

    private val httpClient = OkHttpClient.Builder()
        .addInterceptor { chain ->
            val request = chain.request()
            val token = SesionBarbero.sesionActual?.token
            val esLogin = request.url.encodedPath.endsWith("/api/Auth/login")
            val builder = request.newBuilder()
            if (!token.isNullOrBlank() && !esLogin) {
                builder.header("Authorization", "Bearer $token")
            }
            // Las citas del cliente se validan con su teléfono.
            val telefono = SesionCliente.clienteActual?.telefono
            if (!telefono.isNullOrBlank() && request.url.encodedPath.contains("/api/Citas")) {
                builder.header("X-Cliente-Telefono", telefono)
            }
            chain.proceed(builder.build())
        }
        .addInterceptor(loggingInterceptor)
        .connectTimeout(30, TimeUnit.SECONDS)
        .readTimeout(30, TimeUnit.SECONDS)
        .writeTimeout(30, TimeUnit.SECONDS)
        .build()

    private val retrofit: Retrofit = Retrofit.Builder()
        .baseUrl(BuildConfig.API_BASE_URL)
        .client(httpClient)
        .addConverterFactory(GsonConverterFactory.create())
        .build()

    val barberoApi: BarberoApi = retrofit.create(BarberoApi::class.java)
    val authApi: AuthApi = retrofit.create(AuthApi::class.java)
    val catalogoItemApi: CatalogoItemApi = retrofit.create(CatalogoItemApi::class.java)
    val citaApi: CitaApi = retrofit.create(CitaApi::class.java)
    val clienteApi: ClienteApi = retrofit.create(ClienteApi::class.java)
    val estadosCitaApi: EstadosCitaApi = retrofit.create(EstadosCitaApi::class.java)
}
