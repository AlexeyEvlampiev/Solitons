﻿// ------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version: 17.0.0.0
//  
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
// ------------------------------------------------------------------------------
namespace Solitons.CommandLine
{
    using System.Linq;
    using System.Text;
    using System.Collections.Generic;
    using System;
    
    /// <summary>
    /// Class to produce the template output
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TextTemplating", "17.0.0.0")]
    internal partial class CliActionHelpRtt : Solitons.Text.RuntimeTextTemplate
    {
        /// <summary>
        /// Create the template output
        /// </summary>
        public override string TransformText()
        {
            this.Write("Description:\r\n  ");
            this.Write(this.ToStringHelper.ToStringWithCulture(Description));
            this.Write("\r\n\r\nUsage:");
 foreach(var option in UsageOptions){ 
            this.Write(" \r\n  ");
            this.Write(this.ToStringHelper.ToStringWithCulture(ExecutableName));
            this.Write(" ");
            this.Write(this.ToStringHelper.ToStringWithCulture(option));
            this.Write(" ");
 } 
            this.Write(" \r\n  ");
 if(Arguments.Any()) { 
            this.Write("  \r\nArguments: ");
 foreach(string argument in Arguments){ 
            this.Write("  \r\n  ");
            this.Write(this.ToStringHelper.ToStringWithCulture(argument));
            this.Write(" ");
 } 
            this.Write("  \r\n");
 } 
            this.Write("  \r\nOptions:");
 foreach(string option in Options){ 
            this.Write("  \r\n  ");
            this.Write(this.ToStringHelper.ToStringWithCulture(option));
            this.Write(" ");
 } 
            this.Write("  ");
            return this.GenerationEnvironment.ToString();
        }
    }
}
