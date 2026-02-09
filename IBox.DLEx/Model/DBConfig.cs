namespace IBox.DLEx.Model
{
    public class DBConfig
    {
        private string? databaseId;
        private string? query;
        private string? fieldName;
        private ConvertType convertType;
        private bool executeQuerryAsync = false;

        public string? DatabaseId { get => databaseId; set => databaseId = value; }
        public string? Query { get => query; set => query = value; }
        public string? FieldName { get => fieldName; set => fieldName = value; }
        public ConvertType ConvertType { get => convertType; set => convertType = value; }
        public bool ExecuteQuerryAsync { get => executeQuerryAsync; set => executeQuerryAsync = value; }
    }

    public enum ConvertType
    {
        Value,
        Object,
        Array
    }
}