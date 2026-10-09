# 自然地图志（Naturalist Atlas）前端设计

| 项 | 内容 |
|---|---|
| 文档版本 | v1.0 |
| 适用代码 | `Rampage.NaturalistAtlas.Web`（前端）、`Rampage.NaturalistAtlas.WebApi`（公开站接口） |
| 关联文档 | 《自然地图志产品文档》v0.3（`documents/产品文档/自然地图志产品文档.md`） |
| 读者 | 前端工程师、后端工程师（§6 接口契约）、设计与测试 |

> 本文是前端开发的"施工图"。骨架代码已全部落地并可构建运行，每个待完善点在代码中以 `TODO(设计文档§x.y)` 标注，与本文章节号一一对应；开发前先读对应章节，完成后删除 TODO 并在 §10 勾选。

---

## 一、技术选型与工程规范

### 1.1 技术栈

| 领域 | 选型 | 版本 | 用途与理由 |
|---|---|---|---|
| 框架 | Vue 3（`<script setup>` + TS） | 3.5 | 团队主栈；组合式 API 便于把地图命令式 API 包进可复用逻辑 |
| 构建 | Vite | 7 | 秒级冷启动；`manualChunks` 拆出 maplibre / echarts / naive 三个大块 |
| 类型 | TypeScript strict | 5.9 | 开启 `noUnusedLocals` / `noUnusedParameters`，`npm run build` 前置 `vue-tsc -b` |
| 地图 | MapLibre GL JS | 5.24 | 开源免 key；单引擎同时支持 mercator / globe 投影、地形、天空、fill-extrusion，2D/2.5D/3D 不需换库 |
| 客户端聚合 | supercluster | 9 | 1 万点建索引 < 100ms，缩放/平移零请求 |
| UI 组件 | naive-ui | 2.45 | 按需引入、TS 友好、`themeOverrides` 可被设计令牌完全驱动 |
| 样式 | Tailwind CSS v4 | 4.2 | `@theme inline` 把 CSS 变量映射为语义类，主题切换零重渲染 |
| 数据请求 | @tanstack/vue-query | 5 | 缓存、去重、`keepPreviousData` 防闪烁、离线禁用 |
| 状态 | Pinia（setup store） | 3 | 城市、筛选、地图视图、主题、UI 五个 store |
| 图表 | ECharts + vue-echarts | 6 / 8 | 物候曲线、类群环图；按需注册（`plugins/echarts.ts`） |
| 工具 | @vueuse/core、@vueuse/motion | 15 / 3 | 响应式断点、防抖、入场动效 |
| 国际化 | vue-i18n（composition 模式） | 11 | 中文优先，英文完整对照 |

### 1.2 目录结构

```
src/
├── api/            http.ts（请求封装 + camelCase 转换）、types.ts（接口契约）、atlas.ts（每个端点一个函数）
├── composables/    queries.ts（全部 vue-query 钩子）、useUrlSync.ts、useResponsive.ts
├── i18n/           zh-CN.ts、en-US.ts（类型受 zh-CN 约束）、index.ts
├── map/            MapEngine.ts、basemaps.ts、viewModes.ts
│   └── layers/     ObservationCardLayer、HeatmapLayer、GlobalTileLayer、CityBoundsLayer、SpeciesFocusLayer、markers.css
├── plugins/        echarts.ts
├── router/         index.ts
├── stores/         theme、city、filter、mapView、ui
├── styles/         main.css（Tailwind 入口 + 全局样式）
├── theme/          tokens.css（设计令牌）、themes.ts、naiveTheme.ts、taxonPalette.ts
├── utils/          format.ts
├── components/
│   ├── layout/     AppShell、TopBar、CitySelect、ThemeSwitcher、SidePanel、MobileSheet
│   ├── map/        MapCanvas、MapHud、ViewModeSwitch、LayerSwitcher、GeoSearchBox、TaxonLegendFilter、TimeSlider
│   ├── common/     TaxonChip、PanelHeader、QueryState、EmptyState、LicenseBadge
│   ├── observation/PhotoCard、PhotoLightbox、ReportDialog
│   ├── species/    SpeciesRankItem、PhotoWall、EmergenceChart
│   ├── phenology/  PhenologyMatrix
│   └── stats/      OverviewCards、TaxonDonut
└── views/          Explore、Overview、SpeciesList、SpeciesDetail、Phenology、Gaps、NotFound
```

### 1.3 编码规范

1. **注释**：每个文件顶部写用途与关键约定；导出函数/组件 props 写 JSDoc；非显然的约束（阈值、口径、兼容性）就地说明。
2. **取数只走 `composables/queries.ts`**：组件禁止直接调用 `api/atlas.ts`；queryKey 统一 `[领域, 动作, 参数对象]`。
3. **颜色只走令牌**：组件内禁止硬编码色值，用 Tailwind 语义类（§3.4）或 `var(--c-*)`；类群色只从 `taxonPalette.ts` 取。
4. **地图只走 `MapEngine` 与图层类**：Vue 组件不直接持有 `maplibregl.Map`；唯一的胶水层是 `MapCanvas.vue`（store → 引擎命令，moveend → store）。
5. **自有地图资源 id 统一 `atlas-` 前缀**，避免与底图样式冲突。
6. **照片出现处必带 `LicenseBadge`**（产品文档 §9 版权要求）。
7. **i18n**：界面文案一律 `t('key')`；新增 key 同时补 `zh-CN.ts` 与 `en-US.ts`（en-US 类型受约束，漏写会编译失败）。
8. **提交前**：`npm run build` 必须通过（含类型检查）。

### 1.4 环境变量与命令

| 变量 | 默认 | 说明 |
|---|---|---|
| `VITE_API_BASE_URL` | `http://localhost:5214` | WebApi 基地址（开发期跨域直连，后端 CORS `AllowLocalhost`） |
| `VITE_TIANDITU_KEY` | 空 | 天地图 key；为空则底图列表不出现天地图 |

```bash
npm install --include=dev   # 全局 npm 配置了 omit=dev 时必须带 --include=dev
npm run dev                 # http://localhost:5174（固定端口，区别于 SourcePower 前端）
npm run build               # vue-tsc -b && vite build
```

---

## 二、底图选型与合规

### 2.1 推荐的免费公开底图服务

| id | 服务 | 类型 | key | 坐标系 | 用途 |
|---|---|---|---|---|---|
| `ofm` | **OpenFreeMap**（OpenMapTiles schema） | 矢量 | 无需、无限额 | WGS-84 / Web 墨卡托 | **默认**。三主题各用一套官方样式：journal→`positron`、night→`dark`、spectrum→`liberty`，再用 `transformStyle` 打主题补丁；自带 `building` 图层供 2.5D/3D 挤出 |
| `tianditu` | **天地图**（国家地理信息公共服务平台） | 栅格 WMTS（`vec_w` + `cva_w`） | 免费申请 | CGCS2000 ≈ WGS-84 | 面向中国大陆公众上线的合规底图（带审图号）；配置 `VITE_TIANDITU_KEY` 后出现 |
| `imagery` | **Esri World Imagery** | 栅格影像 | 无需 | Web 墨卡托 | 卫星影像；必须保留署名 |
| DEM | **AWS Terrain Tiles**（Terrarium 编码） | raster-dem | 无需 | Web 墨卡托 | 3D 地形与山体阴影 |
| 全球视角 | **iNaturalist v1 瓦片** `api.inaturalist.org/v1/heatmap/{z}/{x}/{y}.png` | 栅格叠加 | 无需 | Web 墨卡托 | 全球观察热力（**仅 v1 提供瓦片**，v2 没有） |
| 全球视角 | **GBIF v2 密度瓦片** `api.gbif.org/v2/map/occurrence/density/{z}/{x}/{y}@1x.png` | 栅格叠加 | 无需 | Web 墨卡托 | 全球记录密度（内置 greenHeat / orangeHeat / blueHeat 配色，按主题选用） |
| 物种分布 | **iNaturalist v1** `api.inaturalist.org/v1/taxon_ranges/{id}/{z}/{x}/{y}.png` | 栅格叠加 | 无需 | Web 墨卡托 | 物种详情页全球分布范围（使用源站 taxon id） |

> 备选（未接入，按需评估）：MapTiler / Stadia（免费额度需 key，样式更丰富）、Protomaps（自托管 PMTiles，零外部依赖）。

### 2.2 选型原则与合规

1. **坐标一致性优先**：观察点为 WGS-84，底图必须是 WGS-84/CGCS2000 系，**禁止**高德/腾讯等 GCJ-02 底图（会产生 100~700 米系统偏移）。
2. **中国大陆公开上线须用带审图号底图**：生产环境对中国大陆访客默认切到天地图，并在署名补充当年审图号（`basemaps.ts` 已留注释位）；OpenFreeMap 用于开发与海外访问。
3. **署名**：MapLibre `AttributionControl` 常驻右下角（紧凑模式），所有数据源署名不可移除。
4. **流量**：全球瓦片由浏览器直连源站 CDN，不经后端、不占采集端请求预算。

---

## 三、三套视觉方案（全部实现，可切换）

主题通过 `<html data-theme="journal|night|spectrum">` 切换，由 `stores/theme.ts` 写入并持久化到 `localStorage['na.theme']`。切换时同时作用于：CSS 令牌 → naive-ui `themeOverrides` → 矢量底图补丁 → 栅格底图调色 → 天空/山体阴影 → 类群调色板 → 照片卡与聚合簇样式。

### 3.1 方案 A · 博物志·纸本（`journal`）

- **气质**：自然博物馆标本柜与野外手账——暖米纸、墨线、矿物颜料色，安静耐读，适合教师投屏与长时间阅读。
- **界面**：不透明纸片面板 + 1px 墨线描边、几乎无阴影；标题用宋体（Noto Serif SC）；圆角 2~6px；背景叠极轻 SVG 噪点纸纹。
- **地图**：OpenFreeMap `positron` + 米色陆地 / 灰青水体 / 橄榄绿地补丁；影像底图降饱和 + 暖色偏移，像旧地图集。
- **照片卡**：白色"标本签"——照片下方一条朱砂红标签线，写物种名与学名（斜体）；聚合簇为墨线环。

### 3.2 方案 B · 夜观·萤火（`night`，默认）

- **气质**：夜观自然、萤火虫与头灯——深林夜色里的点点荧光，第一眼的"惊艳感"最强，契合"照片卡片地图是第一印象"。
- **界面**：玻璃拟态面板（`backdrop-filter: blur(18px)`、62% 透明墨绿）、萤火绿强调色 `#7cf2b0`；圆角 8~18px。
- **地图**：OpenFreeMap `dark` + 墨绿陆地 / 深青水体补丁；影像底图压暗降饱和；3D 模式为深青天空与夜雾。
- **照片卡**：圆角照片卡 + 类群色光晕（`--glow`），聚合簇外环为类群构成色环并带呼吸动画（`firefly-breath`，`prefers-reduced-motion` 时关闭）。

### 3.3 方案 C · 四季·光谱（`spectrum`）

- **气质**：明亮通透的季节杂志——大照片、柔和阴影，强调色随时间滑块月份在 12 色光谱间流转（1 月冰蓝 → 4 月樱粉 → 7 月盛夏绿 → 10 月枫橙）。
- **界面**：白色半透明面板（82% + blur 14px）、大圆角 10~24px、无描边柔阴影。
- **地图**：OpenFreeMap `liberty` + 浅灰陆地 / 明亮水体 / 嫩绿补丁；3D 模式为晴空蓝天。
- **实现要点**：`SEASON_ACCENTS[12]`（`theme/themes.ts`）；`stores/theme.ts` 监听时间滑块月份写入 `--c-accent` 并同步 naive 主色。

### 3.4 设计令牌表

组件使用 Tailwind 语义类（左列），不直接写变量：

| 语义类 | CSS 变量 | journal | night | spectrum |
|---|---|---|---|---|
| `bg-bg` | `--c-bg` | `#efe6d2` | `#0b1512` | `#f7f9fb` |
| `bg-surface` | `--c-surface` | `#fbf7ee` | `#14231f` | `#ffffff` |
| `bg-surface-2` | `--c-surface-2` | `#f3ecdc` | `#1b2e29` | `#f1f4f7` |
| `bg-glass` / `.glass-panel` | `--c-surface-glass` + `--panel-blur` | 96% / 0 | 62% / 18px | 82% / 14px |
| `border-line` | `--c-line` | `#d6c7a6` | 14% 薄荷 | `#e3e8ee` |
| `text-ink` | `--c-ink` | `#2b2118` | `#e7f3ec` | `#17202b` |
| `text-ink-2` | `--c-ink-2` | `#5a4a38` | `#a9c2b6` | `#4a5665` |
| `text-ink-3` | `--c-ink-3` | `#8c7a62` | `#6f8a7e` | `#8a96a5` |
| `text-accent` / `bg-accent` | `--c-accent` | `#7a4f2a` 赭石 | `#7cf2b0` 萤火绿 | 随月份（默认 `#2f9e5b`） |
| `bg-accent-soft` | `--c-accent-soft` | 12% 赭石 | 14% 萤火绿 | 14% 当月色 |
| `rounded-chip` / `rounded-card` / `rounded-panel` | `--radius-sm/md/lg` | 2 / 4 / 6px | 8 / 12 / 18px | 10 / 16 / 24px |
| `font-display` | `--font-display` | Noto Serif SC | Noto Sans SC | Noto Sans SC |
| — | `--panel-shadow` / `--card-shadow` | 纸片投影 | 深色长投影 | 柔和投影 |
| — | `--card-border` / `--card-label-line` | 墨线 / 朱砂 | 薄荷细线 / 无 | 无 / 无 |
| — | `--cluster-ring` / `--glow` | 墨色 / 无 | 白 / 光晕 | 白 / 无 |

共享令牌：`--topbar-h: 56px`、`--panel-w: 420px`、层级 `--z-map/overlay/panel/topbar/modal = 0/10/20/30/50`、动效 `--ease-out`、`--dur-fast/base/slow = 150/260/480ms`。

### 3.5 类群调色板

`theme/taxonPalette.ts` 是全站唯一类群色源，代码与后端 `IconicTaxon` 一致。三主题共享色相序列、只调明度/饱和度，保证"图例即筛选器"跨主题语义一致：

| 代码 | 类群 | 字形 | journal（矿物色） | night（荧光色） | spectrum（标准色） |
|---|---|---|---|---|---|
| 1 | 鸟类 Birds | 鸟 | `#3d6e9e` | `#5eb3ff` | `#3b82f6` |
| 2 | 哺乳 Mammals | 兽 | `#8b5a3c` | `#e8a877` | `#a0522d` |
| 3 | 两栖 Amphibians | 蛙 | `#3f8f86` | `#3ee6c8` | `#14b8a6` |
| 4 | 爬行 Reptiles | 蜥 | `#7d8b3a` | `#b6f24a` | `#65a30d` |
| 5 | 辐鳍鱼 Ray-finned Fishes | 鱼 | `#4f5b93` | `#8b8cff` | `#6366f1` |
| 6 | 昆虫 Insects | 虫 | `#c8702a` | `#ffa24a` | `#f97316` |
| 7 | 蛛形 Arachnids | 蛛 | `#a8423c` | `#ff5f7a` | `#e11d48` |
| 8 | 软体 Mollusks | 螺 | `#b8913a` | `#ffd35a` | `#eab308` |
| 9 | 植物 Plants | 植 | `#4d8a4a` | `#5cf28a` | `#22c55e` |
| 10 | 真菌 Fungi | 菌 | `#a6537a` | `#ff6ec7` | `#ec4899` |
| 11 | 其他动物 | 动 | `#7b7d80` | `#b3c2d6` | `#64748b` |
| 12 | 原生动物 | 原 | `#6d5a92` | `#b58cff` | `#8b5cf6` |
| 13 | 色藻 Chromista | 藻 | `#8a6d3b` | `#d9a55a` | `#a16207` |
| 14 | 未知 | ? | `#8c8c8c` | `#8894a6` | `#94a3b8` |

图例只展示 1~13；14 仅用于兜底着色。

### 3.6 新增主题的步骤

1. `themes.ts` 的 `ThemeId` 与 `THEMES` 增加条目；2. `tokens.css` 新增 `[data-theme='x']` 块（变量必须齐全）；3. `taxonPalette.ts` 每个类群补 `colors.x`；4. `basemaps.ts` 补 `OFM_STYLE` / `PATCH` / `rasterPaint`；5. `viewModes.ts` 补 `skyOf`；6. `naiveTheme.ts` 与 `GlobalTileLayer` 的 GBIF 配色补分支；7. i18n 补主题名。

---

## 四、应用骨架与地图内核

### 4.1 地图引擎（`map/MapEngine.ts`）

单例 `mapEngine` 封装一个 `maplibregl.Map`，对外只暴露命令式 API：

| 方法 | 说明 |
|---|---|
| `init({ container, center, zoom, theme, basemap, mode })` | 创建地图、导航/比例尺/署名控件 |
| `setTheme(theme)` / `setBasemap(id)` | 重新 `setStyle(style, { diff: false, transformStyle })`；矢量底图在 `transformStyle` 中打主题补丁 |
| `setMode(mode)` | 调用 `applyViewMode`（§4.2） |
| `addLayer(layer)` / `removeLayer(id)` | 注册实现 `AtlasLayer { id, mount(map), unmount(map), setTheme?(theme) }` 的图层 |
| `onReady(cb)` | 首次样式加载完成回调 |
| `fitBBox(bbox, animate?)` / `flyTo(center, zoom)` / `getBBox()` | 相机控制 |
| `destroy()` | 卸载全部图层并销毁地图 |

**样式重载恢复**：换主题/底图会整体替换样式，自有 source/layer 全部丢失。引擎在每次 `style.load` 后依次：重新 `applyViewMode(mode, animate=false)` → 按注册顺序对每个图层 `mount(map)` 与 `setTheme(theme)`。因此**图层类必须保证 `mount` 幂等**且把状态（数据、可见性、筛选）保存在自身字段里。

### 4.2 视图模式状态机（`map/viewModes.ts`）

| 模式 | 投影 | 倾角 | 地形 DEM | 山体阴影 | 建筑挤出 | 天空/大气 | 旋转 |
|---|---|---|---|---|---|---|---|
| `2d` | mercator | 0（动画后 `setMaxPitch(0)` 锁定） | 关 | 关 | 关 | 关 | 禁用 |
| `2.5d` | mercator | 55° | 关 | 关 | 开（zoom ≥ 13 淡入） | 关 | 允许 |
| `3d` | globe | zoom ≥ 6 时 60°，否则 0 | 开（夸张 ×1.4） | 开 | 开 | 开（按主题） | 允许 |

- 任意两态之间可直接切换（全连通），相机 `easeTo` 1200ms；样式重载后的恢复调用 `animate=false`。
- `3d` 的 globe 投影在高缩放时自动过渡为墨卡托观感，所以"地球仪 → 城市地形"是连续的，不另设"地球仪"模式。
- 建筑挤出依赖 OpenMapTiles `building` 图层，**栅格底图（天地图/影像）下 2.5D 只有倾斜无建筑**，`ViewModeSwitch` 需提示。
- 自有面/栅格图层插在第一个 symbol 图层之下（`firstSymbolLayerId`），保证地名注记在最上。
- 模式持久化在 `mapView.mode`（`localStorage['na.viewMode']`），首次访问默认 `2d`；低端移动设备（`navigator.hardwareConcurrency <= 4`）切到 3D 时建议提示耗电（待开发）。

### 4.3 布局与路由

**布局（`AppShell.vue`）**：地图常驻全屏底层；TopBar（56px，城市切换 / 导航 / 主题 / 语言）悬浮顶部；页面内容以**侧栏**（桌面 ≥ 1024px，宽 420px，玻璃面板，可收起）或**底部抽屉**（移动端，三档吸附，§7.2）叠加在地图上；MapHud（图例筛选、时间滑块、视图模式、图层开关、地点搜索）悬浮于地图四角。切换路由不重建地图。

| 路由 | 名称 | 视图 | 地图联动 |
|---|---|---|---|
| `/` | explore | ExploreView：本视野观察卡片列表 | 照片卡层 + 热力；列表悬停高亮地图卡片 |
| `/overview` | overview | OverviewView：城市总览 | 保持探索图层 |
| `/species` | species | SpeciesListView：物种榜 | 保持探索图层 |
| `/species/:id` | species-detail | SpeciesDetailView | SpeciesFocusLayer 高亮该种分布 + iNat 分布范围 |
| `/phenology` | phenology | PhenologyView：物候矩阵 | 点击矩阵行跳转物种详情 |
| `/gaps` | gaps | GapsView（P2 占位） | — |
| `/:pathMatch(.*)*` | not-found | NotFoundView | — |

路由 meta：`titleKey`（i18n 页面标题）、`nav`（是否出现在顶部导航）。

### 4.4 图层栈

自下而上（注册顺序即叠放顺序）：

1. 底图（矢量/栅格）+ 山体阴影 + 建筑挤出（插在 symbol 之下）
2. `CityBoundsLayer`：城市范围外半透明遮罩（带洞多边形）+ 虚线边框
3. `GlobalTileLayer`：iNat / GBIF 全球瓦片（全球视角时显现）
4. `HeatmapLayer`：本地观察热力（按类群色加权）
5. `SpeciesFocusLayer`：物种详情页的分布点 + 分布范围瓦片
6. `ObservationCardLayer`：照片卡与聚合簇（HTML Marker，始终在 canvas 之上）

### 4.5 状态管理与 URL 同步

| store | 职责 | 持久化 |
|---|---|---|
| `theme` | `theme`、`seasonMonth`、`accentOverride`、naive 主题对象；watch 写 `data-theme`、`--c-accent`、meta theme-color | `na.theme` |
| `city` | 城市列表（后端 `/geo/cities`；失败回落内置 `FALLBACK_CITIES`，id 为负=离线）、`currentSlug`、`current`、`currentId`、`offline` | URL `city` |
| `filter` | `iconicTaxa`、`monthRange`、`quality`（默认 `[1,2]`）、派生 `query`（含 cityId） | URL `t` `m` `q` |
| `mapView` | `mode`、`basemap`、`globalProvider`、`overlays`（cards/heatmap/global/cityBounds）、`bounds`、`zoom`、`inGlobalView`、`ready` | mode/basemap/provider/overlays |
| `ui` | `panelOpen`、`sheetSnap`、`lightboxId`、`reportId`、`hoverId` | 否 |

URL 参数（`useUrlSync.ts`，`router.replace` 不产生历史）：`city=beijing`、`t=1,6,9`、`m=3-5`（全年省略，可跨年 `11-2`）、`q=1`（默认 `1,2` 省略）。首次进入 URL → store，之后 store → URL。

**离线模式**：`/geo/cities` 失败时使用内置城市（负 id），所有数据查询 `enabled=false`，面板显示"后端未连接"空态，地图、底图、全球瓦片仍可用——方便前端独立开发。

### 4.6 性能预算

| 项 | 预算 | 手段 |
|---|---|---|
| 首屏可交互 | ≤ 2s（4G） | 路由懒加载；maplibre/echarts/naive 独立 chunk；地图先渲染底图再灌点位 |
| 点位接口 | 单次 ≤ 8000 点（最大 10000），gzip 后 ≈ 400KB | 超限返回 `truncated`，前端提示"放大查看更多" |
| 聚合索引 | 1 万点建索引 < 100ms | supercluster（radius 64，maxZoom 16） |
| 单屏 Marker | ≤ 400 | 按视野 diff 增删 HTML Marker，复用 DOM |
| 帧率 | 平移缩放 ≥ 50fps | moveend 才重绘 Marker；热力为 GPU 图层 |
| 请求去重 | 同筛选 2~10 分钟内不重复请求 | vue-query staleTime：明细 2 分钟、统计 10 分钟（与后端缓存一致） |

---

## 五、功能详细设计

### 5.1 地图探索 · 照片卡（P0，`ObservationCardLayer`、`ExploreView`、`PhotoCard`）

**已实现**：城市级点位一次拉取（`/observation/points`）→ supercluster 聚合 → 聚合簇（数量 + 类群构成 conic-gradient 色环）/ 照片卡（方图 + 类群色边）；zoom ≥ 15 展开为带物种名的大卡片；点击簇缩放到展开级别，点击卡片开大图；侧栏列出当前视野内观察卡片（`/observation/list` + bbox），悬停列表高亮地图卡片。

**待开发**（`TODO(设计文档§5.1)`）：

1. **下拉加载更多**：`useObservationList` 改为 `useInfiniteQuery`，`getNextPageParam = pager.pageIndex < pager.totalPage ? pageIndex + 1 : undefined`；列表底部放 `IntersectionObserver` 哨兵（`useIntersectionObserver`），每页 30 条；视野变化（`mapView.bounds` 防抖 400ms）时重置。
2. **按距离地图中心排序**：纯前端对当前页排序（Haversine），开关放列表头；不改接口。
3. **截断提示**：`pointResult.truncated` 为真时在 MapHud 顶部显示"当前条件共 N 条，地图显示最新 8000 条，放大或加筛选查看更多"。
4. **卡片入场动效**：新增 Marker 使用 `scale(.6)→1` + 透明度 180ms（`--ease-out`），`prefers-reduced-motion` 时跳过。

**验收**：聚合簇缩放散开流畅无闪烁；卡片含照片、物种名（中/英/学名）、观察者、拍摄时间、版权署名；类群切换后地图与列表 1 秒内响应。

### 5.2 类群图例筛选（P0，`TaxonLegendFilter`、`stores/filter.ts`）

**已实现**：13 个类群芯片（字形 + 类群色），单击切换多选，双击"只看该类"（`soloTaxon`），"全部"复位；总览页类群环图点击扇区同样调用 `soloTaxon`；筛选写入 URL `t`。

**待完善**：触屏无双击习惯，移动端改为长按 500ms 触发"只看该类"（`onLongPress`，@vueuse/core）。

**交互规则**：空选 = 全部；多选时未选类群芯片置灰 40%；与月份区间、鉴定级别组合（AND），任一变化触发点位与列表重新查询（`keepPreviousData` 避免闪空）。移动端芯片横向滚动，仅显示字形。

### 5.3 双视角 · 全球瓦片（P0，`GlobalTileLayer`）

**已实现**：`zoom < min(GLOBAL_ZOOM(8), 城市完整显示级别 − 1.5)` 或视野中心离开当前城市 bbox 时进入全球视角（相对阈值保证北京、重庆这类大城市完整显示时——级别本身就 < 8——仍是城市视角）——显示 iNat v1 热力（或 GBIF v2 密度）瓦片，同时隐藏本地照片卡/热力；回到城市内自动切回。筛选（类群 → `iconic_taxa`、月份 → `month`、鉴定级别 → `quality_grade`）同步为瓦片 URL 参数；GBIF 按主题选配色。

**待完善**：

1. **渐变衔接**：进入/退出全球视角时对 `raster-opacity` 做 400ms 过渡（`raster-opacity-transition`），本地 Marker 同步淡出。
2. **全球视角提示条**：MapHud 显示"全球视角 · 数据来自 iNaturalist/GBIF 官方瓦片 · 点击回到 {城市}"。
3. **源站异常降级**：瓦片 `error` 事件累计 > 5 次时隐藏全球层并提示，避免满屏破图。

### 5.4 时间滑块 ·「城市苏醒」（P1，`TimeSlider`）

**已实现**：1~12 月双柄区间（支持跨年，如 11~2）、播放/暂停（从起始月逐月推进，每月停留 1.8s，`PLAY_INTERVAL`）；播放中写 `filter.monthRange`，区间起始月同步 `theme.seasonMonth`（光谱主题强调色随月变化）；点位按月缓存，第二轮播放零请求。

**待开发**（`TODO(设计文档§5.4)`）：

1. **逐旬播放档位**：档位 `月 | 旬`；旬模式前端按 `d1/d2` 日期窗查询（当年 1~36 旬），点位接口已支持 `d1/d2`。
2. **季节剧本文案**：每月一条（3 月山樱与早蝶 → 4 月候鸟过境 → 6 月萤火虫 → 10 月真菌季…），放 i18n `season.script[1..12]`，播放时在滑块上方以字幕形式淡入。
3. **投屏模式**：全屏按钮 + 隐藏侧栏 + 放大字幕，便于教师课堂演示（US-05）。
4. 播放时预取下一月点位（`queryClient.prefetchQuery`），保证切换无等待。

### 5.5 城市物种榜（P0，`SpeciesListView`、`SpeciesRankItem`）

**已实现**：按记录数/观察者数排序、物种名关键字搜索（防抖 300ms）、组合筛选联动、分页；条目显示代表图、中/英/学名、类群芯片、受威胁标记、记录数/观察者数、最近观察日期；点击进入物种详情。

**待开发**（`TODO(设计文档§5.5)`）：

1. **稀缺物种榜**：新增排序 `order=rare`（记录数升序、观察者最少，且 research ≥ 1 以排除误鉴定）——后端 `ObservationDao.SelectSpeciesRankPage` 增加分支即可。
2. **榜单导出**：导出当前筛选下前 500 条为 CSV（前端生成，字段：学名、中文名、类群、记录数、观察者数、最近日期）。
3. **数据窗口说明**：榜单头注明"统计口径：{城市} · {鉴定级别} · {月份区间}"（产品文档 §4.3）。

### 5.6 物种详情 · 照片墙（P0，`SpeciesDetailView`、`PhotoWall`、`SpeciesFocusLayer`）

**已实现**：分类档案（中/英/学名、类群、受威胁）、本市记录数/观察者数/首末次日期、照片墙（最新 60 张有图记录）、物候曲线（§5.7）、地图聚焦该物种本市分布点 + iNat 全球分布范围瓦片（有源站 taxon id 时）。

**待开发**（`TODO(设计文档§5.6)`）：

1. **瀑布流 + 无限滚动**：照片墙改用 `/observation/list?taxonId=&cityId=` 分页（每页 30），CSS `columns` 瀑布流（桌面 3 列、移动 2 列），哨兵触发下一页。
2. **分类阶元面包屑**：`taxon.ancestry` 为源站 id 链，需后端补 `GET /api/taxon/{id}/ancestors`（返回各级学名/中文名）后渲染"界 › 门 › 纲 › 目 › 科 › 属"。
3. **最佳观察时间文案**：由物候曲线计算峰值旬/月与占比 ≥ 10% 的连续区间，生成"最常在 4 月中旬 ~ 5 月下旬被记录"（前端纯计算，注明样本量）。
4. **相似物种推荐**：同属且本市有记录的物种（需后端接口，P2）。

### 5.7 物候曲线与物候矩阵（P0/P1，`EmergenceChart`、`PhenologyMatrix`、`PhenologyView`）

**物候曲线（已实现）**：`mode=detail` 近两年逐旬 36 点 / `mode=history` 全历史逐月 12 点；"仅研究级"开关；ECharts 面积折线，颜色取类群色；图下常驻口径说明与样本量（防止把观察热度误读为物候）。

**物候矩阵（已实现）**：Top 30 物种 × 12 月热力格（CSS Grid，单元格透明度 = 该月数 / 该行峰值），行头类群色点 + 名称，点击行进入物种详情；受类群/鉴定级别筛选影响、不受月份筛选影响。

**待开发**（`TODO(设计文档§5.7)`）：

1. **列头排序**：点击月份列头按"该月占比"降序；新增"按峰值月排序"（把全年节奏排成对角线，直观呈现季节剧本）。
2. **Top N 选择**：30 / 50 / 100（接口 `top` 参数已支持）。
3. **虚拟滚动**：行数 > 200 时启用（`@vueuse/core` `useVirtualList`）。
4. **导出 PNG**：`html-to-image` 或 canvas 重绘，供教师投屏（需评估引入依赖）。

### 5.8 城市总览（P0，`OverviewView`、`OverviewCards`、`TaxonDonut`）

**已实现**：指标卡（总记录、研究级数、物种数、观察者数、本月新增）、类群构成环图（点击扇区 = 只看该类群）、本月新星列表（本月首次累计达到 ≥ 5 条 research 级记录的物种）、活跃观察者 Top 10、数据新鲜度（最新源站发布时间相对时间）、"仅研究级"切换。

**待开发**（`TODO(设计文档§5.8)`）：

1. **近 12 个月趋势迷你图**：需后端在 overview 增加 `monthly12: [{ month: 'YYYY-MM', count }]`（参考管理端 `SelectMonthlyDistribution`，加 city_id 条件）。
2. **本月新星首次达标日期**：新星卡片已横向滚动展示；待补"x 月 x 日达标"——后端在新星查询中补 `reachedOn`（研究级第 5 条记录的 observed_on，用窗口函数 `ROW_NUMBER()` 求得）。
3. 数字滚动入场动效（`@vueuse/motion`）。

### 5.9 集邮缺口（P2，`GapsView` 占位）

规划实现：

1. 后端新增 `GET /api/gaps?cityId=&iconic=`：采集端定期调用 iNat `/v1/taxa/{id}/wanted`（或 `species_counts` 对比周边区域）写入缓存表 `city_wanted_taxon`，接口只读缓存，不在请求链路直连源站；
2. 前端列表：代表图 + 名称 + "周边已记录 N 次" + "去 iNaturalist 记录"外链；
3. 按类群筛选，与图例联动；
4. 空态文案鼓励"成为本城第一位记录者"。

### 5.10 大图与举报（P0，`PhotoLightbox`、`ReportDialog`）

**已实现**：大图（large 1024）+ 物种名、观察者、时间（按访客时区格式化）、鉴定级别、授权署名、坐标保护/受威胁提示（"位置经保护性模糊"）、"在地图上定位"、源站原文链接；举报对话框（原因 5 选 1、补充说明 ≤ 500、联系方式 ≤ 100；选"其他"必须填说明），每次打开生成 `Idempotency-Key`，后端业务校验文案直接提示。

**待完善**：键盘左右切换同列表上一张/下一张；图片加载占位用方图模糊放大（LQIP）；举报成功后本地标记"已举报"避免重复。

### 5.11 地点搜索与城市切换（P0，`GeoSearchBox`、`CitySelect`）

**已实现**：输入防抖 300ms 调 `/geo/search`，结果分组（城市 / 省份 / 国家）显示层级与上级名称；选城市 → `city.setCity(slug)` 并 `fitBBox`；选省份/国家 → `flyTo(center)`（国家 zoom 4、省份 zoom 6）。城市切换器为可搜索下拉（英文界面显示英文名），后端未连接时旁边显示黄点提示"离线城市列表"。

**待完善**：键盘上下选择与回车确认；最近搜索（localStorage 5 条）；搜索无结果时提示"暂未收录，欢迎反馈"；城市多于 30 个后下拉按国家分组（naive `NSelect` 的 `type: 'group'` 选项）。

---

## 六、接口契约

基址 `${VITE_API_BASE_URL}/api`。后端 JSON 为 **PascalCase**（Rampage 框架约定，幂等回放缓存也按 PascalCase 序列化），`api/http.ts` 在解包前**递归转换为 camelCase**，前端类型（`api/types.ts`）一律 camelCase。后端 DTO 位于 `Rampage.NaturalistAtlas.Model/Dto/Public/`，两侧字段一一对应，改一侧必须同步另一侧与本节。

### 6.1 通用约定

- 响应：`{ Code, Message, Data }`，`Code = 0` 成功；业务失败 `Code = 1000`（`Message` 为可直接展示的中文文案）；前端 `ApiError.code`：-1 网络、< 600 HTTP、≥ 1000 业务。
- 分页：`{ Pager: { PageIndex, PageSize, RecordCount, TotalPage }, DataList }`。
- 枚举一律数字：数据源 1=iNat 2=GBIF；类群 1~14；鉴定级别 1=research 2=needs_id 3=casual；坐标保护 0/1/2；授权 0~8（8=PDM）；举报原因 0~4。
- 日期：`observedOn` 为观察者本地日期 `YYYY-MM-DD`；时刻类（`observedAt`、`newestSourceCreatedAt`）为 ISO 8601 UTC（带 `Z`）。
- 请求头：`Accept-Language`（zh-CN / en-US）、`X-Timezone`（IANA）、POST 带 `Idempotency-Key`。
- 数组参数逗号分隔，非法值静默忽略。

### 6.2 组合筛选参数（`AtlasObservationQuery`）

| 参数 | 类型 | 说明 |
|---|---|---|
| `cityId` | int | 城市 id |
| `bbox` | `"w,s,e,n"` | 视野矩形（不支持跨日界线） |
| `iconic` | `"1,6,9"` | 类群代码 1~14 |
| `monthFrom` / `monthTo` | 1~12 | 月份区间；`from > to` 表示跨年（11~2）；1~12 视为不筛选 |
| `quality` | `"1,2"` | 鉴定级别，缺省 `1,2` |
| `d1` / `d2` | `YYYY-MM-DD` | 观察日期窗（含两端） |
| `taxonId` | string | 限定物种 |

### 6.3 端点

| 方法 | 路径 | 额外参数 | 返回 `Data` | 服务端缓存 |
|---|---|---|---|---|
| GET | `/geo/cities` | — | `CityItem[]` | 10 分钟 |
| GET | `/geo/search` | `keyword`、`limit`(12, ≤30) | `GeoSearchItem[]` | — |
| GET | `/observation/points` | 筛选 + `limit`(8000, ≤10000) | `ObservationPointResult` | 5 分钟 |
| GET | `/observation/list` | 筛选 + `pageIndex`、`pageSize`(24, ≤60)、`photoOnly`(false) | `PageData<ObservationCard>` | — |
| GET | `/observation/{id}` | — | `ObservationDetail` | — |
| GET | `/species/rank` | 筛选 + `order`(record/observer/rare)、`keyword`、`pageIndex`、`pageSize`(30, ≤100) | `PageData<SpeciesRankItem>` | 5 分钟 |
| GET | `/species/{id}` | `cityId` | `SpeciesDetail` | 5 分钟 |
| GET | `/species/{id}/emergence` | `cityId`、`mode`(detail/history)、`researchOnly` | `EmergenceResult` | 10 分钟 |
| GET | `/species/{id}/similar` | `cityId` | `SpeciesRankItem[]` | 10 分钟 |
| GET | `/taxon/{id}/ancestors` | — | `TaxonAncestor[]` | 10 分钟 |
| GET | `/gaps` | `cityId`、`iconic` | `WantedTaxonItem[]` | 10 分钟 |
| GET | `/phenology/matrix` | 筛选（忽略月份）+ `top`(30, 5~100) | `PhenologyMatrix` | 10 分钟 |
| GET | `/stats/overview` | `cityId`、`researchOnly` | `CityOverview` | 10 分钟 |
| POST | `/report` | JSON `ReportInput` + `Idempotency-Key` | 举报 id（string） | — |

### 6.4 数据结构

```ts
CityItem        { id, name, nameEn, slug, countryId, countryName, provinceName, bbox: [swLng, swLat, neLng, neLat], center: [lng, lat] }
GeoSearchItem   { level: 'country'|'province'|'city', id, name, nameEn, parentName, center, bbox|null, slug|null }
ObservationPoint{ id, lng, lat, iconicTaxon, qualityGrade, taxonId, name, photoSquareUrl, license, observedOn }
ObservationPointResult { points, total, truncated }
ObservationCard { id, source, taxonId, scientificName, commonNameCn, commonNameEn, iconicTaxon, qualityGrade,
                  observedOn, observedAt, lng, lat, geoprivacy, photoSquareUrl, photoMediumUrl, license,
                  userLogin, datasetTitle, threatened }
ObservationDetail extends ObservationCard { sourceObservationId, photoLargeUrl, positionalAccuracy,
                  identificationsCount, recordBasis, cityName, sourceUrl }
ReportInput     { observationId, reason: 0..4, content?, contact? }
SpeciesRankItem { taxonId, scientificName, commonNameCn, commonNameEn, iconicTaxon, threatened,
                  recordCount, observerCount, photoSquareUrl, photoMediumUrl, photoLicense?, photoUserLogin?, lastObservedOn, reachedOn? }
TaxonInfo       { taxonId, source, sourceTaxonId, rank, scientificName, commonNameCn, commonNameEn, iconicTaxon, threatened, ancestry }
TaxonAncestor   { taxonId, rank: 0..11, scientificName, commonNameCn, commonNameEn }
WantedTaxonItem { sourceTaxonId, rank, scientificName, commonNameCn, commonNameEn, iconicTaxon, nearbyCount, photoSquareUrl, photoMediumUrl, photoLicense? }
SpeciesDetail   { taxon, recordCount, observerCount, firstObservedOn, lastObservedOn, photos: ObservationCard[], points: ObservationPoint[] }
EmergenceResult { mode, unit: 'tenDay'|'month', sampleSize, series: [{ index, recordCount, researchCount }] }
PhenologyMatrix { rows: [{ taxonId, scientificName, commonNameCn, iconicTaxon, total, counts: number[12] }] }
CityOverview    { cityId, cityName, totalObservation, researchCount, speciesCount, observerCount, monthObservation,
                  monthly12?: [{ month: 'YYYY-MM', count }], iconicDistribution: [{ iconicTaxon, count }],
                  risingStars: SpeciesRankItem[], topObservers: [{ userLogin, count }], newestSourceCreatedAt }
```

### 6.5 后端实现要点（`Rampage.NaturalistAtlas.WebApi`）

- 分层：`InternalApi/*ApiController` → BLL `IAtlas*Service`（`AtlasGeoService`、`AtlasObservationService`、`AtlasSpeciesService`、`AtlasStatsService`）→ DAL（`ObservationDao.Public.cs`、`CityDao`、`TaxonDao`、`EmergenceStatDao`、`EmergenceHistoryDao`）。
- 筛选 SQL 统一由 `DAL/Implementations/AtlasFilterSql.cs` 构建：恒含 `status = 0`；类群/鉴定级别为已校验 short 内联 IN 列表；其余参数化；跨年月份用 `EXTRACT(MONTH …) >= from OR <= to`。
- 城市 center = bbox 中点；源站链接：iNat `https://www.inaturalist.org/observations/{id}`、GBIF `https://www.gbif.org/occurrence/{id}`。
- 物候 detail 优先 `emergence_stat`（近两年），为空则按 `(月-1)×3 + LEAST(2,(日-1)/10) + 1` 实时聚合；history 优先 `emergence_history`，为空或 researchOnly 时按月实时聚合。
- 本月新星：研究级累计在本月 1 日前 < 5 且当前 ≥ 5（口径固定研究级，不受开关影响）。
- 举报：校验原因、长度、观察存在且正常；`SnowflakeTool.NextIdString()` 主键；空说明/联系方式存空串。

---

## 七、响应式与无障碍

### 7.1 断点

| 断点 | 宽度 | 布局 |
|---|---|---|
| phone | < 640px | 底部抽屉；TopBar 折叠导航为菜单；图例仅字形；时间滑块单行 |
| tablet | 640~1023px | 底部抽屉（中档更高）；图例完整 |
| desktop | ≥ 1024px | 左侧 420px 侧栏（可收起）；MapHud 四角完整 |

`useResponsive()` 提供 `isDesktop` / `isPhone`，布局组件据此切换 SidePanel / MobileSheet。

### 7.2 移动端底部抽屉（`MobileSheet`）

**已实现**：三档 `peek`（18dvh，露出导航标签栏与标题）/ `half`（50dvh）/ `full`（88dvh）；拖拽把手（Pointer Events + `setPointerCapture`）实时改高度，松手就近吸附；点击导航标签时从 `peek` 展开到 `half`；抽屉顶部内置地点搜索与移动端导航。

**待开发**（`TODO(设计文档§7.2)`）：

1. 惯性拖拽与速度判定：`pointermove` 记录最近 100ms 位移计算速度，|v| > 0.5px/ms 时按方向跳档，否则就近吸附；
2. 抽屉内容滚动到顶部时继续下拉才拖动抽屉（`overscroll-behavior: contain`）；
3. 打开大图时自动收到 `peek`、关闭后恢复原档位；
4. 动画改用 `transform: translateY` + `--ease-out`（避免改 height 触发重排）。

### 7.3 无障碍

- 对比度：三套主题正文 ≥ 4.5:1、次级文字 ≥ 3:1（令牌已按此取色，新增色须复核）。
- 所有图标按钮带 `aria-label`（i18n）；图例芯片为 `role="checkbox"` + `aria-checked`。
- 键盘：Tab 顺序 TopBar → HUD → 面板；地图可用方向键平移、+/- 缩放（MapLibre 内置）；大图 Esc 关闭。
- `prefers-reduced-motion`：关闭呼吸光晕、相机飞行改瞬移、卡片入场动效跳过。
- 照片 `alt` = 物种名 + 观察者。

---

## 八、国际化

- `vue-i18n` composition 模式；`zh-CN.ts` 为权威源，`en-US.ts` 类型为 `typeof zhCN`（缺 key 编译失败）。
- 语言切换：TopBar 语言按钮 → `setLocale()` 同步 `<html lang>` 与请求头 `Accept-Language`（`setRequestLocale`），持久化 `na.locale`；naive-ui 语言包由 `App.vue` 随 `locale` 计算切换。
- 物种名显示规则（`utils/format.ts speciesName`）：中文界面 中文名 > 英文名 > 学名；英文界面 英文名 > 学名；学名始终斜体副标题。
- 日期时间：`observedOn` 原样按本地化格式显示（不做时区换算）；`observedAt` 等 UTC 时刻按访客时区格式化；相对时间用 `Intl.RelativeTimeFormat`。
- 数字：`Intl.NumberFormat` 千分位。

---

## 九、测试策略

| 层 | 工具（建议引入） | 覆盖 |
|---|---|---|
| 单元 | Vitest | `utils/format.ts`、`useUrlSync` 解析、`GlobalTileLayer` 月份展开（跨年）、`taxonPalette`、`http.ts` camelCase 转换 |
| 组件 | Vitest + @vue/test-utils | TaxonLegendFilter 多选/solo、TimeSlider 跨年区间、ReportDialog 校验与幂等键 |
| 契约 | 后端集成测试 | 每个端点的 JSON 字段名与 `types.ts` 一致（快照） |
| E2E | Playwright | 首屏地图加载 ≤ 2s、筛选联动、城市切换、2D/2.5D/3D 切换、主题切换后图层恢复、移动端抽屉 |
| 视觉 | Playwright 截图对比 | 三套主题 × 探索/总览/物种详情 |

手工回归清单：主题 × 底图 × 视图模式 全组合下自有图层不丢失；离线模式（停后端）界面可用；中英文切换无遗漏文案。

---

## 十、任务拆分与排期

| # | 任务 | 章节 | 优先级 | 估时 | 状态 |
|---|---|---|---|---|---|
| 1 | 框架、主题、布局、路由、store、i18n | §1~§4 | P0 | — | ✅ 已完成 |
| 2 | 地图内核、视图模式、底图、图层栈 | §2、§4 | P0 | — | ✅ 已完成 |
| 3 | 公开站 WebApi 全部端点 | §6 | P0 | — | ✅ 已完成 |
| 4 | 探索列表无限滚动 + 截断提示 + 距离排序 | §5.1 | P0 | 1.5d | ✅ 已完成 |
| 5 | 全球视角渐变衔接与降级 | §5.3 | P0 | 1d | ✅ 已完成 |
| 6 | 物种详情瀑布流照片墙 + 最佳观察时间 | §5.6 | P0 | 2d | ✅ 已完成 |
| 7 | 大图键盘切换 / LQIP / 已举报标记 | §5.10 | P0 | 1d | ✅ 已完成 |
| 8 | 地点搜索键盘操作与最近搜索 | §5.11 | P0 | 0.5d | ✅ 已完成 |
| 9 | 移动抽屉惯性拖拽 | §7.2 | P0 | 1d | ✅ 已完成 |
| 10 | 时间滑块逐旬档位 + 季节剧本 + 投屏 | §5.4 | P1 | 2d | ✅ 已完成 |
| 11 | 物候矩阵排序 / TopN / 虚拟滚动 | §5.7 | P1 | 1.5d | ✅ 已完成 |
| 12 | 总览趋势迷你图 + 新星横滑（含后端字段） | §5.8 | P1 | 1.5d | ✅ 已完成 |
| 13 | 稀缺物种榜 + 导出（含后端分支） | §5.5 | P1 | 1d | ✅ 已完成 |
| 14 | 分类阶元面包屑（含后端接口） | §5.6 | P1 | 1d | ✅ 已完成 |
| 15 | 单元/组件测试基建（Vitest 14 文件 82 用例；Playwright 后续） | §9 | P1 | 2d | ✅ 已完成 |
| 16 | 集邮缺口（含采集缓存表与接口） | §5.9 | P2 | 3d | ✅ 已完成 |
| 17 | 矩阵导出 PNG、相似物种推荐 | §5.6/§5.7 | P2 | 2d | ✅ 已完成 |

---

## 十一、上线前检查清单

1. 天地图 key 配置并在署名补当年审图号；中国大陆访客默认天地图底图。
2. `VITE_API_BASE_URL` 指向生产域名，后端 CORS 白名单加入生产前端域名。
3. 所有照片位置带授权署名；受威胁物种提示文案复核。
4. 三主题对比度复核；`prefers-reduced-motion` 验证。
5. 首屏性能（Lighthouse 移动端 Performance ≥ 80）、点位接口 P95 ≤ 500ms。
6. 源站瓦片失败降级验证（断网 iNat / GBIF 域名）。
