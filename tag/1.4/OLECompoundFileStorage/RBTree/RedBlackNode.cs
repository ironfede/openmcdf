///<summary>
/// The RedBlackNode class encapsulates a node in the tree
///</summary>

using System;
using System.Text;

namespace RBTree
{
	public class RedBlackNode
	{
        // tree node colors
		public static int	RED		= 0;
		public static int	BLACK	= 1;

		// key provided by the calling class
		private IComparable ordKey;
		// the data or value associated with the key
		private object objData;
		// color - used to balance the tree
		private int intColor;
		// left node 
		private RedBlackNode rbnLeft;
		// right node 
		private RedBlackNode rbnRight;
        // parent node 
        private RedBlackNode rbnParent;
		
		///<summary>
		///Key
		///</summary>
		public IComparable Key
		{
			get
            {
				return ordKey;
			}
			
			set
			{
				ordKey = value;
			}
		}
		///<summary>
		///Data
		///</summary>
		public object Data
		{
			get
            {
				return objData;
			}
			
			set
			{
				objData = value;
			}
		}
		///<summary>
		///Color
		///</summary>
		public int Color
		{
			get
            {
				return intColor;
			}
			
			set
			{
				intColor = value;
			}
		}
		///<summary>
		///Left
		///</summary>
		public RedBlackNode Left
		{
			get
            {
				return rbnLeft;
			}
			
			set
			{
				rbnLeft = value;
			}
		}
		///<summary>
		/// Right
		///</summary>
		public RedBlackNode Right
		{
			get
            {
				return rbnRight;
			}
			
			set
			{
				rbnRight = value;
			}
		}
        public RedBlackNode Parent
        {
            get
            {
                return rbnParent;
            }
			
            set
            {
                rbnParent = value;
            }
        }

		public RedBlackNode()
		{
			Color = RED;
		}
	}
}
