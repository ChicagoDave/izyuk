using System;
using System.Collections.Generic;
using System.Text;

namespace Izyuk.Core
{
    public interface IGraph
    {
        void Clear();

        // Relationship Types
        HashSet<string> GetRelationshipTypes();
        Dictionary<string, int> GetRelationshipTypesCount();
        int GetRelationshipTypeCount(string type);

        // Nodes
        int AddNode(string label, string key);
        int AddNode(string label, string key, Dictionary<string, object> properties);
        bool RemoveNode(string label, string key);
        Dictionary<string, object> GetNode(string label, string key);
        int GetNodeId(string label, string key);
        Dictionary<string, object> GetNodeById(int id);
        string GetNodeLabel(int id);
        string GetNodeKey(int id);

        // Node Properties
        object GetNodeProperty(string label, string key, string property);
        bool UpdateNodeProperties(string label, string key, Dictionary<string, object> properties);
        bool DeleteNodeProperties(string label, string key);
        bool UpdateNodeProperty(string label, string key, string property, object value);
        bool DeleteNodeProperty(string label, string key, string property);

        object GetNodeProperty(int id, string property);
        bool UpdateNodeProperties(int id, Dictionary<string, object> properties);
        bool DeleteNodeProperties(int id);
        bool UpdateNodeProperty(int id, string property, object value);
        bool DeleteNodeProperty(int id, string property);

        // Relationships
        int addRelationship(string type, string label1, string from, string label2, string to);
        int addRelationship(string type, string label1, string from, string label2, string to, Dictionary<string, object> properties);
        bool removeRelationship(int id);
        bool removeRelationship(string type, string label1, string from, string label2, string to);
        bool removeRelationship(string type, string label1, string from, string label2, string to, int number);
        Dictionary<string, object> GetRelationship(string type, string label1, string from, string label2, string to);
        Dictionary<string, object> GetRelationship(string type, string label1, string from, string label2, string to, int number);
        Dictionary<string, object> GetRelationshipById(int id);

        // Relationship Properties
        object GetRelationshipProperty(int id, string property);
        object GetRelationshipProperty(string type, string label1, string from, string label2, string to, string property);
        object GetRelationshipProperty(string type, string label1, string from, string label2, string to, int number, string property);
        bool UpdateRelationshipProperties(int id, Dictionary<string, object> properties);
        bool UpdateRelationshipProperties(string type, string label1, string from, string label2, string to, Dictionary<string, object> properties);
        bool UpdateRelationshipProperties(string type, string label1, string from, string label2, string to, int number, Dictionary<string, object> properties);
        bool DeleteRelationshipProperties(int id);
        bool DeleteRelationshipProperties(string type, string label1, string from, string label2, string to);
        bool DeleteRelationshipProperties(string type, string label1, string from, string label2, string to, int number);
        bool UpdateRelationshipProperty(int id, string property, object value);
        bool UpdateRelationshipProperty(string type, string label1, string from, string label2, string to, string property, object value);
        bool UpdateRelationshipProperty(string type, string label1, string from, string label2, string to, int number, string property, object value);
        bool DeleteRelationshipProperty(int id, string property);
        bool DeleteRelationshipProperty(string type, string label1, string from, string label2, string to, string property);
        bool DeleteRelationshipProperty(string type, string label1, string from, string label2, string to, int number, string property);

        // Node Degree
        int GetNodeDegree(string label, string key);
        int GetNodeDegree(string label, string key, Direction direction);
        int GetNodeDegree(string label, string key, Direction direction, string type);
        int GetNodeDegree(string label, string key, Direction direction, List<string> types);

        // Traversing
        List<Dictionary<string, object>> GetOutgoingRelationships(string label1, string from);
        List<Dictionary<string, object>> GetOutgoingRelationships(int from);
        List<Dictionary<string, object>> GetOutgoingRelationships(string type, string label1, string from);
        List<Dictionary<string, object>> GetOutgoingRelationships(string type, int from);
        List<Dictionary<string, object>> GetIncomingRelationships(string label1, string from);
        List<Dictionary<string, object>> GetIncomingRelationships(int from);
        List<Dictionary<string, object>> GetIncomingRelationships(string type, string label1, string from);
        List<Dictionary<string, object>> GetIncomingRelationships(string type, int from);

        List<int> GetOutgoingRelationshipIds(string label1, string from);
        List<int> GetOutgoingRelationshipIds(int from);
        List<int> GetOutgoingRelationshipIds(string type, string label1, string from);
        List<int> GetOutgoingRelationshipIds(string type, int from);
        List<int> GetIncomingRelationshipIds(string label1, string from);
        List<int> GetIncomingRelationshipIds(int from);
        List<int> GetIncomingRelationshipIds(string type, string label1, string from);
        List<int> GetIncomingRelationshipIds(string type, int from);


        List<int> GetOutgoingRelationshipNodeIds(string type, string label1, string from);
        List<int> GetOutgoingRelationshipNodeIds(string type, int from);
        List<int> GetOutgoingRelationshipNodeIds(string label1, string from);
        List<int> GetOutgoingRelationshipNodeIds(int from);

        object[] GetOutgoingRelationshipNodes(string type, string label1, string from);
        object[] GetIncomingRelationshipNodes(string type, string label2, string to);
        object[] GetOutgoingRelationshipNodes(string label1, string from);
        object[] GetIncomingRelationshipNodes(string label2, string to);

        List<int> GetIncomingRelationshipNodeIds(string type, string label2, string to);
        List<int> GetIncomingRelationshipNodeIds(string type, int to);
        List<int> GetIncomingRelationshipNodeIds(string label2, string to);
        List<int> GetIncomingRelationshipNodeIds(int to);

        object[] GetOutgoingRelationshipNodes(string type, int from);
        object[] GetIncomingRelationshipNodes(string type, int to);
        object[] GetOutgoingRelationshipNodes(int from);
        object[] GetIncomingRelationshipNodes(int to);

        // Extras
        IEnumerator<Dictionary<string, object>> GetAllNodes();
        IEnumerator<int> GetAllNodeIds();
        IEnumerator<Dictionary<string, object>> GetNodes(string label);
        IEnumerator<Dictionary<string, object>> GetAllRelationships();
        IEnumerator<int> GetAllRelationshipIds();
        IEnumerator<Dictionary<string, object>> GetRelationships(string type);

        // Related
        bool Related(string label1, string from, string label2, string to);
        bool Related(string label1, string from, string label2, string to, Direction direction, string type);
        bool Related(string label1, string from, string label2, string to, Direction direction, List<string> types);
    }
}
