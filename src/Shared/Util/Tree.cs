namespace Util
{
    public sealed class TreeNode<T>
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public bool IsLeaf { get; set; }

        public T? Data { get; set; }

        public TreeNode<T>? Parent { get; internal set; }

        public List<TreeNode<T>> Children { get; } = new();

        public IEnumerable<TreeNode<T>> BranchChildren => Children.Where(node => !node.IsLeaf);
    }

    public static class TreeBuilder
    {
        public static TreeNode<T> Build<T>(
            IEnumerable<(string Path, bool IsLeaf, T Data)> items,
            string[] delimiters,
            string rootName = "Root")
        {
            // delimiters ??= ["/", "\\"];

            TreeNode<T> root = new TreeNode<T>
            {
                Name = rootName,
                FullPath = string.Empty,
                IsLeaf = false
            };

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.Path))
                {
                    continue;
                }

                AddPath(root, item.Data, item.Path, delimiters, item.IsLeaf);
            }

            return root;
        }

        private static void AddPath<T>(
            TreeNode<T> root,
            T data,
            string path,
            string[] delimiters,
            bool isLeaf)
        {
            string[] parts = path.Split(
                delimiters,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            TreeNode<T> currentNode = root;
            string currentPath = string.Empty;
            string outputDelimiter = delimiters[0];

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                bool isLastPart = i == parts.Length - 1;

                currentPath = string.IsNullOrEmpty(currentPath)
                    ? part
                    : currentPath + outputDelimiter + part;

                TreeNode<T>? childNode = currentNode.Children
                    .FirstOrDefault(node => node.Name == part);

                if (childNode is null)
                {
                    childNode = new TreeNode<T>
                    {
                        Name = part,
                        FullPath = currentPath,
                        IsLeaf = isLastPart && isLeaf,
                        Parent = currentNode
                    };

                    currentNode.Children.Add(childNode);
                }

                if (isLastPart)
                {
                    childNode.IsLeaf = isLeaf;
                    childNode.Data = data;
                }

                currentNode = childNode;
            }
        }
    }
}