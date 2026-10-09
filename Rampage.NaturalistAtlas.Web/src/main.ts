/**
 * 应用入口 / Application entry
 * 装配顺序：Pinia（状态）→ vue-router（路由）→ vue-i18n（国际化）→ vue-query（接口缓存）→ Motion（动效）
 * / Wiring order: Pinia (state) → vue-router (routing) → vue-i18n (i18n) → vue-query (API caching) → Motion (animation)
 * 路由就绪后再挂载，保证 useUrlSync 读取到首屏 URL 查询参数。
 * / Mount after the router is ready so useUrlSync can read the initial URL query params.
 */
import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query'
import App from './App.vue'
import router from './router'
import { i18n } from './i18n'
import './styles/main.css'

/** 全局查询客户端：失败重试 1 次、窗口聚焦不自动刷新（地图应用避免无意义重拉） / Global query client: 1 retry on failure, no refetch on window focus (avoids pointless refetches in a map app) */
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false
    }
  }
})

const app = createApp(App)
app.use(createPinia())
app.use(router)
app.use(i18n)
app.use(VueQueryPlugin, { queryClient })

router.isReady().then(() => app.mount('#app'))
