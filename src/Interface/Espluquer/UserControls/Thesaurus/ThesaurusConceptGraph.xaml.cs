using Espluque.Contracts.Interfaces;
using Espluquer.Entities;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace Espluquer.UserControls.Thesaurus
{
    public partial class ThesaurusConceptGraph : UserControl
    {
        private ConceptDto _conceptDto;
        private readonly IThesaurusService _thesaurusService;

        public ThesaurusConceptGraph(ConceptDto conceptDto, IThesaurusService thesaurusService)
        {
            InitializeComponent();

            _thesaurusService = thesaurusService;
            _conceptDto = conceptDto;

            Loaded += ThesaurusConceptGraph_Loaded;
        }

        private async Task<List<(int ConceptId, string MainTerm, string Relation)>?> GetNodes()
        {
            if (_conceptDto.Id is not int conceptId)
            {
                return null;
            }

            var nodes = await _thesaurusService.GetNodes(conceptId);
            return nodes;
        }

        private async Task<List<(int ParentConceptId, int ChildConceptId, string Relation)>?> GetEdges()
        {
            if (_conceptDto.Id is not int conceptId)
            {
                return null;
            }

            var edges = await _thesaurusService.GetEdges(conceptId);
            return edges;
        }

        private async void ThesaurusConceptGraph_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadGraphAsync();
        }

        private async Task LoadGraphAsync()
        {
            var nodes = await GetNodes();
            var edges = await GetEdges();

            if (nodes is null || edges is null)
            {
                return;
            }

            var visNodes = nodes.Select(node => new
            {
                id = node.ConceptId,
                label = $"{node.ConceptId} - {node.MainTerm}",
                group = node.Relation
            });

            var visEdges = edges.Select(edge => new
            {
                from = edge.ParentConceptId,
                to = edge.ChildConceptId,
                arrows = "to",
                color = edge.Relation == "Ancestor" ? "#2E7D32" : "#1565C0"
            });

            string nodesJson = JsonSerializer.Serialize(visNodes);
            string edgesJson = JsonSerializer.Serialize(visEdges);

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
        const nodes = new vis.DataSet({{nodesJson}});
        const edges = new vis.DataSet({{edgesJson}});

        const container = document.getElementById("graph");

        const data = {
            nodes: nodes,
            edges: edges
        };

        const options = {
            nodes: {
                shape: "box",
                font: {
                    size: 14
                }
            },
            groups: {
                Selected: {
                    color: {
                        background: "#FFCDD2",
                        border: "#C62828",
                        highlight: {
                            background: "#EF9A9A",
                            border: "#B71C1C"
                        }
                    }
                },
                Ancestor: {
                    color: {
                        background: "#C8E6C9",
                        border: "#2E7D32",
                        highlight: {
                            background: "#A5D6A7",
                            border: "#1B5E20"
                        }
                    }
                },
                Descendant: {
                    color: {
                        background: "#BBDEFB",
                        border: "#1565C0",
                        highlight: {
                            background: "#90CAF9",
                            border: "#0D47A1"
                        }
                    }
                }
            },
            edges: {
                smooth: true
            },
            physics: {
                stabilization: true
            }
        };

        new vis.Network(container, data, options);
    </script>
</body>
</html>
""";

            await GraphWebView.EnsureCoreWebView2Async();
            GraphWebView.NavigateToString(html);
        }
    }
}
