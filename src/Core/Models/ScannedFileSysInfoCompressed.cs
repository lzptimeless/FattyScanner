using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FattyScanner.Core.Models
{
    /// <summary>
    /// 用以模块内部保存文件扫描结果，Name使用UTF8编码压缩大小
    /// </summary>
    internal class ScannedFileSysInfoCompressed
    {
        #region fields
        private byte[]? _nameBuffer;
        #endregion

        #region properties
        /// <summary>
        /// 这个对象是文件还是文件夹
        /// </summary>
        public bool IsDir { get; set; }
        /// <summary>
        /// Subs中是否已经包含子文件，为了节约空间大部分CompressedScannedFileSysInfo.Subs内部只存了子文件夹节点没有子文件节点
        /// </summary>
        public bool IsFileFilled { get; set; }
        /// <summary>
        /// The size of byte
        /// </summary>
        public long Size { get; set; }
        /// <summary>
        /// 自文件或文件夹对象
        /// </summary>
        public List<ScannedFileSysInfoCompressed>? Subs { get; set; }
        #endregion

        #region public methods
        /// <summary>
        /// 将文件名或文件夹名转换为UTF8保存，以节约内存
        /// </summary>
        /// <param name="name">要保存的文件名或文件夹名</param>
        public void SetName(string? name)
        {
            if (name == null)
                _nameBuffer = null;
            else
                _nameBuffer = Encoding.UTF8.GetBytes(name);
        }

        /// <summary>
        /// 将保存UTF8文件名返回为string
        /// </summary>
        /// <returns></returns>
        public string? GetName()
        {
            var buffer = _nameBuffer;
            if (buffer == null)
                return null;
            else
                return Encoding.UTF8.GetString(buffer);
        }

        /// <summary>
        /// 添加子文件夹或文件，如果Subs==null就新建Subs
        /// </summary>
        /// <param name="sub">子文件夹或文件</param>
        public void AddSub(ScannedFileSysInfoCompressed sub)
        {
            if (Subs == null) Subs = new List<ScannedFileSysInfoCompressed>();

            Subs.Add(sub);
        }

        /// <summary>
        /// 清理子级，递归清理子级Subs属性
        /// </summary>
        public void CleanSubs()
        {
            if (Subs != null)
            {
                foreach (var sub in Subs)
                {
                    sub.CleanSubs();
                }
                Subs = null;
            }
        }

        public ScannedFileSysInfo Copy(string nodeFullPath, string nodeName, int deep, double totalSize, double ignoreSize)
        {
            totalSize = Math.Max(totalSize, 1); // 防止被除数为0
            ScannedFileSysInfo resInfo = new ScannedFileSysInfo
            {
                Name = nodeName,
                IsDir = IsDir,
                Size = Size
            };

            if (deep > 1)
            {
                int subDeep = deep - 1;
                long totalChildSize = 0;
                if (Subs != null)
                {
                    foreach (var sub in Subs)
                    {
                        if (ignoreSize >= 1)
                        {
                            if (sub.Size < ignoreSize) continue;
                        }
                        else if (ignoreSize > 0)
                        {
                            if (sub.Size / totalSize < ignoreSize) continue;
                        }

                        var subName = sub.GetName();
                        if (!string.IsNullOrEmpty(subName))
                        {
                            var subFullPath = Path.Combine(nodeFullPath, subName);
                            var subInfo = sub.Copy(subFullPath, subName, subDeep, totalSize, ignoreSize);
                            resInfo.AddSub(subInfo);
                            totalChildSize += subInfo.Size;
                        }
                    }
                }

                if (!IsFileFilled)
                {
                    try
                    {
                        foreach (var fileInfo in new DirectoryInfo(nodeFullPath).GetFiles())
                        {
                            // The file is offline. The data of the file is not immediately available.
                            if (fileInfo.Attributes.HasFlag(FileAttributes.Offline)) continue;
                            // 为了节约内存，只取初始状态需要显示给用户看的文件节点，过滤大小为0的节点
                            if (fileInfo.Length == 0) continue;

                            if (ignoreSize >= 1)
                            {
                                if (fileInfo.Length < ignoreSize) continue;
                            }
                            else if (ignoreSize > 0)
                            {
                                if (fileInfo.Length / totalSize < ignoreSize) continue;
                            }

                            resInfo.AddSub(new ScannedFileSysInfo
                            {
                                Name = fileInfo.Name,
                                Size = fileInfo.Length
                            });
                            totalChildSize += fileInfo.Length;
                        }
                    }
                    catch { }
                }

                if (totalChildSize < Size)
                {
                    resInfo.AddSub(new ScannedFileSysInfo
                    {
                        Name = "Others",
                        Size = Size - totalChildSize,
                        IsOthers = true
                    });
                }
            }// (deep > 1)

            return resInfo;
        }

        public override string ToString()
        {
            return $"{GetName()} {ByteSizeFormatter.SizeSuffix(Size)}";
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
        #endregion
    }
}
