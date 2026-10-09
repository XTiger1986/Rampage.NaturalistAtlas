# 自然地图志前端

- 自然地图志响应式 Web 应用（桌面 + 移动），地图常驻全屏、功能面板叠加在地图之上。
- 前后端分离：本项目只消费 `Rampage.NaturalistAtlas.WebApi` 公开接口。
- **详细设计、各功能待开发点与接口契约见《自然地图志前端设计》**（`documents/产品文档/自然地图志前端设计.md`）；
  代码中的 `TODO(设计文档§x.y)` 与该文档章节号一一对应。
- **前端开发时，务必写清代码注释；注释为中英双语**（与后端 Rampage 惯例一致）：单行 `// 中文 / English`，JSDoc 块内逐行 `* 中文 / English`。新增代码即遵此规范，存量已全部双语化。

## 技术栈

| 类别 | 选型 | 说明 |
|---|---|---|
| 框架 | Vue 3.5 + TypeScript 5.9（strict） | `<script setup>`；`vue-tsc` 类型检查 |
| 构建 | Vite 7 | 要求 Node 20.19+ / 22.12+（当前 Node 24）；maplibre / echarts / naive 独立 chunk |
| 地图 | MapLibre GL 5 + supercluster | 单引擎 2D / 2.5D / 3D（globe + 地形 + 天空 + 建筑挤出）；客户端聚合照片卡 |
| UI | naive-ui + Tailwind CSS v4 | naive 主题与 Tailwind 语义类都由设计令牌（CSS 变量）驱动 |
| 数据 | @tanstack/vue-query | 缓存、去重、`keepPreviousData`、离线禁用 |
| 状态 | Pinia（setup store） | theme / city / filter / mapView / ui |
| 图表 | ECharts 6 + vue-echarts | 按需注册 |
| 其他 | @vueuse/core、@vueuse/motion、vue-i18n | 断点/防抖、动效、中英双语 |

## 常用命令

```bash
npm install --include=dev   # 安装依赖（见下方注意事项）
npm run dev                 # 开发服务器 http://localhost:5174（固定端口，区别于 SourcePower 前端的 5173）
npm run build               # 类型检查（vue-tsc）+ 生产构建
npm run preview             # 预览生产构建产物
npm run typecheck           # 仅类型检查
```

> **注意**：本机全局 npm 配置了 `omit=dev`，直接 `npm install` 会跳过 devDependencies，
> 导致 `vite` / `vue-tsc` 不可用。安装时务必带 `--include=dev`，或先执行 `npm config delete omit`。

## 环境变量

| 变量 | 默认 | 说明 |
|---|---|---|
| `VITE_API_BASE_URL` | `http://localhost:5214` | WebApi 基地址；开发期跨域直连（5174 → 5214），后端已开启 CORS（AllowLocalhost） |
| `VITE_TIANDITU_KEY` | 空 | 天地图 key；配置后底图列表出现"天地图"（中国大陆公开上线须使用） |

文件：`.env.development`（dev）、`.env.production`（build），模板见 `.env.example`。

**后端未启动也能开发**：城市列表回落为内置离线城市，数据查询自动禁用并显示"后端未连接"，地图、底图、主题、视图模式、全球瓦片照常可用。

## 目录结构

```
src/
├── main.ts / App.vue      # 入口：Pinia、路由、i18n、vue-query、motion；NConfigProvider 注入主题与语言包
├── api/                   # http.ts（统一响应解包 + PascalCase→camelCase）、types.ts（接口契约）、atlas.ts（端点函数）
├── composables/           # queries.ts（全部取数钩子）、useUrlSync.ts（筛选⇄URL）、useResponsive.ts
├── i18n/                  # zh-CN（权威）、en-US
├── map/                   # MapEngine（地图单例）、basemaps（底图注册）、viewModes（2D/2.5D/3D）
│   └── layers/            # 照片卡聚合、热力、全球瓦片、城市边界、物种聚焦
├── theme/                 # tokens.css（设计令牌）、themes.ts、naiveTheme.ts、taxonPalette.ts（类群色）
├── stores/                # theme / city / filter / mapView / ui
├── styles/main.css        # Tailwind 入口、@theme 语义映射、全局样式
├── plugins/echarts.ts     # ECharts 按需注册
├── utils/format.ts        # 署名、授权、数字、相对时间、物种名
├── components/            # layout / map / common / observation / species / phenology / stats
└── views/                 # Explore、Overview、SpeciesList、SpeciesDetail、Phenology、Gaps、NotFound
```

## 路由

| 路径 | 页面 | 功能 |
|---|---|---|
| `/` | 地图探索 | 照片卡聚合、类群图例筛选、时间滑块、热力、视野内观察列表 |
| `/overview` | 城市总览 | 指标卡、类群构成、本月新星、活跃观察者 |
| `/species` | 物种榜 | 按记录数/观察者数排序、关键字搜索 |
| `/species/:id` | 物种详情 | 照片墙、本市分布点、全球分布范围、物候曲线 |
| `/phenology` | 物候日历 | 物种 × 月份矩阵、物候曲线 |
| `/gaps` | 集邮缺口 | P2 占位 |

城市与筛选同步到 URL：`?city=beijing&t=1,6&m=3-5&q=1`。

## 三套主题

| id | 名称 | 气质 |
|---|---|---|
| `journal` | 博物志·纸本 | 暖米纸、墨线、宋体标题、标本签照片卡 |
| `night` | 夜观·萤火（默认） | 深林夜色、玻璃面板、荧光类群色与光晕 |
| `spectrum` | 四季·光谱 | 明亮通透、大圆角，强调色随月份流转 |

组件内**禁止硬编码颜色**：使用 `bg-surface`、`text-ink`、`text-ink-2`、`border-line`、`text-accent`、`bg-accent-soft`、`rounded-card`、`font-display`、`.glass-panel` 等语义类；类群色只从 `theme/taxonPalette.ts` 取。

## 底图

| 底图 | 说明 |
|---|---|
| OpenFreeMap（默认） | 免费开源矢量底图，无需 key；三主题分别用 positron / dark / liberty 并打主题补丁 |
| 天地图 | 配置 `VITE_TIANDITU_KEY` 后出现；中国大陆公开上线须使用并补审图号 |
| Esri 卫星影像 | 无需 key，保留署名 |
| AWS Terrain Tiles | 3D 地形（Terrarium 编码） |
| iNaturalist v1 / GBIF v2 瓦片 | 全球视角（缩小到城市全貌以外或离开城市范围时自动切换） |

全部底图为 WGS-84 系，与观察点坐标直接对齐；**不要引入高德/腾讯等 GCJ-02 底图**。
