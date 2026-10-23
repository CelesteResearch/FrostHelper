using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Commands.Binding
{
    internal static class ScalarFunctionCompatibility
    {
        internal static readonly MethodInfo Log2Method = BindLog2();

        internal static readonly Func<float, float> Log2 =
            (Func<float, float>)Log2Method.CreateDelegate(typeof(Func<float, float>));

        private static MethodInfo BindLog2()
        {
            var method = typeof(MathF).GetMethod("Log2", BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(float) }, null);
            if (method is not null) return method;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return typeof(ScalarFunctionCompatibility).GetMethod(nameof(WindowsLog2),
                    BindingFlags.NonPublic | BindingFlags.Static)!;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return typeof(ScalarFunctionCompatibility).GetMethod(nameof(MacLog2),
                    BindingFlags.NonPublic | BindingFlags.Static)!;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return typeof(ScalarFunctionCompatibility).GetMethod(nameof(LinuxLog2),
                    BindingFlags.NonPublic | BindingFlags.Static)!;
            throw new PlatformNotSupportedException("The runtime does not provide single precision log2.");
        }

        [DllImport("ucrtbase.dll", EntryPoint = "log2f", CallingConvention = CallingConvention.Cdecl)]
        private static extern float WindowsLog2(float value);

        [DllImport("libm.so.6", EntryPoint = "log2f", CallingConvention = CallingConvention.Cdecl)]
        private static extern float LinuxLog2(float value);

        [DllImport("/usr/lib/libSystem.B.dylib", EntryPoint = "log2f", CallingConvention = CallingConvention.Cdecl)]
        private static extern float MacLog2(float value);
    }
}
