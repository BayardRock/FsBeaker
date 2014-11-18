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

    let testSimpleExecute(client: ConsoleKernelClient) = 
        let code = "[1..1000]"
        let result = client.Execute(code)
        
        Assert.AreEqual(ExecuteReponseStatus.OK, result.Status)
        Assert.AreEqual("text/plain", result.Result.ContentType)

    let testChartAndIntellisense(client: ConsoleKernelClient) = 
        let code = 
            StringBuilder().AppendLine("[1..100]")
                .AppendLine("|> Seq.map float")
                .AppendLine("|> Seq.map (fun x -> x, x)")
                .AppendLine("|> Chart.Line")
                .ToString()

        let result = client.Execute(code)
        Assert.NotNull(result)
        Assert.AreEqual("image/png", result.Result.ContentType)

        let intellisense = client.Intellisense(code, 1, 7)
        Assert.NotNull(intellisense)
        Assert.AreEqual(68, intellisense.Declarations.Length)

        let intellisense2 = client.Intellisense(code, 1, 8)
        Assert.NotNull(intellisense2)
        Assert.AreEqual(68, intellisense2.Declarations.Length)

    let testWorldBankDataAndIntellisense(client: ConsoleKernelClient) =
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

    [<Test>]
    member __.TestKernel() = 
    
        use client = ConsoleKernelClient.StartNewProcess()

        // test sync
        testSimpleExecute client 
        testChartAndIntellisense client
        testWorldBankDataAndIntellisense client

        // test async
        let testAll() =
            testSimpleExecute client 
            testChartAndIntellisense client
            testWorldBankDataAndIntellisense client

        Async.Parallel [for i in 0..20 -> async { testAll() }]
        |> Async.RunSynchronously
        |> ignore
