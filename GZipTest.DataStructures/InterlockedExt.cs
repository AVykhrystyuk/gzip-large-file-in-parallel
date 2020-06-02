using System.Runtime.CompilerServices;
using System.Threading;

namespace GZipTest.DataStructures
{
    internal static class InterlockedExt
    {
        // ReSharper disable once IdentifierTypo
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CAS(ref int location, int comparand, int newValue)
        {
            return Interlocked.CompareExchange(ref location, newValue, comparand) == comparand;
        }

        // ReSharper disable once IdentifierTypo
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CAS<T>(ref T location, T comparand, T newValue)
            where T : class?
        {
            return Interlocked.CompareExchange(ref location, newValue, comparand) == comparand;
        }

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public static T Read<T>(ref T location)
        //     where T : class?
        // {
        //     return Interlocked.CompareExchange(ref location, default!, comparand: default!);
        // }
    }
}
