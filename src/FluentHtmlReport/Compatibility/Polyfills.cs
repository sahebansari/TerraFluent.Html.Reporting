#if NETSTANDARD2_0
namespace System.Runtime.CompilerServices
{
    // netstandard2.0 has no built-in support for C# 9 init-only setters; the compiler
    // only needs this marker type to exist, it never gets instantiated at runtime.
    internal static class IsExternalInit
    {
    }
}
#endif
