import java.math.BigDecimal
import java.util.Properties
import org.gradle.testing.jacoco.tasks.JacocoCoverageVerification
import org.gradle.testing.jacoco.tasks.JacocoReport

plugins {
    jacoco
    alias(libs.plugins.android.application)
    alias(libs.plugins.kotlin.compose)
    alias(libs.plugins.ksp)
    alias(libs.plugins.hilt.android)
}

val localProperties = Properties().apply {
    rootProject.file("local.properties").takeIf { it.isFile }?.inputStream()?.use { load(it) }
}
val localApiBaseUrl = localProperties.getProperty("bookinghubApiBaseUrl")
val productionApiBaseUrl = providers.gradleProperty("bookinghubProductionApiBaseUrl")
    .orElse("https://proyectofinal-ashja9azeag8fxc9.mexicocentral-01.azurewebsites.net/")
    .get()
val debugApiBaseUrl = providers.gradleProperty("bookinghubApiBaseUrl")
    .orElse(localApiBaseUrl ?: productionApiBaseUrl)
    .get()
val appVersionName = providers.gradleProperty("bookinghubVersionName")
    .orElse("1.0.0")
    .get()
val appVersionCode = providers.gradleProperty("bookinghubVersionCode")
    .orElse("1")
    .get()
    .toIntOrNull()
    ?: throw GradleException("bookinghubVersionCode debe ser un entero positivo")
val releaseKeyStorePath = providers.environmentVariable("ANDROID_KEYSTORE_PATH").orNull
val releaseKeyStorePassword = providers.environmentVariable("ANDROID_KEYSTORE_PASSWORD").orNull
val releaseKeyAlias = providers.environmentVariable("ANDROID_KEY_ALIAS").orNull
val releaseKeyPassword = providers.environmentVariable("ANDROID_KEY_PASSWORD").orNull
val releaseSigningConfigured = listOf(
    releaseKeyStorePath,
    releaseKeyStorePassword,
    releaseKeyAlias,
    releaseKeyPassword
).all { !it.isNullOrBlank() }
val releaseSigningRequired = providers.gradleProperty("bookinghubRequireReleaseSigning")
    .orElse("false")
    .get()
    .toBoolean()

if (appVersionCode <= 0) {
    throw GradleException("bookinghubVersionCode debe ser mayor que cero")
}
if (releaseSigningRequired && !releaseSigningConfigured) {
    throw GradleException(
        "Falta configurar ANDROID_KEYSTORE_PATH, ANDROID_KEYSTORE_PASSWORD, " +
            "ANDROID_KEY_ALIAS y ANDROID_KEY_PASSWORD para firmar release"
    )
}

android {
    namespace = "com.bookinghub.app"
    compileSdk = 37

    defaultConfig {
        applicationId = "com.bookinghub.app"
        minSdk = 26
        targetSdk = 36
        versionCode = appVersionCode
        versionName = appVersionName
        testInstrumentationRunner = "androidx.test.runner.AndroidJUnitRunner"
    }

    signingConfigs {
        if (releaseSigningConfigured) {
            create("release") {
                storeFile = file(releaseKeyStorePath!!)
                storePassword = releaseKeyStorePassword
                keyAlias = releaseKeyAlias
                keyPassword = releaseKeyPassword
            }
        }
    }

    buildTypes {
        debug {
            enableUnitTestCoverage = true
            buildConfigField("String", "API_BASE_URL", "\"$debugApiBaseUrl\"")
            manifestPlaceholders["usesCleartextTraffic"] = debugApiBaseUrl.startsWith("http://").toString()
        }
        release {
            isMinifyEnabled = true
            isShrinkResources = true
            buildConfigField("String", "API_BASE_URL", "\"$productionApiBaseUrl\"")
            manifestPlaceholders["usesCleartextTraffic"] = "false"
            if (releaseSigningConfigured) {
                signingConfig = signingConfigs.getByName("release")
            }
            proguardFiles(getDefaultProguardFile("proguard-android-optimize.txt"), "proguard-rules.pro")
        }
    }
    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }
    testOptions {
        unitTests.isIncludeAndroidResources = true
    }
    buildFeatures {
        compose = true
        buildConfig = true
    }
}

tasks.register<JacocoReport>("jacocoDebugUnitTestReport") {
    dependsOn(tasks.named("testDebugUnitTest"))
    reports {
        xml.required = true
        html.required = true
        csv.required = true
    }
    sourceDirectories.setFrom(files("src/main/java"))
    classDirectories.setFrom(
        layout.buildDirectory.dir("intermediates/built_in_kotlinc/debug/compileDebugKotlin/classes"),
        layout.buildDirectory.dir("intermediates/javac/debug/compileDebugJavaWithJavac/classes")
    )
    executionData.setFrom(layout.buildDirectory.file("outputs/unit_test_code_coverage/debugUnitTest/testDebugUnitTest.exec"))
}

tasks.register<JacocoCoverageVerification>("jacocoCoreCoverageVerification") {
    dependsOn(tasks.named("testDebugUnitTest"))
    classDirectories.setFrom(
        layout.buildDirectory.dir("intermediates/built_in_kotlinc/debug/compileDebugKotlin/classes"),
        layout.buildDirectory.dir("intermediates/javac/debug/compileDebugJavaWithJavac/classes")
    )
    executionData.setFrom(layout.buildDirectory.file("outputs/unit_test_code_coverage/debugUnitTest/testDebugUnitTest.exec"))
    violationRules {
        rule {
            element = "BUNDLE"
            includes = listOf(
                "com.bookinghub.app.data.model.*",
                "com.bookinghub.app.data.repository.*",
                "com.bookinghub.app.ui.viewmodel.*"
            )
            limit {
                counter = "LINE"
                value = "COVEREDRATIO"
                minimum = BigDecimal("0.70")
            }
        }
    }
}

tasks.named("check") {
    dependsOn(tasks.named("jacocoCoreCoverageVerification"))
}

dependencies {
    implementation(platform(libs.compose.bom))
    implementation(platform(libs.okhttp.bom))

    implementation(libs.androidx.core.ktx)
    implementation(libs.androidx.lifecycle.runtime.ktx)
    implementation(libs.androidx.lifecycle.viewmodel.compose)
    implementation(libs.androidx.activity.compose)
    implementation(libs.androidx.hilt.lifecycle.viewmodel.compose)

    // Compose
    implementation(libs.compose.ui)
    implementation(libs.compose.ui.graphics)
    implementation(libs.compose.ui.tooling.preview)
    implementation(libs.compose.material3)
    implementation(libs.compose.material.icons.extended)

    // Navigation
    implementation(libs.androidx.navigation.compose)

    // Retrofit
    implementation(libs.retrofit.core)
    implementation(libs.retrofit.converter.gson)
    implementation(libs.okhttp.core)
    implementation(libs.okhttp.logging)

    // Coroutines
    implementation(libs.kotlinx.coroutines.android)
    implementation(libs.androidx.work.runtime.ktx)

    // Dependency injection for the app graph and ViewModels.
    implementation(libs.hilt.android)
    ksp(libs.hilt.compiler)

    debugImplementation(libs.compose.ui.tooling)
    debugImplementation(libs.compose.ui.test.manifest)

    testImplementation(libs.junit4)
    testImplementation(libs.mockk)
    testImplementation(libs.kotlinx.coroutines.test)
    testImplementation(libs.turbine)

    androidTestImplementation(platform(libs.compose.bom))
    androidTestImplementation(libs.compose.ui.test.junit4)
    androidTestImplementation(libs.androidx.test.runner)
    androidTestImplementation(libs.androidx.test.ext.junit)
    androidTestImplementation(libs.mockk.android)
}
