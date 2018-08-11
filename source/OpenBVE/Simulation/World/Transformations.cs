﻿using System;
using OpenBveApi.Math;

namespace OpenBve
{
	internal static partial class World
	{
		/// <summary>Creates the cross product of two vectors, input as induvidual co-ordinates</summary>
		internal static void Cross(double ax, double ay, double az, double bx, double by, double bz, out double cx, out double cy, out double cz)
		{
			cx = ay * bz - az * by;
			cy = az * bx - ax * bz;
			cz = ax * by - ay * bx;
		}

		// transformation
		internal struct Transformation
		{
			internal Vector3 X;
			internal Vector3 Y;
			internal Vector3 Z;
			/// <summary>Creates a new transformation, based upon yaw pitch and roll values</summary>
			/// <param name="Yaw">The yaw to apply</param>
			/// <param name="Pitch">The pitch to apply</param>
			/// <param name="Roll">The roll to apply</param>
			internal Transformation(double Yaw, double Pitch, double Roll)
			{
				if (Yaw == 0.0 & Pitch == 0.0 & Roll == 0.0)
				{
					this.X = new Vector3(1.0, 0.0, 0.0);
					this.Y = new Vector3(0.0, 1.0, 0.0);
					this.Z = new Vector3(0.0, 0.0, 1.0);
				}
				else if (Pitch == 0.0 & Roll == 0.0)
				{
					double cosYaw = Math.Cos(Yaw);
					double sinYaw = Math.Sin(Yaw);
					this.X = new Vector3(cosYaw, 0.0, -sinYaw);
					this.Y = new Vector3(0.0, 1.0, 0.0);
					this.Z = new Vector3(sinYaw, 0.0, cosYaw);
				}
				else
				{
					double sx = 1.0, sy = 0.0, sz = 0.0;
					double ux = 0.0, uy = 1.0, uz = 0.0;
					double dx = 0.0, dy = 0.0, dz = 1.0;
					double cosYaw = Math.Cos(Yaw);
					double sinYaw = Math.Sin(Yaw);
					double cosPitch = Math.Cos(-Pitch);
					double sinPitch = Math.Sin(-Pitch);
					double cosRoll = Math.Cos(-Roll);
					double sinRoll = Math.Sin(-Roll);
					Rotate(ref sx, ref sy, ref sz, ux, uy, uz, cosYaw, sinYaw);
					Rotate(ref dx, ref dy, ref dz, ux, uy, uz, cosYaw, sinYaw);
					Rotate(ref ux, ref uy, ref uz, sx, sy, sz, cosPitch, sinPitch);
					Rotate(ref dx, ref dy, ref dz, sx, sy, sz, cosPitch, sinPitch);
					Rotate(ref sx, ref sy, ref sz, dx, dy, dz, cosRoll, sinRoll);
					Rotate(ref ux, ref uy, ref uz, dx, dy, dz, cosRoll, sinRoll);
					this.X = new Vector3(sx, sy, sz);
					this.Y = new Vector3(ux, uy, uz);
					this.Z = new Vector3(dx, dy, dz);
				}
			}
			/// <summary>Creates a new transformation, based upon an initial transformation, plus secondary yaw pitch and roll values</summary>
			/// <param name="Transformation">The initial transformation</param>
			/// <param name="Yaw">The yaw to apply</param>
			/// <param name="Pitch">The pitch to apply</param>
			/// <param name="Roll">The roll to apply</param>
			internal Transformation(Transformation Transformation, double Yaw, double Pitch, double Roll)
			{
				double sx = Transformation.X.X, sy = Transformation.X.Y, sz = Transformation.X.Z;
				double ux = Transformation.Y.X, uy = Transformation.Y.Y, uz = Transformation.Y.Z;
				double dx = Transformation.Z.X, dy = Transformation.Z.Y, dz = Transformation.Z.Z;
				double cosYaw = Math.Cos(Yaw);
				double sinYaw = Math.Sin(Yaw);
				double cosPitch = Math.Cos(-Pitch);
				double sinPitch = Math.Sin(-Pitch);
				double cosRoll = Math.Cos(Roll);
				double sinRoll = Math.Sin(Roll);
				Rotate(ref sx, ref sy, ref sz, ux, uy, uz, cosYaw, sinYaw);
				Rotate(ref dx, ref dy, ref dz, ux, uy, uz, cosYaw, sinYaw);
				Rotate(ref ux, ref uy, ref uz, sx, sy, sz, cosPitch, sinPitch);
				Rotate(ref dx, ref dy, ref dz, sx, sy, sz, cosPitch, sinPitch);
				Rotate(ref sx, ref sy, ref sz, dx, dy, dz, cosRoll, sinRoll);
				Rotate(ref ux, ref uy, ref uz, dx, dy, dz, cosRoll, sinRoll);
				this.X = new Vector3(sx, sy, sz);
				this.Y = new Vector3(ux, uy, uz);
				this.Z = new Vector3(dx, dy, dz);
			}
			/// <summary>Creates a new transformation, based upon a base transformation and an auxiliary transformation</summary>
			/// <param name="BaseTransformation">The base transformation</param>
			/// <param name="AuxTransformation">The auxiliary transformation</param>
			internal Transformation(Transformation BaseTransformation, Transformation AuxTransformation)
			{
				Vector3 x = BaseTransformation.X;
				Vector3 y = BaseTransformation.Y;
				Vector3 z = BaseTransformation.Z;
				Vector3 s = AuxTransformation.X;
				Vector3 u = AuxTransformation.Y;
				Vector3 d = AuxTransformation.Z;
				Rotate(ref x.X, ref x.Y, ref x.Z, d.X, d.Y, d.Z, u.X, u.Y, u.Z, s.X, s.Y, s.Z);
				Rotate(ref y.X, ref y.Y, ref y.Z, d.X, d.Y, d.Z, u.X, u.Y, u.Z, s.X, s.Y, s.Z);
				Rotate(ref z.X, ref z.Y, ref z.Z, d.X, d.Y, d.Z, u.X, u.Y, u.Z, s.X, s.Y, s.Z);
				this.X = x;
				this.Y = y;
				this.Z = z;
			}
		}

		/// <summary>Rotates one vector based upon a second vector, input as induvidual co-ordinates</summary>
		/// <param name="p">The vector to rotate</param>
		/// <param name="dx">The X co-ordinate of the second vector</param>
		/// <param name="dy">The Y co-ordinate of the second vector</param>
		/// <param name="dz">The Z co-ordinate of the second vector</param>
		/// <param name="cosa">The Cosine of the angle to rotate by</param>
		/// <param name="sina">The Sine of the angle to rotate by</param>
		internal static void Rotate(ref Vector3 p, double dx, double dy, double dz, double cosa, double sina)
		{
			double t = 1.0 / Math.Sqrt(dx * dx + dy * dy + dz * dz);
			dx *= t; dy *= t; dz *= t;
			double oc = 1.0 - cosa;
			double Opt1 = oc * dx * dy;
			double Opt2 = sina * dz;
			double Opt3 = oc * dy * dz;
			double Opt4 = sina * dx;
			double Opt5 = sina * dy;
			double Opt6 = oc * dx * dz;
			double x = (cosa + oc * dx * dx) * p.X + (Opt1 - Opt2) * p.Y + (Opt6 + Opt5) * p.Z;
			double y = (cosa + oc * dy * dy) * p.Y + (Opt1 + Opt2) * p.X + (Opt3 - Opt4) * p.Z;
			double z = (cosa + oc * dz * dz) * p.Z + (Opt6 - Opt5) * p.X + (Opt3 + Opt4) * p.Y;
			p.X = x; p.Y = y; p.Z = z;
		}

		/// <summary>Rotates one vector based upon a second vector, both input as induvidual co-ordinates</summary>
		/// <param name="px">The X co-ordinate of the first vector</param>
		/// <param name="py">The Y co-ordinate of the first vector</param>
		/// <param name="pz">The Z co-ordinate of the first vector</param>
		/// <param name="dx">The X co-ordinate of the second vector</param>
		/// <param name="dy">The Y co-ordinate of the second vector</param>
		/// <param name="dz">The Z co-ordinate of the second vector</param>
		/// <param name="cosa">The Cosine of the angle to rotate by</param>
		/// <param name="sina">The Sine of the angle to rotate by</param>
		internal static void Rotate(ref double px, ref double py, ref double pz, double dx, double dy, double dz, double cosa, double sina)
		{
			double t = 1.0 / Math.Sqrt(dx * dx + dy * dy + dz * dz);
			dx *= t; dy *= t; dz *= t;
			double oc = 1.0 - cosa;
			double Opt1 = oc * dx * dy;
			double Opt2 = sina * dz;
			double Opt3 = oc * dy * dz;
			double Opt4 = sina * dx;
			double Opt5 = sina * dy;
			double Opt6 = oc * dx * dz;
			double x = (cosa + oc * dx * dx) * px + (Opt1 - Opt2) * py + (Opt6 + Opt5) * pz;
			double y = (cosa + oc * dy * dy) * py + (Opt1 + Opt2) * px + (Opt3 - Opt4) * pz;
			double z = (cosa + oc * dz * dz) * pz + (Opt6 - Opt5) * px + (Opt3 + Opt4) * py;
			px = x; py = y; pz = z;
		}

		internal static void Rotate(ref double px, ref double py, ref double pz, double dx, double dy, double dz, double ux, double uy, double uz, double sx, double sy, double sz)
		{
			var x = sx * px + ux * py + dx * pz;
			var y = sy * px + uy * py + dy * pz;
			var z = sz * px + uz * py + dz * pz;
			px = x; py = y; pz = z;
		}

		internal static void Rotate(ref double px, ref double py, ref double pz, Transformation t)
		{
			var x = t.X.X * px + t.Y.X * py + t.Z.X * pz;
			var y = t.X.Y * px + t.Y.Y * py + t.Z.Y * pz;
			var z = t.X.Z * px + t.Y.Z * py + t.Z.Z * pz;
			px = x; py = y; pz = z;
		}
		internal static void RotatePlane(ref Vector3 Vector, double cosa, double sina)
		{
			double u = Vector.X * cosa - Vector.Z * sina;
			double v = Vector.X * sina + Vector.Z * cosa;
			Vector.X = u;
			Vector.Z = v;
		}
	}
}
