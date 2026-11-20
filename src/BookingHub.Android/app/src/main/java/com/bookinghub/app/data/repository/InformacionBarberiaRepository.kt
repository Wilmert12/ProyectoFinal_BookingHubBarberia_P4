package com.bookinghub.app.data.repository

import com.bookinghub.app.data.api.ApiClient
import com.bookinghub.app.data.model.InformacionBarberiaDto
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class InformacionBarberiaRepository @Inject constructor(private val apiClient: ApiClient) {
    suspend fun obtener(): ApiResult<InformacionBarberiaDto> =
        executeApiRequest { apiClient.informacionBarberiaApi.getInformacion() }
}
