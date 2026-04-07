<template>
  <v-container class="fill-height d-flex align-end pb-12 position-relative" style="z-index: 2;">

    <v-card class="rounded-xl pa-4" :color="isDark ? 'rgba(30, 30, 30, 0.85)' : 'rgba(255, 255, 255, 0.85)'"
      style="backdrop-filter: blur(20px); border: 1px solid rgba(255,255,255,0.1); width: 800px;" elevation="12">
      <v-card-item>
        <template v-slot:prepend>
          <v-avatar size="80" class="rounded-lg mr-4 elevation-5 pa-1">
            <v-img :src="app.icon" :alt="app.name"></v-img>
          </v-avatar>
        </template>

        <v-card-title class="text-h3 font-weight-black pb-0" style="line-height: 1.1;">
          {{ app.name }}
        </v-card-title>

        <div v-if="app.tags && app.tags.length > 0" class="d-flex flex-wrap mt-1 mb-3">
          <v-chip v-for="(tag, index) in app.tags" :key="index" size="small"
            :color="isDark ? 'grey-lighten-2' : 'grey-darken-2'" variant="tonal" class="mr-2 font-weight">
            <template v-slot:prepend v-if="tag.icon && tag.icon.trim() !== ''">
              <v-icon v-if="tag.icon.startsWith('mdi-')" :icon="tag.icon" start size="16"></v-icon>

              <img v-else :src="tag.icon"
                style="width: 16px; height: 16px; margin-right: 8px; object-fit: contain; display: block;" />
            </template>

            {{ loc(tag.text) }}
          </v-chip>
        </div>

        <div class="text-body-1" :class="isDark ? 'text-grey-lighten-1' : 'text-grey-darken-2'"
          style="line-height: 1.6;">
          {{ isDescLong ? shortDesc : app.desc }}

          <a v-if="isDescLong" @click.prevent="showInfoModal = true" style="cursor: pointer"
            class="text-primary text-decoration-none ml-1 font-weight-bold">
            {{ $t('game_page.read_more') }}
          </a>
        </div>

        <v-dialog v-model="showInfoModal" max-width="600" transition="dialog-bottom-transition">
          <v-card rounded="xl" :color="isDark ? '#121212' : '#FFFFFF'" class="modal-border">
            <v-card-title class="text-h5 font-weight-bold d-flex align-center pa-4 pb-2"
              :class="isDark ? 'text-white' : 'text-black'">
              <v-avatar size="36" rounded="lg" class="mr-3 elevation-2 pa-1"
                :class="isDark ? 'bg-grey-darken-3' : 'bg-grey-lighten-4'">
                <v-img :src="app.icon"></v-img>
              </v-avatar>
              {{ app.name }}
              <v-spacer></v-spacer>
              <v-btn icon="mdi-close" variant="text" @click="showInfoModal = false"></v-btn>
            </v-card-title>

            <v-card-text class="pa-4 pt-2">
              <div v-if="app.tags && app.tags.length > 0" class="mb-5 d-flex flex-wrap">
                <v-chip v-for="(tag, index) in app.tags" :key="index" size="small" variant="flat"
                  :color="isDark ? '#333333' : '#e6e6e8'" class="custom-chip mr-2 mb-2 font-weight-bold px-3">
                  <template v-slot:prepend v-if="tag.icon">
                    <v-icon v-if="tag.icon.startsWith('mdi-')" :icon="tag.icon" start size="16" color="white"></v-icon>
                    <img v-else :src="tag.icon" class="chip-svg" />
                  </template>

                  <span :class="isDark ? 'text-grey-lighten-2' : 'text-grey-darken-2'">{{ loc(tag.text) }}</span>
                </v-chip>
              </div>

              <p class="text-body-1" :class="isDark ? 'text-grey-lighten-1' : 'text-grey-darken-3'"
                style="white-space: pre-wrap; line-height: 1.7;">
                {{ localizedDesc }}
              </p>
            </v-card-text>
          </v-card>
        </v-dialog>
      </v-card-item>

      <v-card-actions class="mt-6 px-4 pb-4 flex-column align-stretch">
        <div v-if="downloadingApps[app.id]" class="w-100 d-flex align-stretch mb-2" style="gap: 12px; height: 68px;">

          <v-progress-linear :model-value="downloadingApps[app.id].percent"
            :color="stageConfig[downloadingApps[app.id].status]?.color || 'primary'"
             height="68" rounded="xl" class="flex-grow-1 custom-bar position-relative overflow-hidden"
            :indeterminate="stageConfig[downloadingApps[app.id].status]?.loading"
            :style="{ border: isDark ? '1px solid rgba(255, 255, 255, 0.15)' : '1px solid rgba(0, 0, 0, 0.15)' }"
            :bg-color="isDark ? '#121212' : '#333333'" 
            :bg-opacity="0.9" >
            <div class="w-100 d-flex align-center px-4" style="height: 100%; z-index: 2;">
              
              <div v-if="!stageConfig[downloadingApps[app.id].status]?.loading" 
                   class="d-flex align-baseline mr-5" 
                   :class="'text-white'">
                <span class="font-weight-regular" style="font-size: 36px; font-family: 'Google Sans', sans-serif;">
                  {{ Math.ceil(downloadingApps[app.id].percent || 0) }}
                </span>
                <span class="font-weight-regular" style="font-size: 20px; font-family: 'Google Sans', sans-serif;">%</span>
              </div>

              <div class="d-flex flex-column justify-center" 
                   :class="'text-white'">
                 <span class="font-weight-regular" style="font-size: 26px; font-family: 'Google Sans', sans-serif;">
                   {{ 
                     ['DOWNLOADING_EXE', 'DOWNLOADING_ANDROID'].includes(downloadingApps[app.id].status) 
                       ? (downloadingApps[app.id].speed || '0 MB/s') 
                       : (stageConfig[downloadingApps[app.id].status]?.text || $t('app.stages.pause')) 
                   }}
                 </span>
                 <span v-if="['DOWNLOADING_EXE', 'DOWNLOADING_ANDROID', 'PAUSED'].includes(downloadingApps[app.id].status)" 
                       style="font-size: 14px; opacity: 0.9; font-family: 'Google Sans', sans-serif;">
                   {{ downloadingApps[app.id].downloaded  }}
                 </span>
              </div>
            </div>
          </v-progress-linear>

          <template v-if="!stageConfig[downloadingApps[app.id].status]?.loading">

            <v-btn v-if="['DOWNLOADING_EXE', 'DOWNLOADING_ANDROID', 'PAUSED'].includes(downloadingApps[app.id].status)"
              :color="downloadingApps[app.id].status === 'PAUSED' ? 'success' : 'warning'"
              class="rounded-xl fill-height" style="min-width: 68px; padding: 0;" variant="flat" elevation="0"
              @click.stop="$emit('toggle-pause', app.id)">
              <v-icon size="36" color="white">{{ downloadingApps[app.id].status === 'PAUSED' ? 'mdi-play' : 'mdi-pause'
                }}</v-icon>
            </v-btn>

            <v-btn color="#ff4b4b" class="rounded-xl fill-height" style="min-width: 68px; padding: 0;" variant="flat"
              elevation="0" @click.stop="$emit('confirm-cancel', app.id)">
              <v-icon size="36" color="white">mdi-close</v-icon>
            </v-btn>

          </template>

        </div>

        <template v-else>
          <div v-if="app.isInstalled && app.isRunning" class="d-flex w-100 align-center" style="gap: 12px;">
            <v-btn color="grey-darken-3" variant="flat" size="x-large" rounded="pill"
              class="flex-grow-1 font-weight-bold" disabled>
              <v-icon start icon="mdi-loading mdi-spin" class="mr-2"></v-icon> {{ $t('game_page.opening') }}
            </v-btn>
            <v-btn color="error" variant="tonal" size="x-large" icon="mdi-power" @click="$emit('kill-app')"></v-btn>
          </div>

          <template v-else-if="app.isInstalled">
            <div class="d-flex w-100" style="gap: 12px;">
              <v-btn color="success" variant="flat" size="x-large" rounded="pill" class="flex-grow-1 font-weight-bold"
                @click="$emit('play')">
                <v-icon start icon="mdi-play" class="mr-2"></v-icon> {{ $t('game_page.open_app') }}
              </v-btn>

              <v-menu location="top end" transition="slide-y-reverse-transition" >
                <template v-slot:activator="{ props }">
                  <v-btn v-bind="props" color="success" variant="tonal" size="x-medium" style="gap: 8px;" icon="mdi-cog"
                    rounded="pill">
                  </v-btn>
                </template>

                <v-list class="rounded-xl pa-2 mt-2" :theme="isDark ? 'dark' : 'light'" border elevation="12">
                  <v-list-item prepend-icon="mdi-layers-triple" :title="$t('game_page.multi_instance_manager')"
                    @click="$emit('extra-action', { type: 'OPEN_MULTI', id: app.id })">
                  </v-list-item>

                  <v-list-item prepend-icon="mdi-broom" :title="$t('game_page.clear_memory')" class="rounded-lg mb-1"
                    @click="$emit('extra-action', { type: 'CLEANUP', id: app.id })">
                  </v-list-item>

                  <v-list-item prepend-icon="mdi-cloud-upload" :title="$t('game_page.backup')" class="rounded-lg mb-1"
                    @click="$emit('extra-action', { type: 'BACKUP', id: app.id })">
                  </v-list-item>

                  <v-list-item prepend-icon="mdi-cloud-download" :title="$t('game_page.restore')"
                    class="rounded-lg mb-1" @click="$emit('extra-action', { type: 'RESTORE', id: app.id })">
                  </v-list-item>

                  <v-divider class="my-1"></v-divider>

                  <v-list-item prepend-icon="mdi-source-branch" :title="$t('game_page.change_version')"
                    class="rounded-lg text-primary"
                    @click="$emit('extra-action', { type: 'CHANGE_VERSION', id: app.id })">
                  </v-list-item>

                  <v-list-item prepend-icon="mdi-trash-can-outline" :title="$t('game_page.uninstall')"
                    class="rounded-lg text-error" @click="$emit('extra-action', { type: 'UNINSTALL', id: app.id })">
                  </v-list-item>
                </v-list>
              </v-menu>
            </div>
          </template>

          <v-btn v-else color="primary" variant="flat" size="x-large" rounded="pill" block
            :disabled="isOtherAppDownloading" @click="$emit('open-install')">
            <v-icon start icon="mdi-download" class="mr-2"></v-icon> {{ $t('game_page.install_now') }}
          </v-btn>
        </template>
      </v-card-actions>
    </v-card>

  </v-container>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue';
import { useTheme } from 'vuetify';
import { useI18n } from 'vue-i18n'; // Khai báo i18n

const { t, locale } = useI18n(); // Gọi hàm t()

// 1. Gán biến props CỰC KỲ QUAN TRỌNG
const props = defineProps(['app', 'downloadingApps', 'stageConfig', 'manifest']); // Nhận từ App.vue
// 2. Bổ sung 'kill-app' vào danh sách khai báo
// GamePage.vue
defineEmits(['play', 'open-install', 'kill-app', 'toggle-pause', 'confirm-cancel', 'extra-action']);

const isOtherAppDownloading = computed(() => {
  return Object.keys(props.downloadingApps).some(id => id !== props.app.id);
});

const theme = useTheme();
const isDark = computed(() => theme.global.current.value.dark);

// Hàm gửi tiêu đề lên C#
const updateWindowTitle = (name) => {
  if (window.chrome?.webview && name) {
    window.chrome.webview.postMessage({
      type: "CHANGE_TITLE",
      title: name
    });
  }
};


onMounted(() => {
  window.chrome.webview.postMessage({ type: "SYNC_DOWNLOAD_STATUS" });

  updateWindowTitle(props.app?.name);
});

// 2. Cập nhật ngay lập tức nếu dữ liệu app thay đổi (Chuyển game)
watch(() => props.app?.name, (newName) => {
  updateWindowTitle(newName);
}, { immediate: true });

// Tạo biến màu overlay dựa trên theme
const overlayColor = computed(() =>
  isDark.value ? '0, 0, 0' : '255, 255, 255'
);

const showInfoModal = ref(false);

const loc = (text) => {
  if (!text) return "";
  const p = text.split('|');
  // Nếu đang dùng tiếng anh và có dấu | thì lấy phần tử số 1, ngược lại lấy số 0
  return locale.value.startsWith('en') && p.length > 1 ? p[1].trim() : p[0].trim();
};

// Cập nhật lại các biến xử lý độ dài mô tả (dùng hàm loc ở trên)
const localizedDesc = computed(() => loc(props.app?.desc));

const isDescLong = computed(() => {
  return localizedDesc.value && localizedDesc.value.length > 100;
});

const shortDesc = computed(() => {
  if (!localizedDesc.value) return "";
  return localizedDesc.value.substring(0, 100) + "...";
});
</script>

<style scoped>
.overlay {
  position: absolute;
  inset: 0;
  z-index: 1;
  /* Sử dụng biến v-bind để đổi màu đen/trắng tùy theo theme */
  background: linear-gradient(90deg,
      rgba(v-bind(overlayColor), 0.7) 0%,
      rgba(v-bind(overlayColor), 0.1) 70%,
      transparent 100%);
}

/* Tạo viền mờ cho Modal khi ở chế độ sáng */
.modal-border {
  border: 1px solid rgba(var(--v-border-color), 0.1);
}

/* Chip style giống ảnh image_1a32cf.png */
.custom-chip {
  border-radius: 20px !important;
  /* Bo tròn pill */
  box-shadow: 0 2px 5px rgba(0, 0, 0, 0.2) !important;
  /* Đổ bóng nhẹ */
  height: 28px !important;
  transition: transform 0.2s;
}

.custom-chip:hover {
  transform: translateY(-1px);
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.3) !important;
}

.chip-svg {
  width: 16px;
  height: 16px;
  margin-right: 6px;
  object-fit: contain;
  display: block;
}
</style>

<style scoped>
/* 1. Ưu tiên Google Sans cho toàn bộ text */
.modern-notifier-box {
  font-family: 'Google Sans', 'Inter', sans-serif !important;
  padding: 8px 4px;
}

/* 2. CHẶN Google Sans đè lên Icon (Lỗi mất icon nằm ở đây) */
.v-icon {
  font-family: "Material Design Icons" !important;
}

.app-name-label {
  font-size: 12px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.huge-percent {
  font-size: 24px;
  font-weight: 900;
  color: #2196F3;
  line-height: 1;
  /* Giúp số đứng yên khi thay đổi nhanh */
  font-variant-numeric: tabular-nums;
}

/* 3. Font cho thông số (Dùng Monospace để tối ưu CPU) */
.mono-font {
  font-family: 'JetBrains Mono', 'Roboto Mono', monospace !important;
  font-size: 10px;
  opacity: 0.7;
}

.speed-highlight {
  color: #2196F3;
  font-weight: 700;
}

.custom-bar {
  background: rgba(255, 255, 255, 0.05);
}

.loading-stripes::after {
  content: "";
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  /* Tăng độ sáng (opacity) từ 0.08 lên 0.15 để nhìn rõ hơn */
  background-image: linear-gradient(-45deg,
      rgba(255, 255, 255, 0.15) 25%,
      transparent 25%,
      transparent 50%,
      rgba(255, 255, 255, 0.15) 50%,
      rgba(255, 255, 255, 0.15) 75%,
      transparent 75%,
      transparent);
  background-size: 40px 40px;
  animation: strip-move 1s linear infinite;
  pointer-events: none;
  /* Không cản trở click chuột */
}

@keyframes strip-move {
  0% {
    background-position: 0 0;
  }

  100% {
    background-position: 40px 0;
  }
}
</style>