// ClearScreen.cs
//
//  Copyright (C) 2007 Chris Howie
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
//
//

using System;
using System.ComponentModel;
using OpenVP;
using Tao.OpenGl;

namespace OpenVP.Core {
	[Serializable, Browsable(true), DisplayName("Clear screen"),
	 Category("Render"), Description("Clears the screen")]
	public sealed class ClearScreen : Effect {
		private Color mClearColor = new Color(0, 0, 0);
		
		[Browsable(true), DisplayName("Clear color"), Category("Display"),
		 Description("The color to clear the screen with.")]
		public Color ClearColor {
			get {
				return this.mClearColor;
			}
			set {
				this.mClearColor = value;
			}
		}
		
		public ClearScreen() {
		}
		
		public override void NextFrame(Controller controller) {
		}
		
		public override void RenderFrame(Controller controller) {
			this.ClearColor.Use();
			
			Gl.glMatrixMode(Gl.GL_PROJECTION);
			Gl.glPushMatrix();
			Gl.glLoadIdentity();
			
			Gl.glMatrixMode(Gl.GL_MODELVIEW);
			Gl.glPushMatrix();
			Gl.glLoadIdentity();
			
			Gl.glBegin(Gl.GL_QUADS);
			
			Gl.glVertex2i(-1, -1);
			Gl.glVertex2i( 1, -1);
			Gl.glVertex2i( 1,  1);
			Gl.glVertex2i(-1,  1);
			
			Gl.glEnd();
			
			Gl.glPopMatrix();
			
			Gl.glMatrixMode(Gl.GL_PROJECTION);
			Gl.glPopMatrix();
		}
	}
}
