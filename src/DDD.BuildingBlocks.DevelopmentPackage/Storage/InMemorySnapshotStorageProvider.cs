using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DDD.BuildingBlocks.Core.Exception;
using DDD.BuildingBlocks.Core.Persistence.Storage;
using DDD.BuildingBlocks.Core.Util;

#pragma warning disable 1998

namespace DDD.BuildingBlocks.DevelopmentPackage.Storage
{
    using Core.Persistence.SnapshotSupport;

    public class InMemorySnapshotStorageProvider : ISnapshotStorageProvider {

        private readonly Dictionary<Guid, Snapshot> _items = new();
        private readonly Dictionary<string, Guid> _idMapping = new();

        private readonly string _memoryDumpFile;
        public int SnapshotFrequency { get; }

        public InMemorySnapshotStorageProvider(int frequency, string memoryDumpFile)
        {
            SnapshotFrequency = frequency;
            _memoryDumpFile = memoryDumpFile;

            if (File.Exists(_memoryDumpFile))
            {
                _items = SerializerHelper
                    .LoadListFromFile<Dictionary<Guid, Snapshot>>(_memoryDumpFile).First();

                _idMapping = SerializerHelper
                    .LoadListFromFile<Dictionary<string, Guid>>(_memoryDumpFile + "._mappings").First();
            }
        }

        public async Task<Snapshot?> GetSnapshotAsync(string key)
        {
            ArgumentNullException.ThrowIfNull(key);

            if (!_idMapping.ContainsKey(key))
            {
                return null;
            }

            var aggregateId = _idMapping[key];

            if (_items.ContainsKey(aggregateId))
            {
                return _items[aggregateId];
            }

            throw new ProviderException($"Mapping for key {key} exists, but no snapshot data found.");
        }

        public async Task<Snapshot?> GetSnapshotAsync(string key, int version)
        {
            ArgumentNullException.ThrowIfNull(key);

            if (!_idMapping.ContainsKey(key))
            {
                return null;
            }

            var aggregateId = _idMapping[key];

            if (_items.ContainsKey(aggregateId) && _items[aggregateId].Version == version)
            {
                return _items[aggregateId];
            }

            throw new ProviderException($"Mapping for key {key} exists, but no snapshot data found.");
        }

        public async Task SaveSnapshotAsync(Snapshot? snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            if (snapshot.SerializedAggregateId == null)
            {
                throw new InvalidOperationException("SerializedAggregateId must not be null");
            }

            if (!_idMapping.ContainsKey(snapshot.SerializedAggregateId))
            {
                var id = Guid.NewGuid();
                _idMapping.Add(snapshot.SerializedAggregateId, id);
                _items.Add(id, snapshot);
            }
            else
            {

                _items[_idMapping[snapshot.SerializedAggregateId]] = snapshot;
            }

            SerializerHelper.SaveListToFile(_memoryDumpFile, new[] { _items });
        }
    }
}
