# Reglas específicas de R8 para la app.
-keepattributes Signature,*Annotation*,InnerClasses,EnclosingMethod
-keep class com.bookinghub.app.data.model.** { *; }
-keep class com.bookinghub.app.data.api.** { *; }
-keepclassmembers,allowobfuscation class * {
    @com.google.gson.annotations.SerializedName <fields>;
}
-keep class retrofit2.** { *; }
-dontwarn retrofit2.**
-keep class okhttp3.** { *; }
-dontwarn okhttp3.**
-keep class com.google.gson.** { *; }
-dontwarn com.google.gson.**
-keep class dagger.hilt.** { *; }
-dontwarn dagger.hilt.**
