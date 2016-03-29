﻿using System.Linq;
using System.Windows;

namespace ErrH.Wpf.net45.Extensions
{
    public static class StyleExtensions
    {
        public static T? FindSetter<T>(this Style style, DependencyProperty property) where T : struct
        {
            if (style == null) return null;

            //if (style.Setters.Count == 0) return null;

            var setr = style.Setters.FirstOrDefault(x
                => ((Setter)x).Property == property) as Setter;

            if (setr != null) return (T?)setr.Value;

            if (style.BasedOn != null)
            {
                var val = style.BasedOn.FindSetter<T>(property);
                if (val.HasValue) return val;
            }
            return null;
        }
    }
}
