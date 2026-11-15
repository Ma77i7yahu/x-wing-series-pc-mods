// Binary deltas for the patch installers: a C# 5 port of Colin Percival's bsdiff 4.3 algorithm
// (suffix-sorted old file, approximate matches, a "diff" block of bytewise differences and an
// "extra" block of new bytes). The patch is stored uncompressed here; the installer's zip
// deflates it, and the mostly-zero diff block compresses well.
//
// Patch layout (little endian):
//   "XWSD1\0\0\0"  int64 oldSize  int64 newSize  int32 controlCount
//   controlCount x { int64 diffLength, int64 extraLength, int64 oldSeek }
//   int64 diffBytes, diff block, int64 extraBytes, extra block
using System;
using System.Collections.Generic;
using System.IO;

static class Delta {
    static readonly byte[] Magic = { (byte)'X', (byte)'W', (byte)'S', (byte)'D', (byte)'1', 0, 0, 0 };

    // ---- apply (used by the installer) ----

    public static byte[] Apply(byte[] old, byte[] patch) {
        using (var r = new BinaryReader(new MemoryStream(patch))) {
            var m = r.ReadBytes(8);
            for (int i = 0; i < 8; i++) if (m[i] != Magic[i]) throw new InvalidDataException("Not a delta patch.");
            long oldSize = r.ReadInt64(), newSize = r.ReadInt64();
            if (oldSize != old.Length) throw new InvalidDataException("The delta is for a file of a different size.");
            int n = r.ReadInt32();
            var ctrl = new long[n * 3];
            for (int i = 0; i < ctrl.Length; i++) ctrl[i] = r.ReadInt64();
            byte[] diff = r.ReadBytes(checked((int)r.ReadInt64()));
            byte[] extra = r.ReadBytes(checked((int)r.ReadInt64()));
            var result = new byte[newSize];
            long oldPos = 0, newPos = 0, dp = 0, ep = 0;
            for (int c = 0; c < n; c++) {
                long a = ctrl[c * 3], b = ctrl[c * 3 + 1], s = ctrl[c * 3 + 2];
                if (a < 0 || b < 0 || newPos + a + b > newSize || dp + a > diff.Length || ep + b > extra.Length)
                    throw new InvalidDataException("Corrupt delta patch.");
                for (long i = 0; i < a; i++) {
                    long op = oldPos + i;
                    result[newPos + i] = (byte)(diff[dp + i] + (op >= 0 && op < old.Length ? old[op] : 0));
                }
                dp += a; newPos += a; oldPos += a;
                Buffer.BlockCopy(extra, (int)ep, result, (int)newPos, (int)b);
                ep += b; newPos += b; oldPos += s;
            }
            if (newPos != newSize) throw new InvalidDataException("Corrupt delta patch.");
            return result;
        }
    }

    // ---- create (used by the build) ----

    static void Split(int[] I, int[] V, int start, int len, int h) {
        int i, j, k, x, tmp, jj, kk;
        if (len < 16) {
            for (k = start; k < start + len; k += j) {
                j = 1; x = V[I[k] + h];
                for (i = 1; k + i < start + len; i++) {
                    if (V[I[k + i] + h] < x) { x = V[I[k + i] + h]; j = 0; }
                    if (V[I[k + i] + h] == x) { tmp = I[k + j]; I[k + j] = I[k + i]; I[k + i] = tmp; j++; }
                }
                for (i = 0; i < j; i++) V[I[k + i]] = k + j - 1;
                if (j == 1) I[k] = -1;
            }
            return;
        }
        x = V[I[start + len / 2] + h];
        jj = 0; kk = 0;
        for (i = start; i < start + len; i++) {
            if (V[I[i] + h] < x) jj++;
            if (V[I[i] + h] == x) kk++;
        }
        jj += start; kk += jj;
        i = start; j = 0; k = 0;
        while (i < jj) {
            if (V[I[i] + h] < x) i++;
            else if (V[I[i] + h] == x) { tmp = I[i]; I[i] = I[jj + j]; I[jj + j] = tmp; j++; }
            else { tmp = I[i]; I[i] = I[kk + k]; I[kk + k] = tmp; k++; }
        }
        while (jj + j < kk) {
            if (V[I[jj + j] + h] == x) j++;
            else { tmp = I[jj + j]; I[jj + j] = I[kk + k]; I[kk + k] = tmp; k++; }
        }
        if (jj > start) Split(I, V, start, jj - start, h);
        for (i = 0; i < kk - jj; i++) V[I[jj + i]] = kk - 1;
        if (jj == kk - 1) I[jj] = -1;
        if (start + len > kk) Split(I, V, kk, start + len - kk, h);
    }

    static int[] SuffixSort(byte[] old) {
        int n = old.Length;
        var I = new int[n + 1]; var V = new int[n + 1];
        var buckets = new int[256];
        for (int i = 0; i < n; i++) buckets[old[i]]++;
        for (int i = 1; i < 256; i++) buckets[i] += buckets[i - 1];
        for (int i = 255; i > 0; i--) buckets[i] = buckets[i - 1];
        buckets[0] = 0;
        for (int i = 0; i < n; i++) I[++buckets[old[i]]] = i;
        I[0] = n;
        for (int i = 0; i < n; i++) V[i] = buckets[old[i]];
        V[n] = 0;
        for (int i = 1; i < 256; i++) if (buckets[i] == buckets[i - 1] + 1) I[buckets[i]] = -1;
        I[0] = -1;
        for (int h = 1; I[0] != -(n + 1); h += h) {
            int len = 0, i = 0;
            while (i < n + 1) {
                if (I[i] < 0) { len -= I[i]; i -= I[i]; }
                else {
                    if (len != 0) I[i - len] = -len;
                    len = V[I[i]] + 1 - i;
                    Split(I, V, i, len, h);
                    i += len; len = 0;
                }
            }
            if (len != 0) I[i - len] = -len;
        }
        for (int i = 0; i < n + 1; i++) I[V[i]] = i;
        return I;
    }

    static int MatchLen(byte[] a, int ai, byte[] b, int bi) {
        int i = 0;
        while (ai + i < a.Length && bi + i < b.Length && a[ai + i] == b[bi + i]) i++;
        return i;
    }

    static int Compare(byte[] a, int ai, byte[] b, int bi) {
        int n = Math.Min(a.Length - ai, b.Length - bi);
        for (int i = 0; i < n; i++) if (a[ai + i] != b[bi + i]) return a[ai + i] < b[bi + i] ? -1 : 1;
        return 0;
    }

    static int Search(int[] I, byte[] old, byte[] nw, int scan, int st, int en, out int pos) {
        while (en - st >= 2) {
            int x = st + (en - st) / 2;
            if (Compare(old, I[x], nw, scan) < 0) st = x; else en = x;
        }
        int a = MatchLen(old, I[st], nw, scan), b = MatchLen(old, I[en], nw, scan);
        if (a > b) { pos = I[st]; return a; }
        pos = I[en]; return b;
    }

    public static byte[] Create(byte[] old, byte[] nw) {
        var ctrl = new List<long>();
        var db = new MemoryStream(); var eb = new MemoryStream();
        if (old.Length == 0) {
            ctrl.Add(0); ctrl.Add(nw.Length); ctrl.Add(0);
            eb.Write(nw, 0, nw.Length);
        } else {
            int[] I = SuffixSort(old);
            int oldsize = old.Length, newsize = nw.Length;
            int scan = 0, len = 0, pos = 0, lastscan = 0, lastpos = 0, lastoffset = 0;
            while (scan < newsize) {
                int oldscore = 0, scsc;
                for (scsc = scan += len; scan < newsize; scan++) {
                    len = Search(I, old, nw, scan, 0, oldsize, out pos);
                    for (; scsc < scan + len; scsc++)
                        if (scsc + lastoffset < oldsize && old[scsc + lastoffset] == nw[scsc]) oldscore++;
                    if ((len == oldscore && len != 0) || len > oldscore + 8) break;
                    if (scan + lastoffset < oldsize && old[scan + lastoffset] == nw[scan]) oldscore--;
                }
                if (len != oldscore || scan == newsize) {
                    int s = 0, Sf = 0, lenf = 0, i;
                    for (i = 0; lastscan + i < scan && lastpos + i < oldsize; ) {
                        if (old[lastpos + i] == nw[lastscan + i]) s++;
                        i++;
                        if (s * 2 - i > Sf * 2 - lenf) { Sf = s; lenf = i; }
                    }
                    int lenb = 0;
                    if (scan < newsize) {
                        s = 0; int Sb = 0;
                        for (i = 1; scan >= lastscan + i && pos >= i; i++) {
                            if (old[pos - i] == nw[scan - i]) s++;
                            if (s * 2 - i > Sb * 2 - lenb) { Sb = s; lenb = i; }
                        }
                    }
                    if (lastscan + lenf > scan - lenb) {
                        int overlap = (lastscan + lenf) - (scan - lenb);
                        s = 0; int Ss = 0, lens = 0;
                        for (i = 0; i < overlap; i++) {
                            if (nw[lastscan + lenf - overlap + i] == old[lastpos + lenf - overlap + i]) s++;
                            if (nw[scan - lenb + i] == old[pos - lenb + i]) s--;
                            if (s > Ss) { Ss = s; lens = i + 1; }
                        }
                        lenf += lens - overlap;
                        lenb -= lens;
                    }
                    for (i = 0; i < lenf; i++) db.WriteByte((byte)(nw[lastscan + i] - old[lastpos + i]));
                    int extraLen = (scan - lenb) - (lastscan + lenf);
                    eb.Write(nw, lastscan + lenf, extraLen);
                    ctrl.Add(lenf); ctrl.Add(extraLen); ctrl.Add((pos - lenb) - (lastpos + lenf));
                    lastscan = scan - lenb;
                    lastpos = pos - lenb;
                    lastoffset = pos - scan;
                }
            }
        }
        var outp = new MemoryStream();
        using (var w = new BinaryWriter(outp)) {
            w.Write(Magic); w.Write((long)old.Length); w.Write((long)nw.Length);
            w.Write(ctrl.Count / 3);
            foreach (var c in ctrl) w.Write(c);
            w.Write(db.Length); db.WriteTo(outp);
            w.Write(eb.Length); eb.WriteTo(outp);
        }
        return outp.ToArray();
    }
}
