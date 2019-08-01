using System.Threading.Tasks;

namespace PictureView
{
    class FileSystemNode
    {
        public FileSystemNode Parent { get; set; }

        public string Name { get; }

        public  string FullName { get; }

        public Task<DirectoriesBinaryTree> ChildrenTask { get; }

        public DirectoriesBinaryTree Children { get; set; }

        public Task< FilesBinaryTree> FilesTask { get; set; }

        public FilesBinaryTree Files { get; set; }

        public FileSystemNode(FileSystemNode parent, string name, string fullName, 
            Task<DirectoriesBinaryTree> childrenTask, Task<FilesBinaryTree> filesTask)
        {
            Parent = parent;
            Name = name;
            FullName = fullName;
            ChildrenTask = childrenTask;
            FilesTask = filesTask;

            SetChildren();
            SetFiles();
        }

        private async void SetChildren()
        {
            Children = await ChildrenTask;
        }

        private async void SetFiles()
        {
            Files = await FilesTask;
        }

        public async Task<FileSystemNode> GetChild(string name)
        {
            DirectoriesBinaryTree tree = await ChildrenTask;

            FileSystemNode node;
            return tree.TryGetValue(name.ToLower(), out node) ? node : null;
        }

        public async Task<FileSystemNode> GetSuccessorChild(string name)
        {
            DirectoriesBinaryTree tree = await ChildrenTask;

            FileSystemNode node;
            return tree.TryGetSuccessor(name.ToLower(), out node) ? node : null;
        }

        public async Task<FileSystemNode> GetPredecessorChild(string name)
        {
            DirectoriesBinaryTree tree = await ChildrenTask;

            FileSystemNode node;
            return tree.TryGetPredecessor(name.ToLower(), out node) ? node : null;
        }

        public async Task<bool> ContainsFile(string name)
        {
            FilesBinaryTree tree = await FilesTask;

            return tree.Contains(name.ToLower());
        }

        public async Task<string> GetSuccessorFile(string name)
        {
            FilesBinaryTree tree = await FilesTask;

            string file;
            return tree.TryGetSuccessor(name.ToLower(), out file) ? file : null;
        }

        public async Task<string> GetPredecessorFile(string name)
        {
            FilesBinaryTree tree = await FilesTask;

            string file;
            return tree.TryGetPredecessor(name.ToLower(), out file) ? file : null;
        }
    }
}
