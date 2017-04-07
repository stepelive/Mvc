// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class RazorPagesRazorViewEngineOptionsSetup : IConfigureOptions<RazorViewEngineOptions>
    {
        public void Configure(RazorViewEngineOptions options)
        {
            options.ViewLocationExpanders.Add(new PageViewLocationExpander());
        }
    }
}
