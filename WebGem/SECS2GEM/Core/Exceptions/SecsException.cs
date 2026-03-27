namespace SECS2GEM.Core.Exceptions
{
    /// <summary>
    /// SECS异常基类
    /// </summary>
    /// <remarks>
    /// 所有SECS/GEM相关异常的基类。
    /// 提供统一的异常处理入口。
    /// </remarks>
    public class SecsException : Exception
    {
        /// <summary>
        /// 初始化SecsException
        /// </summary>
        public SecsException()
        {
        }

        /// <summary>
        /// 初始化SecsException
        /// </summary>
        /// <param name="message">错误消息</param>
        public SecsException(string message) : base(message)
        {
        }

        /// <summary>
        /// 初始化SecsException
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="innerException">内部异常</param>
        public SecsException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
