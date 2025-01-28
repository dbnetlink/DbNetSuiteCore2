namespace DbNetSuiteCore.Attributes
{
    class FilterExpressionAttribute : Attribute
    {
        public string Expression { get; private set; }

        public FilterExpressionAttribute(string expression)
        {
            this.Expression = expression;
        }
    }
}
