using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Create.ImportClasses
{
    internal class ViewNameGetter
    {
        public static string ExtractViewName(string fileName)
        {
            if (fileName.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
                fileName = fileName.Substring(0, fileName.Length - 4);

            string toRemove = "exported_view - Floor Plan - ";
            if (fileName.StartsWith(toRemove, StringComparison.OrdinalIgnoreCase))
                fileName = fileName.Substring(toRemove.Length);

            return fileName.Trim();
        }
    }
}
