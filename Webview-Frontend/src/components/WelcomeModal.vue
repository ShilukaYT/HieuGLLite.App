<template>
    <v-dialog v-model="isOpen" max-width="500" persistent transition="dialog-bottom-transition">
        <v-card class="rounded-xl border text-center pa-6" :theme="isDark ? 'dark' : 'light'" elevation="24">
            <v-img src="../assets/images/logo.png" width="150" class="mx-auto"></v-img>
            <br>
            <v-card-title class="text-h5 font-weight-bold text-info text-wrap pb-0">
                CHÀO MỪNG ĐẾN VỚI PUBLIC BETA
            </v-card-title>

            <v-card-text class="text-body-1 mt-3 pa-2" style="line-height: 1.6;">
                Cảm ơn bạn đã tham gia thử nghiệm <b>Hiếu GL Lite</b> phiên bản Public Beta!<br><br>
                Trong giai đoạn này, ứng dụng đang được tối ưu hóa và có thể còn một vài thiếu sót nhỏ. Mọi đóng góp của
                bạn sẽ giúp chúng tôi hoàn thiện sản phẩm tốt hơn.
            </v-card-text>

            <v-card-actions class="justify-center mt-5 pa-0">
                <v-btn color="info" variant="flat" rounded="pill" size="large" class="px-8 font-weight-bold"
                    @click="closeModal">
                    BẮT ĐẦU TRẢI NGHIỆM
                </v-btn>
            </v-card-actions>

        </v-card>
    </v-dialog>
</template>

<script setup>
import { ref, computed } from 'vue';
import { useTheme } from 'vuetify';

const emit = defineEmits(['close']);
const theme = useTheme();
const isDark = computed(() => theme.global.current.value.dark);

const isOpen = ref(true);

const closeModal = () => {
    isOpen.value = false;
    // Đợi animation đóng của Vuetify chạy xong rồi mới báo cho App.vue gỡ component
    setTimeout(() => {
        emit('close');
    }, 300);
};
</script>