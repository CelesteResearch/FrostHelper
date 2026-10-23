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
    internal sealed class LambdaExpression(IList<string> argumentNames, AbstractExpression code) : AbstractExpression
    {
        public IList<string> ArgumentNames => argumentNames;

        public AbstractExpression Code => code;
    }
}
