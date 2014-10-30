namespace FsBeaker.Kernel

open System
open System.IO
open System.Text
open Microsoft.FSharp.Compiler.Interactive.Shell

[<AutoOpen>]
module Evaluation = 

    let internal sbOut = new StringBuilder()
    let internal sbErr = new StringBuilder()
    let internal inStream = new StringReader("")
    let internal outStream = new StringWriter(sbOut)
    let internal errStream = new StringWriter(sbErr)
    let internal fsiConfig = FsiEvaluationSession.GetDefaultConfiguration()
    let internal fsiEval = FsiEvaluationSession.Create(fsiConfig, [|"--noninteractive"|], inStream, outStream, errStream)

    let PreprocessSource (source:string, character) =
        let lines = source.Split([| '\n' |])
        let mutable offset = character
        let mutable total = 0
        let mutable idx = 0
        while idx < lines.Length && total + lines.[idx].Length < character do
            offset <- offset - lines.[idx].Length - 1
            total <- total + lines.[idx].Length + 1
            idx <- idx + 1
        lines, idx, offset

    let GetDeclarations(source, character) = 
        let (lines, lineIndex, charIndex) = PreprocessSource(source, character)
        let (parse, c1, c2) = fsiEval.ParseAndCheckInteraction(source)
        let line = lines.[lineIndex]
        let (names, startIdx) = FsCompilerInternals.extractNames(line, charIndex)
        let filterString = line.Substring(startIdx, charIndex - startIdx)
        let decls = 
            c1.GetDeclarationListInfo(Some(parse), lineIndex + 1, charIndex + 1, line, names, filterString)
            |> Async.RunSynchronously

        decls, filterString

    /// Gets `it` only if `it` was printed to the console
    let GetLastExpression() =

        let lines = 
            sbOut.ToString().Split('\r', '\n')
            |> Seq.filter (fun x -> x <> "")
            |> Seq.toArray

        let index = lines |> Seq.tryFindIndex (fun x -> x.StartsWith("val it : "))
        if index.IsSome then
            try 
                fsiEval.EvalExpression("it")
            with _ -> None
        else 
            None
