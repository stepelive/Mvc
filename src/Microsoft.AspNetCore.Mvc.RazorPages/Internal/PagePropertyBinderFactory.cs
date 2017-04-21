// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public static class PagePropertyBinderFactory
    {
        public static Func<Page, object, Task> CreateBinder(
            ParameterBinder parameterBinder,
            IModelMetadataProvider modelMetadataProvider,
            CompiledPageActionDescriptor actionDescriptor)
        {
            if (parameterBinder == null)
            {
                throw new ArgumentNullException(nameof(parameterBinder));
            }

            if (actionDescriptor == null)
            {
                throw new ArgumentNullException(nameof(actionDescriptor));
            }

            var bindPropertiesOnPage = actionDescriptor.ModelTypeInfo == null;
            var target = bindPropertiesOnPage ? actionDescriptor.PageTypeInfo : actionDescriptor.ModelTypeInfo;
            var propertiesToBind = GetPropertiesToBind(modelMetadataProvider, target);

            if (propertiesToBind.Count == 0)
            {
                return null;
            }

            return Bind;

            Task Bind(Page page, object model)
            {
                if (page == null)
                {
                    throw new ArgumentNullException(nameof(page));
                }

                if (!bindPropertiesOnPage && model == null)
                {
                    throw new ArgumentNullException(nameof(model));
                }

                var pageContext = page.PageContext;
                var instance = bindPropertiesOnPage ? page : model;
                return BindPropertiesAsync(parameterBinder, pageContext, instance, propertiesToBind);
            }
        }

        private static async Task BindPropertiesAsync(
            ParameterBinder parameterBinder,
            PageContext pageContext,
            object instance,
            IList<PropertyBindingInfo> propertiesToBind)
        {
            var valueProvider = await GetCompositeValueProvider(pageContext);
            for (var i = 0; i < propertiesToBind.Count; i++)
            {
                var propertyBindingInfo = propertiesToBind[i];
                var modelBindingResult = await parameterBinder.BindModelAsync(
                    pageContext, 
                    valueProvider, 
                    propertyBindingInfo.ParameterDescriptor);
                if (modelBindingResult.IsModelSet)
                {
                    var modelMetadata = propertyBindingInfo.ModelMetadata;
                    PropertyValueSetter.SetValue(
                        modelMetadata,
                        instance,
                        modelBindingResult.Model);
                }
            }
        }

        private static IList<PropertyBindingInfo> GetPropertiesToBind(
            IModelMetadataProvider modelMetadataProvider,
            TypeInfo handlerSourceTypeInfo)
        {
            var handlerType = handlerSourceTypeInfo.AsType();
            var properties = PropertyHelper.GetVisibleProperties(type: handlerType);
            var typeMetadata = modelMetadataProvider.GetMetadataForType(handlerType);

            // If the type has a [BindPropertyAttribute] then we'll consider any and all public properties bindable.
            var bindPropertyOnType = handlerSourceTypeInfo.GetCustomAttribute<BindPropertyAttribute>();

            var propertyBindingInfo = new List<PropertyBindingInfo>();
            for (var i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                var bindingInfo = BindingInfo.GetBindingInfo(property.Property.GetCustomAttributes());

                if (bindingInfo == null && bindPropertyOnType == null)
                {
                    continue;
                }

                var propertyMetadata = typeMetadata.Properties[property.Name];
                if (propertyMetadata == null)
                {
                    continue;
                }

                var descriptor = new PageBoundPropertyDescriptor()
                {
                    BindingInfo = bindingInfo ?? new BindingInfo(),
                    Name = property.Name,
                    Property = property.Property,
                    ParameterType = property.Property.PropertyType,
                };

                propertyBindingInfo.Add(new PropertyBindingInfo(descriptor, propertyMetadata));
            }

            return propertyBindingInfo;
        }

        private static async Task<CompositeValueProvider> GetCompositeValueProvider(PageContext pageContext)
        {
            var factories = pageContext.ValueProviderFactories;
            var valueProviderFactoryContext = new ValueProviderFactoryContext(pageContext);
            for (var i = 0; i < factories.Count; i++)
            {
                var factory = factories[i];
                await factory.CreateValueProviderAsync(valueProviderFactoryContext);
            }

            return new CompositeValueProvider(valueProviderFactoryContext.ValueProviders);
        }

        private struct PropertyBindingInfo
        {
            public PropertyBindingInfo(
                PageBoundPropertyDescriptor parameterDescriptor,
                ModelMetadata modelMetadata)
            {
                ParameterDescriptor = parameterDescriptor;
                ModelMetadata = modelMetadata;
            }

            public PageBoundPropertyDescriptor ParameterDescriptor { get; }

            public ModelMetadata ModelMetadata { get; }
        }
    }
}
