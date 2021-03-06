﻿using Naruto.Subscribe;
using Naruto.Subscribe.Extension;
using Naruto.Subscribe.Interface;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Naruto.Subscribe.Internal
{
    public interface IDynamicMethodExpression : IDisposable
    {
        /// <summary>
        /// 执行动态方法
        /// </summary>
        /// <param name="service">服务</param>
        /// <param name="action">方法名</param>
        /// <param name="isParameter">是否是有参数的s</param>
        /// <param name="parameterType">参数类型</param>
        /// <param name="parameterEntity">参数</param>
        /// <returns></returns>
        Task ExecAsync(object service, string action, bool isParameter, Type parameterType, object parameterEntity);
    }
    /// <summary>
    /// 张海波
    /// 2020-09-05
    /// 执行对应方法的表达式目录树
    /// </summary>
    public class DynamicMethodExpression<SubscribeType> : IDynamicMethodExpression where SubscribeType : ISubscribe
    {
        private readonly IMethodCache methodCache;

        /// <summary>
        /// 存储委托
        /// </summary>
        private static ConcurrentDictionary<string, Delegate> exec;

        public DynamicMethodExpression(IMethodCache _methodCache)
        {
            exec = new ConcurrentDictionary<string, Delegate>();
            methodCache = _methodCache;
        }

        /// <summary>
        /// 执行方法
        /// </summary>
        /// <param name="service"></param>
        /// <param name="action">执行的方法</param>
        /// <param name="parameterType">参数类型</param>
        /// <param name="isParameter">是否为有参数的</param>    
        /// <param name="parameterEntity">方法的参数</param>
        /// <returns></returns>
        public Task ExecAsync(object service, string action, bool isParameter, Type parameterType, object parameterEntity)
        {
            service.CheckNull();
            action.CheckNullOrEmpty();
            //从缓存中取
            if (exec.TryGetValue(service.GetType().Name + action, out var res))
            {
                return res.DynamicInvoke(service, parameterEntity) as Task;
            }
            return Create(service, action, isParameter, parameterType, parameterEntity);
        }


        /// <summary>
        /// 创建委托
        /// </summary>
        /// <param name="service">继承NarutoWebSocketService的服务</param>
        /// <param name="action">执行的方法</param>
        /// <param name="isParameter">是否为带参数的方法</param>
        /// <param name="parameterType">参数类型</param>
        /// <param name="parameterEntity">方法的参数</param>
        /// <returns></returns>
        private Task Create(object service, string action, bool isParameter, Type parameterType, object parameterEntity)
        {
            //定义输入参数
            var p1 = Expression.Parameter(service.GetType(), "service");
            //方法的参数对象
            var methodParameter = Expression.Parameter(!isParameter ? typeof(object) : parameterType, "methodParameter");

            //动态执行方法
            var methods = methodCache.Get(service.GetType(), action);
            if (methods.method == null)
            {
                return default;
            }
            var methodInfo = methods.method;
            //获取参数
            var parameters = methods.parameterInfos;
            //调用指定的方法
            MethodCallExpression actionCall = null;
            //验证是否方法是否 有参数
            if (!isParameter)
            {
                //执行无参方法
                actionCall = Expression.Call(p1, methodInfo);
            }
            else
            {
                //执行有参的方法
                actionCall = Expression.Call(p1, methodInfo, methodParameter);
            }
            //生成lambda
            var lambda = Expression.Lambda(actionCall, new ParameterExpression[] { p1, methodParameter });
            //获取key
            var key = service.GetType().Name + action;
            //存储
            exec.TryAdd(key, lambda.Compile());

            return exec[key].DynamicInvoke(service, parameterEntity) as Task;
        }

        public void Dispose()
        {
            exec?.Clear();
        }
    }
}
