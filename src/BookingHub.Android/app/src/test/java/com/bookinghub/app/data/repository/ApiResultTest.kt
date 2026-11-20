package com.bookinghub.app.data.repository

import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.test.runTest
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.ResponseBody.Companion.toResponseBody
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test
import retrofit2.Response
import java.io.IOException

class ApiResultTest {
    @Test
    fun executeApiRequest_returnsBodyForSuccess() = runTest {
        val result = executeApiRequest { Response.success("ok") }

        assertEquals(ApiResult.Success("ok"), result)
    }

    @Test
    fun executeApiRequest_mapsUnauthorizedToExpiredSession() = runTest {
        val result = executeApiRequest<String> {
            Response.error(401, "{}".toResponseBody("application/json".toMediaType()))
        }

        assertEquals(ApiResult.SessionExpired, result)
    }

    @Test
    fun executeApiRequest_extractsConflictMessage() = runTest {
        val result = executeApiRequest<String> {
            Response.error(409, """{"message":"El horario fue tomado"}""".toResponseBody("application/json".toMediaType()))
        }

        assertEquals(ApiResult.HttpError(409, "El horario fue tomado"), result)
    }

    @Test
    fun executeApiRequest_mapsIoExceptionToNetworkError() = runTest {
        val result = executeApiRequest<String> { throw IOException("offline") }

        assertEquals(ApiResult.NetworkError, result)
    }

    @Test
    fun executeApiRequest_doesNotSwallowCancellation() = runTest {
        val result = runCatching {
            executeApiRequest<String> { throw CancellationException("cancelled") }
        }

        assertTrue(result.exceptionOrNull() is CancellationException)
    }

    @Test
    fun noContentRequest_acceptsEmptySuccessfulResponse() = runTest {
        val result = executeApiRequestNoContent { Response.success(Unit) }

        assertEquals(ApiResult.Success(Unit), result)
    }
}
