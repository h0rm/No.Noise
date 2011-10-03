using System;
using System.Runtime.InteropServices;
using GLib;
using Clutter;
using Cogl;
using Hyena;

namespace NoNoise.Visualization.Util
{
    /// <summary>
    /// [Old] Helper class for clutter c-function calls.
    /// </summary>
	public static class ClutterHelper
	{
        private static int state;

        [DllImport ("libclutter-gtk-0.10.so.0")]
        static extern Clutter.InitError gtk_clutter_init (IntPtr argc, IntPtr argv);

        [DllImport ("libclutter-gtk-0.10.so.0")]
        static extern bool cogl_material_set_blend (IntPtr material, string text, IntPtr error);

        public static void GtkInit ()
        {

           if (gtk_clutter_init (IntPtr.Zero, IntPtr.Zero) != InitError.Success)
                    throw new System.NotSupportedException ("Unable to initialize GtkClutter");
           Hyena.Log.Information("GtkClutter initialized");
        }

        public static void Init ()
        {
            if (state < 1)
                GtkInit();
            state = 1;
        }

        public static bool SetBlendFunction (IntPtr material, string text)
        {
            return cogl_material_set_blend (material, text, IntPtr.Zero);
        }

	}
}

