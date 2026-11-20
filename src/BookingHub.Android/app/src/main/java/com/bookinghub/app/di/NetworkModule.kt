package com.bookinghub.app.di

import com.bookinghub.app.BuildConfig
import com.bookinghub.app.util.SesionBarbero
import com.bookinghub.app.util.SesionCliente
import com.google.gson.Gson
import com.google.gson.GsonBuilder
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import java.util.concurrent.TimeUnit
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
object NetworkModule {
    @Provides
    @Singleton
    fun provideGson(): Gson = GsonBuilder().create()

    @Provides
    @Singleton
    fun provideLoggingInterceptor(): HttpLoggingInterceptor = HttpLoggingInterceptor().apply {
        level = if (BuildConfig.DEBUG) {
            HttpLoggingInterceptor.Level.BASIC
        } else {
            HttpLoggingInterceptor.Level.NONE
        }
    }

    @Provides
    @Singleton
    fun provideOkHttpClient(
        sesionBarbero: SesionBarbero,
        sesionCliente: SesionCliente,
        authenticator: RefreshTokenAuthenticator,
        loggingInterceptor: HttpLoggingInterceptor
    ): OkHttpClient = OkHttpClient.Builder()
        .addInterceptor { chain ->
            val request = chain.request()
            val token = sesionBarbero.sesionActual?.token ?: sesionCliente.clienteActual?.token
            val rutaPublica = request.url.encodedPath.let { path ->
                path.endsWith("/api/Auth/login") || path.endsWith("/api/Auth/registro-cliente") ||
                    path.endsWith("/api/Auth/refresh") || path.endsWith("/api/Auth/olvide-contrasena") ||
                    path.endsWith("/api/Auth/restablecer-contrasena")
            }
            val requestAutenticado = if (token.isNullOrBlank() || rutaPublica) {
                request
            } else {
                request.newBuilder().header("Authorization", "Bearer $token").build()
            }
            chain.proceed(requestAutenticado)
        }
        .addInterceptor(loggingInterceptor)
        .authenticator(authenticator)
        .connectTimeout(30, TimeUnit.SECONDS)
        .readTimeout(30, TimeUnit.SECONDS)
        .writeTimeout(30, TimeUnit.SECONDS)
        .build()

    @Provides
    @Singleton
    fun provideRetrofit(httpClient: OkHttpClient, gson: Gson): Retrofit = Retrofit.Builder()
        .baseUrl(BuildConfig.API_BASE_URL)
        .client(httpClient)
        .addConverterFactory(GsonConverterFactory.create(gson))
        .build()
}
