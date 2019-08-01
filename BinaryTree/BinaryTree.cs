using System;

namespace PictureView
{
    class BinaryTree<TKey, TValue> where TKey : IComparable<TKey>
    {
        private BinaryTreeNode<TKey, TValue> root;

        public void Add(TKey key, TValue value)
        {
            if (root == null)
            {
                root = new BinaryTreeNode<TKey, TValue>(key, value, null);
                return;
            }

            BinaryTreeNode<TKey, TValue> node = root;

            while (true)
            {
                if (key.CompareTo(node.Key) < 0)
                {
                    if (node.Left == null)
                    {
                        node.Left = new BinaryTreeNode<TKey, TValue>(key, value, node);
                        return;
                    }

                    node = node.Left;
                }
                else
                {
                    if (node.Right == null)
                    {
                        node.Right = new BinaryTreeNode<TKey, TValue>(key, value, node);
                        return;
                    }

                    node = node.Right;
                }
            }
        }

        public bool SetValue(TKey key, TValue newValue)
        {
            BinaryTreeNode<TKey, TValue> node = root;

            while (node != null)
            {
                switch (Math.Sign(key.CompareTo(node.Key)))
                {
                    case -1:
                        node = node.Left;
                        break;

                    case 0:
                        node.Value = newValue;
                        return true;

                    case 1:
                        node = node.Right;
                        break;
                }
            }

            return false;
        }

        public bool Contains(TKey key)
        {
            BinaryTreeNode<TKey, TValue> node = root;

            while (node != null)
            {
                switch (Math.Sign(key.CompareTo(node.Key)))
                {
                    case -1:
                        node = node.Left;
                        break;

                    case 0:
                        return true;

                    case 1:
                        node = node.Right;
                        break;
                }
            }

            return false;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            BinaryTreeNode<TKey, TValue> node = root;

            while (node != null)
            {
                switch (Math.Sign(key.CompareTo(node.Key)))
                {
                    case -1:
                        node = node.Left;
                        break;

                    case 0:
                        value = node.Value;
                        return true;

                    case 1:
                        node = node.Right;
                        break;
                }
            }

            value = default(TValue);
            return false;
        }

        public bool TryGetMinimum(out TValue minimumValue)
        {
            minimumValue = default(TValue);

            if (root == null) return false;

            BinaryTreeNode<TKey, TValue> node = root;

            while (node.Left != null) node = node.Left;

            minimumValue = node.Value;
            return true;
        }

        public bool TryGetMaximum(out TValue maximumValue)
        {
            maximumValue = default(TValue);

            if (root == null) return false;

            BinaryTreeNode<TKey, TValue> node = root;

            while (node.Right != null) node = node.Right;

            maximumValue = node.Value;
            return true;
        }

        public bool TryGetSuccessor(TKey key, out TValue successorValue)
        {
            bool foundSuccessor = false;
            BinaryTreeNode<TKey, TValue> node = root;
            TKey successorKey = default(TKey);
            successorValue = default(TValue);

            while (node != null)
            {
                if (key.CompareTo(node.Key) < 0)
                {
                    if (!foundSuccessor || successorKey.CompareTo(node.Key) > 0)
                    {
                        successorKey = node.Key;
                        successorValue = node.Value;
                        foundSuccessor = true;
                    }

                    node = node.Left;
                }
                else node = node.Right;
            }

            return foundSuccessor;
        }

        public bool TryGetPredecessor(TKey key, out TValue predecessorValue)
        {
            bool foundPredecessor = false;
            BinaryTreeNode<TKey, TValue> node = root;
            TKey predecessorKey = default(TKey);
            predecessorValue = default(TValue);

            while (node != null)
            {
                if (key.CompareTo(node.Key) > 0)
                {
                    if (!foundPredecessor || predecessorKey.CompareTo(node.Key) < 0)
                    {
                        predecessorKey = node.Key;
                        predecessorValue = node.Value;
                        foundPredecessor = true;
                    }

                    node = node.Right;
                }
                else node = node.Left;
            }

            return foundPredecessor;
        }
    }
}
