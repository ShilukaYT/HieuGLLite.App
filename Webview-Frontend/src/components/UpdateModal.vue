<template>
  <v-dialog 
    :model-value="modelValue" 
    @update:model-value="$emit('update:modelValue', $event)" 
    max-width="500" 
    persistent 
    transition="dialog-bottom-transition"
  >
    <v-card class="rounded-xl border text-center pa-6" :theme="isDark ? 'dark' : 'light'" elevation="24">
      <v-icon color="primary" size="80" class="mx-auto mb-3">mdi-package-up</v-icon>
      
      <v-card-title class="text-h5 font-weight-bold text-primary text-wrap pb-0">
        CẬP NHẬT PHẦN MỀM
      </v-card-title>
      
      <v-card-text class="text-body-1 mt-2 pa-0" style="line-height: 1.6;">
        Phiên bản mới <b>{{ updateData?.versionName }}</b> đã sẵn sàng!<br>
        Để có trải nghiệm tốt nhất, vui lòng cập nhật ứng dụng.

        <v-card 
          variant="tonal" 
          color="primary" 
          class="mt-5 pa-4 rounded-lg text-left" 
          style="max-height: 200px; overflow-y: auto; border: 1px solid rgba(var(--v-theme-primary), 0.1);"
        >
          <div class="font-weight-bold mb-2 d-flex align-center">
            <v-icon icon="mdi-star-four-points" size="small" class="mr-2"></v-icon>
            Chi tiết thay đổi:
          </div>
          <div class="text-body-2" style="white-space: pre-line;">
            {{ updateData?.changelog || 'Không có thông tin thay đổi nào được ghi nhận.' }}
          </div>
        </v-card>
      </v-card-text>

      <v-card-actions class="justify-center mt-6 pa-0">
        <v-btn 
          v-if="!updateData?.isForceUpdate" 
          color="grey" 
          variant="text" 
          rounded="pill" 
          size="large" 
          class="font-weight-bold mr-2" 
          @click="$emit('update:modelValue', false)"
        >
          ĐỂ SAU
        </v-btn>
        
        <v-btn 
          color="primary" 
          variant="flat" 
          rounded="pill" 
          size="large" 
          class="px-8 font-weight-bold" 
          @click="$emit('download')"
          :disabled="true"
        >
          CẬP NHẬT NGAY
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

<style scoped>
/* Tùy chỉnh thanh cuộn cho khu vực Changelog nhìn xịn hơn */
::-webkit-scrollbar {
  width: 6px;
}

::-webkit-scrollbar-thumb {
  background: rgba(var(--v-theme-primary), 0.3);
  border-radius: 10px;
}

::-webkit-scrollbar-thumb:hover {
  background: rgba(var(--v-theme-primary), 0.5);
}
</style>