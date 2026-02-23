<template>
  <v-container class="fill-height d-flex align-end pb-12 position-relative" style="z-index: 2;">
    
    <v-card
  class="rounded-xl pa-4"
  :color="isDark ? 'rgba(30, 30, 30, 0.85)' : 'rgba(255, 255, 255, 0.85)'"
  style="backdrop-filter: blur(20px); border: 1px solid rgba(255,255,255,0.1);"
  elevation="12"
>
      <v-card-item>
        <template v-slot:prepend>
          <v-avatar size="80" class="rounded-lg mr-4 elevation-5">
            <v-img :src="app.icon" :alt="app.name"></v-img>
          </v-avatar>
        </template>
        
        <v-card-title class="text-h3 font-weight-black">
      {{ app.name }}
    </v-card-title>
        
        <v-card-subtitle class="text-h6 mt-2 opacity-70">
      {{ app.desc }}
    </v-card-subtitle>
      </v-card-item>

      <v-card-actions class="mt-6 px-4 pb-4">
        <v-btn
          v-if="app.isInstalled"
          color="success"
          variant="flat"
          size="x-large"
          rounded="pill"
          prepend-icon="mdi-play"
          class="font-weight-bold text-h6"
          block
          @click="$emit('play')"
        >
          MỞ GIẢ LẬP
        </v-btn>

        <v-btn
          v-else
          color="primary"
          variant="flat"
          size="x-large"
          rounded="pill"
          prepend-icon="mdi-download"
          class="font-weight-bold text-h6"
          block
          @click="$emit('open-install')"
        >
          CÀI ĐẶT NGAY
        </v-btn>
      </v-card-actions>
    </v-card>

  </v-container>
</template>

<script setup>
defineProps(['app']);
defineEmits(['play', 'open-install']);
import { computed } from 'vue';
import { useTheme } from 'vuetify';
const theme = useTheme();
const isDark = computed(() => theme.global.current.value.dark);

// Tạo biến màu overlay dựa trên theme
const overlayColor = computed(() => 
  isDark.value ? '0, 0, 0' : '255, 255, 255'
);
</script>

<style scoped>
.overlay {
  position: absolute;
  inset: 0;
  z-index: 1;
  /* Sử dụng biến v-bind để đổi màu đen/trắng tùy theo theme */
  background: linear-gradient(
    90deg, 
    rgba(v-bind(overlayColor), 0.7) 0%, 
    rgba(v-bind(overlayColor), 0.1) 70%, 
    transparent 100%
  );
}
</style>