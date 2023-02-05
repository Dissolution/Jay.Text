using System;
using System.Diagnostics;
using Jay.Text.Building;

using var writer = new TextWriter();

int id = 147;

writer.IndentBlock("    ", w =>
{
    w.Write($$"""
        public void Bliss()
        {
            Console.WriteLine("{{id}}");
        }
        """);
});

string text = writer.ToString();

Console.WriteLine(text);

Debugger.Break();

Console.WriteLine("Press Enter to close");
Console.ReadLine();
