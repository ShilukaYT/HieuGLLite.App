<template>
  <v-dialog 
    :model-value="modelValue" 
    @update:model-value="$emit('update:modelValue', $event)" 
    max-width="650" 
    persistent 
    transition="dialog-bottom-transition"
  >
    <v-card class="rounded-xl overflow-hidden"
      :style="{
        background: isDark ? 'linear-gradient(135deg, #1e2a3a 0%, #121212 100%)' : 'linear-gradient(135deg, #ffffff 0%, #f0f2f5 100%)',
        backdropFilter: 'blur(30px)',
        border: isDark ? '2px solid rgba(88, 101, 242, 0.3)' : '2px solid rgba(88, 101, 242, 0.1)',
        boxShadow: isDark ? '0 20px 40px rgba(0,0,0,0.6)' : '0 20px 40px rgba(0,0,0,0.2)'
      }">
      
      <v-card-title class="d-flex flex-column align-center pt-8 pb-4 px-6 text-center">
        <v-avatar color="primary" size="80" class="mb-4 elevation-12">
          <v-icon icon="mdi-rocket-launch" size="48" color="white"></v-icon>
        </v-avatar>
        <div class="text-h4 font-weight-bold text-primary">Đã có bản cập nhật mới!</div>
        <div class="text-subtitle-1 mt-2 opacity-80">
          Phiên bản <v-chip color="primary" label class="font-weight-bold mx-1">{{ updateData?.versionName }}</v-chip> đã sẵn sàng.
        </div>
      </v-card-title>

      <v-card-text class="px-8 pt-2 pb-6">
        <p class="text-body-1 mb-4 text-center">
          Để có trải nghiệm tốt nhất, hãy cập nhật phần mềm!
        </p>

        <v-card 
          flat
          class="pa-5 rounded-lg text-body-2" 
          :style="{ 
            backgroundColor: isDark ? 'rgba(0,0,0,0.2)' : 'rgba(88, 101, 242, 0.05)', 
            border: isDark ? '1px solid rgba(255,255,255,0.1)' : '1px solid rgba(88, 101, 242, 0.1)',
            whiteSpace: 'pre-line',
            maxHeight: '250px',
            overflowY: 'auto'
          }"
        >
          <div class="d-flex align-center font-weight-bold mb-3 text-primary">
            <v-icon icon="mdi-star-four-points" size="small" class="mr-2"></v-icon>
            Chi tiết thay đổi:
          </div>
          {{ updateData?.changelog }}
        </v-card>
      </v-card-text>

      <v-divider class="opacity-20 mx-8"></v-divider>

      <v-card-actions class="pa-6 d-flex justify-space-between align-center">
        <v-btn v-if="!updateData?.isForceUpdate" variant="text" color="grey" class="text-none px-4" @click="$emit('update:modelValue', false)">
          Nhắc tôi sau
        </v-btn>
        
        <v-btn 
          color="primary" 
          size="x-large"
          variant="elevated" 
          class="px-10 rounded-pill text-none font-weight-bold elevation-8" 
          prepend-icon="mdi-cloud-download" 
          @click="$emit('download')"
          style="transition: all 0.3s ease;"
        >
          Tải và Cài đặt ngay
        </v-btn>
      </v-card-actions>
      
    </v-card>
  </v-dialog>
</template>

<script setup>
import { computed } from 'vue';
import { useTheme } from 'vuetify';

const props = defineProps({
  modelValue: Boolean,
  updateData: Object
});

const emit = defineEmits(['update:modelValue', 'download']);

const theme = useTheme();
const isDark = computed(() => theme.global.current.value.dark);
</script>
