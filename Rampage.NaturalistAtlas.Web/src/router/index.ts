import { createRouter, createWebHistory } from 'vue-router'
import type { RouteRecordRaw } from 'vue-router'
import { i18n } from '@/i18n'

/**
 * 路由元信息扩展 / Route meta extension
 * - titleKey：页面标题 i18n key，afterEach 拼接为「{标题} · 自然地图志」
 *   / titleKey: i18n key of the page title; afterEach composes "{title} · Naturalist Atlas"
 * - nav：所属顶部导航项（高亮用） / nav: the top-nav section this page belongs to (for highlighting)
 */
declare module 'vue-router' {
  interface RouteMeta {
    titleKey?: string
    nav?: 'explore' | 'overview' | 'species' | 'phenology' | 'gaps'
  }
}

/**
 * 路由表（设计文档 §4.3） / Route table (design doc §4.3)
 *
 * 地图是常驻底座（AppShell 中的 MapCanvas 不随路由销毁），
 * 以下页面均渲染在地图之上的面板中（桌面：左侧浮动面板；移动：底部抽屉）。
 * / The map is the persistent base (MapCanvas in AppShell is never destroyed by routing);
 * the pages below render in panels above the map (desktop: left floating panel; mobile: bottom sheet).
 * 所有页面路由级懒加载。/ All pages are lazy-loaded at the route level.
 */
const routes: RouteRecordRaw[] = [
  {
    path: '/',
    name: 'explore',
    component: () => import('@/views/ExploreView.vue'),
    meta: { titleKey: 'nav.explore', nav: 'explore' }
  },
  {
    path: '/overview',
    name: 'overview',
    component: () => import('@/views/OverviewView.vue'),
    meta: { titleKey: 'overview.title', nav: 'overview' }
  },
  {
    path: '/species',
    name: 'species-list',
    component: () => import('@/views/SpeciesListView.vue'),
    meta: { titleKey: 'species.title', nav: 'species' }
  },
  {
    path: '/species/:id',
    name: 'species-detail',
    component: () => import('@/views/SpeciesDetailView.vue'),
    meta: { titleKey: 'species.title', nav: 'species' }
  },
  {
    path: '/phenology',
    name: 'phenology',
    component: () => import('@/views/PhenologyView.vue'),
    meta: { titleKey: 'phenology.title', nav: 'phenology' }
  },
  {
    path: '/gaps',
    name: 'gaps',
    component: () => import('@/views/GapsView.vue'),
    meta: { titleKey: 'gaps.title', nav: 'gaps' }
  },
  {
    // 兜底 404 / Catch-all 404
    path: '/:pathMatch(.*)*',
    name: 'not-found',
    component: () => import('@/views/NotFoundView.vue'),
    meta: { titleKey: 'notFound.title' }
  }
]

const router = createRouter({
  // HTML5 History 模式：部署到静态服务器时需将全部路径回退到 index.html
  // / HTML5 history mode: a static-server deployment must fall back all paths to index.html
  history: createWebHistory(import.meta.env.BASE_URL),
  routes
})

// 同步页面标题 / Sync the page title
router.afterEach((to) => {
  const t = i18n.global.t
  document.title = to.meta.titleKey ? `${t(to.meta.titleKey)} · ${t('app.name')}` : t('app.name')
})

export default router
