using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Knotty.Generators;

[DiagnosticAnalyzer (LanguageNames.CSharp)]
public class StateImmutabilityAnalyzer : DiagnosticAnalyzer
{
    private const string KnottyStoreMetadataName = "Knotty.KnottyStore`2";

    public static readonly DiagnosticDescriptor NotRecordRule = new DiagnosticDescriptor (
        id: "KNOT001",
        title: "TState should be a record",
        messageFormat: "'{0}' is used as TState but is not a record. Knotty requires immutable state — use a record type.",
        category: "Knotty.Design",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "KnottyStore requires immutable state. Declare TState as a record to prevent accidental mutation.");

    public static readonly DiagnosticDescriptor MutablePropertyRule = new DiagnosticDescriptor (
        id: "KNOT002",
        title: "State record property has a mutable setter",
        messageFormat: "Property '{0}' on state type '{1}' has a mutable setter. Use init-only properties to ensure immutability.",
        category: "Knotty.Design",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "State record properties should use init-only accessors (e.g., { get; init; }) to prevent mutation after construction.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create (NotRecordRule, MutablePropertyRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution ();
        context.RegisterSymbolAction (AnalyzeNamedType, SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var namedType = (INamedTypeSymbol)context.Symbol;

        // KnottyStore<TState, TIntent>를 직접 또는 간접 상속하는 클래스만 검사
        var stateType = GetKnottyStoreStateType (namedType, context.Compilation);
        if (stateType == null)
            return;

        // KNOT001: TState가 record가 아닌 경우
        if (!stateType.IsRecord)
        {
            var declaration = stateType.DeclaringSyntaxReferences.FirstOrDefault ();
            if (declaration != null)
            {
                var location = Location.Create (declaration.SyntaxTree, declaration.Span);
                context.ReportDiagnostic (Diagnostic.Create (NotRecordRule, location, stateType.Name));
            }
            // record가 아니면 KNOT002 검사는 의미 없음
            return;
        }

        // KNOT002: record 프로퍼티에 일반 setter가 있는 경우
        foreach (var member in stateType.GetMembers ())
        {
            if (member is not IPropertySymbol property)
                continue;

            // set accessor가 있고 init이 아닌 경우
            if (property.SetMethod != null && !property.SetMethod.IsInitOnly)
            {
                var declaration = property.DeclaringSyntaxReferences.FirstOrDefault ();
                if (declaration == null)
                    continue;

                var location = Location.Create (declaration.SyntaxTree, declaration.Span);
                context.ReportDiagnostic (Diagnostic.Create (MutablePropertyRule, location,
                    property.Name, stateType.Name));
            }
        }
    }

    /// <summary>
    /// 주어진 타입이 KnottyStore&lt;TState, TIntent&gt;를 상속하면 TState 타입을 반환합니다.
    /// 간접 상속도 탐색합니다.
    /// </summary>
    private static INamedTypeSymbol? GetKnottyStoreStateType(INamedTypeSymbol type, Compilation compilation)
    {
        var knottyStoreSymbol = compilation.GetTypeByMetadataName (KnottyStoreMetadataName);
        if (knottyStoreSymbol == null)
            return null;

        var current = type.BaseType;
        while (current != null)
        {
            if (current.IsGenericType &&
                SymbolEqualityComparer.Default.Equals (current.OriginalDefinition, knottyStoreSymbol))
            {
                // TState는 첫 번째 타입 인수
                return current.TypeArguments.Length > 0
                    ? current.TypeArguments[0] as INamedTypeSymbol
                    : null;
            }
            current = current.BaseType;
        }

        return null;
    }
}
