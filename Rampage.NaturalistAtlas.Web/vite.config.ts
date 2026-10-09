import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    // Tailwind CSS v4 官方 Vite 插件（CSS 优先配置，无需 tailwind.config.js）
    tailwindcss()
  ],
  resolve: {
    alias: {
      // @ 指向 src 目录
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  },
  server: {
    // 与 Rampage.SourcePower 前端（Vite 默认 5173）区分的固定端口；被占用时直接报错而非漂移
    // Fixed port distinct from the SourcePower frontend (Vite default 5173); fail loudly when taken
    port: 5174,
    strictPort: true
    // 后端地址由 .env.* 的 VITE_API_BASE_URL 决定（直连模式，后端开启 CORS）。
    // 若需同源代理：将 VITE_API_BASE_URL 置空并恢复以下配置
    // proxy: {
    //   '/api': { target: 'http://localhost:5214', changeOrigin: true }
    // }
  },
  build: {
    // 地图与图表引擎体积大，单独拆包以利用浏览器长缓存
    chunkSizeWarningLimit: 1500,
    rollupOptions: {
      output: {
        manualChunks: {
          maplibre: ['maplibre-gl'],
          echarts: ['echarts', 'vue-echarts'],
          naive: ['naive-ui']
        }
      }
    }
  }
})
