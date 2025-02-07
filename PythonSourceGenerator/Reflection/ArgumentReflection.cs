﻿using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PythonSourceGenerator.Parser.Types;
using System.Collections.Generic;

namespace PythonSourceGenerator.Reflection;

public class ArgumentReflection
{
    private static readonly PythonTypeSpec TupleAny = new() { Name = "tuple", Arguments = [new PythonTypeSpec { Name = "Any" }] };
    private static readonly PythonTypeSpec DictStrAny = new() { Name = "dict", Arguments = [new PythonTypeSpec { Name = "str" }, new PythonTypeSpec { Name = "Any" }] };

    public static ParameterSyntax ArgumentSyntax(PythonFunctionParameter parameter)
    {
        TypeSyntax reflectedType;
        // Treat *args as tuple<Any> and **kwargs as dict<str, Any>
        if (parameter.IsStar)
        {
            reflectedType = TypeReflection.AsPredefinedType(TupleAny);
            parameter.DefaultValue = new PythonConstant { IsNone = true };
        }
        else if (parameter.IsDoubleStar)
        {
            reflectedType = TypeReflection.AsPredefinedType(DictStrAny);
            parameter.DefaultValue = new PythonConstant { IsNone = true };
        }
        else
        {
            reflectedType = TypeReflection.AsPredefinedType(parameter.Type);
        
        }

        if (parameter.DefaultValue == null)
        {
            // TODO : Mangle reserved keyword identifiers, e.g. "new"
            return SyntaxFactory
                .Parameter(SyntaxFactory.Identifier(parameter.Name.ToLowerPascalCase()))
                .WithType(reflectedType);
        } else
        {
            LiteralExpressionSyntax literalExpressionSyntax;
            if (parameter.DefaultValue.IsInteger)
                literalExpressionSyntax = SyntaxFactory.LiteralExpression(
                                                            SyntaxKind.NumericLiteralExpression,
                                                            SyntaxFactory.Literal(parameter.DefaultValue.IntegerValue));
            else if (parameter.DefaultValue.IsString)
                literalExpressionSyntax = SyntaxFactory.LiteralExpression(
                                                            SyntaxKind.StringLiteralExpression,
                                                            SyntaxFactory.Literal(parameter.DefaultValue.StringValue));
            else if (parameter.DefaultValue.IsFloat)
                literalExpressionSyntax = SyntaxFactory.LiteralExpression(
                                                            SyntaxKind.NumericLiteralExpression,
                                                            SyntaxFactory.Literal(parameter.DefaultValue.FloatValue));
            else if (parameter.DefaultValue.IsBool && parameter.DefaultValue.BoolValue == true)
                literalExpressionSyntax = SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);
            else if (parameter.DefaultValue.IsBool && parameter.DefaultValue.BoolValue == false)
                literalExpressionSyntax = SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);
            else if (parameter.DefaultValue.IsNone)
                literalExpressionSyntax = SyntaxFactory.LiteralExpression(
                                                            SyntaxKind.NullLiteralExpression);
            else
                // TODO : Handle other types?
                literalExpressionSyntax = SyntaxFactory.LiteralExpression(
                                                            SyntaxKind.NullLiteralExpression);
            // TODO : Mangle reserved keyword identifiers, e.g. "new"
            return SyntaxFactory
                .Parameter(SyntaxFactory.Identifier(parameter.Name.ToLowerPascalCase()))
                .WithType(reflectedType)
                .WithDefault(SyntaxFactory.EqualsValueClause(literalExpressionSyntax));

        }
    }

    public static ParameterListSyntax ParameterListSyntax(PythonFunctionParameter[] parameters)
    {
        var parameterListSyntax = new List<ParameterSyntax>();
        foreach (var pythonParameter in parameters)
        {
            // TODO : Handle Kind, see https://docs.python.org/3/library/inspect.html#inspect.Parameter
            parameterListSyntax.Add(ArgumentSyntax(pythonParameter));
        }
        return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameterListSyntax));
    }
}
