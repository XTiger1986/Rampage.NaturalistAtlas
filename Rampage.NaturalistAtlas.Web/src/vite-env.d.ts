/// <reference types="vite/client" />

/** 前端环境变量声明（.env.* 中 VITE_ 前缀变量，实际取值见各环境文件注释） / Frontend env variable declarations (VITE_-prefixed keys in .env.*; see each env file for actual values) */
interface ImportMetaEnv {
  /** 后端接口基地址（不带 /api 后缀） / Backend API base URL (without the /api suffix) */
  readonly VITE_API_BASE_URL: string
  /** 天地图 key（可选，留空则不注册天地图底图） / Tianditu key (optional; when empty the Tianditu basemap is not registered) */
  readonly VITE_TIANDITU_KEY?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
