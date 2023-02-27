﻿// ReSharper disable once CheckNamespace
namespace Jay.Text.Code.CSharpCodeBuilderExtensions;

public static class CSharpCodeBuilderExtensions
{
    public static string DefaultIndent { get; set; } = "    ";

#region Fluent CS File
    /// <summary>
    /// Adds the `// &lt;auto-generated/&gt; ` line, optionally expanding it to include a <paramref name="comment"/>
    /// </summary>
    public static CodeBuilder AutoGeneratedHeader(this CodeBuilder codeBuilder, string? comment = null)
    {
        if (comment is null)
        {
            return codeBuilder.AppendLine("// <auto-generated/>");
        }

        return codeBuilder
            .AppendLine("// <auto-generated>")
            .Enumerate(comment.TextSplit(Environment.NewLine), static (cb, cl) => cb.Append("// ").AppendLine(cl))
            .AppendLine("// </auto-generated>");
    }

    public static CodeBuilder Nullable(this CodeBuilder codeBuilder, bool enable = true)
    {
        return codeBuilder
            .Append("#nullable ")
            .Append(enable ? "enable" : "disable")
            .NewLine();
    }

    /// <summary>
    /// Writes a `using <paramref name="nameSpace"/>;` line
    /// </summary>
    public static CodeBuilder Using(this CodeBuilder codeBuilder, string nameSpace)
    {
        ReadOnlySpan<char> ns = nameSpace
            .AsSpan()
            .TrimStart()
            .TrimStart("using ".AsSpan())
            .TrimEnd()
            .TrimEnd(';');
        if (ns.Length > 0)
        {
            return codeBuilder.Append("using ").Append(ns).AppendLine(';');
        }
        return codeBuilder;
    }

    /// <summary>
    /// Writes multiple <c>using</c> <paramref name="namespaces"/>
    /// </summary>
    public static CodeBuilder Using(this CodeBuilder codeBuilder, params string[] namespaces)
    {
        foreach (var nameSpace in namespaces)
        {
            Using(codeBuilder, nameSpace);
        }
        return codeBuilder;
    }

    public static CodeBuilder Namespace(this CodeBuilder codeBuilder, string nameSpace)
    {
        ReadOnlySpan<char> ns = nameSpace.AsSpan().Trim();
        if (ns.Length == 0)
            throw new ArgumentException("Invalid namespace", nameof(nameSpace));
        return codeBuilder.Append("namespace ").Append(ns).AppendLine(';');
    }


    /// <summary>
    /// Writes the given <paramref name="comment"/> as a comment line / lines
    /// </summary>
    public static CodeBuilder Comment(this CodeBuilder codeBuilder, string? comment)
    {
        /* Most of the time, this is probably a single line.
         * But we do want to watch out for newline characters to turn
         * this into a multi-line comment */


        var e = comment.TextSplit(Environment.NewLine).GetEnumerator();
        // Null or empty comment is blank
        if (!e.MoveNext())
        {
            return codeBuilder.AppendLine("// ");
        }
        // Only a single comment
        if (e.AtEnd)
        {
            // Single line
            return codeBuilder.Append("// ").AppendLine(e.Current);
        }
        
        // Multiple comments
        codeBuilder.Append("/* ").AppendLine(e.Current);
        while (e.MoveNext())
        {
            codeBuilder.Append(" * ").AppendLine(e.Current);
        }
        return codeBuilder.AppendLine(" */");
    }

    public static CodeBuilder Comment(this CodeBuilder codeBuilder, string? comment, CommentType commentType)
    {
        var splitEnumerable = comment.TextSplit(Environment.NewLine);
        if (commentType == CommentType.SingleLine)
        {
            foreach (var line in splitEnumerable)
            {
                codeBuilder.Append("// ").AppendLine(line);
            }
        }
        else if (commentType == CommentType.XML)
        {
            foreach (var line in splitEnumerable)
            {
                codeBuilder.Append("/// ").AppendLine(line);
            }
        }
        else
        {
            var e = splitEnumerable.GetEnumerator();
            // Null or empty comment is blank
            if (!e.MoveNext())
            {
                return codeBuilder.AppendLine("/* */");
            }
            // Only a single comment
            if (e.AtEnd)
            {
                // Single line
                return codeBuilder.Append("/* ").Append(e.Current).AppendLine(" */");
            }
        
            // Multiple comments
            codeBuilder.Append("/* ").AppendLine(e.Current);
            while (e.MoveNext())
            {
                codeBuilder.Append(" * ").AppendLine(e.Current);
            }
            codeBuilder.AppendLine(" */");
        }

        return codeBuilder;
    }
#endregion

    public static CodeBuilder IndentBlock(this CodeBuilder codeBuilder, TextBuilderAction<CodeBuilder> indentBlock)
        => codeBuilder.IndentBlock(DefaultIndent, indentBlock);


    public static CodeBuilder BracketBlock(this CodeBuilder codeBuilder, TextBuilderAction<CodeBuilder> bracketBlock, string? indent = null)
    {
        indent ??= DefaultIndent;
        // Trim all trailing whitespace
        codeBuilder.TrimEnd()
            .NewLine()
            .AppendLine('{')
            .IndentBlock(indent, bracketBlock);
        if (!codeBuilder.Written.EndsWith(codeBuilder._newLineIndent.AsSpan()))
        {
            codeBuilder.NewLine();
        }
        return codeBuilder.Append('}');
    }
}