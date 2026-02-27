<template>
  <v-container class="fill-height d-flex align-end pb-12 position-relative" style="z-index: 2;">

    <v-card class="rounded-xl pa-4" :color="isDark ? 'rgba(30, 30, 30, 0.85)' : 'rgba(255, 255, 255, 0.85)'"
      style="backdrop-filter: blur(20px); border: 1px solid rgba(255,255,255,0.1); width: 800px;" elevation="12">
      <v-card-item>
        <template v-slot:prepend>
          <v-avatar size="80" class="rounded-lg mr-4 elevation-5">
            <v-img :src="app.icon" :alt="app.name"></v-img>
          </v-avatar>
        </template>

        <v-card-title class="text-h3 font-weight-black pb-0" style="line-height: 1.1;">
          {{ app.name }}
        </v-card-title>

        <div v-if="app.tags && app.tags.length > 0" class="d-flex flex-wrap mt-1 mb-3">
          <v-chip v-for="(tag, index) in app.tags" :key="index" size="small" :color="isDark ? 'grey-lighten-2' : 'grey-darken-2'" variant="tonal"
            class="mr-2 font-weight">
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

          <a v-if="isDescLong" href="#" @click.prevent="showInfoModal = true"
            class="text-primary text-decoration-none ml-1 font-weight-bold">
            Xem thêm
          </a>
        </div>

        <v-dialog v-model="showInfoModal" max-width="600" transition="dialog-bottom-transition">
          <v-card rounded="xl" color="grey-darken-4">
            <v-card-title class="text-h5 font-weight-bold d-flex align-center pa-4 pb-2">
              <v-avatar size="36" class="mr-3">
                <v-img :src="app.icon"></v-img>
              </v-avatar>
              {{ app.name }}
              <v-spacer></v-spacer>
              <v-btn icon="mdi-close" variant="text" @click="showInfoModal = false"></v-btn>
            </v-card-title>

            <v-card-text class="pa-4 pt-2 text-body-1" :class="isDark ? 'text-grey-lighten-1' : 'text-grey-darken-2'">
              <div v-if="app.tags" class="mb-4">
                <v-chip v-for="tag in app.tags" :key="tag" size="small" color="primary" variant="flat" class="mr-2">
                  {{ tag }}
                </v-chip>
              </div>

              <p style="white-space: pre-wrap; line-height: 1.7;">{{ app.desc }}</p>
            </v-card-text>
          </v-card>
        </v-dialog>
      </v-card-item>

      <v-card-actions class="mt-6 px-4 pb-4">
        <div v-if="app.isInstalled && app.isRunning" class="d-flex w-100 align-center" style="gap: 12px;">
          <v-btn color="grey-darken-1" variant="flat" size="x-large" rounded="pill"
            class="font-weight-bold text-h6 flex-grow-1" disabled>
            ĐANG CHẠY...
          </v-btn>

          <v-btn color="error" variant="flat" size="x-large" icon="mdi-power" @click="$emit('kill-app')">
          </v-btn>
        </div>

        <v-btn v-else-if="app.isInstalled && !app.isRunning" color="success" variant="flat" size="x-large"
          rounded="pill" prepend-icon="mdi-play" class="font-weight-bold text-h6" block @click="$emit('play')">
          MỞ ỨNG DỤNG
        </v-btn>

        <v-btn v-else color="primary" variant="flat" size="x-large" rounded="pill" prepend-icon="mdi-download"
          class="font-weight-bold text-h6" block @click="$emit('open-install')">
          CÀI ĐẶT NGAY
        </v-btn>
      </v-card-actions>
    </v-card>

  </v-container>
</template>

<script setup>
import { ref, computed } from 'vue';
import { useTheme } from 'vuetify';

// 1. Gán biến props CỰC KỲ QUAN TRỌNG
const props = defineProps(['app']);

// 2. Bổ sung 'kill-app' vào danh sách khai báo
defineEmits(['play', 'open-install', 'kill-app']);

const theme = useTheme();
const isDark = computed(() => theme.global.current.value.dark);

// Tạo biến màu overlay dựa trên theme
const overlayColor = computed(() =>
  isDark.value ? '0, 0, 0' : '255, 255, 255'
);

const showInfoModal = ref(false);

// Dùng props.app chuẩn xác
const isDescLong = computed(() => {
  return props.app?.desc && props.app.desc.length > 130;
});

// Cắt ngắn chuỗi mô tả và thêm dấu "..."
const shortDesc = computed(() => {
  if (!props.app?.desc) return "";
  return props.app.desc.substring(0, 130) + "...";
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
</style>