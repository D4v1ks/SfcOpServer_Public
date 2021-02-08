#pragma warning disable CA1031, CA1034, IDE0057

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Text;

namespace SfcOpServer
{
    public class GameFile
    {
        private const string ASPAS = "\"";
        private const string SEPARATOR = "/";

        public class Entry
        {
            [Flags]
            public enum Flags : int
            {
                None = 0,

                Path = 1,
                Key = 2,
                Value = 4,
                Comment = 8,

                Quotes = 16,

                // composite flags

                IsEmpty = None,
                IsComment = Comment,
                IsPath = Path | Comment,
                IsEntry = Path | Key | Value | Comment,
            }

            public string Path { get; set; }
            public string Key { get; set; }
            public string Value { get; set; }
            public string Comment { get; set; }

            public bool Quotes { get; set; }

            public bool IsEmpty => Path == null && Key == null && Value == null && Comment == null;
            public bool IsComment => Path == null && Key == null && Value == null && Comment != null;
            public bool IsPath => Path != null && Key == null && Value == null && Comment != null;
            public bool IsEntry => Path != null && Key != null && Value != null && Comment != null;

            public Entry()
            { }

            public Entry(string comment)
            {
                Contract.Assert(comment != null && comment.Length > 0);

                Comment = comment;
            }

            public Entry(string path, string comment)
            {
                Contract.Assert(path != null && comment != null);

                Path = path;
                Comment = comment;
            }

            public Entry(string path, string key, string value, string comment, bool quotes)
            {
                Contract.Assert(path != null && key != null && key.Length > 0 && value != null && (value.Length > 0 || quotes) && comment != null);

                Path = path;
                Key = key;
                Value = value;
                Comment = comment;

                Quotes = quotes;
            }

            public Flags GetFlags()
            {
                Flags f = Flags.None;

                if (Path != null) f |= Flags.Path;
                if (Key != null) f |= Flags.Key;
                if (Value != null) f |= Flags.Value;
                if (Comment != null) f |= Flags.Comment;

                if (Quotes) f |= Flags.Quotes;

                return f;
            }

            public bool IsEqual(Entry entry)
            {
                Contract.Assert(entry != null);

                Flags a = GetFlags();
                Flags b = entry.GetFlags();

                if (a != b)
                    return false;

                Flags c = a & b;

                return
                    ((c & Flags.Path) == Flags.None || Path.Equals(entry.Path, StringComparison.Ordinal)) &&
                    ((c & Flags.Key) == Flags.None || Key.Equals(entry.Key, StringComparison.Ordinal)) &&
                    ((c & Flags.Value) == Flags.None || Value.Equals(entry.Value, StringComparison.Ordinal)) &&
                    ((c & Flags.Comment) == Flags.None || Comment.Equals(entry.Comment, StringComparison.Ordinal));
            }
        }

        public string Filename { get; set; }
        public string Root { get; set; }

        public bool IsEmpty => _entries.Count == 0;

        private readonly char[] _letters;
        private readonly char[] _separators;
        private readonly char[] _spaces;

        private readonly Dictionary<string, Entry> _entries;

        public GameFile()
        {
            Filename = string.Empty;
            Root = string.Empty;

            _letters = "#0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz".ToCharArray();
            _separators = new char[] { ',' };
            _spaces = new char[] { '\t', ' ' };

            _entries = new Dictionary<string, Entry>();

#if DEBUG
            Filename = "Test.gf";
            Root = "Test";

            AddOrUpdate("values", "integer", 5000);
            AddOrUpdate("strings", "quoted", "Carlos", true);
            AddOrUpdate("values", "single", 0.005f);
            AddOrUpdate("strings", "unquoted", "Carlos", "removed quotes", false);

            Debug.Assert(TryGetValue("values", "integer", out int i) && i == 5000);
            Debug.Assert(TryGetValue("values", "single", out float f) && f == 0.005f);

            Debug.Assert(TryRemoveKey("strings", "quoted"));
            Debug.Assert(!TryGetValue("strings", "quoted", out string t, out bool q));

            q = false;

            Debug.Assert(TryGetValue("strings", "unquoted", out t, out q) && t.Equals("Carlos", StringComparison.Ordinal));
            Debug.Assert(TryRemovePath("values"));
            Debug.Assert(TryRemovePath("strings"));

            AddOrUpdate("temp");

            Debug.Assert(TryRemovePath("temp"));
            Debug.Assert(_entries.Count == 0);

            Clear();
#endif

        }

        public void Clear()
        {
            Filename = string.Empty;
            Root = string.Empty;

            _entries.Clear();
        }

        public bool Load(StreamReader r)
        {
            if (r == null || _entries.Count > 0)
                return false;

            Initialize(out string commentPrefix, out int flags);

            int lineNumber = 0;
            string path = string.Empty;

            while (!r.EndOfStream)
            {
                string t = r.ReadLine().Trim(_spaces);

                lineNumber++;

                if (t.Length == 0)
                    continue;

                string k;

                // checks if it is a comment

                string comment;

                if (commentPrefix != null && t.StartsWith(commentPrefix, StringComparison.Ordinal))
                {
                    comment = t.Substring(commentPrefix.Length).TrimStart(_spaces);

                    if (comment.Length > 0)
                    {
                        k = _entries.Count.ToString(CultureInfo.InvariantCulture);

                        _entries.Add(k, new Entry(comment));
                    }

                    continue;
                }

                // checks if it is a path or a key&value pair

                string key;
                string value;
                bool quotes;

                if (t.StartsWith("[", StringComparison.Ordinal))
                {
                    if (t.EndsWith("]", StringComparison.Ordinal))
                    {
                        path = t.Substring(1, t.Length - 2);
                        comment = string.Empty;

                        goto addPath;
                    }

                    // checks if there is a comment attached

                    if (commentPrefix != null)
                    {
                        int i = t.IndexOf(']', 2);

                        if (i >= 2)
                        {
                            comment = t.Substring(i + 1).TrimStart(_spaces);

                            if (comment.StartsWith(commentPrefix, StringComparison.Ordinal))
                            {
                                path = t.Substring(1, i - 1);
                                comment = comment.Substring(commentPrefix.Length).TrimStart(_spaces);

                                goto addPath;
                            }
                        }
                    }
                }
                else
                {
                    int i = t.IndexOf("=", StringComparison.Ordinal);

                    if (i < 1)
                        goto badFormat;

                    key = t.Substring(0, i).TrimEnd(_spaces);

                    if (key.TrimEnd(_letters).Length != 0)
                        goto badFormat;

                    value = t.Substring(i + 1).TrimStart(_spaces);
                    comment = string.Empty;

                    // checks if there is a comment attached

                    if (commentPrefix != null)
                    {
                        i = value.IndexOf(commentPrefix, StringComparison.Ordinal);

                        if (i >= 0)
                        {
                            comment = value.Substring(i + commentPrefix.Length).TrimStart(_spaces);
                            value = value.Substring(0, i).TrimEnd(_spaces);
                        }
                    }

                    if ((flags & 1) == 1)
                    {
                        quotes = false;
                    }
                    else
                    {
                        quotes = value.StartsWith(ASPAS, StringComparison.Ordinal) && value.EndsWith(ASPAS, StringComparison.Ordinal);

                        if (quotes)
                        {
                            // checks if we have a even number of quotes

                            int c = 1;

                            i = 1;

                            do
                            {
                                i = value.IndexOf(ASPAS, i, StringComparison.Ordinal);

                                if (i == -1)
                                    break;

                                c++;
                                i++;
                            }
                            while (i < value.Length);

                            if ((c & 1) != 0)
                                goto badFormat;

                            value = value.Substring(1, value.Length - 2);
                        }
                        else
                        {
                            const NumberStyles number = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

                            i = value.IndexOfAny(_separators);

                            if (i >= 0)
                            {
                                string[] arg = value.Split(_separators, StringSplitOptions.None);

                                for (i = 0; i < arg.Length; i++)
                                {
                                    if (double.TryParse(arg[i], number, CultureInfo.InvariantCulture, out _) || ulong.TryParse(arg[i], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out _))
                                        continue;

                                    goto badFormat;
                                }
                            }
                            else if (!value.StartsWith("%", StringComparison.Ordinal) || !value.EndsWith("%", StringComparison.Ordinal))
                            {
                                if (value.StartsWith("0x", StringComparison.Ordinal))
                                {
                                    if (ulong.TryParse(value.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out _))
                                        goto addKeyValue;
                                }
                                else if (double.TryParse(value, number, CultureInfo.InvariantCulture, out _) || ulong.TryParse(value, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out _))
                                {
                                    goto addKeyValue;
                                }

                                goto badFormat;
                            }
                        }
                    }

                    goto addKeyValue;
                }

                goto badFormat;

            addKeyValue:

                if (Root.Length == 0)
                {
                    if (key.Equals("Name", StringComparison.OrdinalIgnoreCase) || key.Equals("Version", StringComparison.OrdinalIgnoreCase))
                    {
                        Root = value;
                    }
                    else if (!key.Equals("ProfilesPath", StringComparison.OrdinalIgnoreCase))
                    {
                        goto badFormat;
                    }
                }

                k = (Root + SEPARATOR + path + SEPARATOR + key).ToUpperInvariant();

                if (_entries.ContainsKey(k))
                    goto badFormat;

                _entries.Add(k, new Entry(path, key, value, comment, quotes));

                continue;

            addPath:

                path = path.Trim(_spaces);

                if (path.Length > 0)
                {
                    k = (Root + SEPARATOR + path + SEPARATOR).ToUpperInvariant();

                    if (_entries.ContainsKey(k))
                        goto badFormat;

                    _entries.Add(k, new Entry(path, comment));

                    if ((flags & 4) == 4 && path.Equals("Objects", StringComparison.Ordinal))
                        break;

                    continue;
                }

            badFormat:

                Debug.WriteLine("Unknown format at line " + lineNumber + " {" + t + "}");
            }

            return true;
        }

        public bool Load(string fileName)
        {
            if (File.Exists(fileName))
            {
                StreamReader r = null;

                try
                {
                    r = new StreamReader(fileName, Encoding.ASCII);

                    Filename = fileName;
                    Root = Path.GetFileNameWithoutExtension(Filename);

                    return Load(r);
                }
                catch (Exception)
                { }
                finally
                {
                    r?.Dispose();
                }
            }

            return false;
        }

        public bool Save(StreamWriter w, int leftPadding, int rightPadding)
        {
            if (w == null || _entries.Count == 0)
                return false;

            Initialize(out string commentPrefix, out _);

            int c = Math.Max(Math.Abs(leftPadding), Math.Abs(rightPadding));

            char[] padding;

            if (c > 0)
            {
                padding = new char[c];

                for (int i = 0; i < c; i++)
                    padding[i] = ' ';
            }
            else
            {
                padding = null;
            }

            Entry.Flags last = Entry.Flags.IsEmpty;

            c = 0;

            foreach (KeyValuePair<string, Entry> p in _entries)
            {
                Entry entry = p.Value;
                Entry.Flags flags = entry.GetFlags();

                string t;

                if ((flags & Entry.Flags.IsEntry) == Entry.Flags.IsEntry)
                {
                    t = entry.Key;

                    w.Write(t);

                    if (leftPadding < 0)
                        w.Write(padding, 0, -leftPadding);
                    else if (leftPadding > t.Length)
                        w.Write(padding, 0, leftPadding - t.Length);

                    w.Write("=");

                    if (rightPadding < 0)
                        w.Write(padding, 0, -rightPadding);

                    t = entry.Value;

                    if (entry.Quotes)
                        w.Write(ASPAS);

                    w.Write(t);

                    if (entry.Quotes)
                        w.Write(ASPAS);

                    t = entry.Comment;

                    if (t.Length > 0)
                    {
                        w.Write(" ");
                        w.Write(commentPrefix);
                        w.Write(" ");
                        w.Write(t);
                    }

                    w.WriteLine();

                    last = Entry.Flags.IsEntry;
                }
                else if ((flags & Entry.Flags.IsPath) == Entry.Flags.IsPath)
                {
                    if (c > 0)
                        w.WriteLine();

                    bool wroteSomething = false;

                    // tries to write the path

                    t = entry.Path;

                    if (t.Length > 0)
                    {
                        w.Write("[");
                        w.Write(t);
                        w.Write("]");

                        wroteSomething = true;
                    }

                    // tries to write the comment

                    t = entry.Comment;

                    if (t.Length > 0)
                    {
                        w.Write(" ");
                        w.Write(commentPrefix);
                        w.Write(" ");
                        w.Write(t);

                        wroteSomething = true;
                    }

                    // tries to finish the line

                    if (wroteSomething)
                        w.WriteLine();

                    last = Entry.Flags.IsPath;
                }
                else if ((flags & Entry.Flags.IsComment) == Entry.Flags.IsComment)
                {
                    if (c > 0 && last != Entry.Flags.IsComment)
                        w.WriteLine();

                    t = entry.Comment;

                    w.Write(commentPrefix);
                    w.Write(" ");
                    w.WriteLine(t);

                    last = Entry.Flags.IsComment;
                }
                else
                {
                    Contract.Assert(flags == Entry.Flags.IsEmpty);

                    w.WriteLine();

                    last = Entry.Flags.IsEmpty;
                }

                c++;
            }

            w.Flush();

            return true;
        }

        public bool Save(string filename, int leftPadding, int rightPadding)
        {
            if (filename != null)
            {
                Filename = filename;

                StreamWriter w = null;

                try
                {
                    w = new StreamWriter(filename, false, Encoding.ASCII);

                    return Save(w, leftPadding, rightPadding);
                }
                catch (Exception)
                { }
                finally
                {
                    w?.Dispose();
                }
            }

            return false;
        }

        public bool Save(int leftPadding, int rightPadding)
        {
            return Save(Filename, leftPadding, rightPadding);
        }

        public bool Save()
        {
            return Save(Filename, 0, 0);
        }

        public Dictionary<string, Entry> Clone()
        {
            Dictionary<string, Entry> d = new Dictionary<string, Entry>(_entries.Count);

            foreach (KeyValuePair<string, Entry> p in _entries)
            {
                string k = p.Key;
                Entry v = p.Value;

                d.Add(k, new Entry(v.Path, v.Key, v.Value, v.Comment, v.Quotes));
            }

            return d;
        }

        public void AddOrUpdate(string path, string key, string value, string comment, bool quotes)
        {
            Contract.Requires(path != null && key != null);

            string k1 = (Root + SEPARATOR + path + SEPARATOR).ToUpperInvariant();

            if (key.Length > 0)
            {
                string k2 = k1 + key.ToUpperInvariant();

                if (_entries.ContainsKey(k2))
                {
                    Entry entry = _entries[k2];

                    entry.Value = value;
                    entry.Comment = comment;

                    entry.Quotes = quotes;
                }
                else if (_entries.ContainsKey(k1))
                {
                    int c = _entries.Count;

                    if (c == 1)
                    {
                        Entry entry = new Entry(path, key, value, comment, quotes);

                        _entries.Add(k2, entry);

                        return;
                    }

                    c++;

                    string[] keys = new string[c];
                    Entry[] values = new Entry[c];

                    c = 0;

                    foreach (KeyValuePair<string, Entry> p in _entries)
                    {
                        keys[c] = p.Key;
                        values[c] = p.Value;

                        if (keys[c].Equals(k1, StringComparison.Ordinal))
                        {
                            c++;

                            keys[c] = k2;
                            values[c] = new Entry(path, key, value, comment, quotes);
                        }

                        c++;
                    }

                    _entries.Clear();

                    for (int i = 0; i < c; i++)
                        _entries.Add(keys[i], values[i]);
                }
                else
                {
                    Entry entry1 = new Entry(path, string.Empty);
                    Entry entry2 = new Entry(path, key, value, comment, quotes);

                    _entries.Add(k1, entry1);
                    _entries.Add(k2, entry2);
                }
            }
            else if (_entries.ContainsKey(k1))
            {
                Entry entry = _entries[k1];

                entry.Comment = comment;
            }
            else
            {
                Entry entry = new Entry(path, comment);

                _entries.Add(k1, entry);
            }
        }

        public void AddOrUpdate(string path, string key, string value, string comment)
        {
            AddOrUpdate(path, key, value, comment, false);
        }

        public void AddOrUpdate(string path, string key, string value, bool quotes)
        {
            AddOrUpdate(path, key, value, string.Empty, quotes);
        }

        public void AddOrUpdate(string path, string key, string value)
        {
            AddOrUpdate(path, key, value, string.Empty, false);
        }

        public void AddOrUpdate(string path, string key, int value)
        {
            AddOrUpdate(path, key, value.ToString(CultureInfo.InvariantCulture), string.Empty, false);
        }

        public void AddOrUpdate(string path, string key, float value)
        {
            string v;

            if (value == Math.Floor(value))
                v = value.ToString("f1", CultureInfo.InvariantCulture);
            else
                v = value.ToString(CultureInfo.InvariantCulture);

            AddOrUpdate(path, key, v, string.Empty, false);
        }

        public void AddOrUpdate(string path)
        {
            AddOrUpdate(path, string.Empty, string.Empty, string.Empty, false);
        }

        public bool ContainsKey(string path, string key)
        {
            string k = (Root + SEPARATOR + path + SEPARATOR + key).ToUpperInvariant();

            return _entries.ContainsKey(k);
        }

        public bool ContainsPath(string path)
        {
            string k = (Root + SEPARATOR + path + SEPARATOR).ToUpperInvariant();

            return _entries.ContainsKey(k);
        }

        public bool TryGetValue(string path, string key, out string value, out bool quotes)
        {
            string k = (Root + SEPARATOR + path + SEPARATOR + key).ToUpperInvariant();

            if (_entries.TryGetValue(k, out Entry entry))
            {
                value = entry.Value;
                quotes = entry.Quotes;

                return true;
            }

            value = string.Empty;
            quotes = false;

            return false;
        }

        public bool TryGetValue(string path, string key, out int value)
        {
            if (TryGetValue(path, key, out string v, out _) && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                return true;

            value = 0;

            return false;
        }

        public bool TryGetValue(string path, string key, out float value)
        {
            if (TryGetValue(path, key, out string v, out _) && float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return true;

            value = 0f;

            return false;
        }

        public string GetValue(string path, string key, string defaultValue)
        {
            const bool defaultQuotes = true;

            if (TryGetValue(path, key, out string value, out bool quotes) && quotes == defaultQuotes)
                return value;

            AddOrUpdate(path, key, defaultValue, defaultQuotes);

            return defaultValue;
        }

        public int GetValue(string path, string key, int defaultValue)
        {
            if (TryGetValue(path, key, out int value))
                return value;

            AddOrUpdate(path, key, defaultValue);

            return defaultValue;
        }

        public float GetValue(string path, string key, float defaultValue)
        {
            if (TryGetValue(path, key, out float value))
                return value;

            AddOrUpdate(path, key, defaultValue);

            return defaultValue;
        }

        public bool TryRemoveKey(string path, string key)
        {
            string k = (Root + SEPARATOR + path + SEPARATOR + key).ToUpperInvariant();

            return _entries.Remove(k);
        }

        public bool TryRemovePath(string path)
        {
            int c = _entries.Count;

            if (c == 0)
                return false;

            if (c == 1)
            {
                _entries.Clear();

                return true;
            }

            string[] keys = new string[c];
            Entry[] values = new Entry[c];

            c = 0;

            string k = (Root + SEPARATOR + path + SEPARATOR).ToUpperInvariant();

            foreach (KeyValuePair<string, Entry> p in _entries)
            {
                keys[c] = p.Key;
                values[c] = p.Value;

                if (keys[c].StartsWith(k, StringComparison.Ordinal))
                    continue;

                c++;
            }

            if (_entries.Count == c)
                return false;

            _entries.Clear();

            for (int i = 0; i < c; i++)
                _entries.Add(keys[i], values[i]);

            return true;
        }

        private void Initialize(out string commentPrefix, out int flags)
        {
            if (Filename.EndsWith(".ini", StringComparison.OrdinalIgnoreCase) || Filename.EndsWith(".conf", StringComparison.OrdinalIgnoreCase))
            {
                commentPrefix = ";";
                flags = 1;

                return;
            }

            if (Filename.EndsWith(".gf", StringComparison.OrdinalIgnoreCase))
            {
                commentPrefix = "//";
                flags = 2;

                return;
            }

            if (Filename.EndsWith(".mvm", StringComparison.OrdinalIgnoreCase))
            {
                commentPrefix = null;
                flags = 4;

                return;
            }

            throw new NotImplementedException();
        }
    }
}
