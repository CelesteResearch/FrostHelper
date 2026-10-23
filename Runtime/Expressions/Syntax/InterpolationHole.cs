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
    internal record InterpolationHole(bool IsLiteral)
    {
        public string PreEndBracketTrivia { get; init; }

        public List<ExpressionToken> Tokens { get; init; }
    }
}
