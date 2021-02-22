namespace Cake.Addin.Analyzer.Rules
{
	using System.Linq;
	using Cake.Addin.Analyzer.Constants;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;
	using Microsoft.CodeAnalysis.Diagnostics;

	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class AliasMethodMarkedRule : BaseRule
	{
		public AliasMethodMarkedRule()
			: base(
				  Identifiers.AliasMethodMarkedRule,
				  nameof(Resources.MethodMarkedTitle),
				  nameof(Resources.MethodMarkedDescription),
				  nameof(Resources.MethodMarkedMessageFormat),
				  Categories.Usage,
				  severity: DiagnosticSeverity.Error,
				  customTags: "Cake Build")
		{
		}

		protected override void RegisterActions(AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(AnalyzeMethodNode, SyntaxKind.MethodDeclaration);
		}

		private void AnalyzeMethodNode(SyntaxNodeAnalysisContext obj)
		{
			if (!(obj.Node is MethodDeclarationSyntax methodDecl) || !IsPublic(methodDecl))
			{
				return;
			}

			var firstParameter = methodDecl.ParameterList.Parameters.FirstOrDefault();

			if (firstParameter is null ||
				!HasExpectedParameter(obj, firstParameter, "Cake.Core.ICakeContext") ||
				!firstParameter.Modifiers.Any(SyntaxKind.ThisKeyword))
			{
				return;
			}

			var attributes = methodDecl.AttributeLists.SelectMany(a => a.Attributes);
			if (attributes.Any(a =>
				HasExpectedAttribute(obj, a, "Cake.Core.Annotations.CakeMethodAliasAttribute") ||
				HasExpectedAttribute(obj, a, "Cake.Core.Annotations.CakePropertyAliasAttribute")))
			{
				return;
			}

			var diagnostic = Diagnostic.Create(Rule, methodDecl.Identifier.GetLocation(), methodDecl.Identifier.Text);
			obj.ReportDiagnostic(diagnostic);
		}

		private bool IsPublic(MemberDeclarationSyntax? methodDecl)
		{
			if (methodDecl is null || methodDecl is NamespaceDeclarationSyntax)
			{
				return true;
			}

			if (!methodDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
			{
				return false;
			}

			return IsPublic(methodDecl.Parent as MemberDeclarationSyntax);
		}
	}
}
