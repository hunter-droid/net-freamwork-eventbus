using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freamwork.EventBus
{
    public interface ILogger
    {
        #region DebugAsync      
        /// <summary>
        /// Debug
        /// </summary>
        /// <param name="source">标记1</param>
        /// <param name="tranCode">标记2</param>
        /// <param name="msg">日志信息</param>
        void Debug(string source, string tranCode, Object msg);

        #endregion

        #region InfoAsync      
        /// <summary>
        /// Info
        /// </summary>
        /// <param name="source">标记1</param>
        /// <param name="tranCode">标记2 </param>
        /// <param name="msg">日志信息</param>
        void Info(string source, string tranCode, Object msg);

        #endregion

        #region Warn
        /// <summary>
        /// Warn
        /// </summary>
        /// <param name="source">标记1</param>
        /// <param name="tranCode">标记2 记录文件日志时作为文件夹名存在</param>
        /// <param name="msg">日志信息</param>
        /// <param name="stack">堆栈信息 堆栈信息主要包括 模块+命名空间+类名+方法名+行号，缺省值为调用此方法的堆栈信息</param>
        void Warn(string source, string tranCode, Object msg);

        #endregion

        #region Error       
        /// <summary>
        /// Error
        /// </summary>
        /// <param name="source">标记1</param>
        /// <param name="tranCode">标记2 记录文件日志时作为文件夹名存在</param>
        /// <param name="ex">错误信息</param>
        /// <param name="messsage"></param>
        void Error(string source, string tranCode,string messsage, Exception ex);

        #endregion

        #region Fatal     
        /// <summary>
        /// Warn
        /// </summary>
        /// <param name="source">标记1</param>
        /// <param name="tranCode">标记2 记录文件日志时作为文件夹名存在</param>
        /// <param name="msg">日志信息</param>
        void Fatal(string source, string tranCode, Object msg);
        #endregion
    }
}
