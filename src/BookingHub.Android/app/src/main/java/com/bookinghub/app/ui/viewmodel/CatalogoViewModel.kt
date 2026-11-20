package com.bookinghub.app.ui.viewmodel

import android.content.Context
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.bookinghub.app.data.model.CatalogoItemDto
import com.bookinghub.app.data.repository.ApiResult
import com.bookinghub.app.data.repository.CatalogoRepository
import com.bookinghub.app.data.repository.mensajeParaUsuario
import com.bookinghub.app.util.CitasCache
import dagger.hilt.android.qualifiers.ApplicationContext
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

data class CatalogoUiState(
    val items: List<CatalogoItemDto> = emptyList(),
    val cargando: Boolean = false,
    val error: String? = null,
    val aviso: String? = null
)

@HiltViewModel
class CatalogoViewModel @Inject constructor(
    private val catalogoRepository: CatalogoRepository,
    @ApplicationContext private val context: Context
) : ViewModel() {
    private val _uiState = MutableStateFlow(CatalogoUiState())
    val uiState: StateFlow<CatalogoUiState> = _uiState

    init {
        cargarCatalogo()
    }

    fun cargarCatalogo(q: String? = null, esServicio: Boolean? = null, ordenar: String? = null) {
        val cache = CitasCache.leerCatalogo(context)
        val cacheFiltrada = cache?.filter { item ->
            (q.isNullOrBlank() || item.nombre.contains(q, true) || item.descripcion.orEmpty().contains(q, true)) &&
                (esServicio == null || item.esServicio == esServicio)
        }?.sortedWith(when (ordenar) {
            "precio" -> compareBy { it.precio }
            "precio-desc" -> compareByDescending { it.precio }
            "duracion" -> compareBy { it.duracionMin ?: Int.MAX_VALUE }
            else -> compareBy(String.CASE_INSENSITIVE_ORDER) { it.nombre }
        })
        viewModelScope.launch {
            _uiState.value = CatalogoUiState(items = cacheFiltrada.orEmpty(), cargando = true)
            when (val resultado = catalogoRepository.listar(q?.takeIf(String::isNotBlank), esServicio, ordenar)) {
                is ApiResult.Success -> {
                    CitasCache.guardarCatalogo(context, resultado.data)
                    _uiState.value = CatalogoUiState(items = resultado.data)
                }
                else -> _uiState.value = if (cache != null) CatalogoUiState(
                    items = cacheFiltrada.orEmpty(),
                    aviso = "Sin conexión. Se muestra el último catálogo guardado."
                ) else CatalogoUiState(error = resultado.mensajeParaUsuario("No se pudo cargar el catálogo."))
            }
        }
    }
}
