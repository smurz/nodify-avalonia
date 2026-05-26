using System;
using System.Collections;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace Nodify.Playground
{
    public class PlaygroundViewModel : ObservableObject
    {
        public NodifyEditorViewModel GraphViewModel { get; } = new NodifyEditorViewModel();

        public PlaygroundViewModel()
        {
            GenerateRandomNodesCommand = new DelegateCommand(GenerateRandomNodes);
            PerformanceTestCommand = new DelegateCommand(PerformanceTest);
            ToggleConnectionsCommand = new DelegateCommand(ToggleConnections);
            ResetCommand = new DelegateCommand(ResetGraph);

            //BindingOperations.EnableCollectionSynchronization(GraphViewModel.Nodes, GraphViewModel.Nodes);
            //BindingOperations.EnableCollectionSynchronization(GraphViewModel.Connections, GraphViewModel.Connections);

            Settings.PropertyChanged += OnSettingsChanged;
        }

        private void OnSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PlaygroundSettings.ShouldConnectNodes))
                OnPropertyChanged(nameof(ConnectNodesText));
        }

        public ICommand GenerateRandomNodesCommand { get; }
        public ICommand PerformanceTestCommand { get; }
        public ICommand ToggleConnectionsCommand { get; }
        public ICommand ResetCommand { get; }
        public PlaygroundSettings Settings => PlaygroundSettings.Instance;

        public string ConnectNodesText => Settings.ShouldConnectNodes ? "CONNECT NODES" : "DISCONNECT NODES";

        private void ResetGraph()
        {
            GraphViewModel.Nodes.Clear();
            EditorSettings.Instance.Location = new Point(0, 0);
            EditorSettings.Instance.Zoom = 1.0d;
        }

        private async void GenerateRandomNodes()
        {
            uint minNodesByType = Settings.MinNodes / 2;
            uint maxNodesByType = Settings.MaxNodes / 2;

            var nodes = RandomNodesGenerator.GenerateNodes<FlowNodeViewModel>(new NodesGeneratorSettings(minNodesByType)
            {
                MinNodesCount = minNodesByType,
                MaxNodesCount = maxNodesByType,
                MinInputCount = Settings.MinConnectors,
                MaxInputCount = Settings.MaxConnectors,
                MinOutputCount = Settings.MinConnectors,
                MaxOutputCount = Settings.MaxConnectors,
                GridSnap = EditorSettings.Instance.GridSpacing
            });

            var verticalNodes = RandomNodesGenerator.GenerateNodes<VerticalNodeViewModel>(new NodesGeneratorSettings(minNodesByType)
            {
                MinNodesCount = minNodesByType,
                MaxNodesCount = maxNodesByType,
                MinInputCount = Settings.MinConnectors,
                MaxInputCount = Settings.MaxConnectors,
                MinOutputCount = Settings.MinConnectors,
                MaxOutputCount = Settings.MaxConnectors,
                GridSnap = EditorSettings.Instance.GridSpacing
            });

            GraphViewModel.Nodes.Clear();
            await CopyToAsync(nodes, GraphViewModel.Nodes);
            await CopyToAsync(verticalNodes, GraphViewModel.Nodes);

            if (Settings.ShouldConnectNodes)
            {
                await ConnectNodes();
            }
        }

        private async void ToggleConnections()
        {
            if (Settings.ShouldConnectNodes)
            {
                await ConnectNodes();
            }
            else
            {
                GraphViewModel.Connections.Clear();
            }
        }

        private async void PerformanceTest()
        {
            uint count = Settings.PerformanceTestNodes;
            int distance = 500;
            int size = (int)count / (int)Math.Sqrt(count);

            var nodes = RandomNodesGenerator.GenerateNodes<FlowNodeViewModel>(new NodesGeneratorSettings(count)
            {
                NodeLocationGenerator = (s, i) => new Point(i % size * distance, i / size * distance),
                MinInputCount = Settings.MinConnectors,
                MaxInputCount = Settings.MaxConnectors,
                MinOutputCount = Settings.MinConnectors,
                MaxOutputCount = Settings.MaxConnectors,
                GridSnap = EditorSettings.Instance.GridSpacing
            });

            GraphViewModel.Nodes.Clear();
            await CopyToAsync(nodes, GraphViewModel.Nodes);

            if (Settings.ShouldConnectNodes)
            {
                await ConnectNodes();
            }
        }

        private async Task ConnectNodes()
        {
            var schema = new GraphSchema();
            var connections = RandomNodesGenerator.GenerateConnections(GraphViewModel.Nodes);

            // Mutations must run on the UI thread — observable collections bound to ItemsControls
            // aren't thread-safe; cross-thread Add races Avalonia 12's PanelContainerGenerator and
            // crashes with ArgumentOutOfRangeException. AsyncLoading still yields to keep the
            // window responsive during large batches.
            const int batchSize = 50;
            for (int i = 0; i < connections.Count; i++)
            {
                var con = connections[i];
                schema.TryAddConnection(con.Input, con.Output);

                if (Settings.AsyncLoading && i % batchSize == batchSize - 1)
                    await Task.Yield();
            }
        }

        private async Task CopyToAsync(IList source, IList target)
        {
            const int batchSize = 50;
            for (int i = 0; i < source.Count; i++)
            {
                target.Add(source[i]);

                if (Settings.AsyncLoading && i % batchSize == batchSize - 1)
                    await Task.Yield();
            }
        }
    }
}
