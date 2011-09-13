/* NOTES:
 This is a modified version of http://quadtree.svn.sourceforge.net/ written
 by John McDonald and Gary Texmo.
 */

using System.Collections;
using System.Collections.Generic;
using System;

namespace Banshee.Cluttertest
{
    /// <summary>
    /// This interface is used enable storage of data
    /// </summary>
	public interface IStorable
    {
        //XY Coordinates are used to store objects
        Point XY { get; }

       // bool NeedsUpdate { get; }
    }

    /// <summary>
    /// Wrapper class which connects a IStorable object with an owner node in
    /// the tree.
    /// </summary>
    public class QuadTreeObject<T> where T : IStorable
    {
        /// <summary>
        /// The storable object
        /// </summary>
        public T Value {
            get;
            private set;
        }

        /// <summary>
        /// The owner node
        /// </summary>
        public QuadTreeNode<T> Owner
        {
            get;
            set;
        }

        public QuadTreeObject (T val)
        {
            Value = val;
        }
    }


    /// <summary>
    /// The main QuadTree class which handles Insertion, Deletion and Retrieval of objects.
    /// </summary>
    public class QuadTree<T> where T : IStorable
    {
        private readonly Dictionary<T, QuadTreeObject<T>> dictionary
                                            = new Dictionary<T, QuadTreeObject<T>> ();

        private readonly QuadTreeNode<T> quadTreeRoot;

        public QuadTree (QRectangle rect)
        {
            quadTreeRoot = new QuadTreeNode<T> (rect);
        }

        public QuadTree (double x, double y, double width, double height)
        {
            Hyena.Log.Debug ("New quad tree at " + new Point (x,y) + " width: " + width + " height: " + height);
            quadTreeRoot = new QuadTreeNode<T> (x,y,width,height);
        }

        public QRectangle Rectangle {
            get { return quadTreeRoot.Rectangle; }
        }

        public void Add (T item)
        {
            QuadTreeObject<T> wrappedItem = new QuadTreeObject<T> (item);

            dictionary.Add (item, wrappedItem);
            //Hyena.Log.Debug ("Add item at (" + item.XY.X + "," + item.XY.Y + ")");
            quadTreeRoot.Insert (wrappedItem);
        }

        public event OnCreateQuadEvent OnCreateQuad {
            add { create_quad_handler += value; }
            remove { create_quad_handler -= value; }
        }

        internal static OnCreateQuadEvent create_quad_handler;
    }

    #region Handler

    public class OnCreateQuadArgs
    {
        public QRectangle Rectangle {
            get;
            private set;
        }

        public int Level {
            get;
            private set;
        }

        public OnCreateQuadArgs (QRectangle rect, int level)
        {
            Rectangle = rect;
            Level = level;
        }
    }

    public delegate void OnCreateQuadEvent (OnCreateQuadArgs args);

    #endregion

    /// <summary>
    /// This class represents a node in the quadtree
    /// </summary>
    public class QuadTreeNode<T> where T : IStorable
    {

        //number of maximum objects in a node. if exceeded subdivision is performed
        private const int MAX_OBJECTS = 20;
        private const int MAX_LEVEL = 20;
        private int level = 0;

        //parent node
        private QuadTreeNode<T> parent = null;
        //child nodes
        private QuadTreeNode<T> top_left = null;
        private QuadTreeNode<T> bottom_left = null;
        private QuadTreeNode<T> top_right = null;
        private QuadTreeNode<T> bottom_right = null;

        //list of stored objects
        private List<QuadTreeObject<T>> objects = new List<QuadTreeObject<T>> ();

        //rectangle which represents the node geometrically
        private QRectangle boundary_rectangle;

        public int Level {
            get { return level; }
        }

        public QRectangle Rectangle {
            get { return boundary_rectangle; }
        }

        public QuadTreeNode<T> TopLeft {
            get { return top_left; }
        }

        public QuadTreeNode<T> TopRight {
            get { return top_right; }
        }

        public QuadTreeNode<T> BottomRight {
            get { return bottom_right; }
        }

        public QuadTreeNode<T> BottomLeft {
            get { return bottom_left; }
        }

        public List<QuadTreeObject<T>> Objects {
            get { return objects; }
        }

        public QuadTreeNode (QRectangle rect) {
            this.boundary_rectangle = rect;
        }

        public QuadTreeNode (double x, double y, double width, double height)
        {
            this.boundary_rectangle = new QRectangle (x, y, width, height);

            if (QuadTree<T>.create_quad_handler != null)
                QuadTree<T>.create_quad_handler (new OnCreateQuadArgs (boundary_rectangle,this.level));
        }

        private QuadTreeNode (QuadTreeNode<T> parent, double x, double y, double width, double height)
            :this (parent, new QRectangle (x, y, width, height))
        {
        }

        private QuadTreeNode (QuadTreeNode<T> parent, QRectangle rect)
        {
            this.parent = parent;
            this.boundary_rectangle = rect;
            this.level = parent.Level+1;

            if (QuadTree<T>.create_quad_handler != null)
                QuadTree<T>.create_quad_handler (new OnCreateQuadArgs (boundary_rectangle,this.level));
        }

        /// <summary>
        /// Adds an object to the node. This node is the owner of the object.
        /// </summary>
        /// <param name="item">
        /// A <see cref="QuadTreeObject<T>"/>
        /// </param>
        private void Add (QuadTreeObject<T> item)
        {
            item.Owner = this;
            objects.Add (item);
        }

        /// <summary>
        /// Removes an object from the node.
        /// </summary>
        /// <param name="item">
        /// A <see cref="QuadTreeObject<T>"/>
        /// </param>
        private void Remove (QuadTreeObject<T> item)
        {
            objects.Remove (item);
        }


        /// <summary>
        /// Subdivides the node into 4 subnodes. All objects are assigned accordingly.
        /// </summary>
        private void Subdivide ()
        {
            double width_half = boundary_rectangle.Width/2;
            double height_half = boundary_rectangle.Height/2;

            double x_half = boundary_rectangle.X + width_half;
            double y_half = boundary_rectangle.Y + height_half;

            double x = boundary_rectangle.X;
            double y = boundary_rectangle.Y;

            double x_right = boundary_rectangle.TopRight.X;
            double y_top = boundary_rectangle.TopRight.Y;

            top_left = new QuadTreeNode<T> (this,
                               new QRectangle (new Point (x,y_half), new Point (x_half,y_top)));

            top_right = new QuadTreeNode<T> (this,
                               new QRectangle (new Point (x_half,y_half), new Point (x_right,y_top)));

            bottom_left = new QuadTreeNode<T> (this,
                               new QRectangle (new Point (x,y), new Point (x_half, y_half)));

            bottom_right = new QuadTreeNode<T> (this,
                               new QRectangle (new Point (x_half,y), new Point (x_right, y_half)));

           //TODO : sicherstellen dass keine löcher entstehen - sollt passen siehe rectangle

            //Assign objects to nodes
            for (int i=0; i < objects.Count; i++)
            {
                QuadTreeNode<T> node = GetDestNode (objects[i]);

                if (node != this) {

                    node.Insert (objects[i]);
                    objects.Remove (objects[i]);
                    i--;
                }
            }
        }

        /// <summary>
        /// Returns the destination node, i.e. the node which encapsulates the item.
        /// </summary>
        /// <param name="item">
        /// A <see cref="QuadTreeObject<T>"/>
        /// </param>
        /// <returns>
        /// A <see cref="QuadTreeNode<T>"/>
        /// </returns>
        private QuadTreeNode<T> GetDestNode (QuadTreeObject<T> item)
        {
            QuadTreeNode<T> dest = this;

            if (top_left.Rectangle.Contains (item.Value.XY)) {
                dest = top_left;
            }
            else if (top_right.Rectangle.Contains (item.Value.XY)) {
                dest = top_right;
            }
            else if (bottom_left.Rectangle.Contains (item.Value.XY)) {
                dest = bottom_left;
            }
            else if (bottom_right.Rectangle.Contains (item.Value.XY)) {
                dest = bottom_right;
            }

            return dest;
        }


        /// <summary>
        /// Inserts an item into this quad
        /// </summary>
        /// <param name="item">
        /// A <see cref="QuadTreeObject<T>"/>
        /// </param>
        internal void Insert (QuadTreeObject<T> item)
        {
            if (!boundary_rectangle.Contains (item.Value.XY)) {     //Item not contained in this (sub)tree

                if (parent == null) {     //if root, add and warn (should not happen)
                    Add (item);
                    Hyena.Log.Warning ("Added item is outside tree boundaries");
                } else {       //do nothing
                    return;
                }
            }

            if ((top_left == null && objects.Count < MAX_OBJECTS ) || Level >= MAX_LEVEL) {
                //if enough space (no subdivision) or max level reached
                Add (item);     //simply add
            } else {

                if (top_left == null)   //if not subdivided
                    Subdivide ();

                QuadTreeNode<T> dest = GetDestNode (item);  //find destination node

                if (dest == this)   //should not happen in our case -> we have only points to add
                    Add (item);
                else
                    dest.Insert (item);     //add

            }

        }

        /// <summary>
        /// Gets all objects of this subtree.
        /// </summary>
        /// <param name="results">
        /// A <see cref="List<T>"/>
        /// </param>
        public void GetAllObjects (ref List<T> results)
        {
            if (results == null)
                return;

            if (objects != null) {          //add all objects in this node
                foreach (QuadTreeObject<T> item in objects)
                    results.Add (item.Value);
            }

            if (top_left != null) {         //add all objects in child nodes
                top_left.GetAllObjects (ref results);
                top_right.GetAllObjects (ref results);
                bottom_left.GetAllObjects (ref results);
                bottom_right.GetAllObjects (ref results);
            }
        }

        /// <summary>
        /// Gets all objects in this subtree which are in the specified search area
        /// </summary>
        /// <param name="rect">
        /// A <see cref="QRectangle"/>
        /// </param>
        /// <param name="results">
        /// A <see cref="List<T>"/>
        /// </param>
        public void GetObjects (QRectangle rect, ref List<T> results)
        {
            if (results == null)
                return;

            if (rect.Contains (boundary_rectangle)) {

                // If whole quad in search area add all items of subtree
                GetAllObjects (ref results);

            } else if (rect.Intersects (boundary_rectangle)) {

                // For every item in this quad check if it is contained in search area
                foreach (QuadTreeObject<T> item in objects) {

                    if (rect.Contains (item.Value.XY))
                        results.Add (item.Value);
                }

                if (top_left != null) {

                    //check all child quads
                    top_left.GetObjects (rect, ref results);
                    top_right.GetObjects (rect, ref results);
                    bottom_left.GetObjects (rect, ref results);
                    bottom_right.GetObjects (rect, ref results);
                }

            }
        }

        /// <summary>
        /// Gets all objects in this subtree which are in the specified circular search area
        /// </summary>
        /// <param name="circle">
        /// A <see cref="QCircle"/>
        /// </param>
        /// <param name="results">
        /// A <see cref="List<T>"/>
        /// </param>
        public void GetObjects (QCircle circle, ref List<T> results)
        {
            if (results == null)
                return;

            if (circle.Contains (boundary_rectangle)) {

                GetAllObjects (ref results);

            } else if (boundary_rectangle.Intersects (circle)) {

                foreach (QuadTreeObject<T> item in objects) {

                    if (circle.Contains (item.Value.XY))
                        results.Add (item.Value);
                }

                if (top_left != null) {
                    top_left.GetObjects (circle, ref results);
                    top_right.GetObjects (circle, ref results);
                    bottom_left.GetObjects (circle, ref results);
                    bottom_right.GetObjects (circle, ref results);
                }
            }
        }
    }
    #region helper classes

    /// <summary>
    /// Helper class wich is used as geometric representation of a node
    /// </summary>
    public class QRectangle
    {

        private Point bottom_left;
        private Point top_right;


        public QRectangle (double x, double y, double width, double height)
        {
            this.bottom_left = new Point (x,y);
            this.top_right = new Point (x+width, y+height);
        }

        public QRectangle (Point bottom_left, Point top_right)
        {
            this.bottom_left = bottom_left;
            this.top_right = top_right;
        }

        public double Width {
            get { return top_right.X - bottom_left.X; }
        }

        public double Height {
            get { return top_right.Y - bottom_left.Y; }
        }

        public Point TopRight {
            get { return top_right; }
        }

        public Point BottomLeft {
            get { return bottom_left; }
        }

        public Point Center {
            get { return new Point (X + Width/2, Y + Height /2); }
        }

        public double X {
            get { return bottom_left.X; }
        }

        public double Y {
            get { return bottom_left.Y; }
        }

        /// <summary>
        /// Checks if a point is inside the rectangle
        /// </summary>
        /// <param name="p">
        /// A <see cref="Point"/>
        /// </param>
        /// <returns>
        /// A <see cref="System.Boolean"/>
        /// </returns>
        public bool Contains (Point p)
        {
            return p.X <= top_right.X && p.X >= bottom_left.X &&
                   p.Y <= top_right.Y && p.Y >= bottom_left.Y;
        }

        /// <summary>
        /// Checks if another rectangle is contained in this rectangle
        /// </summary>
        /// <param name="rect">
        /// A <see cref="QRectangle"/>
        /// </param>
        /// <returns>
        /// A <see cref="System.Boolean"/>
        /// </returns>
        public bool Contains (QRectangle rect)
        {

            return X <= rect.X && top_right.Y >= rect.top_right.Y &&
                    top_right.X >= rect.top_right.X && Y <= rect.Y;
        }

        /// <summary>
        /// Checks if a circle is contained in this rectangle
        /// </summary>
        /// <param name="circle">
        /// A <see cref="QCircle"/>
        /// </param>
        /// <returns>
        /// A <see cref="System.Boolean"/>
        /// </returns>
        public bool Contains (QCircle circle)
        {
            return X <= circle.X - circle.Radius && top_right.X >= circle.X + circle.Radius &&
                    Y <= circle.Y - circle.Radius && top_right.Y >= circle.Y + circle.Radius;
        }

        /// <summary>
        /// Checks if another rectangle intersects with this rectangle
        /// </summary>
        /// <param name="rect">
        /// A <see cref="QRectangle"/>
        /// </param>
        /// <returns>
        /// A <see cref="System.Boolean"/>
        /// </returns>
        public bool Intersects (QRectangle rect)
        {
            //check if rect is completely to the left, right, bottom, top - else the rectangles intersect
            return !(rect.top_right.X < X || rect.X > top_right.X ||
                     rect.top_right.Y < Y || rect.Y > top_right.Y);
        }

        /// <summary>
        /// Checks if a given circle intersects with this rectangle
        /// </summary>
        /// <param name="circle">
        /// A <see cref="QCircle"/>
        /// </param>
        /// <returns>
        /// A <see cref="System.Boolean"/>
        /// </returns>
        public bool Intersects (QCircle circle)
        {
            // check in which region around the rectangle the circle center lies
            // 0 - bottom/left, 1 - center, 2 - top/right
            int x = circle.X < X ? 0 : (circle.X > top_right.X ? 2 : 1);
            int y = circle.Y < Y ? 0 : (circle.Y > top_right.Y ? 2 : 1);

            int zone = x + 3 * y;

            switch (zone) {

            //bottom center
            case 1:
                return Y - circle.Y <= circle.Radius;

            //top center
            case 7:
                return circle.Y - top_right.Y <= circle.Radius;

            //left center
            case 3:
                return X - circle.X <= circle.Radius;

            //right center
            case 5:
                return circle.X - top_right.X <= circle.Radius;

            //circle in the center
            case 4:
                return true;

            //corner zones - check if corner is inside the circle
            default:
                double cx = (zone == 0 || zone == 6) ? X : top_right.X;
                double cy = (zone == 0 || zone == 2) ? Y : top_right.Y;

                return circle.Contains (new Point (cx, cy));
            }
        }
    }

    public class QCircle
    {
        public QCircle (Point center, double radius)
        {
            Center = center;
            Radius = radius;
        }

        public QCircle (double x, double y, double radius) :
                                this (new Point (x,y), radius)
        {
        }

        public Point Center {
            get;
            private set;
        }

        public double Radius {
            get;
            private set;
        }

        public double X {
            get { return Center.X; }
        }

        public double Y {
            get { return Center.Y; }
        }

        public bool Contains (Point p)
        {
            return Center.DistanceTo (p) <= Radius;
        }

        public bool Contains (QRectangle rect)
        {
            return Center.DistanceTo (rect.BottomLeft) <= Radius &&
                    Center.DistanceTo (rect.TopRight) <= Radius &&
                    Center.DistanceTo (new Point (rect.X, rect.TopRight.Y)) <= Radius &&
                    Center.DistanceTo (new Point (rect.TopRight.X, rect.Y)) <= Radius;
        }
    }

    #endregion

}