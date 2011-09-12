/* NOTES:
 This is a modified version of http://quadtree.svn.sourceforge.net/ written
 by John McDonald and Gary Texmo.
 */

using System.Collections;
using System.Collections.Generic;

namespace Banshee.Cluttertest
{
    /// <summary>
    /// This interface is used enable storage of data
    /// </summary>
	public interface IStorable
    {
        //XY Coordinates are used to store objects
        Point XY { get; }

        bool NeedsUpdate { get; }
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

        public QuadTree (Point center, double width, double height)
        {
            quadTreeRoot = new QuadTreeNode<T> (new QRectangle (center,width,height));
        }

        public QRectangle Rectangle {
            get { return quadTreeRoot.Rectangle; }
        }

        public void Add (T item)
        {
            QuadTreeObject<T> wrappedItem = new QuadTreeObject<T> (item);

            dictionary.Add (item, wrappedItem);
            ///Inserv fehlt no
        }
    }

    /// <summary>
    /// This class represents a node in the quadtree
    /// </summary>
    public class QuadTreeNode<T> where T : IStorable
    {
        //number of maximum objects in a node. if exceeded subdivision is performed
        private const int MAX_OBJECTS = 4;

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

        public QuadTreeNode (Point center, double width, double height)
        {
            this.boundary_rectangle = new QRectangle (center, width, height);
        }

        private QuadTreeNode (QuadTreeNode<T> parent, QRectangle rect)
        {
            this.parent = parent;
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
            Point half = new Point (boundary_rectangle.Width/2, boundary_rectangle.Height/2);
            Point quad = new Point (boundary_rectangle.Width/4, boundary_rectangle.Height/4);

            //New child nodes
            top_left = new QuadTreeNode<T> (this,
                                  new QRectangle (boundary_rectangle.X-quad.X,boundary_rectangle.Y+quad.Y, half.X, half.Y));
            top_right = new QuadTreeNode<T> (this,
                                  new QRectangle (boundary_rectangle.X+quad.X, boundary_rectangle.Y+quad.Y, half.X, half.Y));
            bottom_left = new QuadTreeNode<T> (this,
                                  new QRectangle (boundary_rectangle.X-quad.X,boundary_rectangle.Y-quad.Y, half.X, half.Y));
            bottom_right = new QuadTreeNode<T> (this,
                                  new QRectangle (boundary_rectangle.X+quad.X,boundary_rectangle.Y-quad.Y, half.X, half.Y));

            //TODO : sicherstellen dass keine löcher entstehen - sollt passen siehe rectangle

            //Assign objects to nodes
            for (int i=0; i < objects.Count; i++)
            {
                QuadTreeNode<T> node = GetDestNode (objects[i]);

                if (node != this) {

                    node.Add (objects[i]);      //passt noch nicht ganze - Insert statt Add
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
        }

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

            if (top_left == null && objects.Count < MAX_OBJECTS ) {   //if enough space (no subdivision)
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
    }
    #region helper classes

    /// <summary>
    /// Helper class wich is used as geometric representation of a node
    /// </summary>
    public class QRectangle
    {
        // to circumvent rounding problems this number is used
        private const double E = 0.000001;

        private double height, width;
        private Point center;

        public QRectangle (Point center, double width, double height)
        {
            this.center = center;
            this.width = width;
            this.height = height;
        }

        public QRectangle (double x, double y, double width, double height)
                                                :this (new Point (x,y),width,height)
        {
        }

        public double Width {
            get { return width; }
        }

        public double Height {
            get { return height; }
        }

        public Point Center {
            get { return center; }
        }

        public double X {
            get { return center.X; }
        }

        public double Y {
            get { return center.Y; }
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
            double h_y = Height /2;
            double h_x = Width /2;

            return p.X <= X + h_x + E && p.X >= X - h_x - E &&
                   p.Y <= Y + h_y + E && p.Y >= Y - h_y -E;
        }
    }

    public class QCircle
    {
        private Point center;
        private double radius;

        public QCircle (Point center, double radius)
        {
            this.center = center;
            this.radius = radius;
        }

        public Point Center {
            get { return center; }
        }

        public double Radius {
            get { return radius; }
        }
    }

    #endregion

}