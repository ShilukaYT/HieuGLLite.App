<template>
  <v-dialog model-value="true" persistent max-width="500">
    <v-card class="rounded-xl border text-center pa-6" :theme="isDark ? 'dark' : 'light'" elevation="24">
      <v-icon color="error" size="80" class="mx-auto mb-4">mdi-earth-box-off</v-icon>
      
      <v-card-title class="text-h5 font-weight-black text-error text-wrap">
        REGION NOT SUPPORTED
      </v-card-title>
      
      <v-card-text class="text-body-1 mt-2" style="line-height: 1.6;">
        We apologize, but the Hiếu GL Lite application is currently only available in <b>Vietnam (VN)</b>.<br><br>
        Our system has detected that your IP address belongs to another region. Please disable your VPN or access the application from Vietnam to continue.
      </v-card-text>

      <v-card-actions class="justify-center mt-5">
        <v-btn color="error" variant="flat" rounded="pill" size="large" class="px-8 font-weight-bold" @click="exitApp">
          EXIT APPLICATION
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup>
import { computed } from 'vue';
import { useTheme } from 'vuetify';

const theme = useTheme();
const isDark = computed(() => theme.global.current.value.dark);

const exitApp = () => {
  if (window.chrome?.webview) {
    window.chrome.webview.postMessage({ type: "CLOSE_WINDOW" }); // Báo C# tắt app
  } else {
    window.close(); // Tắt tab nếu ở web ngoài
  }
};
</script>