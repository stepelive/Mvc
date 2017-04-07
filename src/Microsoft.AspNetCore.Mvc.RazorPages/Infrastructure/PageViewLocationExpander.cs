// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Razor;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class PageViewLocationExpander : IViewLocationExpander
    {
        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            if (string.IsNullOrEmpty(context.PageName))
            {
                // Not a page - just act natural.
                return viewLocations;
            }

            return ExpandPageHierarchy();

            IEnumerable<string> ExpandPageHierarchy()
            {
                var first = true;
                var end = context.PageName.Length;
                while (end > 0 && (end = context.PageName.LastIndexOf('/', end - 1)) != -1)
                {
                    foreach (var location in viewLocations)
                    {
                        if (location.Contains("{1}"))
                        {
                            yield return location.Replace("{1}", context.PageName.Substring(0, end));
                        }
                        else if (first)
                        {
                            yield return location;
                        }
                    }
                    
                    first = false;
                }
            }
        }

        public void PopulateValues(ViewLocationExpanderContext context)
        {
        }
    }
}
