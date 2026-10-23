using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Syntax
{
    internal class FieldAccessTokenOperand(string name)
    {
        public string Name { get; set; } = name;

        public List<ExpressionToken>? ObjectTokens { get; set; }

        /// <summary>
        /// For syntax like field.method(arg1, ...), stores the function arguments.
        /// </summary>
        public List<BracketOperand>? Arguments { get; set; }
    }
}
