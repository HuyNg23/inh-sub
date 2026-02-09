namespace IBox.DLEx.Model
{
    public class FormattingConfig
    {
        private FormatType? formatType;
        private string? fieldName;

        public FormatType? FormatType { get => formatType; set => formatType = value; }
        public string? FieldName { get => fieldName; set => fieldName = value; }
    }

    public enum FormatType
    {
        AddEnd,
        Replate,
        SplitToObject,
        ListDataToListObject,
        GetGUID,
        RandomNumber,
        GetDateTime
    }

    public class Replate : FormattingConfig
    {
        private string valueSource = string.Empty;
        private string valueDestination = string.Empty;

        public string ValueSource { get => valueSource; set => valueSource = value; }
        public string ValueDestination { get => valueDestination; set => valueDestination = value; }
    }

    public class AddEnd : FormattingConfig
    {
        private string? value;

        public string? Value { get => value; set => this.value = value; }
    }

    public class SplitToObject : FormattingConfig
    {
        private string charValueForSplit = string.Empty;
        private string modelJsonMappingForSplit = string.Empty;

        public string CharValueForSplit { get => charValueForSplit; set => charValueForSplit = value; }
        public string ModelJsonMappingForSplit { get => modelJsonMappingForSplit; set => modelJsonMappingForSplit = value; }
    }

    public class ListToObject : FormattingConfig
    {
        private string modelJsonMappingForListToObject = string.Empty;
        private string fieldNameDestination = string.Empty;

        public string ModelJsonMappingForListToListObject { get => modelJsonMappingForListToObject; set => modelJsonMappingForListToObject = value; }
        public string FieldNameDestination { get => fieldNameDestination; set => fieldNameDestination = value; }
    }
}