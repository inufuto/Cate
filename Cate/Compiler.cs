using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Inu.Language;

namespace Inu.Cate;

public abstract class Compiler
{
    public const int Failure = 1;
    public const int Success = 0;

    public static Compiler Instance { get; private set; } = null!;
    public readonly ByteOperation ByteOperation;
    public readonly WordOperation WordOperation;
    private readonly Tokenizer tokenizer = new();
    private readonly Dictionary<SourcePosition, string> errors = new();
    private List<Token>? tokens = null;
    private int tokenIndex;
    private readonly GlobalBlock globalBlock = new();
    private Block currentBlock;
    public Function? CurrentFunction;
    private LoopStatement? currentLoopStatement;
    private SwitchStatement? currentSwitchStatement;
    private BreakableStatement? currentBreakableStatement;
    private readonly ISet<string> externalNames = new SortedSet<string>();
    public readonly bool ConstantData;


    protected Compiler(ByteOperation byteOperation, WordOperation wordOperation, bool constantData = false)
    {
        Debug.Assert(Instance == null);
        ByteOperation = byteOperation;
        WordOperation = wordOperation;
        Instance = this;
        currentBlock = globalBlock;
        ReservedWord.AddWords(Keyword.Words);
        this.ConstantData = constantData;
    }


    private Variable AddStringVariable(int id)
    {
        var s = StringValue.FromId(id);
        var bytes = s.ToCharArray().Select(c => (int)c).ToList();
        bytes.Add(0);
        var elementValues = bytes.Select(b => (Constant)new ConstantInteger(IntegerType.ByteType, b)).ToList();
        var arrayType = new ArrayType(IntegerType.ByteType, elementValues.Count);
        var value = new ConstantArray(arrayType, elementValues);
        var name = LabelPrefix + "string" + id;
        var variableId = Identifier.Add(name);
        return globalBlock.AddVariable(variableId, arrayType, Visibility.Private, true, value);
    }


    public int Main(NormalArgument normalArgument)
    {
        var args = normalArgument.Values;
        if (args.Count < 1) {
            Console.Error.WriteLine("No source file.");
            return Failure;
        }
        var sourceName = Path.GetFullPath(args[0]);
        var assemblyName = Path.ChangeExtension(sourceName, "asm");
        return Compile(sourceName, assemblyName);
    }

    private void ShowError(SourcePosition position, string error)
    {
        if (errors.ContainsKey(position))
            return;
        var s = $"{position}: {error}";
        errors[position] = s;
        Console.Error.WriteLine(s);
    }

    private void ShowError(Error error)
    {
        ShowError(error.Position, error.Message);
    }

    private Token CurrentToken
    {
        get
        {
            Debug.Assert(tokens != null);
            return tokens[tokenIndex];
        }
    }

    private Token NextToken()
    {
        Debug.Assert(tokens != null);
        return tokens[++tokenIndex];
    }


    private int Compile(string sourceName, string assemblyName)
    {
        tokens = tokenizer.GetTokens(sourceName);

        tokenIndex = 0;
        globalBlock.Clear();
        currentBlock = globalBlock;
        ParseModule();

        Debug.Assert(currentBlock == globalBlock);
        Debug.Assert(CurrentFunction == null);

        if (errors.Count > 0)
            return Failure;

        WriteAssembly(assemblyName);

        return Success;
    }

    private bool ParseReservedWord(int id)
    {
        if (!CurrentToken.IsReservedWord(id))
            return false;
        NextToken();
        return true;
    }

    private void AcceptReservedWord(int id)
    {
        if (!ParseReservedWord(id)) {
            throw new Error(CurrentToken.Position, "Missing " + ReservedWord.FromId(id));
        }
    }

    private void ParseModule()
    {
        var match = true;
        while (match && !CurrentToken.IsEof()) {
            try {
                match =
                    ParseConstantDefinition() ||
                    ParseTypeDefinition() ||
                    ParseVariableDeclaration(null) ||
                    ParseFunctionDefinition();
                if (!match) {
                    ShowError(new SyntaxError(CurrentToken));
                }
            }
            catch (Error e) {
                ShowError(e);
            }
        }
    }

    private bool ParseConstantDefinition()
    {
        if (!CurrentToken.IsReservedWord(Keyword.ConstExpr)) { return false; }

        var token = NextToken();
        var type = ParseParameterizableType() ?? throw new SyntaxError(token);
        do {
            if (CurrentToken is Identifier identifier) {
                NextToken();
                AcceptReservedWord('=');
                var value = ParseConstantExpression();
                if (value != null) {
                    currentBlock.AddConstant(identifier, type, value);
                }
            }
            else {
                throw new SyntaxError(CurrentToken);
            }
        } while (ParseReservedWord(','));
        AcceptReservedWord(';');
        return true;
    }


    private bool ParseTypeDefinition()
    {
        if (!ParseReservedWord(Keyword.Struct)) { return false; }

        if (CurrentToken is not Identifier identifier) throw new SyntaxError(CurrentToken);
        NextToken();
        StructureType? baseStructureType = null;
        if (CurrentToken.IsReservedWord(':')) {
            NextToken();
            if (CurrentToken is not Identifier baseIdentifier) throw new SyntaxError(CurrentToken);
            var baseType = currentBlock.FindNamedType(baseIdentifier.Id);
            if (baseType == null) {
                throw new UndefinedIdentifierError(baseIdentifier);
            }
            if (baseType is not StructureType structureType) {
                throw new TypeMismatchError(baseIdentifier);
            }
            NextToken();
            baseStructureType = structureType;
        }
        var type = new StructureType(baseStructureType);
        currentBlock.AddType(identifier, type);

        if (ParseReservedWord('{')) {
            do {
                if (type.BaseType != null && CurrentToken.IsReservedWord('}')) continue;

                var token = CurrentToken;
                var memberType = ParseType() ?? throw new SyntaxError(token);
                do {
                    if (CurrentToken is Identifier memberIdentifier) {
                        type.AddMember(memberIdentifier.Id, memberType);
                        NextToken();
                    }
                    else {
                        throw new SyntaxError(CurrentToken);
                    }
                } while (ParseReservedWord(','));

                AcceptReservedWord(';');
            } while (!ParseReservedWord('}'));
        }

        AcceptReservedWord(';');
        return true;

    }

    private bool ParseVariableDeclaration(CompositeStatement? compositeStatement)
    {
        var mark = tokenIndex;
        {
            var visibility = currentBlock.IsGlobal() ? Visibility.Public : Visibility.Private;
            var @static = currentBlock.IsGlobal();
            if (CurrentToken is ReservedWord reservedWord) {
                switch (reservedWord.Id) {
                    case Keyword.Static:
                        NextToken();
                        visibility = Visibility.Private;
                        @static = true;
                        break;
                    case Keyword.Extern:
                        NextToken();
                        visibility = Visibility.External;
                        break;
                }
            }
            var constant = false;
            if (CurrentToken.IsReservedWord(Keyword.Const)) {
                NextToken();
                constant = true;
            }

            var type = ParseType();
            if (type == null || type.IsVoid()) {
                goto cancel;
            }
            do {
                if (CurrentToken.Type != TokenType.Identifier) {
                    goto cancel;
                }
                var identifier = CurrentToken;
                NextToken();
                if (!(CurrentToken.IsReservedWord(';') || CurrentToken.IsReservedWord(',') || CurrentToken.IsReservedWord('='))) {
                    goto cancel;
                }
                Constant? constantValue = null;
                Value? variableValue = null;
                if (constant && visibility != Visibility.External) {
                    if (!CurrentToken.IsReservedWord('=')) {
                        throw new Error(CurrentToken.Position, "Constant storage must be initialized.");
                    }
                    NextToken();
                    constantValue = type.ParseConstant(this);
                    if (constantValue == null) {
                        throw new Error(CurrentToken.Position, "Missing initial value.");
                    }
                    if (type is ArrayType arrayType && constantValue is ConstantArray constantArray && arrayType.ElementCount == null) {
                        type = new ArrayType(arrayType.ElementType, constantArray.Type.ElementCount);
                    }
                }
                else {
                    if (CurrentToken.IsReservedWord('=')) {
                        if (compositeStatement == null) {
                            throw new Error(CurrentToken.Position, "Not constant.");
                        }
                        NextToken();
                        var valueToken = CurrentToken;
                        variableValue = ParseExpression();
                        if (variableValue == null) {
                            throw new Error(valueToken.Position, "Missing initial value.");
                        }
                        if (type is ArrayType) {
                            throw new Error(valueToken.Position, "Array variable cannot be initialized.");
                        }
                        variableValue = variableValue.ConvertTypeTo(type) ?? throw new TypeMismatchError(valueToken);
                    }
                }
                var variable = currentBlock.AddVariable((Identifier)identifier, type, visibility, @static, constantValue);
                if (variableValue != null) {
                    var assignment = new Assignment(new VariableValue(variable), variableValue);
                    var statement = new ExpressionStatement(assignment);
                    Debug.Assert(compositeStatement != null);
                    compositeStatement.Statements.Add(statement);
                }
            } while (ParseReservedWord(','));
            AcceptReservedWord(';');
            return true;
        }
    cancel:
        tokenIndex = mark;
        return false;
    }

    private bool ParseFunctionDefinition()
    {
        var mark = tokenIndex;
        {
            var visibility = currentBlock.IsGlobal() ? Visibility.Public : Visibility.Private;
            if (CurrentToken is ReservedWord reservedWord) {
                switch (reservedWord.Id) {
                    case Keyword.Static:
                        NextToken();
                        visibility = Visibility.Private;
                        break;
                    case Keyword.Extern:
                        NextToken();
                        visibility = Visibility.External;
                        break;
                }
            }

            Type? type;
            if (ParseReservedWord(Keyword.Void)) {
                type = VoidType.Type;
            }
            else {
                type = ParseParameterizableType();
                if (type == null) {
                    goto cancel;
                }
            }

            if (CurrentToken is not Identifier identifier) {
                throw new SyntaxError(CurrentToken);
            }

            NextToken();
            if (!ParseReservedWord('(')) {
                goto cancel;
            }

            var function = new Function(globalBlock, identifier.Id, visibility, type);
            while (!ParseReservedWord(')')) {
                var token = CurrentToken;
                var parameterType = ParseParameterizableType() ?? throw new SyntaxError(token);
                var parameter = CurrentToken;
                int? id = null;
                if (CurrentToken is Identifier parameterIdentifier) {
                    id = parameterIdentifier.Id;
                    NextToken();
                }
                else if (visibility != Visibility.External) {
                    throw new Error(CurrentToken.Position, "Missing parameter name.");
                }

                if (!function.AddParameter(parameterType, id)) {
                    throw new MultipleIdentifierError(parameter);
                }

                if (!ParseReservedWord(',') && !CurrentToken.IsReservedWord(')')) {
                    throw new SyntaxError(CurrentToken);
                }
            }

            function = globalBlock.AddFunction(identifier, function);
            if (visibility == Visibility.External) {
                AcceptReservedWord(';');
            }
            else {
                currentBlock = function.CreateBlock();
                CurrentFunction = function;

                var statementToken = CurrentToken;
                var statement = ParseCompositeStatement() ?? throw new SyntaxError(statementToken);
                statement.BuildInstructions(function);

                currentBlock.End(function);
                Debug.Assert(currentBlock.Parent != null);
                currentBlock = currentBlock.Parent;
                CurrentFunction = null;
            }
        }
        return true;
    cancel:
        tokenIndex = mark;
        return false;
    }

    private Type? ParseType()
    {
        var mark = tokenIndex;
        {
            if (ParseReservedWord(Keyword.Void)) {
                return VoidType.Type;
            }
            Type? type = ParseParameterizableType();
            if (type == null) {
                if (CurrentToken is Identifier identifier) {
                    type = currentBlock.FindNamedType(identifier.Id);
                    if (type == null) {
                        goto cancel;
                    }

                    NextToken();
                }
            }

            if (type == null)
                return type;
            if (!ParseReservedWord('['))
                return type;
            var token = CurrentToken;
            int? elementCount = null;
            var value = ParseConstantExpression();
            if (value != null) {
                if (value is not ConstantInteger constantInteger) {
                    throw new Error(token.Position, "Must be integer");
                }

                elementCount = constantInteger.IntegerValue;
            }

            AcceptReservedWord(']');
            type = new ArrayType(type, elementCount);
            return type;
        }
    cancel:
        tokenIndex = mark;
        return null;
    }

    private ParameterizableType? ParseParameterizableType()
    {
        if (!ParseReservedWord(Keyword.Ptr))
            return ParsePrimitiveType();
        AcceptReservedWord('<');
        var token = CurrentToken;
        var elementType = ParseType() ?? throw new SyntaxError(token);
        AcceptReservedWord('>');
        return new PointerType(elementType);

    }

    private ParameterizableType? ParsePrimitiveType()
    {
        if (CurrentToken is not ReservedWord reservedWord)
            return null;
        ParameterizableType type;
        switch (reservedWord.Id) {
            case Keyword.Byte:
                type = IntegerType.ByteType;
                break;
            case Keyword.SByte:
                type = IntegerType.SignedByteType;
                break;
            case Keyword.Word:
                type = IntegerType.WordType;
                break;
            case Keyword.SWord:
                type = IntegerType.SignedWordType;
                break;
            case Keyword.Bool:
                type = BooleanType.Type;
                break;
            default:
                return null;
        }

        NextToken();
        return type;
    }


    private Statement ParseStatement()
    {
        var token = CurrentToken;

        if (ParseReservedWord(';')) {
            return new EmptyStatement();
        }

        var statement = ParseCompositeStatement();
        if (statement != null)
            return statement;

        statement = ParseIfStatement();
        if (statement != null)
            return statement;

        statement = ParseWhileStatement();
        if (statement != null)
            return statement;

        statement = ParseDoStatement();
        if (statement != null)
            return statement;

        statement = ParseForStatement();
        if (statement != null)
            return statement;

        statement = ParseRepeatStatement();
        if (statement != null)
            return statement;

        statement = ParseSwitchStatement();
        if (statement != null)
            return statement;

        statement = ParseCaseStatement();
        if (statement != null)
            return statement;

        statement = ParseDefaultStatement();
        if (statement != null)
            return statement;

        statement = ParseBreakStatement();
        if (statement != null)
            return statement;

        statement = ParseContinueStatement();
        if (statement != null)
            return statement;

        statement = ParseGotoStatement();
        if (statement != null)
            return statement;

        statement = ParseReturnStatement();
        if (statement != null)
            return statement;

        statement = ParseLabeledStatement();
        if (statement != null)
            return statement;

        var value = ParseExpression();
        if (value != null) {
            AcceptReservedWord(';');
            return new ExpressionStatement(value);
        }

        Debug.Assert(tokens != null);
        if (tokenIndex + 1 < tokens.Count) {
            NextToken();
        }
        throw new SyntaxError(token);
    }

    private Statement? ParseCompositeStatement()
    {
        if (!ParseReservedWord('{')) { return null; }

        var block = new LocalBlock(currentBlock);
        if (currentBlock is LocalBlock localBlock) {
            localBlock.AddBlock(block);
        }
        currentBlock = block;

        try {
            var compositeStatement = new CompositeStatement(block);
            while (!ParseReservedWord('}')) {
                try {
                    while (
                        ParseConstantDefinition() ||
                        ParseTypeDefinition() ||
                        ParseVariableDeclaration(compositeStatement)
                    ) { }
                    if (CurrentToken.IsReservedWord('}')) continue;
                    var statement = ParseStatement();
                    compositeStatement.Statements.Add(statement);
                }
                catch (Error e) {
                    ShowError(e);
                    break;
                }
            }
            return compositeStatement;
        }
        finally {
            Debug.Assert(currentBlock.Parent != null);
            currentBlock = currentBlock.Parent;
        }
    }

    private Statement? ParseLabeledStatement()
    {
        var mark = tokenIndex;
        {
            if (CurrentToken is not Identifier identifier)
                return null;
            NextToken();
            if (!ParseReservedWord(':'))
                goto cancel;
            Debug.Assert(CurrentFunction != null);
            NamedLabel namedLabel = CurrentFunction.AddNamedLabel(identifier);
            var statement = ParseStatement();
            return new LabeledStatement(namedLabel, statement);
        }
    cancel:
        tokenIndex = mark;
        return null;
    }

    private Statement? ParseIfStatement()
    {
        if (!ParseReservedWord(Keyword.If))
            return null;
        AcceptReservedWord('(');
        var expressionToken = CurrentToken;
        var value = ParseExpression();
        AcceptReservedWord(')');
        var booleanValue = (value?.ToBooleanValue()) ?? throw new MustBeBooleanError(expressionToken.Position);
        var trueStatement = ParseStatement();

        Statement? falseStatement = null;
        if (ParseReservedWord(Keyword.Else)) {
            falseStatement = ParseStatement();
        }

        return new IfStatement(booleanValue, trueStatement, falseStatement);
    }

    private Statement? ParseWhileStatement()
    {
        if (!ParseReservedWord(Keyword.While))
            return null;
        AcceptReservedWord('(');
        var expressionToken = CurrentToken;
        var value = ParseExpression();
        AcceptReservedWord(')');
        var booleanValue = (value?.ToBooleanValue()) ?? throw new MustBeBooleanError(expressionToken.Position);
        Debug.Assert(CurrentFunction != null);
        var whileStatement = new WhileStatement(booleanValue, CurrentFunction);

        var oldLoopStatement = currentLoopStatement;
        var oldBreakableStatement = currentBreakableStatement;
        currentBreakableStatement = currentLoopStatement = whileStatement;

        whileStatement.Statement = ParseStatement();

        currentBreakableStatement = oldBreakableStatement;
        currentLoopStatement = oldLoopStatement;

        return whileStatement;
    }

    private Statement? ParseDoStatement()
    {
        if (!ParseReservedWord(Keyword.Do))
            return null;

        Debug.Assert(CurrentFunction != null);
        var doStatement = new DoStatement(CurrentFunction);

        var statementToken = CurrentToken;
        var oldLoopStatement = currentLoopStatement;
        var oldBreakableStatement = currentBreakableStatement;
        currentBreakableStatement = currentLoopStatement = doStatement;

        doStatement.Statement = ParseStatement();

        currentBreakableStatement = oldBreakableStatement;
        currentLoopStatement = oldLoopStatement;
        if (doStatement.Statement == null) { throw new SyntaxError(statementToken); }

        AcceptReservedWord(Keyword.While);
        AcceptReservedWord('(');
        var expressionToken = CurrentToken;
        var value = ParseExpression();
        AcceptReservedWord(')');
        AcceptReservedWord(';');
        var booleanValue = (value?.ToBooleanValue()) ?? throw new MustBeBooleanError(expressionToken.Position);
        doStatement.Condition = booleanValue;

        return doStatement;
    }

    private Statement? ParseForStatement()
    {
        if (!ParseReservedWord(Keyword.For))
            return null;
        AcceptReservedWord('(');

        var mark = tokenIndex;
        {
            var pointerToken = CurrentToken;
            var pointer = ParseTrinomial();
            if (!ParseReservedWord(':'))
                goto cancel;
            if (pointer is not AssignableValue assignableValue) {
                throw new SyntaxError(pointerToken);
            }
            if (assignableValue.Type is not PointerType pointerType) {
                throw new Error(pointerToken.Position, "Must be a pointer.");
            }
            var arrayToken = CurrentToken;
            var array = ParseTrinomial() ?? throw new SyntaxError(arrayToken);
            AcceptReservedWord(')');

            if (array is not ConstantPointer constantPointer || constantPointer.ElementCount == null) {
                throw new Error(pointerToken.Position, "Must be an array.");
            }

            if (constantPointer.ElementCount == null || constantPointer.ElementCount.Value == 0) {
                throw new Error(pointerToken.Position, "Array is empty.");
            }
            if (!pointerType.ElementType.Equals(constantPointer.Type.ElementType)) {
                throw new TypeMismatchError(arrayToken);
            }

            Debug.Assert(CurrentFunction != null);
            var forEachStatement = new ForEachStatement(assignableValue, constantPointer, CurrentFunction);

            var oldLoopStatement = currentLoopStatement;
            var oldBreakableStatement = currentBreakableStatement;
            currentBreakableStatement = currentLoopStatement = forEachStatement;

            forEachStatement.Statement = ParseStatement();

            currentBreakableStatement = oldBreakableStatement;
            currentLoopStatement = oldLoopStatement;

            return forEachStatement;
        }
    cancel:
        tokenIndex = mark;
        {
            var initialize = ParseExpression();
            AcceptReservedWord(';');
            var value = ParseExpression();
            AcceptReservedWord(';');
            var expressionToken = CurrentToken;
            var booleanValue = (value?.ToBooleanValue()) ?? throw new MustBeBooleanError(expressionToken.Position);
            var update = ParseExpression();
            AcceptReservedWord(')');

            Debug.Assert(CurrentFunction != null);
            var forStatement = new ForStatement(initialize, booleanValue, update, CurrentFunction);

            var oldLoopStatement = currentLoopStatement;
            var oldBreakableStatement = currentBreakableStatement;
            currentBreakableStatement = currentLoopStatement = forStatement;

            forStatement.Statement = ParseStatement();

            currentBreakableStatement = oldBreakableStatement;
            currentLoopStatement = oldLoopStatement;

            return forStatement;
        }
    }

    private Statement? ParseRepeatStatement()
    {
        if (!ParseReservedWord(Keyword.Repeat))
            return null;
        AcceptReservedWord('(');
        var expressionToken = CurrentToken;
        var value = ParseExpression();
        AcceptReservedWord(')');

        if (value is not ConstantInteger constantInteger || value.Type.ByteCount != 1) {
            throw new Error(expressionToken.Position, "Must be a constant byte.");
        }
        Debug.Assert(CurrentFunction != null);
        var repeatStatement = new RepeatStatement(constantInteger.IntegerValue, CurrentFunction);

        var oldLoopStatement = currentLoopStatement;
        var oldBreakableStatement = currentBreakableStatement;
        currentBreakableStatement = currentLoopStatement = repeatStatement;

        repeatStatement.Statement = ParseStatement();

        currentBreakableStatement = oldBreakableStatement;
        currentLoopStatement = oldLoopStatement;

        return repeatStatement;
    }


    private Statement? ParseSwitchStatement()
    {
        if (!ParseReservedWord(Keyword.Switch))
            return null;
        AcceptReservedWord('(');
        var expressionToken = CurrentToken;
        var value = ParseExpression();
        AcceptReservedWord(')');
        if (value == null) {
            throw new SyntaxError(expressionToken);
        }
        if (value.Type is not IntegerType) {
            throw new TypeMismatchError(expressionToken);
        }
        Debug.Assert(CurrentFunction != null);
        var switchStatement = new SwitchStatement(value, CurrentFunction);
        var oldSwitchStatement = currentSwitchStatement;
        var oldBreakableStatement = currentBreakableStatement;
        currentBreakableStatement = currentSwitchStatement = switchStatement;
        switchStatement.Statement = ParseStatement();
        currentSwitchStatement = oldSwitchStatement;
        currentBreakableStatement = oldBreakableStatement;
        return switchStatement;
    }

    private Statement? ParseCaseStatement()
    {
        if (!ParseReservedWord(Keyword.Case))
            return null;
        var expressionToken = CurrentToken;
        var value = ParseExpression();
        if (value is not ConstantInteger constantInteger) {
            throw new Error(expressionToken.Position, "Must be a integer constant.");
        }
        if (currentSwitchStatement == null) {
            throw new Error(CurrentToken.Position, "No switch statement.");
        }
        AcceptReservedWord(':');
        var integer = new ConstantInteger(currentSwitchStatement.Type, constantInteger.IntegerValue);
        Debug.Assert(CurrentFunction != null);
        var caseStatement = new CaseStatement(integer, CurrentFunction);
        currentSwitchStatement.Add(caseStatement);
        caseStatement.Statement = ParseStatement();
        return caseStatement;
    }

    private Statement? ParseDefaultStatement()
    {
        if (!ParseReservedWord(Keyword.Default))
            return null;
        if (currentSwitchStatement == null) {
            throw new Error(CurrentToken.Position, "No switch statement.");
        }
        if (currentSwitchStatement.DefaultStatement != null) {
            throw new Error(CurrentToken.Position, "Multiple default.");
        }
        AcceptReservedWord(':');
        Debug.Assert(CurrentFunction != null);
        var defaultStatement = new DefaultStatement(CurrentFunction);
        currentSwitchStatement.DefaultStatement = defaultStatement;
        defaultStatement.Statement = ParseStatement();
        return defaultStatement;
    }

    private Statement? ParseBreakStatement()
    {
        if (!ParseReservedWord(Keyword.Break))
            return null;
        AcceptReservedWord(';');
        if (currentBreakableStatement == null) {
            throw new Error(CurrentToken.Position, "No switch or loop statement.");
        }
        return new BreakOrContinueStatement(currentBreakableStatement.BreakAnchor);
    }

    private Statement? ParseContinueStatement()
    {
        if (!ParseReservedWord(Keyword.Continue))
            return null;
        AcceptReservedWord(';');
        if (currentLoopStatement == null) {
            throw new Error(CurrentToken.Position, "No loop statement.");
        }
        return new BreakOrContinueStatement(currentLoopStatement.ContinueAnchor);
    }

    private Statement? ParseGotoStatement()
    {
        if (!ParseReservedWord(Keyword.Goto))
            return null;
        if (CurrentToken is not Identifier identifier)
            throw new SyntaxError(CurrentToken);
        NextToken();
        AcceptReservedWord(';');
        Debug.Assert(CurrentFunction != null);
        var namedLabel = CurrentFunction.FindNamedLabel(identifier);
        return new GotoStatement(namedLabel);

    }

    private Statement? ParseReturnStatement()
    {
        if (!ParseReservedWord(Keyword.Return))
            return null;
        Debug.Assert(CurrentFunction != null);
        Value? value = null;
        var functionType = CurrentFunction.Type;
        if (functionType.ByteCount > 0) {
            var valueToken = CurrentToken;
            value = ParseExpression();
            if (value != null) {
                var convertedValue = value.ConvertTypeTo(functionType);
                value = convertedValue ?? throw new TypeMismatchError(valueToken);
            }
        }
        AcceptReservedWord(';');
        return new ReturnStatement(value, CurrentFunction.ExitAnchor);
    }

    private Value? ParseExpression()
    {
        Value? value;
        do {
            value = ParseUnitExpression();
        } while (ParseReservedWord(','));
        return value;
    }

    private Value? ParseUnitExpression()
    {
        return ParseAssignmentExpression() ?? ParseTrinomial();
    }

    private static readonly Dictionary<int, int> AssignmentOperators = new()
    {
        { '=', 0},
        { Keyword.AddAssign, '+' },
        { Keyword.SubtractAssign, '-' },
        { Keyword.AndAssign, '&' },
        { Keyword.XorAssign, '^' },
        { Keyword.OrAssign, '|' },
        { Keyword.ShiftLeftAssign, Keyword.ShiftLeft },
        { Keyword.ShiftRightAssign, Keyword.ShiftRight },
        { Keyword.LogicalOrAssign, Keyword.LogicalOr },
        { Keyword.LogicalAndAssign, Keyword.LogicalAnd },
    };
    private Value? ParseAssignmentExpression()
    {
        var leftToken = CurrentToken;
        var leftValue = ParseTrinomial();
        if (leftValue == null) { return null; }
        if (leftValue is not AssignableValue assignableValue) {
            return leftValue;
        }

        if (CurrentToken is not ReservedWord operatorToken)
            return leftValue;
        if (!AssignmentOperators.TryGetValue(operatorToken.Id, out var operatorId)) {
            return leftValue;
        }

        if (leftValue.Type is not ParameterizableType) {
            throw new InvalidOperatorError(operatorToken);
        }

        NextToken();

        var rightToken = CurrentToken;
        var rightValue = ParseAssignmentExpression() ?? throw new SyntaxError(rightToken);
        if (operatorId != 0) {
            rightValue = leftValue.BinomialResult(rightToken.Position, operatorId, rightValue);
            if (rightValue == null) {
                throw new InvalidOperatorError(operatorToken);
            }
        }

        if (!assignableValue.CanAssign()) {
            throw new Error(leftToken.Position, "Cannot assign to constant.");
        }

        var convertedValue = rightValue.ConvertTypeTo(leftValue.Type) ?? throw new TypeMismatchError(rightToken);
        return new Assignment(assignableValue, convertedValue);

    }

    private Constant? ParseConstantExpression()
    {
        var token = CurrentToken;
        var value = ParseTrinomial();
        if (value != null && value is not Constant) {
            throw new MustBeConstantError(token.Position);
        }
        return (Constant?)value;
    }

    private Value? ParseTrinomial()
    {
        var mark = tokenIndex;
        {
            var conditionToken = CurrentToken;
            var conditionValue = ParseBinomial();
            if (conditionValue == null)
                goto cancel;
            if (!ParseReservedWord('?'))
                return conditionValue;
            var booleanValue = conditionValue.ToBooleanValue() ?? throw new TypeMismatchError(conditionToken);
            var trueToken = CurrentToken;
            var trueValue = ParseExpression() ?? throw new SyntaxError(trueToken);
            AcceptReservedWord(':');
            var falseToken = CurrentToken;
            var falseValue = ParseTrinomial() ?? throw new SyntaxError(falseToken);
            if (trueValue.Type.CombineType(falseValue.Type) is not ParameterizableType resultType) {
                throw new TypeMismatchError(falseToken);
            }
            return new Trinomial(resultType, booleanValue, trueValue, falseValue);
        }
    cancel:
        tokenIndex = mark;
        return null;
    }

    private static readonly int[]?[] BinomialLevels = {
        new int[] { Keyword.LogicalOr },
        new int[] { Keyword.LogicalAnd },
        new int[] { '|' },
        new int[] { '^' },
        new int[] { '&' },
        new int[] { Keyword.Equal, Keyword.NotEqual },
        new int[] { '<','>',Keyword.LessEqual,Keyword.GreaterEqual },
        new int[] { Keyword.ShiftLeft, Keyword.ShiftRight },
        new int[] { '+', '-' },
        new int[] { '*', '/', '%' },
        null
    };

    private Value? ParseBinomial(int level = 0)
    {
        var next = BinomialLevels[level + 1];
        Func<Value?> factorFunction;
        if (next == null || next.Length <= 0) {
            factorFunction = ParsePrefixExpression;
        }
        else {
            factorFunction = () => ParseBinomial(level + 1);
        }

        var left = factorFunction();
        if (left == null) {
            return null;
        }
        while (true) {
            if (CurrentToken is ReservedWord operatorToken) {
                var operatorId = operatorToken.Id;
                var binomialLevel = BinomialLevels[level];
                if (binomialLevel != null && !binomialLevel.Contains(operatorId))
                    return left;
                var rightToken = NextToken();
                var right = factorFunction() ?? throw new SyntaxError(rightToken);
                var result = left.BinomialResult(operatorToken.Position, (int)operatorId, right);
                left = result ?? throw new InvalidOperatorError(operatorToken);
            }
            else {
                return left;
            }
        }
    }

    private Value? ParsePrefixExpression()
    {
        var value = ParseSizeOf();
        if (value != null) { return value; }
        value = ParseCastExpression();
        if (value != null) { return value; }
        value = ParseMonomial();
        if (value != null) { return value; }
        value = ParseDereference();
        if (value != null) { return value; }
        value = ParseReference();
        if (value != null) { return value; }
        value = ParsePreIncrementOrDecrement();
        if (value != null) { return value; }
        return ParsePostfixExpression();
    }

    private Value? ParseSizeOf()
    {
        if (!ParseReservedWord(Keyword.Sizeof))
            return null;
        var expressionToken = CurrentToken;
        var type = ParseType();
        if (type != null)
            return new ConstantInteger(type?.ByteCount ?? 0);
        var expression = ParseExpression();
        if (expression != null) {
            return new ConstantInteger(expression.Type.ByteCount);
        }
        throw new SyntaxError(expressionToken);
    }

    private Value? ParseDereference()
    {
        if (!ParseReservedWord('*'))
            return null;
        var token = CurrentToken;
        var value = ParsePrefixExpression() ?? throw new SyntaxError(token);
        if (value.Type is not PointerType pointerType)
            throw new TypeMismatchError(token);
        return new Dereference(pointerType.ElementType, value);
    }

    private Value? ParseReference()
    {
        var token = CurrentToken;
        if (!ParseReservedWord('&'))
            return null;
        var value = ParsePrefixExpression();
        if (value is AssignableValue assignableValue) {
            return assignableValue.Reference(CurrentToken.Position);
        }
        throw new InvalidOperatorError((ReservedWord)token);
    }

    private Value? ParsePreIncrementOrDecrement()
    {
        if (CurrentToken is not ReservedWord operatorToken)
            return null;
        switch (operatorToken.Id) {
            case Keyword.Increment:
            case Keyword.Decrement: {
                    NextToken();
                    var value = ParsePrefixExpression();
                    if (value is AssignableValue assignableValue && value.Type is ParameterizableType) {
                        return new PreIncrementOrDecrement(operatorToken.Id, assignableValue);
                    }
                    throw new InvalidOperatorError(operatorToken);
                }
        }
        return null;
    }

    private Value? ParseCastExpression()
    {
        var mark = tokenIndex;
        {
            var operatorToken = CurrentToken;
            if (ParseReservedWord('(')) {
                var type = ParseType();
                if (type == null) { goto cancel; }
                if (!CurrentToken.IsReservedWord(')')) {
                    goto cancel;
                }
                NextToken();
                var token = CurrentToken;
                var value = ParsePrefixExpression() ?? throw new SyntaxError(token);
                value = value.CastTo(type);
                if (value == null) {
                    throw new InvalidOperatorError((ReservedWord)operatorToken);
                }
                return value;
            }
        }
    cancel:
        tokenIndex = mark;
        return null;
    }

    private static readonly int[] UnaryOperators = new int[]
    {
        '+', '-', '~', '!',
    };



    private Value? ParseMonomial()
    {
        if (CurrentToken is not ReservedWord operatorToken)
            return null;
        var id = operatorToken.Id;
        if (!UnaryOperators.Contains(id))
            return null;
        var rightToken = NextToken();
        var factor = ParsePrefixExpression() ?? throw new SyntaxError(rightToken);
        return factor.MonomialResult(operatorToken.Position, id);

    }

    private Value? ParsePostfixExpression()
    {
        var factorToken = CurrentToken;
        var value = ParseFactor();
        if (value == null) { return null; }
    repeat:
        if (CurrentToken is not ReservedWord operatorToken)
            return value;
        switch (operatorToken.Id) {
            case '[': {
                    // []
                    var expressionToken = NextToken();
                    var indexValue = ParseExpression();
                    ParseReservedWord(']');
                    if (indexValue == null) {
                        throw new SyntaxError(expressionToken);
                    }

                    if (value.Type is not PointerType pointerType)
                        throw new TypeMismatchError(factorToken);
                    if (indexValue is ConstantInteger constantInteger && value is not ConstantPointer) {
                        value = new Dereference(pointerType.ElementType, value, constantInteger.IntegerValue);
                    }
                    else {
                        var addedPointer = value.BinomialResult(expressionToken.Position, '+', indexValue) ?? throw new MustBeConstantError(expressionToken.Position);
                        value = new Dereference(pointerType.ElementType, addedPointer);
                    }
                    goto repeat;
                }
            case '.': {
                    NextToken();
                    if (CurrentToken is not Identifier identifier)
                        throw new SyntaxError(CurrentToken);
                    NextToken();
                    if (value.Type is StructureType structureType && value is AssignableValue assignableValue) {
                        value = structureType.MemberValue(identifier, assignableValue);
                    }
                    else {
                        throw new InvalidOperatorError(operatorToken);
                    }
                    goto repeat;
                }
            case Keyword.Arrow: {
                    NextToken();
                    if (CurrentToken is not Identifier identifier)
                        throw new SyntaxError(CurrentToken);
                    NextToken();
                    if (value.Type is PointerType pointerType) {
                        var elementType = pointerType.ElementType;
                        var dereference = new Dereference(elementType, value);
                        if (elementType is StructureType structureType) {
                            value = structureType.MemberValue(identifier, dereference);
                        }
                        else {
                            throw new InvalidOperatorError(operatorToken);
                        }
                    }
                    else {
                        throw new InvalidOperatorError(operatorToken);
                    }
                    goto repeat;
                }
            case Keyword.Increment:
            case Keyword.Decrement: {
                    NextToken();
                    if (value is not AssignableValue assignableValue || value.Type is not ParameterizableType)
                        throw new InvalidOperatorError(operatorToken);
                    value = new PostIncrementOrDecrement(operatorToken.Id, assignableValue);
                    goto repeat;
                }
        }
        return value;
    }

    private Value? ParseFactor()
    {
        Value? value;
        if (ParseReservedWord('(')) {
            value = ParseExpression();
            AcceptReservedWord(')');
            return value;
        }
        value = ParseNamedValue();
        return value ?? ParseLiteral();
    }

    private Value? ParseNamedValue()
    {
        if (CurrentToken is not Identifier identifier)
            return null;
        NextToken();
        var namedValue = currentBlock.FindNamedValueIncludingAncestors(identifier.Id) ?? throw new UndefinedIdentifierError(identifier);
        return namedValue switch
        {
            Variable { Type: ArrayType arrayType } variable => new ConstantPointer(
                arrayType.ToPointerType(), variable, 0, arrayType.ElementCount),
            Variable variable => new VariableValue(variable),
            NamedConstant namedConstant => namedConstant.Value,
            Function function => ParseFunctionCall(function),
            _ => throw new NotImplementedException()
        };
    }

    private Value? ParseFunctionCall(Function function)
    {
        var functionCall = new FunctionCall(function);
        AcceptReservedWord('(');
        var index = 0;
        foreach (var parameter in function.Parameters) {
            var parameterToken = CurrentToken;
            var parameterValue = ParseUnitExpression() ?? throw new SyntaxError(parameterToken);
            var convertedValue = parameterValue.ConvertTypeTo(parameter.Type) ?? throw new TypeMismatchError(parameterToken);
            functionCall.AddParameter(convertedValue);
            if (++index < function.Parameters.Count) {
                AcceptReservedWord(',');
            }
        }
        AcceptReservedWord(')');
        return functionCall;
    }

    private Value? ParseLiteral()
    {
        switch (CurrentToken) {
            case NumericValue numericValue: {
                    var intValue = numericValue.Value;
                    NextToken();
                    return new ConstantInteger(intValue);
                }
            case StringValue stringValue: {
                    var id = stringValue.Id;
                    NextToken();
                    Variable stringVariable = AddStringVariable(id);
                    return new ConstantPointer(new PointerType(IntegerType.ByteType), stringVariable, 0);
                }
            case ReservedWord reservedWord:
                switch (reservedWord.Id) {
                    case Keyword.True:
                        NextToken();
                        return new ConstantBoolean(true);
                    case Keyword.False:
                        NextToken();
                        return new ConstantBoolean(false);
                    case Keyword.NullPtr:
                        NextToken();
                        return new NullPointer();
                }
                break;
        }
        return null;
    }

    public ConstantInteger ParseConstantInteger()
    {
        var token = CurrentToken;
        var value = ParseConstantExpression();
        if (value is not ConstantInteger constantInteger) {
            throw new TypeMismatchError(token);
        }
        return constantInteger;
    }

    public ConstantBoolean ParseConstantBoolean()
    {
        var token = CurrentToken;
        var value = ParseConstantExpression();
        if (value is not ConstantBoolean constantBoolean) {
            throw new TypeMismatchError(token);
        }
        return constantBoolean;
    }

    public Constant ParseConstantPointer()
    {
        var token = CurrentToken;
        var value = ParseConstantExpression();
        if (value is NullPointer) return value;
        if (value is not ConstantPointer constantInteger) {
            throw new TypeMismatchError(token);
        }
        return constantInteger;
    }

    public ConstantArray? ParseConstantArray(ArrayType type)
    {
        var elementType = type.ElementType;
        if (CurrentToken.Type == TokenType.StringValue) {
            if (elementType is not IntegerType integerType) {
                throw new TypeMismatchError(CurrentToken);
            }

            var s = CurrentToken.ToString() ?? "";
            var elementValues = Encoding.ASCII.GetBytes(s).Select(c => new ConstantInteger(integerType, c)).Cast<Constant>().ToList();
            NextToken();
            elementValues.Add(new ConstantInteger(integerType, 0));
            var arrayType = type.ElementCount != null ? type : new ArrayType(type.ElementType, elementValues.Count);
            return new ConstantArray(arrayType, elementValues);
        }
        {
            if (!ParseReservedWord('{'))
                return null;
            var elementValues = new List<Constant>();
            while ((type.ElementCount == null || elementValues.Count < type.ElementCount) && !CurrentToken.IsReservedWord('}')) {
                var elementValue = elementType.ParseConstant(this) ?? elementType.DefaultValue();
                elementValues.Add(elementValue);
                if (CurrentToken.IsReservedWord('}')) {
                    break;
                }

                AcceptReservedWord(',');
            }
            AcceptReservedWord('}');
            if (type.ElementCount == null) {
                type = new ArrayType(type.ElementType, elementValues.Count);
            }
            return new ConstantArray(type, elementValues);
        }
    }

    public ConstantStructure? ParseConstantStructure(StructureType type)
    {
        if (!ParseReservedWord('{'))
            return null;
        var memberValues = new List<Constant>();
        while (memberValues.Count < type.Members.Count && !CurrentToken.IsReservedWord('}')) {
            var memberType = type.Members[memberValues.Count].Type;
            var memberValue = memberType.ParseConstant(this) ?? memberType.DefaultValue();
            memberValues.Add(memberValue);
            if (CurrentToken.IsReservedWord('}')) { break; }
            AcceptReservedWord(',');
        }
        AcceptReservedWord('}');
        return new ConstantStructure(type, memberValues);
    }


    private void WriteAssembly(string assemblyName)
    {
        using var writer = new StreamWriter(assemblyName, false, Encoding.UTF8);
        WriteAssembly(writer);
        writer.Close();
    }

    protected virtual void WriteAssembly(StreamWriter writer)
    {
        var codeOffset = 0;
        var dataOffset = 0;
        globalBlock.WriteAssembly(writer, ref codeOffset, ref dataOffset);
        MakeAlignment(writer, "cseg", ref codeOffset);
        MakeAlignment(writer, "dseg", ref dataOffset);
        if (!externalNames.Any())
            return;
        writer.WriteLine();
        foreach (var externalName in externalNames) {
            writer.WriteLine("\textrn\t" + externalName);
        }
    }

    public virtual void AddSavingRegister(ISet<Register> registers, Register register)
    {
        if (registers.Contains(register)) return;

        if (register is ByteRegister byteRegister) {
            var pairRegister = byteRegister.PairRegister;
            if (pairRegister != null) {
                if (registers.Contains(pairRegister)) return;
                if (registers.Any(r => r is WordRegister p && Equals(p, pairRegister))) return;
                var added = new HashSet<Register>();
                var removed = new HashSet<Register>();
                foreach (var r in registers) {
                    if (r is not ByteRegister b || !Equals(b.PairRegister, pairRegister)) continue;
                    added.Add(pairRegister);
                    removed.Add(b);
                }
                if (added.Any()) {
                    foreach (var r in removed) {
                        registers.Remove(r);
                    }
                    foreach (var r in added) {
                        registers.Add(r);
                    }
                    return;
                }
            }
        }
        if (register is WordRegister wordRegister) {
            if (registers.Any(r => r is WordRegister p && Equals(p, wordRegister))) return;
            var added = new HashSet<Register>();
            var removed = new HashSet<Register>();
            foreach (var r in registers) {
                if (r is not ByteRegister b || !Equals(b.PairRegister, wordRegister)) continue;
                added.Add(wordRegister);
                removed.Add(b);
            }
            if (added.Any()) {
                foreach (var r in removed) {
                    registers.Remove(r);
                }
                foreach (var r in added) {
                    registers.Add(r);
                }
                return;
            }
        }
        registers.Add(register);
    }


    protected ISet<Register> SavingRegisters(Register register)
    {
        return SavingRegisters(new List<Register> { register });
    }

    public ISet<Register> SavingRegisters(IEnumerable<Register> registers)
    {
        var savingRegisters = new HashSet<Register>();
        foreach (var register in registers) {
            AddSavingRegister(savingRegisters, register);
        }
        return savingRegisters;
    }


    public virtual void SaveRegisters(StreamWriter writer, ISet<Register> registers, Function instruction)
    {
        var set = SavingRegisters(registers).ToImmutableSortedSet();
        foreach (var r in set) {
            r.Save(writer, null, null, 0);
        }
    }

    public virtual void SaveRegisters(StreamWriter writer, IEnumerable<Variable> variables, Instruction? instruction,
        int tabCount)
    {
        var dictionary = DistinctRegisters(variables);
        foreach (var (register, list) in dictionary.OrderBy(p => p.Key)) {
            var comment = "\t; " + string.Join(',', list.Select(v => v.Name).ToArray());
            register.Save(writer, comment, instruction, tabCount);
        }
    }

    public virtual void RestoreRegisters(StreamWriter writer, ISet<Register> registers, int byteCount)
    {
        foreach (var register in SavingRegisters(registers).ToImmutableSortedSet().Reverse()) {
            RestoreRegister(writer, register, byteCount);
        }
    }

    protected virtual void RestoreRegister(StreamWriter writer, Register register, int byteCount)
    {
        register.Restore(writer, null, null, 0);
    }

    public virtual void RestoreRegisters(StreamWriter writer, IEnumerable<Variable> variables, Instruction? instruction,
        int tabCount)
    {
        var dictionary = DistinctRegisters(variables);
        foreach (var (register, list) in dictionary.OrderByDescending(p => p.Key)) {
            var comment = "\t; " + string.Join(',', list.Select(v => v.Name).ToArray());
            register.Restore(writer, comment, instruction, tabCount);
        }
    }

    protected Dictionary<Register, List<Variable>> DistinctRegisters(IEnumerable<Variable> variables)
    {
        var dictionary = new Dictionary<Register, List<Variable>>();
        foreach (var variable in variables) {
            Debug.Assert(variable.Register != null);
            var registers = new HashSet<Register>();
            AddSavingRegister(registers, variable.Register);
            foreach (var register in registers) {
                if (dictionary.TryGetValue(register, out var list)) {
                    list.Add(variable);
                }
                else {
                    dictionary[register] = [variable];
                }
            }
        }
        return dictionary;
    }

    public abstract void AllocateRegisters(List<Variable> variables, Function function);
    public abstract Register? ParameterRegister(int index, ParameterizableType type);
    public abstract Register? ReturnRegister(ParameterizableType type);

    public LoadInstruction CreateLoadInstruction(Function function, AssignableOperand destinationOperand,
        Operand sourceOperand)
    {
        return destinationOperand.Type.ByteCount == 1
            ? CreateByteLoadInstruction(function, destinationOperand, sourceOperand)
            : CreateWordLoadInstruction(function, destinationOperand, sourceOperand);
    }


    protected virtual LoadInstruction CreateByteLoadInstruction(Function function, AssignableOperand destinationOperand,
        Operand sourceOperand)
    {
        return new ByteLoadInstruction(function, destinationOperand, sourceOperand);
    }

    protected virtual LoadInstruction CreateWordLoadInstruction(Function function, AssignableOperand destinationOperand,
        Operand sourceOperand)
    {
        return new WordLoadInstruction(function, destinationOperand, sourceOperand);
    }

    public abstract BinomialInstruction CreateBinomialInstruction(Function function, int operatorId,
        AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand);
    public abstract MonomialInstruction CreateMonomialInstruction(Function function, int operatorId,
        AssignableOperand destinationOperand, Operand sourceOperand);
    public abstract ResizeInstruction CreateResizeInstruction(Function function,
        AssignableOperand destinationOperand,
        IntegerType destinationType, Operand sourceOperand, IntegerType sourceType);
    public abstract CompareInstruction CreateCompareInstruction(Function function, int operatorId,
        Operand leftOperand,
        Operand rightOperand,
        Anchor anchor);
    public abstract JumpInstruction CreateJumpInstruction(Function function, Anchor anchor);
    public abstract SubroutineInstruction CreateSubroutineInstruction(Function function, Function targetFunction,
        AssignableOperand? destinationOperand, List<Operand> sourceOperands);
    public abstract ReturnInstruction CreateReturnInstruction(Function function, Operand? sourceOperand,
        Anchor anchor);
    public abstract DecrementJumpInstruction CreateDecrementJumpInstruction(Function function,
        AssignableOperand operand,
        Anchor anchor);
    public abstract ReadOnlySpan<char> EndOfFunction { get; }
    public abstract MultiplyInstruction CreateMultiplyInstruction(Function function,
        AssignableOperand destinationOperand,
        Operand leftOperand, int rightValue);

    public abstract IEnumerable<Register> IncludedRegisters(Register register);

    public void AddExternalName(string externalName)
    {
        externalNames.Add(externalName);
    }


    public static Type ToByteType(Operand operand)
    {
        Type newType;
        switch (operand.Type) {
            case IntegerType integerType:
                Debug.Assert(integerType.ByteCount == 2);
                newType = new IntegerType(1, integerType.Signed);
                break;
            case PointerType pointerType:
                newType = new PointerType(pointerType.ElementType);
                break;
            default:
                throw new NotImplementedException();
        }
        return newType;
    }

    public virtual bool IsAssignedRegisterPrior() => false;
    public abstract Operand LowByteOperand(Operand operand);
    public abstract Operand HighByteOperand(Operand operand);

    public abstract void CallExternal(Instruction instruction, string functionName);

    public virtual void WriteBeginningOfFunction(StreamWriter writer, Function function) { }

    public virtual void WriteEndOfFunction(StreamWriter writer, Function function)
    {
        writer.WriteLine(EndOfFunction);
    }

    public virtual int Alignment => 1;
    public virtual IntegerType CounterType => IntegerType.ByteType;
    public virtual string ParameterPrefix => "@";
    public virtual string LabelPrefix => "@";
    public virtual string PointerConstantDirective => "defw";
    public virtual int PointerByteCount => 2;

    public int AlignedSize(int size)
    {
        while (size % Alignment != 0) {
            ++size;
        }
        return size;
    }

    public static void WriteAlignment(StreamWriter writer, int count)
    {
        if (count > 0) {
            writer.WriteLine("\tdefs " + count + " ; alignment");
        }
    }

    public void MakeAlignment(StreamWriter writer, ref int offset)
    {
        var newOffset = AlignedSize(offset);
        WriteAlignment(writer, newOffset - offset);
        offset = newOffset;
    }

    public void MakeAlignment(StreamWriter writer, int elementSize, ref int offset)
    {
        var alignment = elementSize < Alignment ? Alignment / (Alignment / elementSize) : Alignment;
        var newOffset = offset;
        while (newOffset % alignment != 0) {
            ++newOffset;
        }
        WriteAlignment(writer, newOffset - offset);
        offset = newOffset;
    }

    public void MakeAlignment(StreamWriter writer, string name, ref int offset)
    {
        var gap = AlignedSize(offset) - offset;
        if (gap <= 0) return;
        writer.WriteLine();
        writer.WriteLine(name);
        MakeAlignment(writer, ref offset);
    }

    public virtual void RemoveSavingRegister(ISet<Register> savedRegisters, Register returnRegister) { }

    protected Register? AllocatableRegister<T>(Variable variable, IEnumerable<T> registers, Function function) where T : Register
    {
        T? maxRegister = null;
        int? max = null;
        foreach (var register in registers) {
            if (Conflict(variable.Intersections, register)) continue;
            var adaptability = RegisterAdaptability(variable, register);
            if (adaptability != null && (max == null || adaptability > max)) {
                maxRegister = register;
                max = adaptability;
            }
        }
        return maxRegister;
    }

    protected static bool Conflict<T>(IEnumerable<Variable> variables, T register) where T : Register
    {
        return variables.Any(v =>
            v.Register != null && register.Conflicts(v.Register));
    }

    protected virtual int? RegisterAdaptability(Variable variable, Register register)
    {
        var function = variable.Block.Function;
        Debug.Assert(function != null);
        var first = variable.Usages.First().Key;
        var last = variable.Usages.Last().Key;
        var sum = 0;
        for (var address = first; address <= last; ++address) {
            var instruction = function.Instructions[address];
            var adaptability = instruction.RegisterAdaptability(variable, register);
            if (adaptability == null) return null;
            sum += adaptability.Value;
        }
        return sum;
    }

    //public virtual void FirstInstruction(Instruction instruction) { }
}