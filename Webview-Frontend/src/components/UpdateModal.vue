<template>
  <v-dialog 
    :model-value="modelValue" 
    @update:model-value="(val) => { if(!updateData?.isForceUpdate && !updateData?.isDownloading && !updateData?.isReady) $emit('update:modelValue', val) }" 
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
          class="mt-5 rounded-lg text-left overflow-hidden" 
          style="border: 1px solid rgba(var(--v-theme-primary), 0.1);"
        >
          <div class="pa-4" style="max-height: 180px; overflow-y: auto;">
            <div class="font-weight-bold mb-2 d-flex align-center">
              <v-icon icon="mdi-star-four-points" size="small" class="mr-2"></v-icon>
              Chi tiết thay đổi:
            </div>
            <div class="text-body-2 pr-2" style="white-space: pre-line; line-height: 1.5;">
              {{ updateData?.changelog || 'Không có thông tin thay đổi nào được ghi nhận.' }}
            </div>
          </div>
        </v-card>

        <v-expand-transition>
          <div v-if="updateData?.isDownloading || updateData?.isReady" class="mt-4">
            <div class="d-flex justify-space-between text-caption font-weight-bold mb-1">
              <span>{{ updateData?.isReady ? 'Tải hoàn tất!' : 'Đang tải bản cập nhật...' }}</span>
              <span>{{ updateData?.percent || 0 }}%</span>
            </div>
            <v-progress-linear 
              :model-value="updateData?.percent || 0" 
              :color="updateData?.isReady ? 'success' : 'primary'" 
              height="8" 
              rounded 
         
              :active="true"
            ></v-progress-linear>
          </div>
        </v-expand-transition>

      </v-card-text>

      <v-card-actions class="justify-center mt-6 pa-0">
        <v-btn 
          v-if="!updateData?.isForceUpdate && !updateData?.isDownloading && !updateData?.isReady" 
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
          v-if="!updateData?.isReady"
          color="primary" 
          variant="flat" 
          rounded="pill" 
          size="large" 
          class="px-8 font-weight-bold" 
          @click="$emit('download')"
          :loading="updateData?.isDownloading"
        >
          CẬP NHẬT NGAY
        </v-btn>

        <v-btn 
          v-else
          color="success" 
          variant="flat" 
          rounded="pill" 
          size="large" 
          class="px-8 font-weight-bold" 
          @click="$emit('install')"
        >
          <v-icon start icon="mdi-restart"></v-icon>
          KHỞI ĐỘNG LẠI & CÀI ĐẶT
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

const emit = defineEmits(['update:modelValue', 'download', 'install']);

const theme = useTheme();
const isDark = computed(() => theme.global.current.value.dark);
</script>

<style scoped>
/* Làm đẹp thanh cuộn */
::-webkit-scrollbar {
  width: 4px; /* Thu nhỏ thanh cuộn lại cho tinh tế */
}

::-webkit-scrollbar-track {
  background: transparent; /* Ẩn nền thanh cuộn */
}

::-webkit-scrollbar-thumb {
  background: rgba(var(--v-theme-primary), 0.2);
  border-radius: 10px;
}

::-webkit-scrollbar-thumb:hover {
  background: rgba(var(--v-theme-primary), 0.5);
}
</style>