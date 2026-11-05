using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class AstEnumVariantCompletenessTests
{
    [TestCase(typeof(SqlStatementKind), "Unknown,Empty,Select,Insert,Update,Delete,CreateTable,AlterTable,DropTable,CreateIndex,DropIndex,CreateView,DropView,CreateTrigger,DropTrigger,CreateVirtualTable,Pragma,Analyze,Vacuum,Attach,Detach,Reindex,Begin,Commit,Rollback,Savepoint,Release,Explain,Merge,End")]
    [TestCase(typeof(SqlExpressionKind), "Unknown,Name,Literal,Parameter,Wildcard,Unary,Binary,FunctionCall,Parenthesized,Cast,Case,Exists,Subquery,RowValue,Between,In,Collate,Is,Like,Window,Distinctness,Extract,Array,Subscript,JsonOperator")]
    [TestCase(typeof(SqlLiteralKind), "Unknown,Null,Integer,Decimal,String,Blob,Boolean,CurrentDate,CurrentTime,CurrentTimestamp")]
    [TestCase(typeof(SqlUnaryOperatorKind), "None,Positive,Negative,BitwiseNot,Not")]
    [TestCase(typeof(SqlBinaryOperatorKind), "None,Add,Subtract,Multiply,Divide,Modulo,Concat,Equal,NotEqual,Less,LessOrEqual,Greater,GreaterOrEqual,BitwiseAnd,BitwiseOr,BitwiseXor,ShiftLeft,ShiftRight,And,Or,JsonExtract,JsonExtractText")]
    [TestCase(typeof(SqlJoinKind), "Unknown,Comma,Cross,Inner,Left,Right,Full,Natural,Semi,Anti,CrossApply,OuterApply,AsOf,Straight")]
    [TestCase(typeof(SqlJoinModifierKind), "None,Natural,Inner,Left,Right,Full,Cross,Outer,Semi,Anti,Apply,AsOf,Straight")]
    [TestCase(typeof(SqlJoinConstraintKind), "None,On,Using")]
    [TestCase(typeof(SqlTableReferenceKind), "Unknown,Named,Subquery,Function,Join,Parenthesized,Values")]
    [TestCase(typeof(SqlInsertSourceKind), "Unknown,Values,Select,DefaultValues")]
    [TestCase(typeof(SqlUpsertActionKind), "Unknown,DoNothing,DoUpdate")]
    [TestCase(typeof(SqlUpsertSyntaxKind), "Unknown,OnConflict,OnDuplicateKey")]
    [TestCase(typeof(SqlConflictTargetKind), "None,IndexedColumns,Constraint,DuplicateKey")]
    [TestCase(typeof(SqlConflictActionKind), "None,Rollback,Abort,Fail,Ignore,Replace")]
    [TestCase(typeof(SqlMergeMatchKind), "Unknown,Matched,NotMatchedByTarget,NotMatchedBySource")]
    [TestCase(typeof(SqlMergeActionKind), "Unknown,Update,Delete,Insert,DoNothing")]
    [TestCase(typeof(SqlConstraintKind), "Unknown,PrimaryKey,NotNull,Unique,Check,Default,Collate,References,Generated,ForeignKey")]
    [TestCase(typeof(SqlGeneratedColumnKind), "None,Virtual,Stored")]
    [TestCase(typeof(SqlAlterTableActionKind), "Unknown,RenameTable,RenameColumn,AddColumn,DropColumn")]
    [TestCase(typeof(SqlForeignKeyActionKind), "None,NoAction,Restrict,SetNull,SetDefault,Cascade")]
    [TestCase(typeof(SqlForeignKeyDeferrabilityKind), "None,Deferrable,NotDeferrable")]
    [TestCase(typeof(SqlForeignKeyInitialTimingKind), "None,Deferred,Immediate")]
    [TestCase(typeof(SqlForeignKeyClauseKind), "Unknown,OnDelete,OnUpdate,Match,Deferrability")]
    [TestCase(typeof(SqlTableOptionKind), "Unknown,WithoutRowId,Strict")]
    [TestCase(typeof(SqlTriggerTiming), "Unknown,Before,After,InsteadOf")]
    [TestCase(typeof(SqlTriggerEvent), "Unknown,Insert,Update,Delete")]
    [TestCase(typeof(SqlPragmaValueKind), "None,Equals,Parenthesized")]
    [TestCase(typeof(SqlTransactionActionKind), "Unknown,Begin,Commit,End,Rollback")]
    [TestCase(typeof(SqlTransactionMode), "None,Deferred,Immediate,Exclusive")]
    [TestCase(typeof(SqlExplainKind), "Unknown,Explain,QueryPlan")]
    [TestCase(typeof(SqlCompoundOperatorKind), "None,Union,UnionAll,Intersect,Except")]
    [TestCase(typeof(SqlCteMaterializationKind), "None,Materialized,NotMaterialized")]
    [TestCase(typeof(SqlOrderingDirection), "None,Asc,Desc")]
    [TestCase(typeof(SqlNullOrdering), "None,First,Last")]
    [TestCase(typeof(SqlWindowFrameKind), "None,Rows,Range,Groups")]
    [TestCase(typeof(SqlWindowFrameBoundaryKind), "None,UnboundedPreceding,Preceding,CurrentRow,Following,UnboundedFollowing")]
    [TestCase(typeof(SqlWindowFrameExclusionKind), "None,CurrentRow,Group,Ties,NoOthers")]
    [TestCase(typeof(SqlDistinctnessKind), "None,IsDistinctFrom,IsNotDistinctFrom")]
    [TestCase(typeof(SqlInTargetKind), "None,List,Subquery,Table,Function")]
    [TestCase(typeof(SqlCastSyntaxKind), "None,CastFunction,PostgreSql")]
    [TestCase(typeof(SqlOperatorExpressionKind), "None,Json,Custom")]
    [TestCase(typeof(SqlFetchDirectionKind), "None,First,Next")]
    [TestCase(typeof(SqlUnsupportedQueryClauseKind), "None,Qualify,Sample,Pivot,Unpivot,ConnectBy,Model,Other")]
    [TestCase(typeof(SqlUnsupportedSyntaxKind), "Unknown,UnknownStatement,DialectFeature,SkippedText")]
    [TestCase(typeof(SqlNameQuoteStyle), "None,DoubleQuote,Bracket,Backtick,SingleQuote")]
    public void AstEnum_ContainsEveryNamedVariantWithStableOrdinal(Type enumType, string commaSeparatedNames)
    {
        string[] expectedNames = commaSeparatedNames.Split(',');
        string[] actualNames = Enum.GetNames(enumType);
        Array actualValues = Enum.GetValues(enumType);

        Assert.That(actualNames, Is.EqualTo(expectedNames), enumType.Name);
        Assert.That(actualValues.Length, Is.EqualTo(expectedNames.Length), enumType.Name);
        int index = 0;
        while (index < actualValues.Length)
        {
            Assert.That(Convert.ToInt32(actualValues.GetValue(index)), Is.EqualTo(index), enumType.Name + "." + expectedNames[index]);
            index = index + 1;
        }
    }
}
