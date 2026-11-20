package com.bookinghub.app.data.api

import retrofit2.Retrofit
import javax.inject.Inject
import javax.inject.Singleton

/** Crea los servicios Retrofit una vez y los entrega a los repositorios. */
@Singleton
class ApiClient @Inject constructor(retrofit: Retrofit) {
    val authApi: AuthApi = retrofit.create(AuthApi::class.java)
    val barberoApi: BarberoApi = retrofit.create(BarberoApi::class.java)
    val catalogoItemApi: CatalogoItemApi = retrofit.create(CatalogoItemApi::class.java)
    val citaApi: CitaApi = retrofit.create(CitaApi::class.java)
    val clienteApi: ClienteApi = retrofit.create(ClienteApi::class.java)
    val estadosCitaApi: EstadosCitaApi = retrofit.create(EstadosCitaApi::class.java)
    val informacionBarberiaApi: InformacionBarberiaApi = retrofit.create(InformacionBarberiaApi::class.java)
}
