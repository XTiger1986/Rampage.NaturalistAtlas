/**
 * ECharts 按需注册（仅引入用到的图表与组件，控制包体积）
 * / On-demand ECharts registration (only the charts and components in use, to keep the bundle small)
 * 使用：在图表组件中 import '@/plugins/echarts' 后使用 <VChart :option="..." autoresize />
 * / Usage: import '@/plugins/echarts' in a chart component, then use <VChart :option="..." autoresize />
 */
import { use } from 'echarts/core'
import { CanvasRenderer } from 'echarts/renderers'
import { BarChart, LineChart, PieChart } from 'echarts/charts'
import { GridComponent, TooltipComponent, MarkAreaComponent } from 'echarts/components'

use([
  CanvasRenderer,
  LineChart,
  BarChart,
  PieChart,
  GridComponent,
  TooltipComponent,
  MarkAreaComponent
])

/**
 * 从 CSS 设计令牌读取当前主题色（ECharts 不识别 CSS 变量，需解析为具体色值）
 * / Read the current theme color from CSS design tokens (ECharts cannot resolve CSS variables, a concrete value is required)
 * @param name 变量名，如 --c-ink / Variable name, e.g. --c-ink
 */
export function cssVar(name: string): string {
  return getComputedStyle(document.documentElement).getPropertyValue(name).trim()
}
