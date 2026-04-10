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
    private const string IntentCommandAttributeName = "Knotty.IntentCommandAttribute";
    private const string AsyncIntentCommandAttributeName = "Knotty.AsyncIntentCommandAttribute";

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

        // 소스 생성 (CompilationProvider 와 결합하지 않음 — Compilation 은 사용되지 않으며,
        // 결합하면 매 키 입력마다 generator 가 재실행되어 IDE 성능을 저하시킨다)
        context.RegisterSourceOutput(allMembers, static (spc, source) =>
        {
            Execute(source.Left, source.Right, spc);
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
                    var canExecuteKind = ResolveCanExecuteKind(fieldSymbol.ContainingType, canExecute, parameterType: null);

                    return new CommandMemberInfo(
                        MemberName: fieldSymbol.Name,
                        ClassName: fieldSymbol.ContainingType.Name,
                        Namespace: fieldSymbol.ContainingType.ContainingNamespace.ToDisplayString(),
                        IsAsync: isAsyncIntentCommand,
                        IsMethod: false,
                        ParameterType: null,
                        CommandName: commandName,
                        CanExecute: canExecute,
                        CanExecuteKind: canExecuteKind);
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

                var canExecuteKind = ResolveCanExecuteKind(methodSymbol.ContainingType, canExecute, parameterType);

                return new CommandMemberInfo(
                    MemberName: methodSymbol.Name,
                    ClassName: methodSymbol.ContainingType.Name,
                    Namespace: methodSymbol.ContainingType.ContainingNamespace.ToDisplayString(),
                    IsAsync: isAsyncIntentCommand,
                    IsMethod: true,
                    ParameterType: parameterType,
                    CommandName: commandName,
                    CanExecute: canExecute,
                    CanExecuteKind: canExecuteKind);
            }
        }

        return null;
    }

    private static CanExecuteKind ResolveCanExecuteKind(
        INamedTypeSymbol containingType,
        string? canExecuteName,
        string? parameterType)
    {
        if (string.IsNullOrEmpty(canExecuteName)) return CanExecuteKind.None;

        // 오버로드 우선순위:
        //   1순위: 파라미터 타입이 일치하는 메서드 (parameterized command 일 때만 의미 있음)
        //   2순위: 파라미터 없는 메서드 / bool 프로퍼티
        CanExecuteKind? fallback = null;

        for (var t = containingType; t is not null; t = t.BaseType)
        {
            foreach (var m in t.GetMembers(canExecuteName!))
            {
                switch (m)
                {
                    case IMethodSymbol method when method.MethodKind == MethodKind.Ordinary
                                                && method.ReturnType.SpecialType == SpecialType.System_Boolean:
                        if (parameterType is not null
                            && method.Parameters.Length == 1
                            && method.Parameters[0].Type.ToDisplayString() == parameterType)
                        {
                            return CanExecuteKind.MethodWithParam; // 1순위 — 즉시 반환
                        }
                        if (method.Parameters.Length == 0)
                        {
                            fallback ??= CanExecuteKind.Method;
                        }
                        break;

                    case IPropertySymbol p when p.Type.SpecialType == SpecialType.System_Boolean:
                        fallback ??= CanExecuteKind.Property;
                        break;
                }
            }
        }

        // 못 찾으면 Method 로 가정 → 사용자 코드에서 자연스러운 컴파일 에러로 드러남
        return fallback ?? CanExecuteKind.Method;
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
        sb.AppendLine("using Knotty;");
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
            var canExecuteParam = BuildCanExecuteParam(member);

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

        // CanExecute가 있는 멤버가 하나라도 있으면 OnStateChanged override를 emit한다.
        // KnottyStore.OnStateChanged()는 State/IsLoading 변경 시 호출되므로,
        // 여기서 각 Command의 RaiseCanExecuteChanged()를 직접 호출한다.
        // CommandManager.InvalidateRequerySuggested()는 RoutedCommand 전용이라 IntentCommand에 효과 없음.
        var canExecuteMembers = members.Where(m => m.CanExecuteKind != CanExecuteKind.None).ToList();
        if (canExecuteMembers.Count > 0)
        {
            sb.AppendLine($"    protected override void OnStateChanged()");
            sb.AppendLine($"    {{");
            foreach (var m in canExecuteMembers)
            {
                var cmdName = m.CommandName ?? GenerateCommandName(m.MemberName, m.IsMethod);
                sb.AppendLine($"        (_{ToCamelCase(cmdName)} as Knotty.INotifyCanExecuteChanged)?.RaiseCanExecuteChanged();");
            }
            sb.AppendLine($"    }}");
            sb.AppendLine();
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string BuildCanExecuteParam(CommandMemberInfo m)
    {
        if (m.CanExecuteKind == CanExecuteKind.None || string.IsNullOrEmpty(m.CanExecute))
            return "";

        bool isParameterizedCommand = m.IsMethod && m.ParameterType != null;

        return (m.CanExecuteKind, isParameterizedCommand) switch
        {
            // Command(intent, Func<bool>)
            (CanExecuteKind.Method,          false) => $", {m.CanExecute}",
            (CanExecuteKind.Property,        false) => $", () => {m.CanExecute}",
            (CanExecuteKind.MethodWithParam, false) => $", {m.CanExecute}",

            // Command<TParam>(factory, Func<TParam,bool>)
            (CanExecuteKind.Method,          true)  => $", _ => {m.CanExecute}()",
            (CanExecuteKind.Property,        true)  => $", _ => {m.CanExecute}",
            (CanExecuteKind.MethodWithParam, true)  => $", {m.CanExecute}",

            _ => "",
        };
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

internal enum CanExecuteKind
{
    None,
    Method,           // () => bool  →  Func<bool>
    MethodWithParam,  // (TParam) => bool  →  Func<TParam, bool>
    Property,         // bool 값 — 람다로 래핑 필요
}

internal record CommandMemberInfo(
    string MemberName,
    string ClassName,
    string Namespace,
    bool IsAsync,
    bool IsMethod,
    string? ParameterType,
    string? CommandName,
    string? CanExecute,
    CanExecuteKind CanExecuteKind);
