namespace IBox.Common.Objects
{
    public class FindConfig
    {
        public string FieldName { get; set; } = string.Empty;
        public FindOption FindOption { get; set; } = 0;
        public string FindWithValue { get; set; } = string.Empty;
        public string AddToField { get; set; } = string.Empty;
    }

    public enum FindOption
    {
        Firt,
        Last,
        Any,
    }
}