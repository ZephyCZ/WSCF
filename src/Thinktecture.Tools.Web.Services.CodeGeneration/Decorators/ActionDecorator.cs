using System;
using System.CodeDom;

namespace Thinktecture.Tools.Web.Services.CodeGeneration.Decorators
{
	/// <summary>
	/// Applies the SOAP actions (Action and ReplyAction) using the default WCF formatting.
	/// </summary>
	public class ActionDecorator : ICodeDecorator
	{
		/// <summary>
		/// Applies the decorator to the extended CodeDom tree.
		/// </summary>
		/// <param name="code">The extended CodeDom tree.</param>
		/// <param name="options">The custom code generation options.</param>
		public void Decorate(ExtendedCodeDomTree code, CustomCodeGenerationOptions options)
		{
			foreach (CodeTypeExtension serviceContract in code.ServiceContracts)
			{
				CodeAttributeDeclaration serviceContractAttribute = serviceContract.FindAttribute("System.ServiceModel.ServiceContractAttribute");
				if (serviceContractAttribute == null) continue;

				CodeAttributeArgument namespaceArgument = serviceContractAttribute.FindArgument("Namespace");
				if (namespaceArgument == null) continue;

				string serviceNamespace = (string)((CodePrimitiveExpression)namespaceArgument.Value).Value;
				string serviceName = serviceContract.ExtendedObject.Name;

				UpdateActions(serviceNamespace, serviceName, serviceContract, options);
			}
		}

		private static void UpdateActions(string serviceNamespace, string serviceName, CodeTypeExtension serviceContract, CustomCodeGenerationOptions options)
		{
			foreach (CodeTypeMemberExtension method in serviceContract.Methods)
			{
				CodeAttributeDeclaration operationAttribute = method.FindAttribute("System.ServiceModel.OperationContractAttribute");
				if (operationAttribute != null)
				{
					FormatActions(method, operationAttribute, serviceNamespace, serviceName, options);					
				}

				foreach (CodeAttributeDeclaration faultAttribute in method.FindAttributes("System.ServiceModel.FaultContractAttribute"))
				{
					FormatActions(method, faultAttribute, serviceNamespace, serviceName, options, isFault: true);					
				}
			}
		}

		private static void FormatActions(AttributableCodeDomObject method, CodeAttributeDeclaration targetAttribute, string serviceNamespace, string serviceName, 
			CustomCodeGenerationOptions options, bool isFault = false)
		{
			serviceNamespace = serviceNamespace.TrimEnd('/');
			string operationName = method.ExtendedObject.Name;
			string faultSuffix = isFault ? "Fault" : string.Empty;
            CodeAttributeArgument asyncPatternArgument = targetAttribute.FindArgument("AsyncPattern");
			if (asyncPatternArgument != null)
			{
				CodePrimitiveExpression value = asyncPatternArgument.Value as CodePrimitiveExpression;
				if (value != null && (bool)value.Value && operationName.StartsWith("Begin", StringComparison.OrdinalIgnoreCase))
				{
					operationName = operationName.Substring(5);
				}
			}

			CodeAttributeArgument actionArgument = targetAttribute.FindArgument("Action");
			if (actionArgument != null)
			{
                string action = string.Format("{0}:{1}{2}", serviceNamespace, operationName, faultSuffix);
                if (options.FormatSoapActions)
                    action = string.Format("{0}/{1}/{2}{3}", serviceNamespace, serviceName, operationName, faultSuffix);
				
				actionArgument.Value = new CodePrimitiveExpression(action);
			}

			CodeAttributeArgument replyArgument = targetAttribute.FindArgument("ReplyAction");
			if (replyArgument != null)
			{
                string action = string.Format("{0}:{1}Response", serviceNamespace, operationName);
                if (options.FormatSoapActions)
                    action = string.Format("{0}/{1}/{2}Response", serviceNamespace, serviceName, operationName);

				replyArgument.Value = new CodePrimitiveExpression(action);
            }
		}
	}
}