namespace IBox.DLEx.Model
{
    public class Condition
    {
        private List<ConditionItem>? conditionItems;
        private string? nextStepTrue;
        private string? nextStepFalse;

        public List<ConditionItem>? ConditionItems { get => conditionItems; set => conditionItems = value; }
        public string? NextStepTrue { get => nextStepTrue; set => nextStepTrue = value; }
        public string? NextStepFalse { get => nextStepFalse; set => nextStepFalse = value; }
    }

    public class ConditionItem
    {
        private string? fieldName;
        private ConditionType? conditionType;
        private string? value;
        private List<ConditionItem>? conditions;

        public string? FieldName { get => fieldName; set => fieldName = value; }
        public ConditionType? ConditionType { get => conditionType; set => conditionType = value; }
        public string? Value { get => value; set => this.value = value; }
        public List<ConditionItem>? Conditions { get => conditions; set => conditions = value; }
    }

    public enum ConditionType
    {
        // number
        NumberLess,

        NumberLessOrEqual,
        NumberBigger,
        NumberBiggerOrEqual,
        NumberEqual,

        // datetime
        DateLess,

        DateLessOrEqual,
        DateBigger,
        DateBiggerOrEqual,
        DateEqual,
        DateSubtractionNowBiggerOrEqual,

        // text
        TextEqual,

        TextContain,
        TextIsNullOrEmpty,

        //Object
        ObjectIsNull,
    }
}