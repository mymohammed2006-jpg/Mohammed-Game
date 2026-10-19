using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace MG;

// Math.sin/cos/tan/atan/atan2/exp/pow exactly as Electron 44 (V8 15.2, Linux x64) computes them, bit
// for bit, on every platform. Never calls the platform libm (Math.Sin etc.); only +-*/, Math.Sqrt,
// Math.Round, Math.FusedMultiplyAdd and integer ops, which are exact or IEEE correctly rounded.
//  - sin, cos, tan, exp: V8 calls LLVM libc (built without FMA). Its first passes are ported literally;
//    when their rounding test fails, LLVM libc's 128-bit accurate pass is replaced by a correctly rounded
//    double-double path with a BigInteger fallback (Ziv loop).
//  - atan, atan2: literal ports of LLVM libc (not correctly rounded, no rounding test).
//  - pow: V8 handles y == 2 (x*x) and y == 0.5 (sqrt) itself and otherwise calls std::pow, i.e. glibc
//    2.39's pow (the FMA variant picked on any CPU with FMA); this is a port of that variant.
//    Electron on Windows/macOS uses the platform pow, so its own results can differ there.
public static partial class JS
{
	public static double sin(double x) => JsMathImpl.Sin(x);
	public static double cos(double x) => JsMathImpl.Cos(x);
	public static double tan(double x) => JsMathImpl.Tan(x);
	public static double atan(double x) => JsMathImpl.Atan(x);
	public static double atan2(double y, double x) => JsMathImpl.Atan2(y, x);
	public static double exp(double x) => JsMathImpl.Exp(x);
	public static double pow(double x, double y) => JsMathImpl.Pow(x, y);
}

internal static class JsMathImpl
{
	// Relative error bound of the double-double fast paths (actual error is below 2^-100;
	// see mathtest). If the result cannot be rounded with this margin, the slow path decides.
	const double EpsSC = 1.0 / (1L << 40) / (1L << 40) / (1L << 8);   // 2^-88
	const double EpsExp = EpsSC;

	// When set (tests only), every call takes the BigInteger path.
	internal static bool ForceSlow;
	internal static long SlowCalls;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static double BitsToD(ulong b) => BitConverter.Int64BitsToDouble((long)b);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static ulong DToBits(double d) => (ulong)BitConverter.DoubleToInt64Bits(d);
	// exact power of two, n in [-1074, 1023]
	static double Pow2(int n) => n >= -1022 ? BitsToD((ulong)(n + 1023) << 52) : BitsToD(1UL << (n + 1074));

	static double[] Conv(ulong[] b) { var d = new double[b.Length]; for (int i = 0; i < b.Length; i++) d[i] = BitsToD(b[i]); return d; }

	// ---------------- double-double primitives ----------------
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static void TwoSum(double a, double b, out double s, out double e)
	{ s = a + b; double bb = s - a; e = (a - (s - bb)) + (b - bb); }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static void FastTwoSum(double a, double b, out double s, out double e)
	{ s = a + b; e = b - (s - a); }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static void Mul(double ah, double al, double bh, double bl, out double rh, out double rl)
	{
		double p = ah * bh;
		double e = Math.FusedMultiplyAdd(ah, bh, -p);
		e = Math.FusedMultiplyAdd(ah, bl, Math.FusedMultiplyAdd(al, bh, e));
		rh = p + e; rl = e - (rh - p);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static void Add(double ah, double al, double bh, double bl, out double rh, out double rl)
	{
		TwoSum(ah, bh, out double s, out double e);
		e += al + bl;
		rh = s + e; rl = e - (rh - s);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static void Div(double ah, double al, double bh, double bl, out double qh, out double ql)
	{
		double q1 = ah / bh;
		double p = q1 * bh, pe = Math.FusedMultiplyAdd(q1, bh, -p);
		double r = (((ah - p) - pe) + al) - q1 * bl;
		double q2 = r / bh;
		qh = q1 + q2; ql = q2 - (qh - q1);
	}
	// Rounding test: the true value lies within h+l +- |h|*eps.
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool TryRound(double h, double l, double eps, out double r)
	{
		double e = Math.Abs(h) * eps;
		double a = h + (l - e), b = h + (l + e);
		r = a;
		return a == b;
	}

	// ======================= sin / cos / tan =======================
	public static double Sin(double x)
	{
		ulong ax = DToBits(x) & 0x7FFFFFFFFFFFFFFFUL;
		// |x| < 2^-26: llvm-libc returns multiply_add(x, -2^-54, x) (two roundings without FMA; differs
		// from the correctly rounded value for some tiny x where x * 2^-54 is subnormal)
		if (ax < 0x3E50000000000000UL) return x == 0.0 ? x : x * -5.551115123125783e-17 + x;
		if (ax >= 0x7FF0000000000000UL) return x - x;       // inf, NaN -> NaN
		if (!ForceSlow && LTrigFast(x, 0, out double r)) return r;
		if (!ForceSlow && TrigDD(x, 0, out double h, out double l) && TryRound(h, l, EpsSC, out r)) return r;
		return SlowTrig(x, 0);
	}

	public static double Cos(double x)
	{
		ulong ax = DToBits(x) & 0x7FFFFFFFFFFFFFFFUL;
		if (ax < 0x3E40000000000000UL) return 1.0;          // |x| < 2^-27
		if (ax >= 0x7FF0000000000000UL) return x - x;
		if (!ForceSlow && LTrigFast(x, 1, out double r)) return r;
		if (!ForceSlow && TrigDD(x, 1, out double h, out double l) && TryRound(h, l, EpsSC, out r)) return r;
		return SlowTrig(x, 1);
	}

	public static double Tan(double x)
	{
		ulong ax = DToBits(x) & 0x7FFFFFFFFFFFFFFFUL;
		// |x| < 2^-27: llvm-libc returns multiply_add(x, 2^-54, x)
		if (ax < 0x3E40000000000000UL) return x == 0.0 ? x : x * 5.551115123125783e-17 + x;
		if (ax >= 0x7FF0000000000000UL) return x - x;
		if (!ForceSlow && LTrigFast(x, 2, out double r)) return r;
		if (!ForceSlow && TrigDD(x, 2, out double h, out double l) && TryRound(h, l, EpsSC, out r)) return r;
		return SlowTrig(x, 2);
	}

	// sin (which=0), cos (1) or tan (2) of finite x, |x| >= 2^-27, as double-double
	internal static bool TrigDD(double x, int which, out double h, out double l)
	{
		ulong ax = DToBits(x) & 0x7FFFFFFFFFFFFFFFUL;
		if (!Reduce(ax, out int n, out double dh, out double dl)) { h = l = 0; return false; }
		if (which == 0)
		{
			SinCosKernel(n, dh, dl, out h, out l, out _, out _, 1);
			if (x < 0) { h = -h; l = -l; }
		}
		else if (which == 1) SinCosKernel(n, dh, dl, out _, out _, out h, out l, 2);
		else
		{
			SinCosKernel(n, dh, dl, out double sh, out double sl, out double ch, out double cl, 3);
			Div(sh, sl, ch, cl, out h, out l);
			if (x < 0) { h = -h; l = -l; }
		}
		return true;
	}

	// |x| = (n + f) * pi/512 + 2*pi*k, |f| <= 1/2 (about); returns n mod 1024 and d = f*pi/512 as double-double.
	static bool Reduce(ulong ax, out int n, out double dh, out double dl)
	{
		if (ax < 0x3FE921FB54442D18UL)   // |x| < pi/4: Cody-Waite with pi/512 in three parts
		{
			double x = BitsToD(ax);
			double k = Math.Floor(x * (512 / Math.PI) + 0.5);
			n = (int)k;
			double p = k * Pi512H, pe = Math.FusedMultiplyAdd(k, Pi512H, -p);
			double t = x - p;                                   // exact (Sterbenz), or x when k = 0
			TwoSum(t, -pe, out double u, out double v);
			double c2 = k * Pi512L, c2e = Math.FusedMultiplyAdd(k, Pi512L, -c2);
			v = v - c2 - (c2e + k * Pi512LL);
			FastTwoSum(u, v, out dh, out dl);
			return true;
		}
		// Payne-Hanek with integer arithmetic: x = m * 2^e, T = x * 512/pi = m * W * 2^-s (mod 1024)
		int be = (int)(ax >> 52);
		ulong m = (ax & 0xFFFFFFFFFFFFFUL) | (1UL << 52);
		int e = be - 1075;
		int i0 = e - 1; if (i0 < 1) i0 = 1;                       // first bit of 2/pi that matters
		int s = i0 + 247 - e;                                     // 246..301
		int wi = (i0 - 1) >> 6, sh = (i0 - 1) & 63;
		ulong w0, w1, w2, w3;
		var tp = TwoOverPi;
		if (sh == 0) { w0 = tp[wi]; w1 = tp[wi + 1]; w2 = tp[wi + 2]; w3 = tp[wi + 3]; }
		else
		{
			int rs = 64 - sh;
			w0 = (tp[wi] << sh) | (tp[wi + 1] >> rs);
			w1 = (tp[wi + 1] << sh) | (tp[wi + 2] >> rs);
			w2 = (tp[wi + 2] << sh) | (tp[wi + 3] >> rs);
			w3 = (tp[wi + 3] << sh) | (tp[wi + 4] >> rs);
		}
		ulong p0, p1, p2, p3, p4, lo, hi, c;
		hi = Math.BigMul(m, w3, out p0);
		ulong h2 = Math.BigMul(m, w2, out lo); p1 = lo + hi; c = p1 < lo ? 1UL : 0UL; hi = h2 + c;
		ulong h1 = Math.BigMul(m, w1, out lo); p2 = lo + hi; c = p2 < lo ? 1UL : 0UL; hi = h1 + c;
		ulong h0 = Math.BigMul(m, w0, out lo); p3 = lo + hi; c = p3 < lo ? 1UL : 0UL; p4 = h0 + c;
		// Q = P >> (s - 192): 192 fraction bits in q2:q1:q0, integer bits in q3
		int r = s - 192;                                          // 54..109
		ulong a0, a1, a2, a3, a4;
		if (r >= 64) { r -= 64; a0 = p1; a1 = p2; a2 = p3; a3 = p4; a4 = 0; }
		else { a0 = p0; a1 = p1; a2 = p2; a3 = p3; a4 = p4; }
		ulong q0, q1, q2, q3;
		if (r == 0) { q0 = a0; q1 = a1; q2 = a2; q3 = a3; }
		else
		{
			int rl = 64 - r;
			q0 = (a0 >> r) | (a1 << rl); q1 = (a1 >> r) | (a2 << rl);
			q2 = (a2 >> r) | (a3 << rl); q3 = (a3 >> r) | (a4 << rl);
		}
		bool neg = (q2 >> 63) != 0;
		if (neg)
		{   // round up: f = Q - 2^192 < 0, take magnitude
			n = (int)((q3 + 1) & 1023);
			q0 = ~q0; q1 = ~q1; q2 = ~q2;
			q0 += 1; if (q0 == 0) { q1 += 1; if (q1 == 0) q2 += 1; }
		}
		else n = (int)(q3 & 1023);
		ulong u1, u2; int lz;
		if (q2 != 0)
		{
			lz = BitOperations.LeadingZeroCount(q2);
			if (lz == 0) { u1 = q2; u2 = q1; }
			else { u1 = (q2 << lz) | (q1 >> (64 - lz)); u2 = (q1 << lz) | (q0 >> (64 - lz)); }
		}
		else if (q1 != 0)
		{
			int l = BitOperations.LeadingZeroCount(q1);
			lz = 64 + l;
			if (l == 0) { u1 = q1; u2 = q0; }
			else { u1 = (q1 << l) | (q0 >> (64 - l)); u2 = q0 << l; }
		}
		else { dh = dl = 0; return false; }                       // cannot happen for doubles
		// f = u1*2^(-64-lz) + u2*2^(-128-lz)
		double fh = (double)(long)(u1 >> 11) * Pow2(-53 - lz);
		double fl = (double)(long)(((u1 & 0x7FF) << 52) | (u2 >> 12)) * Pow2(-116 - lz);
		if (neg) { fh = -fh; fl = -fl; }
		Mul(fh, fl, Pi512H, Pi512L, out dh, out dl);
		return true;
	}

	// which: 1 = sin(|x|) wanted, 2 = cos(|x|), 3 = both
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static void SinCosKernel(int n, double dh, double dl, out double sinh, out double sinl, out double cosh, out double cosl, int which)
	{
		int q = (n >> 8) & 3, j = n & 255;
		// z = d^2
		double zh = dh * dh, zl = Math.FusedMultiplyAdd(dh, dh, -zh) + 2 * dh * dl;
		double th, tl, wh, wl;
		// sin d = d + d*z*(S3 + z*(S5 + z*(S7 + z*(S9 + z*S11))))
		double qs = S7 + zh * (S9 + zh * S11);
		double ph = zh * qs, pe = Math.FusedMultiplyAdd(zh, qs, -ph);
		TwoSum(S5H, ph, out th, out tl); tl += S5L + pe;
		Mul(zh, zl, th, tl, out wh, out wl);
		Add(S3H, S3L, wh, wl, out th, out tl);
		Mul(zh, zl, th, tl, out wh, out wl);
		Mul(dh, dl, wh, wl, out wh, out wl);
		Add(dh, dl, wh, wl, out double sdh, out double sdl);
		// cos d - 1 = z*(-1/2 + z*(C4 + z*(C6 + z*(C8 + z*(C10 + z*C12)))))
		double qc = C6 + zh * (C8 + zh * (C10 + zh * C12));
		ph = zh * qc; pe = Math.FusedMultiplyAdd(zh, qc, -ph);
		TwoSum(C4H, ph, out th, out tl); tl += C4L + pe;
		Mul(zh, zl, th, tl, out wh, out wl);
		TwoSum(-0.5, wh, out th, out tl); tl += wl;
		Mul(zh, zl, th, tl, out double cmh, out double cml);
		// table: a = j*pi/512, S = sin a, C = cos a
		var st = SinTab;
		double Sh = st[2 * j], Sl = st[2 * j + 1], Ch = st[2 * (256 - j)], Cl = st[2 * (256 - j) + 1];
		double sah = 0, sal = 0, cah = 0, cal = 0;
		bool needSa = which == 3 || (which == 1) == ((q & 1) == 0);
		bool needCa = which == 3 || !needSa;
		if (needSa)
		{   // sin(a+d) = S + (S*(cos d - 1) + C*sin d)
			Mul(Sh, Sl, cmh, cml, out double ah, out double al);
			Mul(Ch, Cl, sdh, sdl, out double bh, out double bl);
			Add(ah, al, bh, bl, out ah, out al);
			Add(Sh, Sl, ah, al, out sah, out sal);
		}
		if (needCa)
		{   // cos(a+d) = C + (C*(cos d - 1) - S*sin d)
			Mul(Ch, Cl, cmh, cml, out double ah, out double al);
			Mul(Sh, Sl, sdh, sdl, out double bh, out double bl);
			Add(ah, al, -bh, -bl, out ah, out al);
			Add(Ch, Cl, ah, al, out cah, out cal);
		}
		// sin(|x|) = [sa, ca, -sa, -ca][q], cos(|x|) = [ca, -sa, -ca, sa][q]
		switch (q)
		{
			case 0: sinh = sah; sinl = sal; cosh = cah; cosl = cal; break;
			case 1: sinh = cah; sinl = cal; cosh = -sah; cosl = -sal; break;
			case 2: sinh = -sah; sinl = -sal; cosh = -cah; cosl = -cal; break;
			default: sinh = -cah; sinl = -cal; cosh = sah; cosl = sal; break;
		}
	}

	// ======================= atan / atan2 =======================
	// llvm-libc's atan and atan2 (src/__support/math/atan.h, atan2.h) are not correctly rounded: they
	// return a double-double result without a rounding test. Ported operation by operation, using the
	// code path for x86-64 without FMA (multiply_add(a, b, c) = a * b + c, Dekker products).

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static void VSplit(double a, out double hi, out double lo)
	{   // Veltkamp split, constant 2^27 + 1
		double t1 = 134217729.0 * a;
		double t2 = a - t1;
		hi = t1 + t2;
		lo = a - hi;
	}
	// fputil::exact_mult without FMA (Dekker)
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static void LExactMult(double a, double b, out double rhi, out double rlo)
	{
		VSplit(a, out double ah, out double al);
		VSplit(b, out double bh, out double bl);
		rhi = a * b;
		double t1 = ah * bh - rhi;
		double t2 = ah * bl + t1;
		double t3 = al * bh + t2;
		rlo = al * bl + t3;
	}
	// fputil::exact_add (Fast2Sum)
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static void LExactAdd(double a, double b, out double hi, out double lo)
	{
		hi = a + b;
		double t = hi - a;
		lo = b - t;
	}
	// fputil::add(DoubleDouble, DoubleDouble)
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static void LAdd(double ahi, double alo, double bhi, double blo, out double hi, out double lo)
	{
		LExactAdd(ahi, bhi, out double rh, out double rl);
		double l = alo + blo;
		LExactAdd(rh, rl + l, out hi, out lo);
	}
	// fputil::div without FMA
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static void LDiv(double ahi, double alo, double bhi, double blo, out double rhi, out double rlo)
	{
		double q = 1.0 / bhi;
		rhi = ahi * q;
		LExactMult(bhi, -rhi, out double p1h, out double p1l);
		LExactMult(blo, -rhi, out double p2h, out double p2l);
		double e_hi = (ahi + p1h) + p1l;
		double e_lo = (alo + p2h) + p2l;
		rlo = q * (e_hi + e_lo);
	}
	// atan_internal::atan_eval(DoubleDouble)
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static void LAtanEval(double xhi, double xlo, out double phi, out double plo)
	{
		phi = xhi;
		double x_hi_sq = xhi * xhi;
		double c0 = x_hi_sq * 0.2 + -0.3333333333333333;                  // 0x1.999999999999ap-3, -0x1.5555555555555p-2
		double c1 = x_hi_sq * 0.1111111111111111 + -0.14285714285714285;  // 0x1.c71c71c71c71cp-4, -0x1.2492492492492p-3
		double x_hi_3 = x_hi_sq * xhi;
		double x_hi_4 = x_hi_sq * x_hi_sq;
		double d0 = x_hi_4 * c1 + c0;
		double d1 = (x_hi_4 - x_hi_sq) * xlo + xlo;
		plo = x_hi_3 * d0 + d1;
	}
	const double PIO2_LO = 6.123233995736766e-17, PIO4_LO = 3.061616997868383e-17, PI_LO = 1.2246467991473532e-16,
		TPIO4_HI = 2.356194490192345, TPIO4_LO = 9.184850993605148e-17;

	public static double Atan(double x)
	{
		ulong xu = DToBits(x);
		bool x_sign = (xu >> 63) != 0;
		ulong x_abs = xu & 0x7FFFFFFFFFFFFFFFUL;
		double sgn = x_sign ? -1.0 : 1.0;
		int x_exp = (int)(x_abs >> 52) - 1023;
		if (x_exp < 0)
		{
			if (x_exp < -26)
			{
				if (x == 0.0) return x;
				return -5.551115123125783e-17 * x + x;   // multiply_add(-0x1.0p-54, x, x)
			}
			double x_d = BitsToD(x_abs);
			double k = Math.Round(64.0 * x_d);
			int idx = (int)k;
			k *= 0.015625;
			double num_hi = x_d - k, num_lo = 0.0;
			double den_hi = x_d * k + 1.0;
			LExactMult(x_d, k, out double prh, out double prl);
			double den_lo = ((1.0 - den_hi) + prh) + prl;
			LDiv(num_hi, num_lo, den_hi, den_lo, out double xrh, out double xrl);
			LAtanEval(xrh, xrl, out double ph, out double pl);
			LExactAdd(AtanI[2 * idx], ph, out double c0h, out double c0l);
			double c1 = c0l + (AtanI[2 * idx + 1] + pl);
			return sgn * (c0h + c1);
		}
		if (x_exp >= 53)
		{
			if (x != x) return x;
			return sgn * PiO2H + sgn * PIO2_LO;   // multiply_add(IS_NEG, PI_OVER_2.hi, IS_NEG * PI_OVER_2.lo)
		}
		{
			double x_d = BitsToD(x_abs);
			double y = 1.0 / x_d;
			double k = Math.Round(64.0 * y);
			int idx = (int)k;
			k *= 0.015625;
			LExactAdd(x_d, k, out double den_hi, out double den_lo);
			double num_hi = -x_d * k + 1.0;
			LExactMult(x_d, k, out double prh, out double prl);
			double num_lo = ((1.0 - num_hi) - prh) - prl;
			LDiv(num_hi, num_lo, den_hi, den_lo, out double xrh, out double xrl);
			LAtanEval(xrh, xrl, out double ph, out double pl);
			LExactAdd(-PiO2H, AtanI[2 * idx], out double c0h, out double c0l);
			LExactAdd(c0h, ph, out double c1h, out double c1l);
			double c2 = c1l + (c0l + pl) + (AtanI[2 * idx + 1] + -PIO2_LO);
			return -sgn * (c1h + c2);
		}
	}

	public static double Atan2(double y, double x)
	{
		ulong xu = DToBits(x), yu = DToBits(y);
		bool x_sign = (xu >> 63) != 0, y_sign = (yu >> 63) != 0;
		ulong x_abs = xu & 0x7FFFFFFFFFFFFFFFUL, y_abs = yu & 0x7FFFFFFFFFFFFFFFUL;
		bool recip = x_abs < y_abs;
		ulong min_abs = recip ? x_abs : y_abs, max_abs = !recip ? x_abs : y_abs;
		uint min_exp = (uint)(min_abs >> 52), max_exp = (uint)(max_abs >> 52);
		double num = BitsToD(min_abs), den = BitsToD(max_abs);
		if (max_exp > 0x7ffU - 128U || min_exp < 128U)
		{
			if (x != x || y != y) return double.NaN;
			int x_except = x == 0.0 ? 0 : (x_abs == 0x7FF0000000000000UL ? 2 : 1);
			int y_except = y == 0.0 ? 0 : (y_abs == 0x7FF0000000000000UL ? 2 : 1);
			if (x_except != 1 || y_except != 1)
			{
				// EXCEPTS[y_except][x_except][x_sign]
				double rh, rl;
				if (y_except == 0) { if (x_sign) { rh = PiH; rl = PI_LO; } else { rh = 0.0; rl = 0.0; } }
				else if (y_except == 1)
				{
					if (x_except == 0) { rh = PiO2H; rl = PIO2_LO; }
					else if (x_sign) { rh = PiH; rl = PI_LO; }
					else { rh = 0.0; rl = 0.0; }
				}
				else
				{
					if (x_except != 2) { rh = PiO2H; rl = PIO2_LO; }
					else if (x_sign) { rh = TPIO4_HI; rl = TPIO4_LO; }
					else { rh = PiO4D; rl = PIO4_LO; }
				}
				double s = y_sign ? -1.0 : 1.0;
				return s * rh + s * rl;
			}
			bool scale_up = min_exp < 128U, scale_down = max_exp > 0x7ffU - 128U;
			if (scale_up)
			{
				num *= 18446744073709551616.0;   // 2^64
				if (!scale_down) den *= 18446744073709551616.0;
			}
			else if (scale_down)
			{
				den *= 5.421010862427522e-20;    // 2^-64
				if (!scale_up) num *= 5.421010862427522e-20;
			}
			min_abs = DToBits(num); max_abs = DToBits(den);
			min_exp = (uint)(min_abs >> 52); max_exp = (uint)(max_abs >> 52);
		}
		double final_sign = ((x_sign != y_sign) != recip) ? -1.0 : 1.0;
		// CONST_ADJ[x_sign][y_sign][recip]
		double ch, cl;
		if (!x_sign)
		{
			if (recip) { ch = -PiO2H; cl = -PIO2_LO; }
			else if (!y_sign) { ch = 0.0; cl = 0.0; }
			else { ch = -0.0; cl = -0.0; }
		}
		else if (recip) { ch = PiO2H; cl = PIO2_LO; }
		else { ch = -PiH; cl = -PI_LO; }
		uint exp_diff = max_exp - min_exp;
		if (exp_diff > 54)
			return final_sign * ch + final_sign * (cl + num / den);
		double k = Math.Round(64.0 * num / den);
		int idx = (int)k;
		k *= 0.015625;
		LExactMult(num, k, out double nkh, out double nkl);
		LExactMult(den, k, out double dkh, out double dkl);
		LExactAdd(num - dkh, -dkl, out double ndh, out double ndl);
		LExactAdd(den, nkh, out double ddh, out double ddl);
		ddl += nkl;
		LDiv(ndh, ndl, ddh, ddl, out double qh, out double ql);
		LAtanEval(qh, ql, out double ph, out double pl);
		LAdd(AtanI[2 * idx], AtanI[2 * idx + 1], ph, pl, out double th, out double tl);
		LAdd(ch, cl, th, tl, out double rh2, out double rl2);
		rh2 *= final_sign; rl2 *= final_sign;
		return rh2 + rl2;
	}

	// ======================= exp =======================
	public static double Exp(double x)
	{
		ulong xu = DToBits(x);
		// llvm-libc's exceptional range: x <= -0x1.74910d52d3052p9, x >= 0x1.62e42fefa39fp9, |x| < 2^-53, inf, NaN
		if (xu >= 0xC0874910D52D3052UL || (xu < 0xBCA0000000000000UL && xu >= 0x40862E42FEFA39F0UL) || xu < 0x3CA0000000000000UL)
		{
			if ((xu & 0x7FFFFFFFFFFFFFFFUL) <= 0x3CA0000000000000UL) return 1.0 + x;
			if (double.IsNaN(x)) return x;
			return xu >= 0xC0874910D52D3052UL ? 0.0 : double.PositiveInfinity;
		}
		if (!ForceSlow && LExpFast(x, out double r)) return r;
		if (!ForceSlow && ExpDD(x, out double h, out double l, out int k) && TryRound(h, l, EpsExp, out r))
			return k <= 1023 ? r * Pow2(k) : r * Pow2(1023) * 2.0;
		return SlowExp(x);
	}

	// exp(x) = (h + l) * 2^k for -745.2 <= x <= 709.8; false when the result may be subnormal
	internal static bool ExpDD(double x, out double h, out double l, out int k)
	{
		double kd = Math.Round(x * InvLn2x256);
		int n = (int)kd;
		k = n >> 8;
		int j = n & 255;
		if (k < -1021) { h = l = 0; return false; }
		// r = x - n*ln2/256
		double t = x - kd * Ln2o256H;                      // exact
		double p2 = kd * Ln2o256L, e2 = Math.FusedMultiplyAdd(kd, Ln2o256L, -p2);
		TwoSum(t, -p2, out double rh, out double rl);
		rl -= e2 + kd * Ln2o256L3;
		FastTwoSum(rh, rl, out rh, out rl);
		// exp(r) = 1 + r(1 + r(1/2 + r(1/6 + r(1/24 + r*q))))
		double q = E5 + rh * (E6 + rh * (E7 + rh * (E8 + rh * (E9 + rh * E10))));
		double ph = rh * q, pe = Math.FusedMultiplyAdd(rh, q, -ph) + rl * q;
		Add(E4H, E4L, ph, pe, out double th, out double tl);
		Mul(rh, rl, th, tl, out th, out tl);
		Add(E3H, E3L, th, tl, out th, out tl);
		Mul(rh, rl, th, tl, out th, out tl);
		Add(0.5, 0, th, tl, out th, out tl);
		Mul(rh, rl, th, tl, out th, out tl);
		Add(1.0, 0, th, tl, out th, out tl);
		Mul(rh, rl, th, tl, out th, out tl);
		Add(1.0, 0, th, tl, out th, out tl);
		Mul(ExpTab[2 * j], ExpTab[2 * j + 1], th, tl, out h, out l);
		return true;
	}

	// ======================= llvm-libc fast passes for sin / cos / tan / exp =======================
	// V8 15.2 calls llvm-libc (x86-64 build: no FMA, so multiply_add(a, b, c) = a * b + c and products
	// are Dekker splits). Their first passes end in a Ziv rounding test whose error bound is not always
	// safe, so a few results are not correctly rounded (e.g. cos(-0.052637790502375076)). These are
	// literal ports of those passes; when the rounding test fails, llvm-libc runs a 128-bit accurate
	// pass, which is replaced here by the correctly rounded paths above.
	const double C27 = 134217729.0, C28 = 268435457.0;        // 2^27 + 1, 2^28 + 1

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static void LSplit(double a, double c, out double hi, out double lo)
	{
		double t1 = c * a;
		double t2 = a - t1;
		hi = t1 + t2;
		lo = a - hi;
	}
	// exact_mult<double, SPLIT_B>(as, a, b), cb = 2^SPLIT_B + 1
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static void LExactMultS(double ash, double asl, double a, double b, double cb, out double rhi, out double rlo)
	{
		LSplit(b, cb, out double bh, out double bl);
		rhi = a * b;
		double t1 = ash * bh - rhi;
		double t2 = ash * bl + t1;
		double t3 = asl * bh + t2;
		rlo = asl * bl + t3;
	}
	// quick_mult<SPLIT_B>(DoubleDouble a, DoubleDouble b)
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static void LQuickMult(double ahi, double alo, double bhi, double blo, double cb, out double rhi, out double rlo)
	{
		LSplit(ahi, C27, out double ash, out double asl);
		LExactMultS(ash, asl, ahi, bhi, cb, out rhi, out double r0);
		double t1 = ahi * blo + r0;
		rlo = alo * bhi + t1;
	}
	// exact_add<false> (TwoSum)
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static void LTwoSum(double a, double b, out double hi, out double lo)
	{
		hi = a + b;
		double t1 = hi - a;
		double t2 = hi - t1;
		double t3 = b - t1;
		double t4 = a - t2;
		lo = t3 + t4;
	}

	// range_reduction_small: x = k * pi/128 + u, |x| < 2^16
	static uint LRedSmall(double x, out double uhi, out double ulo)
	{
		double prod_hi = x * 40.74366543152521;
		double kd = Math.Round(prod_hi);
		double y_hi = kd * -0.024543692605220713 + x;
		uhi = kd * -9.495469541099947e-13 + y_hi;
		double u0 = y_hi - uhi;
		double u1 = kd * -9.495469541099947e-13 + u0;
		ulo = kd * -3.159791013743673e-23 + u1;
		return unchecked((uint)(long)kd);
	}

	// LargeRangeReduction::fast (nofma), |x| >= 2^16
	static uint LRedLarge(double x, out double uhi, out double ulo)
	{
		ulong xb = DToBits(x);
		int x_e_m62 = (int)((xb >> 52) & 0x7FF) - (1023 + 62);
		int idx = (x_e_m62 >> 4) + 3;
		double x_reduced = BitsToD((xb & 0x800FFFFFFFFFFFFFUL) | ((ulong)((x_e_m62 & 15) + 1023 + 62) << 52));
		LSplit(x_reduced, C27, out double xsh, out double xsl);
		int o = 4 * idx;
		LExactMultS(xsh, xsl, x_reduced, L128Pi[o], C28, out double phh, out double phl);
		LExactMultS(xsh, xsl, x_reduced, L128Pi[o + 1], C28, out double pmh, out double pml);
		LExactMultS(xsh, xsl, x_reduced, L128Pi[o + 2], C28, out double plh, out double pll);
		double sum_hi = phl + pmh;
		double kd = Math.Round(sum_hi);
		double y_hi = (phl - kd) + pmh;
		LExactAdd(pml, plh, out double ymh, out double yml);
		double y_l = x_reduced * L128Pi[o + 3] + pll;
		LExactAdd(y_hi, ymh, out double yh, out double yl);
		yl += yml + y_l;
		LQuickMult(yh, yl, 0.02454369260617026, 9.567553118338697e-19, C28, out uhi, out ulo);
		_ = phh;
		return unchecked((uint)(long)kd);
	}

	// sin (which=0), cos (1), tan (2) first pass; finite x, |x| >= 2^-27 (2^-26 for sin).
	// Returns false when llvm-libc would go on to its accurate pass.
	internal static bool LTrigFast(double x, int which, out double r)
	{
		ulong xb = DToBits(x);
		int x_e = (int)((xb >> 52) & 0x7FF);
		double uhi, ulo;
		uint k;
		if (x_e < 1023 + 16)
		{
			if (which == 0 && x_e < 1023 - 4)
			{
				double x_sq = x * x;
				double c0 = x_sq * 0.008333333333320677 + -0.16666666666666666;
				double c1 = x_sq * 2.7549179097726733e-06 + -0.00019841269299313875;
				double x4 = x_sq * x_sq;
				double x3 = x * x_sq;
				double r_lo = (x4 * c1 + c0) * x3;
				double err = x_sq * 1.1102230246251565e-16 + 3.3881317890172014e-21;
				double r_lo_u = x * err + r_lo;
				double r_lo_l = -x * err + r_lo;
				r = x + r_lo_u;
				return r == x + r_lo_l;
			}
			if (which == 1 && x_e < 1023 - 4)
			{
				double x_sq = x * x;
				double c0 = x_sq * 0.041666666666661425 + -0.5;
				double c1 = x_sq * 2.479925520961421e-05 + -0.0013888888826265567;
				double x4 = x_sq * x_sq;
				double r_lo = (x4 * c1 + c0) * x_sq;
				double err = x_sq * 1.1102230246251565e-16 + 1.6940658945086007e-21;
				r = 1.0 + (r_lo + err);
				return r == 1.0 + (r_lo - err);
			}
			if (which == 2 && x_e < 1023 - 7) { k = 0; uhi = x; ulo = 0.0; }
			else k = LRedSmall(x, out uhi, out ulo);
		}
		else k = LRedLarge(x, out uhi, out ulo);

		double u_hi_sq = uhi * uhi;
		if (which == 2)
		{
			// tan_eval
			double p1 = u_hi_sq * 0.021869488536155203 + 0.05396825396825397;
			double p2 = u_hi_sq * 0.13333333333333333 + 0.3333333333333333;
			double q1 = u_hi_sq * 0.6666666666666666 + 1.0;
			double u_hi_3t = u_hi_sq * uhi;
			double u_hi_4t = u_hi_sq * u_hi_sq;
			double p3 = u_hi_4t * p1 + p2;
			double q2t = u_hi_sq * q1 + 1.0;
			double tan_lo = u_hi_3t * p3 + ulo * q2t;
			LExactAdd(uhi, tan_lo, out double tyh, out double tyl);
			double terr = Math.Abs(u_hi_3t) * 4.440892098500626e-16 + 1.9721522630525295e-31;
			int im = (int)((k + 128) & 255) * 2, ic = (int)((k + 64) & 255) * 2;
			double mskl = LSinK[im], mskh = LSinK[im + 1], ckl = LSinK[ic], ckh = LSinK[ic + 1];
			LQuickMult(tyh, tyl, ckh, ckl, C27, out double ckth, out double cktl);
			LQuickMult(tyh, tyl, mskh, mskl, C27, out double mkth, out double mktl);
			LTwoSum(ckth, -mskh, out double nh, out double nl);
			LTwoSum(mkth, ckh, out double dh, out double dl);
			nl += cktl - mskl;
			dl += mktl + ckl;
			LDiv(nh, nl, dh, dl, out double txh, out double txl);
			double den_inv = BitsToD((1024UL << 53) - (DToBits(dh) & 0x7FF0000000000000UL));
			double tan_err = terr * (den_inv * Math.Abs(txh) + 1.0);
			double err_higher = txl + tan_err;
			double err_lower = txl - tan_err;
			r = txh + err_higher;
			return r == txh + err_lower;
		}

		// sincos_eval
		double sp1 = u_hi_sq * -0.0001984126984126984 + 0.008333333333333333;
		double sq1 = u_hi_sq * 0.041666666666666664 + -0.5;
		double u_hi_3 = u_hi_sq * uhi;
		double sp2 = u_hi_sq * sp1 + -0.16666666666666666;
		double sq2 = u_hi_sq * sq1 + 1.0;
		double sin_lo = u_hi_3 * sp2 + ulo * sq2;
		double u_hi_neg_half = -0.5 * uhi;
		LExactMult(uhi, u_hi_neg_half, out double sqh, out double sql);
		LExactAdd(1.0, sqh, out double vh, out double vl);
		vl += sql;
		double r1 = u_hi_sq * 2.48015873015873e-05 + -0.001388888888888889;
		double s1 = u_hi_sq * 0.16666666666666666 + -1.0;
		double u_hi_4 = u_hi_sq * u_hi_sq;
		double u_hi_u_lo = uhi * ulo;
		double r2 = u_hi_sq * r1 + 0.041666666666666664;
		double s2 = u_hi_u_lo * s1 + vl;
		double cos_lo = u_hi_4 * r2 + s2;
		LExactAdd(uhi, sin_lo, out double syh, out double syl);
		LExactAdd(vh, cos_lo, out double cyh, out double cyl);
		double serr = Math.Abs(u_hi_3) * 4.440892098500626e-16 + 2.465190328815662e-32;

		int ia, ib;
		if (which == 0) { ia = (int)(k & 255) * 2; ib = (int)((k + 64) & 255) * 2; }          // sin_k, cos_k
		else { ia = (int)((k + 64) & 255) * 2; ib = (int)((k + 128) & 255) * 2; }             // cos_k, -sin_k
		// sin: sin_k*cos_y + cos_k*sin_y ; cos: cos_k*cos_y + (-sin_k)*sin_y
		LQuickMult(cyh, cyl, LSinK[ia + 1], LSinK[ia], C27, out double ah, out double al);
		LQuickMult(syh, syl, LSinK[ib + 1], LSinK[ib], C27, out double bh, out double bl);
		LExactAdd(ah, bh, out double rrh, out double rrl);
		rrl += al + bl;
		r = rrh + (rrl + serr);
		return r == rrh + (rrl - serr);
	}

	// exp first two passes (x already inside llvm-libc's non-exceptional range)
	internal static bool LExpFast(double x, out double r)
	{
		double tmp = x * 1.4426950408889634 + 3145728.0001220703;
		int k = unchecked((int)(DToBits(tmp) >> 19));
		double kd = k;
		int idx1 = (k >> 6) & 0x3f, idx2 = k & 0x3f;
		int hi = k >> 12;
		bool denorm = hi <= -1022;
		int i1 = 3 * idx1, i2 = 3 * idx2;
		LQuickMult(LExpMid1[i1 + 2], LExpMid1[i1 + 1], LExpMid2[i2 + 2], LExpMid2[i2 + 1], C27, out double emh, out double eml);
		double lo_h = kd * -0.00016922538588914904 + x;
		double dx = kd * 1.0256140314162804e-14 + lo_h;
		double mid_lo = dx * emh;
		double dx2 = dx * dx;
		double c0 = dx * 0.5 + 1.0;
		double c1 = dx * 0.041666666666666664 + 0.16666666666666666;
		double p = dx2 * c1 + c0;
		double lo = p * mid_lo + eml;
		const double EXP_ERR_D = 1.6263032587282567e-19, EXP_ERR_DD = 1.5777218104420236e-30;
		if (denorm)
		{
			if (LZivDenorm(hi, emh, lo, EXP_ERR_D, out r)) return true;
		}
		else
		{
			double upper = emh + (lo + EXP_ERR_D);
			if (upper == emh + (lo - EXP_ERR_D)) { r = BitsToD(unchecked((ulong)(((long)hi << 52) + (long)DToBits(upper)))); return true; }
		}
		// exp_double_double
		double t1 = kd * -0.00016922538588914904 + x;
		double t2 = kd * 1.0256140311365355e-14;
		double t3 = kd * 2.797449396191097e-24;
		LExactAdd(t1, t2, out double dxh, out double dxl);
		dxl += t3;
		// polyeval with DoubleDouble: p = c0 + dx*(c1 + dx*(... + dx*c6)), each step add(c, quick_mult(dx, p))
		double ph = 0.001388888888888889, pl = -5.300543954373577e-20;
		LQuickMult(dxh, dxl, ph, pl, C27, out double mh, out double ml); LAdd(0.008333333333333333, 1.1564823173178714e-19, mh, ml, out ph, out pl);
		LQuickMult(dxh, dxl, ph, pl, C27, out mh, out ml); LAdd(0.041666666666666664, 2.3129646346357427e-18, mh, ml, out ph, out pl);
		LQuickMult(dxh, dxl, ph, pl, C27, out mh, out ml); LAdd(0.16666666666666666, 9.25185853854297e-18, mh, ml, out ph, out pl);
		LQuickMult(dxh, dxl, ph, pl, C27, out mh, out ml); LAdd(0.5, 0.0, mh, ml, out ph, out pl);
		LQuickMult(dxh, dxl, ph, pl, C27, out mh, out ml); LAdd(1.0, 0.0, mh, ml, out ph, out pl);
		LQuickMult(dxh, dxl, ph, pl, C27, out mh, out ml); LAdd(1.0, 0.0, mh, ml, out ph, out pl);
		LQuickMult(emh, eml, ph, pl, C27, out double rdh, out double rdl);
		if (denorm) return LZivDenorm(hi, rdh, rdl, EXP_ERR_DD, out r);
		double upper_dd = rdh + (rdl + EXP_ERR_DD);
		if (upper_dd == rdh + (rdl - EXP_ERR_DD)) { r = BitsToD(unchecked((ulong)(((long)hi << 52) + (long)DToBits(upper_dd)))); return true; }
		r = 0;
		return false;
	}

	// ziv_test_denorm (exp_utils.h)
	static bool LZivDenorm(int hi, double mid, double lo, double err, out double r)
	{
		long exp_hi = (long)(hi + 1022) << 52;
		double mid_hi = BitsToD(unchecked((ulong)(exp_hi + (long)DToBits(mid))));
		double lo_scaled = lo != 0.0 ? BitsToD(unchecked((ulong)(exp_hi + (long)DToBits(lo)))) : 0.0;
		double extra_factor = 0.0;
		ulong scale_down = 0x3FE0000000000000UL;
		if ((1.0 - mid_hi) > lo_scaled)
		{
			err += 2.220446049250313e-16;
			extra_factor = 1.0;
			scale_down = 0x3FF0000000000000UL;
		}
		double err_scaled = BitsToD(unchecked((ulong)(exp_hi + (long)DToBits(err))));
		double lo_u = lo_scaled + err_scaled;
		double lo_l = lo_scaled - err_scaled;
		double upper = extra_factor + (mid_hi + lo_u);
		double lower = extra_factor + (mid_hi + lo_l);
		r = BitsToD(DToBits(upper) - scale_down);
		return upper == lower;
	}

	// ======================= slow path (BigInteger, Ziv loop) =======================
	static readonly object slowLock = new object();
	static BigInteger piFix, ln2Fix;
	static int piBits, ln2Bits;

	// pi * 2^g, error below 2 units
	static BigInteger PiFixed(int g)
	{
		if (g + 8 > piBits)
		{
			int w = Math.Max(g + 64, 2 * piBits) + 32;
			BigInteger v = 16 * AtanInv(5, w) - 4 * AtanInv(239, w);
			piBits = w - 32; piFix = v >> 32;
		}
		return piFix >> (piBits - g);
	}
	// ln 2 * 2^g = 2*atanh(1/3)
	static BigInteger Ln2Fixed(int g)
	{
		if (g + 8 > ln2Bits)
		{
			int w = Math.Max(g + 64, 2 * ln2Bits) + 32;
			BigInteger t = (BigInteger.One << w) / 3, sum = t;
			for (int k = 1; ; k++) { t /= 9; if (t.IsZero) break; sum += t / (2 * k + 1); }
			ln2Bits = w - 32; ln2Fix = (2 * sum) >> 32;
		}
		return ln2Fix >> (ln2Bits - g);
	}
	static BigInteger AtanInv(int n, int w)
	{
		BigInteger t = (BigInteger.One << w) / n, sum = t, n2 = n * n;
		for (int k = 1; ; k++)
		{
			t /= n2; if (t.IsZero) break;
			BigInteger u = t / (2 * k + 1);
			if ((k & 1) != 0) sum -= u; else sum += u;
		}
		return sum;
	}

	// round v * 2^e (|error| <= err units) to double; false if the rounding is not decided
	static bool RoundBig(BigInteger v, int e, BigInteger err, out double r)
	{
		r = 0;
		int sign = v.Sign;
		BigInteger a = BigInteger.Abs(v);
		if (a <= err) return false;
		double lo = ToDouble(a - err, e), hi = ToDouble(a + err, e);
		if (lo != hi) return false;
		r = sign < 0 ? -lo : lo;
		return true;
	}
	// correctly rounded (to nearest, ties to even) a * 2^e, a > 0
	static double ToDouble(BigInteger a, int e)
	{
		long len = (long)a.GetBitLength();
		long t = len - 1 + e;
		if (t >= 1024) return double.PositiveInfinity;
		long lsb = t >= -1022 ? t - 52 : -1074;
		long sh = lsb - e;
		BigInteger q;
		if (sh <= 0) q = a << (int)(-sh);
		else if (sh > len + 1) return 0.0;
		else
		{
			q = a >> (int)sh;
			BigInteger rem = a - (q << (int)sh), half = BigInteger.One << (int)(sh - 1);
			int c = rem.CompareTo(half);
			if (c > 0 || (c == 0 && !q.IsEven)) q += 1;
		}
		return (double)(long)q * Pow2((int)lsb);
	}
	// |x| = m * 2^e
	static void Decompose(double x, out BigInteger m, out int e)
	{
		ulong b = DToBits(x) & 0x7FFFFFFFFFFFFFFFUL;
		int be = (int)(b >> 52);
		ulong mm = b & 0xFFFFFFFFFFFFFUL;
		if (be == 0) { m = mm; e = -1074; }
		else { m = mm | (1UL << 52); e = be - 1075; }
	}

	// which: 0 sin, 1 cos, 2 tan. x finite, |x| >= 2^-27
	static double SlowTrig(double x, int which)
	{
		lock (slowLock)
		{
			SlowCalls++;
			Decompose(x, out BigInteger m, out int e);
			int ext = Math.Max(0, e + 53);
			double best = 0;
			for (int p = 128; p <= 16384; p *= 2)
			{
				int H = Math.Max(p + 72 + ext, -e + p);
				BigInteger X = m << (e + H);
				BigInteger halfPi = PiFixed(H - 1);                // (pi/2) * 2^H
				BigInteger k = (X + (halfPi >> 1)) / halfPi;
				BigInteger R = X - k * halfPi;
				int q = (int)(k & 3);
				BigInteger one = BigInteger.One << H, R2 = (R * R) >> H;
				BigInteger s = R, c = one, t = R;
				int terms = 0;
				for (int n = 2; ; n += 2) { t = -((t * R2) >> H) / (n * (n + 1)); if (t.IsZero) break; s += t; terms++; }
				t = one;
				for (int n = 1; ; n += 2) { t = -((t * R2) >> H) / (n * (n + 1)); if (t.IsZero) break; c += t; terms++; }
				BigInteger sv = q == 0 ? s : q == 1 ? c : q == 2 ? -s : -c;
				BigInteger cv = q == 0 ? c : q == 1 ? -s : q == 2 ? -c : s;
				if (x < 0) sv = -sv;
				BigInteger err = (BigInteger.One << (ext + 3)) + 4 * terms + 16;
				double r;
				if (which == 0) { if (RoundBig(sv, -H, err, out r)) return r; best = ToDouble(BigInteger.Abs(sv), -H) * sv.Sign; }
				else if (which == 1) { if (RoundBig(cv, -H, err, out r)) return r; best = ToDouble(BigInteger.Abs(cv), -H) * cv.Sign; }
				else
				{
					if (cv.IsZero) continue;
					BigInteger Q = (sv << H) / cv;
					BigInteger errQ = 4 * ((err << H) * (BigInteger.Abs(cv) + BigInteger.Abs(sv)) / (cv * cv)) + 4;
					if (RoundBig(Q, -H, errQ, out r)) return r;
					best = ToDouble(BigInteger.Abs(Q), -H) * Q.Sign;
				}
			}
			return best;
		}
	}

	// exp for -745.2 <= x <= 709.8, |x| >= 2^-54
	static double SlowExp(double x)
	{
		lock (slowLock)
		{
			SlowCalls++;
			Decompose(x, out BigInteger m, out int e);
			if (x < 0) m = -m;
			double best = 0;
			for (int p = 128; p <= 16384; p *= 2)
			{
				int W = Math.Max(p + 80, -e + p);
				BigInteger X = m << (e + W);
				BigInteger ln2 = Ln2Fixed(W);
				BigInteger k = X.Sign >= 0 ? (X + (ln2 >> 1)) / ln2 : (X - (ln2 >> 1)) / ln2;
				BigInteger R = X - k * ln2;
				BigInteger one = BigInteger.One << W, sum = one, t = one;
				int terms = 0;
				for (int n = 1; ; n++) { t = ((t * R) >> W) / n; if (t.IsZero) break; sum += t; terms++; }
				if (RoundBig(sum, (int)k - W, (BigInteger.One << 13) + 4 * terms + 16, out double r)) return r;
				best = ToDouble(sum, (int)k - W);
			}
			return best;
		}
	}

	// ======================= pow =======================
	// V8 15.2 Math.pow (v8::internal::math::pow with --use-std-math-pow, the default): ECMAScript NaN
	// rules, then std::pow, i.e. the platform's libm. The reference (Electron 44 on Linux x64) is glibc's
	// pow (glibc 2.28+, ARM optimized-routines algorithm, "fma" ifunc variant on CPUs with FMA).
	// GlibcPow below is a literal port of that code (checked equal on Linux). The game uses the platform's pow
	// (Math.Pow = the same libm Electron calls on that machine: glibc on Linux, the Windows CRT on Windows),
	// so on the players' Windows PCs it matches their Electron app, not the Linux reference.
	public static double Pow(double x, double y)
	{
		if (double.IsNaN(y)) return double.NaN;
		if (double.IsInfinity(y) && (x == 1 || x == -1)) return double.NaN;
		// V8 (math.cc / ieee754 pow wrapper) special-cases these exponents before calling std::pow
		if (y == 2.0) return x * x;
		if (y == 0.5) return x == double.NegativeInfinity ? double.PositiveInfinity : Math.Sqrt(0.0 + x);
		return Math.Pow(x, y);
	}

	const double P769 = 3.105036184601418e+231, Pm767 = 1.2882297539194267e-231, P1009 = 5.486124068793689e+303, Pm1022 = 2.2250738585072014e-308, P52 = 4503599627370496.0; // 2^769, 2^-767, 2^1009, 2^-1022, 2^52
	internal static int GPowMode = 1;   // 1 = fma ifunc variant (default), 0 = sse2 variant (tests)

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static uint Top12(double x) => (uint)(DToBits(x) >> 52);
	static bool ZeroInfNan(ulong i) => 2 * i - 1 >= 2 * 0x7FF0000000000000UL - 1;
	static int CheckInt(ulong iy)
	{
		int e = (int)(iy >> 52 & 0x7ff);
		if (e < 0x3ff) return 0;
		if (e > 0x3ff + 52) return 2;
		if ((iy & ((1UL << (0x3ff + 52 - e)) - 1)) != 0) return 0;
		if ((iy & (1UL << (0x3ff + 52 - e))) != 0) return 1;
		return 2;
	}
	static double GOflow(uint sign) => (sign != 0 ? -P769 : P769) * P769;
	static double GUflow(uint sign) => (sign != 0 ? -Pm767 : Pm767) * Pm767;

	static double GLogInline(ulong ix, out double tail)
	{
		const ulong OFF = 0x3fe6955500000000UL;
		ulong tmp = ix - OFF;
		int i = (int)((tmp >> (52 - 7)) % 128);
		int k = (int)((long)tmp >> 52);
		ulong iz = ix - (tmp & 0xfffUL << 52);
		double z = BitsToD(iz);
		double kd = k;
		double invc = GPowLog[3 * i], logc = GPowLog[3 * i + 1], logctail = GPowLog[3 * i + 2];
		double r, rhi = 0, rlo = 0;
		bool fma = GPowMode == 1;
		if (fma) r = Math.FusedMultiplyAdd(z, invc, -1.0);
		else
		{
			double zhi = BitsToD((iz + (1UL << 31)) & (ulong.MaxValue << 32));
			double zlo = z - zhi;
			rhi = zhi * invc - 1.0;
			rlo = zlo * invc;
			r = rhi + rlo;
		}
		double t1, t2, lo1, lo2, ar, ar2, ar3, hi, lo3, lo4, lo;
		if (fma)
		{   // gcc -mfma -ffp-contract=fast, as in libm's __pow_fma (checked against its disassembly)
			t1 = Math.FusedMultiplyAdd(kd, GLn2hi, logc);
			t2 = t1 + r;
			lo1 = Math.FusedMultiplyAdd(kd, GLn2lo, logctail);
			lo2 = t1 - t2 + r;
			ar = GA0 * r;
			ar2 = r * ar;
			ar3 = r * ar2;
			hi = t2 + ar2;
			lo3 = Math.FusedMultiplyAdd(ar, r, -ar2);
			lo4 = t2 - hi + ar2;
			double q12 = Math.FusedMultiplyAdd(r, GA2, GA1), q34 = Math.FusedMultiplyAdd(r, GA4, GA3), q56 = Math.FusedMultiplyAdd(r, GA6, GA5);
			double pp = Math.FusedMultiplyAdd(ar2, Math.FusedMultiplyAdd(ar2, q56, q34), q12);
			lo = Math.FusedMultiplyAdd(ar3, pp, lo1 + lo2 + lo3 + lo4);
		}
		else
		{
			t1 = kd * GLn2hi + logc;
			t2 = t1 + r;
			lo1 = kd * GLn2lo + logctail;
			lo2 = t1 - t2 + r;
			ar = GA0 * r;
			ar2 = r * ar;
			ar3 = r * ar2;
			double arhi = GA0 * rhi;
			double arhi2 = rhi * arhi;
			hi = t2 + arhi2;
			lo3 = rlo * (ar + arhi);
			lo4 = t2 - hi + arhi2;
			double p = ar3 * (GA1 + r * GA2 + ar2 * (GA3 + r * GA4 + ar2 * (GA5 + r * GA6)));
			lo = lo1 + lo2 + lo3 + lo4 + p;
		}
		double y = hi + lo;
		tail = hi - y + lo;
		return y;
	}

	static double GSpecialCase(double tmp, ulong sbits, ulong ki)
	{
		double scale, y;
		if ((ki & 0x80000000) == 0)
		{
			sbits -= 1009UL << 52;
			scale = BitsToD(sbits);
			y = P1009 * (GPowMode == 1 ? Math.FusedMultiplyAdd(tmp, scale, scale) : scale + scale * tmp);
			return y;
		}
		sbits += 1022UL << 52;
		scale = BitsToD(sbits);
		double st = scale * tmp;   // shared product: not fused in either variant
		y = scale + st;
		if (Math.Abs(y) < 1.0)
		{
			double hi, lo, one = 1.0;
			if (y < 0.0) one = -1.0;
			lo = scale - y + st;
			hi = one + y;
			lo = one - hi + y + lo;
			y = (hi + lo) - one;
			if (y == 0.0) y = BitsToD(sbits & 0x8000000000000000UL);
		}
		y = Pm1022 * y;
		return y;
	}

	static double GExpInline(double x, double xtail, uint sign_bias)
	{
		uint abstop = Top12(x) & 0x7ff;
		if (abstop - 0x3c9u >= 0x408u - 0x3c9u)
		{
			if (abstop - 0x3c9u >= 0x80000000u)
			{
				double one = 1.0 + x;
				return sign_bias != 0 ? -one : one;
			}
			if (abstop >= 0x409u)
				return (DToBits(x) >> 63) != 0 ? GUflow(sign_bias) : GOflow(sign_bias);
			abstop = 0;
		}
		bool fma = GPowMode == 1;
		double kd, r, tmp;
		ulong ki;
		if (fma)
		{
			kd = Math.FusedMultiplyAdd(x, GInvLn2N, GShift);
			ki = DToBits(kd);
			kd -= GShift;
			r = Math.FusedMultiplyAdd(kd, GNegLn2loN, Math.FusedMultiplyAdd(kd, GNegLn2hiN, x));
			r += xtail;
		}
		else
		{
			double z = GInvLn2N * x;
			kd = z + GShift;
			ki = DToBits(kd);
			kd -= GShift;
			r = x + kd * GNegLn2hiN + kd * GNegLn2loN;
			r += xtail;
		}
		ulong idx = 2 * (ki % 128);
		ulong top = (ki + sign_bias) << (52 - 7);
		double tail = BitsToD(GExpTab[idx]);
		ulong sbits = GExpTab[idx + 1] + top;
		double r2 = r * r;
		if (fma)
			tmp = Math.FusedMultiplyAdd(Math.FusedMultiplyAdd(r, GC5, GC4), r2 * r2,
				Math.FusedMultiplyAdd(Math.FusedMultiplyAdd(r, GC3, GC2), r2, tail + r));
		else
			tmp = tail + r + r2 * (GC2 + r * GC3) + r2 * r2 * (GC4 + r * GC5);
		if (abstop == 0) return GSpecialCase(tmp, sbits, ki);
		double scale = BitsToD(sbits);
		return fma ? Math.FusedMultiplyAdd(tmp, scale, scale) : scale + scale * tmp;
	}

	static double GlibcPow(double x, double y)
	{
		const uint SIGN_BIAS = 0x800 << 7;
		const ulong ONE = 0x3FF0000000000000UL, INF = 0x7FF0000000000000UL;
		uint sign_bias = 0;
		ulong ix = DToBits(x), iy = DToBits(y);
		uint topx = Top12(x), topy = Top12(y);
		if (topx - 0x001u >= 0x7ffu - 0x001u || (topy & 0x7ff) - 0x3beu >= 0x43eu - 0x3beu)
		{
			if (ZeroInfNan(iy))
			{
				if (2 * iy == 0) return 1.0;
				if (ix == ONE) return 1.0;
				if (2 * ix > 2 * INF || 2 * iy > 2 * INF) return x + y;
				if (2 * ix == 2 * ONE) return 1.0;
				if ((2 * ix < 2 * ONE) == ((iy >> 63) == 0)) return 0.0;
				return y * y;
			}
			if (ZeroInfNan(ix))
			{
				double x2 = x * x;
				if ((ix >> 63) != 0 && CheckInt(iy) == 1) { x2 = -x2; sign_bias = 1; }
				if (2 * ix == 0 && (iy >> 63) != 0) return sign_bias != 0 ? double.NegativeInfinity : double.PositiveInfinity;
				return (iy >> 63) != 0 ? 1 / x2 : x2;
			}
			if ((ix >> 63) != 0)
			{
				int yint = CheckInt(iy);
				if (yint == 0) return double.NaN;
				if (yint == 1) sign_bias = SIGN_BIAS;
				ix &= 0x7fffffffffffffffUL;
				topx &= 0x7ff;
			}
			if ((topy & 0x7ff) - 0x3beu >= 0x43eu - 0x3beu)
			{
				if (ix == ONE) return 1.0;
				if ((topy & 0x7ff) < 0x3be) return ix > ONE ? 1.0 + y : 1.0 - y;
				return (ix > ONE) == (topy < 0x800) ? GOflow(0) : GUflow(0);
			}
			if (topx == 0)
			{
				ix = DToBits(x * P52);
				ix &= 0x7fffffffffffffffUL;
				ix -= 52UL << 52;
			}
		}
		double hi = GLogInline(ix, out double lo);
		double ehi, elo;
		if (GPowMode == 1)
		{
			ehi = y * hi;
			elo = Math.FusedMultiplyAdd(y, lo, Math.FusedMultiplyAdd(hi, y, -ehi));
		}
		else
		{
			double yhi = BitsToD(iy & (ulong.MaxValue << 27));
			double ylo = y - yhi;
			double lhi = BitsToD(DToBits(hi) & (ulong.MaxValue << 27));
			double llo = hi - lhi + lo;
			ehi = yhi * lhi;
			elo = ylo * lhi + y * llo;
		}
		return GExpInline(ehi, elo, sign_bias);
	}

	// ---------------- constants and tables ----------------
	// (static initializers run in text order: the double tables below must follow their bit arrays)
	const double Pi512H = 0.006135923151542565, Pi512L = 2.391888279584674e-19; // 0.0061359231515425649189
	const double Pi512LL = -5.849159784606132e-36; // -5.8491597846061319427e-36
	const double PiH = 3.141592653589793, PiL = 1.2246467991473532e-16; // 3.1415926535897932385
	const double PiO2H = 1.5707963267948966, PiO2L = 6.123233995736766e-17; // 1.5707963267948966192
	const double PiO4D = 0.7853981633974483; // 0.78539816339744830962
	const double ThreePiO4D = 2.356194490192345; // 2.3561944901923449288
	const double PiO2D = 1.5707963267948966; // 1.5707963267948966192
	const double PiD = 3.141592653589793; // 3.1415926535897932385
	const double S3H = -0.16666666666666666, S3L = -9.25185853854297e-18; // -0.16666666666666666667
	const double S5H = 0.008333333333333333, S5L = 1.1564823173178714e-19; // 0.0083333333333333333333
	const double S7 = -0.0001984126984126984; // -0.0001984126984126984127
	const double S9 = 2.7557319223985893e-06; // 2.7557319223985890653e-6
	const double S11 = -2.505210838544172e-08; // -2.5052108385441718775e-8
	const double C4H = 0.041666666666666664, C4L = 2.3129646346357427e-18; // 0.041666666666666666667
	const double C6 = -0.001388888888888889; // -0.0013888888888888888889
	const double C8 = 2.48015873015873e-05; // 0.000024801587301587301587
	const double C10 = -2.755731922398589e-07; // -2.7557319223985890653e-7
	const double C12 = 2.08767569878681e-09; // 2.0876756987868098979e-9
	const double A3H = -0.3333333333333333, A3L = -1.850371707708594e-17; // -0.33333333333333333333
	const double A5H = 0.2, A5L = -1.1102230246251566e-17; // 0.2
	const double A7 = -0.14285714285714285; // -0.14285714285714285714
	const double A9 = 0.1111111111111111; // 0.11111111111111111111
	const double A11 = -0.09090909090909091; // -0.090909090909090909091
	const double A13 = 0.07692307692307693; // 0.076923076923076923077
	const double A15 = -0.06666666666666667; // -0.066666666666666666667
	const double E3H = 0.16666666666666666, E3L = 9.25185853854297e-18; // 0.16666666666666666667
	const double E4H = 0.041666666666666664, E4L = 2.3129646346357427e-18; // 0.041666666666666666667
	const double E5 = 0.008333333333333333; // 0.0083333333333333333333
	const double E6 = 0.001388888888888889; // 0.0013888888888888888889
	const double E7 = 0.0001984126984126984; // 0.0001984126984126984127
	const double E8 = 2.48015873015873e-05; // 0.000024801587301587301587
	const double E9 = 2.7557319223985893e-06; // 2.7557319223985890653e-6
	const double E10 = 2.755731922398589e-07; // 2.7557319223985890653e-7
	const double Ln2o256H = 0.002707606173999011; // 0.0027076061739990109345
	const double Ln2o256L = 6.327543041662719e-14; // 6.327543041662719e-14
	const double Ln2o256L3 = -5.1268353194473654e-30; // -5.1268353194473654e-30
	const double InvLn2x256 = 369.3299304675746; // 369.32993046757463228
	static readonly ulong[] SinTabBits = {
		0x0000000000000000UL, 0x0000000000000000UL, 0x3F7921F0FE670071UL, 0x3BFAB967FE6B7A9BUL,
		0x3F8921D1FCDEC784UL, 0x3C29878EBE836D9DUL, 0x3F92D936BBE30EFDUL, 0x3C2B5F91EE371D64UL,
		0x3F992155F7A3667EUL, 0xBBFB1D63091A0130UL, 0x3F9F693731D1CF01UL, 0xBBD3FE9BC66286C7UL,
		0x3FA2D865759455CDUL, 0x3C2686F65BA93AC0UL, 0x3FA5FC00D290CD43UL, 0x3C4A2669A693A8E1UL,
		0x3FA91F65F10DD814UL, 0xBC2912BD0D569A90UL, 0x3FAC428D12C0D7E3UL, 0xBC389BC74B58C513UL,
		0x3FAF656E79F820E0UL, 0xBC22E1EBE392BFFEUL, 0x3FB1440134D709B3UL, 0xBC5FEC446DAEA6ADUL,
		0x3FB2D52092CE19F6UL, 0xBC49A088A8BF6B2CUL, 0x3FB4661179272096UL, 0xBC54B109F2406C4CUL,
		0x3FB5F6D00A9AA419UL, 0xBC4F4022D03F6C9AUL, 0x3FB787586A5D5B21UL, 0x3C55F7589F083399UL,
		0x3FB917A6BC29B42CUL, 0xBC3E2718D26ED688UL, 0x3FBAA7B724495C03UL, 0x3C5E5399BA0967B8UL,
		0x3FBC3785C79EC2D5UL, 0xBC24F39DF133FB21UL, 0x3FBDC70ECBAE9FC9UL, 0x3C32FDA2D73295EEUL,
		0x3FBF564E56A9730EUL, 0x3C4A2704729AE56DUL, 0x3FC072A047BA831DUL, 0x3C519DB1F70118CAUL,
		0x3FC139F0CEDAF577UL, 0xBC6523434D1B3CFAUL, 0x3FC20116D4EC7BCFUL, 0xBC6242C8E1053452UL,
		0x3FC2C8106E8E613AUL, 0x3C513000A89A11E0UL, 0x3FC38EDBB0CD8D14UL, 0xBC6198C21FBF7718UL,
		0x3FC45576B1293E5AUL, 0xBC5285A24119F7B1UL, 0x3FC51BDF8597C5F2UL, 0xBC29F9976AF04AA5UL,
		0x3FC5E214448B3FC6UL, 0x3C6531FF779DDAC6UL, 0x3FC6A81304F64AB2UL, 0x3C5F0CD73FB5D8D4UL,
		0x3FC76DD9DE50BF31UL, 0x3C61D5EEEC501B2FUL, 0x3FC83366E89C64C6UL, 0xBC6192952DF10DB8UL,
		0x3FC8F8B83C69A60BUL, 0xBC626D19B9FF8D82UL, 0x3FC9BDCBF2DC4366UL, 0x3C69632D189956FEUL,
		0x3FCA82A025B00451UL, 0xBC687905FFD084ADUL, 0x3FCB4732EF3D6722UL, 0x3C6BBE5D5D75CBD8UL,
		0x3FCC0B826A7E4F63UL, 0xBC1AF1439E521935UL, 0x3FCCCF8CB312B286UL, 0x3C52382B0AECADF8UL,
		0x3FCD934FE5454311UL, 0x3C675B92277107ADUL, 0x3FCE56CA1E101A1BUL, 0x3C646AC3F9FD0227UL,
		0x3FCF19F97B215F1BUL, 0xBC642DEEF11DA2C4UL, 0x3FCFDCDC1ADFEDF9UL, 0xBC62DBA4580ED7BBUL,
		0x3FD04FB80E37FDAEUL, 0xBC0412CDB72583CCUL, 0x3FD0B0D9CFDBDB90UL, 0x3C53B3A7B8D1200DUL,
		0x3FD111D262B1F677UL, 0x3C7824C20AB7AA9AUL, 0x3FD172A0D7765177UL, 0x3C622575F33366BEUL,
		0x3FD1D3443F4CDB3EUL, 0xBC6720D41C13519EUL, 0x3FD233BBABC3BB71UL, 0x3C799B04E23259EFUL,
		0x3FD294062ED59F06UL, 0xBC75D28DA2C4612DUL, 0x3FD2F422DAEC0387UL, 0xBC77501BA473DA6FUL,
		0x3FD35410C2E18152UL, 0xBC73CB002F96E062UL, 0x3FD3B3CEFA0414B7UL, 0x3C7F36DC4A9C2294UL,
		0x3FD4135C94176601UL, 0x3C70C97C4AFA2518UL, 0x3FD472B8A5571054UL, 0xBC701EA0FE4DFF23UL,
		0x3FD4D1E24278E76AUL, 0x3C62417218792858UL, 0x3FD530D880AF3C24UL, 0xBC7FAB8E2103FBD6UL,
		0x3FD58F9A75AB1FDDUL, 0xBC1EFDC0D58CF620UL, 0x3FD5EE27379EA693UL, 0x3C7634FF2FA75245UL,
		0x3FD64C7DDD3F27C6UL, 0x3C510D2B4A664121UL, 0x3FD6AA9D7DC77E17UL, 0xBC738B470592C7B3UL,
		0x3FD7088530FA459FUL, 0xBC744B19E0864C5DUL, 0x3FD766340F2418F6UL, 0x3C72B2ADC9041B2CUL,
		0x3FD7C3A9311DCCE7UL, 0x3C19A3F21EF3E8D9UL, 0x3FD820E3B04EAAC4UL, 0xBC492379EB01C6B6UL,
		0x3FD87DE2A6AEA963UL, 0xBC672CEDD3D5A610UL, 0x3FD8DAA52EC8A4B0UL, 0xBC672EB2DB8C621EUL,
		0x3FD9372A63BC93D7UL, 0x3C6684319E5AD5B1UL, 0x3FD993716141BDFFUL, 0xBC715E8CCE261C55UL,
		0x3FD9EF7943A8ED8AUL, 0x3C66DA81290BDBABUL, 0x3FDA4B4127DEA1E5UL, 0xBC7BEC6F01BC22F1UL,
		0x3FDAA6C82B6D3FCAUL, 0xBC7D5F106EE5CCF7UL, 0x3FDB020D6C7F4009UL, 0x3C5414AE7E555208UL,
		0x3FDB5D1009E15CC0UL, 0x3C65B362CB974183UL, 0x3FDBB7CF2304BD01UL, 0x3C69E1A5BD9269D4UL,
		0x3FDC1249D8011EE7UL, 0xBC7813AABB515206UL, 0x3FDC6C7F4997000BUL, 0xBC7BEC2669C68E74UL,
		0x3FDCC66E9931C45EUL, 0x3C56850E59C37F8FUL, 0x3FDD2016E8E9DB5BUL, 0xBC6C8BCE9D93EFB8UL,
		0x3FDD79775B86E389UL, 0x3C7550EC87BC0575UL, 0x3FDDD28F1481CC58UL, 0xBC4E7576FA6C944EUL,
		0x3FDE2B5D3806F63BUL, 0x3C5E0D891D3C6841UL, 0x3FDE83E0EAF85114UL, 0xBC67BC380EF24BA7UL,
		0x3FDEDC1952EF78D6UL, 0xBC7DD0F7C33EDEE6UL, 0x3FDF3405963FD067UL, 0x3C706846D44A238FUL,
		0x3FDF8BA4DBF89ABAUL, 0xBC32EC1FC1B776B8UL, 0x3FDFE2F64BE71210UL, 0xBC7297AB1CA2D7DBUL,
		0x3FE01CFC874C3EB7UL, 0xBC734A35E7C2368CUL, 0x3FE0485626AE221AUL, 0x3C8B937D9091FF70UL,
		0x3FE073879922FFEEUL, 0xBC8A5A014347406CUL, 0x3FE09E907417C5E1UL, 0xBC8FE573741A9BD4UL,
		0x3FE0C9704D5D898FUL, 0xBC88D3D7DE6EE9B2UL, 0x3FE0F426BB2A8E7EUL, 0xBC8BB58FB774F8EEUL,
		0x3FE11EB3541B4B23UL, 0xBC8EF23B69ABE4F1UL, 0x3FE14915AF336CEBUL, 0x3C7F3660558A0213UL,
		0x3FE1734D63DEDB49UL, 0xBC87EEF2CCC50575UL, 0x3FE19D5A09F2B9B8UL, 0xBC633656C68A1D4AUL,
		0x3FE1C73B39AE68C8UL, 0x3C8B25DD267F6600UL, 0x3FE1F0F08BBC861BUL, 0xBC610D9DCAFB74CBUL,
		0x3FE21A799933EB59UL, 0xBC83A7B177C68FB2UL, 0x3FE243D5FB98AC1FUL, 0x3C7C533D0A284A8DUL,
		0x3FE26D054CDD12DFUL, 0xBC85DA743EF3770CUL, 0x3FE2960727629CA8UL, 0x3C756D6C7AF02D5CUL,
		0x3FE2BEDB25FAF3EAUL, 0xBC514981C796EE46UL, 0x3FE2E780E3E8EA17UL, 0xBC8B19FAFE36587AUL,
		0x3FE30FF7FCE17035UL, 0xBC6EFCC626F74A6FUL, 0x3FE338400D0C8E57UL, 0xBC8ABF2A5E95E6E5UL,
		0x3FE36058B10659F3UL, 0xBC81FCB3A35857E7UL, 0x3FE3884185DFEB22UL, 0xBC7A038026ABE6B2UL,
		0x3FE3AFFA292050B9UL, 0x3C7E3E25E3954964UL, 0x3FE3D78238C58344UL, 0xBC80219F5F0F79CEUL,
		0x3FE3FED9534556D4UL, 0x3C836916608C5061UL, 0x3FE425FF178E6BB1UL, 0x3C87B38D675140CAUL,
		0x3FE44CF325091DD6UL, 0x3C68076A2CFDC6B3UL, 0x3FE473B51B987347UL, 0x3C6CA1953514E41BUL,
		0x3FE49A449B9B0939UL, 0xBC827EE16D719B94UL, 0x3FE4C0A145EC0004UL, 0x3C52630CFAFCEAA1UL,
		0x3FE4E6CABBE3E5E9UL, 0x3C63C293EDCEB327UL, 0x3FE50CC09F59A09BUL, 0x3C7693463A2C2E6FUL,
		0x3FE5328292A35596UL, 0xBC7A12EB89DA0257UL, 0x3FE5581038975137UL, 0x3C84570D9EFE26DFUL,
		0x3FE57D69348CECA0UL, 0xBC875720992BFBB2UL, 0x3FE5A28D2A5D7250UL, 0x3C857A25F8B13430UL,
		0x3FE5C77BBE65018CUL, 0x3C8069EA9C0BC32AUL, 0x3FE5EC3495837074UL, 0x3C7DEA89A9B8F727UL,
		0x3FE610B7551D2CDFUL, 0xBC7251B352FF2A37UL, 0x3FE63503A31C1BE9UL, 0x3C61248F09E6587CUL,
		0x3FE6591925F0783DUL, 0x3C8C3D64FBF5DE23UL, 0x3FE67CF78491AF10UL, 0x3C4750AB23477B61UL,
		0x3FE6A09E667F3BCDUL, 0xBC8BDD3413B26456UL, 0x3FE6C40D73C18275UL, 0x3C625D4F802BE257UL,
		0x3FE6E74454EAA8AFUL, 0xBC8DBC03C84E226EUL, 0x3FE70A42B3176D7AUL, 0xBC7D9E3FBE2E15A0UL,
		0x3FE72D0837EFFF96UL, 0x3C80D4EF0F1D915CUL, 0x3FE74F948DA8D28DUL, 0x3C019900A3B9A3A2UL,
		0x3FE771E75F037261UL, 0x3C75CFCE8D84068FUL, 0x3FE79400574F55E5UL, 0xBC80ADADBDB4C65AUL,
		0x3FE7B5DF226AAFAFUL, 0xBC70F537ACDF0AD7UL, 0x3FE7D7836CC33DB2UL, 0x3C7162715EF03F85UL,
		0x3FE7F8ECE3571771UL, 0xBC89C8D8CE93C917UL, 0x3FE81A1B33B57ACCUL, 0xBC85DEA12D66BB66UL,
		0x3FE83B0E0BFF976EUL, 0xBC76F420F8EA3475UL, 0x3FE85BC51AE958CCUL, 0x3C845BA6478086CCUL,
		0x3FE87C400FBA2EBFUL, 0xBC82DABC0C3F64CDUL, 0x3FE89C7E9A4DD4AAUL, 0x3C8DB6EA04A8678FUL,
		0x3FE8BC806B151741UL, 0xBC82C5E12ED1336DUL, 0x3FE8DC45331698CCUL, 0x3C61D9FCD83634D7UL,
		0x3FE8FBCCA3EF940DUL, 0xBC66DFA99C86F2F1UL, 0x3FE91B166FD49DA2UL, 0xBC63BE953A7FE996UL,
		0x3FE93A22499263FBUL, 0x3C83D419A920DF0BUL, 0x3FE958EFE48E6DD7UL, 0xBC8561335DA0F4E7UL,
		0x3FE9777EF4C7D742UL, 0xBC815479A240665EUL, 0x3FE995CF2ED80D22UL, 0x3C77783E907FBD7BUL,
		0x3FE9B3E047F38741UL, 0xBC830EE286712474UL, 0x3FE9D1B1F5EA80D5UL, 0x3C8C5FADD5FFB36FUL,
		0x3FE9EF43EF29AF94UL, 0x3C7B1DFCB60445C2UL, 0x3FEA0C95EABAF937UL, 0xBC8E0CA3ACBD049AUL,
		0x3FEA29A7A0462782UL, 0xBC7128BB015DF175UL, 0x3FEA4678C8119AC8UL, 0x3C81B4C0DD3F212AUL,
		0x3FEA63091B02FAE2UL, 0xBC7E911152248D10UL, 0x3FEA7F58529FE69DUL, 0xBC897A441584A179UL,
		0x3FEA9B66290EA1A3UL, 0x3C39F630E8B6DAC8UL, 0x3FEAB7325916C0D4UL, 0x3C8A8B8C85BAAA9BUL,
		0x3FEAD2BC9E21D511UL, 0xBC847FBE07BEA548UL, 0x3FEAEE04B43C1474UL, 0xBC83A79A438BF8CCUL,
		0x3FEB090A58150200UL, 0xBC8926DA300FFCCEUL, 0x3FEB23CD470013B4UL, 0x3C75A1BB35AD6D2EUL,
		0x3FEB3E4D3EF55712UL, 0xBC8EB6B8BF11A493UL, 0x3FEB5889FE921405UL, 0xBC6DF49B307C8602UL,
		0x3FEB728345196E3EUL, 0xBC8BC69F324E6D61UL, 0x3FEB8C38D27504E9UL, 0xBC81529ABFF40E45UL,
		0x3FEBA5AA673590D2UL, 0x3C87EA4E370753B6UL, 0x3FEBBED7C49380EAUL, 0x3C4BEACBD88500B4UL,
		0x3FEBD7C0AC6F952AUL, 0xBC8825A732AC700AUL, 0x3FEBF064E15377DDUL, 0x3C62156026A1E028UL,
		0x3FEC08C426725549UL, 0x3C5B157FD80E2946UL, 0x3FEC20DE3FA971B0UL, 0xBC8B4CA2BAB1322CUL,
		0x3FEC38B2F180BDB1UL, 0xBC76E0B1757C8D07UL, 0x3FEC5042012B6907UL, 0xBC65C058DD8EABA5UL,
		0x3FEC678B3488739BUL, 0x3C6D86CAC7C5FF5BUL, 0x3FEC7E8E52233CF3UL, 0x3C6B2AD324AA35C1UL,
		0x3FEC954B213411F5UL, 0xBC52FB761E946603UL, 0x3FECABC169A0B900UL, 0x3C8C42D3E10851D1UL,
		0x3FECC1F0F3FCFC5CUL, 0x3C7E57613B68F6ABUL, 0x3FECD7D9898B32F6UL, 0xBC6F2FA062496738UL,
		0x3FECED7AF43CC773UL, 0xBC5E7B6BB5AB58AEUL, 0x3FED02D4FEB2BD92UL, 0x3C8195FF41BC55FEUL,
		0x3FED17E7743E35DCUL, 0xBC5101DA3540130AUL, 0x3FED2CB220E0EF9FUL, 0xBC7F07656D4E6652UL,
		0x3FED4134D14DC93AUL, 0xBC84EF5295D25AF2UL, 0x3FED556F52E93EB1UL, 0xBC880ED9233A9630UL,
		0x3FED696173C9E68BUL, 0xBC7E8C61C6393D55UL, 0x3FED7D0B02B8ECF9UL, 0x3C8800F4CE65CD6EUL,
		0x3FED906BCF328D46UL, 0x3C7457E610231AC2UL, 0x3FEDA383A9668988UL, 0xBC85811000B39D84UL,
		0x3FEDB6526238A09BUL, 0xBC7ADEE7EAE69460UL, 0x3FEDC8D7CB410260UL, 0x3C76B7872773830DUL,
		0x3FEDDB13B6CCC23CUL, 0x3C883C37C6107DB3UL, 0x3FEDED05F7DE47DAUL, 0xBC82CC4C1F8BA966UL,
		0x3FEDFEAE622DBE2BUL, 0xBC8514EA88425567UL, 0x3FEE100CCA2980ACUL, 0xBC602D182ACDF825UL,
		0x3FEE212104F686E5UL, 0xBC8014C76C126527UL, 0x3FEE31EAE870CE25UL, 0xBC7BC7094538D678UL,
		0x3FEE426A4B2BC17EUL, 0x3C8A873889744882UL, 0x3FEE529F04729FFCUL, 0x3C89075D6E6DFC8BUL,
		0x3FEE6288EC48E112UL, 0xBC616B56F2847754UL, 0x3FEE7227DB6A9744UL, 0x3C82128794DA5A50UL,
		0x3FEE817BAB4CD10DUL, 0xBC7D0AFE686B5E0AUL, 0x3FEE9084361DF7F2UL, 0x3C8CDFC7CE9DC3E9UL,
		0x3FEE9F4156C62DDAUL, 0x3C8760B1E2E3F81EUL, 0x3FEEADB2E8E7A88EUL, 0xBC892EC52EA226A3UL,
		0x3FEEBBD8C8DF0B74UL, 0x3C7C6C8C615E7277UL, 0x3FEEC9B2D3C3BF84UL, 0x3C719119D358DE05UL,
		0x3FEED740E7684963UL, 0x3C7E82C791F59CC2UL, 0x3FEEE482E25A9DBCUL, 0xBC7B6066EF81AF2AUL,
		0x3FEEF178A3E473C2UL, 0x3C86310A67FE774FUL, 0x3FEEFE220C0B95ECUL, 0x3C8C853B7BF7E0CDUL,
		0x3FEF0A7EFB9230D7UL, 0x3C752C7ADC6B4989UL, 0x3FEF168F53F7205DUL, 0xBC626A6C1F015601UL,
		0x3FEF2252F7763ADAUL, 0xBC820CB81C8D94ABUL, 0x3FEF2DC9C9089A9DUL, 0x3C45407460BDFC07UL,
		0x3FEF38F3AC64E589UL, 0xBC7D7BAFB51F72E6UL, 0x3FEF43D085FF92DDUL, 0xBC88FDE71E361C05UL,
		0x3FEF4E603B0B2F2DUL, 0xBC78EE01E695AC05UL, 0x3FEF58A2B1789E84UL, 0x3C71F4A188AA3680UL,
		0x3FEF6297CFF75CB0UL, 0x3C7562172A361FD3UL, 0x3FEF6C3F7DF5BBB7UL, 0x3C78561CE9D5EF5BUL,
		0x3FEF7599A3A12077UL, 0x3C884F31D743195CUL, 0x3FEF7EA629E63D6EUL, 0x3C8BA92D57EBFEDDUL,
		0x3FEF8764FA714BA9UL, 0x3C7AB256778FFCB6UL, 0x3FEF8FD5FFAE41DBUL, 0xBC78CFD77FD970D2UL,
		0x3FEF97F924C9099BUL, 0xBC8E2AE0EEA5963BUL, 0x3FEF9FCE55ADB2C8UL, 0x3C7F2A06FAB9F9D1UL,
		0x3FEFA7557F08A517UL, 0xBC87A0A8CA13571FUL, 0x3FEFAE8E8E46CFBBUL, 0xBC73A9E414732D97UL,
		0x3FEFB5797195D741UL, 0x3C71BFAC7397CC08UL, 0x3FEFBC1617E44186UL, 0xBC458EC496DC4ECBUL,
		0x3FEFC26470E19FD3UL, 0x3C81EC8668ECACEEUL, 0x3FEFC8646CFEB721UL, 0x3C83143DC43A9B9DUL,
		0x3FEFCE15FD6DA67BUL, 0xBC75DD6F830D4C09UL, 0x3FEFD37914220B84UL, 0x3C852E9D7B772791UL,
		0x3FEFD88DA3D12526UL, 0xBC887DF6378811C7UL, 0x3FEFDD539FF1F456UL, 0xBC7AB13CBBEC1781UL,
		0x3FEFE1CAFCBD5B09UL, 0x3C6A23E3202A884EUL, 0x3FEFE5F3AF2E3940UL, 0x3C8B213F18C9CF17UL,
		0x3FEFE9CDAD01883AUL, 0x3C6521ECD0C67E35UL, 0x3FEFED58ECB673C4UL, 0xBC7E6E462A7AE686UL,
		0x3FEFF095658E71ADUL, 0x3C801A8CE18A4B9EUL, 0x3FEFF3830F8D575CUL, 0xBC795E1E79D335F7UL,
		0x3FEFF621E3796D7EUL, 0xBC6C57BC2E24AA15UL, 0x3FEFF871DADB81DFUL, 0x3C78B1C676208AA4UL,
		0x3FEFFA72EFFEF75DUL, 0xBC88B4CDCDB25956UL, 0x3FEFFC251DF1D3F8UL, 0x3C77A7D209F32D43UL,
		0x3FEFFD886084CD0DUL, 0xBC81354D4556E4CBUL, 0x3FEFFE9CB44B51A1UL, 0x3C75B43366DF6670UL,
		0x3FEFFF62169B92DBUL, 0x3C85DDA3C81FBD0DUL, 0x3FEFFFD8858E8A92UL, 0x3C8359C71883BCF7UL,
		0x3FF0000000000000UL, 0x0000000000000000UL,
	};
	static readonly ulong[] ExpTabBits = {
		0x3FF0000000000000UL, 0x0000000000000000UL, 0x3FF00B1AFA5ABCBFUL, 0xBC84F6B2A7609F71UL,
		0x3FF0163DA9FB3335UL, 0x3C9B61299AB8CDB7UL, 0x3FF02168143B0281UL, 0xBC82BF310FC54EB6UL,
		0x3FF02C9A3E778061UL, 0xBC719083535B085DUL, 0x3FF037D42E11BBCCUL, 0x3C656811EEADE11AUL,
		0x3FF04315E86E7F85UL, 0xBC90A31C1977C96EUL, 0x3FF04E5F72F654B1UL, 0x3C84C3793AA0D08DUL,
		0x3FF059B0D3158574UL, 0x3C8D73E2A475B465UL, 0x3FF0650A0E3C1F89UL, 0xBC95CB7B5799C397UL,
		0x3FF0706B29DDF6DEUL, 0xBC8C91DFE2B13C27UL, 0x3FF07BD42B72A836UL, 0x3C83233454458700UL,
		0x3FF0874518759BC8UL, 0x3C6186BE4BB284FFUL, 0x3FF092BDF66607E0UL, 0xBC968063800A3FD1UL,
		0x3FF09E3ECAC6F383UL, 0x3C91487818316136UL, 0x3FF0A9C79B1F3919UL, 0x3C85D16C873D1D38UL,
		0x3FF0B5586CF9890FUL, 0x3C98A62E4ADC610BUL, 0x3FF0C0F145E46C85UL, 0x3C94F98906D21CEFUL,
		0x3FF0CC922B7247F7UL, 0x3C901EDC16E24F71UL, 0x3FF0D83B23395DECUL, 0xBC9BC14DE43F316AUL,
		0x3FF0E3EC32D3D1A2UL, 0x3C403A1727C57B53UL, 0x3FF0EFA55FDFA9C5UL, 0xBC949DB9BC54021BUL,
		0x3FF0FB66AFFED31BUL, 0xBC6B9BEDC44EBD7BUL, 0x3FF1073028D7233EUL, 0x3C8D46EB1692FDD5UL,
		0x3FF11301D0125B51UL, 0xBC96C51039449B3AUL, 0x3FF11EDBAB5E2AB6UL, 0xBC9CA454F703FB72UL,
		0x3FF12ABDC06C31CCUL, 0xBC51B514B36CA5C7UL, 0x3FF136A814F204ABUL, 0xBC67108FBA48DCF0UL,
		0x3FF1429AAEA92DE0UL, 0xBC932FBF9AF1369EUL, 0x3FF14E95934F312EUL, 0xBC8B91E839BF44ABUL,
		0x3FF15A98C8A58E51UL, 0x3C82406AB9EEAB0AUL, 0x3FF166A45471C3C2UL, 0x3C58F23B82EA1A32UL,
		0x3FF172B83C7D517BUL, 0xBC819041B9D78A76UL, 0x3FF17ED48695BBC0UL, 0x3C709E3FE2AC5A64UL,
		0x3FF18AF9388C8DEAUL, 0xBC911023D1970F6CUL, 0x3FF1972658375D2FUL, 0x3C94AADD85F17E08UL,
		0x3FF1A35BEB6FCB75UL, 0x3C8E5B4C7B4968E4UL, 0x3FF1AF99F8138A1CUL, 0x3C97BF85A4B69280UL,
		0x3FF1BBE084045CD4UL, 0xBC995386352EF607UL, 0x3FF1C82F95281C6BUL, 0x3C9009778010F8C9UL,
		0x3FF1D4873168B9AAUL, 0x3C9E016E00A2643CUL, 0x3FF1E0E75EB44027UL, 0xBC96FDD8088CB6DEUL,
		0x3FF1ED5022FCD91DUL, 0xBC91DF98027BB78CUL, 0x3FF1F9C18438CE4DUL, 0xBC9BF524A097AF5CUL,
		0x3FF2063B88628CD6UL, 0x3C8DC775814A8495UL, 0x3FF212BE3578A819UL, 0x3C93592D2CFCAAC9UL,
		0x3FF21F49917DDC96UL, 0x3C82A97E9494A5EEUL, 0x3FF22BDDA27912D1UL, 0x3C8D34FB5577D69FUL,
		0x3FF2387A6E756238UL, 0x3C99B07EB6C70573UL, 0x3FF2451FFB82140AUL, 0x3C8ACFCC911CA996UL,
		0x3FF251CE4FB2A63FUL, 0x3C8AC155BEF4F4A4UL, 0x3FF25E85711ECE75UL, 0x3C93E1A24AC31B2CUL,
		0x3FF26B4565E27CDDUL, 0x3C82BD339940E9D9UL, 0x3FF2780E341DDF29UL, 0x3C9E067C05F9E76CUL,
		0x3FF284DFE1F56381UL, 0xBC9A4C3A8C3F0D7EUL, 0x3FF291BA7591BB70UL, 0xBC82CC7228401CBDUL,
		0x3FF29E9DF51FDEE1UL, 0x3C8612E8AFAD1255UL, 0x3FF2AB8A66D10F13UL, 0xBC995743191690A7UL,
		0x3FF2B87FD0DAD990UL, 0xBC410ADCD6381AA4UL, 0x3FF2C57E39771B2FUL, 0xBC950145A6EB5124UL,
		0x3FF2D285A6E4030BUL, 0x3C90024754DB41D5UL, 0x3FF2DF961F641589UL, 0x3C9D16CFFBBCE198UL,
		0x3FF2ECAFA93E2F56UL, 0x3C71CA0F45D52383UL, 0x3FF2F9D24ABD886BUL, 0xBC653C55532BDA93UL,
		0x3FF306FE0A31B715UL, 0x3C86F46AD23182E4UL, 0x3FF31432EDEEB2FDUL, 0x3C8959A3F3F3FCD1UL,
		0x3FF32170FC4CD831UL, 0x3C8A9CE78E18047CUL, 0x3FF32EB83BA8EA32UL, 0xBC9C45E83CB4F318UL,
		0x3FF33C08B26416FFUL, 0x3C932721843659A6UL, 0x3FF3496266E3FA2DUL, 0xBC835A75930881A4UL,
		0x3FF356C55F929FF1UL, 0xBC8B5CEE5C4E4628UL, 0x3FF36431A2DE883BUL, 0xBC8C3144A06CB85EUL,
		0x3FF371A7373AA9CBUL, 0xBC963AEABF42EAE2UL, 0x3FF37F26231E754AUL, 0xBC99F5CA9ECEB23CUL,
		0x3FF38CAE6D05D866UL, 0xBC9E958D3C9904BDUL, 0x3FF39A401B7140EFUL, 0xBC99A9A5FC8E2934UL,
		0x3FF3A7DB34E59FF7UL, 0xBC75E436D661F5E3UL, 0x3FF3B57FBFEC6CF4UL, 0x3C954C66E26FFF18UL,
		0x3FF3C32DC313A8E5UL, 0xBC9EFFF8375D29C3UL, 0x3FF3D0E544EDE173UL, 0x3C7FE8D08C284C71UL,
		0x3FF3DEA64C123422UL, 0x3C8ADA0911F09EBCUL, 0x3FF3EC70DF1C5175UL, 0xBC8AF6637B8C9BCAUL,
		0x3FF3FA4504AC801CUL, 0xBC97D023F956F9F3UL, 0x3FF40822C367A024UL, 0x3C8BDDF8B6F4D048UL,
		0x3FF4160A21F72E2AUL, 0xBC5EF3691C309278UL, 0x3FF423FB2709468AUL, 0xBC98462DC0B314DDUL,
		0x3FF431F5D950A897UL, 0xBC81C7DDE35F7999UL, 0x3FF43FFA3F84B9D4UL, 0x3C8880BE9704C003UL,
		0x3FF44E086061892DUL, 0x3C489B7A04EF80D0UL, 0x3FF45C2042A7D232UL, 0xBC68641982FB1F8EUL,
		0x3FF46A41ED1D0057UL, 0x3C9C944BD1648A76UL, 0x3FF4786D668B3237UL, 0xBC9C20F0ED445733UL,
		0x3FF486A2B5C13CD0UL, 0x3C73C1A3B69062F0UL, 0x3FF494E1E192AED2UL, 0xBC83B2895E499EA0UL,
		0x3FF4A32AF0D7D3DEUL, 0x3C99CB62F3D1BE56UL, 0x3FF4B17DEA6DB7D7UL, 0xBC8125B87F2897F0UL,
		0x3FF4BFDAD5362A27UL, 0x3C7D4397AFEC42E2UL, 0x3FF4CE41B817C114UL, 0x3C905E29690ABD5DUL,
		0x3FF4DCB299FDDD0DUL, 0x3C98ECDBBC6A7833UL, 0x3FF4EB2D81D8ABFFUL, 0xBC95257D2E5D7A52UL,
		0x3FF4F9B2769D2CA7UL, 0xBC94B309D25957E3UL, 0x3FF508417F4531EEUL, 0x3C7A249B49B7465FUL,
		0x3FF516DAA2CF6642UL, 0xBC8F768569BD93EFUL, 0x3FF5257DE83F4EEFUL, 0xBC7C998D43EFEF71UL,
		0x3FF5342B569D4F82UL, 0xBC807ABE1DB13CADUL, 0x3FF542E2F4F6AD27UL, 0x3C87926D192D5F7EUL,
		0x3FF551A4CA5D920FUL, 0xBC8D689CEFEDE59BUL, 0x3FF56070DDE910D2UL, 0xBC90FB6E168EEBF0UL,
		0x3FF56F4736B527DAUL, 0x3C99BB2C011D93ADUL, 0x3FF57E27DBE2C4CFUL, 0xBC90B98C8A57B9C4UL,
		0x3FF58D12D497C7FDUL, 0x3C8295E15B9A1DE8UL, 0x3FF59C0827FF07CCUL, 0xBC97E2CEE467E60FUL,
		0x3FF5AB07DD485429UL, 0x3C96324C054647ADUL, 0x3FF5BA11FBA87A03UL, 0xBC9B77A14C233E1AUL,
		0x3FF5C9268A5946B7UL, 0x3C3C4B1B816986A2UL, 0x3FF5D84590998B93UL, 0xBC9CD6A7A8B45643UL,
		0x3FF5E76F15AD2148UL, 0x3C9BA6F93080E65EUL, 0x3FF5F6A320DCEB71UL, 0xBC89EADDE3CDCF92UL,
		0x3FF605E1B976DC09UL, 0xBC93E2429B56DE47UL, 0x3FF6152AE6CDF6F4UL, 0x3C9E4B3E4AB84C27UL,
		0x3FF6247EB03A5585UL, 0xBC9383C17E40B497UL, 0x3FF633DD1D1929FDUL, 0x3C984710BEB964E5UL,
		0x3FF6434634CCC320UL, 0xBC8C483C759D8933UL, 0x3FF652B9FEBC8FB7UL, 0xBC9AE3D5C9A73E09UL,
		0x3FF6623882552225UL, 0xBC9BB60987591C34UL, 0x3FF671C1C70833F6UL, 0xBC8E8732586C6134UL,
		0x3FF68155D44CA973UL, 0x3C6038AE44F73E65UL, 0x3FF690F4B19E9538UL, 0x3C8804BD9AEB445DUL,
		0x3FF6A09E667F3BCDUL, 0xBC9BDD3413B26456UL, 0x3FF6B052FA75173EUL, 0x3C7A38F52C9A9D0EUL,
		0x3FF6C012750BDABFUL, 0xBC72895667FF0B0DUL, 0x3FF6CFDCDDD47645UL, 0x3C9C7AA9B6F17309UL,
		0x3FF6DFB23C651A2FUL, 0xBC6BBE3A683C88ABUL, 0x3FF6EF9298593AE5UL, 0xBC90B9749E1AC8B2UL,
		0x3FF6FF7DF9519484UL, 0xBC883C0F25860EF6UL, 0x3FF70F7466F42E87UL, 0x3C59D644D45AA65FUL,
		0x3FF71F75E8EC5F74UL, 0xBC816E4786887A99UL, 0x3FF72F8286EAD08AUL, 0xBC920AA02CD62C72UL,
		0x3FF73F9A48A58174UL, 0xBC90A8D96C65D53CUL, 0x3FF74FBD35D7CBFDUL, 0x3C9047FD618A6E1CUL,
		0x3FF75FEB564267C9UL, 0xBC90245957316DD3UL, 0x3FF77024B1AB6E09UL, 0x3C9B7877169147F8UL,
		0x3FF780694FDE5D3FUL, 0x3C9866B80A02162DUL, 0x3FF790B938AC1CF6UL, 0x3C9349A862AADD3EUL,
		0x3FF7A11473EB0187UL, 0xBC841577EE04992FUL, 0x3FF7B17B0976CFDBUL, 0xBC9BEBB58468DC88UL,
		0x3FF7C1ED0130C132UL, 0x3C9F124CD1164DD6UL, 0x3FF7D26A62FF86F0UL, 0x3C91BDDBFB72B8B4UL,
		0x3FF7E2F336CF4E62UL, 0x3C705D02BA15797EUL, 0x3FF7F3878491C491UL, 0xBC807F11CF9311AEUL,
		0x3FF80427543E1A12UL, 0xBC927C86626D972BUL, 0x3FF814D2ADD106D9UL, 0x3C9464370D151D4DUL,
		0x3FF82589994CCE13UL, 0xBC9D4C1DD41532D8UL, 0x3FF8364C1EB941F7UL, 0x3C999B9A31DF2BD5UL,
		0x3FF8471A4623C7ADUL, 0xBC88D684A341CDFBUL, 0x3FF857F4179F5B21UL, 0xBC5BA748F8B216D0UL,
		0x3FF868D99B4492EDUL, 0xBC9FC6F89BD4F6BAUL, 0x3FF879CAD931A436UL, 0x3C85D2D7D2DB47BDUL,
		0x3FF88AC7D98A6699UL, 0x3C9994C2F37CB53AUL, 0x3FF89BD0A478580FUL, 0x3C9D53954475202BUL,
		0x3FF8ACE5422AA0DBUL, 0x3C96E9F156864B27UL, 0x3FF8BE05BAD61778UL, 0x3C9ECB5EFC43446EUL,
		0x3FF8CF3216B5448CUL, 0xBC70D55E32E9E3AAUL, 0x3FF8E06A5E0866D9UL, 0xBC97114A6FC9B2E6UL,
		0x3FF8F1AE99157736UL, 0x3C85CC13A2E3976CUL, 0x3FF902FED0282C8AUL, 0x3C9592CA85FE3FD2UL,
		0x3FF9145B0B91FFC6UL, 0xBC9DD6792E582524UL, 0x3FF925C353AA2FE2UL, 0xBC83455FA639DB7FUL,
		0x3FF93737B0CDC5E5UL, 0xBC675FC781B57EBCUL, 0x3FF948B82B5F98E5UL, 0xBC8DC3D6797D2D99UL,
		0x3FF95A44CBC8520FUL, 0xBC764B7C96A5F039UL, 0x3FF96BDD9A7670B3UL, 0xBC5BA5967F19C896UL,
		0x3FF97D829FDE4E50UL, 0xBC9D185B7C1B85D1UL, 0x3FF98F33E47A22A2UL, 0x3C7CABDAA24C78EDUL,
		0x3FF9A0F170CA07BAUL, 0xBC9173BD91CEE632UL, 0x3FF9B2BB4D53FE0DUL, 0xBC9DD84E4DF6D518UL,
		0x3FF9C49182A3F090UL, 0x3C7C7C46B071F2BEUL, 0x3FF9D674194BB8D5UL, 0xBC9516BEA3DD8233UL,
		0x3FF9E86319E32323UL, 0x3C7824CA78E64C6EUL, 0x3FF9FA5E8D07F29EUL, 0xBC84A9CEAAF1FACEUL,
		0x3FFA0C667B5DE565UL, 0xBC9359495D1CD533UL, 0x3FFA1E7AED8EB8BBUL, 0x3C9C6618EE8BE70EUL,
		0x3FFA309BEC4A2D33UL, 0x3C96305C7DDC36ABUL, 0x3FFA42C980460AD8UL, 0xBC9AA780589FB120UL,
		0x3FFA5503B23E255DUL, 0xBC9D2F6EDB8D41E1UL, 0x3FFA674A8AF46052UL, 0x3C650F5630670366UL,
		0x3FFA799E1330B358UL, 0x3C9BCB7ECAC563C7UL, 0x3FFA8BFE53C12E59UL, 0xBC94F867B2BA15A9UL,
		0x3FFA9E6B5579FDBFUL, 0x3C90FAC90EF7FD31UL, 0x3FFAB0E521356EBAUL, 0x3C889C31DAE94545UL,
		0x3FFAC36BBFD3F37AUL, 0xBC8F9234CAE76CD0UL, 0x3FFAD5FF3A3C2774UL, 0x3C97EF3BB6B1B8E5UL,
		0x3FFAE89F995AD3ADUL, 0x3C97A1CD345DCC81UL, 0x3FFAFB4CE622F2FFUL, 0xBC94B2FC0F315ECDUL,
		0x3FFB0E07298DB666UL, 0xBC9BDEF54C80E425UL, 0x3FFB20CE6C9A8952UL, 0x3C94DD024A0756CCUL,
		0x3FFB33A2B84F15FBUL, 0xBC62805E3084D708UL, 0x3FFB468415B749B1UL, 0xBC7F763DE9DF7C90UL,
		0x3FFB59728DE5593AUL, 0xBC9C71DFBBBA6DE3UL, 0x3FFB6C6E29F1C52AUL, 0x3C92A8F352883F6EUL,
		0x3FFB7F76F2FB5E47UL, 0xBC75584F7E54AC3BUL, 0x3FFB928CF22749E4UL, 0xBC9B721654CB65C6UL,
		0x3FFBA5B030A1064AUL, 0xBC9EFCD30E54292EUL, 0x3FFBB8E0B79A6F1FUL, 0xBC3F52D1C9696205UL,
		0x3FFBCC1E904BC1D2UL, 0x3C823DD07A2D9E84UL, 0x3FFBDF69C3F3A207UL, 0xBC3C262360EA5B52UL,
		0x3FFBF2C25BD71E09UL, 0xBC9EFDCA3F6B9C73UL, 0x3FFC06286141B33DUL, 0xBC8D8A5AA1FBCA34UL,
		0x3FFC199BDD85529CUL, 0x3C811065895048DDUL, 0x3FFC2D1CD9FA652CUL, 0xBC96E51617C8A5D7UL,
		0x3FFC40AB5FFFD07AUL, 0x3C9B4537E083C60AUL, 0x3FFC544778FAFB22UL, 0x3C912F072493B5AFUL,
		0x3FFC67F12E57D14BUL, 0x3C92884DFF483CADUL, 0x3FFC7BA88988C933UL, 0xBC8E76BBBE255559UL,
		0x3FFC8F6D9406E7B5UL, 0x3C71ACBC48805C44UL, 0x3FFCA3405751C4DBUL, 0xBC87F2BED10D08F5UL,
		0x3FFCB720DCEF9069UL, 0x3C7503CBD1E949DBUL, 0x3FFCCB0F2E6D1675UL, 0xBC7D220F86009093UL,
		0x3FFCDF0B555DC3FAUL, 0xBC8DD83B53829D72UL, 0x3FFCF3155B5BAB74UL, 0xBC9A08E9B86DFF57UL,
		0x3FFD072D4A07897CUL, 0xBC9CBC3743797A9CUL, 0x3FFD1B532B08C968UL, 0x3C955636219A36EEUL,
		0x3FFD2F87080D89F2UL, 0xBC9D487B719D8578UL, 0x3FFD43C8EACAA1D6UL, 0x3C93DB53BF5A1614UL,
		0x3FFD5818DCFBA487UL, 0x3C82ED02D75B3707UL, 0x3FFD6C76E862E6D3UL, 0x3C5FE87A4A8165A0UL,
		0x3FFD80E316C98398UL, 0xBC911EC18BEDDFE8UL, 0x3FFD955D71FF6075UL, 0x3C9A052DBB9AF6BEUL,
		0x3FFDA9E603DB3285UL, 0x3C9C2300696DB532UL, 0x3FFDBE7CD63A8315UL, 0xBC9B76F1926B8BE4UL,
		0x3FFDD321F301B460UL, 0x3C92DA5778F018C3UL, 0x3FFDE7D5641C0658UL, 0xBC9CA5528E79BA8FUL,
		0x3FFDFC97337B9B5FUL, 0xBC91A5CD4F184B5CUL, 0x3FFE11676B197D17UL, 0xBC72B529BD5C7F44UL,
		0x3FFE264614F5A129UL, 0xBC97B627817A1496UL, 0x3FFE3B333B16EE12UL, 0xBC99F4A431FDC68BUL,
		0x3FFE502EE78B3FF6UL, 0x3C839E8980A9CC8FUL, 0x3FFE653924676D76UL, 0xBC863FF87522B735UL,
		0x3FFE7A51FBC74C83UL, 0x3C92D522CA0C8DE2UL, 0x3FFE8F7977CDB740UL, 0xBC91089480B054B1UL,
		0x3FFEA4AFA2A490DAUL, 0xBC9E9C23179C2893UL, 0x3FFEB9F4867CCA6EUL, 0x3C94832F2293E4F2UL,
		0x3FFECF482D8E67F1UL, 0xBC9C93F3B411AD8CUL, 0x3FFEE4AAA2188510UL, 0x3C91C68DA487568DUL,
		0x3FFEFA1BEE615A27UL, 0x3C9DC7F486A4B6B0UL, 0x3FFF0F9C1CB6412AUL, 0xBC93220065181D45UL,
		0x3FFF252B376BBA97UL, 0x3C93A1A5BF0D8E43UL, 0x3FFF3AC948DD7274UL, 0xBC795A5A3ED837DEUL,
		0x3FFF50765B6E4540UL, 0x3C99D3E12DD8A18BUL, 0x3FFF6632798844F8UL, 0x3C9FA37B3539343EUL,
		0x3FFF7BFDAD9CBE14UL, 0xBC9DBB12D006350AUL, 0x3FFF91D802243C89UL, 0xBC612EA8A779F689UL,
		0x3FFFA7C1819E90D8UL, 0x3C874853F3A5931EUL, 0x3FFFBDBA3692D514UL, 0xBC79677315098EB6UL,
		0x3FFFD3C22B8F71F1UL, 0x3C62EB74966579E7UL, 0x3FFFE9D96B2A23D9UL, 0x3C74A6037442FDE3UL,
	};
	static readonly ulong[] TwoOverPi = {
		0xA2F9836E4E441529UL, 0xFC2757D1F534DDC0UL, 0xDB6295993C439041UL, 0xFE5163ABDEBBC561UL,
		0xB7246E3A424DD2E0UL, 0x06492EEA09D1921CUL, 0xFE1DEB1CB129A73EUL, 0xE88235F52EBB4484UL,
		0xE99C7026B45F7E41UL, 0x3991D639835339F4UL, 0x9C845F8BBDF9283BUL, 0x1FF897FFDE05980FUL,
		0xEF2F118B5A0A6D1FUL, 0x6D367ECF27CB09B7UL, 0x4F463F669E5FEA2DUL, 0x7527BAC7EBE5F17BUL,
		0x3D0739F78A5292EAUL, 0x6BFB5FB11F8D5D08UL, 0x56033046FC7B6BABUL, 0xF0CFBC209AF4361DUL,
		0xA9E391615EE61B08UL, 0x6599855F14A06840UL, 0x8DFFD8804D732731UL, 0x06061556CA73A8C9UL,
	};
	// llvm-libc atan_utils.h ATAN_I: atan(i/64) as {hi, lo}
	static readonly ulong[] AtanIBits = {
		0x0000000000000000UL, 0x0000000000000000UL, 0x3F8FFF555BBB729BUL, 0xBC2220C39D4DFF50UL,
		0x3F9FFD55BBA97625UL, 0xBC35EC431444912CUL, 0x3FA7FB818430DA2AUL, 0xBC086EF8F794F105UL,
		0x3FAFF55BB72CFDEAUL, 0xBC3C934D86D23F1DUL, 0x3FB3F59F0E7C559DUL, 0x3C5AC4CE285DF847UL,
		0x3FB7EE182602F10FUL, 0xBC5CFB654C0C3D98UL, 0x3FBBE39EBE6F07C3UL, 0x3C5F7B8F29A05987UL,
		0x3FBFD5BA9AAC2F6EUL, 0xBC4CD37686760C17UL, 0x3FC1E1FAFB043727UL, 0xBC4B485914DACF8CUL,
		0x3FC3D6EEE8C6626CUL, 0x3C661A3B0CE9281BUL, 0x3FC5C9811E3EC26AUL, 0xBC5054AB2C010F3DUL,
		0x3FC7B97B4BCE5B02UL, 0x3C5347B0B4F881CAUL, 0x3FC9A6A8E96C8626UL, 0x3C4CF601E7B4348EUL,
		0x3FCB90D7529260A2UL, 0x3C217B10D2E0E5ABUL, 0x3FCD77D5DF205736UL, 0x3C6C648D1534597EUL,
		0x3FCF5B75F92C80DDUL, 0x3C68AB6E3CF7AFBDUL, 0x3FD09DC597D86362UL, 0x3C762E47390CB865UL,
		0x3FD18BF5A30BF178UL, 0x3C630CA4748B1BF9UL, 0x3FD278372057EF46UL, 0xBC7077CDD36DFC81UL,
		0x3FD362773707EBCCUL, 0xBC6963A544B672D8UL, 0x3FD44AA436C2AF0AUL, 0xBC75D5E43C55B3BAUL,
		0x3FD530AD9951CD4AUL, 0xBC62566480884082UL, 0x3FD614840309CFE2UL, 0xBC7A725715711F00UL,
		0x3FD6F61941E4DEF1UL, 0xBC7C63AAE6F6E918UL, 0x3FD7D5604B63B3F7UL, 0x3C769C885C2B249AUL,
		0x3FD8B24D394A1B25UL, 0x3C7B6D0BA3748FA8UL, 0x3FD98CD5454D6B18UL, 0x3C79E6C988FD0A77UL,
		0x3FDA64EEC3CC23FDUL, 0xBC724DEC1B50B7FFUL, 0x3FDB3A911DA65C6CUL, 0x3C7AE187B1CA5040UL,
		0x3FDC0DB4C94EC9F0UL, 0xBC7CC1CE70934C34UL, 0x3FDCDE53432C1351UL, 0xBC7A2CFA4418F1ADUL,
		0x3FDDAC670561BB4FUL, 0x3C7A2B7F222F65E2UL, 0x3FDE77EB7F175A34UL, 0x3C70E53DC1BF3435UL,
		0x3FDF40DD0B541418UL, 0xBC6A3992DC382A23UL, 0x3FE0039C73C1A40CUL, 0xBC8B32C949C9D593UL,
		0x3FE0657E94DB30D0UL, 0xBC7D5B495F6349E6UL, 0x3FE0C6145B5B43DAUL, 0x3C5974FA13B5404FUL,
		0x3FE1255D9BFBD2A9UL, 0xBC52BDAEE1C0EE35UL, 0x3FE1835A88BE7C13UL, 0x3C8C621CEC00C301UL,
		0x3FE1E00BABDEFEB4UL, 0xBC5928DF287A668FUL, 0x3FE23B71E2CC9E6AUL, 0x3C6C421C9F38224EUL,
		0x3FE2958E59308E31UL, 0xBC709E73B0C6C087UL, 0x3FE2EE628406CBCAUL, 0x3C8C5D5E9FF0CF8DUL,
		0x3FE345F01CCE37BBUL, 0x3C81021137C71102UL, 0x3FE39C391CD4171AUL, 0xBC82304331D8BF46UL,
		0x3FE3F13FB89E96F4UL, 0x3C7ECF8B492644F0UL, 0x3FE445065B795B56UL, 0xBC7F76D0163F79C8UL,
		0x3FE4978FA3269EE1UL, 0x3C72419A87F2A458UL, 0x3FE4E8DE5BB6EC04UL, 0x3C84A33DBEB3796CUL,
		0x3FE538F57B89061FUL, 0xBC81BB74ABDA520CUL, 0x3FE587D81F732FBBUL, 0xBC75E5C9D8C5A950UL,
		0x3FE5D58987169B18UL, 0x3C60028E4BC5E7CAUL, 0x3FE6220D115D7B8EUL, 0xBC62B785350EE8C1UL,
		0x3FE66D663923E087UL, 0xBC76EA6FEBE8BBBAUL, 0x3FE6B798920B3D99UL, 0xBC8A80386188C50EUL,
		0x3FE700A7C5784634UL, 0xBC78C34D25AADEF6UL, 0x3FE748978FBA8E0FUL, 0x3C47B2A6165884A1UL,
		0x3FE78F6BBD5D315EUL, 0x3C8406A089803740UL, 0x3FE7D528289FA093UL, 0x3C8560821E2F3AA9UL,
		0x3FE819D0B7158A4DUL, 0xBC7BF76229D3B917UL, 0x3FE85D69576CC2C5UL, 0x3C66B66E7FC8B8C3UL,
		0x3FE89FF5FF57F1F8UL, 0xBC855B9A5E177A1BUL, 0x3FE8E17AA99CC05EUL, 0xBC7EC182AB042F61UL,
		0x3FE921FB54442D18UL, 0x3C81A62633145C07UL,
	};
	// glibc 2.39 pow tables (sysdeps/ieee754/dbl-64/e_pow_log_data.c {invc, logc, logctail}, e_exp_data.c tab)
	const double GInvLn2N = 184.6649652337873; // 0x1.71547652b82fep+7
	const double GNegLn2hiN = -0.005415212348111709; // -0x1.62e42fefa0000p-8
	const double GNegLn2loN = -1.2864023111638346e-14; // -0x1.cf79abc9e3b3ap-47
	const double GShift = 6755399441055744.0; // 0x1.8000000000000p+52
	const double GLn2hi = 0.6931471805598903; // 0x1.62e42fefa3800p-1
	const double GLn2lo = 5.497923018708371e-14; // 0x1.ef35793c76730p-45
	const double GA0 = -0.5; // -0x1.0000000000000p-1
	const double GA1 = -0.6666666666666679; // -0x1.5555555555560p-1
	const double GA2 = 0.5000000000000007; // 0x1.0000000000006p-1
	const double GA3 = 0.7999999995323976; // 0x1.999999959554ep-1
	const double GA4 = -0.6666666663487739; // -0x1.555555529a47ap-1
	const double GA5 = -1.142909628459501; // -0x1.2495b9b4845e9p+0
	const double GA6 = 1.0000415263675542; // 0x1.0002b8b263fc3p+0
	const double GC2 = 0.49999999999996786; // 0x1.ffffffffffdbdp-2
	const double GC3 = 0.16666666666665886; // 0x1.555555555543cp-3
	const double GC4 = 0.0416666808410674; // 0x1.55555cf172b91p-5
	const double GC5 = 0.008333335853059549; // 0x1.1111167a4d017p-7
	static readonly ulong[] GPowLogBits = {
		0x3FF6A00000000000UL, 0xBFD62C82F2B9C800UL, 0x3CFAB42428375680UL,
		0x3FF6800000000000UL, 0xBFD5D1BDBF580800UL, 0xBD1CA508D8E0F720UL,
		0x3FF6600000000000UL, 0xBFD5767717455800UL, 0xBD2362A4D5B6506DUL,
		0x3FF6400000000000UL, 0xBFD51AAD872DF800UL, 0xBCE684E49EB067D5UL,
		0x3FF6200000000000UL, 0xBFD4BE5F95777800UL, 0xBD041B6993293EE0UL,
		0x3FF6000000000000UL, 0xBFD4618BC21C6000UL, 0x3D13D82F484C84CCUL,
		0x3FF5E00000000000UL, 0xBFD404308686A800UL, 0x3CDC42F3ED820B3AUL,
		0x3FF5C00000000000UL, 0xBFD3A64C55694800UL, 0x3D20B1C686519460UL,
		0x3FF5A00000000000UL, 0xBFD347DD9A988000UL, 0x3D25594DD4C58092UL,
		0x3FF5800000000000UL, 0xBFD2E8E2BAE12000UL, 0x3D267B1E99B72BD8UL,
		0x3FF5600000000000UL, 0xBFD2895A13DE8800UL, 0x3D15CA14B6CFB03FUL,
		0x3FF5600000000000UL, 0xBFD2895A13DE8800UL, 0x3D15CA14B6CFB03FUL,
		0x3FF5400000000000UL, 0xBFD22941FBCF7800UL, 0xBD165A242853DA76UL,
		0x3FF5200000000000UL, 0xBFD1C898C1699800UL, 0xBD1FAFBC68E75404UL,
		0x3FF5000000000000UL, 0xBFD1675CABABA800UL, 0x3D1F1FC63382A8F0UL,
		0x3FF4E00000000000UL, 0xBFD1058BF9AE4800UL, 0xBD26A8C4FD055A66UL,
		0x3FF4C00000000000UL, 0xBFD0A324E2739000UL, 0xBD0C6BEE7EF4030EUL,
		0x3FF4A00000000000UL, 0xBFD0402594B4D000UL, 0xBCF036B89EF42D7FUL,
		0x3FF4A00000000000UL, 0xBFD0402594B4D000UL, 0xBCF036B89EF42D7FUL,
		0x3FF4800000000000UL, 0xBFCFB9186D5E4000UL, 0x3D0D572AAB993C87UL,
		0x3FF4600000000000UL, 0xBFCEF0ADCBDC6000UL, 0x3D2B26B79C86AF24UL,
		0x3FF4400000000000UL, 0xBFCE27076E2AF000UL, 0xBD172F4F543FFF10UL,
		0x3FF4200000000000UL, 0xBFCD5C216B4FC000UL, 0x3D21BA91BBCA681BUL,
		0x3FF4000000000000UL, 0xBFCC8FF7C79AA000UL, 0x3D27794F689F8434UL,
		0x3FF4000000000000UL, 0xBFCC8FF7C79AA000UL, 0x3D27794F689F8434UL,
		0x3FF3E00000000000UL, 0xBFCBC286742D9000UL, 0x3D194EB0318BB78FUL,
		0x3FF3C00000000000UL, 0xBFCAF3C94E80C000UL, 0x3CBA4E633FCD9066UL,
		0x3FF3A00000000000UL, 0xBFCA23BC1FE2B000UL, 0xBD258C64DC46C1EAUL,
		0x3FF3A00000000000UL, 0xBFCA23BC1FE2B000UL, 0xBD258C64DC46C1EAUL,
		0x3FF3800000000000UL, 0xBFC9525A9CF45000UL, 0xBD2AD1D904C1D4E3UL,
		0x3FF3600000000000UL, 0xBFC87FA06520D000UL, 0x3D2BBDBF7FDBFA09UL,
		0x3FF3400000000000UL, 0xBFC7AB890210E000UL, 0x3D2BDB9072534A58UL,
		0x3FF3400000000000UL, 0xBFC7AB890210E000UL, 0x3D2BDB9072534A58UL,
		0x3FF3200000000000UL, 0xBFC6D60FE719D000UL, 0xBD10E46AA3B2E266UL,
		0x3FF3000000000000UL, 0xBFC5FF3070A79000UL, 0xBD1E9E439F105039UL,
		0x3FF3000000000000UL, 0xBFC5FF3070A79000UL, 0xBD1E9E439F105039UL,
		0x3FF2E00000000000UL, 0xBFC526E5E3A1B000UL, 0xBD20DE8B90075B8FUL,
		0x3FF2C00000000000UL, 0xBFC44D2B6CCB8000UL, 0x3D170CC16135783CUL,
		0x3FF2C00000000000UL, 0xBFC44D2B6CCB8000UL, 0x3D170CC16135783CUL,
		0x3FF2A00000000000UL, 0xBFC371FC201E9000UL, 0x3CF178864D27543AUL,
		0x3FF2800000000000UL, 0xBFC29552F81FF000UL, 0xBD248D301771C408UL,
		0x3FF2600000000000UL, 0xBFC1B72AD52F6000UL, 0xBD2E80A41811A396UL,
		0x3FF2600000000000UL, 0xBFC1B72AD52F6000UL, 0xBD2E80A41811A396UL,
		0x3FF2400000000000UL, 0xBFC0D77E7CD09000UL, 0x3D0A699688E85BF4UL,
		0x3FF2400000000000UL, 0xBFC0D77E7CD09000UL, 0x3D0A699688E85BF4UL,
		0x3FF2200000000000UL, 0xBFBFEC9131DBE000UL, 0xBD2575545CA333F2UL,
		0x3FF2000000000000UL, 0xBFBE27076E2B0000UL, 0x3D2A342C2AF0003CUL,
		0x3FF2000000000000UL, 0xBFBE27076E2B0000UL, 0x3D2A342C2AF0003CUL,
		0x3FF1E00000000000UL, 0xBFBC5E548F5BC000UL, 0xBD1D0C57585FBE06UL,
		0x3FF1C00000000000UL, 0xBFBA926D3A4AE000UL, 0x3D253935E85BAAC8UL,
		0x3FF1C00000000000UL, 0xBFBA926D3A4AE000UL, 0x3D253935E85BAAC8UL,
		0x3FF1A00000000000UL, 0xBFB8C345D631A000UL, 0x3D137C294D2F5668UL,
		0x3FF1A00000000000UL, 0xBFB8C345D631A000UL, 0x3D137C294D2F5668UL,
		0x3FF1800000000000UL, 0xBFB6F0D28AE56000UL, 0xBD269737C93373DAUL,
		0x3FF1600000000000UL, 0xBFB51B073F062000UL, 0x3D1F025B61C65E57UL,
		0x3FF1600000000000UL, 0xBFB51B073F062000UL, 0x3D1F025B61C65E57UL,
		0x3FF1400000000000UL, 0xBFB341D7961BE000UL, 0x3D2C5EDACCF913DFUL,
		0x3FF1400000000000UL, 0xBFB341D7961BE000UL, 0x3D2C5EDACCF913DFUL,
		0x3FF1200000000000UL, 0xBFB16536EEA38000UL, 0x3D147C5E768FA309UL,
		0x3FF1000000000000UL, 0xBFAF0A30C0118000UL, 0x3D2D599E83368E91UL,
		0x3FF1000000000000UL, 0xBFAF0A30C0118000UL, 0x3D2D599E83368E91UL,
		0x3FF0E00000000000UL, 0xBFAB42DD71198000UL, 0x3D1C827AE5D6704CUL,
		0x3FF0E00000000000UL, 0xBFAB42DD71198000UL, 0x3D1C827AE5D6704CUL,
		0x3FF0C00000000000UL, 0xBFA77458F632C000UL, 0xBD2CFC4634F2A1EEUL,
		0x3FF0C00000000000UL, 0xBFA77458F632C000UL, 0xBD2CFC4634F2A1EEUL,
		0x3FF0A00000000000UL, 0xBFA39E87B9FEC000UL, 0x3CF502B7F526FEAAUL,
		0x3FF0A00000000000UL, 0xBFA39E87B9FEC000UL, 0x3CF502B7F526FEAAUL,
		0x3FF0800000000000UL, 0xBF9F829B0E780000UL, 0xBD2980267C7E09E4UL,
		0x3FF0800000000000UL, 0xBF9F829B0E780000UL, 0xBD2980267C7E09E4UL,
		0x3FF0600000000000UL, 0xBF97B91B07D58000UL, 0xBD288D5493FAA639UL,
		0x3FF0400000000000UL, 0xBF8FC0A8B0FC0000UL, 0xBCDF1E7CF6D3A69CUL,
		0x3FF0400000000000UL, 0xBF8FC0A8B0FC0000UL, 0xBCDF1E7CF6D3A69CUL,
		0x3FF0200000000000UL, 0xBF7FE02A6B100000UL, 0xBD19E23F0DDA40E4UL,
		0x3FF0200000000000UL, 0xBF7FE02A6B100000UL, 0xBD19E23F0DDA40E4UL,
		0x3FF0000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL,
		0x3FF0000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL,
		0x3FEFC00000000000UL, 0x3F80101575890000UL, 0xBD10C76B999D2BE8UL,
		0x3FEF800000000000UL, 0x3F90205658938000UL, 0xBD23DC5B06E2F7D2UL,
		0x3FEF400000000000UL, 0x3F98492528C90000UL, 0xBD2AA0BA325A0C34UL,
		0x3FEF000000000000UL, 0x3FA0415D89E74000UL, 0x3D0111C05CF1D753UL,
		0x3FEEC00000000000UL, 0x3FA466AED42E0000UL, 0xBD2C167375BDFD28UL,
		0x3FEE800000000000UL, 0x3FA894AA149FC000UL, 0xBD197995D05A267DUL,
		0x3FEE400000000000UL, 0x3FACCB73CDDDC000UL, 0xBD1A68F247D82807UL,
		0x3FEE200000000000UL, 0x3FAEEA31C006C000UL, 0xBD0E113E4FC93B7BUL,
		0x3FEDE00000000000UL, 0x3FB1973BD1466000UL, 0xBD25325D560D9E9BUL,
		0x3FEDA00000000000UL, 0x3FB3BDF5A7D1E000UL, 0x3D2CC85EA5DB4ED7UL,
		0x3FED600000000000UL, 0x3FB5E95A4D97A000UL, 0xBD2C69063C5D1D1EUL,
		0x3FED400000000000UL, 0x3FB700D30AEAC000UL, 0x3CEC1E8DA99DED32UL,
		0x3FED000000000000UL, 0x3FB9335E5D594000UL, 0x3D23115C3ABD47DAUL,
		0x3FECC00000000000UL, 0x3FBB6AC88DAD6000UL, 0xBD1390802BF768E5UL,
		0x3FECA00000000000UL, 0x3FBC885801BC4000UL, 0x3D2646D1C65AACD3UL,
		0x3FEC600000000000UL, 0x3FBEC739830A2000UL, 0xBD2DC068AFE645E0UL,
		0x3FEC400000000000UL, 0x3FBFE89139DBE000UL, 0xBD2534D64FA10AFDUL,
		0x3FEC000000000000UL, 0x3FC1178E8227E000UL, 0x3D21EF78CE2D07F2UL,
		0x3FEBE00000000000UL, 0x3FC1AA2B7E23F000UL, 0x3D2CA78E44389934UL,
		0x3FEBA00000000000UL, 0x3FC2D1610C868000UL, 0x3D039D6CCB81B4A1UL,
		0x3FEB800000000000UL, 0x3FC365FCB0159000UL, 0x3CC62FA8234B7289UL,
		0x3FEB400000000000UL, 0x3FC4913D8333B000UL, 0x3D25837954FDB678UL,
		0x3FEB200000000000UL, 0x3FC527E5E4A1B000UL, 0x3D2633E8E5697DC7UL,
		0x3FEAE00000000000UL, 0x3FC6574EBE8C1000UL, 0x3D19CF8B2C3C2E78UL,
		0x3FEAC00000000000UL, 0x3FC6F0128B757000UL, 0xBD25118DE59C21E1UL,
		0x3FEAA00000000000UL, 0x3FC7898D85445000UL, 0xBD1C661070914305UL,
		0x3FEA600000000000UL, 0x3FC8BEAFEB390000UL, 0xBD073D54AAE92CD1UL,
		0x3FEA400000000000UL, 0x3FC95A5ADCF70000UL, 0x3D07F22858A0FF6FUL,
		0x3FEA000000000000UL, 0x3FCA93ED3C8AE000UL, 0xBD28724350562169UL,
		0x3FE9E00000000000UL, 0x3FCB31D8575BD000UL, 0xBD0C358D4EACE1AAUL,
		0x3FE9C00000000000UL, 0x3FCBD087383BE000UL, 0xBD2D4BC4595412B6UL,
		0x3FE9A00000000000UL, 0x3FCC6FFBC6F01000UL, 0xBCF1EC72C5962BD2UL,
		0x3FE9600000000000UL, 0x3FCDB13DB0D49000UL, 0xBD2AFF2AF715B035UL,
		0x3FE9400000000000UL, 0x3FCE530EFFE71000UL, 0x3CC212276041F430UL,
		0x3FE9200000000000UL, 0x3FCEF5ADE4DD0000UL, 0xBCCA211565BB8E11UL,
		0x3FE9000000000000UL, 0x3FCF991C6CB3B000UL, 0x3D1BCBECCA0CDF30UL,
		0x3FE8C00000000000UL, 0x3FD07138604D5800UL, 0x3CF89CDB16ED4E91UL,
		0x3FE8A00000000000UL, 0x3FD0C42D67616000UL, 0x3D27188B163CEAE9UL,
		0x3FE8800000000000UL, 0x3FD1178E8227E800UL, 0xBD2C210E63A5F01CUL,
		0x3FE8600000000000UL, 0x3FD16B5CCBACF800UL, 0x3D2B9ACDF7A51681UL,
		0x3FE8400000000000UL, 0x3FD1BF99635A6800UL, 0x3D2CA6ED5147BDB7UL,
		0x3FE8200000000000UL, 0x3FD214456D0EB800UL, 0x3D0A87DEBA46BAEAUL,
		0x3FE7E00000000000UL, 0x3FD2BEF07CDC9000UL, 0x3D2A9CFA4A5004F4UL,
		0x3FE7C00000000000UL, 0x3FD314F1E1D36000UL, 0xBD28E27AD3213CB8UL,
		0x3FE7A00000000000UL, 0x3FD36B6776BE1000UL, 0x3D116ECDB0F177C8UL,
		0x3FE7800000000000UL, 0x3FD3C25277333000UL, 0x3D183B54B606BD5CUL,
		0x3FE7600000000000UL, 0x3FD419B423D5E800UL, 0x3D08E436EC90E09DUL,
		0x3FE7400000000000UL, 0x3FD4718DC271C800UL, 0xBD2F27CE0967D675UL,
		0x3FE7200000000000UL, 0x3FD4C9E09E173000UL, 0xBD2E20891B0AD8A4UL,
		0x3FE7000000000000UL, 0x3FD522AE0738A000UL, 0x3D2EBE708164C759UL,
		0x3FE6E00000000000UL, 0x3FD57BF753C8D000UL, 0x3D1FADEDEE5D40EFUL,
		0x3FE6C00000000000UL, 0x3FD5D5BDDF596000UL, 0xBD0A0B2A08A465DCUL,
	};
	static readonly ulong[] GExpTab = {
		0x0000000000000000UL, 0x3FF0000000000000UL, 0x3C9B3B4F1A88BF6EUL, 0x3FEFF63DA9FB3335UL,
		0xBC7160139CD8DC5DUL, 0x3FEFEC9A3E778061UL, 0xBC905E7A108766D1UL, 0x3FEFE315E86E7F85UL,
		0x3C8CD2523567F613UL, 0x3FEFD9B0D3158574UL, 0xBC8BCE8023F98EFAUL, 0x3FEFD06B29DDF6DEUL,
		0x3C60F74E61E6C861UL, 0x3FEFC74518759BC8UL, 0x3C90A3E45B33D399UL, 0x3FEFBE3ECAC6F383UL,
		0x3C979AA65D837B6DUL, 0x3FEFB5586CF9890FUL, 0x3C8EB51A92FDEFFCUL, 0x3FEFAC922B7247F7UL,
		0x3C3EBE3D702F9CD1UL, 0x3FEFA3EC32D3D1A2UL, 0xBC6A033489906E0BUL, 0x3FEF9B66AFFED31BUL,
		0xBC9556522A2FBD0EUL, 0x3FEF9301D0125B51UL, 0xBC5080EF8C4EEA55UL, 0x3FEF8ABDC06C31CCUL,
		0xBC91C923B9D5F416UL, 0x3FEF829AAEA92DE0UL, 0x3C80D3E3E95C55AFUL, 0x3FEF7A98C8A58E51UL,
		0xBC801B15EAA59348UL, 0x3FEF72B83C7D517BUL, 0xBC8F1FF055DE323DUL, 0x3FEF6AF9388C8DEAUL,
		0x3C8B898C3F1353BFUL, 0x3FEF635BEB6FCB75UL, 0xBC96D99C7611EB26UL, 0x3FEF5BE084045CD4UL,
		0x3C9AECF73E3A2F60UL, 0x3FEF54873168B9AAUL, 0xBC8FE782CB86389DUL, 0x3FEF4D5022FCD91DUL,
		0x3C8A6F4144A6C38DUL, 0x3FEF463B88628CD6UL, 0x3C807A05B0E4047DUL, 0x3FEF3F49917DDC96UL,
		0x3C968EFDE3A8A894UL, 0x3FEF387A6E756238UL, 0x3C875E18F274487DUL, 0x3FEF31CE4FB2A63FUL,
		0x3C80472B981FE7F2UL, 0x3FEF2B4565E27CDDUL, 0xBC96B87B3F71085EUL, 0x3FEF24DFE1F56381UL,
		0x3C82F7E16D09AB31UL, 0x3FEF1E9DF51FDEE1UL, 0xBC3D219B1A6FBFFAUL, 0x3FEF187FD0DAD990UL,
		0x3C8B3782720C0AB4UL, 0x3FEF1285A6E4030BUL, 0x3C6E149289CECB8FUL, 0x3FEF0CAFA93E2F56UL,
		0x3C834D754DB0ABB6UL, 0x3FEF06FE0A31B715UL, 0x3C864201E2AC744CUL, 0x3FEF0170FC4CD831UL,
		0x3C8FDD395DD3F84AUL, 0x3FEEFC08B26416FFUL, 0xBC86A3803B8E5B04UL, 0x3FEEF6C55F929FF1UL,
		0xBC924AEDCC4B5068UL, 0x3FEEF1A7373AA9CBUL, 0xBC9907F81B512D8EUL, 0x3FEEECAE6D05D866UL,
		0xBC71D1E83E9436D2UL, 0x3FEEE7DB34E59FF7UL, 0xBC991919B3CE1B15UL, 0x3FEEE32DC313A8E5UL,
		0x3C859F48A72A4C6DUL, 0x3FEEDEA64C123422UL, 0xBC9312607A28698AUL, 0x3FEEDA4504AC801CUL,
		0xBC58A78F4817895BUL, 0x3FEED60A21F72E2AUL, 0xBC7C2C9B67499A1BUL, 0x3FEED1F5D950A897UL,
		0x3C4363ED60C2AC11UL, 0x3FEECE086061892DUL, 0x3C9666093B0664EFUL, 0x3FEECA41ED1D0057UL,
		0x3C6ECCE1DAA10379UL, 0x3FEEC6A2B5C13CD0UL, 0x3C93FF8E3F0F1230UL, 0x3FEEC32AF0D7D3DEUL,
		0x3C7690CEBB7AAFB0UL, 0x3FEEBFDAD5362A27UL, 0x3C931DBDEB54E077UL, 0x3FEEBCB299FDDD0DUL,
		0xBC8F94340071A38EUL, 0x3FEEB9B2769D2CA7UL, 0xBC87DECCDC93A349UL, 0x3FEEB6DAA2CF6642UL,
		0xBC78DEC6BD0F385FUL, 0x3FEEB42B569D4F82UL, 0xBC861246EC7B5CF6UL, 0x3FEEB1A4CA5D920FUL,
		0x3C93350518FDD78EUL, 0x3FEEAF4736B527DAUL, 0x3C7B98B72F8A9B05UL, 0x3FEEAD12D497C7FDUL,
		0x3C9063E1E21C5409UL, 0x3FEEAB07DD485429UL, 0x3C34C7855019C6EAUL, 0x3FEEA9268A5946B7UL,
		0x3C9432E62B64C035UL, 0x3FEEA76F15AD2148UL, 0xBC8CE44A6199769FUL, 0x3FEEA5E1B976DC09UL,
		0xBC8C33C53BEF4DA8UL, 0x3FEEA47EB03A5585UL, 0xBC845378892BE9AEUL, 0x3FEEA34634CCC320UL,
		0xBC93CEDD78565858UL, 0x3FEEA23882552225UL, 0x3C5710AA807E1964UL, 0x3FEEA155D44CA973UL,
		0xBC93B3EFBF5E2228UL, 0x3FEEA09E667F3BCDUL, 0xBC6A12AD8734B982UL, 0x3FEEA012750BDABFUL,
		0xBC6367EFB86DA9EEUL, 0x3FEE9FB23C651A2FUL, 0xBC80DC3D54E08851UL, 0x3FEE9F7DF9519484UL,
		0xBC781F647E5A3ECFUL, 0x3FEE9F75E8EC5F74UL, 0xBC86EE4AC08B7DB0UL, 0x3FEE9F9A48A58174UL,
		0xBC8619321E55E68AUL, 0x3FEE9FEB564267C9UL, 0x3C909CCB5E09D4D3UL, 0x3FEEA0694FDE5D3FUL,
		0xBC7B32DCB94DA51DUL, 0x3FEEA11473EB0187UL, 0x3C94ECFD5467C06BUL, 0x3FEEA1ED0130C132UL,
		0x3C65EBE1ABD66C55UL, 0x3FEEA2F336CF4E62UL, 0xBC88A1C52FB3CF42UL, 0x3FEEA427543E1A12UL,
		0xBC9369B6F13B3734UL, 0x3FEEA589994CCE13UL, 0xBC805E843A19FF1EUL, 0x3FEEA71A4623C7ADUL,
		0xBC94D450D872576EUL, 0x3FEEA8D99B4492EDUL, 0x3C90AD675B0E8A00UL, 0x3FEEAAC7D98A6699UL,
		0x3C8DB72FC1F0EAB4UL, 0x3FEEACE5422AA0DBUL, 0xBC65B6609CC5E7FFUL, 0x3FEEAF3216B5448CUL,
		0x3C7BF68359F35F44UL, 0x3FEEB1AE99157736UL, 0xBC93091FA71E3D83UL, 0x3FEEB45B0B91FFC6UL,
		0xBC5DA9B88B6C1E29UL, 0x3FEEB737B0CDC5E5UL, 0xBC6C23F97C90B959UL, 0x3FEEBA44CBC8520FUL,
		0xBC92434322F4F9AAUL, 0x3FEEBD829FDE4E50UL, 0xBC85CA6CD7668E4BUL, 0x3FEEC0F170CA07BAUL,
		0x3C71AFFC2B91CE27UL, 0x3FEEC49182A3F090UL, 0x3C6DD235E10A73BBUL, 0x3FEEC86319E32323UL,
		0xBC87C50422622263UL, 0x3FEECC667B5DE565UL, 0x3C8B1C86E3E231D5UL, 0x3FEED09BEC4A2D33UL,
		0xBC91BBD1D3BCBB15UL, 0x3FEED503B23E255DUL, 0x3C90CC319CEE31D2UL, 0x3FEED99E1330B358UL,
		0x3C8469846E735AB3UL, 0x3FEEDE6B5579FDBFUL, 0xBC82DFCD978E9DB4UL, 0x3FEEE36BBFD3F37AUL,
		0x3C8C1A7792CB3387UL, 0x3FEEE89F995AD3ADUL, 0xBC907B8F4AD1D9FAUL, 0x3FEEEE07298DB666UL,
		0xBC55C3D956DCAEBAUL, 0x3FEEF3A2B84F15FBUL, 0xBC90A40E3DA6F640UL, 0x3FEEF9728DE5593AUL,
		0xBC68D6F438AD9334UL, 0x3FEEFF76F2FB5E47UL, 0xBC91EEE26B588A35UL, 0x3FEF05B030A1064AUL,
		0x3C74FFD70A5FDDCDUL, 0x3FEF0C1E904BC1D2UL, 0xBC91BDFBFA9298ACUL, 0x3FEF12C25BD71E09UL,
		0x3C736EAE30AF0CB3UL, 0x3FEF199BDD85529CUL, 0x3C8EE3325C9FFD94UL, 0x3FEF20AB5FFFD07AUL,
		0x3C84E08FD10959ACUL, 0x3FEF27F12E57D14BUL, 0x3C63CDAF384E1A67UL, 0x3FEF2F6D9406E7B5UL,
		0x3C676B2C6C921968UL, 0x3FEF3720DCEF9069UL, 0xBC808A1883CCB5D2UL, 0x3FEF3F0B555DC3FAUL,
		0xBC8FAD5D3FFFFA6FUL, 0x3FEF472D4A07897CUL, 0xBC900DAE3875A949UL, 0x3FEF4F87080D89F2UL,
		0x3C74A385A63D07A7UL, 0x3FEF5818DCFBA487UL, 0xBC82919E2040220FUL, 0x3FEF60E316C98398UL,
		0x3C8E5A50D5C192ACUL, 0x3FEF69E603DB3285UL, 0x3C843A59AC016B4BUL, 0x3FEF7321F301B460UL,
		0xBC82D52107B43E1FUL, 0x3FEF7C97337B9B5FUL, 0xBC892AB93B470DC9UL, 0x3FEF864614F5A129UL,
		0x3C74B604603A88D3UL, 0x3FEF902EE78B3FF6UL, 0x3C83C5EC519D7271UL, 0x3FEF9A51FBC74C83UL,
		0xBC8FF7128FD391F0UL, 0x3FEFA4AFA2A490DAUL, 0xBC8DAE98E223747DUL, 0x3FEFAF482D8E67F1UL,
		0x3C8EC3BC41AA2008UL, 0x3FEFBA1BEE615A27UL, 0x3C842B94C3A9EB32UL, 0x3FEFC52B376BBA97UL,
		0x3C8A64A931D185EEUL, 0x3FEFD0765B6E4540UL, 0xBC8E37BAE43BE3EDUL, 0x3FEFDBFDAD9CBE14UL,
		0x3C77893B4D91CD9DUL, 0x3FEFE7C1819E90D8UL, 0x3C5305C14160CC89UL, 0x3FEFF3C22B8F71F1UL,
	};
	// llvm-libc tables (verbatim bit patterns)
	// SIN_K_PI_OVER_128: {lo, hi} x 256
	static readonly ulong[] LSinKBits = {
		0x0000000000000000UL, 0x0000000000000000UL, 0x3C5F938A73DB97FBUL, 0x3F992155F7A3667CUL,
		0xBC2912BD0D569A90UL, 0x3FA91F65F10DD814UL, 0x3C7CCBEEEAE8129AUL, 0x3FB2D52092CE19F4UL,
		0xBC3E2718D26ED688UL, 0x3FB917A6BC29B42CUL, 0xBC7CBB1F71ACA352UL, 0x3FBF564E56A97310UL,
		0xBC8DD9FFEAECBDC4UL, 0x3FC2C8106E8E613CUL, 0xBC8AB3802218894FUL, 0x3FC5E214448B3FC8UL,
		0xBC849B466E7FE360UL, 0x3FC8F8B83C69A60CUL, 0xBC8035E2873CA432UL, 0x3FCC0B826A7E4F64UL,
		0xBC850B7BBC4768B1UL, 0x3FCF19F97B215F1CUL, 0xBC83ED9EFAA42AB3UL, 0x3FD111D262B1F678UL,
		0x3C9A8B5C974EE7B5UL, 0x3FD294062ED59F04UL, 0x3C94325F12BE8946UL, 0x3FD4135C94176600UL,
		0x3C8FC2047E54E614UL, 0x3FD58F9A75AB1FDCUL, 0xBC9512C678219317UL, 0x3FD7088530FA45A0UL,
		0xBC92E59DBA7AB4C2UL, 0x3FD87DE2A6AEA964UL, 0xBC9D24AFDADE848BUL, 0x3FD9EF7943A8ED8CUL,
		0x3C65B362CB974183UL, 0x3FDB5D1009E15CC0UL, 0xBC9E97AF1A63C807UL, 0x3FDCC66E9931C460UL,
		0xBC8C3E4EDC5872F8UL, 0x3FDE2B5D3806F63CUL, 0x3C9FB44F80F92225UL, 0x3FDF8BA4DBF89AB8UL,
		0x3CA9697FAF2E2FE5UL, 0x3FE073879922FFECUL, 0xBCA7BC8EDA6AF93CUL, 0x3FE11EB3541B4B24UL,
		0x3C8B25DD267F6600UL, 0x3FE1C73B39AE68C8UL, 0xBCA5769D0FBCDDC3UL, 0x3FE26D054CDD12E0UL,
		0x3C9C20673B2116B2UL, 0x3FE30FF7FCE17034UL, 0x3CA3C7C4BC72A92CUL, 0x3FE3AFFA292050B8UL,
		0xBCAE7F895D302395UL, 0x3FE44CF325091DD8UL, 0x3CA13C293EDCEB32UL, 0x3FE4E6CABBE3E5E8UL,
		0xBC875720992BFBB2UL, 0x3FE57D69348CECA0UL, 0xBCA24A366A5FE547UL, 0x3FE610B7551D2CE0UL,
		0x3C921165F626CDD5UL, 0x3FE6A09E667F3BCCUL, 0xBCABCAC43C389BA9UL, 0x3FE72D0837EFFF98UL,
		0xBCA21EA6F59BE15BUL, 0x3FE7B5DF226AAFB0UL, 0x3CAD217BE0E2B971UL, 0x3FE83B0E0BFF976CUL,
		0x3C969D0F6897664AUL, 0x3FE8BC806B151740UL, 0xBC9615F32B6F907AUL, 0x3FE93A22499263FCUL,
		0x3C96788EBCC76DC6UL, 0x3FE9B3E047F38740UL, 0x3CADDAE89FD441D1UL, 0x3FEA29A7A0462780UL,
		0xBC9F98273C5D2495UL, 0x3FEA9B66290EA1A4UL, 0xBC8926DA300FFCCEUL, 0x3FEB090A58150200UL,
		0x3CA90E58336C64A8UL, 0x3FEB728345196E3CUL, 0x3CA9F6963354E3FEUL, 0x3FEBD7C0AC6F9528UL,
		0x3C9A47D3A2A0DCBEUL, 0x3FEC38B2F180BDB0UL, 0x3C9ED0489E16B9A0UL, 0x3FEC954B213411F4UL,
		0xBCA0F3DB5DAD5AC5UL, 0x3FECED7AF43CC774UL, 0x3CAAC42B5A8B6943UL, 0x3FED4134D14DC938UL,
		0xBCAD75033DFB9CA8UL, 0x3FED906BCF328D48UL, 0x3C883C37C6107DB3UL, 0x3FEDDB13B6CCC23CUL,
		0x3C97F59C49F6CD6DUL, 0x3FEE212104F686E4UL, 0x3CAEE94A90D7B88BUL, 0x3FEE6288EC48E110UL,
		0xBCAA27D3874701F9UL, 0x3FEE9F4156C62DDCUL, 0xBC985F4E1B8298D0UL, 0x3FEED740E7684964UL,
		0xBC9AB4E148E52D9EUL, 0x3FEF0A7EFB9230D8UL, 0x3C98A11412B82346UL, 0x3FEF38F3AC64E588UL,
		0x3C7562172A361FD3UL, 0x3FEF6297CFF75CB0UL, 0x3CA3564ACEF1FF97UL, 0x3FEF8764FA714BA8UL,
		0xBCA5E82A3284D5C8UL, 0x3FEFA7557F08A518UL, 0xBC9709BCCB89A989UL, 0x3FEFC26470E19FD4UL,
		0x3CA9E082721DFB8EUL, 0x3FEFD88DA3D12524UL, 0xBCAEADE132F3981DUL, 0x3FEFE9CDAD01883CUL,
		0x3CAE3A843D1DB55FUL, 0x3FEFF621E3796D7CUL, 0x3C9765595D548D9AUL, 0x3FEFFD886084CD0CUL,
		0x0000000000000000UL, 0x3FF0000000000000UL, 0x3C9765595D548D9AUL, 0x3FEFFD886084CD0CUL,
		0x3CAE3A843D1DB55FUL, 0x3FEFF621E3796D7CUL, 0xBCAEADE132F3981DUL, 0x3FEFE9CDAD01883CUL,
		0x3CA9E082721DFB8EUL, 0x3FEFD88DA3D12524UL, 0xBC9709BCCB89A989UL, 0x3FEFC26470E19FD4UL,
		0xBCA5E82A3284D5C8UL, 0x3FEFA7557F08A518UL, 0x3CA3564ACEF1FF97UL, 0x3FEF8764FA714BA8UL,
		0x3C7562172A361FD3UL, 0x3FEF6297CFF75CB0UL, 0x3C98A11412B82346UL, 0x3FEF38F3AC64E588UL,
		0xBC9AB4E148E52D9EUL, 0x3FEF0A7EFB9230D8UL, 0xBC985F4E1B8298D0UL, 0x3FEED740E7684964UL,
		0xBCAA27D3874701F9UL, 0x3FEE9F4156C62DDCUL, 0x3CAEE94A90D7B88BUL, 0x3FEE6288EC48E110UL,
		0x3C97F59C49F6CD6DUL, 0x3FEE212104F686E4UL, 0x3C883C37C6107DB3UL, 0x3FEDDB13B6CCC23CUL,
		0xBCAD75033DFB9CA8UL, 0x3FED906BCF328D48UL, 0x3CAAC42B5A8B6943UL, 0x3FED4134D14DC938UL,
		0xBCA0F3DB5DAD5AC5UL, 0x3FECED7AF43CC774UL, 0x3C9ED0489E16B9A0UL, 0x3FEC954B213411F4UL,
		0x3C9A47D3A2A0DCBEUL, 0x3FEC38B2F180BDB0UL, 0x3CA9F6963354E3FEUL, 0x3FEBD7C0AC6F9528UL,
		0x3CA90E58336C64A8UL, 0x3FEB728345196E3CUL, 0xBC8926DA300FFCCEUL, 0x3FEB090A58150200UL,
		0xBC9F98273C5D2495UL, 0x3FEA9B66290EA1A4UL, 0x3CADDAE89FD441D1UL, 0x3FEA29A7A0462780UL,
		0x3C96788EBCC76DC6UL, 0x3FE9B3E047F38740UL, 0xBC9615F32B6F907AUL, 0x3FE93A22499263FCUL,
		0x3C969D0F6897664AUL, 0x3FE8BC806B151740UL, 0x3CAD217BE0E2B971UL, 0x3FE83B0E0BFF976CUL,
		0xBCA21EA6F59BE15BUL, 0x3FE7B5DF226AAFB0UL, 0xBCABCAC43C389BA9UL, 0x3FE72D0837EFFF98UL,
		0x3C921165F626CDD5UL, 0x3FE6A09E667F3BCCUL, 0xBCA24A366A5FE547UL, 0x3FE610B7551D2CE0UL,
		0xBC875720992BFBB2UL, 0x3FE57D69348CECA0UL, 0x3CA13C293EDCEB32UL, 0x3FE4E6CABBE3E5E8UL,
		0xBCAE7F895D302395UL, 0x3FE44CF325091DD8UL, 0x3CA3C7C4BC72A92CUL, 0x3FE3AFFA292050B8UL,
		0x3C9C20673B2116B2UL, 0x3FE30FF7FCE17034UL, 0xBCA5769D0FBCDDC3UL, 0x3FE26D054CDD12E0UL,
		0x3C8B25DD267F6600UL, 0x3FE1C73B39AE68C8UL, 0xBCA7BC8EDA6AF93CUL, 0x3FE11EB3541B4B24UL,
		0x3CA9697FAF2E2FE5UL, 0x3FE073879922FFECUL, 0x3C9FB44F80F92225UL, 0x3FDF8BA4DBF89AB8UL,
		0xBC8C3E4EDC5872F8UL, 0x3FDE2B5D3806F63CUL, 0xBC9E97AF1A63C807UL, 0x3FDCC66E9931C460UL,
		0x3C65B362CB974183UL, 0x3FDB5D1009E15CC0UL, 0xBC9D24AFDADE848BUL, 0x3FD9EF7943A8ED8CUL,
		0xBC92E59DBA7AB4C2UL, 0x3FD87DE2A6AEA964UL, 0xBC9512C678219317UL, 0x3FD7088530FA45A0UL,
		0x3C8FC2047E54E614UL, 0x3FD58F9A75AB1FDCUL, 0x3C94325F12BE8946UL, 0x3FD4135C94176600UL,
		0x3C9A8B5C974EE7B5UL, 0x3FD294062ED59F04UL, 0xBC83ED9EFAA42AB3UL, 0x3FD111D262B1F678UL,
		0xBC850B7BBC4768B1UL, 0x3FCF19F97B215F1CUL, 0xBC8035E2873CA432UL, 0x3FCC0B826A7E4F64UL,
		0xBC849B466E7FE360UL, 0x3FC8F8B83C69A60CUL, 0xBC8AB3802218894FUL, 0x3FC5E214448B3FC8UL,
		0xBC8DD9FFEAECBDC4UL, 0x3FC2C8106E8E613CUL, 0xBC7CBB1F71ACA352UL, 0x3FBF564E56A97310UL,
		0xBC3E2718D26ED688UL, 0x3FB917A6BC29B42CUL, 0x3C7CCBEEEAE8129AUL, 0x3FB2D52092CE19F4UL,
		0xBC2912BD0D569A90UL, 0x3FA91F65F10DD814UL, 0x3C5F938A73DB97FBUL, 0x3F992155F7A3667CUL,
		0x0000000000000000UL, 0x0000000000000000UL, 0xBC5F938A73DB97FBUL, 0xBF992155F7A3667CUL,
		0x3C2912BD0D569A90UL, 0xBFA91F65F10DD814UL, 0xBC7CCBEEEAE8129AUL, 0xBFB2D52092CE19F4UL,
		0x3C3E2718D26ED688UL, 0xBFB917A6BC29B42CUL, 0x3C7CBB1F71ACA352UL, 0xBFBF564E56A97310UL,
		0x3C8DD9FFEAECBDC4UL, 0xBFC2C8106E8E613CUL, 0x3C8AB3802218894FUL, 0xBFC5E214448B3FC8UL,
		0x3C849B466E7FE360UL, 0xBFC8F8B83C69A60CUL, 0x3C8035E2873CA432UL, 0xBFCC0B826A7E4F64UL,
		0x3C850B7BBC4768B1UL, 0xBFCF19F97B215F1CUL, 0x3C83ED9EFAA42AB3UL, 0xBFD111D262B1F678UL,
		0xBC9A8B5C974EE7B5UL, 0xBFD294062ED59F04UL, 0xBC94325F12BE8946UL, 0xBFD4135C94176600UL,
		0xBC8FC2047E54E614UL, 0xBFD58F9A75AB1FDCUL, 0x3C9512C678219317UL, 0xBFD7088530FA45A0UL,
		0x3C92E59DBA7AB4C2UL, 0xBFD87DE2A6AEA964UL, 0x3C9D24AFDADE848BUL, 0xBFD9EF7943A8ED8CUL,
		0xBC65B362CB974183UL, 0xBFDB5D1009E15CC0UL, 0x3C9E97AF1A63C807UL, 0xBFDCC66E9931C460UL,
		0x3C8C3E4EDC5872F8UL, 0xBFDE2B5D3806F63CUL, 0xBC9FB44F80F92225UL, 0xBFDF8BA4DBF89AB8UL,
		0xBCA9697FAF2E2FE5UL, 0xBFE073879922FFECUL, 0x3CA7BC8EDA6AF93CUL, 0xBFE11EB3541B4B24UL,
		0xBC8B25DD267F6600UL, 0xBFE1C73B39AE68C8UL, 0x3CA5769D0FBCDDC3UL, 0xBFE26D054CDD12E0UL,
		0xBC9C20673B2116B2UL, 0xBFE30FF7FCE17034UL, 0xBCA3C7C4BC72A92CUL, 0xBFE3AFFA292050B8UL,
		0x3CAE7F895D302395UL, 0xBFE44CF325091DD8UL, 0xBCA13C293EDCEB32UL, 0xBFE4E6CABBE3E5E8UL,
		0x3C875720992BFBB2UL, 0xBFE57D69348CECA0UL, 0x3CA24A366A5FE547UL, 0xBFE610B7551D2CE0UL,
		0xBC921165F626CDD5UL, 0xBFE6A09E667F3BCCUL, 0x3CABCAC43C389BA9UL, 0xBFE72D0837EFFF98UL,
		0x3CA21EA6F59BE15BUL, 0xBFE7B5DF226AAFB0UL, 0xBCAD217BE0E2B971UL, 0xBFE83B0E0BFF976CUL,
		0xBC969D0F6897664AUL, 0xBFE8BC806B151740UL, 0x3C9615F32B6F907AUL, 0xBFE93A22499263FCUL,
		0xBC96788EBCC76DC6UL, 0xBFE9B3E047F38740UL, 0xBCADDAE89FD441D1UL, 0xBFEA29A7A0462780UL,
		0x3C9F98273C5D2495UL, 0xBFEA9B66290EA1A4UL, 0x3C8926DA300FFCCEUL, 0xBFEB090A58150200UL,
		0xBCA90E58336C64A8UL, 0xBFEB728345196E3CUL, 0xBCA9F6963354E3FEUL, 0xBFEBD7C0AC6F9528UL,
		0xBC9A47D3A2A0DCBEUL, 0xBFEC38B2F180BDB0UL, 0xBC9ED0489E16B9A0UL, 0xBFEC954B213411F4UL,
		0x3CA0F3DB5DAD5AC5UL, 0xBFECED7AF43CC774UL, 0xBCAAC42B5A8B6943UL, 0xBFED4134D14DC938UL,
		0x3CAD75033DFB9CA8UL, 0xBFED906BCF328D48UL, 0xBC883C37C6107DB3UL, 0xBFEDDB13B6CCC23CUL,
		0xBC97F59C49F6CD6DUL, 0xBFEE212104F686E4UL, 0xBCAEE94A90D7B88BUL, 0xBFEE6288EC48E110UL,
		0x3CAA27D3874701F9UL, 0xBFEE9F4156C62DDCUL, 0x3C985F4E1B8298D0UL, 0xBFEED740E7684964UL,
		0x3C9AB4E148E52D9EUL, 0xBFEF0A7EFB9230D8UL, 0xBC98A11412B82346UL, 0xBFEF38F3AC64E588UL,
		0xBC7562172A361FD3UL, 0xBFEF6297CFF75CB0UL, 0xBCA3564ACEF1FF97UL, 0xBFEF8764FA714BA8UL,
		0x3CA5E82A3284D5C8UL, 0xBFEFA7557F08A518UL, 0x3C9709BCCB89A989UL, 0xBFEFC26470E19FD4UL,
		0xBCA9E082721DFB8EUL, 0xBFEFD88DA3D12524UL, 0x3CAEADE132F3981DUL, 0xBFEFE9CDAD01883CUL,
		0xBCAE3A843D1DB55FUL, 0xBFEFF621E3796D7CUL, 0xBC9765595D548D9AUL, 0xBFEFFD886084CD0CUL,
		0x0000000000000000UL, 0xBFF0000000000000UL, 0xBC9765595D548D9AUL, 0xBFEFFD886084CD0CUL,
		0xBCAE3A843D1DB55FUL, 0xBFEFF621E3796D7CUL, 0x3CAEADE132F3981DUL, 0xBFEFE9CDAD01883CUL,
		0xBCA9E082721DFB8EUL, 0xBFEFD88DA3D12524UL, 0x3C9709BCCB89A989UL, 0xBFEFC26470E19FD4UL,
		0x3CA5E82A3284D5C8UL, 0xBFEFA7557F08A518UL, 0xBCA3564ACEF1FF97UL, 0xBFEF8764FA714BA8UL,
		0xBC7562172A361FD3UL, 0xBFEF6297CFF75CB0UL, 0xBC98A11412B82346UL, 0xBFEF38F3AC64E588UL,
		0x3C9AB4E148E52D9EUL, 0xBFEF0A7EFB9230D8UL, 0x3C985F4E1B8298D0UL, 0xBFEED740E7684964UL,
		0x3CAA27D3874701F9UL, 0xBFEE9F4156C62DDCUL, 0xBCAEE94A90D7B88BUL, 0xBFEE6288EC48E110UL,
		0xBC97F59C49F6CD6DUL, 0xBFEE212104F686E4UL, 0xBC883C37C6107DB3UL, 0xBFEDDB13B6CCC23CUL,
		0x3CAD75033DFB9CA8UL, 0xBFED906BCF328D48UL, 0xBCAAC42B5A8B6943UL, 0xBFED4134D14DC938UL,
		0x3CA0F3DB5DAD5AC5UL, 0xBFECED7AF43CC774UL, 0xBC9ED0489E16B9A0UL, 0xBFEC954B213411F4UL,
		0xBC9A47D3A2A0DCBEUL, 0xBFEC38B2F180BDB0UL, 0xBCA9F6963354E3FEUL, 0xBFEBD7C0AC6F9528UL,
		0xBCA90E58336C64A8UL, 0xBFEB728345196E3CUL, 0x3C8926DA300FFCCEUL, 0xBFEB090A58150200UL,
		0x3C9F98273C5D2495UL, 0xBFEA9B66290EA1A4UL, 0xBCADDAE89FD441D1UL, 0xBFEA29A7A0462780UL,
		0xBC96788EBCC76DC6UL, 0xBFE9B3E047F38740UL, 0x3C9615F32B6F907AUL, 0xBFE93A22499263FCUL,
		0xBC969D0F6897664AUL, 0xBFE8BC806B151740UL, 0xBCAD217BE0E2B971UL, 0xBFE83B0E0BFF976CUL,
		0x3CA21EA6F59BE15BUL, 0xBFE7B5DF226AAFB0UL, 0x3CABCAC43C389BA9UL, 0xBFE72D0837EFFF98UL,
		0xBC921165F626CDD5UL, 0xBFE6A09E667F3BCCUL, 0x3CA24A366A5FE547UL, 0xBFE610B7551D2CE0UL,
		0x3C875720992BFBB2UL, 0xBFE57D69348CECA0UL, 0xBCA13C293EDCEB32UL, 0xBFE4E6CABBE3E5E8UL,
		0x3CAE7F895D302395UL, 0xBFE44CF325091DD8UL, 0xBCA3C7C4BC72A92CUL, 0xBFE3AFFA292050B8UL,
		0xBC9C20673B2116B2UL, 0xBFE30FF7FCE17034UL, 0x3CA5769D0FBCDDC3UL, 0xBFE26D054CDD12E0UL,
		0xBC8B25DD267F6600UL, 0xBFE1C73B39AE68C8UL, 0x3CA7BC8EDA6AF93CUL, 0xBFE11EB3541B4B24UL,
		0xBCA9697FAF2E2FE5UL, 0xBFE073879922FFECUL, 0xBC9FB44F80F92225UL, 0xBFDF8BA4DBF89AB8UL,
		0x3C8C3E4EDC5872F8UL, 0xBFDE2B5D3806F63CUL, 0x3C9E97AF1A63C807UL, 0xBFDCC66E9931C460UL,
		0xBC65B362CB974183UL, 0xBFDB5D1009E15CC0UL, 0x3C9D24AFDADE848BUL, 0xBFD9EF7943A8ED8CUL,
		0x3C92E59DBA7AB4C2UL, 0xBFD87DE2A6AEA964UL, 0x3C9512C678219317UL, 0xBFD7088530FA45A0UL,
		0xBC8FC2047E54E614UL, 0xBFD58F9A75AB1FDCUL, 0xBC94325F12BE8946UL, 0xBFD4135C94176600UL,
		0xBC9A8B5C974EE7B5UL, 0xBFD294062ED59F04UL, 0x3C83ED9EFAA42AB3UL, 0xBFD111D262B1F678UL,
		0x3C850B7BBC4768B1UL, 0xBFCF19F97B215F1CUL, 0x3C8035E2873CA432UL, 0xBFCC0B826A7E4F64UL,
		0x3C849B466E7FE360UL, 0xBFC8F8B83C69A60CUL, 0x3C8AB3802218894FUL, 0xBFC5E214448B3FC8UL,
		0x3C8DD9FFEAECBDC4UL, 0xBFC2C8106E8E613CUL, 0x3C7CBB1F71ACA352UL, 0xBFBF564E56A97310UL,
		0x3C3E2718D26ED688UL, 0xBFB917A6BC29B42CUL, 0xBC7CCBEEEAE8129AUL, 0xBFB2D52092CE19F4UL,
		0x3C2912BD0D569A90UL, 0xBFA91F65F10DD814UL, 0xBC5F938A73DB97FBUL, 0xBF992155F7A3667CUL,
	};
	// ONE_TWENTY_EIGHT_OVER_PI: 64 x 4
	static readonly ulong[] L128PiBits = {
		0x4040000000000014UL, 0x3CE7CC1B727220A8UL, 0x39A4FE13ABE8FA9CUL, 0xB66911F924EB5336UL,
		0x4040000000145F30UL, 0x3CEB727220A94FE0UL, 0x39A3ABE8FA9A6EE0UL, 0x364B6C52B3278872UL,
		0x404000145F306DC8UL, 0x3D0C882A53F84EB0UL, 0xB9A70565911F9250UL, 0x3664ACC9E21C8210UL,
		0x40445F306DC9C884UL, 0xBD05AC07B1505C14UL, 0xB9C96447E493AD4CUL, 0xB67B0EF1BEF806BAUL,
		0xC03F246C6EFAB580UL, 0xBCEEC54170565910UL, 0xB9AF924EB53361E0UL, 0x366C820FF28B1D5FUL,
		0x403391054A7F09D4UL, 0x3CFF47D4D377036CUL, 0x39B8A5664F10E410UL, 0x365FE5163ABDEBBCUL,
		0x401529FC2757D1F4UL, 0x3CD34DDC0DB62958UL, 0x39993C439041FE50UL, 0x36563ABDEBBC561BUL,
		0xBFEEC54170565910UL, 0xBCAF924EB53361E0UL, 0x396C820FF28B1D60UL, 0xB620A21D4F246DC9UL,
		0xC04505C1596447E4UL, 0xBCF275A99B0EF1C0UL, 0x39B07F9458EAF7B0UL, 0xB670EA79236E4717UL,
		0xC00596447E493AD4UL, 0xBCB9B0EF1BEF806CUL, 0x39563ABDEBBC561CUL, 0xB601B7238B7B645AUL,
		0x404BB81B6C52B328UL, 0xBCEDE37DF00D74E4UL, 0x39A5EF5DE2B0DB94UL, 0xB66C8E2DED916900UL,
		0x404B6C52B3278874UL, 0xBD0F7C035D38A844UL, 0x39C778AC36E48DC8UL, 0xB676F6C8B47FE6DBUL,
		0x4042B32788720840UL, 0xBCDAE9C5421443A8UL, 0xB99E48DB91C5BDB4UL, 0x365D2E006492EEA1UL,
		0xC048778DF7C035D4UL, 0x3CED5EF5DE2B0DB8UL, 0x39A2371D2126E970UL, 0x35F924BBA8274648UL,
		0xC03BEF806BA71508UL, 0xBCD443A9E48DB91CUL, 0xB976F6C8B47FE6DCUL, 0x36277504E8C90E7FUL,
		0xBFDAE9C5421443A8UL, 0xBC9E48DB91C5BDB4UL, 0x395D2E006492EEA0UL, 0x3603A32439FC3BD6UL,
		0xC0438A84288753C8UL, 0xBD01B7238B7B645CUL, 0x39CC00C925DD413CUL, 0xB68CDBC603C429C7UL,
		0xC020A21D4F246DC8UL, 0xBCDC5BDB22D1FF9CUL, 0x39825DD413A32438UL, 0x364FC3BD63962535UL,
		0xC02D4F246DC8E2E0UL, 0x3CE26E9700324978UL, 0xB995F62E6DE301E4UL, 0x365EB1CB129A73EFUL,
		0xC03236E4716F6C8CUL, 0x3CE7003249775050UL, 0xB9A736F180F10A70UL, 0xB66A76B2C608BBEEUL,
		0x403B8E909374B800UL, 0x3CF924BBA8274648UL, 0x399CFE1DEB1CB128UL, 0x365A73EE88235F53UL,
		0x40309374B801924CUL, 0xBCD15F62E6DE3020UL, 0x399DEB1CB129A740UL, 0xB65177DCA0AD144CUL,
		0xC0268FFCDB688AFCUL, 0x3CDD1921CFE1DEB0UL, 0x399CB129A73EE884UL, 0xB65CA0AD144BB7B1UL,
		0x3FF924BBA8274648UL, 0x3C9CFE1DEB1CB128UL, 0x395A73EE88235F54UL, 0xB61144BB7B166390UL,
		0xC04A22BEC5CDBC60UL, 0xBCDE214E34ED658CUL, 0xB95177DCA0AD144CUL, 0x35F213A671C09AD1UL,
		0x4003A32439FC3BD8UL, 0xBCCC69DACB1822F0UL, 0x3961AFA975DA2428UL, 0xB616638FD94BA082UL,
		0xC03B78C0788538D4UL, 0x3CD29A73EE882360UL, 0xB985A28976F62CC8UL, 0x363C09AD17DF904EUL,
		0x404FC3BD63962534UL, 0x3CFCFBA208D7D4BCUL, 0xB9B12EDEC598E3F8UL, 0x367AD17DF904E647UL,
		0xC014E34ED658C118UL, 0x3CC046BEA5D76890UL, 0x3973A671C09AD17CUL, 0x363F904E64758E61UL,
		0x40462534E7DD1048UL, 0xBD0415A28976F62CUL, 0xB9B8E3F652E82070UL, 0x3653991D63983534UL,
		0xC0363045DF7282B4UL, 0xBCD44BB7B16638FCUL, 0xB9994BA081BEC670UL, 0x365D639835339F4AUL,
		0x404D1046BEA5D768UL, 0x3CF213A671C09AD0UL, 0x39B7DF904E647590UL, 0xB679F2B3182D8DEFUL,
		0x402AFA975DA24274UL, 0x3CD9C7026B45F7E4UL, 0x3953991D63983534UL, 0xB5F82D8DEE81D108UL,
		0xC04A28976F62CC70UL, 0xBD0FB29741037D8CUL, 0xB9BB8A719F2B3184UL, 0x367272117E2EF7E5UL,
		0xC0476F62CC71FB28UL, 0xBD0741037D8CDC54UL, 0x39ACC1A99CFA4E44UL, 0xB66D03A21036BE27UL,
		0x404D338E04D68BF0UL, 0xBCDBEC66E29C67CCUL, 0x399339F49C845F8CUL, 0xB63081B5F13801DAUL,
		0x403C09AD17DF9050UL, 0xBCF9B8A719F2B318UL, 0xB986C6F740E8840CUL, 0xB64AF89C00ED0004UL,
		0x40468BEFC827323CUL, 0xBD038CF9598C16C8UL, 0x39C08BF177BF2508UL, 0xB673801DA00087EAUL,
		0xC04037D8CDC538D0UL, 0x3CEA99CFA4E422FCUL, 0x39877BF250763FF0UL, 0x3642FFFBC0B301FEUL,
		0xC048CDC538CF9598UL, 0xBCF82D8DEE81D108UL, 0xB97B5F13801DA000UL, 0xB620FD33F8086877UL,
		0xC024E33E566305B0UL, 0xBCEBDD03A21036C0UL, 0x39AD8FFC4BFFEF04UL, 0xB6633F80868773A5UL,
		0xC03F2B3182D8DEE8UL, 0xBCBD1081B5F13800UL, 0xB97DA00087E99FC0UL, 0xB610D0EE74A5F593UL,
		0xC048C16C6F740E88UL, 0xBCE036BE27003B40UL, 0xB920FD33F8086878UL, 0x35D8B5A0A6D1F6D3UL,
		0x4043908BF177BF24UL, 0x3D00763FF12FFFBCUL, 0x3976603FBCBC462CUL, 0x3636829B47DB4DA0UL,
		0x4037E2EF7E4A0EC8UL, 0xBC7DA00087E99FC0UL, 0xB910D0EE74A5F594UL, 0x35D1F6D367ECF27DUL,
		0xC03081B5F13801DCUL, 0x3CFFFF7816603FBCUL, 0x39A788C5AD053690UL, 0xB6425930261B069FUL,
		0xC04AF89C00ED0004UL, 0xBCDFA67F010D0EE8UL, 0x3986B414DA3EDA6CUL, 0x363FB3C9F2C26DD4UL,
		0xC04C00ED00043F4CUL, 0xBCFFC04343B9D298UL, 0x3984DA3EDA6CFDA0UL, 0xB64B069EC9161738UL,
		0x4042FFFBC0B301FCUL, 0x3D0E5E2316B414DCUL, 0xB9CC125930261B08UL, 0x3686136E9E8C7ECDUL,
		0xC020FD33F8086878UL, 0x3CD8B5A0A6D1F6D4UL, 0xB9830261B069EC90UL, 0xB6461738132C3403UL,
		0xC039FC04343B9D28UL, 0xBCF7D64B824B2604UL, 0xB9A86C1A7B24585CUL, 0xB65C09961A015D29UL,
		0xC010D0EE74A5F594UL, 0x3CD1F6D367ECF27CUL, 0x3986136E9E8C7ECCUL, 0x3643CBFD45AEA4F7UL,
		0xC04DCE94BEB25C14UL, 0x3D0A6CFD9E4F9614UL, 0xB9B22C2E70265868UL, 0xB615D28AD8453814UL,
		0xC044BEB25C125930UL, 0xBCD30D834F648B0CUL, 0x3978FD9A797FA8B4UL, 0x363D49EEB1FAF97CUL,
		0x403B47DB4D9FB3C8UL, 0x3CFF2C26DD3D18FCUL, 0x39B9A797FA8B5D48UL, 0x367EEB1FAF97C5EDUL,
		0xC0425930261B06A0UL, 0x3D036E9E8C7ECD3CUL, 0x39B7FA8B5D49EEB0UL, 0x367FAF97C5ECF41DUL,
		0x403FB3C9F2C26DD4UL, 0xBCC738132C3402BCUL, 0x398AEA4F758FD7CCUL, 0xB60D0985F18C10EBUL,
		0xC04B069EC9161738UL, 0xBCC32C3402BA515CUL, 0x397EEB1FAF97C5ECUL, 0x362E839CFBC52949UL,
		0xC04EC9161738132CUL, 0xBCDA015D28AD8454UL, 0x397FAF97C5ECF41CUL, 0x362CFBC529497536UL,
		0xC0461738132C3404UL, 0x3D045AEA4F758FD8UL, 0xB99A0E84C2F8C608UL, 0xB63D6B5B45650128UL,
		0x402FB34F2FF516BCUL, 0xBCE6C229C0A0D074UL, 0xB9730BE31821D6B4UL, 0xB63B4565012813B8UL,
		0x4043CBFD45AEA4F8UL, 0xBCF4E050683A130CUL, 0x397CE7DE294A4BA8UL, 0x363AFED7EC47E357UL,
		0xC015D28AD8453814UL, 0xBC9A0E84C2F8C608UL, 0xB93D6B5B45650128UL, 0xB5B3B81CA8BDEA7FUL,
		0xC0415B08A7028340UL, 0xBD0D0985F18C10ECUL, 0x39B4A4BA9AFED7ECUL, 0x3651F8D5D0856033UL,
	};
	// EXP2_MID1 / EXP2_MID2: {lo, mid, hi} x 64
	static readonly ulong[] LExpMid1Bits = {
		0x0000000000000000UL, 0x0000000000000000UL, 0x3FF0000000000000UL,
		0xB919085B0A3D74D5UL, 0xBC719083535B085DUL, 0x3FF02C9A3E778061UL,
		0x39105FF94F8D257EUL, 0x3C8D73E2A475B465UL, 0x3FF059B0D3158574UL,
		0x39015820D96B414FUL, 0x3C6186BE4BB284FFUL, 0x3FF0874518759BC8UL,
		0xB9367C9BD6EBF74CUL, 0x3C98A62E4ADC610BUL, 0x3FF0B5586CF9890FUL,
		0xB8E5AA76994E9DDBUL, 0x3C403A1727C57B53UL, 0x3FF0E3EC32D3D1A2UL,
		0x3929D58B988F562DUL, 0xBC96C51039449B3AUL, 0x3FF11301D0125B51UL,
		0xB932FE7BB4C76416UL, 0xBC932FBF9AF1369EUL, 0x3FF1429AAEA92DE0UL,
		0x3924F2406AA13FF0UL, 0xBC819041B9D78A76UL, 0x3FF172B83C7D517BUL,
		0x390AD36183926AE8UL, 0x3C8E5B4C7B4968E4UL, 0x3FF1A35BEB6FCB75UL,
		0x391EA62D0881B918UL, 0x3C9E016E00A2643CUL, 0x3FF1D4873168B9AAUL,
		0xB90781DBC16F1EA4UL, 0x3C8DC775814A8495UL, 0x3FF2063B88628CD6UL,
		0xB924D89F9AF532E0UL, 0x3C99B07EB6C70573UL, 0x3FF2387A6E756238UL,
		0x391277393A461B77UL, 0x3C82BD339940E9D9UL, 0x3FF26B4565E27CDDUL,
		0x390DE54485604690UL, 0x3C8612E8AFAD1255UL, 0x3FF29E9DF51FDEE1UL,
		0xB91EE9D8F8CB9307UL, 0x3C90024754DB41D5UL, 0x3FF2D285A6E4030BUL,
		0x3917B7B2F09CD0D9UL, 0x3C86F46AD23182E4UL, 0x3FF306FE0A31B715UL,
		0xB93406A2EA6CFC6BUL, 0x3C932721843659A6UL, 0x3FF33C08B26416FFUL,
		0x39387E3E12516BFAUL, 0xBC963AEABF42EAE2UL, 0x3FF371A7373AA9CBUL,
		0x3909B0B1FF17C296UL, 0xBC75E436D661F5E3UL, 0x3FF3A7DB34E59FF7UL,
		0xB92808BA68FA8FB7UL, 0x3C8ADA0911F09EBCUL, 0x3FF3DEA64C123422UL,
		0xB8D32B43EAFC6518UL, 0xBC5EF3691C309278UL, 0x3FF4160A21F72E2AUL,
		0xB8D0AC312DE3D922UL, 0x3C489B7A04EF80D0UL, 0x3FF44E086061892DUL,
		0x390E1EEBAE743AC0UL, 0x3C73C1A3B69062F0UL, 0x3FF486A2B5C13CD0UL,
		0x38EC06C7745C2B39UL, 0x3C7D4397AFEC42E2UL, 0x3FF4BFDAD5362A27UL,
		0xB8F1AA1FD7B685CDUL, 0xBC94B309D25957E3UL, 0x3FF4F9B2769D2CA7UL,
		0x390FA733951F214CUL, 0xBC807ABE1DB13CADUL, 0x3FF5342B569D4F82UL,
		0xB90FF86852A613FFUL, 0x3C99BB2C011D93ADUL, 0x3FF56F4736B527DAUL,
		0xB92744EE506FDAFEUL, 0x3C96324C054647ADUL, 0x3FF5AB07DD485429UL,
		0xB9395F9AB75FA7D6UL, 0x3C9BA6F93080E65EUL, 0x3FF5E76F15AD2148UL,
		0x3905D8E757CFB991UL, 0xBC9383C17E40B497UL, 0x3FF6247EB03A5585UL,
		0x3934A337F4DC0A3BUL, 0xBC9BB60987591C34UL, 0x3FF6623882552225UL,
		0x39357D3E3ADEC175UL, 0xBC9BDD3413B26456UL, 0x3FF6A09E667F3BCDUL,
		0x38CA59F88ABBE778UL, 0xBC6BBE3A683C88ABUL, 0x3FF6DFB23C651A2FUL,
		0xB92269796953A4C3UL, 0xBC816E4786887A99UL, 0x3FF71F75E8EC5F74UL,
		0xB938F8E7FA19E5E8UL, 0xBC90245957316DD3UL, 0x3FF75FEB564267C9UL,
		0xB8E4217A932D10D4UL, 0xBC841577EE04992FUL, 0x3FF7A11473EB0187UL,
		0x38F70A1427F8FCDFUL, 0x3C705D02BA15797EUL, 0x3FF7E2F336CF4E62UL,
		0x38F0F6AD65CBBAC1UL, 0xBC9D4C1DD41532D8UL, 0x3FF82589994CCE13UL,
		0xB92F16F65181D921UL, 0xBC9FC6F89BD4F6BAUL, 0x3FF868D99B4492EDUL,
		0xB9130644A7836333UL, 0x3C96E9F156864B27UL, 0x3FF8ACE5422AA0DBUL,
		0x38D3BF26D2B85163UL, 0x3C85CC13A2E3976CUL, 0x3FF8F1AE99157736UL,
		0x390697E257AC0DB2UL, 0xBC675FC781B57EBCUL, 0x3FF93737B0CDC5E5UL,
		0x3937EDB9D7144B6FUL, 0xBC9D185B7C1B85D1UL, 0x3FF97D829FDE4E50UL,
		0x3916376B7943085CUL, 0x3C7C7C46B071F2BEUL, 0x3FF9C49182A3F090UL,
		0x392354084551B4FBUL, 0xBC9359495D1CD533UL, 0x3FFA0C667B5DE565UL,
		0xB90BFD7ADFD63F48UL, 0xBC9D2F6EDB8D41E1UL, 0x3FFA5503B23E255DUL,
		0x3928B16AE39E8CB9UL, 0x3C90FAC90EF7FD31UL, 0x3FFA9E6B5579FDBFUL,
		0x393A7FBC3AE675EAUL, 0x3C97A1CD345DCC81UL, 0x3FFAE89F995AD3ADUL,
		0x3902BABC0EDDA4D9UL, 0xBC62805E3084D708UL, 0x3FFB33A2B84F15FBUL,
		0x390AA64481E1AB72UL, 0xBC75584F7E54AC3BUL, 0x3FFB7F76F2FB5E47UL,
		0x3929A164050E1258UL, 0x3C823DD07A2D9E84UL, 0x3FFBCC1E904BC1D2UL,
		0x39199E51125928DAUL, 0x3C811065895048DDUL, 0x3FFC199BDD85529CUL,
		0xB92FC44C329D5CB2UL, 0x3C92884DFF483CADUL, 0x3FFC67F12E57D14BUL,
		0x391D8765566B032EUL, 0x3C7503CBD1E949DBUL, 0x3FFCB720DCEF9069UL,
		0xB93E7044039DA0F6UL, 0xBC9CBC3743797A9CUL, 0x3FFD072D4A07897CUL,
		0xB90AB053B05531FCUL, 0x3C82ED02D75B3707UL, 0x3FFD5818DCFBA487UL,
		0x3937F6246F0EC615UL, 0x3C9C2300696DB532UL, 0x3FFDA9E603DB3285UL,
		0x393B7225A944EFD6UL, 0xBC91A5CD4F184B5CUL, 0x3FFDFC97337B9B5FUL,
		0x3921E92CB3C2D278UL, 0x3C839E8980A9CC8FUL, 0x3FFE502EE78B3FF6UL,
		0xB92FC0F242BBF3DEUL, 0xBC9E9C23179C2893UL, 0x3FFEA4AFA2A490DAUL,
		0x393F6DD5D229FF69UL, 0x3C9DC7F486A4B6B0UL, 0x3FFEFA1BEE615A27UL,
		0xB914019BFFC80EF3UL, 0x3C99D3E12DD8A18BUL, 0x3FFF50765B6E4540UL,
		0x38FDC060C36F7651UL, 0x3C874853F3A5931EUL, 0x3FFFA7C1819E90D8UL,
	};
	static readonly ulong[] LExpMid2Bits = {
		0x0000000000000000UL, 0x0000000000000000UL, 0x3FF0000000000000UL,
		0x39339726694630E3UL, 0x3C9AE8E38C59C72AUL, 0x3FF000B175EFFDC7UL,
		0x38FE5E06DDD31156UL, 0xBC57B5D0D58EA8F4UL, 0x3FF00162F3904052UL,
		0x3905A0768B51F609UL, 0x3C94115CB6B16A8EUL, 0x3FF0021478E11CE6UL,
		0x390D008403605217UL, 0xBC8D7C96F201BB2FUL, 0x3FF002C605E2E8CFUL,
		0x39289BC16F765708UL, 0x3C984711D4C35E9FUL, 0x3FF003779A95F959UL,
		0xB924535B7F8C1E2DUL, 0xBC80484245243777UL, 0x3FF0042936FAA3D8UL,
		0xB938BA92F6B25456UL, 0xBC94B237DA2025F9UL, 0x3FF004DADB113DA0UL,
		0xB8E30C72E81F4294UL, 0xBC75E00E62D6B30DUL, 0x3FF0058C86DA1C0AUL,
		0xB9134A5384E6F0B9UL, 0x3C9A1D6CEDBB9481UL, 0x3FF0063E3A559473UL,
		0x393F8D0580865D2EUL, 0xBC94ACF197A00142UL, 0x3FF006EFF583FC3DUL,
		0xB90002BCB3AE9A99UL, 0xBC6EAF2EA42391A5UL, 0x3FF007A1B865A8CAUL,
		0x390C3C5AEDEE9851UL, 0x3C7DA93F90835F75UL, 0x3FF0085382FAEF83UL,
		0x3927217851D1EC6EUL, 0xBC86A79084AB093CUL, 0x3FF00905554425D4UL,
		0xB9180CBCA335A7C3UL, 0x3C986364F8FBE8F8UL, 0x3FF009B72F41A12BUL,
		0xB91706BD4EB22595UL, 0xBC882E8E14E3110EUL, 0x3FF00A6910F3B6FDUL,
		0xB90B55DD523F3C08UL, 0xBC84F6B2A7609F71UL, 0x3FF00B1AFA5ABCBFUL,
		0x39190A1E207CCED1UL, 0xBC7E1A258EA8F71BUL, 0x3FF00BCCEB7707ECUL,
		0x39178D0472DB37C5UL, 0x3C74362CA5BC26F1UL, 0x3FF00C7EE448EE02UL,
		0xB92BCD4DB3CB52FEUL, 0x3C9095A56C919D02UL, 0x3FF00D30E4D0C483UL,
		0xB8FCF1B131575EC2UL, 0xBC6406AC4E81A645UL, 0x3FF00DE2ED0EE0F5UL,
		0xB8F6AAA1FA7FF913UL, 0x3C9B5A6902767E09UL, 0x3FF00E94FD0398E0UL,
		0x39168F236DFF3218UL, 0xBC991B2060859321UL, 0x3FF00F4714AF41D3UL,
		0xB92E8BB58067E60AUL, 0x3C8427068AB22306UL, 0x3FF00FF93412315CUL,
		0x393D4CD5E1D71FDFUL, 0x3C9C1D0660524E08UL, 0x3FF010AB5B2CBD11UL,
		0x393E4ECF350EBE88UL, 0xBC9E7BDFB3204BE8UL, 0x3FF0115D89FF3A8BUL,
		0x3926A2AA2C89C4F8UL, 0x3C8843AA8B9CBBC6UL, 0x3FF0120FC089FF63UL,
		0x3911CA368A20ED05UL, 0xBC734104EE7EDAE9UL, 0x3FF012C1FECD613BUL,
		0x38DEDB1095D925CFUL, 0xBC72B6AEB6176892UL, 0x3FF0137444C9B5B5UL,
		0xB90488C78EDED75FUL, 0x3C7A8CD33B8A1BB3UL, 0x3FF01426927F5278UL,
		0xB8E7480F5EA1B3C9UL, 0x3C72EDC08E5DA99AUL, 0x3FF014D8E7EE8D2FUL,
		0xB90AE45989A04DD5UL, 0x3C857BA2DC7E0C73UL, 0x3FF0158B4517BB88UL,
		0x392BF48007D80987UL, 0x3C9B61299AB8CDB7UL, 0x3FF0163DA9FB3335UL,
		0x3921AA91A059292CUL, 0xBC990565902C5F44UL, 0x3FF016F0169949EDUL,
		0x391B6663292855F5UL, 0x3C870FC41C5C2D53UL, 0x3FF017A28AF25567UL,
		0x393E7FBCA6793D94UL, 0x3C94B9A6E145D76CUL, 0x3FF018550706AB62UL,
		0xB915B9F5C7DE3B93UL, 0xBC7008EFF5142BF9UL, 0x3FF019078AD6A19FUL,
		0x3914638BF2F6ACABUL, 0xBC977669F033C7DEUL, 0x3FF019BA16628DE2UL,
		0xB92AB237B9A069C5UL, 0xBC909BB78EEEAD0AUL, 0x3FF01A6CA9AAC5F3UL,
		0x3933AB358BE97CEFUL, 0x3C9371231477ECE5UL, 0x3FF01B1F44AF9F9EUL,
		0xB914027B2294BB64UL, 0x3C75E7626621EB5BUL, 0x3FF01BD1E77170B4UL,
		0x390656394426C990UL, 0xBC9BC72B100828A5UL, 0x3FF01C8491F08F08UL,
		0x390BF9785189BDD8UL, 0xBC6CE39CBBAB8BBEUL, 0x3FF01D37442D5070UL,
		0x3927C12F86114FE3UL, 0x3C816996709DA2E2UL, 0x3FF01DE9FE280AC8UL,
		0xB92653D5D24B5D28UL, 0xBC8C11F5239BF535UL, 0x3FF01E9CBFE113EFUL,
		0x39204A0CDC1D86D7UL, 0x3C8E1D4EB5EDC6B3UL, 0x3FF01F4F8958C1C6UL,
		0x392C678C46149782UL, 0xBC9AFB99946EE3F0UL, 0x3FF020025A8F6A35UL,
		0x39348524E1E9DF70UL, 0xBC98F06D8A148A32UL, 0x3FF020B533856324UL,
		0x3929953EA727FF0BUL, 0xBC82BF310FC54EB6UL, 0x3FF02168143B0281UL,
		0xB93CCFBBEC22D28EUL, 0xBC9C95A035EB4175UL, 0x3FF0221AFCB09E3EUL,
		0x3939E2BB6E181DE1UL, 0xBC9491793E46834DUL, 0x3FF022CDECE68C4FUL,
		0x391F17609AE29308UL, 0xBC73E8D0D9C49091UL, 0x3FF02380E4DD22ADUL,
		0xB91C7DC2C476BFB8UL, 0xBC9314AA16278AA3UL, 0x3FF02433E494B755UL,
		0xB92FAB994971D4A3UL, 0x3C848DAF888E9651UL, 0x3FF024E6EC0DA046UL,
		0x392848B62CBDD0AFUL, 0x3C856DC8046821F4UL, 0x3FF02599FB483385UL,
		0xB92BF603BA715D0CUL, 0x3C945B42356B9D47UL, 0x3FF0264D1244C719UL,
		0x39189434E751E1AAUL, 0xBC7082EF51B61D7EUL, 0x3FF027003103B10EUL,
		0xB9103B54FD64E8ACUL, 0x3C72106ED0920A34UL, 0x3FF027B357854772UL,
		0x3927785EA0ACC486UL, 0xBC9FD4CF26EA5D0FUL, 0x3FF0286685C9E059UL,
		0xB92CE447FDB35FF9UL, 0xBC909F8775E78084UL, 0x3FF02919BBD1D1D8UL,
		0x38F5B884AAB5642AUL, 0x3C564CBBA902CA27UL, 0x3FF029CCF99D720AUL,
		0xB93CFB3E46D7C1C0UL, 0x3C94383EF231D207UL, 0x3FF02A803F2D170DUL,
		0xB8F0D40CEE4B81AFUL, 0x3C94A47A505B3A47UL, 0x3FF02B338C811703UL,
		0x3926AE7D36D7C1F7UL, 0x3C9E47120223467FUL, 0x3FF02BE6E199C811UL,
	};
	static readonly double[] SinTab = Conv(SinTabBits);   // sin(j*pi/512) hi,lo  j=0..256
	static readonly double[] ExpTab = Conv(ExpTabBits);   // 2^(j/256)    hi,lo  j=0..255
	static readonly double[] AtanI = Conv(AtanIBits);   // {hi, lo} x 65
	static readonly double[] LSinK = Conv(LSinKBits);         // [2k] = lo, [2k+1] = hi
	static readonly double[] L128Pi = Conv(L128PiBits);       // 64 x 4
	static readonly double[] LExpMid1 = Conv(LExpMid1Bits);   // [3i] = lo, [3i+1] = mid, [3i+2] = hi
	static readonly double[] LExpMid2 = Conv(LExpMid2Bits);
	static readonly double[] GPowLog = Conv(GPowLogBits);   // {invc, logc, logctail} x 128
}
