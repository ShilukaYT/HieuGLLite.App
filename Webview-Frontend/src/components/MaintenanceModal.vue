<template>
  <v-dialog model-value="true" persistent max-width="500" transition="dialog-bottom-transition"
    class="maintenance-dialog">
    <v-card class="maintenance-card rounded-xl overflow-hidden" :color="isDark ? '#121212' : '#FFFFFF'"
      :theme="isDark ? 'dark' : 'light'">
      <div class="card-bg-glow"></div>

      <v-card-text class="text-center pa-8 relative" style="z-index: 2;">
        <div class="icon-wrapper mb-6">
          <v-icon icon="mdi-alert-octagon-outline" color="error" size="100" class="maintenance-icon"></v-icon>
        </div>

        <h2 class="text-h4 font-weight-bold mb-4 text-uppercase ls-1">
          Hệ thống bảo trì
        </h2>

        <div class="text-body-1 text-medium-emphasis mb-8 px-4 font-weight-medium">
          <p class="mb-4">
            Máy chủ hiện đang tạm dừng để nâng cấp hệ thống và cải thiện hiệu suất.
          </p>
          <p class="text-body-2 opacity-70">
            Chúng tôi đang nỗ lực làm việc để đưa mọi thứ trở lại sớm nhất. Vui lòng quay lại sau!
          </p>
        </div>

        <v-divider class="mb-6 border-opacity-25 mx-12"></v-divider>

        <div class="d-flex flex-column align-center">
          <span class="text-caption text-medium-emphasis mb-3 text-uppercase font-weight-bold ls-2">
            Theo dõi cập nhật tại
          </span>
          <div class="d-flex gap-4">
            <v-btn icon variant="tonal" color="error" class="rounded-lg" size="large">
              <Icon icon="ic:sharp-discord" width="24" height="24" />
            </v-btn>
            <v-btn icon variant="tonal" color="error" class="rounded-lg" size="large">
              <v-icon icon="mdi-youtube"></v-icon>
            </v-btn>
            <v-btn icon variant="tonal" color="error" class="rounded-lg" size="large">
              <v-icon icon="mdi-web"></v-icon>
            </v-btn>
          </div>
        </div>

        <!-- Thêm text và nút làm mới trang -->
          
      </v-card-text>
    </v-card>
  </v-dialog>
</template>

<script setup>
import { computed } from 'vue';
import { useTheme } from 'vuetify';
import { Icon } from '@iconify/vue';

const theme = useTheme();
const isDark = computed(() => theme.global.current.value.dark);

const requestOpenURL = (url) =>
{
  if (window.chrome?.webview)
{
  
}
}
</script>

<style scoped>
:deep(.maintenance-dialog) {
  font-family: 'GoogleSans', sans-serif !important;
}

:deep(.v-overlay__scrim) {
  background: v-bind('isDark ? "rgba(0, 0, 0, 0.8)" : "rgba(255, 255, 255, 0.6)"') !important;
  backdrop-filter: blur(8px);
}

.maintenance-card {
  position: relative;
  /* Đã xóa dòng background: v-bind(...) gây lỗi ở đây */
  border: 1px solid v-bind('isDark ? "rgba(220, 53, 69, 0.3)" : "rgba(220, 53, 69, 0.2)"');
  box-shadow: v-bind('isDark ? "0 20px 60px rgba(0,0,0,0.5)" : "0 20px 60px rgba(220, 53, 69, 0.1)"') !important;
}

/* Lớp phủ gradient vẫn giữ nguyên để tạo hiệu ứng */
.card-bg-glow {
  position: absolute;
  inset: 0;
  z-index: 1;
  background: radial-gradient(circle at center,
      v-bind('isDark ? "rgba(220, 53, 69, 0.15)" : "rgba(220, 53, 69, 0.08)"') 0%,
      transparent 70%);
  pointer-events: none;
}

.maintenance-icon {
  filter: drop-shadow(0 0 25px rgba(220, 53, 69, 0.5));
  animation: pulse-slow 3s infinite ease-in-out;
}

@keyframes pulse-slow {

  0%,
  100% {
    transform: scale(1);
    opacity: 1;
  }

  50% {
    transform: scale(1.05);
    opacity: 0.8;
  }
}

.gap-4 {
  gap: 16px;
}

.ls-1 {
  letter-spacing: 1px;
}

.ls-2 {
  letter-spacing: 2px;
}
</style>