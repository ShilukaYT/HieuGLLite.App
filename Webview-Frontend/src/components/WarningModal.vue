<template>
  <v-dialog
    model-value="true"
    persistent
    max-width="550"
    transition="scale-transition"
    class="warning-dialog"
  >
    <v-card
      class="warning-box rounded-xl pa-8 text-center"
      :color="isDark ? '#121212' : '#FFFFFF'"
      :theme="isDark ? 'dark' : 'light'"
      style="backdrop-filter: blur(20px);"
      :style="{ borderColor: isDark ? 'rgba(220, 53, 69, 0.5)' : 'rgba(220, 53, 69, 0.8)' }"
    >
      <v-icon
        icon="mdi-alert-octagon"
        color="error"
        size="80"
        class="mb-6 warning-icon"
      ></v-icon>

      <v-card-title class="text-h4 font-weight-bold  mb-2 pb-0">
        Môi trường không hợp lệ!
      </v-card-title>

      <v-card-text class="text-body-1 text-medium-emphasis px-4 mb-6 pt-2">
        Hệ thống phát hiện bạn đang chạy ứng dụng trong môi trường khác, 
        vui lòng cài đặt ứng dụng để có trải nghiệm tốt nhất.
        <br /><br />
        Nếu bạn là nhà phát triển và muốn bỏ qua cảnh báo này, vui lòng nhấn nút bên dưới.
      </v-card-text>

      <v-card-actions class="justify-center">
        <v-btn
          color="error"
          variant="outlined"
          rounded="pill"
          size="x-large"
          class="font-weight-bold px-10"
          @click="$emit('close')"
        >
          TÔI LÀ DEV, BỎ QUA CẢNH BÁO
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup>
import { computed } from 'vue';
import { useTheme } from 'vuetify';

// Khai báo sự kiện 'close'
defineEmits(['close']);

// Lấy thông tin Theme hiện tại từ hệ thống Vuetify
const theme = useTheme();
const isDark = computed(() => theme.global.current.value.dark);
</script>

<style scoped>
/* Chỉnh phông chữ */
.warning-dialog .v-card .v-card-text,.warning-dialog .v-card .v-btn {
  font-family: 'GoogleSans', sans-serif !important;
}
/* Đồng bộ màu overlay dựa trên chế độ sáng tối */
:deep(.v-overlay__layer) {
  background: v-bind('isDark ? "rgba(0, 0, 0, 0.7)" : "rgba(255, 255, 255, 0.5)"') !important;
}

.warning-box {
  border-width: 2px !important;
  border-style: solid !important;
  /* Hiệu ứng bóng đổ cũng thay đổi theo nền */
  box-shadow: v-bind('isDark ? "0 10px 40px rgba(220, 53, 69, 0.15)" : "0 10px 30px rgba(220, 53, 69, 0.1)"') !important;
}

.warning-icon {
  filter: drop-shadow(0 0 15px rgba(220, 53, 69, 0.6));
}
</style>