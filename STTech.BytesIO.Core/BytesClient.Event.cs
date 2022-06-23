using STTech.BytesIO.Core.Entity;
using STTech.BytesIO.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STTech.BytesIO.Core
{
    // ===============================================================================
    // 
    //                                  委托及事件
    // 
    // ===============================================================================

    ///// <summary>
    ///// 连接成功事件委托
    ///// </summary>
    ///// <param name="sender"></param>
    ///// <param name="e"></param>
    //public delegate void ConnectedSuccessfullyHandler(object sender, ConnectedSuccessfullyEventArgs e);
    ///// <summary>
    ///// 连接失败事件委托
    ///// </summary>
    ///// <param name="sender"></param>
    ///// <param name="e"></param>
    //public delegate void ConnectionFailedHandler(object sender, ConnectionFailedEventArgs e);
    ///// <summary>
    ///// 断开连接事件委托
    ///// </summary>
    ///// <param name="sender"></param>
    ///// <param name="e"></param>
    //public delegate void DisconnectedHandler(object sender, DisconnectedEventArgs e);
    ///// <summary>
    ///// 接收到数据事件委托
    ///// </summary>
    ///// <param name="sender"></param>
    ///// <param name="e"></param>
    //public delegate void DataReceivedHandler(object sender, DataReceivedEventArgs e);
    ///// <summary>
    ///// 异常发送事件委托
    ///// </summary>
    ///// <param name="sender"></param>
    ///// <param name="e"></param>
    //public delegate void ExceptionOccursHandler(object sender, ExceptionOccursEventArgs e);
    ///// <summary>
    ///// 数据发送事件委托
    ///// </summary>
    ///// <param name="sender"></param>
    ///// <param name="e"></param>
    //public delegate void DataSentHandler(object sender, DataSentEventArgs e);

    public abstract partial class BytesClient
    {
        /// <summary>
        /// 在接收到数据时发生
        /// </summary>
        public event EventHandler<DataReceivedEventArgs> OnDataReceived;
        /// <summary>
        /// 在产生异常时发生
        /// </summary>
        public event EventHandler<ExceptionOccursEventArgs> OnExceptionOccurs;
        /// <summary>
        /// 在建立通信成功时发生
        /// </summary>
        public event EventHandler<ConnectedSuccessfullyEventArgs> OnConnectedSuccessfully;
        /// <summary>
        /// 在建立通信失败时发生
        /// </summary>
        public event EventHandler<ConnectionFailedEventArgs> OnConnectionFailed;
        /// <summary>
        /// 在通信断开时发生
        /// </summary>
        public event EventHandler<DisconnectedEventArgs> OnDisconnected;
        /// <summary>
        /// 在主动发送数据时发生
        /// </summary>
        public event EventHandler<DataSentEventArgs> OnDataSent;

        /// <summary>
        /// 触发数据已接受事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void RaiseDataReceived(object sender, DataReceivedEventArgs e)
        {
            SafelyInvokeCallback(() => { OnDataReceived?.Invoke(sender, e); });
        }

        /// <summary>
        /// 触发触发异常发生事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void RaiseExceptionOccurs(object sender, ExceptionOccursEventArgs e)
        {
            SafelyInvokeCallback(() =>
            {
                OnExceptionOccurs?.Invoke(sender, e);

                // 每次产生异常都检查连接是否正常
                if (!IsConnected)
                {
                    Disconnect(DisconnectionReasonCode.Error, e.Exception);
                }
            });
        }

        /// <summary>
        /// 触发建立连接成功事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void RaiseConnectedSuccessfully(object sender, ConnectedSuccessfullyEventArgs e)
        {
            SafelyInvokeCallback(() => { OnConnectedSuccessfully?.Invoke(sender, e); });
        }

        /// <summary>
        /// 触发连接失败事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void RaiseConnectionFailed(object sender, ConnectionFailedEventArgs e)
        {
            SafelyInvokeCallback(() => { OnConnectionFailed?.Invoke(sender, e); });
        }

        /// <summary>
        /// 触发连接已断开事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void RaiseDisconnected(object sender, DisconnectedEventArgs e)
        {
            SafelyInvokeCallback(() => { OnDisconnected?.Invoke(sender, e); });
        }

        /// <summary>
        /// 触发数据已发送事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void RaiseDataSent(object sender, DataSentEventArgs e)
        {
            SafelyInvokeCallback(() => { OnDataSent?.Invoke(sender, e); });
        }
    }
}
