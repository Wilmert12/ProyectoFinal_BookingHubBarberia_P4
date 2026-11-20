package com.bookinghub.app.data.repository

import com.bookinghub.app.data.api.ApiClient
import com.bookinghub.app.data.model.CatalogoItemDto
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class CatalogoRepository @Inject constructor(private val apiClient: ApiClient) {
    suspend fun listar(q: String? = null, esServicio: Boolean? = null, ordenar: String? = null): ApiResult<List<CatalogoItemDto>> =
        executeApiRequest { apiClient.catalogoItemApi.getCatalogoItems(q, esServicio, ordenar) }
}
