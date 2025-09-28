using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonorousDAB.Helpers
{
    using System.Runtime.InteropServices;

    // Very simple class, this only exists just to set the title bar to dark mode
    public static class DwmApi
    {
        [DllImport("dwmapi.dll", PreserveSig = true)]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
    }
}
