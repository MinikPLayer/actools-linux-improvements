using System;
using System.Collections.Concurrent;
using System.Reflection;
using Avalonia.Interactivity;

namespace System.Windows {
    public enum RoutingStrategy {
        Tunnel,
        Bubble,
        Direct
    }

    public delegate void RoutedEventHandler(object sender, RoutedEventArgs e);

    /// <summary>
    /// Replacement for WPF’s EventManager.RegisterRoutedEvent(), creates Avalonia’s routed events.
    /// </summary>
    public static class EventManager {
        private static readonly MethodInfo RegisterMethod = typeof(EventManager).GetMethod(nameof(RegisterCore), BindingFlags.NonPublic | BindingFlags.Static);

        public static RoutedEvent RegisterRoutedEvent(string name, RoutingStrategy routingStrategy, Type handlerType, Type ownerType) {
            var invoke = handlerType.GetMethod("Invoke");
            var argsType = invoke == null ? typeof(RoutedEventArgs) : invoke.GetParameters()[1].ParameterType;
            if (!typeof(RoutedEventArgs).IsAssignableFrom(argsType)) {
                argsType = typeof(RoutedEventArgs);
            }

            var strategy = routingStrategy == RoutingStrategy.Bubble ? RoutingStrategies.Bubble
                    : routingStrategy == RoutingStrategy.Tunnel ? RoutingStrategies.Tunnel : RoutingStrategies.Direct;
            return (RoutedEvent)RegisterMethod.MakeGenericMethod(ownerType, argsType).Invoke(null, new object[] { name, strategy });
        }

        private static RoutedEvent RegisterCore<TOwner, TArgs>(string name, RoutingStrategies strategy) where TArgs : RoutedEventArgs {
            return RoutedEvent.Register<TOwner, TArgs>(name, strategy);
        }
    }
}
