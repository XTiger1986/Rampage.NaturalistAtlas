/**
 * Vitest 配置（独立于 vite.config：隔离 Tailwind 插件与构建专属配置）
 * 运行：npm test（单次）/ npm run test:watch（监听）
 */
import { fileURLToPath } from 'node:url'
import { defineConfig } from 'vitest/config'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/__tests__/setup.ts'],
    include: ['src/**/__tests__/**/*.spec.ts']
  }
})
