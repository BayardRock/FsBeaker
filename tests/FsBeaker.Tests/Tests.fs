namespace FsBeaker.Tests

open NUnit.Framework
open System.Text
open FsBeaker
open FsBeaker.Kernel

[<TestFixture>]
type TestClass() = 

    let codeAndLocation(str:string) = 
        let findString = "||"
        let lines = str.Split('\n')
        let lineIndex = lines |> Array.findIndex (fun x -> x.Contains(findString))
        let charIndex = lines.[lineIndex].IndexOf(findString)
        let newCode = str.Replace(findString, "")
        newCode, lineIndex, charIndex

    [<Test>]
    member __.TestKernel() = 
    
        use client = ConsoleKernelClient.StartNewProcess()
        let code = 
            StringBuilder().AppendLine("[1..100]")
                .AppendLine("|> Seq.map float")
                .AppendLine("|> Seq.map (fun x -> x, x)")
                .AppendLine("|> Chart.Line")
                .ToString()

        let result = client.Execute(code)
        Assert.NotNull(result)
        Assert.AreEqual("image/png", result.Result.ContentType)

        let autoComplete = client.Autocomplete(code, 17)
        Assert.NotNull(autoComplete)
        Assert.AreEqual(68, autoComplete.Declarations.Length)

        let autoComplete2 = client.Autocomplete(code, 18)
        Assert.NotNull(autoComplete2)
        Assert.AreEqual(7, autoComplete2.Declarations.Length)
        Assert.AreEqual(7, autoComplete2.Declarations |> Seq.filter (fun x -> x.StartsWith("m")) |> Seq.length)

        let intellisense = client.Intellisense(code, 1, 7)
        Assert.NotNull(intellisense)
        Assert.AreEqual(68, intellisense.Declarations.Length)

        let intellisense2 = client.Intellisense(code, 1, 8)
        Assert.NotNull(intellisense2)
        Assert.AreEqual(68, intellisense2.Declarations.Length)

        let code2 = 
            StringBuilder()
                .AppendLine("#r \"FSharp.Data.dll\"")
                .AppendLine("open FSharp.Data")
                .AppendLine("let wb = WorldBankData.CreateContext()")
                .AppendLine("wb.Countries.``United States``.Indicators.||")
                .ToString()

        let newCode, lineIndex, charIndex = codeAndLocation code2
        let executed = client.Execute("#r \"FSharp.Data.dll\"")
        Assert.NotNull(executed)

        let intellisense3 = client.Intellisense(newCode, lineIndex, charIndex)
        Assert.NotNull(intellisense3)
