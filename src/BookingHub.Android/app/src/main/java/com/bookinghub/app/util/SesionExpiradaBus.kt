package com.bookinghub.app.util

import kotlinx.coroutines.channels.BufferOverflow
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.asSharedFlow

object SesionExpiradaBus {
    private val eventos = MutableSharedFlow<Unit>(
        replay = 0,
        extraBufferCapacity = 1,
        onBufferOverflow = BufferOverflow.DROP_OLDEST
    )
    val expiraciones = eventos.asSharedFlow()

    fun avisar() {
        eventos.tryEmit(Unit)
    }
}
