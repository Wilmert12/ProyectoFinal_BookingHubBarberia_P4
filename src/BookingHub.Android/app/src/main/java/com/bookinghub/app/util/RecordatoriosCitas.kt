package com.bookinghub.app.util

import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.content.Context
import android.content.Intent
import android.net.Uri
import androidx.core.app.NotificationCompat
import androidx.core.app.NotificationManagerCompat
import androidx.work.CoroutineWorker
import androidx.work.OneTimeWorkRequestBuilder
import androidx.work.WorkManager
import androidx.work.WorkerParameters
import androidx.work.workDataOf
import com.bookinghub.app.MainActivity
import com.bookinghub.app.R
import com.bookinghub.app.data.model.CitaDto
import java.time.LocalDateTime
import java.time.ZoneId
import java.util.concurrent.TimeUnit

/** Programa recordatorios locales persistentes; los trabajos sobreviven a reinicios del proceso y del dispositivo. */
object RecordatoriosCitas {
    fun sincronizar(context: Context, citas: List<CitaDto>) {
        citas.forEach { cita ->
            if (cita.estado.orEmpty().lowercase() in setOf("cancelada", "completada", "noshow")) cancelar(context, cita.citaId)
            else programar(context, cita)
        }
    }

    fun programar(context: Context, cita: CitaDto) {
        val inicio = runCatching { LocalDateTime.parse(cita.fechaInicio.take(19)) }.getOrNull() ?: return
        val horaCita = inicio.atZone(ZoneId.systemDefault()).toInstant().toEpochMilli()
        listOf(24L to "Tu cita es mañana", 1L to "Tu cita es en una hora").forEach { (horas, titulo) ->
            val nombre = nombreTrabajo(cita.citaId, horas)
            WorkManager.getInstance(context.applicationContext).cancelUniqueWork(nombre)
            val espera = horaCita - TimeUnit.HOURS.toMillis(horas) - System.currentTimeMillis()
            if (espera <= 0L) return@forEach
            val data = workDataOf(
                Claves.CITA_ID to cita.citaId,
                Claves.TITULO to titulo,
                Claves.MENSAJE to "${cita.barberoNombre.orEmpty()} · ${inicio.toLocalDate()} ${inicio.toLocalTime().toString().take(5)}"
            )
            val work = OneTimeWorkRequestBuilder<RecordatorioCitaWorker>()
                .setInitialDelay(espera, TimeUnit.MILLISECONDS)
                .setInputData(data)
                .build()
            WorkManager.getInstance(context.applicationContext).enqueueUniqueWork(
                nombre, androidx.work.ExistingWorkPolicy.REPLACE, work
            )
        }
    }

    fun cancelar(context: Context, citaId: Int) {
        WorkManager.getInstance(context.applicationContext).cancelUniqueWork(nombreTrabajo(citaId, 24))
        WorkManager.getInstance(context.applicationContext).cancelUniqueWork(nombreTrabajo(citaId, 1))
    }

    private fun nombreTrabajo(citaId: Int, horas: Long) = "cita-$citaId-recordatorio-$horas-horas"
}

class RecordatorioCitaWorker(context: Context, params: WorkerParameters) : CoroutineWorker(context, params) {
    override suspend fun doWork(): Result {
        val citaId = inputData.getInt(Claves.CITA_ID, 0)
        val titulo = inputData.getString(Claves.TITULO) ?: return Result.failure()
        val mensaje = inputData.getString(Claves.MENSAJE).orEmpty()
        if (citaId <= 0) return Result.failure()

        val channelId = "recordatorios_citas"
        val manager = applicationContext.getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
        manager.createNotificationChannel(NotificationChannel(
            channelId, "Recordatorios de citas", NotificationManager.IMPORTANCE_DEFAULT
        ).apply { description = "Avisos locales de las próximas reservas" })
        val intent = Intent(Intent.ACTION_VIEW, Uri.parse("bookinghub://cita/$citaId"), applicationContext, MainActivity::class.java)
            .putExtra(Claves.CITA_ID, citaId)
            .addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP or Intent.FLAG_ACTIVITY_SINGLE_TOP)
        val pending = PendingIntent.getActivity(applicationContext, citaId,
            intent, PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE)
        val notification = NotificationCompat.Builder(applicationContext, channelId)
            .setSmallIcon(R.drawable.ic_logo)
            .setContentTitle(titulo)
            .setContentText(mensaje)
            .setStyle(NotificationCompat.BigTextStyle().bigText(mensaje))
            .setContentIntent(pending)
            .setAutoCancel(true)
            .build()
        return try {
            NotificationManagerCompat.from(applicationContext).notify(citaId, notification)
            Result.success()
        } catch (_: SecurityException) {
            Result.success()
        }
    }
}

object Claves {
    const val CITA_ID = "cita_id"
    const val TITULO = "titulo"
    const val MENSAJE = "mensaje"
    const val ABRIR_CITAS = "abrir_mis_citas"
}
