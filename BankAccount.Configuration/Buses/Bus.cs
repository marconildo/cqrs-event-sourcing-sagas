﻿using System;
using System.Collections.Generic;
using System.Linq;
using BankAccount.Infrastructure;
using BankAccount.Infrastructure.Buses;
using BankAccount.Infrastructure.Commanding;
using BankAccount.Infrastructure.Eventing;
using Microsoft.Practices.Unity;

namespace BankAccount.Configuration.Buses
{
    /// <summary>
    /// this is the heart and soul of the application
    /// </summary>
    public sealed class Bus : IBus
    {
        #region Fields

        private static readonly IDictionary<Type, Type> RegisteredSagas = new Dictionary<Type, Type>();
        private static readonly IList<Type> RegisteredHandlers = new List<Type>();

        #endregion

        #region ISagaBus

        /// <summary>
        /// this one is responsible for sending commands to the corresponding saga
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command"></param>
        public void Send<T>(T command) where T : Command
        {
            this._Send(command);
        }

        /// <summary>
        /// this one raises events that have been already saved by event store
        /// and are dispatched by the dispatcher,
        /// and now the corresponding event handler (denormalizer) have to be notified
        /// about the aggregate changes so that the presentation layer
        /// could update it's stale data
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="event"></param>
        public void RaiseEvent<T>(T @event) where T : DomainEvent
        {
            this._Send(@event);
        }

        public void RegisterSaga<T>()
        {
            var sagaType = typeof(T);
            if (!sagaType.GetInterfaces().Any(i => i.Name.StartsWith(typeof(IAmStartedBy<>).Name)))
            {
                throw new InvalidOperationException("The specified saga must implement the IAmStartedBy<T> interface.");
            }

            var messageType = sagaType.
                GetInterfaces().
                First(i => i.Name.StartsWith(typeof(IAmStartedBy<>).Name)).
                GenericTypeArguments.
                First();

            RegisteredSagas.Add(messageType, sagaType);
        }

        public void RegisterHandler<T>()
        {
            RegisteredHandlers.Add(typeof(T));
        }

        #endregion

        #region Helpers

        private void _Send<T>(T message)
        {
            BootRegisteredSagas(message);
            DeliverMessageToRunningSagas(message);
            DeliverMessageToHandlers(message);
        }

        private void BootRegisteredSagas<T>(T message)
        {
            var messageType = message.GetType();
            var openInterface = typeof(IAmStartedBy<>);
            var closedInterface = openInterface.MakeGenericType(messageType);
            var sagasToStartup = from s in RegisteredSagas.Values
                                 where closedInterface.IsAssignableFrom(s)
                                 select s;
            foreach (var s in sagasToStartup)
            {
                dynamic sagaInstance = IoCServiceLocator.Container.Resolve(s);
                sagaInstance.Handle((dynamic)message);
            }
        }

        private void DeliverMessageToRunningSagas<T>(T message)
        {
            var messageType = message.GetType();
            var openInterface = typeof(IHandleMessage<>);
            var closedInterface = openInterface.MakeGenericType(messageType);
            var sagasToNotify = from s in RegisteredSagas.Values
                                where closedInterface.IsAssignableFrom(s)
                                select s;
            foreach (var s in sagasToNotify)
            {
                dynamic sagaInstance = IoCServiceLocator.Container.Resolve(s);
                sagaInstance.Handle((dynamic)message);
            }
        }

        private void DeliverMessageToHandlers<T>(T message)
        {
            if (RegisteredHandlers == null || !RegisteredHandlers.Any())
                return;

            var messageType = message.GetType();
            var openInterface = typeof(IHandleMessage<>);
            var closedInterface = openInterface.MakeGenericType(messageType);
            var handlersToNotify = from h in RegisteredHandlers
                                   where closedInterface.IsAssignableFrom(h)
                                   select h;
            foreach (var handlerInstance in handlersToNotify.Select(h => IoCServiceLocator.Container.Resolve(h)))
            {
                ((dynamic)handlerInstance).Handle((dynamic)message);
            }
        }

        #endregion
    }
}
