using DbNetSuiteCore.Attributes;

namespace DbNetSuiteCore.Enums
{
    public enum SearchOperator
    {
        [FilterExpression("= {0}")]
        EqualTo,
        [FilterExpression("<> {0}")]
        NotEqualTo,
        [FilterExpression("like {0}")]
        Contains,
        [FilterExpression("not like {0}")]
        DoesNotContain,
        [FilterExpression("like {0}")]
        StartsWith,
        [FilterExpression("not like {0}")]
        DoesNotStartWith,
        [FilterExpression("like {0}")]
        EndsWith,
        [FilterExpression("not like {0}")]
        DoesNotEndWith,
        [FilterExpression("in ({0})")]
        In,
        [FilterExpression("not in ({0})")]
        NotIn,
        [FilterExpression("> ({0})")]
        GreaterThan,
        [FilterExpression("< {0}")]
        LessThan,
        [FilterExpression("between {0} and {1}")]
        Between,
        [FilterExpression("not between {0} and {1}")]
        NotBetween,
        [FilterExpression(">= {0}")]
        NotLessThan,
        [FilterExpression("<= {0}")]
        NotGreaterThan,
        [FilterExpression("= {0}")]
        True,
        [FilterExpression("= {0}")]
        False,
        [FilterExpression("is null")]
        IsEmpty,
        [FilterExpression("is not null")]
        IsNotEmpty
    }
}
