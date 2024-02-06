using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace FattyScanner.Core.Models
{
    /// <summary>
    /// 扫描到的文件信息
    /// </summary>
    public class ScannedFileSysInfo
    {
        /// <summary>
        /// 文件名或文件夹名
        /// </summary>
        public string? Name { get; set; }
        /// <summary>
        /// 这个对象是文件还是文件夹
        /// </summary>
        public bool IsDir { get; set; }
        /// <summary>
        /// 表示多个文件或文件夹和在一起的节点
        /// </summary>
        public bool IsOthers { get; set; }
        /// <summary>
        /// The size of byte
        /// </summary>
        public long Size { get; set; }
        /// <summary>
        /// 子文件或文件夹对象
        /// </summary>
        public List<ScannedFileSysInfo>? Subs { get; set; }

        /// <summary>
        /// 添加子文件夹或文件，如果Subs==null就新建Subs
        /// </summary>
        /// <param name="sub">子文件夹或文件</param>
        public void AddSub(ScannedFileSysInfo sub)
        {
            if (Subs == null) Subs = new List<ScannedFileSysInfo>();

            Subs.Add(sub);
        }

        public override string ToString()
        {
            return $"{Name} {ByteSizeFormatter.SizeSuffix(Size)}";
        }

        /// <summary>
        /// 获取指定深度的文件树字符串
        /// </summary>
        /// <param name="deep">文件树深度</param>
        /// <param name="indent">初始缩进</param>
        /// <param name="sb">外部StringBuilder，用以接收格式化结果，可以传入null来自动创建</param>
        /// <returns></returns>
        public StringBuilder ToStringBuilder(int deep, int indent = 0, StringBuilder? sb = null)
        {
            if (sb == null) sb = new StringBuilder();

            for (int i = 0; i < indent; i++)
            {
                sb.Append("  "); // 添加缩进
            }

            sb.Append(ToString());
            if (deep > 1 && Subs != null && Subs.Count > 0)
            {
                int subDeep = deep - 1;
                int subIndent = indent + 1;
                foreach (var item in Subs)
                {
                    sb.AppendLine();
                    item.ToStringBuilder(subDeep, subIndent, sb);
                }
            }

            return sb;
        }
    }
}
