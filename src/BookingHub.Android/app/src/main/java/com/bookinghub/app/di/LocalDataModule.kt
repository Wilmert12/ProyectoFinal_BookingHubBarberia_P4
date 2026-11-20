package com.bookinghub.app.di

import android.content.Context
import com.bookinghub.app.util.CitasCache
import com.bookinghub.app.util.SesionBarbero
import com.bookinghub.app.util.SesionCliente
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.android.qualifiers.ApplicationContext
import dagger.hilt.components.SingletonComponent
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
object LocalDataModule {
    @Provides
    @Singleton
    fun provideSesionCliente(@ApplicationContext context: Context): SesionCliente {
        SesionCliente.inicializar(context)
        return SesionCliente
    }

    @Provides
    @Singleton
    fun provideSesionBarbero(@ApplicationContext context: Context): SesionBarbero {
        SesionBarbero.inicializar(context)
        return SesionBarbero
    }

    @Provides
    @Singleton
    fun provideCitasCache(): CitasCache = CitasCache
}
