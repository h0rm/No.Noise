using System;
using System.Runtime.InteropServices;
using GLib;
using Clutter;

namespace Banshee.ClutterTest2
{
	public static class ClutterHelper
	{
		[DllImport ("libclutter-glx-1.0.so.0")]
		static extern void clutter_main();
		
		[DllImport ("libclutter-glx-1.0.so.0")]
		static extern void clutter_init (int argc, IntPtr argv);
		
		public static void Init( ) {
			clutter_init (0, IntPtr.Zero);
		}
		
		public static void Main() {
			clutter_main();	
		}
	}
}

