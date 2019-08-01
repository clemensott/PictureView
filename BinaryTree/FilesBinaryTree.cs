using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PictureView
{
    class FilesBinaryTree : BinaryTree<string, string>
    {
        public string DirectoryPath { get; }

        private FilesBinaryTree(string path)
        {
            DirectoryPath = path;
        }

        public static FilesBinaryTree GetFilesTree(string path)
        {
            FilesBinaryTree tree = new FilesBinaryTree(path);

            try
            {
                string[] files = Directory.GetFiles(path);

                AddChildren(tree, files, 0, files.Length);
            }
            catch { }

            return tree;
        }

        private static void AddChildren(FilesBinaryTree tree, string[] values, int begin, int end)
        {
            if (begin >= end) return;

            int middle = (begin + end) / 2;
            string path = values[middle];

            tree.Add(path.ToLower(), path);
            AddChildren(tree, values, begin, middle);
            AddChildren(tree, values, middle + 1, end);
        }
    }
}
