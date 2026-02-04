using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Knotty.Generators;

[Generator]
public class IntentCommandGenerator : IIncrementalGenerator
{
    private const string IntentCommandAttributeName = "Knotty.Core.Attributes.IntentCommandAttribute";
    private const string AsyncIntentCommandAttributeName = "Knotty.Core.Attributes.AsyncIntentCommandAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 어트리뷰트가 있는 필드를 찾음
        var fieldDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsFieldWithAttribute(s),
                transform: static (ctx, _) => GetFieldInfo(ctx))
            .Where(static m => m is not null)
            .Select(static (m, _) => m!);

        // 어트리뷰트가 있는 메서드를 찾음
        var methodDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsMethodWithAttribute(s),
                transform: static (ctx, _) => GetMethodInfo(ctx))
            .Where(static m => m is not null)
            .Select(static (m, _) => m!);

        // 필드와 메서드를 합침
        var allMembers = fieldDeclarations.Collect()
            .Combine(methodDeclarations.Collect());

        // 컴파일 정보와 결합
        var compilationAndMembers = context.CompilationProvider.Combine(allMembers);

        // 소스 생성
        context.RegisterSourceOutput(compilationAndMembers, static (spc, source) => 
        {
            var fields = source.Right.Left;
            var methods = source.Right.Right;
            Execute(fields, methods, spc);
        });
    }

    private static bool IsFieldWithAttribute(SyntaxNode node)
    {
        return node is FieldDeclarationSyntax field && field.AttributeLists.Count > 0;
    }

    private static bool IsMethodWithAttribute(SyntaxNode node)
    {
        return node is MethodDeclarationSyntax method && method.AttributeLists.Count > 0;
    }

    private static CommandMemberInfo? GetFieldInfo(GeneratorSyntaxContext context)
    {
        var fieldDeclaration = (FieldDeclarationSyntax)context.Node;

        foreach (var variable in fieldDeclaration.Declaration.Variables)
        {
            var fieldSymbol = context.SemanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
            if (fieldSymbol is null) continue;

            foreach (var attribute in fieldSymbol.GetAttributes())
            {
                var attrClass = attribute.AttributeClass;
                if (attrClass is null) continue;

                var attrName = attrClass.Name;
                var attrFullName = attrClass.ToDisplayString();

                bool isIntentCommand = attrName == "IntentCommandAttribute" || 
                                       attrFullName == IntentCommandAttributeName;
                bool isAsyncIntentCommand = attrName == "AsyncIntentCommandAttribute" || 
                                            attrFullName == AsyncIntentCommandAttributeName;

                if (isIntentCommand || isAsyncIntentCommand)
                {
                    var commandName = GetNamedArgumentValue(attribute, "CommandName");
                    var canExecute = GetNamedArgumentValue(attribute, "CanExecute");

                    return new CommandMemberInfo(
                        MemberName: fieldSymbol.Name,
                        ClassName: fieldSymbol.ContainingType.Name,
                        Namespace: fieldSymbol.ContainingType.ContainingNamespace.ToDisplayString(),
                        IsAsync: isAsyncIntentCommand,
                        IsMethod: false,
                        ParameterType: null,
                        CommandName: commandName,
                        CanExecute: canExecute);
                }
            }
        }

        return null;
    }

    private static CommandMemberInfo? GetMethodInfo(GeneratorSyntaxContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;
        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration) as IMethodSymbol;
        if (methodSymbol is null) return null;

        foreach (var attribute in methodSymbol.GetAttributes())
        {
            var attrClass = attribute.AttributeClass;
            if (attrClass is null) continue;

            var attrName = attrClass.Name;
            var attrFullName = attrClass.ToDisplayString();

            bool isIntentCommand = attrName == "IntentCommandAttribute" || 
                                   attrFullName == IntentCommandAttributeName;
            bool isAsyncIntentCommand = attrName == "AsyncIntentCommandAttribute" || 
                                        attrFullName == AsyncIntentCommandAttributeName;

            if (isIntentCommand || isAsyncIntentCommand)
            {
                var commandName = GetNamedArgumentValue(attribute, "CommandName");
                var canExecute = GetNamedArgumentValue(attribute, "CanExecute");

                // 메서드의 첫 번째 파라미터 타입 가져오기
                string? parameterType = null;
                if (methodSymbol.Parameters.Length > 0)
                {
                    parameterType = methodSymbol.Parameters[0].Type.ToDisplayString();
                }

                return new CommandMemberInfo(
                    MemberName: methodSymbol.Name,
                    ClassName: methodSymbol.ContainingType.Name,
                    Namespace: methodSymbol.ContainingType.ContainingNamespace.ToDisplayString(),
                    IsAsync: isAsyncIntentCommand,
                    IsMethod: true,
                    ParameterType: parameterType,
                    CommandName: commandName,
                    CanExecute: canExecute);
            }
        }

        return null;
    }

    private static string? GetNamedArgumentValue(AttributeData attribute, string name)
    {
        foreach (var namedArg in attribute.NamedArguments)
        {
            if (namedArg.Key == name && namedArg.Value.Value is string value)
            {
                return value;
            }
        }
        return null;
    }

    private static void Execute(ImmutableArray<CommandMemberInfo> fields, ImmutableArray<CommandMemberInfo> methods, SourceProductionContext context)
    {
        var allMembers = fields.Concat(methods).ToList();
        if (allMembers.Count == 0) return;

        var groupedByClass = allMembers.GroupBy(m => (m.Namespace, m.ClassName));

        foreach (var group in groupedByClass)
        {
            var memberList = group.ToList();
            var source = GenerateClassSource(group.Key.Namespace, group.Key.ClassName, memberList);
            var fileName = $"{group.Key.ClassName}.Commands.g.cs";
            context.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
        }
    }

    private static string GenerateClassSource(string namespaceName, string className, List<CommandMemberInfo> members)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System.Windows.Input;");
        sb.AppendLine("using Knotty.Core.Commands;");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(namespaceName) && namespaceName != "<global namespace>")
        {
            sb.AppendLine($"namespace {namespaceName};");
            sb.AppendLine();
        }

        sb.AppendLine($"partial class {className}");
        sb.AppendLine("{");

        foreach (var member in members)
        {
            var commandName = member.CommandName ?? GenerateCommandName(member.MemberName, member.IsMethod);
            var commandType = member.IsAsync ? "IAsyncCommand" : "ICommand";
            var methodName = member.IsAsync ? "AsyncCommand" : "Command";
            var canExecuteParam = string.IsNullOrEmpty(member.CanExecute) ? "" : $", {member.CanExecute}";

            sb.AppendLine($"    private {commandType}? _{ToCamelCase(commandName)};");

            if (member.IsMethod && member.ParameterType != null)
            {
                // 메서드 + 파라미터 있음 → Command<TParameter>(factory)
                sb.AppendLine($"    public {commandType} {commandName} => _{ToCamelCase(commandName)} ??= {methodName}<{member.ParameterType}>({member.MemberName}{canExecuteParam});");
            }
            else
            {
                // 필드 또는 파라미터 없는 메서드
                sb.AppendLine($"    public {commandType} {commandName} => _{ToCamelCase(commandName)} ??= {methodName}({member.MemberName}{canExecuteParam});");
            }
            sb.AppendLine();
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string GenerateCommandName(string memberName, bool isMethod)
    {
        var name = memberName.TrimStart('_');

        // 메서드인 경우 Create, Get 등의 접두사 제거
        if (isMethod)
        {
            if (name.StartsWith("Create")) name = name.Substring(6);
            else if (name.StartsWith("Get")) name = name.Substring(3);
            else if (name.StartsWith("Make")) name = name.Substring(4);
        }

        if (name.Length == 0) return "Command";

        return char.ToUpperInvariant(name[0]) + name.Substring(1) + "Command";
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }
}

internal record CommandMemberInfo(
    string MemberName,
    string ClassName,
    string Namespace,
    bool IsAsync,
    bool IsMethod,
    string? ParameterType,
    string? CommandName,
    string? CanExecute);
