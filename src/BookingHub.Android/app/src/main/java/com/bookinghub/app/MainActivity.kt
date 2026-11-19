package com.bookinghub.app

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import com.bookinghub.app.ui.navigation.AppNavigation
import com.bookinghub.app.ui.theme.BookingHubTheme

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            BookingHubTheme {
                AppNavigation()
            }
        }
    }
}
