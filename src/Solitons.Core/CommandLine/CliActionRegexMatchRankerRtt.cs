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
    internal partial class CliActionRegexMatchRankerRtt : Solitons.Text.RuntimeTextTemplate
    {
        /// <summary>
        /// Create the template output
        /// </summary>
        public override string TransformText()
        {
            this.Write("^\\s*\\S+\r\n(?:\r\n    # --- OPTIMAL MATCH\r\n    (?<");
            this.Write(this.ToStringHelper.ToStringWithCulture(OptimalMatchGroupName));
            this.Write(">\r\n      (?:\r\n        # --- command segments ---\r\n        ");
 foreach(var segmentExp in CommandSegmentRegularExpressions){ 
            this.Write(" \r\n        \\s+ ");
            this.Write(this.ToStringHelper.ToStringWithCulture(segmentExp));
            this.Write(" ");
 }/* Loop over command segments */ 
            this.Write(" \r\n        # --- command segments ---\r\n        # --- options ---\r\n        (?:\r\n  " +
                    "        \\s+\r\n          (?:$ ");
 foreach(var optionExp in OptionRegularExpressions ){ 
            this.Write(" \r\n             | (?: ");
            this.Write(this.ToStringHelper.ToStringWithCulture(optionExp));
            this.Write(") ");
 } /* Loop over command options */
            this.Write(" \r\n          )\r\n        )*\r\n        # --- options ---\r\n      )\r\n      \\s*$\r\n    )" +
                    " |\r\n    # --- FUZZY MATCH\r\n    (?:\r\n      \\s+\r\n      (?:$");
 foreach(var segmentExp in CommandSegmentRegularExpressions){ 
            this.Write(" \r\n         | ");
            this.Write(this.ToStringHelper.ToStringWithCulture(segmentExp));
            this.Write(" ");
 }/* Loop over command segments */ 
            this.Write(" \r\n         ");
 foreach(var optionExp in OptionRegularExpressions ){ 
            this.Write(" \r\n         | (?:");
            this.Write(this.ToStringHelper.ToStringWithCulture(optionExp));
            this.Write(") ");
 } /* Loop over command options */
            this.Write(" \r\n         | \\S+\r\n      )    \r\n    )*\r\n)");
            return this.GenerationEnvironment.ToString();
        }
    }
}
