using NuReaper.Domain.Entities.DataFlow;

namespace NuReaper.Infrastructure.Repositories.DataFlow.Registries
{
    public sealed class NodeRegistry
    {
        /// <summary>
        /// Centralny rejestr węzłów.
        /// - klucz semantyczny (string) → int ID  (tylko podczas budowy)
        /// - int ID → DataFlowNode                (lookup w binders)
        /// 
        /// Po zakończeniu budowy możesz wyczyścić _keyToId żeby zwolnić pamięć.
        /// </summary>
        private readonly Dictionary<string, int> _keyToId 
            = new(StringComparer.Ordinal);
        
        private readonly List<DataFlowNode> _nodes = new();

        private int _nextId = 1;  // 0 = "null" (sentinel)

        public IReadOnlyList<DataFlowNode> Nodes => _nodes;

        public int GetOrCreate(string semanticKey, Func<int, DataFlowNode> factory)
        {
            if (_keyToId.TryGetValue(semanticKey, out var existingId))
                return existingId;

            var id = _nextId++;
            var node = factory(id);
            _nodes.Add(node);
            _keyToId[semanticKey] = id;
            return id;
        }

        public bool TryGetId(string semanticKey, out int id)
            => _keyToId.TryGetValue(semanticKey, out id);

        public DataFlowNode GetById(int id)
            => _nodes[id - 1];
        /// <summary>
        /// Zwalnia słownik string→int po zakończeniu budowy grafu.
        /// Zostają tylko węzły.
        /// </summary>
        public void FreeKeyIndex()
            => _keyToId.Clear();
    }
}
