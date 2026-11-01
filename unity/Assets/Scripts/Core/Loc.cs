// Sprache Deutsch/Englisch (Fabian 01.11.2026). Der Spielcode bleibt deutsch; übersetzt wird erst beim Anzeigen (LText).
// Wörterbuch Resources/Data/en.json: ganze Texte und Textbausteine (die Stücke, aus denen der Code Sätze zusammensetzt).
// Tr() sucht zuerst den ganzen Text, sonst ersetzt es Bausteine von vorn nach hinten (längster Treffer, nur an Wortgrenzen)
// und stellt deutsche Zahlen (1.234,5) auf englische (1,234.5) um.
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace LunarCleanup.Core
{
    public static class Loc
    {
        const string Key = "LunarCleanup.Sprache";
        public static bool En { get; private set; }
        public static event Action Changed;
        static Dictionary<string, string> dict;
        static readonly Dictionary<string, string> cache = new Dictionary<string, string>();
        class Node { public Dictionary<char, Node> next; public string val; }
        static Node root;
        /// <summary>Nur im Editor: zusammengesetzte Texte und ihre Übersetzung (zum Prüfen, Befehl „locmiss“).</summary>
        public static readonly Dictionary<string, string> Seen = new Dictionary<string, string>();
        static bool inited;

        public static void Init()
        {
            if (inited) return; inited = true;
            string lang = PlayerPrefs.GetString(Key, "");
            if (lang == "") lang = Application.systemLanguage == SystemLanguage.German ? "de" : "en";
            En = lang == "en";
        }
        public static void Set(bool en)
        {
            Init(); if (En == en) return; En = en; PlayerPrefs.SetString(Key, en ? "en" : "de"); PlayerPrefs.Save(); cache.Clear();
            Changed?.Invoke();
        }

        static void Load()
        {
            dict = new Dictionary<string, string>(); root = new Node();
            var ta = Resources.Load<TextAsset>("Data/en");
            if (ta != null && MiniJson.Parse(ta.text) is Dictionary<string, object> m)
                foreach (var kv in m) if (kv.Value is string v && v != kv.Key) dict[kv.Key] = v;
            foreach (var kv in dict)
            {
                string k = kv.Key; if (k.Length < 2 || !HasLetter(k)) continue;
                var n = root; foreach (char c in k) { n.next ??= new Dictionary<char, Node>(); if (!n.next.TryGetValue(c, out var nx)) n.next[c] = nx = new Node(); n = nx; }
                n.val = kv.Value;
            }
        }
        static bool HasLetter(string s) { foreach (char c in s) if (char.IsLetter(c)) return true; return false; }

        /// <summary>Anzeigetext in der gewählten Sprache.</summary>
        public static string Tr(string s)
        {
            if (!inited) Init();
            if (!string.IsNullOrEmpty(s) && s[0] == '\u2060') return s.Substring(1); /* schon übersetzt (Flaggengeschichten) */
            if (!En || string.IsNullOrEmpty(s)) return s;
            if (dict == null) Load();
            if (cache.TryGetValue(s, out var r)) return r;
            if (dict.TryGetValue(s, out var ex)) r = ex; else { r = Pieces(s); if (Application.isEditor && Seen.Count < 20000) Seen[s] = r; }
            r = Numbers(r);
            if (cache.Count > 6000) cache.Clear();
            cache[s] = r; return r;
        }

        static string Pieces(string s)
        {
            var sb = new StringBuilder(s.Length + 16); int i = 0, n = s.Length;
            while (i < n)
            {
                // Rich-Text-Tags unverändert übernehmen
                if (s[i] == '<') { int e = s.IndexOf('>', i); if (e > i && e - i < 40) { sb.Append(s, i, e - i + 1); i = e + 1; continue; } }
                bool startOk = !char.IsLetter(s[i]) || i == 0 || !char.IsLetter(s[i - 1]);
                int best = -1; string bv = null;
                if (startOk)
                {
                    var nd = root;
                    for (int j = i; j < n; j++)
                    {
                        if (nd.next == null || !nd.next.TryGetValue(s[j], out nd)) break;
                        if (nd.val != null && (!char.IsLetter(s[j]) || j + 1 >= n || !char.IsLetter(s[j + 1]))) { best = j + 1; bv = nd.val; }
                    }
                }
                if (best > 0) { sb.Append(bv); i = best; }
                else { sb.Append(s[i]); i++; }
            }
            return sb.ToString();
        }

        static readonly Regex deNum = new Regex(@"(?<![\d.,])(\d{1,3}(?:\.\d{3})+(?:,\d+)?|\d+,\d+)(?![\d]|[.,]\d)", RegexOptions.Compiled);
        static readonly Regex pct = new Regex(@"(\d) %", RegexOptions.Compiled);
        static string Numbers(string s)
        {
            if (s.IndexOf(',') >= 0 || s.IndexOf('.') >= 0) s = deNum.Replace(s, m => { var t = m.Value; var b = new StringBuilder(t.Length); foreach (char c in t) b.Append(c == '.' ? ',' : c == ',' ? '.' : c); return b.ToString(); });
            if (s.IndexOf('%') >= 0) s = pct.Replace(s, "$1%");
            return s;
        }
    }
}
