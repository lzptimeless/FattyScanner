using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace FattyScanner.Core.Models
{
    /// <summary>
    /// 扫描到的文件或文件夹信息
    /// </summary>
    public class FileSysNode
    {
        /// <summary>
        /// 文件名或文件夹名
        /// </summary>
        public string? Name { get; set; }
        /// <summary>
        /// 这个对象是否表示文件夹
        /// </summary>
        public bool IsDir { get; set; }
        /// <summary>
        /// 文件或文件夹大小，单位字节
        /// </summary>
        public long Size { get; set; }
        /// <summary>
        /// 用户扫描时指定的路径，只有根节点有值
        /// </summary>
        public string? ScanPath { get; set; }
        /// <summary>
        /// 父级节点
        /// </summary>
        public FileSysNode? Parent { get; set; }
        /// <summary>
        /// 子文件或文件夹对象
        /// </summary>
        public List<FileSysNode>? Subs { get; set; }

        /// <summary>
        /// 添加子文件夹或文件，如果Subs==null就新建Subs
        /// </summary>
        /// <param name="sub">子文件夹或文件</param>
        public void AddSub(FileSysNode sub)
        {
            if (Subs == null) Subs = new List<FileSysNode>();

            Subs.Add(sub);
        }

        /// <summary>
        /// 清理子节点
        /// </summary>
        public void CleanSub()
        {
            if (Subs != null)
            {
                foreach (FileSysNode sub in Subs)
                {
                    sub.CleanSub();
                }
                Subs = null;
            }
        }

        /// <summary>
        /// 获取完整路径
        /// </summary>
        /// <returns></returns>
        public string GetFullPath()
        {
            if (!string.IsNullOrEmpty(ScanPath))
            {
                return ScanPath;
            }

            var parentPath = Parent?.GetFullPath();
            if (string.IsNullOrEmpty(parentPath))
            {
                return Name ?? "?";
            }

            string fullPath = Path.Combine(parentPath, Name ?? "?");
            return fullPath;
        }

        public override string ToString()
        {
            return $"{Name} {ByteConverter.Format(Size)}";
        }

        /// <summary>
        /// 获取指定深度的文件树字符串
        /// </summary>
        /// <param name="deep">文件树深度</param>
        /// <param name="indent">初始缩进</param>
        /// <param name="sb">外部StringBuilder，用以接收格式化结果，可以传入null来自动创建</param>
        /// <returns></returns>
        public StringBuilder ToTreeString(int deep, int indent = 0, StringBuilder? sb = null)
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
                    item.ToTreeString(subDeep, subIndent, sb);
                }
            }

            return sb;
        }
    }
}
