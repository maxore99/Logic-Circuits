using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainProgram
{
    class TreeNode
    {
        private string ID; // The ID represents the content of each node
        private TreeNode left, right; // The left and right nodes are the branches from this node
        private bool isNOT;

        public TreeNode(string content) // This constructor is used to add pins to a tree
        {
            ID = content;
            left = right = null;
        }

        public TreeNode(string content, TreeNode newRight, TreeNode newLeft) // This constructor is used when generating a tree from a string, in which case the right branch should be executed first
        {
            ID = content;
            left = newLeft;
            right = newRight;
        }

        public TreeNode(string content, bool NOT, TreeNode newLeft, TreeNode newRight) // This constructor is used when generating a tree from a circuit, in which case the left branch should be executed first
        {
            ID = content;
            left = newLeft;
            right = newRight;
            isNOT = NOT;
        }

        public int getDepth()
        {
            if(left == null && right == null) // If an end node, return depth 1
            {
                return 1;
            }
            else if(left == null) // If left is null but right isn't, something is wrong. Return depth 0
            {
                return 0;
            }
            else if(right == null || left.getDepth() > right.getDepth()) // If right is null or left is deeper than right, return 1 + the depth of the left node
            {
                return 1 + left.getDepth();
            }
            else // If right is deeper than left, return 1 + the depth of the right node
            {
                return 1 + right.getDepth();
            }
        }

        public List<string> inOrderTraverse() // This is an in-order traversal of the tree with some extra steps to make NOT gates with only one input work nicely
        {
            List<string> output = new List<string>();

            if (right == null && Convert.ToInt32(ID) > 0 || isNOT) // If this node represents a NOT gate
            {
                output.Add(")"); // Add a 0 to represent a closed bracket
            }

            if (left != null) // If a left node exists
            {
                foreach (string element in left.inOrderTraverse()) // Repeat the traversal for the left node
                {
                    output.Add(element); // Add each element from the left branch
                }
            }

            output.Add(Convert.ToString(ID)); // Add the ID of this node

            if (right != null && !isNOT) // If a right node exists and this is not the NOT form of a gate
            {
                foreach (string element in right.inOrderTraverse()) // Repeat the traversal for the right node
                {
                    output.Add(element); // Add each element from the right branch
                }
            }
            else if(right != null && isNOT)
            {
                foreach (string element in right.inOrderTraverse()) // Repeat the traversal for the right node
                {
                    output.Add(element); // Add each element from the right branch
                }
                output.Add("¬"); // Add a NOT symbol to indicate a NOT gate
            }

            return output; // Return the output list
        }

        public TreeNode getLeft()
        {
            return left;
        }

        public TreeNode getRight()
        {
            return right;
        }

        public string getID()
        {
            return ID;
        }
    }
}
