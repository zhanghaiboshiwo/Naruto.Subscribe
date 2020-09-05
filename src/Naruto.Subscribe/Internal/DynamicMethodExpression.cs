﻿using Naruto.Subscribe;
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
    public interface IDynamicMethodExpression
    {
        /// <summary>
        /// 执行动态方法
        /// </summary>
        /// <param name="service">服务</param>
        /// <param name="action">方法名</param>
        /// <param name="parameterEntity">参数</param>
        /// <returns></returns>
        Task ExecAsync(object service, string action, object parameterEntity);
    }
    /// <summary>
    /// 张海波
    /// 2020-09-05
    /// 执行对应方法的表达式目录树
    /// </summary>
    public class DynamicMethodExpression<SubscribeType> : IDynamicMethodExpression where SubscribeType : ISubscribe
    {
        /// <summary>
        /// 存储委托
        /// </summary>
        private static ConcurrentDictionary<string, Delegate> exec;

        public DynamicMethodExpression()
        {
            exec = new ConcurrentDictionary<string, Delegate>();
        }

        /// <summary>
        /// 执行方法
        /// </summary>
        /// <param name="service"></param>
        /// <param name="action">执行的方法</param>
        /// <param name="parameterEntity">方法的参数</param>
        /// <returns></returns>
        public Task ExecAsync(object service, string action, object parameterEntity)
        {
            //从缓存中取
            if (exec.TryGetValue(service.GetType().Name + action, out var res))
            {
                return res.DynamicInvoke(service, parameterEntity) as Task;
            }
            return Create(service, action, parameterEntity);
        }


        /// <summary>
        /// 创建委托
        /// </summary>
        /// <param name="service">继承NarutoWebSocketService的服务</param>
        /// <param name="action">执行的方法</param>
        /// <param name="parameterEntity">方法的参数</param>
        /// <returns></returns>
        private static Task Create(object service, string action, object parameterEntity)
        {
            //定义输入参数
            var p1 = Expression.Parameter(service.GetType(), "service");
            //方法的参数对象
            var methodParameter = Expression.Parameter(parameterEntity == null ? typeof(object) : parameterEntity.GetType(), "methodParameter");

            //动态执行方法
            var method = service.GetType().GetMethod(action, BindingFlags.Public | BindingFlags.Instance);
            //获取参数
            var parameters = method.GetParameters();
            //调用指定的方法
            MethodCallExpression actionCall = null;
            //验证是否方法是否 有参数
            if (parameters.Count() == 0)
            {
                //执行无参方法
                actionCall = Expression.Call(p1, method);
            }
            else
            {
                //执行有参的方法
                actionCall = Expression.Call(p1, method, methodParameter);
            }
            //生成lambda
            var lambda = Expression.Lambda(actionCall, new ParameterExpression[] { p1, methodParameter });
            //获取key
            var key = service.GetType().Name + action;
            //存储
            exec.TryAdd(key, lambda.Compile());

            return exec[key].DynamicInvoke(service, parameterEntity) as Task;
        }
    }
}
