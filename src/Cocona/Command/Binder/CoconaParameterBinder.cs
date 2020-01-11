﻿using Cocona.CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cocona.Command.Binder
{
    public class CoconaParameterBinder
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ICoconaValueConverter _valueConverter;

        public CoconaParameterBinder(IServiceProvider serviceProvider, ICoconaValueConverter valueConverter)
        {
            _serviceProvider = serviceProvider;
            _valueConverter = valueConverter;
        }

        public object?[] Bind(CommandDescriptor commandDescriptor, CommandOption[] commandOptionValues, CommandArgument[] commandArgumentValues)
        {
            var bindParams = new object?[commandDescriptor.Parameters.Count];
            var optionValueByOption = commandOptionValues.ToDictionary(k => k.Option, v => v.Value);

            var index = 0;
            foreach (var param in commandDescriptor.Parameters)
            {
                switch (param)
                {
                    case CommandOptionDescriptor optionDesc:
                        if (optionValueByOption.TryGetValue(optionDesc, out var optionValue))
                        {
                            bindParams[index++] = _valueConverter.ConvertTo(optionDesc.OptionType, optionValue);
                        }
                        else if (!optionDesc.IsRequired)
                        {
                            bindParams[index++] = optionDesc.DefaultValue.Value;
                        }
                        else
                        {
                            // TODO: Exception type
                            throw new Exception("MissingOption");
                        }
                        break;
                    case CommandArgumentDescriptor argumentDesc:
                        // NOTE: Skip processing arguments at here.
                        index++;
                        break;
                    case CommandServiceParameterDescriptor serviceParamDesc:
                        bindParams[index++] = _serviceProvider.GetService(serviceParamDesc.ServiceType);
                        index++;
                        break;
                    case CommandIgnoredParameterDescriptor ignoredDesc:
                        bindParams[index++] = ignoredDesc.DefaultValue;
                        break;
                    default:
                        throw new NotSupportedException($"CoconaParameterBinder doesn't support '{param.GetType().FullName}' as bind target.");
                }
            }

            // arguments
            index = 0;
            var orderedArgDescWithParamIndex = commandDescriptor.Parameters
                .Select((x, i) => (Param: x, ParameterIndex: i))
                .Where(x => x.Param is CommandArgumentDescriptor)
                .Select(x => (Argument: (CommandArgumentDescriptor)x.Param, x.ParameterIndex))
                .OrderBy(k => k.Argument.Order);
            foreach (var argDesc in orderedArgDescWithParamIndex)
            {
                if (commandArgumentValues.Length == index)
                {
                    if (!argDesc.Argument.IsRequired)
                    {
                        bindParams[argDesc.ParameterIndex] = argDesc.Argument.DefaultValue.Value;
                        continue;
                    }

                    // TODO: Exception type
                    throw new Exception("MissingArgument");
                }
                bindParams[argDesc.ParameterIndex] = commandArgumentValues[index++].Value;
            }

            return bindParams;
        }
    }
}