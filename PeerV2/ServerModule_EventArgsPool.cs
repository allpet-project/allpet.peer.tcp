using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace light.asynctcp
{
     partial class ServerModule
    {
        void InitEventArgsPool(int capacity = 1000)
        {
            poolEventArgs = new System.Collections.Concurrent.ConcurrentStack<SocketAsyncEventArgs>();
            //for (var i = 0; i < capacity; i++)
            //{
            //    poolEventArgs.Push(new SocketAsyncEventArgs());
            //}
        }
        /// <summary>
        /// SocketAsyncEventArgs 池
        /// </summary>
        System.Collections.Concurrent.ConcurrentStack<SocketAsyncEventArgs> poolEventArgs;
        //回收对对象  
        void PushBackEventArgs(SocketAsyncEventArgs item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("Items added to a SocketAsyncEventArgsPool cannot be null");
            }
            poolEventArgs.Push(item);
        }

        //分配对象  
        SocketAsyncEventArgs GetFreeEventArgs()
        {
            var b = poolEventArgs.TryPop(out SocketAsyncEventArgs args);
            if (!b)
            {
                args = new SocketAsyncEventArgs();
                args.Completed += OnCompleted;
            }
            return args;
        }
    }
}
