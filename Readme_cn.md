# 自然地图志 (Naturalist Atlas)

> 基于 **iNaturalist** 与 **GBIF** 开放生物多样性数据的自然探索观察地图。

[English](Readme.md) | 简体中文

**自然地图志**将真实世界的物种观察呈现在交互地图上。产品定位为**只读**的探索型应用——不做观察记录上传。MVP 首城为**北京**，后续将逐步扩展城市范围，乃至超越生物本身（地形、地质、天文气象等自然资源均为远期方向）。

---

## 功能特性

- 🗺 **地图探索** — MapLibre GL 地图 + 观察点位聚合（supercluster），城市切换、时间滑块（月 / 旬双档）、类群 / 鉴定级别 / 仅看有图筛选
- 🐦 **物种** — 物种榜（记录数最多 / 观察者最多 / 稀缺榜）、物种档案含祖先分类链与相似物种发现
- 🌸 **物候** — 逐旬与逐月出现统计、物候矩阵（Top 物种 × 12 月，峰值月排序）、PNG 原生导出
- 🏙 **城市总览** — 近 12 月记录趋势、类群构成、本月新星
- 🎯 **集邮缺口** — 城市 Wanted 物种清单，助力集齐一座城市的"图鉴"
- 🚩 **内容安全** — 公开举报入口 + 后台处置工作流（隐藏 / 恢复观察、结单）
- 🛠 **管理后台** — 国家 / 省份 / 城市、分类单元、观察数据、举报工单、采集任务与日志、运营看板（Chart.js）
- 🔄 **双源采集器** — 定时任务从 iNaturalist（API v2）与 GBIF（Occurrence API）同步：水位增量、时间窗回补、删除对账、限速与退避重试

## 技术栈

| 层 | 技术 |
| --- | --- |
| 后端 | .NET 8、自研 **Rampage** 框架（MVC / DAL / 调度）、PostgreSQL |
| 管理后台 | ASP.NET Core Razor（构建期编译）、Bootstrap 5、jQuery、Chart.js |
| 前端 | Vue 3 + Vite + TypeScript、MapLibre GL、supercluster、Pinia、TanStack Query、vue-i18n、Naive UI、ECharts、Vitest |
| 数据源 | [iNaturalist API v2](https://api.inaturalist.org/v2/docs/)、[GBIF Occurrence API](https://www.gbif.org/developer/summary) |

## 项目结构

```
Rampage.NaturalistAtlas/
├── Rampage.NaturalistAtlas.Net/                  # .NET 8 后端（见目录内 Readme.md）
│   ├── Rampage.NaturalistAtlas.Model             # Po / Dto / 枚举 / 筛选模型
│   ├── Rampage.NaturalistAtlas.Common            # 项目级通用逻辑
│   ├── Rampage.NaturalistAtlas.DAL               # 数据访问层（SQL 只允许出现在这一层）
│   ├── Rampage.NaturalistAtlas.BLL               # 业务逻辑层
│   ├── Rampage.NaturalistAtlas.WebApi            # 公开 / 管理 JSON API
│   ├── Rampage.NaturalistAtlas.ManagementSite    # 管理后台（Razor，端口 5213）
│   ├── Rampage.NaturalistAtlas.Collector         # 采集服务（iNat / GBIF ETL）
│   ├── Rampage.NaturalistAtlas.Tests             # 单元测试（xUnit）
│   └── Rampage.NaturalistAtlas.sln
├── Rampage.NaturalistAtlas.Web/                  # Vue 3 单页前端（开发端口 5174）
└── documents/                                    # 产品文档、前端设计文档、数据表设计
```

## 快速开始

### 环境要求

- .NET 8 SDK
- Node.js 20+
- PostgreSQL 14+

### 1. 数据库

建两个库（业务库 + 后台库），并执行
[`documents/DatabaseTablesDesign/`](documents/DatabaseTablesDesign/Readme.md) 下的 SQL 脚本：

- `NaturalistAtlas.TablesDesign` — 业务库：country / province / city / taxon / observation / sync_cursor / sync_log / report / emergence_stat / emergence_history / city_wanted_taxon，含地理种子数据（249 国家、357 省份、362 城市）
- `NaturalistAtlas.Management.TablesDesign` — 后台库：管理员、菜单、权限、设置、操作日志

### 2. 后端

每个站点读取各自的 `config/app.json`（数据库连接、AES 密钥、日志配置）。配置好后：

```bash
# 公开 JSON API（http://localhost:5214）
dotnet run --project Rampage.NaturalistAtlas.Net/Rampage.NaturalistAtlas.WebApi

# 管理后台（http://localhost:5213）
dotnet run --project Rampage.NaturalistAtlas.Net/Rampage.NaturalistAtlas.ManagementSite
```

运行单元测试：

```bash
dotnet test Rampage.NaturalistAtlas.Net/Rampage.NaturalistAtlas.Tests
```

### 3. 前端

```bash
cd Rampage.NaturalistAtlas.Web
npm install
npm run dev        # http://localhost:5174
npm run build      # 类型检查 + 生产构建，输出 dist/
npm test           # Vitest
```

### 4. 采集端

```bash
dotnet run --project Rampage.NaturalistAtlas.Net/Rampage.NaturalistAtlas.Collector
```

任务按 城市/数据源 调度，水位保存在 `sync_cursor`：增量同步、时间窗回补、分类档案刷新、删除对账、物候直方图聚合、Wanted 同步。一次性 Wanted 模式可用 `--run-wanted`。

## 公开 API（JSON）

| 端点 | 说明 |
| --- | --- |
| `GET /api/geo/search` · `GET /api/geo/cities` | 地点搜索、城市列表 |
| `GET /api/observation/points` · `GET /api/observation/list` | 地图点位、观察卡片分页 |
| `GET /api/species/rank` · `GET /api/species/{id}` · `GET /api/species/{id}/similar` · `GET /api/species/{id}/emergence` | 物种榜、详情、相似物种、物候序列 |
| `GET /api/taxon/{id}/ancestors` | 分类祖先链 |
| `GET /api/phenology/matrix` | 物候矩阵（Top 物种 × 12 月） |
| `GET /api/stats/overview` | 城市总览统计 |
| `GET /api/gaps` | 城市集邮缺口（Wanted 物种） |
| `POST /api/report` | 提交举报 |

## 代码规范

C# 规范由 [.editorconfig](Rampage.NaturalistAtlas.Net/.editorconfig) 固化：类型/成员 Pascal 命名、局部变量驼峰、常量 `UPPER_SNAKE_CASE`、4 空格缩进、花括号独占一行且不可省略、using 字母音序、单行单语句，以及 Service/DAO 命名规约（`Get…` / `…List` / `…Count` / `Save…` / `Remove/Delete` / `Update…`）。SQL 只允许写在 DAL 层。

## 文档

- 产品文档：[documents/ProductDocuments/NaturalistAtlas.ProductDocument.md](documents/ProductDocuments/NaturalistAtlas.ProductDocument.md)
- 前端设计文档：[documents/ProductDocuments/NaturalistAtlas.FrontendDesignDocument.md](documents/ProductDocuments/NaturalistAtlas.FrontendDesignDocument.md)
- 数据表设计：[documents/DatabaseTablesDesign/](documents/DatabaseTablesDesign/)
- 后端说明：[Rampage.NaturalistAtlas.Net/Readme.md](Rampage.NaturalistAtlas.Net/Readme.md)

## 作者

**Nicholas Chow (XTiger1986)** · [1986z.com](https://1986z.com)

© 2026 保留所有权利。
