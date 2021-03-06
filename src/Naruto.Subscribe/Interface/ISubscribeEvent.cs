﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Naruto.Subscribe.Interface
{
    /// <summary>
    /// 张海波
    /// 2020-09-06
    /// 订阅事件的处理
    /// </summary>
    public interface ISubscribeEvent
    {
        /// <summary>
        /// 订阅消息
        /// </summary>
        /// <param name="subscribeName">订阅的名称</param>
        /// <returns></returns>
        Task SubscribeAsync(List<string> subscribeNames);
    }
}
