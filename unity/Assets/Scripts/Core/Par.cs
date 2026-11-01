// Parallele Schleife, die überall läuft: im Browser (WebGL ohne Threads) einfach nacheinander –
// Parallel.For wartet dort auf Threads, die es nicht gibt, und das Spiel bleibt beim Laden hängen (Fabian 01.11.).
using System;

namespace LunarCleanup.Core
{
    public static class Par
    {
        public static void For(int from, int to, Action<int> body)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            for (int i = from; i < to; i++) body(i);
#else
            System.Threading.Tasks.Parallel.For(from, to, body);
#endif
        }
    }
}
