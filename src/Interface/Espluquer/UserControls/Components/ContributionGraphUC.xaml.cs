using Espluquer.Entities;
using Espluque.Contracts.Interfaces;
using Espluque.Contracts.ModuleInterfaces;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

using Util;

namespace Espluquer.UserControls.Components
{
    public partial class ContributionGraphUC : UserControl
    {
        private readonly TreeNode<IThesaurusConcept>? _tree;
        private readonly List<ICatalogEntry> _catalog;

        private string _selectedContribution;

        private readonly double _horizontalSpacing = 170;
        private readonly double _verticalSpacing = 70;

        public ContributionGraphUC(TreeNode<IThesaurusConcept>? tree, List<ICatalogEntry> catalog, string? selectedContribution)
        {
            _tree = tree;
            _catalog = catalog;
            _selectedContribution = selectedContribution ?? "Detector";

            InitializeComponent();

            Loaded += ContributionGraphUC_Loaded;
        }

        private async void ContributionGraphUC_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadGraphAsync();
        }

        private async Task LoadGraphAsync()
        {
            var nodes = BuildNodes();
            var edges = BuildEdges();

            var graphDepth = GetTreeDepth(_tree);
            var graphWidth = GetTreeWidth(nodes, edges);

            Dictionary<int, List<ContributionGraphNode>> nodesByColumn = BuildGraphDictionary(nodes, edges);
            
            DistributeNodesVertically(nodesByColumn, graphWidth);
            PopulateContributionFlags(nodesByColumn);

            await LoadGraphAsync(nodesByColumn, edges);

        }

        #region Nodes and Edges

        private List<(int ConceptId, string MainTerm)> BuildNodes()
        {
            List<(int ConceptId, string MainTerm)> nodes = [];

            if (_tree is null)
            {
                return nodes;
            }

            AddNodes(_tree, nodes);

            return nodes;
        }

        private static void AddNodes( TreeNode<IThesaurusConcept> currentNode, List<(int ConceptId, string MainTerm)> nodes)
        {
            if (currentNode.Data?.Id is int conceptId)
            {
                nodes.Add((conceptId, currentNode.Name));
            }

            foreach (TreeNode<IThesaurusConcept> childNode in currentNode.Children)
            {
                AddNodes(childNode, nodes);
            }
        }

        private List<(int ParentConceptId, int ChildConceptId)> BuildEdges()
        {
            List<(int ParentConceptId, int ChildConceptId)> edges = [];

            if (_tree is null)
            {
                return edges;
            }

            AddEdges(_tree, edges);

            return edges;
        }

        private static void AddEdges(  TreeNode<IThesaurusConcept> currentNode, List<(int ParentConceptId, int ChildConceptId)> edges)
        {
            foreach (TreeNode<IThesaurusConcept> childNode in currentNode.Children)
            {
                if (currentNode.Data?.Id is int parentConceptId &&
                    childNode.Data?.Id is int childConceptId)
                {
                    edges.Add((parentConceptId, childConceptId));
                }

                AddEdges(childNode, edges);
            }
        }

        #endregion


        #region Build grid

        private static int GetTreeDepth(TreeNode<IThesaurusConcept>? node)
        {
            if (node is null || node.Children.Count == 0)
            {
                return 0;
            }

            return 1 + node.Children.Max(GetTreeDepth);
        }

        private static int GetTreeWidth( List<(int ConceptId, string MainTerm)> nodes, List<(int ParentConceptId, int ChildConceptId)> edges)
        {
            Dictionary<int, List<int>> parentsByChild = edges
                .GroupBy(edge => edge.ChildConceptId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(edge => edge.ParentConceptId).ToList());

            Dictionary<int, int> columnsByConceptId = [];

            int GetColumn(int conceptId)
            {
                if (columnsByConceptId.TryGetValue(conceptId, out int existingColumn))
                {
                    return existingColumn;
                }

                if (!parentsByChild.TryGetValue(conceptId, out List<int>? parentConceptIds) ||
                    parentConceptIds.Count == 0)
                {
                    columnsByConceptId[conceptId] = 0;
                    return 0;
                }

                int column = parentConceptIds.Max(GetColumn) + 1;

                columnsByConceptId[conceptId] = column;

                return column;
            }

            foreach ((int conceptId, _) in nodes)
            {
                GetColumn(conceptId);
            }

            return columnsByConceptId.Values
                .GroupBy(column => column)
                .Max(group => group.Count());
        }

        private Dictionary<int, List<ContributionGraphNode>> BuildGraphDictionary(
            List<(int ConceptId, string MainTerm)> nodes,
            List<(int ParentConceptId, int ChildConceptId)> edges)
        {
            List<(int ConceptId, string MainTerm)> distinctNodes = nodes
                .DistinctBy(node => node.ConceptId)
                .ToList();

            List<(int ParentConceptId, int ChildConceptId)> distinctEdges = edges
                .Distinct()
                .ToList();

            Dictionary<int, List<int>> parentsByChild = distinctEdges
                .GroupBy(edge => edge.ChildConceptId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(edge => edge.ParentConceptId).ToList());

            Dictionary<int, int> columnsByConceptId = [];

            int GetColumn(int conceptId)
            {
                if (columnsByConceptId.TryGetValue(conceptId, out int existingColumn))
                {
                    return existingColumn;
                }

                if (!parentsByChild.TryGetValue(conceptId, out List<int>? parentConceptIds) ||
                    parentConceptIds.Count == 0)
                {
                    columnsByConceptId[conceptId] = 0;
                    return 0;
                }

                int column = parentConceptIds.Max(GetColumn) + 1;

                columnsByConceptId[conceptId] = column;

                return column;
            }

            foreach ((int conceptId, _) in distinctNodes)
            {
                GetColumn(conceptId);
            }

            Dictionary<int, List<ContributionGraphNode>> nodesByColumn = [];

            foreach ((int conceptId, string mainTerm) in distinctNodes)
            {
                int column = columnsByConceptId[conceptId];

                if (!nodesByColumn.TryGetValue(column, out List<ContributionGraphNode>? columnNodes))
                {
                    columnNodes = [];
                    nodesByColumn[column] = columnNodes;
                }

                int row = columnNodes.Count;

                columnNodes.Add(new ContributionGraphNode
                {
                    ConceptId = conceptId,
                    Label = $"{conceptId} - {mainTerm}",
                    Column = column,
                    Row = row,
                    X = column * _horizontalSpacing,
                    Y = row * _verticalSpacing
                });
            }

            return nodesByColumn;
        }

        private void PopulateContributionFlags( Dictionary<int, List<ContributionGraphNode>> nodesByColumn)
        {
            Dictionary<int, ContributionGraphNode> graphNodesByConceptId = nodesByColumn
                .Values
                .SelectMany(column => column)
                .ToDictionary(node => node.ConceptId);

            Dictionary<string, HashSet<int>> conceptIdsByTerm =
                new(StringComparer.OrdinalIgnoreCase);

            if (_tree is not null)
            {
                AddConceptTerms(_tree, conceptIdsByTerm);
            }

            foreach (ICatalogEntry catalogEntry in _catalog)
            {
                Action<ContributionGraphNode>? markContribution = catalogEntry.InterfaceType switch
                {
                    "IDetector" => node => { node.HasDetector = true; }
                    ,
                    "IGrabber" => node => { node.HasGrabber = true; }
                    ,
                    "IWpfViewer" => node => { node.HasViewer = true; }
                    ,
                    _ => null
                };

                if (markContribution is null)
                {
                    continue;
                }

                foreach (string tag in catalogEntry.Tags)
                {
                    if (!conceptIdsByTerm.TryGetValue(tag, out HashSet<int>? conceptIds))
                    {
                        continue;
                    }

                    foreach (int conceptId in conceptIds)
                    {
                        if (graphNodesByConceptId.TryGetValue(
                            conceptId,
                            out ContributionGraphNode? graphNode))
                        {
                            markContribution(graphNode);
                        }
                    }
                }
            }
        }

        private static void AddConceptTerms( TreeNode<IThesaurusConcept> currentNode, Dictionary<string, HashSet<int>> conceptIdsByTerm)
        {
            if (currentNode.Data?.Id is int conceptId)
            {
                foreach (IThesaurusTerm term in currentNode.Data.Terms)
                {
                    if (string.IsNullOrWhiteSpace(term.Term))
                    {
                        continue;
                    }

                    if (!conceptIdsByTerm.TryGetValue(
                        term.Term,
                        out HashSet<int>? conceptIds))
                    {
                        conceptIds = [];
                        conceptIdsByTerm[term.Term] = conceptIds;
                    }

                    conceptIds.Add(conceptId);
                }
            }

            foreach (TreeNode<IThesaurusConcept> childNode in currentNode.Children)
            {
                AddConceptTerms(childNode, conceptIdsByTerm);
            }
        }

        private void DistributeNodesVertically( Dictionary<int, List<ContributionGraphNode>> nodesByColumn, int rowCount)
        {
            if (rowCount <= 0)
            {
                return;
            }

            foreach (List<ContributionGraphNode> columnNodes in nodesByColumn.Values)
            {
                int nodeCount = columnNodes.Count;

                if (nodeCount == 0)
                {
                    continue;
                }

                for (int index = 0; index < nodeCount; index++)
                {
                    int row = (int)Math.Floor((index + 0.5) * rowCount / nodeCount);

                    ContributionGraphNode node = columnNodes[index];

                    node.Row = row;
                    node.Y = row * _verticalSpacing;
                }
            }
        }

        #endregion


        private async Task LoadGraphAsync(
            Dictionary<int, List<ContributionGraphNode>> nodesByColumn,
            List<(int ParentConceptId, int ChildConceptId)> edges)
        {
            List<ContributionGraphNode> nodes = nodesByColumn
                .OrderBy(column => column.Key)
                .SelectMany(column => column.Value)
                .ToList();

            var visNodes = nodes.Select(node => new
            {
                id = node.ConceptId,
                label = node.Label,
                x = node.X,
                y = node.Y,
                @fixed = true,
                hasDetector = node.HasDetector,
                hasGrabber = node.HasGrabber,
                hasViewer = node.HasViewer
            });

            var visEdges = edges
                .Distinct()
                .Select(edge => new
                {
                    from = edge.ParentConceptId,
                    to = edge.ChildConceptId,
                    arrows = "to"
                });

            string nodesJson = JsonSerializer.Serialize(visNodes);
            string edgesJson = JsonSerializer.Serialize(visEdges);
            string selectedContributionJson = JsonSerializer.Serialize(_selectedContribution);

            string graphScript = GraphScriptTemplate
                .Replace("__NODES_JSON__", nodesJson)
                .Replace("__EDGES_JSON__", edgesJson)
                .Replace("__DEFAULT_NODE_STYLE__", DefaultNodeStyle)
                .Replace("__NODE_GROUPS__", NodeGroups)
                .Replace("__SELECTED_CONTRIBUTION__", selectedContributionJson);

            string html = $$"""
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <script src="https://unpkg.com/vis-network/standalone/umd/vis-network.min.js"></script>

    <style>
        html, body {
            width: 100%;
            height: 100%;
            margin: 0;
            overflow: hidden;
            background: #FFFFFF;
        }

        #graph {
            width: 100%;
            height: 100%;
        }
    </style>
</head>

<body>
    <div id="graph"></div>

    <script>
{{graphScript}}
    </script>
</body>
</html>
""";

            await GraphWebView.EnsureCoreWebView2Async();

            GraphWebView.CoreWebView2.WebMessageReceived -= GraphWebView_WebMessageReceived;
            GraphWebView.CoreWebView2.WebMessageReceived += GraphWebView_WebMessageReceived;

            GraphWebView.NavigateToString(html);
        }

        #region html graph styles & javascript

        private const string DefaultNodeStyle = """
{
    shape: "box",
    font: {
        size: 14
    },
    color: {
        background: "#E5E7EB",
        border: "#9CA3AF",
        highlight: {
            background: "#FFCDD2",
            border: "#C62828"
        }
    }
}
""";

        private const string NodeGroups = """
        {
            Neutral: {
                color: {
                    background: "#E5E7EB",
                    border: "#9CA3AF",
                    highlight: {
                        background: "#FFCDD2",
                        border: "#C62828"
                    }
                }
            },

            Detector: {
                color: {
                    background: "#E1BEE7",
                    border: "#7B1FA2",
                    highlight: {
                        background: "#FFCDD2",
                        border: "#C62828"
                    }
                }
            },

            Grabber: {
                color: {
                    background: "#FFE0B2",
                    border: "#EF6C00",
                    highlight: {
                        background: "#FFCDD2",
                        border: "#C62828"
                    }
                }
            },

            Viewer: {
                color: {
                    background: "#BBDEFB",
                    border: "#1565C0",
                    highlight: {
                        background: "#FFCDD2",
                        border: "#C62828"
                    }
                }
            }
        }
        """;

        private const string GraphScriptTemplate = """
window.graphNodes = new vis.DataSet(__NODES_JSON__);
window.graphEdges = new vis.DataSet(__EDGES_JSON__);

const container = document.getElementById("graph");

const data = {
    nodes: window.graphNodes,
    edges: window.graphEdges
};

const options = {
    nodes: __DEFAULT_NODE_STYLE__,
    groups: __NODE_GROUPS__,

    edges: {
        color: {
            color: "#94A3B8",
            highlight: "#C62828",
            inherit: false
        },
        width: 1,
        selectionWidth: 2,
        smooth: {
            enabled: true,
            type: "cubicBezier",
            forceDirection: "horizontal",
            roundness: 0.25
        }
    },

    interaction: {
        selectable: true,
        selectConnectedEdges: true
    },

    physics: {
        enabled: false
    }
};

window.graphNetwork = new vis.Network(container, data, options);

window.setContributionType = function(selectedContribution) {
    const selection = window.graphNetwork.getSelection();
    const allNodes = window.graphNodes.get();

    const neutralNodes = allNodes.map(node => ({
        id: node.id,
        group: "Neutral"
    }));

    window.graphNodes.updateOnly(neutralNodes);

    const selectedNodes = allNodes
        .filter(node =>
            (selectedContribution === "Detector" && node.hasDetector) ||
            (selectedContribution === "Grabber" && node.hasGrabber) ||
            (selectedContribution === "Viewer" && node.hasViewer))
        .map(node => ({
            id: node.id,
            group: selectedContribution
        }));

    if (selectedNodes.length > 0) {
        window.graphNodes.updateOnly(selectedNodes);
    }

    window.graphNetwork.setSelection(selection, {
        unselectAll: true,
        highlightEdges: true
    });
};

window.graphNetwork.on("click", function(parameters) {
    if (parameters.nodes.length === 0) {
        window.chrome.webview.postMessage({
            type: "NodeDeselected"
        });

        return;
    }

    const nodeId = parameters.nodes[0];
    const node = window.graphNodes.get(nodeId);

    window.chrome.webview.postMessage({
        type: "NodeSelected",
        id: node.id,
        label: node.label
    });
});

window.setContributionType(__SELECTED_CONTRIBUTION__);
""";

        #endregion


        #region Change selected contribution & change active node

        public async Task SetContributionTypeAsync(string selectedContribution)
        {
            _selectedContribution = selectedContribution;

            if (GraphWebView.CoreWebView2 is null)
            {
                return;
            }

            string selectedContributionJson = JsonSerializer.Serialize(selectedContribution);

            await GraphWebView.ExecuteScriptAsync(
                $"window.setContributionType({selectedContributionJson});");
        }

        private void GraphWebView_WebMessageReceived(
            object? sender,
            Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            using JsonDocument document = JsonDocument.Parse(e.WebMessageAsJson);
            JsonElement root = document.RootElement;

            string? messageType = root.GetProperty("type").GetString();

            switch (messageType)
            {
                case "NodeSelected":
                    int id = root.GetProperty("id").GetInt32();
                    string label = root.GetProperty("label").GetString() ?? string.Empty;

                    NodeSelected(id, label);
                    break;
                case "NodeDeselected":
                    NodeSelected(null, null);
                    break;
                default:
                    break;
            }
        }

        public event Action<int?, string?>? ThesaurusConceptSelected;

        private void NodeSelected(int? id, string? label)
        {
            ThesaurusConceptSelected?.Invoke(id, label);
        }

        #endregion
    }
}
