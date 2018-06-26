using System;
using System.Collections.Generic;
using System.Text;

namespace Izyuk.Core
{
    public class Graph : IGraph
    {
        private object2objectArrayMap<string, object2IntMap<string>> nodeKeys;
        private objectArrayList<Map<string, object>> nodes;
        private object2objectArrayMap<string, Long2IntOpenHashMap> relationshipKeys;
        private objectArrayList<Map<string, object>> relationships;
        private object2objectOpenHashMap<string, ReversibleMultiMap> related;
        private object2IntArrayMap<string> relationshipCounts;
        private object2objectArrayMap<string, Long2IntOpenHashMap> relatedCounts;
        private RoaringBitmap deletedNodes;
        private RoaringBitmap deletedRelationships;

        public Graph()
        {
            nodeKeys = new object2objectArrayMap<>();
            nodes = new objectArrayList<>();
            relationshipKeys = new object2objectArrayMap<>();
            relationships = new objectArrayList<>();
            related = new object2objectOpenHashMap<>();
            relationshipCounts = new object2IntArrayMap<>();
            relationshipCounts.defaultReturnValue(0);
            relatedCounts = new object2objectArrayMap<>();
            deletedNodes = new RoaringBitmap();
            deletedRelationships = new RoaringBitmap();
        }

        public void Clear()
        {
            nodeKeys.clear();
            nodes.clear();
            relationships.clear();
            related.clear();
            relationshipCounts.clear();
            relatedCounts.clear();
            deletedNodes.clear();
            deletedRelationships.clear();
        }

        // Relationship Types
        public HashSet<string> getRelationshipTypes()
        {
            return related.keySet();
        }

        public Dictionary<string, int> getRelationshipTypesCount()
        {
            return relationshipCounts;
        }

        public int getRelationshipTypeCount(string type)
        {
            return relationshipCounts.getInt(type);
        }

        private object2IntDictionary<string> getOrCreateNodeKey(string label)
        {
            object2IntMap<string> nodeKey;

            if (!nodeKeys.ContainsKey(label))
            {
                nodeKey = new object2IntOpenHashMap<>();
                nodeKey.defaultReturnValue(-1);
                nodeKeys.Add(label, nodeKey);
            }
            else
            {
                nodeKey = nodeKeys.get(label);
            }
            return nodeKey;
        }

        private int GetNodeKeyId(string label, string id)
        {
            if (!nodeKeys.ContainsKey(label))
            {
                return -1;
            }
            else
            {
                return nodeKeys.get(label).getInt(id);
            }
        }

        private void removeNodeKeyId(string label, string id)
        {
            nodeKeys.get(label).removeInt(id);
        }

        private void addRelationshipKeyId(string type, int count, int node1, int node2, int id)
        {

            if (!relationshipKeys.ContainsKey(type + count))
            {
                Long2IntOpenHashMap relKey = new Long2IntOpenHashMap();
                relKey.defaultReturnValue(-1);
                relKey.Add(((long)node1 << 32) + node2, id);
                relationshipKeys.Add(type + count, relKey);
            }
            else
            {
                relationshipKeys.get(type + count).Add(((long)node1 << 32) + node2, id);
            }

        }
        private int getRelationshipKeyId(string type, int count, int node1, int node2)
        {

            if (!relationshipKeys.ContainsKey(type + count))
            {
                return -1;
            }
            else
            {
                return relationshipKeys.get(type + count).get(((long)node1 << 32) + node2);
            }
        }

        private void removeRelationshipKeyId(string type, int count, int node1, int node2)
        {
            relationshipKeys.get(type + count).Remove(((long)node1 << 32) + node2);
        }

        // Nodes
        public int addNode(string label, string key)
        {
            return addNode(label, key, new Dictionary<string, object>());
        }

        public int addNode(string label, string key, Dictionary<string, object> properties)
        {
            object2IntMap<string> nodeKey = getOrCreateNodeKey(label);

            if (nodeKey.ContainsKey(key))
            {
                return -1;
            }
            else
            {
                properties.Add("~label", label);
                properties.Add("~key", key);
                int nodeId;
                if (deletedNodes.isEmpty())
                {
                    nodeId = nodes.size();
                    properties.Add("~id", nodeId);
                    nodes.Add(properties);
                    nodeKey.Add(key, nodeId);
                }
                else
                {
                    nodeId = deletedNodes.first();
                    properties.Add("~id", nodeId);
                    nodes.set(nodeId, properties);
                    nodeKey.Add(key, nodeId);
                    deletedNodes.Remove(nodeId);
                }

                return nodeId;
            }
        }

        public bool removeNode(string label, string key)
        {
            int id = GetNodeKeyId(label, key);
            if (id == -1) { return false; }
            nodes.set(id, null);
            deletedNodes.Add(id);

            foreach (string type in related.keySet())
            {
                ReversibleMultiMap rels = related.get(type);
                int outgoingCount = 0;
                int incomingCount = 0;
                for (int value : rels.getRels(id))
                {
                    outgoingCount++;
                    relationships.set(value, null);
                    deletedRelationships.Add(value);
                }
                for (int value : rels.getRelsByValue(id))
                {
                    incomingCount++;
                    relationships.set(value, null);
                    deletedRelationships.Add(value);
                }
                rels.removeAll(id);
                relationshipCounts.Add(type, relationshipCounts.getInt(type) - (outgoingCount + incomingCount));
            }
            removeNodeKeyId(label, key);

            return true;
        }

        public Dictionary<string, object> getNodeById(int id)
        {
            return nodes.get(id);
        }

        public Dictionary<string, object> getNode(string label, string key)
        {
            int id = GetNodeKeyId(label, key);
            if (id == -1) { return null; }
            return nodes.get(id);
        }

        public int getNodeId(string label, string key)
        {
            return GetNodeKeyId(label, key);
        }

        public string GetNodeLabel(int id)
        {
            return (string)nodes.get(id).get("~label");
        }

        public string GetNodeKey(int id)
        {
            return (string)nodes.get(id).get("~key");
        }

        // Node Properties
        public object GetNodeProperty(string label, string key, string property)
        {
            return GetNodeProperty(GetNodeKeyId(label, key), property);
        }

        public object GetNodeProperty(int id, string property)
        {
            if (id == -1) { return null; }
            return nodes.get(id).get(property);
        }

        public bool UpdateNodeProperties(string label, string key, Dictionary<string, object> properties)
        {
            return UpdateNodeProperties(GetNodeKeyId(label, key), properties);
        }

        public bool UpdateNodeProperties(int id, Dictionary<string, object> properties)
        {
            if (id == -1) { return false; }
            if (properties.ContainsKey("~label") || properties.ContainsKey("~key") || properties.ContainsKey("~id"))
            {
                return false;
            }
            Dictionary<string, object> current = nodes.get(id);
            foreach (var key in properties.Keys)
                current.Add(key,properties[key]);
            return true;
        }

        public bool DeleteNodeProperties(string label, string key)
        {
            return DeleteNodeProperties(GetNodeKeyId(label, key));
        }

        public bool DeleteNodeProperties(int id)
        {
            if (id == -1) { return false; }
            Dictionary<string, object> current = nodes.get(id);
            Dictionary<string, object> properties = new Dictionary<string, object>();
            current.Add("~id", id);
            current.Add("~label", current["~label"]);
            current.Add("~key", current["~key"]);
            nodes.Add(id, properties);
            return true;
        }

        public bool UpdateNodeProperty(string label, string key, string property, object value)
        {
            return UpdateNodeProperty(GetNodeKeyId(label, key), property, value);
        }

        public bool UpdateNodeProperty(int id, string property, object value)
        {
            if (id == -1) { return false; }
            Dictionary<string, object> properties = nodes.get(id);
            if (property.StartsWith("~"))
            {
                return false;
            }
            else
            {
                properties.Add(property, value);
            }
            return true;
        }

        public bool DeleteNodeProperty(string label, string key, string property)
        {
            return DeleteNodeProperty(GetNodeKeyId(label, key), property);
        }

        public bool DeleteNodeProperty(int id, string property)
        {
            if (id == -1) { return false; }
            Dictionary<string, object> properties = nodes.get(id);
            if (property.StartsWith("~"))
            {
                return false;
            }
            else
            {
                properties.Remove(property);
                return true;
            }
        }

        // Relationships
        public int AddRelationship(string type, string label1, string from, string label2, string to)
        {
            int node1 = GetNodeKeyId(label1, from);
            int node2 = GetNodeKeyId(label2, to);
            if (node1 == -1 || node2 == -1) { return -1; }

            related.AddIfAbsent(type, new ReversibleMultiMap());
            relationshipCounts.AddIfAbsent(type, 0);
            relationshipCounts.Add(type, relationshipCounts.getInt(type) + 1);

            relatedCounts.AddIfAbsent(type, new Long2IntOpenHashMap());
            Long2IntOpenHashMap relatedCount = relatedCounts.get(type);
            long countId = ((long)node1 << 32) + node2;
            int count = relatedCount.get(countId) + 1;
            int id = relationships.size();
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("~incoming_node_id", node1);
            properties.Add("~outgoing_node_id", node2);
            properties.Add("~type", type);
            properties.Add("~id", id);

            relationships.Add(properties);
            relatedCount.Add(countId, count);
            related.get(type).Add(node1, node2, id);
            addRelationshipKeyId(type, count, node1, node2, id);

            return id;
        }

        public int addRelationship(string type, string label1, string from, string label2, string to, Dictionary<string, object> properties)
        {
            int node1 = GetNodeKeyId(label1, from);
            int node2 = GetNodeKeyId(label2, to);
            if (node1 == -1 || node2 == -1) { return -1; }

            related.AddIfAbsent(type, new ReversibleMultiMap());
            relationshipCounts.AddIfAbsent(type, 0);
            relationshipCounts.Add(type, relationshipCounts.getInt(type) + 1);

            int id = relationships.size();
            properties.Add("~incoming_node_id", node1);
            properties.Add("~outgoing_node_id", node2);
            properties.Add("~type", type);
            properties.Add("~id", id);

            relatedCounts.AddIfAbsent(type, new Long2IntOpenHashMap());
            Long2IntOpenHashMap relatedCount = relatedCounts.get(type);
            long countId = ((long)node1 << 32) + node2;
            int count = relatedCount.get(countId) + 1;
            // If this is the second or greater relationship of this type between these two nodes, add it to the properties, else assume it is one.
            if (count > 1)
            {
                properties.Add("~count", count);
            }

            relationships.Add(properties);
            relatedCount.Add(countId, count);
            related.get(type).Add(node1, node2, id);
            addRelationshipKeyId(type, count, node1, node2, id);

            return id;
        }

        public bool removeRelationship(int id)
        {
            Dictionary<string, object> relationship = relationships.get(id);
            string type = (string)relationship["~type"]);
            int node1 = (int)relationship["~incoming_node_id"];
            int node2 = (int)relationship["~outgoing_node_id"];
            object countobject = null;
            relationship.TryGetValue("~count", out countobject);    // TO DO - Make an extension method to get ~count
            int count = countobject != null ? (int)countobject : 0;
            related.get(type).removeRelationship(node1, node2, id);
            relationships.set(id, null);
            removeRelationshipKeyId(type, count, node1, node2);

            return true;
        }

        public bool removeRelationship(string type, string label1, string from, string label2, string to)
        {
            int node1 = GetNodeKeyId(label1, from);
            int node2 = GetNodeKeyId(label2, to);
            if (node1 == -1 || node2 == -1) { return false; }

            if (!related.ContainsKey(type))
            {
                return false;
            }
            long countId = ((long)node1 << 32) + node2;
            int count = relatedCounts.get(type).get(countId);
            if (count == 0)
            {
                return false;
            }
            relatedCounts.get(type).Add(countId, count - 1);
            relationshipCounts.Add(type, relationshipCounts.getInt(type) - 1);

            int relId = getRelationshipKeyId(type, count, node1, node2);

            related.get(type).removeRelationship(node1, node2, relId);
            relationships.set(relId, null);
            removeRelationshipKeyId(type, count, node1, node2);

            return true;
        }

        public bool removeRelationship(string type, string label1, string from, string label2, string to, int number)
        {
            int node1 = GetNodeKeyId(label1, from);
            int node2 = GetNodeKeyId(label2, to);
            if (node1 == -1 || node2 == -1) { return false; }

            if (!related.ContainsKey(type))
            {
                return false;
            }
            long countId = ((long)node1 << 32) + node2;
            int count = relatedCounts.get(type).get(countId);
            if (count == 0 || count < number)
            {
                return false;
            }
            relatedCounts.get(type).Add(countId, count - 1);
            relationshipCounts.Add(type, relationshipCounts.getInt(type) - 1);

            int relId = getRelationshipKeyId(type, number, node1, node2);

            related.get(type).removeRelationship(node1, node2, relId);
            relationships.set(relId, null);
            if (count == 1)
            {
                removeRelationshipKeyId(type, number, node1, node2);
            }
            else
            {
                if (count != number)
                {
                    int movedRelId = getRelationshipKeyId(type, count, node1, node2);
                    if (number > 1)
                    {
                        relationships.get(movedRelId).Add("~count", number);
                    }
                    else
                    {
                        relationships.get(movedRelId).Remove("~count");
                    }
                    addRelationshipKeyId(type, number, node1, node2, movedRelId);
                }
                removeRelationshipKeyId(type, count, node1, node2);
            }

            return true;
        }

        public Dictionary<string, object> GetRelationshipById(int id)
        {
            return relationships[id];
        }

        public Dictionary<string, object> GetRelationship(string type, string label1, string from, string label2, string to)
        {
            int node1 = GetNodeKeyId(label1, from);
            int node2 = GetNodeKeyId(label2, to);
            if (node1 == -1 || node2 == -1) { return null; }

            long countId = ((long)node1 << 32) + node2;
            int count = relatedCounts.get(type).get(countId);
            if (count == 0) { return null; }
            int relId = getRelationshipKeyId(type, 1, node1, node2);

            return relationships.get(relId);
        }

        public Dictionary<string, object> GetRelationship(string type, string label1, string from, string label2, string to, int number)
        {
            int node1 = GetNodeKeyId(label1, from);
            int node2 = GetNodeKeyId(label2, to);
            if (node1 == -1 || node2 == -1) { return null; }

            int relId = GetRelationshipKeyId(type, number, node1, node2);

            return relationships.get(relId);
        }

        // Relationship Properties
        public object GetRelationshipProperty(int id, string property)
        {
            Dictionary<string, object> relationship = relationships.get(id);
            string type = (string)relationship["~type"];
            int node1 = (int)relationship["~incoming_node_id"];
            int node2 = (int)relationship["~outgoing_node_id"];
            object countobject = null;
            relationship.TryGetValue("~count", out countobject);    // TO DO - Make an extension method to get ~count
            int count = countobject != null ? (int)countobject : 0;
            int relId = getRelationshipKeyId(type, count, node1, node2);
            return relationships.get(relId).get(property);
        }

        public object GetRelationshipProperty(string type, string label1, string from, string label2, string to, string property)
        {
            int node1 = GetNodeKeyId(label1, from);
            int node2 = GetNodeKeyId(label2, to);
            if (node1 == -1 || node2 == -1) { return null; }

            long countId = ((long)node1 << 32) + node2;
            int count = relatedCounts.get(type).get(countId);
            if (count == 0) { return null; }
            int relId = getRelationshipKeyId(type, 1, node1, node2);
            return relationships.get(relId).get(property);
        }

        public object getRelationshipProperty(string type, string label1, string from, string label2, string to, int number, string property)
        {
            int node1 = GetNodeKeyId(label1, from);
            int node2 = GetNodeKeyId(label2, to);
            if (node1 == -1 || node2 == -1) { return null; }

            long countId = ((long)node1 << 32) + node2;
            int count = relatedCounts.get(type).get(countId);
            if (count == 0 || count < number) { return null; }
            int relId = getRelationshipKeyId(type, number, node1, node2);
            return relationships.get(relId).get(property);
        }

        private bool ContainsMetaProperties(Dictionary<string, object> properties)
        {
            foreach (string key in properties.Keys)
            {
                if (key.StartsWith("~"))
                {
                    return true;
                }
            }
            return false;
        }

        public bool UpdateRelationshipProperties(int id, Dictionary<string, object> properties)
        {
            if (ContainsMetaProperties(properties)) return false;
            relationships.get(id).AddAll(properties);
            return true;
        }

        public bool UpdateRelationshipProperties(string type, string label1, string from, string label2, string to, Dictionary<string, object> properties)
        {
            if (ContainsMetaProperties(properties)) return false;
            int node1 = GetNodeKeyId(label1, from);
            int node2 = GetNodeKeyId(label2, to);
            if (node1 == -1 || node2 == -1) { return false; }

            long countId = ((long)node1 << 32) + node2;
            int count = relatedCounts.get(type).get(countId);
            if (count == 0) { return false; }
            int relId = getRelationshipKeyId(type, 1, node1, node2);

            relationships.get(relId).AddAll(properties);
            return true;
        }

        public bool updateRelationshipProperties(string type, string label1, string from, string label2, string to, int number, Dictionary<string, object> properties)
        {
            if (ContainsMetaProperties(properties)) return false;
            int node1 = GetNodeKeyId(label1, from);
            int node2 = GetNodeKeyId(label2, to);
            if (node1 == -1 || node2 == -1) { return false; }

            long countId = ((long)node1 << 32) + node2;
            int count = relatedCounts.get(type).get(countId);
            if (count == 0 || count < number) { return false; }
            int relId = getRelationshipKeyId(type, number, node1, node2);
            relationships.get(relId).AddAll(properties);
            return true;
        }

        public bool DeleteRelationshipProperties(int id)
        {
            Dictionary<string, object> properties = relationships.get(id);
            foreach (string key in properties.Keys)
            {
                if (!key.StartsWith("~"))
                {
                    properties.Remove(key);
                }
            }
            return true;
        }

        public bool DeleteRelationshipProperties(string type, string label1, string from, string label2, string to)
        {
            int node1 = GetNodeKeyId(label1, from);
            int node2 = GetNodeKeyId(label2, to);
            if (node1 == -1 || node2 == -1) { return false; }

            long countId = ((long)node1 << 32) + node2;
            int count = relatedCounts.get(type).get(countId);
            if (count == 0) { return false; }
            int relId = getRelationshipKeyId(type, 1, node1, node2);

            Dictionary<string, object> properties = new Dictionary<string, object>();

            properties.Add("~incoming_node_id", node1);
            properties.Add("~outgoing_node_id", node2);
            properties.Add("~type", type);
            properties.Add("~id", relId);

            relationships.Add(relId, properties);
            return true;
        }

        public bool DeleteRelationshipProperties(string type, string label1, string from, string label2, string to, int number)
        {
            int node1 = GetNodeKeyId(label1, from);
            int node2 = GetNodeKeyId(label2, to);
            if (node1 == -1 || node2 == -1) { return false; }

            long countId = ((long)node1 << 32) + node2;
            int count = relatedCounts.get(type).get(countId);
            if (count == 0 || count < number) { return false; }
            int relId = getRelationshipKeyId(type, number, node1, node2);
            Dictionary<string, object> properties = new Dictionary<string, object>();

            properties.Add("~incoming_node_id", node1);
            properties.Add("~outgoing_node_id", node2);
            properties.Add("~type", type);
            properties.Add("~id", relId);
            properties.Add("~count", count);
            relationships.Add(relId, properties);

            return true;
        }

        public bool UpdateRelationshipProperty(int id, string property, object value)
        {
            if (property.StartsWith("~")) return false;
            relationships.get(id).Add(property, value);
            return true;
        }

        public bool UpdateRelationshipProperty(string type, string label1, string from, string label2, string to, string property, object value)
        {
            if (property.StartsWith("~")) return false;
            int node1 = GetNodeKeyId(label1, from);
            int node2 = GetNodeKeyId(label2, to);
            if (node1 == -1 || node2 == -1) { return false; }

            long countId = ((long)node1 << 32) + node2;
            int count = relatedCounts.get(type).get(countId);
            if (count == 0) { return false; }
            int relId = GetRelationshipKeyId(type, 1, node1, node2);
            relationships.get(relId).Add(property, value);
            return true;
        }

        public bool UpdateRelationshipProperty(string type, string label1, string from, string label2, string to, int number, string property, object value)
        {
            if (property.StartsWith("~")) return false;
            int node1 = GetNodeKeyId(label1, from);
            int node2 = GetNodeKeyId(label2, to);
            if (node1 == -1 || node2 == -1) { return false; }

            long countId = ((long)node1 << 32) + node2;
            int count = relatedCounts.get(type).get(countId);
            if (count == 0 || count < number) { return false; }
            int relId = getRelationshipKeyId(type, number, node1, node2);
            relationships.get(relId).Add(property, value);
            return true;
        }

        public bool DeleteRelationshipProperty(int id, string property)
        {
            if (property.StartsWith("~")) return false;

            Dictionary<string, object> properties = relationships.get(id);
            if (properties.ContainsKey(property))
            {
                properties.Remove(property);
                return true;
            }
            return false;
        }

        public bool DeleteRelationshipProperty(string type, string label1, string from, string label2, string to, string property)
        {
            if (property.StartsWith("~")) return false;
            int node1 = GetNodeKeyId(label1, from);
            int node2 = GetNodeKeyId(label2, to);
            if (node1 == -1 || node2 == -1) { return false; }

            long countId = ((long)node1 << 32) + node2;
            int count = relatedCounts.get(type).get(countId);
            if (count == 0) { return false; }
            int relId = GetRelationshipKeyId(type, 1, node1, node2);

            Dictionary<string, object> properties = relationships.get(relId);
            if (properties.ContainsKey(property))
            {
                properties.Remove(property);
                return true;
            }
            return false;
        }

        public bool DeleteRelationshipProperty(string type, string label1, string from, string label2, string to, int number, string property)
        {
            if (property.StartsWith("~")) return false;
            int node1 = GetNodeKeyId(label1, from);
            int node2 = GetNodeKeyId(label2, to);
            if (node1 == -1 || node2 == -1) { return false; }

            long countId = ((long)node1 << 32) + node2;
            int count = relatedCounts.get(type).get(countId);
            if (count == 0 || count < number) { return false; }
            int relId = GetRelationshipKeyId(type, number, node1, node2);

            Dictionary<string, object> properties = relationships.get(relId);
            if (properties.ContainsKey(property))
            {
                properties.Remove(property);
                return true;
            }
            return true;
        }

        // Degrees
        public int GetNodeDegree(string label, string identifier)
        {
            return GetNodeDegree(label, identifier, Direction.ALL, new List<string>());
        }

        public int GetNodeDegree(string label, string identifier, Direction direction)
        {
            return GetNodeDegree(label, identifier, direction, new List<string>());
        }

        public int GetNodeDegree(string label, string identifier, Direction direction, string type)
        {
            return GetNodeDegree(label, identifier, direction, new List<string>() { type });
        }

        /// <summary>
        /// TO DO - this method needs a deep dive
        /// </summary>
        /// <param name="label"></param>
        /// <param name="key"></param>
        /// <param name="direction"></param>
        /// <param name="types"></param>
        /// <returns></returns>
        public int GetNodeDegree(string label, string key, Direction direction, List<string> types)
        {
            int id = GetNodeKeyId(label, key);
            if (id == -1) { return -1; }

            int count = 0;
            List<string> relTypes;
            if (types.Count == 0)
            {
                relTypes = new List<string>( { related.Keys } ); // fix it
            }
            else
            {
                types.retainAll(related.keySet());
                relTypes = types;
            }

            for (string type : relTypes)
            {
                ReversibleMultiMap rels = related.get(type);
                if (direction != Direction.IN)
                {
                    count += rels.getFromSize(id);
                }
                if (direction != Direction.OUT)
                {
                    count += rels.getToSize(id);
                }
            }
            return count;
        }

        // Traversing
        public List<Dictionary<string, object>> GetOutgoingRelationships(string label, string from)
        {
            return GetOutgoingRelationships(GetNodeKeyId(label, from));
        }

        public List<Dictionary<string, object>> GetOutgoingRelationships(int node1)
        {
            List<Dictionary<string, object>> nodeRelationships = new List<Dictionary<string, object>>();
            for (string type : related.keySet())
            {
                for (int rel : related.get(type).getRels(node1))
                {
                    nodeRelationships.Add(relationships.get(rel));
                }
            }
            return nodeRelationships;
        }

    public List<Dictionary<string, object>> GetOutgoingRelationships(string type, string label, string from)
    {
        return GetOutgoingRelationships(type, GetNodeKeyId(label, from));
    }

    public List<Dictionary<string, object>> GetOutgoingRelationships(string type, int node)
    {
        List<Dictionary<string, object>> nodeRelationships = new ArrayList<>();
        if (related.ContainsKey(type))
        {
            for (int rel : related.get(type).getRels(node))
            {
                nodeRelationships.Add(relationships.get(rel));
            }
        }
        return nodeRelationships;
    }

    public List<Dictionary<string, object>> GetIncomingRelationships(string label, string to)
    {
        return GetIncomingRelationships(GetNodeKeyId(label, to));
    }

    public List<Dictionary<string, object>> GetIncomingRelationships(int node2)
    {
        List<Dictionary<string, object>> nodeRelationships = new List<Dictionary<string, object>>();
        for (string type : related.keySet())
        {
            for (int rel : related.get(type).getRelsByValue(node2))
            {
                nodeRelationships.Add(relationships.get(rel));
            }
        }
        return nodeRelationships;
    }

    public List<Dictionary<string, object>> GetIncomingRelationships(string type, string label, string to)
    {
        return GetIncomingRelationships(type, GetNodeKeyId(label, to));
    }

    public List<Dictionary<string, object>> GetIncomingRelationships(string type, int node)
    {
        List<Dictionary<string, object>> nodeRelationships = new List<Dictionary<string, object>>();
        if (related.ContainsKey(type))
        {
            for (int rel : related.get(type).getRelsByValue(node))
            {
                nodeRelationships.Add(relationships.get(rel));
            }
        }
        return nodeRelationships;
    }

    // Ids
    public List<int> GetOutgoingRelationshipIds(string label, string from)
    {
        return GetOutgoingRelationshipIds(GetNodeKeyId(label, from));
    }

    public List<int> GetOutgoingRelationshipIds(int node1)
    {
        List<int> relationshipIds = new List<int>();
        for (string type : related.keySet())
        {
            relationshipIds.AddAll(related.get(type).getRels(node1));
        }
        return relationshipIds;
    }

    public List<int> GetOutgoingRelationshipIds(string type, string label, string from)
    {
        return GetOutgoingRelationshipIds(type, GetNodeKeyId(label, from));
    }

    public List<int> GetOutgoingRelationshipIds(string type, int node)
    {
        if (related.ContainsKey(type))
        {
            return new ArrayList<>(related.get(type).getRels(node));
        }
        else
        {
            return Collections.emptyList();
        }
    }

    public List<int> GetIncomingRelationshipIds(string label, string to)
    {
        return GetIncomingRelationshipIds(GetNodeKeyId(label, to));
    }

    public List<int> GetIncomingRelationshipIds(int node2)
    {
        List<int> relationshipIds = new ArrayList<>();
        for (string type : related.keySet())
        {
            relationshipIds.AddAll(related.get(type).getRelsByValue(node2));
        }
        return relationshipIds;
    }

    public List<int> GetIncomingRelationshipIds(string type, string label, string to)
    {
        return getIncomingRelationshipIds(type, GetNodeKeyId(label, to));
    }

    public List<int> GetIncomingRelationshipIds(string type, int node)
    {
        if (related.ContainsKey(type))
        {
            return new ArrayList<>(related.get(type).getRelsByValue(node));
        }
        else
        {
            return Collections.emptyList();
        }
    }

    // Nodes

    public object[] GetOutgoingRelationshipNodes(string type, string label, string from)
    {
        List<int> nodeIds;
        if (related.ContainsKey(type))
        {
            nodeIds = (List<int>)related.get(type).getNodes(GetNodeKeyId(label, from));
        }
        else
        {
            nodeIds = Collections.emptyList();
        }
        int size = nodeIds.size();
        object[] nodeArray = new object[size];
        for (int i = -1; ++i < size;)
        {
            nodeArray[i] = nodes.get(nodeIds.get(i));
        }
        return nodeArray;
    }

    public object[] GetOutgoingRelationshipNodes(string label, string from)
    {
        List<int> nodeIds = new ArrayList<>();
        for (string type : related.keySet())
        {
            nodeIds.AddAll(related.get(type).getNodes(GetNodeKeyId(label, from)));
        }
        int size = nodeIds.size();
        object[] nodeArray = new object[size];
        for (int i = -1; ++i < size;)
        {
            nodeArray[i] = nodes.get(nodeIds.get(i));
        }
        return nodeArray;
    }

    public object[] GetIncomingRelationshipNodes(string type, string label, string to)
    {
        List<int> nodeIds;
        if (related.ContainsKey(type))
        {
            nodeIds = (List<int>)related.get(type).getNodesByValue(GetNodeKeyId(label, to));
        }
        else
        {
            nodeIds = Collections.emptyList();
        }

        int size = nodeIds.size();
        object[] nodeArray = new object[size];
        for (int i = -1; ++i < size;)
        {
            nodeArray[i] = nodes.get(nodeIds.get(i));
        }
        return nodeArray;
    }

    public object[] GetIncomingRelationshipNodes(string label, string to)
    {
        List<int> nodeIds = new ArrayList<>();
        for (string type : related.keySet())
        {
            nodeIds.AddAll(related.get(type).getNodesByValue(GetNodeKeyId(label, to)));
        }
        int size = nodeIds.size();
        object[] nodeArray = new object[size];
        for (int i = -1; ++i < size;)
        {
            nodeArray[i] = nodes.get(nodeIds.get(i));
        }
        return nodeArray;
    }

    public object[] GetOutgoingRelationshipNodes(string type, int from)
    {
        List<int> nodeIds;
        if (related.ContainsKey(type))
        {
            nodeIds = (List<int>)related.get(type).getNodes(from);
        }
        else
        {
            nodeIds = Collections.emptyList();
        }
        int size = nodeIds.size();
        object[] nodeArray = new object[size];
        for (int i = -1; ++i < size;)
        {
            nodeArray[i] = nodes.get(nodeIds.get(i));
        }
        return nodeArray;
    }

    public object[] GetOutgoingRelationshipNodes(int from)
    {
        List<int> nodeIds = new ArrayList<>();
        for (string type : related.keySet())
        {
            nodeIds.AddAll(related.get(type).getNodes(from));
        }
        int size = nodeIds.size();
        object[] nodeArray = new object[size];
        for (int i = -1; ++i < size;)
        {
            nodeArray[i] = nodes.get(nodeIds.get(i));
        }
        return nodeArray;
    }

    public object[] GetIncomingRelationshipNodes(string type, int to)
    {
        List<int> nodeIds;
        if (related.ContainsKey(type))
        {
            nodeIds = (List<int>)related.get(type).getNodesByValue(to);
        }
        else
        {
            nodeIds = Collections.emptyList();
        }
        int size = nodeIds.size();
        object[] nodeArray = new object[size];
        for (int i = -1; ++i < size;)
        {
            nodeArray[i] = nodes.get(nodeIds.get(i));
        }
        return nodeArray;
    }

    public object[] GetIncomingRelationshipNodes(int to)
    {
        List<int> nodeIds = new ArrayList<>();
        for (string type : related.keySet())
        {
            nodeIds.AddAll(related.get(type).getNodesByValue(to));
        }
        int size = nodeIds.size();
        object[] nodeArray = new object[size];
        for (int i = -1; ++i < size;)
        {
            nodeArray[i] = nodes.get(nodeIds.get(i));
        }
        return nodeArray;
    }
    public List<int> GetOutgoingRelationshipNodeIds(string type, int from)
    {
        if (related.ContainsKey(type))
        {
            return (List<int>)related.get(type).getNodes(from);
        }
        else
        {
            return Collections.emptyList();
        }
    }

    public List<int> GetOutgoingRelationshipNodeIds(string type, string label, string from)
    {
        if (related.ContainsKey(type))
        {
            return (List<int>)related.get(type).getNodes(GetNodeKeyId(label, from));
        }
        else
        {
            return Collections.emptyList();
        }
    }

    public List<int> GetOutgoingRelationshipNodeIds(int from)
    {
        List<int> nodeIds = new ArrayList<>();
        for (string type : related.keySet())
        {
            nodeIds.AddAll(related.get(type).getNodes(from));
        }
        return nodeIds;
    }

    public List<int> GetOutgoingRelationshipNodeIds(string label, string from)
    {
        List<int> nodeIds = new ArrayList<>();
        for (string type : related.keySet())
        {
            nodeIds.AddAll(related.get(type).getNodes(GetNodeKeyId(label, from)));
        }
        return nodeIds;
    }

    public List<int> GetIncomingRelationshipNodeIds(int to)
    {
        List<int> nodeIds = new ArrayList<>();
        for (string type : related.keySet())
        {
            nodeIds.AddAll(related.get(type).getNodesByValue(to));
        }
        return nodeIds;
    }

    public List<int> GetIncomingRelationshipNodeIds(string label, string to)
    {
        List<int> nodeIds = new ArrayList<>();
        for (string type : related.keySet())
        {
            nodeIds.AddAll(related.get(type).getNodesByValue(GetNodeKeyId(label, to)));
        }
        return nodeIds;
    }

    public List<int> GetIncomingRelationshipNodeIds(string type, int to)
    {
        if (related.ContainsKey(type))
        {
            return (List<int>)related.get(type).getNodesByValue(to);
        }
        else
        {
            return Collections.emptyList();
        }
    }

    public List<int> GetIncomingRelationshipNodeIds(string type, string label, string to)
    {
        if (related.ContainsKey(type))
        {
            return (List<int>)related.get(type).getNodesByValue(GetNodeKeyId(label, to));
        }
        else
        {
            return Collections.emptyList();
        }
    }

    public IEnumerator<Dictionary<string, object>> GetAllNodes()
    {
        IntIterator[] intIterators = new IntIterator[nodeKeys.keySet().size()];
        int i = 0;
        for (string s : nodeKeys.keySet())
        {
            intIterators[i] = nodeKeys.get(s).values().iterator();
            i++;
        }
        return new NodeIterator(IntIterators.concat(intIterators)).invoke();
    }

    // I can't do this, see https://github.com/vigna/fastutil/issues/92
    public IEnumerator<int> getAllNodeIdsDoesNotWork()
    {
        IntIterator[] intIterators = new IntIterator[nodeKeys.keySet().size()];
        int i = 0;
        for (string s : nodeKeys.keySet())
        {
            intIterators[i] = nodeKeys.get(s).values().iterator();
            i++;
        }
        return IntIterators.concat(intIterators);
    }

    public IEnumerator<int> getAllNodeIds()
    {
        RoaringBitmap ids = new RoaringBitmap();

        for (string s : nodeKeys.keySet())
        {
            ids.Add(nodeKeys.get(s).values().toArray(new int[nodeKeys.get(s).size()]));
        }
        return ids.iterator();
    }

    public IEnumerator<Dictionary<string, object>> getNodes(string label)
    {
        if (nodeKeys.ContainsKey(label))
        {
            return new NodeIterator(nodeKeys.get(label).values().iterator()).invoke();
        }

        return null;
    }

    public IEnumerator<Dictionary<string, object>> getAllRelationships()
    {
        ArrayList<Iterator<int>> iterators = new ArrayList<>();
        related.keySet().forEach(key->iterators.Add(related.get(key).getAllRelsIter()));

        return new RelationshipIterator(Iterators.concat(iterators.iterator())).invoke();
    }

    public IEnumerator<int> getAllRelationshipIds()
    {
        //        ArrayList<Iterator> rels = new ArrayList<>();
        //        for (string s : related.keySet()) {
        //            rels.Add(related.get(s).getAllRelIds().iterator());
        //        }
        //
        //        return Iterators.concat(rels.iterator());

        RoaringBitmap ids = new RoaringBitmap();

        for (string s : related.keySet())
        {
            ids.Add(related.get(s).getAllRelIds().toArray(new int[related.get(s).getAllRels().size()]));
        }
        return ids.iterator();
    }

    public IEnumerator<Dictionary<string, object>> getRelationships(string type)
    {
        if (related.ContainsKey(type))
        {
            Iterator<int> iter = related.get(type).getAllRelsIter();
            return new RelationshipIterator(iter).invoke();
        }
        return null;
    }

    public bool Related(string label1, string from, string label2, string to)
    {
        return Related(label1, from, label2, to, Direction.ALL, new List<string>());
    }

    public bool Related(string label1, string from, string label2, string to, Direction direction, string type)
    {
        return Related(label1, from, label2, to, direction, new List<string>() { type });
    }

    public bool Related(string label1, string from, string label2, string to, Direction direction, List<string> types)
    {
        return Related(GetNodeKeyId(label1, from), GetNodeKeyId(label2, to), direction, types);
    }

    public bool Related(int node1, int node2, Direction direction, List<string> types)
    {
        if (node1 == -1 || node2 == -1) { return false; }
        List<string> relTypes;
        if (types.Count == 0)
        {
            relTypes = new List<string>(related.keySet());
        }
        else
        {
            types.retainAll(related.keySet());
            relTypes = types;
        }
        for (int i = relTypes.size() - 1; i >= 0; i--)
        {
            string type = relTypes.get(i);
            if (direction != Direction.IN)
            {
                if (relationshipKeys.get(type + 1).ContainsKey(((long)node1 << 32) + node2))
                {
                    return true;
                }
            }
            if (direction != Direction.OUT)
            {
                if (relationshipKeys.get(type + 1).ContainsKey(((long)node2 << 32) + node1))
                {
                    return true;
                }
            }
        }

        return false;
    }


    private class RelationshipIterator
    {
        private Iterator<int> iter;

        RelationshipIterator(Iterator<int> iter)
        {
            this.iter = iter;
        }

        Iterator<Dictionary<string, object>> invoke()
        {
            return new Iterator<Dictionary<string, object>>() {
                    @Override
                    public bool hasNext()
                    {
                        return iter.hasNext();
                    }

                    @Override
                    public Dictionary<string, object> next()
                    {
                        return relationships.get(iter.next());
                    }
            };
        }
    }

    private class NodeIterator
    {
        private IntIterator iter;

        NodeIterator(IntIterator iter)
        {
            this.iter = iter;
        }

        Iterator<Dictionary<string, object>> invoke()
        {
            return new IEnumerator<Dictionary<string, object>>() {
                    public override bool HasNext()
                    {
                        return iter.hasNext();
                    }

                    public override Dictionary<string, object> Next()
                    {
                        Dictionary<string, object> node = null;
                        try
                        {
                            int id = iter.nextInt();
                            node = nodes.get(id);
                        }
                        catch (ArrayIndexOutOfBoundsException ex)
                        {
                            ex.printStackTrace();
                        }
                        return node;
                    }
            }
        }
    }
}

