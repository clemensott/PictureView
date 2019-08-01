namespace PictureView
{
    class DirectoriesBinaryTree: BinaryTree<string, FileSystemNode>
    {
        public string DirectoryPath { get; }

        public DirectoriesBinaryTree(string path)
        {
            DirectoryPath = path;
        }
    }
}
