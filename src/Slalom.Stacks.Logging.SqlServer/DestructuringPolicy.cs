using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using Serilog.Core;
using Serilog.Events;
using Slalom.Stacks.Reflection;
using Slalom.Stacks.Serialization;
using Slalom.Stacks.Services.Messaging;

namespace Slalom.Stacks.Logging.SqlServer
{
    internal class DestructuringPolicy : IDestructuringPolicy
    {
        static readonly HashSet<Type> BuiltInScalarTypes = new HashSet<Type>
        {
            typeof(bool),
            typeof(char),
            typeof(byte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(decimal),
            typeof(string),
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(TimeSpan),
            typeof(Guid),
            typeof(Uri)
        };

        public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
        {
            var valueType = value.GetType();
            var properties = valueType.GetPropertiesRecursive()
                                      .ToList();

            var enumerable = value as IEnumerable;
            if (enumerable != null)
            {
                if (this.IsValueTypeDictionary(valueType))
                {
                    var typeInfo = typeof(KeyValuePair<,>).MakeGenericType(valueType.GenericTypeArguments).GetTypeInfo();
                    var keyProperty = typeInfo.GetDeclaredProperty("Key");
                    var valueProperty = typeInfo.GetDeclaredProperty("Value");

                    result = new DictionaryValue(enumerable.Cast<object>()
                                                           .Select(kvp => new KeyValuePair<ScalarValue, LogEventPropertyValue>(
                                                                              (ScalarValue)propertyValueFactory.CreatePropertyValue(keyProperty.GetValue(kvp), true),
                                                                              propertyValueFactory.CreatePropertyValue(valueProperty.GetValue(kvp), true)))
                                                           .Where(kvp => kvp.Key.Value != null));
                    return true;
                }

                result = new StructureValue(new[]
                {
                    new LogEventProperty(valueType.Name, new SequenceValue(
                        ((IEnumerable)value).Cast<object>().Select(o => propertyValueFactory.CreatePropertyValue(o, true))))
                });
                return true;
            }

            if (value is ExecutionContext)
            {
                var context = (ExecutionContext)value;
                var items = new[]
                {
                    new LogEventProperty("CorrelationId", new ScalarValue(context.Request.CorrelationId)),
                    new LogEventProperty("SessionId", new ScalarValue(context.Request.SessionId)),
                    new LogEventProperty("User", new ScalarValue(context.Request.User.Identity?.Name))
                };

                result = new StructureValue(items);

                return true;
            }

            var target = new List<LogEventProperty>();
            foreach (var item in properties)
            {
                if (item.GetCustomAttributes<IgnoreAttribute>().Any())
                {
                    continue;
                }

                if (item.GetCustomAttributes<SecureAttribute>().Any())
                {
                    target.Add(new LogEventProperty(item.Name, new ScalarValue(SecureAttribute.DefaultDisplayText)));
                    continue;
                }

                object piValue = null;
                try
                {
                    piValue = item.GetValue(value);
                }
                catch
                {
                    target.Add(new LogEventProperty(item.Name, new ScalarValue("Accessing the property failed.")));
                }
                if (piValue == null)
                {
                    target.Add(new LogEventProperty(item.Name, new ScalarValue(null)));
                    continue;
                }



                if (piValue is Exception)
                {
                    var exception = (Exception)piValue;
                    var items = new List<LogEventProperty>();
                    items.Add(new LogEventProperty("Message", new ScalarValue(exception.Message)));
                    items.Add(new LogEventProperty("Source", new ScalarValue(exception.Source)));
                    items.Add(new LogEventProperty("StackTrace", new ScalarValue(exception.StackTrace)));
#if NET461
                    items.Add(new LogEventProperty("StackTrace", new ScalarValue(exception.TargetSite)));
#endif
                    target.Add(new LogEventProperty(item.Name, new StructureValue(items)));
                    continue;
                }

                // TODO: Continue to build out for specific types.
                if (piValue is ClaimsPrincipal)
                {
                    var p = (ClaimsPrincipal)piValue;
                    var items = new List<LogEventProperty>();
                    items.Add(new LogEventProperty("AuthenticationType", new ScalarValue(p.Identity.AuthenticationType)));

                    items.Add(new LogEventProperty("Claims", new DictionaryValue(p.Claims.Select(e =>
                                                                                                 new KeyValuePair<ScalarValue, LogEventPropertyValue>(new ScalarValue(e.Type), new ScalarValue(e.Value))
                        ))));

                    target.Add(new LogEventProperty(item.Name, new StructureValue(items)));

                    continue;
                }

                if (piValue is string)
                {
                    target.Add(new LogEventProperty(item.Name, new ScalarValue(piValue)));
                    continue;
                }

                if (piValue is IEnumerable)
                {
                    target.Add(new LogEventProperty(item.Name, new SequenceValue(
                        ((IEnumerable)piValue).Cast<object>().Select(o => propertyValueFactory.CreatePropertyValue(o, true)))));
                    continue;
                }

                target.Add(new LogEventProperty(item.Name, propertyValueFactory.CreatePropertyValue(piValue, true)));
            }
            result = new StructureValue(target, valueType.Name);
            return true;
        }

        bool IsValidDictionaryKeyType(Type valueType)
        {
            return BuiltInScalarTypes.Contains(valueType) ||
                   valueType.GetTypeInfo().IsEnum;
        }

        bool IsValueTypeDictionary(Type valueType)
        {
            return valueType.IsConstructedGenericType &&
                   valueType.GetGenericTypeDefinition() == typeof(Dictionary<,>) &&
                   this.IsValidDictionaryKeyType(valueType.GenericTypeArguments[0]);
        }
    }
}