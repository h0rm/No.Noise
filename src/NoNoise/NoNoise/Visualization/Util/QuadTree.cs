/* NOTES:
 This is a modified version of http://quadtree.svn.sourceforge.net/ written
 by John McDonald and Gary Texmo.
 */

using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

namespace NoNoise.Visualization.Util
{
    /// <summary>
    /// This interface is used enable storage of data
    /// </summary>
	public interface IStorable<T>
    {
        //XY Coordinates are used to store objects
        /// <summary>
        /// Point coordinates of the stored object
        /// </summary>
        Point XY { get; }

        /// <summary>
        /// Returns a merged parent object.
        /// </summary>
        /// <param name="a">
        /// A <see cref="T"/>
        /// </param>
        /// <returns>
        /// A <see cref="T"/>
        /// </returns>
        T GetMerged (T a);
       // bool NeedsUpdate { get; }
    }


    /// <summary>
    /// Wrapper class which connects a IStorable object with an owner node in
    /// the tree.
    /// </summary>
    public class QuadTreeObject<T> where T : IStorable<T>
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
    public class QuadTree<T> : ICloneable where T : IStorable<T>
    {
        private readonly Dictionary<T, QuadTreeObject<T>> dictionary
                                            = new Dictionary<T, QuadTreeObject<T>> ();

        private readonly QuadTreeNode<T> quadTreeRoot;


        private QuadTree (QuadTree<T> tree) : this (tree.Rectangle)
        {
            //Create new Tree with the same items
            foreach (T item in tree.dictionary.Keys)
                Add (item);
        }

        public QuadTree (QRectangle rect)
        {
            quadTreeRoot = new QuadTreeNode<T> (rect);
        }

        public QuadTree (double x, double y, double width, double height)
        {
//            Hyena.Log.Debug ("New quad tree at " + new Point (x,y) + " width: " + width + " height: " + height);
            quadTreeRoot = new QuadTreeNode<T> (x,y,width,height);
        }

        public QRectangle Rectangle {
            get { return quadTreeRoot.Rectangle; }
        }

        public int Count {
            get { return dictionary.Count; }
        }

        /// <summary>
        /// Returns all items stored in the quadtree.
        /// </summary>
        /// <returns>
        /// A <see cref="List<T>"/>
        /// </returns>
        public List<T> GetAllObjects ()
        {
            return new List<T> (dictionary.Keys);
        }

        /// <summary>
        /// Adds an item to the quadtree.
        /// </summary>
        /// <param name="item">
        /// A <see cref="T"/>
        /// </param>
        public void Add (T item)
        {
            QuadTreeObject<T> wrappedItem = new QuadTreeObject<T> (item);

            dictionary.Add (item, wrappedItem);
            //Hyena.Log.Debug ("Add item at (" + item.XY.X + "," + item.XY.Y + ")");
            quadTreeRoot.Insert (wrappedItem);
        }

        /// <summary>
        /// Removes an item from the quadtree.
        /// </summary>
        /// <param name="item">
        /// A <see cref="T"/>
        /// </param>
        /// <returns>
        /// A <see cref="System.Boolean"/>
        /// </returns>
        public bool Remove (T item)
        {
            if (dictionary.ContainsKey(item))
            {
                quadTreeRoot.Delete (dictionary[item]);
                dictionary.Remove (item);
                return true;
            } else {
                return false;
            }
        }

        /// <summary>
        /// Clears the quadtree
        /// </summary>
        public void Clear ()
        {
            dictionary.Clear ();
            quadTreeRoot.Clear();
        }

        /// <summary>
        /// Returns a clone of this quadtree.
        /// </summary>
        /// <returns>
        /// A <see cref="System.Object"/>
        /// </returns>
        public object Clone ()
        {
            return new QuadTree<T> (this);
        }

        /// <summary>
        /// Gets all objects in the tree which are in the search area
        /// </summary>
        /// <param name="circle">
        /// A <see cref="QCircle"/>
        /// </param>
        /// <returns>
        /// A <see cref="List<T>"/>
        /// </returns>
        public List<T> GetObjects (QCircle circle)
        {
            return quadTreeRoot.GetObjects (circle);
        }

        /// <summary>
        /// Gets all objects in the tree which are in the search area
        /// </summary>
        /// <param name="rect">
        /// A <see cref="QRectangle"/>
        /// </param>
        /// <returns>
        /// A <see cref="List<T>"/>
        /// </returns>
        public List<T> GetObjects (QRectangle rect)
        {
            return quadTreeRoot.GetObjects (rect);
        }

        /// <summary>
        /// Returns the item which is nearest to the given item.
        /// </summary>
        /// <param name="item">
        /// A <see cref="T"/>
        /// </param>
        /// <param name="start_radius">
        /// A <see cref="System.Double"/> which specifies the maximum search radius.
        /// </param>
        /// <returns>
        /// A <see cref="T"/>
        /// </returns>
        public T GetNearest (T item, double start_radius)
        {
            return quadTreeRoot.GetNearestObjectInTree (new QCircle (item.XY, start_radius), item);
        }

        /// <summary>
        /// Calculates the optimal window dimensions for a given number of points.
        /// </summary>
        /// <param name="num_of_points">
        /// A <see cref="System.Int32"/> which specifies the maximum number of points in the window.
        /// </param>
        /// <param name="width">
        /// A <see cref="System.Double"/>
        /// </param>
        /// <param name="height">
        /// A <see cref="System.Double"/>
        /// </param>
        public void GetWindowDimesions (int num_of_points, out double width, out double height)
        {
            quadTreeRoot.GetWindow (num_of_points, out width, out height);
        }

        private class Neighbours<S> : IComparable<Neighbours<S>> where S : IStorable<S>
        {
            public S First {
                get;
                set;
            }

            public S Second {
                get;
                set;
            }

            public double Distance {
                get {
                    if (Second == null)
                        return double.MaxValue;

                    return First.XY.DistanceTo (Second.XY);
                }
            }

            public int CompareTo (Neighbours<S> other)
            {
                return Distance.CompareTo (other.Distance);
            }
        }

        /// <summary>
        /// Returns a new tree which is a clustered version of this tree. This is an advanced
        /// version, because the nearest neighbours are clustered first.
        /// </summary>
        /// <returns>
        /// A <see cref="QuadTree<T>"/>
        /// </returns>
        public QuadTree<T> GetAdvancedClusteredTree (double max_search_radius)
        {
            QuadTree<T> clustered_tree = new QuadTree<T> (quadTreeRoot.Rectangle);
            QuadTree<T> clone_tree = (QuadTree<T>)Clone ();

            List<T> items = clone_tree.GetAllObjects ();

            List<Neighbours<T>> neighbours = new List<Neighbours<T>> (clone_tree.Count);

            //initialize list
            foreach (T item in items) {
                T nearest = clone_tree.GetNearest (item, max_search_radius);

                neighbours.Add (new Neighbours<T>(){
                    First = item,
                    Second = nearest
                });
            }

            neighbours.Sort ();

            // List empty, abort
            if (neighbours.Count == 0)
                return clustered_tree;

            Neighbours<T> min = neighbours[0];

            // As long as objects in tree merge
            while (clone_tree.Count > 0) {

                items = clone_tree.GetAllObjects ();

                for (int i=0; i<neighbours.Count; i++)
                {
                    Neighbours<T> current = neighbours[i];

                    // If first is in removed connection, remove
                    if (current.First.Equals (min.First) || current.First.Equals(min.Second)) {
                        neighbours[i] = neighbours[neighbours.Count - 1];
                        neighbours.RemoveAt (neighbours.Count - 1);
                        i--;
                        continue;
                    }

                    // If second is removed, recalculate
                    if (current.Second.Equals (min.First) || current.Second.Equals (min.Second)) {
                        neighbours[i].Second = clone_tree.GetNearest (neighbours[i].First, max_search_radius);
                    }
                }

                if (neighbours.Count == 0)
                    break;

                neighbours.Sort ();

                min = neighbours [0];

                Hyena.Log.Debug ("Count " + clone_tree.Count + " Min distance : " + min.Distance);

                clone_tree.Remove (min.First);

                //add to new tree
                if (min.Second == null) {
                    clustered_tree.Add (min.First.GetMerged (default (T)));
                } else {
                    clone_tree.Remove (min.Second);
                    clustered_tree.Add (min.First.GetMerged (min.Second));
                }
            }

            clone_tree = null;
            return clustered_tree;
        }
        /// <summary>
        /// Returns a new tree which is a clustered version of this tree.
        /// </summary>
        /// <returns>
        /// A <see cref="QuadTree<T>"/>
        /// </returns>
        public QuadTree<T> GetClusteredTree (double max_search_radius)
        {
//            Hyena.Log.Debug ("Cluster Tree");
            QuadTree<T> clustered_tree = new QuadTree<T> (quadTreeRoot.Rectangle);
            QuadTree<T> clone_tree = (QuadTree<T>)Clone ();

            List<T> items = GetAllObjects ();
//            Hyena.Log.Debug ("Cluster " + items.Count + " items.");
            T other;

            foreach (T item in items) {
                if (clone_tree.Count == 0)
                    break;

                if (!clone_tree.dictionary.ContainsKey (item))
                    continue;

                clone_tree.Remove (item);
                other = clone_tree.GetNearest (item, max_search_radius);

                //check if item is found
                if (other == null) {

                    clustered_tree.Add (item.GetMerged (default (T)));

                } else {

                    clone_tree.Remove (other);
                    clustered_tree.Add (item.GetMerged (other));
                }
            }

            clone_tree = null;
//            Hyena.Log.Debug ("Clustered Tree count: " + clustered_tree.GetAllObjects().Count);
            return clustered_tree;
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
    public class QuadTreeNode<T> where T : IStorable<T>
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

        /// <summary>
        /// Depth level of this subtree
        /// </summary>
        public int Level {
            get { return level; }
        }

        /// <summary>
        /// Rectangle representing the boundaries of this subtree
        /// </summary>
        public QRectangle Rectangle {
            get { return boundary_rectangle; }
        }

        /// <summary>
        /// Topleft subtree.
        /// </summary>
        public QuadTreeNode<T> TopLeft {
            get { return top_left; }
        }

        /// <summary>
        /// Topright subtree.
        /// </summary>
        public QuadTreeNode<T> TopRight {
            get { return top_right; }
        }

        /// <summary>
        /// Bottomright subtree.
        /// </summary>
        public QuadTreeNode<T> BottomRight {
            get { return bottom_right; }
        }

        /// <summary>
        /// Bottomleft subtree.
        /// </summary>
        public QuadTreeNode<T> BottomLeft {
            get { return bottom_left; }
        }

        /// <summary>
        /// List of all stored objects in this node.
        /// </summary>
        public List<QuadTreeObject<T>> Objects {
            get { return objects; }
        }

        /// <summary>
        /// Returns true if this node has no children and no stored objects.
        /// </summary>
        public bool IsEmptyLeaf
        {
            get { return objects.Count == 0 && TopLeft == null; }
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
            int i = objects.IndexOf (item);
            if (i >= 0){
                objects[i] = objects[objects.Count-1];
                objects.RemoveAt (objects.Count-1);
            }
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
        /// Clean subtree upwards (i.e. check if empty).
        /// </summary>
        private void CleanUpwards ()
        {
            //if children, check if empty and clean
            if (top_left != null){

                if (top_left.IsEmptyLeaf && top_right.IsEmptyLeaf &&
                    bottom_left.IsEmptyLeaf && bottom_right.IsEmptyLeaf)
                {
                    top_left = null;
                    top_right = null;
                    bottom_left = null;
                    bottom_right = null;
                }
            }

            //tell parent to clean up
            if (parent != null && objects.Count == 0)
                parent.CleanUpwards ();
        }

        /// <summary>
        /// Clears this node.
        /// </summary>
        public void Clear ()
        {
            //Clear children
            if (top_left != null){
                top_left.Clear ();
                top_right.Clear ();
                bottom_left.Clear ();
                bottom_right.Clear ();
            }

            //clear objects
            if (objects.Count != 0){
                objects.Clear ();
            }

            //delete references
            top_left = null;
            top_right = null;
            bottom_left = null;
            bottom_right = null;
        }

        /// <summary>
        /// Deletes an object in this subtree and cleans up afterwards.
        /// </summary>
        /// <param name="item">
        /// A <see cref="QuadTreeObject<T>"/>
        /// </param>
        internal void Delete (QuadTreeObject<T> item)
        {
            if (item.Owner != null){
                //someone cares
                if (item.Owner == this){
                    Remove (item);
                    CleanUpwards ();
                } else {
                    item.Owner.Delete (item);
                }
            }
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
        /// Returns a list of all objects in this subtree.
        /// </summary>
        /// <returns>
        /// A <see cref="List<T>"/>
        /// </returns>
        public List<T> GetAllObjects ()
        {
            List<T> result = new List<T> ();
            GetAllObjects (ref result);
            return result;
        }

        /// <summary>
        /// Gets all objects in a subtree which are in the search area
        /// </summary>
        /// <param name="rect">
        /// A <see cref="QRectangle"/>
        /// </param>
        /// <returns>
        /// A <see cref="List<T>"/>
        /// </returns>
        public List<T> GetObjects (QRectangle rect)
        {
            List<T> result = new List<T> ();
            GetObjects (rect, ref result);
            return result;
        }

        /// <summary>
        /// Gets all objects in a subtree which are in the search area
        /// </summary>
        /// <param name="circle">
        /// A <see cref="QCircle"/>
        /// </param>
        /// <returns>
        /// A <see cref="List<T>"/>
        /// </returns>
        public List<T> GetObjects (QCircle circle)
        {
            List<T> result = new List<T> ();
            GetObjects (circle, ref result);
            return result;
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

        /// <summary>
        /// Returns optimal window dimensions for the given number of points.
        /// </summary>
        /// <param name="num_of_points"> which specifies the maximum number of visible points.
        /// A <see cref="System.Int32"/>
        /// </param>
        /// <param name="width">
        /// A <see cref="System.Double"/>
        /// </param>
        /// <param name="height">
        /// A <see cref="System.Double"/>
        /// </param>
        public void GetWindow (int num_of_points, out double width, out double height)
        {
            if (GetAllObjects().Count < num_of_points) {
                // All points fit in this window -> return
                width = boundary_rectangle.Width;
                height = boundary_rectangle.Height;

            } else {
                // Check which is the smallest window of the children which fits all points
                double current_w, current_h;

                top_left.GetWindow (num_of_points, out width, out height);

                top_right.GetWindow (num_of_points, out current_w, out current_h);
                width = current_w < width ? current_w : width;
                height = current_h < height ? current_h : height;

                bottom_left.GetWindow (num_of_points, out current_w, out current_h);
                width = current_w < width ? current_w : width;
                height = current_h < height ? current_h : height;

                bottom_right.GetWindow (num_of_points, out current_w, out current_h);
                width = current_w < width ? current_w : width;
                height = current_h < height ? current_h : height;
            }
        }

        /// <summary>
        /// Returns the nearest object to the given object in this subtree.
        /// </summary>
        /// <param name="circle">
        /// A <see cref="QCircle"/> which specifies the maximum search radius.
        /// </param>
        /// <param name="except">
        /// A <see cref="T"/> which specifies the given object.
        /// </param>
        /// <returns>
        /// A <see cref="T"/>
        /// </returns>
        public T GetNearestObjectInTree (QCircle circle, T except)
        {
            Stack<QuadTreeNode<T>> stack = new Stack<QuadTreeNode<T>> ();
            stack.Push (this);

            T result = default(T);

            while (stack.Count > 0) {
//                Hyena.Log.Information ("Circle at " + circle.Center + " size: " + circle.Radius);
                QuadTreeNode<T> current = stack.Pop ();

                // This node does not intersect with the search area
                if (!current.boundary_rectangle.Intersects (circle))
                    continue;


                if (current.TopLeft == null) {
                    // Leaf node
//                    Hyena.Log.Debug ("No children");
                    T found = current.GetNearestObject (ref circle, except);

                    if (found != null)
                        result = found;
                } else {
//                    Hyena.Log.Debug ("Children");
                    // Add children if intesecting
                    if (current.TopLeft.boundary_rectangle.Intersects (circle))
                        stack.Push (current.TopLeft);
                    if (current.TopRight.boundary_rectangle.Intersects (circle))
                        stack.Push (current.TopRight);
                    if (current.BottomLeft.boundary_rectangle.Intersects (circle))
                        stack.Push (current.BottomLeft);
                    if (current.BottomRight.boundary_rectangle.Intersects (circle))
                        stack.Push (current.BottomRight);
                }

            }
            return result;
        }

        /// <summary>
        /// Get nearest object to point in this node (no children)
        /// </summary>
        /// <param name="circle">
        /// A <see cref="QCircle"/>
        /// </param>
        /// <returns>
        /// A <see cref="T"/>
        /// </returns>
        public T GetNearestObject (ref QCircle c, T except)
        {
            T result = default(T);

            //Leaf node
//            Hyena.Log.Information ("Objects " + objects.Count);

            foreach (QuadTreeObject<T> item in objects) {

                if (item.Value.Equals (except))
                    continue;

                double d = item.Value.XY.DistanceTo (c.Center);

                if (d < c.Radius) {
                    c.Radius = d;
                    result = item.Value;

                }
            }

            return result;
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

        /// <summary>
        /// Coordinates of the top-right corner.
        /// </summary>
        public Point TopRight {
            get { return top_right; }
        }

        /// <summary>
        /// Coordinates of the bottom-left corner.
        /// </summary>
        public Point BottomLeft {
            get { return bottom_left; }
        }

        /// <summary>
        /// Center point.
        /// </summary>
        public Point Center {
            get { return new Point (X + Width/2, Y + Height /2); }
        }

        /// <summary>
        /// X-coorinate of the bottom-left corner.
        /// </summary>
        public double X {
            get { return bottom_left.X; }
        }

        /// <summary>
        /// Y-coordinate of the bottom-left corner.
        /// </summary>
        public double Y {
            get { return bottom_left.Y; }
        }

        /// <summary>
        /// Checks if a point is inside the rectangle.
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
        /// Checks if another rectangle is contained in this rectangle.
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
        /// Checks if a circle is contained in this rectangle.
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
        /// Checks if another rectangle intersects with this rectangle.
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
        /// Checks if a given circle intersects with this rectangle.
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

    /// <summary>
    /// Helper class which is used as a geometric representation of a circular search area.
    /// </summary>
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

        /// <summary>
        /// Center point.
        /// </summary>
        public Point Center {
            get;
            private set;
        }

        public double Radius {
            get;
            set;
        }

        /// <summary>
        /// X-coordinate of the center point.
        /// </summary>
        public double X {
            get { return Center.X; }
        }

        /// <summary>
        /// Y-coordinate of the center point.
        /// </summary>
        public double Y {
            get { return Center.Y; }
        }

        /// <summary>
        /// Returns true if the given point is inside the search area.
        /// </summary>
        /// <param name="p">
        /// A <see cref="Point"/>
        /// </param>
        /// <returns>
        /// A <see cref="System.Boolean"/>
        /// </returns>
        public bool Contains (Point p)
        {
            return Center.DistanceTo (p) <= Radius;
        }

        /// <summary>
        /// Returns true if the given rectangle is contained in the search area. 
        /// </summary>
        /// <param name="rect">
        /// A <see cref="QRectangle"/>
        /// </param>
        /// <returns>
        /// A <see cref="System.Boolean"/>
        /// </returns>
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