using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;

namespace WamBot.Twitch.Api
{
    internal class Command
    {
        private CommandAttribute _commandAttribute;
        private Type _categoryType;

        private CooldownAttribute _cooldown;
        private ChecksAttribute[] _checkAttributes;
        private Dictionary<string, DateTime> _lastRunTimes
            = new Dictionary<string, DateTime>();

        public Command(MethodInfo method, Type categoryType)
        {
            Method = method ?? throw new ArgumentNullException(nameof(method));
            _categoryType = categoryType ?? throw new ArgumentNullException(nameof(categoryType));

            var attributes = method.GetCustomAttributes(true).ToList();
            attributes.AddRange(method.DeclaringType.GetCustomAttributes(true));

            _commandAttribute = (CommandAttribute)attributes.FirstOrDefault(a => a is CommandAttribute);
            if (_commandAttribute == null)
                throw new ArgumentException("Method is not a command method");

            _cooldown = attributes.OfType<CooldownAttribute>().FirstOrDefault();
            _checkAttributes = attributes.OfType<ChecksAttribute>().ToArray();
            Default = attributes.OfType<DefaultAttribute>().Any();
        }

        internal bool Default { get; }
        internal MethodInfo Method { get; }
        public string Name => _commandAttribute.Name;
        public string Description => _commandAttribute.Description;
        public string ExtendedDescription => _commandAttribute.ExtendedDescription;
        public string[] Aliases => _commandAttribute.Aliases;

        public string Usage
            => ReflectionUtilities.GetUsage(Method);

        public bool CanExecute(CommandContext ctx)
        {
            foreach (var attribute in _checkAttributes)
            {
                if (!attribute.DoCheck(ctx))
                    return false;
            }

            if (_cooldown != null && !(ctx.Message.IsBroadcaster || ctx.Message.IsModerator))
            {
                if (_lastRunTimes.TryGetValue(_cooldown.PerUser ? ctx.Message.UserId : "", out var lastRun) && (DateTime.Now - lastRun) < _cooldown.Cooldown)
                    return false;
            }
            
            return true;
        }

        public async Task Run(CommandContext ctx, string[] args)
        {
            var parameters = new List<object> { ctx };
            var position = 0;
            using (var scope = ctx.Services.CreateScope())
            {
                var module = ActivatorUtilities.CreateInstance(scope.ServiceProvider, _categoryType);

                if (!CanExecute(ctx))
                    return;

                if (_cooldown != null && !(ctx.Message.IsBroadcaster || ctx.Message.IsModerator))
                    _lastRunTimes[_cooldown.PerUser ? ctx.Message.UserId : ""] = DateTime.Now;

                foreach (var param in ReflectionUtilities.GetMethodParameters(Method))
                {
                    object obj = null;
                    if (position < args.Length)
                    {
                        if (param.IsParams())
                        {
                            var thing = new List<object>();
                            foreach (string s in args.Skip(position))
                            {
                                obj = await ParseAndValidateParameterAsync(s, ctx, param);
                                thing.Add(obj);
                            }

                            var type = param.ParameterType.GetElementType();
                            var methods = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static);
                            var enumerable = methods.FirstOrDefault(c => c.Name == "Cast").MakeGenericMethod(type).Invoke(null, new[] { thing });
                            var array = methods.FirstOrDefault(c => c.Name == "ToArray").MakeGenericMethod(type).Invoke(null, new[] { enumerable });
                            parameters.Add(array);
                        }
                        else
                        {
                            obj = await ParseAndValidateParameterAsync(args[position], ctx, param);
                            parameters.Add(obj);
                        }
                    }
                    else
                    {
                        if (param.IsParams())
                        {
                            parameters.Add(Array.CreateInstance(param.ParameterType.GetElementType(), 0));
                        }
                        else if (!param.IsOptional)
                        {
                            throw new CommandException($"Hey! You'll need to specify something for \"{param.Name}\"!");
                        }
                        else
                        {
                            parameters.Add(param.DefaultValue);
                        }
                    }

                    position += 1;
                }

                if (Method.Invoke(module, parameters.ToArray()) is Task t)
                    await t;
            }
        }

        private async Task<object> ParseAndValidateParameterAsync(string arg, CommandContext ctx, ParameterInfo info)
        {
            var type = info.ParameterType.HasElementType ? info.ParameterType.GetElementType() : info.ParameterType;
            var obj = await ParseParameterAsync(arg, type, ctx);
            if (obj == null)
                return null;

            return ValidateParameter(obj, info);
        }

        private async Task<object> ParseParameterAsync(string arg, Type t, CommandContext ctx)
        {
            try
            {
                var converter = TypeDescriptor.GetConverter(t);
                if (converter != null && converter.CanConvertFrom(typeof(string)))
                    return converter.ConvertFromInvariantString(arg);

                var converterType = typeof(IParamConverter<>).MakeGenericType(t);
                var paramConverter = ctx.Services.GetService(converterType);
                if (paramConverter == null)
                    return null;

                var result = converterType.GetMethod("Convert").Invoke(paramConverter, new object[] { arg, ctx }) as Task<object>;
                return await result;
            }
            catch (Exception ex)
            {
                throw new CommandException(ex.Message);
            }
        }

        private object ValidateParameter(object obj, ParameterInfo info)
        {
            var context = new ValidationContext(obj) { MemberName = info.Name, DisplayName = info.Name };
            var validationAttributes = info.GetCustomAttributes<ValidationAttribute>();
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateValue(obj, context, results, validationAttributes))
            {
                var builder = new StringBuilder($"That doesn't seem right! Check what you've specified for {info.Name}!");
                if (results.Any(r => !string.IsNullOrWhiteSpace(r.ErrorMessage)))
                {
                    builder.AppendLine("```");

                    foreach (var result in results)
                    {
                        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                        {
                            builder.AppendLine(result.ErrorMessage);
                        }
                    }

                    builder.AppendLine("```");
                }

                throw new CommandException(builder.ToString());
            }

            return obj;
        }
    }
}