using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PictureView
{
    class BinaryTreeNode<TKey, TValue>
    {
        public TKey Key { get; }

        public TValue Value { get; set; }

        public BinaryTreeNode<TKey, TValue> Parent { get; }

        public BinaryTreeNode<TKey, TValue> Left { get; set; }

        public BinaryTreeNode<TKey, TValue> Right { get; set; }

        public BinaryTreeNode(TKey key, TValue value, BinaryTreeNode<TKey, TValue> parent)
        {
            Key = key;
            Value = value;
            Parent = parent;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
