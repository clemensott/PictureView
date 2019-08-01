using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PictureView
{
    class FileSystem
    {
        private readonly string rootPath;
        private readonly FileSystemNode root;

        private FileSystem(FileSystemNode root)
        {
            this.root = root;
            rootPath = root.FullName;

            if (rootPath.Last() != '\\') rootPath += '\\';
        }

        public async Task<string> GetFileSuccessor(string path)
        {
            FileSystemNode node;
            FilesBinaryTree filesTree;
            string successorPath;
            string directory = GetParent(path);

            node = await GetNode(directory);

            if (node != null)
            {
                successorPath = await node.GetSuccessorFile(path);

                if (successorPath != null) return successorPath;

                node = await GetSuccessorNode(node);
            }
            else node = await GetSuccessorNode(directory);

            while (node != null)
            {
                filesTree = await node.FilesTask;

                if (filesTree.TryGetMinimum(out successorPath)) return successorPath;

                node = await GetSuccessorNode(node);
            }

            filesTree = await root.FilesTask;

            return filesTree.TryGetMinimum(out successorPath) ? successorPath : null;
        }

        private async Task<FileSystemNode> GetSuccessorNode(string path)
        {
            FileSystemNode successor;
            DirectoriesBinaryTree tree;
            FileSystemNode node = root;

            foreach (string step in Utils.GetPathSteps(node.FullName, GetParent(path)))
            {
                tree = await node.ChildrenTask;

                if (!tree.TryGetValue(step.ToLower(), out node)) return null;
            }

            tree = await node.ChildrenTask;
            if (tree.TryGetSuccessor(path.ToLower(), out successor)) return successor;

            return await GetSuccessorNode(node);
        }

        private async Task<FileSystemNode> GetSuccessorNode(FileSystemNode node)
        {
            FileSystemNode successor;
            DirectoriesBinaryTree tree = await node.ChildrenTask;

            if (tree.TryGetMinimum(out successor)) return successor;

            while (node.Parent != null)
            {
                tree = await node.Parent.ChildrenTask;

                if (tree.TryGetSuccessor(node.FullName, out successor)) return successor;

                node = node.Parent;
            }

            return null;
        }

        public async Task<string> GetFilePredecessor(string path)
        {
            FileSystemNode node;
            FilesBinaryTree filesTree;
            string predecessorPath;
            string directory = GetParent(path);

            node = await GetNode(directory);

            if (node != null)
            {
                predecessorPath = await node.GetPredecessorFile(path);

                if (predecessorPath != null) return predecessorPath;

                node = await GetPredecessorNode(node);
            }
            else node = await GetPredecessorNode(directory);

            while (node != null)
            {
                filesTree = await node.FilesTask;

                if (filesTree.TryGetMaximum(out predecessorPath)) return predecessorPath;

                node = await GetPredecessorNode(node);
            }

            filesTree = await root.FilesTask;

            return filesTree.TryGetMaximum(out predecessorPath) ? predecessorPath : null;
        }

        private async Task<FileSystemNode> GetPredecessorNode(string path)
        {
            FileSystemNode successor;
            DirectoriesBinaryTree tree;
            FileSystemNode node = root;

            foreach (string step in Utils.GetPathSteps(node.FullName, GetParent(path)))
            {
                tree = await node.ChildrenTask;

                if (!tree.TryGetValue(step.ToLower(), out node)) return null;
            }

            tree = await node.ChildrenTask;
            if (tree.TryGetPredecessor(path.ToLower(), out successor)) return successor;

            return await GetPredecessorNode(node);
        }

        private async Task<FileSystemNode> GetPredecessorNode(FileSystemNode node)
        {
            FileSystemNode successor;
            DirectoriesBinaryTree tree = await node.ChildrenTask;

            if (tree.TryGetMinimum(out successor)) return successor;

            while (node.Parent != null)
            {
                tree = await node.Parent.ChildrenTask;

                if (tree.TryGetPredecessor(node.FullName, out successor)) return successor;

                node = node.Parent;
            }

            return null;
        }

        //public bool ContainsFile(string path)
        //{
        //    FilesBinaryTree filesTree;
        //    string directory = GetParent(path);

        //    return root.TryGetValue(directory, out filesTree) && filesTree.Contains(path);
        //}

        private async Task<FileSystemNode> GetNode(string path)
        {
            FileSystemNode node = root;

            foreach (string step in Utils.GetPathSteps(node.FullName, path))
            {
                DirectoriesBinaryTree tree = await node.ChildrenTask;

                if (!tree.TryGetValue(step.ToLower(), out node)) return null;
            }

            return node;
        }

        private static string GetParent(string path)
        {
            return path.Remove(path.TrimEnd('\\').LastIndexOf('\\')) + '\\';
        }

        //public static async Task<FileSystem> GetFileSystem(string path, string priorityPath)
        //{
        //    //FileSystemNode priorityNode
        //    FileSystemNode root=new FileSystemNode(null, Path.GetFileName(path),Path.GetFullPath(path),)
        //    FileSystem system = new FileSystem(path);

        //    await Task.Run(() =>
        //    {

        //    })

        //    return system;
        //}
    }
}
