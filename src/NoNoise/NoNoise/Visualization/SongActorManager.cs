// 
// SongActorManager.cs
// 
// Author:
//   horm <${AuthorEmail}>
// 
// Copyright (c) 2011 horm
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using Clutter;
using System.Collections.Generic;

namespace NoNoise.Visualization
{
    public class SongActorManager
    {
        private List<SongActor> song_actors;
        private Stack<SongActor> free_actors;

        public SongActorManager (int count)
        {
            SongActor.GeneratePrototypes ();

            Init (count);
        }

        public void Init (int count)
        {
            song_actors = new List<SongActor> (count);
            free_actors = new Stack<SongActor> (count);

            for (int i = 0; i < count; i++)
                free_actors.Push (InitSingle ("SongActor " + i));
        }

        public SongActor InitSingle (string name)
        {
            SongActor clone = new SongActor ();
            clone.SetPrototypeByColor (SongActor.Color.White);
            clone.AnchorPointFromGravity = Gravity.Center;
            clone.SetScaleWithGravity (1.0,1.0, Gravity.NorthWest);

            clone.Reactive = true;
            clone.Name = name;
            clone.Owner = null;
            clone.Hide ();
            //OLD event handler
//            clone.EnterEvent += delegate {
//                FireSongEnter (new SongHighlightArgs (x, y, clone.Name, cluster_list.Count));
//            };
//
//            clone.EnterEvent += delegate {
//                FireSongLeave (new SongHighlightArgs (x, y, clone.Name, cluster_list.Count));
//            };

            song_actors.Add (clone);

            return clone;
        }

        public SongActor AllocateAtPosition (SongPoint p)
        {
            if (free_actors.Count < 1)
                return null;

            SongActor actor = free_actors.Pop ();
            if (!p.Selected) {
                actor.SetPrototypeByColor (SongActor.Color.White);
            } else {
                actor.SetPrototypeByColor (SongActor.Color.Red);
                Hyena.Log.Information ("Point selected");
            }

            actor.SetPosition ((float)p.X, (float)p.Y);
            actor.Opacity = 255;
            actor.Owner = p;
            actor.Show ();

            return actor;
        }

        public void Free (SongActor actor)
        {
            actor.Hide ();
            actor.Owner = null;
            free_actors.Push (actor);
        }

        public List<SongActor> Actors {
            get {return song_actors;}
        }

        public bool HasFree {
            get { return free_actors.Count > 0; }
        }
    }
}

