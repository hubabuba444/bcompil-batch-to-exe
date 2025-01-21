using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace BatchToExeCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Batch to EXE Compiler");

            // Parse command-line arguments for --input and --output
            string inputFile = null;
            string outputFile = null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--input" && i + 1 < args.Length)
                {
                    inputFile = args[i + 1];
                    i++; // Skip the next argument (the input file path)
                }
                else if (args[i] == "--output" && i + 1 < args.Length)
                {
                    outputFile = args[i + 1];
                    i++; // Skip the next argument (the output file path)
                }
            }

            // Validate input and output
            if (string.IsNullOrEmpty(inputFile))
            {
                Console.WriteLine("Error: Input batch file not specified. Use --input <path>");
                return;
            }

            if (string.IsNullOrEmpty(outputFile))
            {
                Console.WriteLine("Error: Output EXE file not specified. Use --output <path>");
                return;
            }

            if (File.Exists(inputFile))
            {
                Console.WriteLine("Compiling batch file to EXE...");
                CreateExeFromBatch(inputFile, outputFile);
            }
            else
            {
                Console.WriteLine("Error: Batch file not found.");
            }
        }

        static void CreateExeFromBatch(string batchFilePath, string exeFilePath)
        {
            try
            {
                // Read the batch file content
                string batchScript = File.ReadAllText(batchFilePath);

                // Compile the EXE with embedded batch content
                var compiler = new Microsoft.CSharp.CSharpCodeProvider();
                string sourceCode = GenerateExeSourceCode(batchScript);

                // Compiler parameters
                var parameters = new CompilerParameters
                {
                    GenerateExecutable = true,
                    OutputAssembly = exeFilePath,
                    ReferencedAssemblies =
                    {
                        "System.dll",        // Zestaw .NET Framework
                        "System.IO.dll"         // Dodanie zestawu do IO (do pracy z plikami)
                    }
                };


                // Compile the EXE and save it to the file system
                var result = compiler.CompileAssemblyFromSource(parameters, sourceCode);

                if (result.Errors.HasErrors)
                {
                    foreach (System.CodeDom.Compiler.CompilerError error in result.Errors)
                    {
                        Console.WriteLine($"Error: {error.ErrorText}");
                    }
                }
                else
                {
                    Console.WriteLine($"EXE successfully created at: {exeFilePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static string GenerateExeSourceCode(string batchScript)
        {
            // Generate C# code that will run the batch script when executed
            return @"
using System;
using System.Diagnostics;
using System.IO;

namespace BatchExe
{
    class Program
    {
        static void Main(string[] args)
        {
            string batchContent = @""" + EscapeBatchScript(batchScript) + @""";
            string tempBatchFile = Path.Combine(Path.GetTempPath(), ""temp.bat"");

            File.WriteAllText(tempBatchFile, batchContent);

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = tempBatchFile,
                UseShellExecute = true,
                CreateNoWindow = true
            };

            Process process = Process.Start(startInfo);
            process.WaitForExit();

            // Clean up the temporary batch file after execution
            File.Delete(tempBatchFile);
        }
    }
}";
        }

        static string EscapeBatchScript(string script)
        {
            // Escape the batch file script for embedding in a C# string literal
            return script.Replace("\"", "\\\"").Replace(Environment.NewLine, "\" + Environment.NewLine + \"");
        }
    }
}
