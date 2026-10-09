using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rampage.NaturalistAtlas.ManagementSite.Internal
{
    /// <summary>
    /// CSV 构建工具（管理后台数据导出）：RFC 4180 转义 + UTF-8 BOM（Excel 直接打开中文不乱码）
    /// CSV builder for admin exports: RFC 4180 escaping plus a UTF-8 BOM (Excel opens Chinese text correctly)
    /// </summary>
    public static class CsvTool
    {
        /// <summary>
        /// 字段转义：含逗号/引号/换行时整体加引号并把内部引号翻倍；null 转空串
        /// Escape a field: quote the whole value when it contains commas / quotes / newlines and
        /// double inner quotes; null becomes empty
        /// </summary>
        public static string Escape(object? field)
        {
            string value = field?.ToString() ?? string.Empty;
            value = value.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');
            if (value.Contains(',') || value.Contains('"'))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }

        /// <summary>
        /// 构建 CSV 文本（首行表头）
        /// Build the CSV text (header row first)
        /// </summary>
        /// <param name="headers">表头 / Headers</param>
        /// <param name="rows">数据行（已字符串化的字段）/ Rows of stringified fields</param>
        public static string Build(IEnumerable<string> headers, IEnumerable<IEnumerable<object?>> rows)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(string.Join(",", headers)).Append("\r\n");
            foreach (IEnumerable<object?> row in rows)
            {
                List<string> cells = new List<string>();
                foreach (object? field in row)
                {
                    cells.Add(Escape(field));
                }
                sb.Append(string.Join(",", cells)).Append("\r\n");
            }
            return sb.ToString();
        }

        /// <summary>带 BOM 的下载字节（Excel 识别 UTF-8）/ Download bytes with the BOM (Excel detects UTF-8)</summary>
        public static byte[] ToDownloadBytes(string csv)
        {
            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        }
    }
}
