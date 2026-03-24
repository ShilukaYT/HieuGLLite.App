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

            {{ tag.text }}
          </v-chip>
        </div>

        <div class="text-body-1" :class="isDark ? 'text-grey-lighten-1' : 'text-grey-darken-2'"
          style="line-height: 1.6;">
          {{ isDescLong ? shortDesc : app.desc }}

          <a v-if="isDescLong" @click.prevent="showInfoModal = true" style="cursor: pointer"
            class="text-primary text-decoration-none ml-1 font-weight-bold">
            Xem thêm
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

                  <span :class="isDark ? 'text-grey-lighten-2' : 'text-grey-darken-2'">{{ tag.text }}</span>
                </v-chip>
              </div>

              <p class="text-body-1" :class="isDark ? 'text-grey-lighten-1' : 'text-grey-darken-3'"
                style="white-space: pre-wrap; line-height: 1.7;">
                {{ app.desc }}
              </p>
            </v-card-text>
          </v-card>
        </v-dialog>
      </v-card-item>

      <v-card-actions class="mt-6 px-4 pb-4 flex-column align-stretch">
        <div v-if="downloadingApps[app.id]" class="modern-notifier-box w-100">
          <div class="d-flex justify-space-between align-center mb-1">
            <div class="d-flex flex-column">
              <span class="app-name-label">{{ stageConfig[downloadingApps[app.id].status]?.text || "TẠM DỪNG" }}</span>
            </div>

            <div v-if="['DOWNLOADING_EXE', 'DOWNLOADING_ANDROID', 'PAUSED'].includes(downloadingApps[app.id].status)"
              class="d-flex align-center">

              <v-btn icon variant="text" size="small" density="compact" class="mr-1"
                :color="downloadingApps[app.id].status === 'PAUSED' ? 'success' : 'warning'"
                @click.stop="$emit('toggle-pause', app.id)">
                <v-icon size="20">
                  {{ downloadingApps[app.id].status === 'PAUSED' ? 'mdi-play-circle' : 'mdi-pause-circle' }}
                </v-icon>
              </v-btn>

              <v-btn icon variant="text" size="small" density="compact" color="error" class="mr-3"
                @click.stop="$emit('confirm-cancel', app.id)">
                <v-icon size="20">mdi-close-circle</v-icon>
              </v-btn>

              <span class="huge-percent">{{ Math.ceil(downloadingApps[app.id].percent) }}<small>%</small></span>
            </div>
          </div>

          <v-progress-linear :model-value="downloadingApps[app.id].percent"
            :color="stageConfig[downloadingApps[app.id].status]?.color" height="6" rounded
            :indeterminate="stageConfig[downloadingApps[app.id].status]?.loading"></v-progress-linear>


          <div v-if="downloadingApps[app.id].downloaded || downloadingApps[app.id].speed"
            class="d-flex justify-space-between mt-1 metrics-footer">
            <span class="mono-font">
              <v-icon size="10" class="mr-1">mdi-harddisk</v-icon>
              {{ downloadingApps[app.id].downloaded }}
            </span>
            <span class="mono-font speed-highlight">
              <v-icon size="10" class="mr-1" color="primary">mdi-wifi</v-icon>
              {{ downloadingApps[app.id].speed }}
            </span>
          </div>
        </div>

        <template v-else>
          <div v-if="app.isInstalled && app.isRunning" class="d-flex w-100 align-center" style="gap: 12px;">
            <v-btn color="grey-darken-3" variant="flat" size="x-large" rounded="pill"
              class="flex-grow-1 font-weight-bold" disabled>
              <v-icon start icon="mdi-loading mdi-spin" class="mr-2"></v-icon> ĐANG MỞ...
            </v-btn>
            <v-btn color="error" variant="tonal" size="x-large" icon="mdi-power" @click="$emit('kill-app')"></v-btn>
          </div>

          <template v-else-if="app.isInstalled">
            <div class="d-flex w-100" style="gap: 12px;">
              <v-btn color="success" variant="flat" size="x-large" rounded="pill" class="flex-grow-1 font-weight-bold"
                @click="$emit('play')">
                <v-icon start icon="mdi-play" class="mr-2"></v-icon> MỞ ỨNG DỤNG
              </v-btn>

              <v-menu location="top end" transition="slide-y-reverse-transition">
                <template v-slot:activator="{ props }">
                  <v-btn v-bind="props" color="success" variant="tonal" size="x-medium" style="gap: 8px;" icon="mdi-cog"
                    rounded="pill">
                  </v-btn>
                </template>

                <v-list class="rounded-xl pa-2 mt-2" :theme="isDark ? 'dark' : 'light'" border elevation="12">
                  <v-list-item prepend-icon="mdi-layers-triple" title="Trình quản lý đa phiên bản"
                    @click="$emit('extra-action', { type: 'OPEN_MULTI', id: app.id })">
                  </v-list-item>

                  <v-list-item prepend-icon="mdi-broom" title="Dọn dẹp bộ nhớ" class="rounded-lg mb-1"
                    @click="$emit('extra-action', { type: 'CLEANUP', id: app.id })">
                  </v-list-item>

                  <v-list-item prepend-icon="mdi-cloud-upload" title="Sao lưu" class="rounded-lg mb-1"
                    @click="$emit('extra-action', { type: 'BACKUP', id: app.id })">
                  </v-list-item>

                  <v-list-item prepend-icon="mdi-cloud-download" title="Khôi phục" class="rounded-lg mb-1"
                    @click="$emit('extra-action', { type: 'RESTORE', id: app.id })">
                  </v-list-item>

                  <v-divider class="my-1"></v-divider>

                  <!-- v-if="props.manifest?.BE_versioncode >= 260310"  -->
                  <v-list-item 
                    prepend-icon="mdi-source-branch" 
                    title="Thay đổi phiên bản"
                    class="rounded-lg text-primary" 
                    @click="$emit('extra-action', { type: 'CHANGE_VERSION', id: app.id })"> 
                  </v-list-item>

                  <!-- <v-list-item prepend-icon="mdi-monitor-multiple" title="Tạo Instance mới"
                    class="rounded-lg text-success"
                    @click="$emit('extra-action', { type: 'INSTALL_MULTI', id: app.id })">
                  </v-list-item> -->

                  <v-list-item prepend-icon="mdi-trash-can-outline" title="Gỡ cài đặt" class="rounded-lg text-error"
                    @click="$emit('extra-action', { type: 'UNINSTALL', id: app.id })">
                  </v-list-item>
                </v-list>
              </v-menu>
            </div>
          </template>

          <v-btn v-else color="primary" variant="flat" size="x-large" rounded="pill" block
            :disabled="isOtherAppDownloading" @click="$emit('open-install')">
            <v-icon start icon="mdi-download" class="mr-2"></v-icon> CÀI ĐẶT NGAY
          </v-btn>
        </template>
      </v-card-actions>
    </v-card>

  </v-container>
</template>

<script setup>
import { ref, computed, onMounted ,watch} from 'vue';
import { useTheme } from 'vuetify';

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

// Dùng props.app chuẩn xác
const isDescLong = computed(() => {
  return props.app?.desc && props.app.desc.length > 100;
});

// Cắt ngắn chuỗi mô tả và thêm dấu "..."
const shortDesc = computed(() => {
  if (!props.app?.desc) return "";
  return props.app.desc.substring(0, 60) + "...";
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
</style>