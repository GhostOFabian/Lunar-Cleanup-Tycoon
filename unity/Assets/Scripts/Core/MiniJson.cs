// Kleiner JSON-Leser für die exportierten Spieldaten (Dictionary / List / double / string / bool / null).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LunarCleanup.Core
{
    public static class MiniJson
    {
        public static object Parse(string json) { int i = 0; return Value(json, ref i); }

        static void Ws(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }

        static object Value(string s, ref int i)
        {
            Ws(s, ref i);
            char c = s[i];
            if (c == '{') return Obj(s, ref i);
            if (c == '[') return Arr(s, ref i);
            if (c == '"') return Str(s, ref i);
            if (c == 't') { i += 4; return true; }
            if (c == 'f') { i += 5; return false; }
            if (c == 'n') { i += 4; return null; }
            return Num(s, ref i);
        }

        static Dictionary<string, object> Obj(string s, ref int i)
        {
            var d = new Dictionary<string, object>(); i++;
            for (;;)
            {
                Ws(s, ref i); if (s[i] == '}') { i++; return d; }
                string k = Str(s, ref i); Ws(s, ref i); i++; // ':'
                d[k] = Value(s, ref i); Ws(s, ref i);
                if (s[i] == ',') { i++; continue; }
                if (s[i] == '}') { i++; return d; }
            }
        }

        static List<object> Arr(string s, ref int i)
        {
            var l = new List<object>(); i++;
            for (;;)
            {
                Ws(s, ref i); if (s[i] == ']') { i++; return l; }
                l.Add(Value(s, ref i)); Ws(s, ref i);
                if (s[i] == ',') { i++; continue; }
                if (s[i] == ']') { i++; return l; }
            }
        }

        static string Str(string s, ref int i)
        {
            var sb = new StringBuilder(); i++;
            while (s[i] != '"')
            {
                char c = s[i++];
                if (c != '\\') { sb.Append(c); continue; }
                char e = s[i++];
                switch (e)
                {
                    case 'n': sb.Append('\n'); break;
                    case 't': sb.Append('\t'); break;
                    case 'r': sb.Append('\r'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'u': sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16)); i += 4; break;
                    default: sb.Append(e); break;
                }
            }
            i++; return sb.ToString();
        }

        static double Num(string s, ref int i)
        {
            int st = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            return double.Parse(s.Substring(st, i - st), NumberStyles.Float, CultureInfo.InvariantCulture);
        }
    }

    /// <summary>Bequemer Zugriff auf geparste JSON-Werte.</summary>
    public static class J
    {
        public static Dictionary<string, object> O(object o) => o as Dictionary<string, object>;
        public static List<object> A(object o) => o as List<object>;
        public static object G(object o, string k) { var d = O(o); return d != null && d.TryGetValue(k, out var v) ? v : null; }
        public static double D(object o, string k, double def = 0) { var v = G(o, k); return v is double x ? x : v is bool b ? (b ? 1 : 0) : def; }
        public static float F(object o, string k, float def = 0) => (float)D(o, k, def);
        public static int I(object o, string k, int def = 0) => (int)Math.Round(D(o, k, def));
        public static string S(object o, string k, string def = "") { var v = G(o, k); return v as string ?? def; }
        public static bool B(object o, string k) { var v = G(o, k); return v is bool b ? b : v is double d && d != 0; }
        public static double[] Dv(object o) { var l = A(o); if (l == null) return new double[0]; var r = new double[l.Count]; for (int i = 0; i < l.Count; i++) r[i] = l[i] is double x ? x : 0; return r; }
    }
}
