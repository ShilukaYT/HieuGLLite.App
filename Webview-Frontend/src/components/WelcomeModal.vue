<template>
    <v-dialog v-model="isOpen" max-width="500" persistent transition="dialog-bottom-transition">
        <v-card class="rounded-xl border text-center pa-6" :theme="isDark ? 'dark' : 'light'" elevation="24">
            <v-img src="./assets/images/logo.png" width="150" class="mx-auto"></v-img>
            <br>
            <v-card-title class="text-h5 font-weight-bold text-info text-wrap pb-0">
                {{ $t('welcome_modal.title') }}
            </v-card-title>

            <v-card-text class="text-body-1 mt-3 pa-2" style="line-height: 1.6;" v-html="$t('welcome_modal.message')">
            </v-card-text>

            <v-card-actions class="justify-center mt-5 pa-0">
                <v-btn color="info" variant="flat" rounded="pill" size="large" class="px-8 font-weight-bold"
                    @click="closeModal">
                    {{ $t('welcome_modal.btn_start') }}
                </v-btn>
            </v-card-actions>

        </v-card>
    </v-dialog>
</template>

<script setup>
import { ref, computed } from 'vue';
import { useTheme } from 'vuetify';
import { useI18n } from 'vue-i18n'; // Khai báo i18n

const { t } = useI18n(); // Gọi hàm t()

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