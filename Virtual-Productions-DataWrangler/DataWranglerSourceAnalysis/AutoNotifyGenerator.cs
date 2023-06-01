//https://github.com/dotnet/roslyn-sdk/blob/main/samples/CSharp/SourceGenerators/SourceGeneratorSamples/AutoNotifyGenerator.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DataWranglerSourceAnalysis
{
	[Generator]
	public class AutoNotifyGenerator : ISourceGenerator
	{
		public DiagnosticDescriptor ClassMustBeTopLevelDiagnostic = new DiagnosticDescriptor("AN001", "AutoNotify class should be top level", "Classes containing AutoFormat parameters should be top level. Class {0}.", "Usage", DiagnosticSeverity.Error, true);
		public DiagnosticDescriptor FieldCannotBeProcessed = new DiagnosticDescriptor("AN002", "AutoNotify field has conflicting name", "AutoNotify field could not deduce a unique property name. Class: {0} Field: {1} Chosen property name: {2}.", "Usage", DiagnosticSeverity.Error, true);

		public void Initialize(GeneratorInitializationContext context)
		{
			// Register a syntax receiver that will be created for each generation pass
			context.RegisterForSyntaxNotifications(() => new AutoNotifySyntaxReceiver());
		}

		public void Execute(GeneratorExecutionContext context)
		{
			// retrieve the populated receiver 
			if (!(context.SyntaxContextReceiver is AutoNotifySyntaxReceiver receiver))
				return;

			// get the added attribute, and INotifyPropertyChanged
			INamedTypeSymbol? attributeSymbol = context.Compilation.GetTypeByMetadataName("AutoNotify.AutoNotifyAttribute");
			INamedTypeSymbol? notifySymbol = context.Compilation.GetTypeByMetadataName("System.ComponentModel.INotifyPropertyChanged");

			if (attributeSymbol == null)
			{
				throw new Exception("Failed to find attribute symbol");
			}

			if (notifySymbol == null)
			{
				throw new Exception("Failed to find notify symbol");
			}

			// group the fields by class, and generate the source
			foreach (IGrouping<INamedTypeSymbol, IFieldSymbol> group in receiver.Fields.GroupBy<IFieldSymbol, INamedTypeSymbol>(f => f.ContainingType, SymbolEqualityComparer.Default))
			{
				string classSource = ProcessClass(group.Key, group.ToList(), attributeSymbol, notifySymbol, context);
				context.AddSource($"{group.Key.Name}_AutoNotify.cs", SourceText.From(classSource, Encoding.UTF8));
			}
		}

		private string ProcessClass(INamedTypeSymbol classSymbol, List<IFieldSymbol> fields, ISymbol attributeSymbol, ISymbol notifySymbol, GeneratorExecutionContext context)
		{
			if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
			{
				context.ReportDiagnostic(Diagnostic.Create(ClassMustBeTopLevelDiagnostic, classSymbol.Locations[0], classSymbol.Name));
				return string.Empty;
			}

			string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

			// begin building the generated source
			StringBuilder source = new StringBuilder($@"
namespace {namespaceName}
{{
    public partial class {classSymbol.Name} : {notifySymbol.ToDisplayString()}
    {{
");

			// if the class doesn't implement INotifyPropertyChanged already, add it
			if (!classSymbol.AllInterfaces.Contains(notifySymbol, SymbolEqualityComparer.Default))
			{
				bool baseContainsAutoNotify = false;
				if (classSymbol.BaseType != null && !classSymbol.BaseType.Equals(context.Compilation.ObjectType, SymbolEqualityComparer.Default))
				{
					foreach (ISymbol baseField in classSymbol.BaseType.GetMembers())
					{
						if (baseField is IFieldSymbol baseTypedField)
						{
							AttributeData? data = baseTypedField.GetAttributes().SingleOrDefault(ad => ad.AttributeClass!.Equals(attributeSymbol, SymbolEqualityComparer.Default));
							if (data != null)
							{
								baseContainsAutoNotify = true;
								break;
							}
						}
					}
				}

				if (!baseContainsAutoNotify)
				{
					source.AppendLine("public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;");
					source.AppendLine(@"protected void OnAutoPropertyChanged(string a_propertyName) { this.PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(a_propertyName)); }");
				}
			}

			// create properties for each field 
			foreach (IFieldSymbol fieldSymbol in fields)
			{
				ProcessField(source, classSymbol.Name, fieldSymbol, attributeSymbol, context);
			}

			source.Append("} }");
			return source.ToString();
		}

		private void ProcessField(StringBuilder a_source, string a_className, IFieldSymbol a_fieldSymbol, ISymbol a_attributeSymbol, GeneratorExecutionContext context)
		{
			// get the name and type of the field
			string fieldName = a_fieldSymbol.Name;
			ITypeSymbol fieldType = a_fieldSymbol.Type;

			// get the AutoNotify attribute from the field, and any associated data
			AttributeData attributeData = a_fieldSymbol.GetAttributes().Single(a_ad => a_ad.AttributeClass!.Equals(a_attributeSymbol, SymbolEqualityComparer.Default));
			TypedConstant overridenNameOpt = attributeData.NamedArguments.SingleOrDefault(a_kvp => a_kvp.Key == "PropertyName").Value;

			string propertyName = ChooseName(fieldName, overridenNameOpt);
			if (propertyName.Length == 0 || propertyName == fieldName)
			{
				context.ReportDiagnostic(Diagnostic.Create(FieldCannotBeProcessed, null, a_fieldSymbol.Locations, a_className, fieldName, propertyName));
				return;
			}

			a_source.Append($@"
[AutoNotify.AutoNotifyProperty(""{fieldName}""), Newtonsoft.Json.JsonIgnore]
public {fieldType} {propertyName} 
{{
    get 
    {{
        return this.{fieldName};
    }}

    set
    {{
        this.{fieldName} = value;
        this.OnAutoPropertyChanged(nameof({propertyName}));
    }}
}}

");

			string ChooseName(string a_fieldName, TypedConstant a_overridenNameOpt)
			{
				if (!a_overridenNameOpt.IsNull)
				{
					return a_overridenNameOpt.Value!.ToString();
				}

				if (a_fieldName.StartsWith("m_"))
				{
					a_fieldName = a_fieldName.Substring(2);
				}

				if (a_fieldName.Length == 0)
					return string.Empty;

				if (a_fieldName.Length == 1)
					return a_fieldName.ToUpper();

				return a_fieldName.Substring(0, 1).ToUpper() + a_fieldName.Substring(1);
			}
		}

		/// <summary>
		/// Created on demand before each generation pass
		/// </summary>
		class AutoNotifySyntaxReceiver : ISyntaxContextReceiver
		{
			public List<IFieldSymbol> Fields { get; } = new List<IFieldSymbol>();

			/// <summary>
			/// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
			/// </summary>
			public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
			{
				// any field with at least one attribute is a candidate for property generation
				if (context.Node is FieldDeclarationSyntax fieldDeclarationSyntax
				    && fieldDeclarationSyntax.AttributeLists.Count > 0)
				{
					foreach (VariableDeclaratorSyntax variable in fieldDeclarationSyntax.Declaration.Variables)
					{
						// Get the symbol being declared by the field, and keep it if its annotated
						IFieldSymbol? fieldSymbol = context.SemanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
						if (fieldSymbol != null && fieldSymbol.GetAttributes().Any(ad => ad.AttributeClass?.ToDisplayString() == "AutoNotify.AutoNotifyAttribute"))
						{
							Fields.Add(fieldSymbol);
						}
					}
				}
			}
		}
	}
}