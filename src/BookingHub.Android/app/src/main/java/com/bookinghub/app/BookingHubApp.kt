package com.bookinghub.app

import android.app.Application
import com.bookinghub.app.util.SesionBarbero
import com.bookinghub.app.util.SesionCliente
import dagger.hilt.android.HiltAndroidApp

@HiltAndroidApp
class BookingHubApp : Application() {
    override fun onCreate() {
        super.onCreate()
        SesionCliente.inicializar(this)
        SesionBarbero.inicializar(this)
    }
}
