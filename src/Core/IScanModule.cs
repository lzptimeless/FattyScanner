using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FattyScanner.Core.Events;
using FattyScanner.Core.Models;

namespace FattyScanner.Core
{
    /// <summary>
    /// Space Radar 模块
    /// </summary>
    public interface IScanModule
    {
        /// <summary>
        /// 获取当前的扫描状态
        /// </summary>
        ScanStates ScanState { get; }
        /// <summary>
        /// 获取调用<see cref="StartScan(string)"/>传入的扫描路径
        /// </summary>
        string? ScanPath { get; }

        /// <summary>
        /// 扫描进度变化事件
        /// </summary>
        event EventHandler<ScanProgressArgs> ScanProgressChanged;
        /// <summary>
        /// 扫描状态变化事件
        /// </summary>
        event EventHandler<ScanStateChangedArgs> ScanStateChanged;

        /// <summary>
        /// 获取当前电脑所有磁盘的盘符，如C:\
        /// </summary>
        /// <returns></returns>
        List<string> GetDisks();
        /// <summary>
        /// 开始扫描
        /// </summary>
        /// <param name="path">需要扫描的路径或盘符，如C:\</param>
        void StartScan(string path);
        /// <summary>
        /// 停止扫描
        /// </summary>
        void StopScan();
        /// <summary>
        /// 停止扫描并清理所有扫描结果
        /// </summary>
        void CleanScan();
        /// <summary>
        /// 获取扫描得到的文件树，可能返回null
        /// </summary>
        /// <param name="startPath">要获取的文件树的起始路径，这个值必须是一个全路径</param>
        /// <param name="deep">要获取的文件树的深度，值必须大于等于1，1表示只获取startPath表示的这个节点的信息不包括子级</param>
        /// <param name="ignoreSize">0: 表示全部返回，不忽略，0< && < 1: 表示只返回size百分比大于等于这个值的节点，例如ignoreSize=0.01
        /// 表示只返回size百分比大于等于百分之一的节点，> 1: 表示只返回size大于等于这个值的节点，例如ignoreSize=1024表示只返回size>=1024byte的节点</param>
        /// <returns></returns>
        ScannedFileSysInfo? GetTree(string? startPath, int deep, double ignoreSize);
        /// <summary>
        /// 打开或执行指定文件
        /// </summary>
        /// <param name="path"></param>
        void OpenFile(string path);
        /// <summary>
        /// 打开指定文件夹
        /// </summary>
        /// <param name="path"></param>
        void OpenFolder(string path);
    }
}
